using System;
using System.Collections.Generic;
using System.Text;

namespace BetterUnturnedExperience.Bii
{
    internal enum ContainerSessionKind : byte
    {
        None,
        PlayerInventory,
        Storage,
        Trunk
    }

    /// <summary>
    /// Container session lifecycle state machine (pure C#, host-testable).
    /// Every engine open/swap event advances a monotonic generation; close and
    /// connection loss invalidate the session so stale projections routed with
    /// an old ContainerReference.SessionGeneration are rejected.
    /// </summary>
    internal sealed class ContainerSessionTracker
    {
        private ContainerSessionKind kind;
        private uint generation;
        private bool active;

        internal bool HasActiveSession { get { return active; } }
        internal ContainerSessionKind Kind { get { return kind; } }
        internal uint SessionGeneration { get { return generation; } }

        internal bool TryGetActiveGeneration(out uint generation)
        {
            generation = this.generation;
            return active;
        }

        internal void OnPlayerInventoryOpened()
        {
            Open(ContainerSessionKind.PlayerInventory);
        }

        internal void OnStorageOpened(bool isTrunk)
        {
            Open(isTrunk ? ContainerSessionKind.Trunk : ContainerSessionKind.Storage);
        }

        private void Open(ContainerSessionKind newKind)
        {
            generation++;
            kind = newKind;
            active = true;
        }

        internal void OnContainerClosed()
        {
            active = false;
            kind = ContainerSessionKind.None;
        }

        internal void OnStorageSwapped()
        {
            if (!active) return;
            generation++;
        }

        internal void OnConnectionLost()
        {
            active = false;
            kind = ContainerSessionKind.None;
        }
    }

    /// <summary>
    /// Point-in-time snapshot of the observable native inventory state, built
    /// from public fields only. The watcher diffs snapshots to raise lifecycle
    /// events. Pure data, host-testable.
    /// </summary>
    internal readonly struct InventoryLifecycleSnapshot : IEquatable<InventoryLifecycleSnapshot>
    {
        public bool DashboardActive { get; }
        public bool IsStoring { get; }
        public bool IsStorageTrunk { get; }
        public int StorageIdentity { get; }
        public bool Connected { get; }

        internal InventoryLifecycleSnapshot(bool dashboardActive, bool isStoring, bool isStorageTrunk, int storageIdentity, bool connected)
        {
            DashboardActive = dashboardActive;
            IsStoring = isStoring;
            IsStorageTrunk = isStorageTrunk;
            StorageIdentity = storageIdentity;
            Connected = connected;
        }

        public bool Equals(InventoryLifecycleSnapshot other)
        {
            return DashboardActive == other.DashboardActive && IsStoring == other.IsStoring &&
                IsStorageTrunk == other.IsStorageTrunk && StorageIdentity == other.StorageIdentity && Connected == other.Connected;
        }
    }

    /// <summary>
    /// Diffs consecutive snapshots and drives the container session tracker.
    /// Pure C#: the engine adapter reads public fields only and never touches
    /// private inventory UI objects.
    /// </summary>
    internal sealed class InventoryLifecycleWatcher
    {
        private readonly ContainerSessionTracker tracker;
        private bool hasLast;
        private InventoryLifecycleSnapshot last;

        internal InventoryLifecycleWatcher(ContainerSessionTracker tracker)
        {
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        internal void Feed(InventoryLifecycleSnapshot snapshot)
        {
            if (hasLast && snapshot.Equals(last)) return;
            var previous = last;
            var hadLast = hasLast;
            var previousConnected = hadLast && previous.Connected;
            last = snapshot;
            hasLast = true;

            if (!snapshot.Connected)
            {
                if (previousConnected) tracker.OnConnectionLost();
                return;
            }

            if (!previousConnected) tracker.OnConnectionLost();

            // Desired session kind from the snapshot; every kind transition
            // closes the old session BEFORE opening the new one so a single
            // generation advance marks the switch.
            if (snapshot.IsStoring)
            {
                var desired = snapshot.IsStorageTrunk ? ContainerSessionKind.Trunk : ContainerSessionKind.Storage;
                var identityChanged = hadLast && previous.IsStoring && previous.StorageIdentity != snapshot.StorageIdentity;
                if (tracker.Kind != desired || !tracker.HasActiveSession)
                {
                    if (tracker.HasActiveSession) tracker.OnContainerClosed();
                    tracker.OnStorageOpened(snapshot.IsStorageTrunk);
                }
                else if (identityChanged)
                {
                    tracker.OnStorageSwapped();
                }
                return;
            }

            if (snapshot.DashboardActive)
            {
                if (tracker.Kind != ContainerSessionKind.PlayerInventory || !tracker.HasActiveSession)
                {
                    if (tracker.HasActiveSession) tracker.OnContainerClosed();
                    tracker.OnPlayerInventoryOpened();
                }
                return;
            }

            if (tracker.HasActiveSession) tracker.OnContainerClosed();
        }
    }

    /// <summary>
    /// Result of the native member detection gate: enable decision plus the
    /// structured diagnostics naming every missing engine member.
    /// </summary>
    internal sealed class InventoryLifecycleGateResult
    {
        public bool Enabled { get; }
        public string Diagnostics { get; }

        internal InventoryLifecycleGateResult(bool enabled, string diagnostics)
        {
            Enabled = enabled;
            Diagnostics = diagnostics ?? string.Empty;
        }
    }

    /// <summary>
    /// Detection snapshot for every public native member the polling watcher
    /// reads. Pure data so the gate stays host-testable.
    /// </summary>
    internal sealed class InventoryLifecycleProbe
    {
        internal bool DashboardActiveField { get; set; }
        internal bool IsStoringField { get; set; }
        internal bool IsStorageTrunkField { get; set; }
        internal bool StorageField { get; set; }
        internal bool ConnectedProperty { get; set; }
        internal bool LocalPlayerProperty { get; set; }

        internal static InventoryLifecycleProbe AllPresent()
        {
            var probe = new InventoryLifecycleProbe();
            probe.DashboardActiveField = true;
            probe.IsStoringField = true;
            probe.IsStorageTrunkField = true;
            probe.StorageField = true;
            probe.ConnectedProperty = true;
            probe.LocalPlayerProperty = true;
            return probe;
        }

        internal static InventoryLifecycleProbe MissingIsStoring()
        {
            var probe = AllPresent();
            probe.IsStoringField = false;
            return probe;
        }
    }

    /// <summary>
    /// Fail-closed gate: the polling watcher is installed only when the client
    /// branch is active AND every polled native member exists. Any missing
    /// member produces a structured diagnostic naming it and keeps Unturned's
    /// native drag alive.
    /// </summary>
    internal static class InventoryLifecycleGate
    {
        internal static InventoryLifecycleGateResult Evaluate(bool isClientBranch, InventoryLifecycleProbe probe)
        {
            if (probe == null) throw new ArgumentNullException(nameof(probe));
            if (!isClientBranch)
            {
                return new InventoryLifecycleGateResult(false,
                    "inventory-lifecycle-gate: headless branch, client inventory hooks disabled before any native UI access");
            }

            var missing = new List<string>();
            if (!probe.DashboardActiveField) missing.Add("PlayerDashboardInventoryUI.active");
            if (!probe.IsStoringField) missing.Add("PlayerInventory.isStoring");
            if (!probe.IsStorageTrunkField) missing.Add("PlayerInventory.isStorageTrunk");
            if (!probe.StorageField) missing.Add("PlayerInventory.storage");
            if (!probe.ConnectedProperty) missing.Add("Provider.isConnected");
            if (!probe.LocalPlayerProperty) missing.Add("Player.LocalPlayer");

            if (missing.Count > 0)
            {
                var diagnostics = new StringBuilder("inventory-lifecycle-gate: native members missing, BUE wiring disabled, native drag preserved -> ");
                for (var index = 0; index < missing.Count; index++)
                {
                    if (index > 0) diagnostics.Append(", ");
                    diagnostics.Append(missing[index]);
                }
                return new InventoryLifecycleGateResult(false, diagnostics.ToString());
            }

            return new InventoryLifecycleGateResult(true, string.Empty);
        }
    }
}
