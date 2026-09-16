namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V5-04 (V5-T5): the verified-context counter for insert recovery.
    /// tryAddItemAuto is the shared chokepoint of EVERY item-acquisition path
    /// (pickup RPC, craft RPC — but also NPC rewards, vendors, mannequins,
    /// clothing swaps, harvest, admin give…), and V5-T5 Q2 froze the ruling:
    /// 第一版只接线主机权威拾取/合成，其它获得路径必须另裁，不能因共用类型
    /// 自动享受. The scope is how the wiring stays honest: the two verified
    /// server-side RPC entry points (ItemManager.ReceiveTakeItemRequest and
    /// PlayerCrafting.ReceiveCraft) open it for the duration of the call
    /// (prefix/finalizer pair — the finalizer closes even on the exception
    /// path), and the recovery adapter refuses every failed add that arrives
    /// outside it. Pure managed counter: no Unity type, host-testable as-is.
    /// All server-side RPC pumping runs on the Unity main thread, so a plain
    /// int (not a lock) is the right depth shape; Exit never goes negative.
    /// </summary>
    internal static class InsertRecoverScope
    {
        private static int depth;

        /// <summary>Current nesting (0 = no verified acquisition context).</summary>
        internal static int Depth { get { return depth; } }

        /// <summary>True while a verified pickup/craft RPC is executing.</summary>
        internal static bool IsOpen { get { return depth > 0; } }

        /// <summary>Open one verified context (the RPC prefix).</summary>
        internal static void Enter()
        {
            depth++;
        }

        /// <summary>Close one verified context (the RPC finalizer). Never
        /// drives the counter negative: an unpaired close can only mask an
        /// already-closed window, never open a spurious one.</summary>
        internal static void Exit()
        {
            if (depth > 0) depth--;
        }

        /// <summary>Host-test seam: full counter reset between groups.</summary>
        internal static void ResetForTests()
        {
            depth = 0;
        }
    }
}
