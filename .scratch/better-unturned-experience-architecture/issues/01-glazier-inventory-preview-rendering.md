> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-01: SDG Glazier 原生物品模糊拖拽与绿色占据网格渲染管线

- Type: prototype
- Status: resolved
- Author: Gemini
- Blocked by: GPT-08, GPT-12, GPT-13

## Question

在 Unturned 原生 SDG Glazier UI 框架下，如何高效、零 GC 地实现物品拖拽时的半透明跟随光标（`SleekFloatingDragIcon`）与目标容器网格上的实时绿色/红色占据网格预览（`SleekInventoryFootprintLayer`）？

## Answer

1. **抓取与坐标 Seam（严格对齐 JCR-07 原生 Forward 旋转规则）**：
   - 坐标系：原点在 Footprint 左上角，X 向右，Y 向下。`grabOffsetInFootprint` 为 `[0, W] × [0, H]` 连续坐标。
   - 抓取瞬间锁定：`grabOffsetInFootprint = pointerGrid - sourceItemOriginGrid`。
   - 每帧换算预期中心：`intendedItemCenterGrid = pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)` 传入 `IPlacementCandidateEvaluator`。
   - 悬浮图标按 `pointerGrid - grabOffsetInFootprint` 保持相对抓取位置跟随（70% 透明度）。
   - 占据框按 Evaluator 返回的 `Candidate.(X, Y)` 渲染。
2. **旋转交互与抓取偏移变换**：
   - 响应 Unturned 原生 `rot++`（`[R]` 键顺时针旋转），尺寸变为 $H \times W$。
   - **Forward 变换（Native `rot + 1`）**：
     $$\text{newGrabX} = H - \text{oldGrabY}$$
     $$\text{newGrabY} = \text{oldGrabX}$$
   - 抓取偏移变换后立即同步重算 `intendedItemCenterGrid` 并重新调用 Evaluator。
3. **占据网格图层（Footprint Overlay）**：
   - 双态渲染：合法显示绿色（`#33E566`, 40% Alpha），阻挡/越界显示红色（`#E53333`, 40% Alpha），移出容器自动隐藏。
   - 挂载于顶级 View，避开单个 `ItemBox` 的视口裁剪（Clip）。
   - 常驻 `SleekImage` 矩阵对象池，设计目标为拖拽全程零 GC 分配（待后续 Release C# 分配测试验证）。
4. **释放与超时**：
   - 释放瞬间淡出增强图层，进入 `AwaitingProjection`（2.0 秒视觉等待预算）。
   - 2.0 秒超时仅结束等待指示，不推断拒绝，不私自回滚；最终显示严格跟随原生库存投影。

*（注：本票据设计在 Wayfinder 阶段已闭环；生产代码实现、C# 零分配及三环境运行有待开发阶段实际验证）*

