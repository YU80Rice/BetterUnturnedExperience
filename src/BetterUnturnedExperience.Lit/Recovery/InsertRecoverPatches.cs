using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-04 (V5-T5): the 入包恢复 wiring — exactly three registered patch
    /// points, no more (the acceptance surface pins this set):
    ///   - the two VERIFIED server-side acquisition RPCs open and close the
    ///     recovery scope (finalizer: the window closes even when the handler
    ///     throws);
    ///   - the behavior point is a POSTFIX on PlayerInventory.tryAddItemAuto:
    ///     vanilla's own verdict is the trigger — __result==true passes through
    ///     untouched (成功放入不排; the success path never even enters the
    ///     adapter), and __result==false gets ONE recovery attempt through
    ///     InsertRecoverAdapter (仅原版第一次失败触发; a recovered commit
    ///     flips the verdict, so vanilla's callers run their success path —
    ///     pickup destroys the ground item, craft does not drop it).
    /// No sendDragItem / onSelectedItem wiring (快速转移 = DEV-V5-05), no
    /// Items-type page hook (the v1.4.0 surface also caught the AREA preview
    /// and STORAGE grids — this ticket's page rule is 只玩家五页 by
    /// construction in the planner), no BII drag-preview entanglement, and no
    /// enabled/feature bool anywhere in the bodies (生命周期登记 = 唯一开关).
    ///
    /// BINDING SHAPE (R2, replacing the R1 [HarmonyPatch] attributes — empirics
    /// against the bundled 0Harmony 2.9 + Assembly-CSharp, not memory):
    ///   - the RPC handlers' first parameter is `in ServerInvocationContext`,
    ///     which reflection exposes as ServerInvocationContext& — a plain
    ///     typeof(ServerInvocationContext) NEVER binds (probed), and a byref
    ///     type cannot be spelled in an attribute argument constant;
    ///   - name-only binding does NOT silently fan out to overloads in this
    ///     fork: it throws "Ambiguous match in Harmony patch" (probed) — and
    ///     ReceiveCraft has two overloads (the [Obsolete] ushort-id stub at
    ///     PlayerCrafting.cs:1326 forwards into the live GUID overload at :679).
    /// Therefore the binder resolves each MethodBase explicitly — the single
    /// binding source of truth, host-verifiable by red tests (the attribute
    /// shape was un-verifiable off-game, which is what let R1's ambiguity and
    /// the byref mismatch slip in). The LIR precedent stays honest: its
    /// name-only attribute binds a UNIQUE `in`-first name — ours differ.
    /// </summary>
    internal static class InsertRecoverPickupScopePatch
    {
        // Bound to ItemManager.ReceiveTakeItemRequest by InsertRecoverBinder
        // (the module owns install/uninstall; PatchAll is never used).
        static void Prefix()
        {
            InsertRecoverScope.Enter();
        }

        static void Finalizer()
        {
            InsertRecoverScope.Exit();
        }
    }

    /// <summary>The craft RPC (ONLY_FROM_OWNER): blueprint outputs arrive via
    /// forceAddItem → tryAddItemAuto inside this handler, so a fragmented-hole
    /// refusal gets the same one-shot recovery as a pickup (V5-T5 Q1.1).
    /// The binder targets the GUID overload (the blueprint UI's path); the
    /// [Obsolete] ushort stub forwards into it, so even a legacy external
    /// call enters the same window exactly once.</summary>
    internal static class InsertRecoverCraftScopePatch
    {
        static void Prefix()
        {
            InsertRecoverScope.Enter();
        }

        static void Finalizer()
        {
            InsertRecoverScope.Exit();
        }
    }

    /// <summary>The behavior point on the host-authoritative auto-add attempt.
    /// Body-free of any feature/config switch; the adapter's gate list is the
    /// decision, the postfix only reads the vanilla verdict.
    ///
    /// WHY tryAddItemAuto is the single correct behavior point for BOTH ticket
    /// paths (U3-SDK verbatim, PlayerInventory.cs):
    ///   - 自动拾取：ItemManager.ReceiveTakeItemRequest 的 to_page==255 分支调
    ///     tryAddItem(item, true)，而 tryAddItem(Item,bool)(464) →
    ///     tryAddItem(Item,bool,bool)(468) → tryAddItemAuto(item,auto,auto,auto,
    ///     playEffect)。委托终点即本行为位。指定坐标五参重载(405)不是碎洞自动
    ///     入包失败场景，属票面「其它获得路径不做」的具名排除。
    ///   - 合成：PlayerCrafting 产物调 forceAddItem(item,true)(597) →
    ///     forceAddItemAuto → tryAddItemAuto(609)；false 才 dropItem。合成失败
    ///     落点同样收敛于此。
    /// 故这一个 postfix 同时看见两条已验证主机路径的第一次入包失败，且只在
    /// __result==false 时进入恢复。</summary>
    internal static class InsertRecoverAutoAddPatch
    {
        /// <summary>internal for the host red tests (the observable trigger
        /// surface); the binder wires it regardless of accessibility.</summary>
        internal static void Postfix(PlayerInventory __instance, Item item, bool autoEquipUseable, ref bool __result)
        {
            if (__result) return; // 成功放入不排：原版成功 = 零介入
            if (InsertRecoverAdapter.TryRecover(__instance, item, autoEquipUseable).Recovered)
                __result = true; // 恢复提交成功 = 这次放入成功（物品已在五页内）
        }
    }

    /// <summary>The binder: one resolver per patch class → the exact
    /// MethodBase + HarmonyMethod pair to install. Pure reflection over the
    /// game assembly (no cctor, no ECall): host red tests resolve the SAME
    /// targets the real game installs, which is what closes R1's un-provable
    /// attribute binding. Adding a fourth surface here is the only way any
    /// new trigger point can ever exist.</summary>
    internal static class InsertRecoverBinder
    {
        /// <summary>The complete trigger surface — pickup opener, craft opener,
        /// behavior point, in install order. Single source (module installs
        /// exactly this list; red tests pin exactly this list).</summary>
        internal static readonly IReadOnlyList<Type> PatchSurface = new[]
        {
            typeof(InsertRecoverPickupScopePatch),
            typeof(InsertRecoverCraftScopePatch),
            typeof(InsertRecoverAutoAddPatch),
        };

        /// <summary>Resolve the original method a patch class binds to
        /// (null = signature drifted out from under us — the installer treats
        /// that as all-or-none failure, never a half surface).</summary>
        internal static MethodBase ResolveTarget(Type patchType)
        {
            if (patchType == typeof(InsertRecoverPickupScopePatch))
            {
                // ReceiveTakeItemRequest: unique name in the assembly (probe:
                // resolves by name even through the in-ServerInvocationContext
                // first parameter — the binder keeps the full shape pinned).
                var take = typeof(ItemManager).GetMethod("ReceiveTakeItemRequest",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (take == null) return null;
                var takeParams = take.GetParameters();
                if (takeParams.Length != 8 || !IsByRefContext(takeParams[0].ParameterType)
                        || takeParams[3].ParameterType != typeof(uint)) return null;
                return take;
            }
            if (patchType == typeof(InsertRecoverCraftScopePatch))
            {
                // The GUID overload specifically (PlayerCrafting.cs:679) — the
                // live blueprint-UI RPC (SendCraft binds the same shape);
                // the [Obsolete] ushort forwarding stub (:1326) must NOT be
                // the target (name-only would hit "Ambiguous match").
                foreach (var method in typeof(PlayerCrafting).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (method.Name != "ReceiveCraft") continue;
                    var parameters = method.GetParameters();
                    if (parameters.Length != 4 || !IsByRefContext(parameters[0].ParameterType)) continue;
                    if (parameters[1].ParameterType != typeof(Guid)) continue;
                    return method;
                }
                return null;
            }
            if (patchType == typeof(InsertRecoverAutoAddPatch))
            {
                return typeof(PlayerInventory).GetMethod("tryAddItemAuto",
                    new[] { typeof(Item), typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
            }
            return null; // unknown class = not on the surface (fail-closed)
        }

        private static bool IsByRefContext(Type parameterType)
        {
            return parameterType.IsByRef &&
                   parameterType.GetElementType() == typeof(ServerInvocationContext);
        }

        /// <summary>Install one surface point onto the harmony instance
        /// (prefix+finalizer for the scope openers, postfix for the behavior
        /// point — each class simply has the methods it has).</summary>
        internal static void Bind(Harmony harmony, Type patchType)
        {
            var original = ResolveTarget(patchType)
                ?? throw new InvalidOperationException("insert-recover target not found: " + patchType.Name);
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
