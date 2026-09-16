using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-05 (V5-T5): the 快速转移恢复 wiring — exactly two registered patch
    /// points, no more (the acceptance surface pins this set):
    ///   - <see cref="FastTransferIntentPatch"/> opens/closes the one-click
    ///     intent window around vanilla's OWN Ctrl+right-click handler. The
    ///     trigger fact is vanilla's verdict (04's rule mirrored): the behavior
    ///     "did the packet go out" is observed, never second-guessed —
    ///   - <see cref="FastTransferDragSendPatch"/> marks that it did, so a
    ///     successful quick move (the common case) gets ZERO intervention
    ///     (成功转移不排). A send during a real mouse drag / BII submission /
    ///     the context-menu 存放到 button happens OUTSIDE any open window and is
    ///     structurally ignored — 「不挂拖放预览」 and 「地面摊归 04」 hold by
    ///     construction, and there is no third trigger point to wire.
    /// No sendDragItem REJECT-path patch (the authority's ReceiveDragItem race
    /// refusal after a client-found space stays vanilla-silent — the named
    /// seam gap), no tryAddItem hook (that is 04's surface), and no enabled/
    /// feature bool anywhere in the bodies (生命周期登记 = 唯一开关).
    ///
    /// BINDING SHAPE: same R2 lesson as 04 — the binder resolves each MethodBase
    /// explicitly (unique-name + declared-only + full parameter-shape match;
    /// unknown classes resolve to null, fail-closed). The LIT 04 note holds for
    /// this fork's 0Harmony: name-only attribute binding is host-unverifiable;
    /// here at least the targets are plain byte signatures — but the binder
    /// stays the single source of truth so the surface is RED-TESTABLE.
    /// </summary>
    internal static class FastTransferIntentPatch
    {
        // Bound to PlayerDashboardInventoryUI.onSelectedItem(byte,byte,byte)
        // by FastTransferRecoverBinder (the module owns install/uninstall;
        // PatchAll is never used).
        static void Prefix(byte page, byte x, byte y)
        {
            var intent = FastTransferRecoverEngine.TryReadIntent(page, x, y);
            if (intent != null) FastTransferIntentScope.Enter(intent);
        }

        static void Finalizer()
        {
            var pending = FastTransferIntentScope.ExitEvaluate();
            if (pending != null) FastTransferRecoverAdapter.RequestFromIntent(pending);
        }
    }

    /// <summary>The send-out witness inside an open window: vanilla already
    /// dispatched this transfer (reliable? no — Unreliable SendDragItem; that
    /// is vanilla's own loss behavior, out of scope per the ticket). The body
    /// touches no parameter of the original and mutates nothing.</summary>
    internal static class FastTransferDragSendPatch
    {
        static void Postfix()
        {
            FastTransferIntentScope.NoteDragSent();
        }
    }

    /// <summary>The binder: one resolver per patch class → the exact MethodBase
    /// + HarmonyMethod pair to install. Pure reflection over the game assembly
    /// (no cctor, no ECall): host red tests resolve the SAME targets the real
    /// game installs. Adding a third surface here is the only way any new
    /// trigger point can ever exist.</summary>
    internal static class FastTransferRecoverBinder
    {
        /// <summary>The complete trigger surface — intent opener, send witness.
        /// Single source (module installs exactly this list; red tests pin it).</summary>
        internal static readonly IReadOnlyList<Type> PatchSurface = new[]
        {
            typeof(FastTransferIntentPatch),
            typeof(FastTransferDragSendPatch),
        };

        /// <summary>Resolve the original method a patch class binds to
        /// (null = drift/unknown — the installer treats that as all-or-none
        /// failure, never a half surface).</summary>
        internal static MethodBase ResolveTarget(Type patchType)
        {
            if (patchType == typeof(FastTransferIntentPatch))
            {
                // onSelectedItem: private static (byte,byte,byte) — exactly one
                // method may carry the name (the ItemIcon delegate FIELD of the
                // same name lives on another type; fields never surface here).
                return FindDeclaredMethod(typeof(PlayerDashboardInventoryUI), "onSelectedItem",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    new[] { typeof(byte), typeof(byte), typeof(byte) });
            }
            if (patchType == typeof(FastTransferDragSendPatch))
            {
                return FindDeclaredMethod(typeof(PlayerInventory), "sendDragItem",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    new[] { typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte) });
            }
            return null; // unknown class = not on the surface (fail-closed)
        }

        private static MethodBase FindDeclaredMethod(Type owner, string name, BindingFlags flags, Type[] parameterTypes)
        {
            MethodBase found = null;
            foreach (var method in owner.GetMethods(flags | BindingFlags.DeclaredOnly))
            {
                if (method.Name != name) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length) continue;
                var shapeMatches = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i]) { shapeMatches = false; break; }
                }
                if (!shapeMatches) continue;
                if (found != null) return null; // ambiguous = refuse (never bind blindly)
                found = method;
            }
            return found;
        }

        /// <summary>Install one surface point (prefix+finalizer for the intent
        /// opener, postfix for the send witness — each class has what it has).</summary>
        internal static void Bind(Harmony harmony, Type patchType)
        {
            var original = ResolveTarget(patchType)
                ?? throw new InvalidOperationException("fast-transfer target not found: " + patchType.Name);
            var prefix = AccessTools.Method(patchType, "Prefix");
            var postfix = AccessTools.Method(patchType, "Postfix");
            var finalizer = AccessTools.Method(patchType, "Finalizer");
            // 6-arg overload: the 5-arg form is [Obsolete] in the bundled 0Harmony 2.9.
            harmony.Patch(original,
                prefix != null ? new HarmonyMethod(prefix) : null,
                postfix != null ? new HarmonyMethod(postfix) : null,
                null,
                finalizer != null ? new HarmonyMethod(finalizer) : null,
                null);
        }
    }
}
