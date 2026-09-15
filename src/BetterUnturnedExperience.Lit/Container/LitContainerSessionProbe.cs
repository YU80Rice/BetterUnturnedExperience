using System;
using System.Runtime.CompilerServices;
using SDG.Unturned;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-03: what the surface/protocol layers ask the world — the local
    /// container session view (client side) and the live facts + grid for one
    /// authoritative execution (server side). Pure data; the production reads
    /// live in LitContainerSessionProbe; host tests inject the whole view
    /// through the module's ContainerProbeOverride (the NetServiceFactory
    /// ForTests precedent). Live/ContainerItems stay null on the pure-client
    /// click path — that role never executes locally.
    /// </summary>
    internal sealed class LitContainerClientView
    {
        public LitContainerSessionObservation Observation;
        /// <summary>The fingerprint of what the client's storage mirror
        /// currently shows — the claimed content version.</summary>
        public ulong Fingerprint;
        /// <summary>Server-role only: the live facts read from the real
        /// opener inventory, paired with the mounted grid to execute on.</summary>
        public LitContainerLiveFacts? Live;
        public Items ContainerItems;
    }

    /// <summary>
    /// The engine-facing reads behind the container session. Every method is
    /// failure-guarded and [MethodImpl(NoInlining)] so the pure container
    /// domain never drags Unturned type initializers into a host-tested path
    /// (the Mono ECall rule the whole plugin follows).
    ///
    /// Kind identification (V5-T4 Q1 + R3 facts):
    ///  - the storage page mount is the vanilla session signal (isStoring);
    ///  - the networked trunk bit distinguishes 后备箱 from a world crate;
    ///  - a world crate the SERVER-side inventory still references is checked
    ///    against the documented virtual-storage hook (onStateRebuilt —
    ///    「Plugin implementation for virtual storage to hook state. Vanilla
    ///    code does not use this.」) and the display-cabinet flag; anything
    ///    hooked/display is projected Unsupported with a diagnostic (T4: 不因
    ///    没有识别字段写成永不支持，也不把没钩子的箱子误判成虚拟);
    ///  - on a pure remote client the crate reference does not exist (the
    ///    field is server-side only, R3 §4.1) — there the client projects from
    ///    the mount flags and the AUTHORITY does the virtual check on the real
    ///    object (button可见 ≠ 授权承诺; every refusal answers in words).
    /// </summary>
    internal static class LitContainerSessionProbe
    {
        /// <summary>The local (this-process) container view for the UI and the
        /// client-role click. All-failure default = "no session" (fail-closed,
        /// never a button on an unreadable surface).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static LitContainerClientView ObserveClient()
        {
            var view = new LitContainerClientView { Observation = new LitContainerSessionObservation() };
            try
            {
                var inv = Player.LocalPlayer?.inventory;
                if (inv == null) return view;
                var storageItems = inv.items != null && PlayerInventory.STORAGE < inv.items.Length ? inv.items[PlayerInventory.STORAGE] : null;
                var obs = new LitContainerSessionObservation
                {
                    TitleBarPresent = true, // refreshed by the patch pump right before evaluation
                    SessionActive = inv.isStoring,
                    GridNonEmpty = storageItems != null && storageItems.width > 0 && storageItems.height > 0,
                    // the page is only ever mounted for the opener/driver by
                    // the server's own open/close flow — client-visible grant:
                    PermissionGranted = inv.isStoring,
                };
                if (!inv.isStoring)
                {
                    view.Observation = obs; // Kind None — silent
                    return view;
                }
                if (inv.isStorageTrunk)
                {
                    obs.Kind = LitContainerSessionKind.VehicleTrunk;
                }
                else if (inv.storage != null)
                {
                    // SP / listen host: the crate object is reachable here —
                    // the honest virtual/display check runs where it can see.
                    obs.Kind = ClassifyCrate(inv.storage, out var diagnostic);
                    obs.UnsupportedDiagnostic = diagnostic;
                }
                else
                {
                    // Remote client: PlayerInventory.storage is server-side
                    // only (R3 §4.1) — the inventory itself cannot identify
                    // the crate. Use the vanilla client UI's own reference for
                    // exactly this decision (PlayerDashboardInventoryUI reads
                    // PlayerInteract.interactable as InteractableStorage for
                    // display-crate controls): when the focused object IS the
                    // crate, classify it client-side (isDisplay/lock/asset flags
                    // are asset-derived and present on the client; the virtual
                    // hook onStateRebuilt is server-side only, so a virtual
                    // crate without a locally observable reference stays the
                    // authority's call). No local reference => honest fallback:
                    // the mount fact still says a container session exists, the
                    // claim carries the unverified diagnostic, and the
                    // authority fail-closes (button可见 ≠ 授权承诺, T4 Q4).
                    var focusedCrate = TryGetFocusedCrate();
                    if (focusedCrate != null)
                    {
                        obs.Kind = ClassifyCrate(focusedCrate, out var crateDiagnostic);
                        obs.UnsupportedDiagnostic = crateDiagnostic;
                    }
                    else
                    {
                        obs.Kind = LitContainerSessionKind.WorldContainer;
                        obs.UnsupportedDiagnostic = "remote-crate-identity-unverified:authority-revalidates";
                    }
                }
                view.Observation = obs;
                if (storageItems != null) view.Fingerprint = LitContainerContentFingerprint.FromItems(storageItems);
                return view;
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[Tidy容器] 客户端容器会话观察失败（按无会话处理）: " + error.Message);
                view.Observation = new LitContainerSessionObservation();
                view.Fingerprint = 0UL;
                return view;
            }
        }

        /// <summary>Server-side live facts + mounted grid for one execution
        /// (used by the production authority for a remote peer's admitted
        /// request, and by the local server-role path with LocalPlayer).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool TryReadServerLive(Player player, out LitContainerLiveFacts live, out Items grid)
        {
            live = new LitContainerLiveFacts();
            grid = null;
            try
            {
                var inv = player?.inventory;
                if (inv?.items == null || PlayerInventory.STORAGE >= inv.items.Length) return false;
                grid = inv.items[PlayerInventory.STORAGE];
                live.SessionActive = inv.isStoring && grid != null && grid.width > 0 && grid.height > 0;
                if (!live.SessionActive)
                {
                    live.KindObserved = LitContainerSessionKind.None;
                    live.LiveFingerprint = 0UL;
                    return true;
                }
                if (inv.isStorageTrunk)
                {
                    live.KindObserved = LitContainerSessionKind.VehicleTrunk;
                    // 后备箱 adapter 自有维度：驾驶座授权仍在位（离座/换座由原版
                    // revokeTrunkAccess→closeTrunk 先拆挂载，呈现为会话失效）。
                    var vehicle = player.movement != null ? player.movement.getVehicle() : null;
                    live.TrunkDriverAuthorized = vehicle != null && vehicle.GetDriverPlayer() == player;
                }
                else if (inv.storage != null)
                {
                    live.KindObserved = ClassifyCrate(inv.storage, out var diagnostic);
                    if (live.KindObserved != LitContainerSessionKind.WorldContainer)
                    {
                        LitRuntime.LogWarning("[Tidy容器] 权威识别到不支持的世界对象（peer 请求被拒，诊断=" + diagnostic + "）");
                        live.WorldOpenerIsRequester = false;
                        live.WorldAccessAllowed = false;
                    }
                    else
                    {
                        // 世界箱 adapter 自有维度：单 opener + 原版锁/组
                        // （checkRot：SP 恒真、联机看 isLocked/owner/group——
                        // 原版访问控制保持，不另造 BUE 整理锁）。距离/关箱由
                        // 原版先拆挂载会话，已在 SessionActive 一步呈现。
                        live.WorldOpenerIsRequester = inv.storage.isOpen && inv.storage.opener == player;
                        live.WorldAccessAllowed = inv.storage.checkRot(
                            player.channel.owner.playerID.steamID, player.quests.groupID);
                    }
                }
                else
                {
                    live.KindObserved = LitContainerSessionKind.Unsupported;
                }
                live.LiveFingerprint = LitContainerContentFingerprint.FromItems(grid);
                return true;
            }
            catch (Exception error)
            {
                LitRuntime.LogWarning("[Tidy容器] 权威现读失败（零修改拒绝）: " + error.Message);
                return false;
            }
        }

        /// <summary>The crate the local player is currently focused on — the
        /// same reference the vanilla dashboard itself uses client-side for
        /// display-crate controls (PlayerDashboardInventoryUI). Null when the
        /// focus is gone (e.g. the player looked away after opening).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InteractableStorage TryGetFocusedCrate()
        {
            try { return PlayerInteract.interactable as InteractableStorage; }
            catch (Exception) { return null; }
        }

        /// <summary>The crate kind from the real object (R3 §4.4 signals):
        /// plugin state hook = virtual storage; display cabinet = its own
        /// surface (rot buttons + quickGrab) — both Unsupported this phase.</summary>
        private static LitContainerSessionKind ClassifyCrate(InteractableStorage storage, out string diagnostic)
        {
            diagnostic = null;
            if (storage == null) return LitContainerSessionKind.None;
            if (storage.onStateRebuilt != null)
            {
                diagnostic = "virtual-container:state-hooked";
                return LitContainerSessionKind.VirtualContainer;
            }
            if (storage.isDisplay)
            {
                diagnostic = "display-cabinet";
                return LitContainerSessionKind.Unsupported;
            }
            return LitContainerSessionKind.WorldContainer;
        }
    }
}
