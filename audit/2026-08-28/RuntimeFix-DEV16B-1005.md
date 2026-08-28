# 🛠️ DEV-16B 运行时修复执行报告 - 2026-08-28（R7）

### 一、问题定位与修复策略
- **R5 实机证据**：`UMM-诊断包_20260828_095717/LogOutput.log` 已确认运行 DLL SHA-256 为 `A5F788C47711075D73B41E8898454367A72C64466CD8EB202479844A37662322`，且 Dashboard/Workshop/Pause 三组 Hook 均安装。
- **剩余症状**：日志仍无 `start-entered`、`plugin-update`、`surface-opened`、`create-button-begin` 或 `add-child-success`；用户三个界面均无入口。结论是 BUE `BaseUnityPlugin.Start/Update` 生命周期在该宿主未被调度，且页面 open 可能早于 BUE 加载。
- **R6 修复**：将同一注入泵挂到原生 `MenuUI.Update` 与 `PlayerUI.Update` Postfix；两者在 U3-SDK 中是持续运行的 UI 宿主更新方法。BUE 自身生命周期保留为诊断与兜底，不再作为唯一运行条件。

### 二、核心代码变更
- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
  - `TickSource` 增加 `HostUi`。
  - 安装 `MenuUI.Update`、`PlayerUI.Update` Postfix。
  - `OnHostUiTick` 在主线程调用 Dashboard/Workshop/Pause 注入泵。
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
  - 新增两个 Host UI Update Hook owner 断言。
- **部署产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r6-20260828/BetterUnturnedExperience.dll`
- **SHA-256**：`3A477F6326A961C27EEF2FCFA97D18B893C258CFE2C50DB966ED1D003D8114E6`

### 三、编译与自测状态
- Release 解决方案：0 errors / 0 warnings。
- 7 个测试程序：全部 PASS。
- Contracts/Core/ClientUi UI-native token scan：全部 PASS。

### 四、审计结论
- R5 审计指出的父容器重绑风险已在 R5 修复。
- R6 新增 Hook 仅在原生 UI Update Postfix 中调用注入泵，不阻断原生 Update；仍保持 Client-only 构造与 `UnpatchSelf` 清理。
- Dashboard 分支是主菜单规范入口；Workshop 分支明确为额外子页入口，不再替代 Dashboard。
- `host-ui-tick` 首帧日志用于证明原生 UI Update Postfix 是否实际执行。
- **状态**：`ready-for-human`，R7 仍需实机验证按钮可见性和点击链。

### 五、人工复测
1. 仅部署 R7 DLL，启动后核对 `event=assembly-identity` SHA-256 为 `DE4410...09990`。
2. 观察日志是否出现 `patch-installed target=MenuUI.Update`、`patch-installed target=PlayerUI.Update` 和 `host-ui-tick`。
3. 打开主菜单 Dashboard、创意工坊页面、游戏内 ESC，确认三个位置出现“BUE 插件管理”。
4. 重点导出 `create-button-begin`、`create-button-result`、`add-child-success`；若仍无注入事件，检查 `MenuUI.Update`/`PlayerUI.Update` Hook 是否触发。
