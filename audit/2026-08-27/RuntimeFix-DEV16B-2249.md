# 🛠️ DEV-16B 运行时诊断报告 - 2026-08-27

### 一、问题定位与当前结论

- **用户症状**：单机启动后未出现 BUE 管理面板入口；截图仍为原生主菜单/暂停菜单；日志没有按钮注入成功或失败记录。
- **证据包**：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260827_222422`
- **已确认**：`LogOutput.log` 证明主 DLL 被 BepInEx 加载，并输出 `BUE-CLIENTUI-002`、`BUE-BOOTSTRAP-001`、官方功能注册成功；这只能证明纯 C# Composition Root/Bootstrap 已运行，不能证明原生按钮已创建或挂载。
- **当前反馈回路**：对原始日志运行 `[BUE-UI-TRACE]` 检查，结果为 `RED: no [BUE-UI-TRACE] evidence; UI injection path not observed`；重复 3 次均为 RED。
- **根因状态**：尚未能从旧部署日志区分 `Update()` 未执行、原生容器为空、反射读取失败、Glazier 创建失败或 `AddChild` 失败；旧版本没有足够的边界日志，因此不能安全地声称具体根因。

### 二、静态证据与假设

1. `BueNativeManagementPanel.Initialize()` 在插件 `Awake()` 中创建并执行一次 `Tick()`；后续依赖插件 `Update()` 轮询。
2. 主菜单目标为 `MenuDashboardUI.container`，游戏内暂停目标为 `PlayerPauseUI.container`，两者在 U3-SDK 中均为私有静态字段。
3. `PatchRebuildHooks()` 仅安装构造函数 postfix；`OnUiRebuilt()` 当前不修改状态，真正注入依赖轮询路径。
4. 原始日志没有按钮创建、`AddChild` 或失败日志，故旧部署未提供注入链证据。

### 三、本轮变更（临时诊断仪表化）

- **修改文件**：`src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
- 新增唯一前缀 `[BUE-UI-TRACE]`，覆盖：构造、初始化、首次 `Tick()`、主/暂停容器状态与 `active`、按钮创建前后、`AddChild` 成功、异常类型与消息。
- 保持业务行为不变；仅增加可区分假设的日志，不改变入口目标、坐标或生命周期策略。

### 四、编译与自测状态

- **编译命令**：`dotnet build BetterUnturnedExperience.sln --configuration Release --nologo`
- **编译结果**：`0 errors / 0 warnings`
- **测试结果**：7 个 Release 测试程序全部 PASS（ClientUi、Contracts、Network、Placement、Plugin、Release、Settings）。
- **新 DLL**：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- **SHA-256**：`FF5D1BE1FA835714C494273091308ABFCCB6059B0EB598A59439C53E3D2E1A4D`

### 五、下一步人工复测

1. 用上述新 DLL 替换 BepInEx/plugins 中旧的 `BetterUnturnedExperience.dll`。
2. 启动单机并至少进入一次主菜单、一次游戏内暂停菜单。
3. 导出新的 UMM 诊断包，保留 `LogOutput.log`。
4. 重点提供包含 `[BUE-UI-TRACE]` 的日志行；这些行将直接确定断点。

### 六、审核记录

- 本轮属于运行时诊断仪表化，尚未宣称 DEV-16B 已修复或关闭。
- 需在新日志确认注入链后再决定是否修改目标类型/生命周期，并重新编译、回归测试及独立审计。

### 七、最终结论

- **状态**：`EVIDENCE_INSUFFICIENT / ready-for-human`
- **阻断项**：缺少新版本 `[BUE-UI-TRACE]` 运行证据，无法确认真实原生 UI 注入是否进入调用链。

### 八、后续复测结果（2026-08-27 23:18）

- 新诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260827_231844`
- 新包确实包含 `[BUE-UI-TRACE]`，证明部署的是带仪表化版本。
- 观测到：`constructed mainField=True pauseField=True`、`initialize complete`、`first tick reached`、`main container state=null active=False`、`pause container state=null active=False`。
- 未观测到：`update heartbeat`、`source=update`、`CreateButton` 或 `AddChild`。

**诊断裁定**：当前已确认 `Initialize()` 内的一次 Tick 执行及初始化时两个容器均为 null。该包对应的第一版仪表化 DLL 尚未包含 `update heartbeat`/`source=update` 日志，因此不能据此证明 `BetterUnturnedExperiencePlugin.Update()` 未被 Unity 调度。截图中的主菜单/暂停菜单仍为原生界面，与“初始化时尚未注入”一致。

**未越权结论**：证据可以确认初始化时尚未进入按钮注入分支，但尚不能仅凭该包证明组件被禁用、插件对象被销毁、Unity 消息调度异常，还是仅仅等待后续 UI 构造。下一步应使用 R2 仪表化 DLL 进行一次复测，再决定是否进入事件驱动接线修复；这属于后续 RuntimeFix，不在本轮诊断仪表化内实现。
