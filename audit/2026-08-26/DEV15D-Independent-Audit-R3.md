# GPT-DEV15D 独立审计报告 R3

## 审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15D-settings-lifecycle-isolation.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`
- 性质：R2 阻断修复后的独立只读复测；不宣称真实 Unity/Glazier、单人、SteamP2PFriends、U3DS 或发布资格通过。

## R2 阻断复核

| 阻断项 | 修复事实 | 判定 |
| :--- | :--- | :---: |
| Disabled/Isolated/SafeMode/Headless 仍可挂载 UI | `OnInventoryOpened` 先清理旧 UI，再检查 `SafeMode/CanRun/satelliteAvailable/headless`；关闭设置也清理图元；缺失卫星不绑定图元 | PASS |
| Surface 重绑未失效旧拖拽 | 新 surface 被接受前调用 `CleanupUiAndDrag()`，卸载旧 sink 并结束 runtime/presenter drag | PASS |
| 正常提交后状态残留 | `OnDragReleased` 成功调用 adapter 后统一 `runtime.EndDrag()` | PASS |
| 错误/旧/重复快照可覆盖 | 官方 FeatureId、ClientPreference scope、严格递增 revision 门禁 Fail-Closed | PASS |

## 需求与边界

- Enabled/AutoRotate 默认均为 true；拖拽开始捕获不可变策略，设置变更只影响下一次拖拽。
- 九态生命周期显式拒绝非法转换并记录 `BUE-DEV15D-INVALID-STATE-TRANSITION`。
- 清理异常继续逆序清理，失败保持 `Isolated` 并记录 `BUE-DEV15D-CLEANUP-INCOMPLETE`，不伪造 `Stopped`。
- SafeMode 清理自定义 UI、投影 `HeadlessOnly`，禁止再次装配 ClientUi。
- 卫星缺失投影 `PresentationDegraded`，Headless 投影 `HeadlessOnly`；核心/设置仍可用。
- 禁用、异常、隔离和不可用路径走原生 Pass-Through；未新增库存 RPC，未修改 Contracts、LMN、Unturned。

## 构建与测试证据

`dotnet build BetterUnturnedExperience.sln --configuration Release --nologo`：`0 errors / 0 warnings`。

七个测试项目全部退出码 0：ClientUi DEV-05/DEV-15A/DEV-15B/DEV-15C/DEV-15D、Contracts DEV-10、Network DEV-06、Placement DEV-04、Plugin DEV-14、Release DEV-08、Settings DEV-03 均 PASS。

静态门禁：ClientUi 10 files、Contracts 2 files、Core 10 files 均 `UI/native token scan PASS`；`git diff --check` 通过。

## 产物 SHA-256

- `BetterItemInteractionLifecycle.cs`: `75646C5C8AC37329C6D4FAA9079677767A00C70594F0678B5E0E0C89F925226C`
- `ItemInteractionUiComponent.cs`: `1912E1CEDF20D3B15FCB2F205E011A2CBB23003FB93A5C92A7CBF6A378C13300`
- `ClientUiTypes.cs`: `532E430D5961CE7403B029AC7853EECB3B48F3C98897EB1DF201CD41F5DD8BA8`
- `Dev15DTests.cs`: `EA30ECB8F35A3E208520C1FDAE0EC407AF71238AEAB39F9B15B13A0419B7165D`
- `BetterUnturnedExperience.ClientUi.dll`: `1B08920444270817B7A5E22C073695174480BE1D0D0F84C3AB2A636D3A3CF737`
- `BetterUnturnedExperience.dll`: `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`

## 最终裁定

**PASS（GPT 独立审计 R3，无阻断）**。Gemini 前端消费复核和真实三环境运行仍是后续门禁。
