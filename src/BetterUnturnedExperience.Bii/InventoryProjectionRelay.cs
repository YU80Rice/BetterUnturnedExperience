using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    internal readonly struct InventoryItemFingerprint : IEquatable<InventoryItemFingerprint>
    {
        public ItemAssetIdentity Asset { get; }
        public byte Width { get; }
        public byte Height { get; }
        public byte Rotation { get; }

        internal InventoryItemFingerprint(ItemAssetIdentity asset, byte width, byte height, byte rotation)
        {
            Asset = asset;
            Width = width;
            Height = height;
            Rotation = (byte)(rotation & 3);
        }

        public bool Equals(InventoryItemFingerprint other)
        {
            return Asset == other.Asset && Width == other.Width && Height == other.Height && Rotation == other.Rotation;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryItemFingerprint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Asset.GetHashCode();
                hash = (hash * 397) ^ Width;
                hash = (hash * 397) ^ Height;
                return (hash * 397) ^ Rotation;
            }
        }

        public static bool operator ==(InventoryItemFingerprint left, InventoryItemFingerprint right) { return left.Equals(right); }
        public static bool operator !=(InventoryItemFingerprint left, InventoryItemFingerprint right) { return !left.Equals(right); }
    }

    internal readonly struct ProjectionBinding : IEquatable<ProjectionBinding>
    {
        public uint DragGeneration { get; }
        public ContainerReference Container { get; }
        public InventoryItemFingerprint Fingerprint { get; }

        internal ProjectionBinding(uint dragGeneration, ContainerReference container, InventoryItemFingerprint fingerprint)
        {
            if (dragGeneration == 0) throw new ArgumentOutOfRangeException(nameof(dragGeneration));
            if (container.SessionGeneration == 0) throw new ArgumentOutOfRangeException(nameof(container));
            DragGeneration = dragGeneration;
            Container = container;
            Fingerprint = fingerprint;
        }

        public bool Equals(ProjectionBinding other)
        {
            return DragGeneration == other.DragGeneration &&
                Container.Kind == other.Container.Kind &&
                Container.Page == other.Container.Page &&
                Container.SessionGeneration == other.Container.SessionGeneration &&
                Fingerprint == other.Fingerprint;
        }

        public override bool Equals(object obj) { return obj is ProjectionBinding other && Equals(other); }
        public override int GetHashCode() { unchecked { return ((int)DragGeneration * 397) ^ Container.GetHashCode() ^ Fingerprint.GetHashCode(); } }
        public static bool operator ==(ProjectionBinding left, ProjectionBinding right) { return left.Equals(right); }
        public static bool operator !=(ProjectionBinding left, ProjectionBinding right) { return !left.Equals(right); }
    }

    internal readonly struct NativeInventorySnapshot
    {
        public uint DragGeneration { get; }
        public ContainerReference Container { get; }
        public InventoryItemFingerprint Fingerprint { get; }
        public ItemGridPosition Position { get; }
        public uint NativeRevision { get; }

        internal NativeInventorySnapshot(uint dragGeneration, ContainerReference container,
            InventoryItemFingerprint fingerprint, ItemGridPosition position, uint nativeRevision)
        {
            DragGeneration = dragGeneration;
            Container = container;
            Fingerprint = fingerprint;
            Position = position;
            NativeRevision = nativeRevision;
        }

        internal bool MatchesGenerationAndContainer(ProjectionBinding binding)
        {
            return DragGeneration == binding.DragGeneration &&
                Container.Kind == binding.Container.Kind &&
                Container.Page == binding.Container.Page &&
                Container.SessionGeneration == binding.Container.SessionGeneration;
        }
    }

    internal interface INativeInventoryProjectionSource
    {
        bool TryCapture(NativeInventorySnapshot snapshot);
    }

    internal interface INativeInventoryProjectionConsumer
    {
        void Apply(NativeInventorySnapshot snapshot);
    }

    internal enum ProjectionPumpDecision : byte
    {
        Applied,
        DroppedStale,
        DroppedUnbound,
        ConsumerFailed
    }

    internal readonly struct ProjectionPumpReport
    {
        public int Applied { get; }
        public int Dropped { get; }
        public int ConsumerFailures { get; }

        internal ProjectionPumpReport(int applied, int dropped, int consumerFailures)
        {
            Applied = applied;
            Dropped = dropped;
            ConsumerFailures = consumerFailures;
        }
    }

    internal sealed class NativeInventoryProjectionRelay : INativeInventoryProjectionSource
    {
        private readonly object sync = new object();
        private readonly object pumpSync = new object();
        private readonly NativeInventorySnapshot[] queue;
        private int head;
        private int count;
        private bool hasBinding;
        private ProjectionBinding binding;
        private uint lastNativeRevision;
        private bool hasNativeRevision;

        internal NativeInventoryProjectionRelay(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            queue = new NativeInventorySnapshot[capacity];
        }

        internal int Capacity { get { return queue.Length; } }
        internal int Count { get { lock (sync) return count; } }

        internal void Bind(ProjectionBinding nextBinding)
        {
            lock (pumpSync)
            {
                lock (sync)
                {
                    BindStateLocked(nextBinding);
                }
            }
        }

        internal void Invalidate()
        {
            lock (pumpSync)
            {
                lock (sync)
                {
                    InvalidateStateLocked();
                }
            }
        }

        public bool TryCapture(NativeInventorySnapshot snapshot) { return TryEnqueue(snapshot); }

        internal bool TryEnqueue(NativeInventorySnapshot snapshot)
        {
            lock (sync)
            {
                if (count == queue.Length) return false;
                var tail = (head + count) % queue.Length;
                queue[tail] = snapshot;
                count++;
                return true;
            }
        }

        internal ProjectionPumpReport Pump(INativeInventoryProjectionConsumer consumer)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            lock (pumpSync)
            {
                var applied = 0;
                var dropped = 0;
                var failures = 0;
                NativeInventorySnapshot snapshot;
                while (TryDequeue(out snapshot))
                {
                    ProjectionBinding current;
                    bool bound;
                    bool staleRevision;
                    lock (sync)
                    {
                        bound = hasBinding;
                        current = binding;
                        staleRevision = hasNativeRevision && snapshot.NativeRevision < lastNativeRevision;
                        if (!staleRevision && bound && snapshot.MatchesGenerationAndContainer(current))
                        {
                            if (!hasNativeRevision || snapshot.NativeRevision > lastNativeRevision)
                            {
                                lastNativeRevision = snapshot.NativeRevision;
                                hasNativeRevision = true;
                            }
                        }
                    }

                    if (!bound || staleRevision || !snapshot.MatchesGenerationAndContainer(current))
                    {
                        dropped++;
                        continue;
                    }

                    try
                    {
                        consumer.Apply(snapshot);
                        applied++;
                    }
                    catch (Exception)
                    {
                        failures++;
                        lock (sync) InvalidateStateLocked();
                        break;
                    }
                }

                return new ProjectionPumpReport(applied, dropped, failures);
            }
        }

        private bool TryDequeue(out NativeInventorySnapshot snapshot)
        {
            lock (sync)
            {
                if (count == 0)
                {
                    snapshot = default(NativeInventorySnapshot);
                    return false;
                }

                snapshot = queue[head];
                queue[head] = default(NativeInventorySnapshot);
                head = (head + 1) % queue.Length;
                count--;
                return true;
            }
        }

        private void BindStateLocked(ProjectionBinding nextBinding)
        {
            binding = nextBinding;
            hasBinding = true;
            head = 0;
            count = 0;
            lastNativeRevision = 0;
            hasNativeRevision = false;
        }

        private void InvalidateStateLocked()
        {
            hasBinding = false;
            head = 0;
            count = 0;
            lastNativeRevision = 0;
            hasNativeRevision = false;
        }
    }

    internal enum AwaitingProjectionState : byte
    {
        Idle,
        AwaitingProjection
    }

    internal enum ProjectionConvergence : byte
    {
        Ignored,
        Converged,
        ObservedLatestFact
    }

    internal sealed class AwaitingProjectionController
    {
        private const uint VisualBudgetMilliseconds = 2000;
        private ProjectionBinding binding;
        private uint deadline;
        private bool awaiting;

        internal AwaitingProjectionState State { get { return awaiting ? AwaitingProjectionState.AwaitingProjection : AwaitingProjectionState.Idle; } }
        internal ProjectionBinding Binding { get { return binding; } }

        internal void Begin(ProjectionBinding nextBinding, uint nowMilliseconds)
        {
            binding = nextBinding;
            deadline = unchecked(nowMilliseconds + VisualBudgetMilliseconds);
            awaiting = true;
        }

        internal ProjectionConvergence Apply(NativeInventorySnapshot snapshot)
        {
            if (!awaiting || !snapshot.MatchesGenerationAndContainer(binding)) return ProjectionConvergence.Ignored;
            awaiting = false;
            return snapshot.Fingerprint == binding.Fingerprint
                ? ProjectionConvergence.Converged
                : ProjectionConvergence.ObservedLatestFact;
        }

        internal bool Tick(uint nowMilliseconds)
        {
            if (!awaiting || unchecked((int)(nowMilliseconds - deadline)) < 0) return false;
            awaiting = false;
            return true;
        }

        internal void Invalidate()
        {
            awaiting = false;
            binding = default(ProjectionBinding);
            deadline = 0;
        }
    }
}
