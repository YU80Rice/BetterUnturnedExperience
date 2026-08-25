# DEV-15B：Coordinate + Preview Wiring

Type: task  
Status: ready-for-human  
Owner: GPT（坐标适配与预览 Seam）  
Required reviewer: Gemini（前端消费与 UI 接线）  
Parent: DEV-15  
Baseline: BUE-V1-RT01-20260824  
SourceSet: BUE-SS-20260824-02  
Specification: `../spec-DEV-15-better-item-interaction.md`  
Dependency: DEV-15A（resolved）

## 目标

把屏幕指针、UI Scale、滚动偏移、网格裁剪和当前抓取偏移转换为冻结算法所需的 `intendedItemCenterGrid`，并通过可替换的纯值预览 Seam 输出绿色/红色占据框与浮动物品图标命令。

## 本票范围

- 坐标转换：左上原点、X 右/Y 下、UI Scale、滚动偏移、网格 viewport 裁剪；
- 按当前旋转计算 footprint center，使用 `pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)`；
- 接线 `InventoryDragPresenter` 与 `PlacementCandidateEvaluator`；
- 合法候选输出绿色占据框和浮动物品图标；非法候选输出红色占据框；Hidden 隐藏全部图元；
- 复用调用方提供的 occupancy/渲染 sink，不在热路径创建临时集合、字符串或闭包；
- 保持 Candidate.Rotation/Width/Height 直接来自 Evaluator，不由表现层重算；
- UI 类型门禁、Release 0 errors/0 warnings、坐标与预览单元测试。

## 明确不做

- 不引用 Unity、Glazier、Sleek、LMN、Unturned、BepInEx 或 Harmony 类型；
- 不实现真实 Unity Hook、Harmony Patch、原生库存投影 Relay 或网络逻辑；
- 不修改 Contracts，不新增库存 RPC，不建立第二库存模型；
- 不改变 DEV-04 Local-Fit 优先级或 JCR-07 Forward 旋转公式；

## 验收条件

- [x] TDD Red → Green：坐标换算覆盖 UI Scale、滚动、当前旋转 footprint center 和 grab offset；
- [x] viewport 外指针隐藏所有预览；
- [x] 合法候选一次更新绿色占据框和浮动物品图标，尺寸/旋转与 Evaluator 一致；
- [x] 非法候选仅显示红色占据框，不发出提交动作；
- [x] Hidden 清理全部图元；
- [x] 连续更新不依赖 LINQ、闭包、临时集合或逐帧字符串日志；
- [x] ClientUi 源码无 UI/Native/LMN 类型泄漏；
- [x] Release 构建 0 errors / 0 warnings；
- [x] 独立 GPT 审计 PASS（`GPT-DEV-15B-Independent-Audit-R2.md`）；
- [ ] Gemini 前端消费复核 ACCEPT 后方可关闭本票。

## 当前证据

- `InventoryPreviewWiring.cs` SHA-256：`88073877388963F50F97E8010EEF7EE59A7675D8E200BBB0DDC3784B66589878`
- `Dev15BTests.cs` SHA-256：`3F9A69C389D6283E5B8225E4C654A0B7103E898288BDDF799A42ED0D3F512073`
- ClientUi Release DLL SHA-256：`A354046A9FB3C23F9C6920DD0D727AA53E52D398232B496AC44A559E626013C8`
- 主插件 Release DLL SHA-256：`A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`
- GPT 独立审计：`audit/2026-08-25/GPT-DEV-15B-Independent-Audit-R2.md`
- Gemini 交接：`handoffs/GPT-to-Gemini-DEV-15B-coordinate-preview.md`

## 证据边界

本票通过仅证明纯 C# 坐标与预览 Seam；不证明真实 Unturned UI、单人、SteamP2PFriends、U3DS 或发布资格。
