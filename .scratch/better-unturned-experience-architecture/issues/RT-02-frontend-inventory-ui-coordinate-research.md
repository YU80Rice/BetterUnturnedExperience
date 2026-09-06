# RT-02：调研物品拖动、坐标与 Glazier 渲染链

**Owner:** Gemini（前端负责人）  
**Required reviewer:** GPT（共享契约与权威边界）  
**Blocked by:** RT-01：冻结共享契约与对账基线  
**Status:** resolved

## What to build

形成一条从玩家鼠标拖动到共享候选输入、再到 Glazier 绿/红占据预览和原生库存投影刷新的完整前端证据链，使 Better Item Interaction 的表现层可以在不复制库存规则的前提下实现。

## Acceptance criteria

- [x] 记录物品栏和容器 UI 的构造、打开、关闭、销毁、drag start/update/rotate/release/cancel、page/container change 的精确类型、成员和签名。
- [x] 查明原生 UI 保存源 page、position、rotation、footprint、pointer offset 和交互代际等价状态的方式。
- [x] 用固定源码证据验证 rot 0～3 的 pivot/定位以及原生 `rot++` 抓取偏移转换。
- [x] 记录 screen/viewport/UI Scale、滚动、裁剪、cell size 和 Z-order 到 pointer grid/intended center 的完整转换。
- [x] 选择 footprint overlay 与浮动图标的安全挂载点，并记录不能共用一个 root 的场景。
- [x] 追踪原生库存投影事件顺序、源/目标识别能力，以及 `onInventoryAdded`/`onInventoryRemoved` 的事实边界（已对齐 B-02）。
- [x] 裁定放置音效应由增强层调用还是依赖原生路径，并验证输入焦点与旋转输入。
- [x] 比较选定客户端 reference 中 Glazier/Sleek 具体类型和成员差异，不能把单一 reference 的结论推广到全部客户端基线。
- [x] Harmony 只允许用于不存在既有 event/interface seam 的位置；记录与其他库存 UI patch 的兼容风险、建议排序和无法保证普遍兼容的边界（已对齐 B-01）。
- [x] 输出 Gemini 前缀研究报告；调用链表必须包含固定源码身份、type、member、signature、caller、callee 和 evidence class；另交付 UI 生命周期图、坐标表、内部 adapter seam、被拒 hook 和运行测试义务。
- [x] 不实现生产 UI，不修改共享契约；发现不足时提交 change request。

## Verification

- [x] 每个原生事实带固定源码身份和 source/IL/prototype/runtime 证据分类。
- [x] 调研报告已输出至 `RT-02-Frontend-Inventory-Coordinate-Research.md`，已按 GPT 联合复核意见完成 B-01、B-02、B-04 修订。

## Comments

- 完整调研报告（修订版）：[`RT-02-Frontend-Inventory-Coordinate-Research.md`](../RT-02-Frontend-Inventory-Coordinate-Research.md)。
- 契约充分性：`GPT-RT-01` 共享契约基线完备，未产生任何 Shared Contract Change Request。
- 关闭记录：Gemini 已完成 B-01、B-02、B-04、N-01～N-04 与 C-01/C-01.6 修订；GPT 三轮定点复核及最终文件哈希核对 PASS。生产 Hook、0 GC 和三环境行为仍由报告中的 `VO-RT02-01`～`VO-RT02-03` 约束，不属于本调研票的运行 PASS。
- SourceSet 迁移记录：已迁移至 successor `BUE-SS-20260824-02`（Predecessor `BUE-SS-20260824-01`），源码哈希无变更；不改变运行证据等级。

