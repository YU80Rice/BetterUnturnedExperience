using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    // DEV-V2-13: anchor registry for shifting the vanilla PlayerPauseUI
    // column down one slot so the BUE entry can join it below Return. Each
    // element's Y is captured the first time it is seen and every Apply sets
    // Y = captured + pitch, so repeated ticks and external touches never
    // accumulate drift. New element instances (UI rebuild) anchor fresh from
    // their own current Y; Restore hands the vanilla layout back for a clean
    // BUE hand-back. Deliberately free of Glazier types: the caller supplies
    // the element as an opaque key plus a Y read and a Y write, which keeps
    // the whole contract testable headless.
    internal sealed class BuePauseColumnShift
    {
        // Anchoring is per element instance: two distinct UI-build instances
        // must never share an anchor even if their Equals overrides collide.
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return RuntimeHelpers.GetHashCode(obj); }
        }

        private readonly float shiftPitch;
        private readonly Dictionary<object, float> anchoredY = new Dictionary<object, float>(new ReferenceComparer());

        internal BuePauseColumnShift(float shiftPitch)
        {
            this.shiftPitch = shiftPitch;
        }

        internal int AnchoredCount { get { return anchoredY.Count; } }

        // Returns true when this call captured a fresh anchor (first sight of
        // the element, e.g. right after a UI rebuild) so the caller can log a
        // one-shot diagnostic instead of spamming per tick.
        internal bool Apply(object element, float currentY, Action<float> setY)
        {
            float anchor;
            var captured = !anchoredY.TryGetValue(element, out anchor);
            if (captured)
            {
                anchor = currentY;
                anchoredY[element] = anchor;
            }
            setY(anchor + shiftPitch);
            return captured;
        }

        // The anchor is removed only after the setter succeeded, so a failed
        // restore keeps its anchor and can be retried (DEV-V2-13 R1-F3).
        internal bool Restore(object element, Action<float> setY)
        {
            float anchor;
            if (!anchoredY.TryGetValue(element, out anchor)) return false;
            setY(anchor);
            anchoredY.Remove(element);
            return true;
        }

        // Point-in-time copy of the anchored instances for restoration walks.
        // The panel walks this snapshot instead of the vanilla static fields,
        // which a UI rebuild may already have repointed to fresh instances.
        internal List<KeyValuePair<object, float>> SnapshotAnchors()
        {
            return new List<KeyValuePair<object, float>>(anchoredY);
        }

        // Drops an anchor without writing (dead instance after a UI rebuild -
        // its layout died with the element, nothing to restore).
        internal bool RemoveAnchor(object element)
        {
            return anchoredY.Remove(element);
        }
    }
}
