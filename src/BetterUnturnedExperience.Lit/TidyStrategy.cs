using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 背包自动整理算法的输入/输出物品表示。
    /// 该类刻意不引用任何 Unity / Unturned 类型，保持算法层纯净，
    /// 便于独立单元测试与跨项目复用。
    /// </summary>
    internal class PackableItem
    {
        /// <summary>
        /// 算法不解释的附加数据；调用方通常在此存放原始 ItemJar 引用，
        /// 算法输出后由调用方读取 Tag 并把坐标写回游戏对象。
        /// </summary>
        public object Tag;

        /// <summary>物品原始（未旋转）宽度（占用的列数）。</summary>
        public byte size_x;

        /// <summary>物品原始（未旋转）高度（占用的行数）。</summary>
        public byte size_y;

        /// <summary>算法输出：在网格中的左上角列坐标。</summary>
        public byte ResultX;

        /// <summary>算法输出：在网格中的左上角行坐标。</summary>
        public byte ResultY;

        /// <summary>算法输出：0 = 不旋转，1 = 旋转 90度（与 Unturned 的 rot 字段语义一致）。</summary>
        public byte ResultRot;

        /// <summary>
        /// 算法输出：true = 已成功放置（ResultX/Y/Rot 有效）；
        /// false = 未放置（尺寸异常或网格装不下），调用方应保留原位。
        /// </summary>
        public bool Placed;

        // ─────────────────────────────────────────────────────────────
        // v2.0.0 扩展：同类聚合 + 稳定排序 + 快捷键迁移支持
        // ─────────────────────────────────────────────────────────────

        /// <summary>同组键，通常 = jar.item.id。同 GroupKey 物品在统一排版内保持连续。</summary>
        public ushort GroupKey;

        /// <summary>捕获顺序索引（调用方在构建 PackableItem 时填入），用于稳定 tie-break。</summary>
        public int StableOrder;

        /// <summary>物品原始 X 坐标（整理前），用于计算移动距离与快捷键迁移。</summary>
        public byte OriginalX;

        /// <summary>物品原始 Y 坐标（整理前），用于计算移动距离与快捷键迁移。</summary>
        public byte OriginalY;

        /// <summary>物品原始旋转（整理前），用于计算旋转变化。</summary>
        public byte OriginalRot;

        /// <summary>偏好旋转（默认 = OriginalRot）。规划时默认正向 = 保留偏好旋转。</summary>
        public byte PreferredRotation;

        // ─────────────────────────────────────────────────────────────
        // DEV-V5-02 扩展：玩家用途标签（统一分段行带排版的分段键）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DEV-V5-02 (V5-T3): the frozen player-use label the unified layout
        /// segments by — filled by the caller from the internal classifier
        /// seam, never from a player surface or the contract. The solver treats
        /// it as an opaque segment key; items without one default to 其他, so
        /// classification failure can never drop an item.
        /// </summary>
        public PlayerUseLabel Label = PlayerUseLabel.Other;
    }

    /// <summary>
    /// 整理算法的历史模式值。DEV-V5-02 (V5-T3 Q2): 该枚举与其三条值只剩存盘
    /// 兼容与线协议占位语义——统一排版是唯一官方整理方法，任何档位都不再决定
    /// 算法；新计划不读它。迁移读取与私有线协议继续复用此类型。
    /// </summary>
    internal enum TidyMode : byte
    {
        /// <summary>历史默认值（原「同类」），现为线协议/存档的占位常量。</summary>
        SameType = 0,

        /// <summary>原「空间」档——退役，不再决定算法。</summary>
        MaxRects = 1,

        /// <summary>原「大件」档——退役，不再决定算法。</summary>
        FFD = 2,
    }

    /// <summary>
    /// DEV-V2-15 (T4 决策 1): the tidy strategy seam — the plan is produced
    /// through this interface so the transaction service never calls a solver
    /// directly. DEV-V5-02: the ONE built-in adapter is
    /// <see cref="TaggedRowBandV1Strategy"/> (unified tagged row-band layout);
    /// the three legacy modes and the old solver are retired with V5-T3, and
    /// no strategy picker UI and no third-party dynamic loading exist in this
    /// phase (spec: 不做).
    /// </summary>
    internal interface ITidyStrategy
    {
        /// <summary>Stable identity of the strategy that produced a plan.</summary>
        string StrategyId { get; }

        /// <summary>
        /// Produces the placement plan for one page. Input and output are
        /// pure C# types (DEV-V5-02: the unified layout module keeps this
        /// seam zero Unity dependencies). The plan consumer treats the
        /// returned placements as its own; the producing strategy must not
        /// retain the list.
        /// </summary>
        TidyPlan BuildPlan(TidyInput input);
    }

    /// <summary>
    /// Pure input record for one tidy plan: the page geometry, the stable
    /// direction (DEV-V4-06: a global saved ClientPreference; DEV-V5-02: the
    /// only remaining meaning is the finish order between otherwise-identical
    /// items) and the legacy packing mode (DEV-V5-02: read for protocol/save
    /// compatibility only — it decides NOTHING in the unified layout), plus
    /// the items to place. <see cref="Items"/> is the concrete list the layout
    /// module consumes read-only: TryPlanLayout never writes the input items;
    /// the plan is a fresh clone list (Tag preserved, one entry per input).
    /// </summary>
    internal sealed class TidyInput
    {
        public TidyInput(byte width, byte height, bool sortDescending, TidyMode mode, List<PackableItem> items)
        {
            Width = width;
            Height = height;
            SortDescending = sortDescending;
            Mode = mode;
            Items = items;
        }

        public byte Width { get; }
        public byte Height { get; }
        public bool SortDescending { get; }
        public TidyMode Mode { get; }
        public List<PackableItem> Items { get; }
    }

    /// <summary>
    /// Pure plan output: one placement entry per input item (same order,
    /// same Tag), the producing strategy's id, and whether every valid item
    /// found a spot (the old TryPack bool, carried under a name that says
    /// what it means).
    /// </summary>
    internal sealed class TidyPlan
    {
        public TidyPlan(string strategyId, IReadOnlyList<PackableItem> placements, bool allPlaced)
        {
            StrategyId = strategyId ?? throw new ArgumentNullException(nameof(strategyId));
            Placements = placements ?? throw new ArgumentNullException(nameof(placements));
            AllPlaced = allPlaced;
        }

        public string StrategyId { get; }
        public IReadOnlyList<PackableItem> Placements { get; }
        public bool AllPlaced { get; }
    }
}
