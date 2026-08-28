# 🛠️ DEV-16B 运行时修复执行报告 - 2026-08-28

### 一、问题定位与修复策略
- **本轮现象**：用户截图停留在 Unturned 主菜单 Dashboard 与暂停设置层，未看到 BUE 管理入口。
- **日志证据**：`UMM-诊断包_20260828_090329/LogOutput.log` 证明上一版 DLL 已安装 `MenuWorkshopUI.open` / `PlayerPauseUI.open` Hook，但没有 `surface-opened`；同时启动时 `MenuWorkshopUI.active=False`、`PlayerPauseUI.active=False`。这与截图所示 Dashboard 页面一致，不能证明 Dashboard 入口存在。
- **对照 U3-SDK**：`MenuDashboardUI` 是主菜单左侧“开始游戏/角色设定/游戏设置/创意工坊”及右侧精选内容的实际宿主；其 `container` 为私有静态字段，`open()` 为公开静态生命周期方法。
- **修复策略**：新增 `MenuDashboardUI.container` 读取、Dashboard 构造/open Hook 与独立按钮注入；保留 Workshop、暂停菜单入口及旧有构造/轮询路径。启动日志新增程序集路径和 SHA-256、`Start` 进入证据，避免人工部署错 DLL 无法识别。随后根据独立审计补上父容器切换时旧按钮的对称清理与重绑断言。

### 二、核心代码变更
- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
  - 新增 Dashboard 容器、按钮、父级引用和清理逻辑。
  - 在 `Tick` 中执行 Dashboard 注入。
  - 安装 `MenuDashboardUI` 构造与 `open` Postfix。
  - Dashboard 按钮挂载到 `MenuDashboardUI.container`，位置位于原生四个主菜单按钮下方，点击打开 BUE 面板。
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
  - `Awake` 输出程序集实际路径与 SHA-256。
  - `Start` 输出 `start-entered`。
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
  - Harmony 回归断言扩展为 Dashboard/Workshop/Pause 三个 `open` Hook。
  - 增加父容器重绑语义回归断言。

### 三、编译与自测状态
- **Release 解决方案编译**：通过，0 errors / 0 warnings。
- **7 个测试程序**：全部 PASS。
- **UI/native token scan**：Contracts 2、Core 10、ClientUi 11 个 C# 文件全部 PASS。
- **部署产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r5-20260828/BetterUnturnedExperience.dll`
- **SHA-256**：`A5F788C47711075D73B41E8898454367A72C64466CD8EB202479844A37662322`

### 四、独立审计与代码审查
- DEV-16B 独立审计：PASS（Headless 隔离、补丁失败隔离、生命周期清理、真实运行证据边界均符合）。
- 代码审查：Standards 轴发现的父容器重绑阻断已修复；Spec 轴确认 Dashboard 是本轮截图对应的主菜单宿主，最终复核待提交后完成。

### 五、人工复测步骤
1. 只部署上述 r5 DLL，并在启动后从日志核对 `event=assembly-identity` 的 SHA-256 与 `A5F788C...62322` 完全一致。
2. 进入截图所示主菜单 Dashboard，确认左侧“创意工坊”按钮下方出现“BUE 插件管理”。
3. 点击入口，确认面板标题 `Better Unturned Experience · 插件管理`。
4. 进入游戏按 ESC，确认暂停菜单也出现同名入口。
5. 导出新 UMM 诊断包；重点检查 `patch-installed target=MenuDashboardUI.open`、`surface-opened`、`create-button-begin surface=MenuDashboardUI` 与 `add-child-success surface=MenuDashboardUI`。
