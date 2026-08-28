using System;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
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

    /// <summary>
    /// Small, testable bridge between a Unity Update message and the BUE
    /// runtime. The callback is cleared before teardown so a stale component
    /// can never call into a destroyed plugin instance.
    /// </summary>
    internal sealed class BueRuntimePump
    {
        private Action tick;

        internal BueRuntimePump(Action tick)
        {
            this.tick = tick ?? throw new ArgumentNullException(nameof(tick));
        }

        internal void Tick()
        {
            var callback = tick;
            if (callback != null) callback();
        }

        internal void Clear()
        {
            tick = null;
        }
    }

    /// <summary>
    /// Owns the logical pump instance and makes repeated initialization
    /// idempotent. The Unity behaviour is managed separately because Unity
    /// objects can be destroyed externally between two initialization calls.
    /// </summary>
    internal sealed class BueRuntimePumpSlot
    {
        private BueRuntimePump current;

        internal BueRuntimePump GetOrCreate(Action tick)
        {
            if (current != null) return current;
            current = new BueRuntimePump(tick);
            return current;
        }

        internal void Clear()
        {
            if (current != null) current.Clear();
            current = null;
        }
    }

    /// <summary>
    /// Independent Unity host for the BUE main-thread pump. BUE does not rely
    /// solely on BaseUnityPlugin.Start/Update because some supported hosts do
    /// not dispatch those messages consistently after plugin Awake.
    /// </summary>
    public sealed class BueRuntimePumpBehaviour : MonoBehaviour
    {
        private BueRuntimePump pump;

        internal static BueRuntimePumpBehaviour Attach(BueRuntimePump pump)
        {
            if (pump == null) throw new ArgumentNullException(nameof(pump));

            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("BUE.RuntimePump");
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(gameObject);

                var behaviour = gameObject.AddComponent<BueRuntimePumpBehaviour>();
                behaviour.pump = pump;
                behaviour.enabled = true;
                gameObject.SetActive(true);
                return behaviour;
            }
            catch
            {
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
                throw;
            }
        }

        public void Update()
        {
            var current = pump;
            if (current != null) current.Tick();
        }

        public void OnDestroy()
        {
            var current = pump;
            pump = null;
            if (current != null) current.Clear();
        }
    }
}
