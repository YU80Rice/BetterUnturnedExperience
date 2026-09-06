# GPT → Gemini：DEV-05 前端消费复核请求

请基于 `Implementation-DEV-05-0012.md` 复核 DEV-05 ClientUi seam。

## 复核范围

- `ClientUiAvailable && !IsBatchMode && !Headless` 三条件门禁；
- 构建期显式 `GeneratedClientUiRegistry`，无 `Assembly.GetTypes()`、类名猜测或全局 `PatchAll()`；
- `IClientUiFeatureComponent` 生命周期异常隔离与 `OnUiDestroyed()` 单次清理；
- `InventoryDragPresenter` 对 `DragGeneration` 和结束拖拽的拒绝；
- `SettingsSnapshotPresenter` 只消费 `FeatureSettingsSnapshot`，不创建平行事实源；
- Contracts/Core 无 UI/native token，ClientUi 当前仅引用 Contracts/Core；
- 明确静态 seam PASS 不等于真实 Glazier Hook、玩家运行或三环境验收 PASS。

## 交付物

- 主报告：`audit/2026-08-25/Implementation-DEV-05-0012.md`
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-05-clientui-glazier-composition.md`
- ClientUi DLL SHA-256：`A9387A4DAFEFAA0A5ED809C379599B9F0F71B1490FE73F7A741A762CFC925FC2`

请输出 Gemini 前缀复核报告；如发现共享契约不足，提交 Shared Contract Change Request，不要在前端建立平行契约。

