# GPT → Gemini：DEV-15B 前端表现组件 R2 返修交接

## 当前裁定

GPT 独立复核：**PARTIAL / BLOCKED**。纯 C# 坐标/预览 Seam 仍 PASS，但表现组件尚不能作为真实前端完成品关闭 DEV-15B。

## 已完成修正

- 占据框映射改为消费 `PreviewFrame.CellPixelSize = input.CellPixelSize * input.UiScale`，移除固定 `50f`。
- 恢复 `HidesAwaitingProjectionState()` 测试入口。
- 中性化表现抽象命名，ClientUi token 扫描通过。

## 请 Gemini 返修并复核

1. 让 `PreviewIcon`/表现 Sink 绑定被拖拽物品的原生图标或明确的资产身份；不能只创建通用 Image。
2. 从真实 inventory surface 接入网格面板、顶层容器、实际 cell size、UI scale、滚动和裁剪；`OnInventoryOpened` 不应为空，`OnDragUpdated` 必须拒绝旧容器/旧 `SessionGeneration`。
3. 保持无程序集扫描、无全局反射、无 Contracts/Core UI 类型泄漏；真实引擎适配应留在 ClientUi 卫星边界。
4. 补充容器切换、UI 关闭、SafeMode/隔离后的图元清理测试。

## 当前证据

- Release：0 errors / 0 warnings。
- 7/7 测试：PASS。
- 详细报告：`audit/2026-08-25/DEV-15B-Frontend-Component-Audit-R2.md`。

修复后请重新输出 Gemini `ACCEPT`/`PARTIAL` 复核报告；在 ACCEPT 前不要推进真实 DEV-15C 集成或宣称 Better Item Interaction 已完成。

