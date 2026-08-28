using System;
using UnityEngine;

namespace BetterUnturnedExperience.Plugin
{
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
            this.tick = tick;
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
    internal sealed class BueRuntimePumpBehaviour : MonoBehaviour
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
                return behaviour;
            }
            catch
            {
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
                throw;
            }
        }

        private void Update()
        {
            var current = pump;
            if (current != null) current.Tick();
        }

        private void OnDestroy()
        {
            var current = pump;
            pump = null;
            if (current != null) current.Clear();
        }
    }
}
