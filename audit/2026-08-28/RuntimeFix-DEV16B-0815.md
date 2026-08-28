# 🛠️ DEV-16B 运行时修复执行报告 - 2026-08-28

### 一、问题定位与修复策略
- **用户现象**：单机主菜单/暂停菜单看不到 BUE 管理入口。
- **日志证据**：`UMM-诊断包_20260828_081850/LogOutput.log` 仅有 `patch-installed`、启动时 `container-state ... state=null`，无 `constructor-postfix`、`create-button-begin`、`add-child-success`，也无原有 Update 心跳；说明 UI 构造发生在 BUE Awake 前或运行期入口泵未命中。
- **对照事实**：U3-SDK `MenuWorkshopUI`/`PlayerPauseUI` 的构造发生于 `MenuDashboardUI` 初始化；`UnturnedPluginManager` 通过 Harmony 构造补丁与每帧 `Tick()` 注入。
- **修复策略**：在保留构造 Postfix、轮询和生命周期清理的基础上，增加 `MenuWorkshopUI.open` 与 `PlayerPauseUI.open` 的 Harmony Postfix；页面打开后立即重新尝试注入，覆盖 BUE 启动晚于 UI 构造的时序。

### 二、核心代码变更
- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
  - 新增两个页面打开 Postfix。
  - 将四个核心 Postfix 分别隔离捕获错误，单个补丁失败不阻断其它 Hook。
  - 新增 `OnSurfaceOpened`，记录 `surface-opened` 并调用 `Tick`。
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
  - 增加 `plugin-update` 诊断心跳，确认 BepInEx/Unity 是否持续调用插件 Update。
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
  - 新增 Harmony 回归断言：`MenuWorkshopUI.open`、`PlayerPauseUI.open` 均安装 BUE owner 补丁。
- `tests/BetterUnturnedExperience.Plugin.Tests/BetterUnturnedExperience.Plugin.Tests.csproj`
  - 为回归测试显式引用 `0Harmony.dll` 与 `Assembly-CSharp.dll`。

### 三、编译与自测状态
- **Release 解决方案编译**：通过，0 errors / 0 warnings。
- **7 个测试程序**：全部 PASS。
- **UI/native token scan**：Contracts 2、Core 10、ClientUi 11 个 C# 文件全部 PASS。
- **部署产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r3-20260828/BetterUnturnedExperience.dll`
- **SHA-256**：`C3CB89BE3A00C23E48BCD10850E030CA0FC9C325361E2CC18B1B3DAD8A3A2BFA`

### 四、独立审计记录
- 子智能体独立审计：**PASS**。
  - Spec：`MenuWorkshopUI.open` / `PlayerPauseUI.open` 均存在并安装 BUE Postfix；补丁仅触发注入泵，不改写原生打开流程。
  - Standards：补丁逐项隔离失败、`activeInstance` 与 `UnpatchSelf()` 清理闭环、Client-only 构造保持 Headless 安全。
  - 限制：Harmony 元数据与自动化测试不能替代真实游戏按钮可见/可点击证据。

### 五、人工复测步骤
1. 覆盖 BepInEx/plugins 中旧版 `BetterUnturnedExperience.dll`。
2. 启动单机，进入创意工坊页与游戏内 ESC 暂停页。
3. 观察是否出现 `BUE 插件管理`；点击后确认面板标题。
4. 导出新的 UMM 诊断包，重点检查 `patch-installed target=MenuWorkshopUI.open`、`surface-opened`、`create-button-begin`、`add-child-success`。
