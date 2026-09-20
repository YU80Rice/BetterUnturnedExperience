using System;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    /// <summary>
    /// Single guarded seam for every main-thread route that can reach the
    /// native management panel. Once a route fails, all later sources are
    /// rejected so an isolated UI cannot continue receiving stale callbacks.
    /// </summary>
    internal sealed class BueRuntimeTickDispatcher
    {
        private readonly Action<BueNativeManagementPanel.TickSource> dispatch;
        private readonly Action<Exception> onFailure;
        private readonly Func<int> frameProvider;
        private bool isolated;
        private bool dispatching;
        private int lastFrame = -1;
        private int dispatchedSources;

        internal BueRuntimeTickDispatcher(Action tick)
            : this(source => tick(), null, () => -1)
        {
            if (tick == null) throw new ArgumentNullException(nameof(tick));
        }

        internal BueRuntimeTickDispatcher(Action<BueNativeManagementPanel.TickSource> dispatch)
            : this(dispatch, null, () => -1)
        {
        }

        internal BueRuntimeTickDispatcher(Action<BueNativeManagementPanel.TickSource> dispatch, Action<Exception> onFailure)
            : this(dispatch, onFailure, () => -1)
        {
        }

        internal BueRuntimeTickDispatcher(Action<BueNativeManagementPanel.TickSource> dispatch, Func<int> frameProvider)
            : this(dispatch, null, frameProvider)
        {
        }

        internal BueRuntimeTickDispatcher(Action<BueNativeManagementPanel.TickSource> dispatch, Action<Exception> onFailure, Func<int> frameProvider)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.onFailure = onFailure;
            this.frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
        }

        internal bool Isolated { get { return isolated; } }

        internal bool Dispatch(BueNativeManagementPanel.TickSource source)
        {
            if (isolated || dispatching) return false;
            int frame;
            try
            {
                frame = frameProvider();
            }
            catch (Exception error)
            {
                IsolateAfterFailure(error);
                return false;
            }
            if (frame != lastFrame)
            {
                lastFrame = frame;
                dispatchedSources = 0;
            }
            var sourceMask = 1 << (int)source;
            if ((dispatchedSources & sourceMask) != 0) return false;
            dispatchedSources |= sourceMask;
            dispatching = true;
            try
            {
                dispatch(source);
                return true;
            }
            catch (Exception error)
            {
                IsolateAfterFailure(error);
                return false;
            }
            finally
            {
                dispatching = false;
            }
        }

        internal void Isolate()
        {
            isolated = true;
        }

        private void IsolateAfterFailure(Exception error)
        {
            isolated = true;
            if (onFailure != null)
            {
                try { onFailure(error); } catch (Exception) { }
            }
        }
    }
}
