using System;
using System.Collections.Generic;
using System.Threading;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Core.Dispatch
{
    /// <summary>
    /// DEV-V3-04: the platform main-thread dispatcher — the host-owned
    /// composition behind the IFeatureMainThread contract seam (public like
    /// its siblings FeatureEventBus / FeatureLifecycleRuntime: public is not
    /// SDK contract, V3-T1; the surface modules see is IFeatureMainThread).
    ///
    /// One queue, pumped by the host's sanctioned main-thread beat (the
    /// plugin Update chain); features never build their own pumps. Frozen
    /// semantics this class implements (the minimal behavior surface the
    /// spec pins — the ticket fixed the queue numbers):
    ///   - tasks are bound to the submitting module's
    ///     (FeatureId, LifecycleGeneration); OpenGeneration registers the
    //      live generation and SUPERSEDES the previous one — pending work
    //      of the dead generation is withdrawn un-executed (the re-enable
    //      boundary: 新代际旧代际全失效);
    ///   - InvalidateOwner (module stop / isolation) withdraws that
    ///     feature's pending tasks and refuses new posts until a fresh
    ///     generation opens; ShutdownHost (plugin stopping) refuses every
    ///     post and drains the queue;
    ///   - capacity 256 pending tasks and at most 32 executions per pump
    ///     beat (the frame budget; the overflow post is an explicit
    ///     CapacityExceeded with a diagnostic — never a silent drop);
    ///   - a single task's fault is isolated into a diagnostic and never
    ///     breaks the pump or the sibling tasks;
    ///   - Pump only runs on the composing (main) thread — a foreign pump
    ///     is rejected with a diagnostic (the main-thread semantic is
    ///     constructed, not hoped for; the LIT/LIR precedent's thread
    ///     assertion, platform-shaped).
    /// Every diagnostic runs OUTSIDE the queue lock (the frozen
    /// no-callback-under-lock discipline); Post never blocks on a task.
    /// </summary>
    public sealed class MainThreadDispatcherRuntime
    {
        /// <summary>The queue bound fixed by DEV-V3-04 (observable, testable).</summary>
        internal const int MaxPendingTasks = 256;
        /// <summary>The per-beat execution budget fixed by DEV-V3-04.</summary>
        internal const int MaxTasksPerPump = 32;

        private sealed class PendingTask
        {
            internal FeatureId Owner;
            internal ulong Generation;
            internal Action Task;
        }

        private sealed class OwnerState
        {
            // HasGeneration=false = no live generation (never opened, or
            // invalidated); Generation then carries no meaning.
            internal ulong Generation;
            internal bool HasGeneration;
        }

        private readonly object sync = new object();
        private readonly Queue<PendingTask> queue = new Queue<PendingTask>();
        private readonly Dictionary<string, OwnerState> owners =
            new Dictionary<string, OwnerState>(StringComparer.Ordinal);
        private readonly Action<string> diagnosticSink;
        private readonly int mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private bool hostStopped;

        public MainThreadDispatcherRuntime(Action<string> diagnosticSink = null)
        {
            this.diagnosticSink = diagnosticSink;
        }

        /// <summary>Pending tasks currently in the queue (observability seam).</summary>
        public int PendingCount
        {
            get { lock (sync) return queue.Count; }
        }

        /// <summary>
        /// Registers the feature's live lifecycle generation. The host start
        /// path calls this when it composes a module's bootstrap (start and
        /// panel re-enable); a superseded generation's pending work is
        /// withdrawn un-executed with a structured diagnostic.
        /// </summary>
        public void OpenGeneration(FeatureId owner, ulong generation)
        {
            int withdrawn = 0;
            lock (sync)
            {
                // Re-arm boundary (the LIT MainThreadDispatcher.EnsureOpen
                // precedent): opening a NEW generation at the host start path
                // reopens the seam after a previous generation's shutdown —
                // the queue belongs to the generation, not the process
                // (same-process reload / panel re-enable must face a live
                // seam). Only the host start path calls this; a stop boundary
                // never does, so a stopped host cannot be resurrected from
                // the module side (there is no mutator on IFeatureMainThread).
                hostStopped = false;
                var state = ObtainOwnerLocked(owner.Value);
                state.HasGeneration = true;
                state.Generation = generation;
                withdrawn = WithdrawPendingLocked(task =>
                    string.Equals(task.Owner.Value, owner.Value, StringComparison.Ordinal) && task.Generation != generation);
            }
            Emit("event=main-thread result=generation-opened feature=" + owner.Value + " generation=" + generation
                + (withdrawn > 0 ? " stale-withdrawn=" + withdrawn : "") + " diagnosticId=BUE-MT-GEN");
        }

        /// <summary>
        /// The stop/isolation boundary: the feature's pending tasks are
        /// withdrawn un-executed and posts are refused until a fresh
        /// generation opens (the re-enable path).
        /// </summary>
        public void InvalidateOwner(FeatureId owner, string reason)
        {
            int withdrawn = 0;
            lock (sync)
            {
                if (owners.TryGetValue(owner.Value, out var state))
                {
                    state.HasGeneration = false;
                    state.Generation = 0UL;
                }
                withdrawn = WithdrawPendingLocked(task =>
                    string.Equals(task.Owner.Value, owner.Value, StringComparison.Ordinal));
            }
            Emit("event=main-thread result=owner-invalidated feature=" + owner.Value + " reason=" + reason
                + (withdrawn > 0 ? " withdrawn=" + withdrawn : "") + " diagnosticId=BUE-MT-002");
        }

        /// <summary>The host stop boundary: every post is refused and the
        /// queue is drained un-executed (plugin stopping).</summary>
        public void ShutdownHost(string reason)
        {
            lock (sync)
            {
                hostStopped = true;
                queue.Clear();
            }
            Emit("event=main-thread result=host-shutdown reason=" + reason + " diagnosticId=BUE-MT-002");
        }

        /// <summary>
        /// The contract view handed to one module for one lifecycle
        /// generation (bootstrap.MainThread). The view answers for its own
        /// (owner, generation) binding; after the generation dies it keeps
        /// answering — explicitly, with GenerationInvalid.
        /// </summary>
        public IFeatureMainThread CreateView(FeatureId owner, ulong generation)
        {
            return new FeatureMainThreadView(this, owner, generation);
        }

        /// <summary>The posting pipeline: null fail-fast → host/owner/generation
        /// gates → capacity. Rejections are explicit results with a structured
        /// diagnostic; the accepted task rides the queue until a pump beat.</summary>
        internal MainThreadPostResult Post(FeatureId owner, ulong generation, Action task)
        {
            if (task == null)
            {
                Emit("event=main-thread result=invalid-task reason=null-task feature=" + owner.Value
                    + " generation=" + generation + " diagnosticId=BUE-MT-004");
                throw new ArgumentException("the main-thread task must never be null (developer error, the null-handler discipline)", nameof(task));
            }
            string deferredDiagnostic = null;
            MainThreadPostResult result;
            lock (sync)
            {
                if (hostStopped)
                {
                    deferredDiagnostic = "event=main-thread result=post-rejected feature=" + owner.Value + " generation=" + generation
                        + " reason=host-stopped diagnosticId=BUE-MT-002";
                    result = new MainThreadPostResult(false, MainThreadPostReason.GenerationInvalid, "BUE-MT-002");
                }
                else if (!IsPostAcceptedLocked(owner.Value, generation, out var rejectReason))
                {
                    deferredDiagnostic = "event=main-thread result=post-rejected feature=" + owner.Value + " generation=" + generation
                        + " reason=" + rejectReason + " diagnosticId=BUE-MT-002";
                    result = new MainThreadPostResult(false, MainThreadPostReason.GenerationInvalid, "BUE-MT-002");
                }
                else if (queue.Count >= MaxPendingTasks)
                {
                    deferredDiagnostic = "event=main-thread result=post-rejected feature=" + owner.Value + " generation=" + generation
                        + " reason=capacity-exceeded pending=" + queue.Count + " capacity=" + MaxPendingTasks + " diagnosticId=BUE-MT-001";
                    result = new MainThreadPostResult(false, MainThreadPostReason.CapacityExceeded, "BUE-MT-001");
                }
                else
                {
                    queue.Enqueue(new PendingTask { Owner = owner, Generation = generation, Task = task });
                    result = new MainThreadPostResult(true, MainThreadPostReason.None, "BUE-MT-ACCEPT");
                }
            }
            // The sink never runs under the queue lock (the frozen discipline).
            Emit(deferredDiagnostic);
            if (result.Posted)
            {
                Emit("event=main-thread result=posted feature=" + owner.Value + " generation=" + generation + " diagnosticId=BUE-MT-ACCEPT");
            }
            return result;
        }

        private bool IsPostAcceptedLocked(string ownerValue, ulong generation, out string rejectReason)
        {
            rejectReason = null;
            if (!owners.TryGetValue(ownerValue, out var state) || !state.HasGeneration)
            {
                rejectReason = "feature-invalidated";
                return false;
            }
            if (state.Generation != generation)
            {
                rejectReason = "generation-superseded";
                return false;
            }
            return true;
        }

        private OwnerState ObtainOwnerLocked(string ownerValue)
        {
            if (!owners.TryGetValue(ownerValue, out var state))
            {
                state = new OwnerState();
                owners[ownerValue] = state;
            }
            return state;
        }

        private int WithdrawPendingLocked(Func<PendingTask, bool> match)
        {
            // Snapshot-and-refilter keeping FIFO order; the withdrawn tasks
            // never execute (generation-boundary rule).
            var keep = new List<PendingTask>(queue.Count);
            var withdrawn = 0;
            foreach (var pending in queue)
            {
                if (match(pending)) withdrawn++;
                else keep.Add(pending);
            }
            if (withdrawn > 0)
            {
                queue.Clear();
                foreach (var kept in keep) queue.Enqueue(kept);
            }
            return withdrawn;
        }

        /// <summary>
        /// The host main-thread beat: execute up to MaxTasksPerPump pending
        /// tasks FIFO. A foreign-thread pump is rejected with a diagnostic
        /// (the main-thread semantic is constructed, never hoped for); one
        /// task's fault is isolated into a diagnostic and the beat continues.
        /// </summary>
        public void Pump()
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                Emit("event=main-thread result=pump-rejected reason=non-main-thread currentThread="
                    + Thread.CurrentThread.ManagedThreadId + " mainThread=" + mainThreadId + " diagnosticId=BUE-MT-005");
                return;
            }
            var batch = new List<PendingTask>(MaxTasksPerPump);
            lock (sync)
            {
                while (batch.Count < MaxTasksPerPump && queue.Count > 0)
                {
                    batch.Add(queue.Dequeue());
                }
            }
            for (var i = 0; i < batch.Count; i++)
            {
                var pending = batch[i];
                try
                {
                    pending.Task();
                }
                catch (Exception error)
                {
                    Emit("event=main-thread result=task-error feature=" + pending.Owner.Value
                        + " generation=" + pending.Generation + " errorType=" + error.GetType().Name
                        + " message=" + error.Message + " diagnosticId=BUE-MT-003");
                }
            }
        }

        private void Emit(string line)
        {
            var sink = diagnosticSink;
            if (sink == null || line == null) return;
            try { sink(line); }
            catch (Exception) { }
        }

        private sealed class FeatureMainThreadView : IFeatureMainThread
        {
            private readonly MainThreadDispatcherRuntime owner;
            private readonly FeatureId feature;
            private readonly ulong generation;

            internal FeatureMainThreadView(MainThreadDispatcherRuntime owner, FeatureId feature, ulong generation)
            {
                this.owner = owner;
                this.feature = feature;
                this.generation = generation;
            }

            public MainThreadPostResult Post(Action task) { return owner.Post(feature, generation, task); }
        }
    }
}
