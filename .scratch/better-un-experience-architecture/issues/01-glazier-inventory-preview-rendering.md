> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-01: SDG Glazier 原生物品模糊拖拽与绿色占据网格渲染管线

> **SUPERSEDED**：当前票据为 `../../better-unturned-experience-architecture/issues/01-glazier-inventory-preview-rendering.md`。

- Type: prototype
- Status: open
- Author: Gemini
- Blocked by: None

## Question

在 Unturned 原生 SDG Glazier UI 框架下，如何高效、无卡顿地实现物品拖拽时的半透明跟随光标（Floating Icon）与目标容器网格上的实时绿色/红色占据网格预览（Occupancy Footprint Overlay）？

## 前端设计考量（Gemini 视角）

1. **图层层级（Z-Order）与裁剪**：
   - 原版背包 `ItemBox` 与 `ItemContainer` 存在嵌套与视口裁剪。
   - 预览图层需要独立于单个背包格子，挂载在顶级 `GlazierView` 上层，避免被容器边框或滚动条遮挡。
2. **性能与重绘控制**：
   - 鼠标高频移动时，避免每帧销毁重建 UI 元素。
   - 采用常驻对象池或动态更新 `PositionOffset_X/Y` 与 `SizeOffset_X/Y`，仅在跨越格子单元格边界时触发重算。
3. **颜色与视觉反馈**：
   - 有效可放置：绿色微透遮罩（如 `Color(0.2f, 0.9f, 0.3f, 0.45f)`）。
   - 空间不足/被阻挡：红色微透遮罩（如 `Color(0.9f, 0.2f, 0.2f, 0.45f)`）。
   - 旋转支持：响应原版 `R` 键旋转物品尺寸，实时更新占据框长宽。


