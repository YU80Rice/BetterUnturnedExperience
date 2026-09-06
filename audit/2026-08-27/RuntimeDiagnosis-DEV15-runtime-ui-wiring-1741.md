# BUE 运行时 UI/库存接线诊断报告

## 一、诊断对象

- 用户症状：部署后未出现绿色/红色占据投影、浮动物品图标，也未出现“更好的 UN 体验”设置项。
- 诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260826_104829`
- 被测试构建：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- 构建 SHA-256：`A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`
- ClientUi 测试程序集 SHA-256：`1B08920444270817B7A5E22C073695174480BE1D0D0F84C3AB2A636D3A3CF737`

本轮为诊断，不修改生产源码。

## 二、Phase 1：红色反馈回路

### 1. 实机日志加载断言

命令对诊断包中的 `LogOutput.log` 断言插件加载、BootstrapReady、注册接受和 RuntimeReady，同时对生产源码断言 ClientUi 注册、ClientUi 编译接线和库存 Hook。

结果：

```text
EVIDENCE Loading [Better Unturned Experience 0.0.0]
EVIDENCE ... status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001
EVIDENCE ... accepted=True reason=None diagnosticId=BUE-REG-ACCEPT
EVIDENCE ... status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003
VERDICT=RED
FAIL=plugin project does not compile/reference ClientUi
FAIL=plugin entry has no inventory/drag hook
```

该命令已实际运行；它能在当前症状下稳定变红。

### 2. 最小运行链断言

命令反射加载 Release 主 DLL，实例化官方注册对象并读取 `ClientUi` 属性：

```text
RUN=1 RED ClientUi=null
RUN=2 RED ClientUi=null
RUN=3 RED ClientUi=null
```

此断言连续 3 次稳定失败，直接覆盖“官方功能能否产生 UI Satellite”这一最小必要条件。

## 三、Phase 2：证据与最小复现

### 已确认的启动事实

诊断包 `LogOutput.log` 第 14～18 行显示：

- BepInEx 5.4.23.5 正常启动；
- BUE 插件被加载；
- 官方 Better Item Interaction 注册返回 `accepted=True`；
- 主机进入 `RuntimeReady`。

因此，BepInEx 加载失败、主 DLL 找不到、注册屏障未开启均不是本次症状的根因。

### 最小失败场景

只需要以下两个条件即可复现：

1. 加载当前 Release 主 DLL；
2. 读取 `BetterItemInteractionFeatureRegistration.Registration.ClientUi`。

读取结果为 `null`，无需进入 Unity 场景、打开背包或执行拖拽即可失败。这是当前症状的最小化复现。

## 四、Phase 3：已评估假设

| 排名 | 假设 | 可证伪预测 | 结果 |
|---|---|---|---|
| 1 | 主插件未接入 ClientUi 程序集/源码 | 将 ClientUi 接入主项目后，编译期闭包断言应转绿 | **支持**：插件项目仅嵌入 Contracts/Core，未引用或编译 ClientUi |
| 2 | 插件入口没有库存 UI/拖拽 Hook | 若入口接入打开、拖拽、释放、关闭回调，运行时才可能出现投影/提交事件 | **支持**：入口只有 Bootstrap、SceneLoaded 和 RuntimeReady |
| 3 | 设置中心注册缺失 | 若创建设置描述/SettingsRuntime 并接入 UI 面板，应出现设置项 | **支持**：主插件未发现设置描述注册或设置面板调用 |
| 4 | 部署 DLL 与诊断包版本不一致 | 同一部署哈希与诊断包日志应能对应 | **未完全复核当前目录**：诊断包历史日志明确加载 BUE，但当前插件目录后来仅剩 SteamP2PFriends；不影响源码根因结论 |
| 5 | BepInEx/Unity 启动失败 | 日志不应同时出现 BootstrapReady、注册接受和 RuntimeReady | **排除** |

## 五、源码证据

### 1. 官方注册明确返回空 UI Satellite

文件：`src/BetterUnturnedExperience.Plugin/OfficialFeatureRegistration.cs`

```text
第 33 行：public IClientUiSatelliteRegistration ClientUi { get { return null; } }
```

同文件第 5～8 行注释还明确写明 native inventory/UI behavior 由后续切片实现。

### 2. 主插件项目未编译 ClientUi

文件：`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj`

第 11～19 行仅嵌入 Contracts/Core 源码；第 20～25 行仅包含插件自身源码，未包含 `ClientUi` 项目引用、`ItemInteractionUiComponent.cs` 或其它 ClientUi 文件。

### 3. 插件入口无真实库存回调

文件：`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`

现有入口仅执行：

- `BueRuntimeHost.Bind`；
- `OpenRegistration`；
- 官方功能注册；
- `SceneManager.sceneLoaded`；
- `CompleteRuntime`。

未发现 `OnInventoryOpened`、`OnDragStarted`、`OnDragUpdated`、`OnDragReleased`、`sendDragItem`、Harmony 库存补丁或等价原生回调接线。

### 4. ClientUi 代码实际存在但仍是独立卫星工程

文件：`src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`

第 26～38 行包含 `ItemInteractionUiComponent.cs`、`InventoryDragPresenter.cs`、`NativeInventoryInteractionAdapter.cs`、`InventoryProjectionRelay.cs` 等，但该工程反向引用 Plugin，主 Plugin 并未消费它；因此单独编译通过不等于运行时进入 BepInEx 主 DLL。

## 六、最终诊断结论

**判定：CONFIRMED ROOT CAUSE。**

当前主 DLL 成功加载并完成的是“BUE Bootstrap + 官方功能登记 + RuntimeReady 屏障”，不是 Better Item Interaction 的真实玩法实现。绿色/红色投影、浮动物品图标和设置项缺失，是因为：

1. 官方注册的 `ClientUi` 固定为 `null`；
2. 主插件未编译/引用 ClientUi；
3. 主插件入口未接入 Unity/Glazier/Sleek 表现层和原生库存拖拽回调；
4. 主插件未接入设置描述与统一设置中心。

这不是用户部署操作错误，也不是 DEV-15E 资格证据缺失造成的运行时隐藏；而是 DEV-15 纯 C# Seam 完成后，尚未实施真实运行时接线。

## 七、边界与下一步

- DEV-15A～DEV-15E 的单元测试与纯 C# Seam 验收不能证明真实 Unity/Glazier/库存 Hook 已部署运行。
- 当前不能宣称 Better Item Interaction 已具备实机功能，也不能采集 DEV-15E 的正式三环境资格证据。
- 应新建并实施 `DEV-16-runtime-clientui-native-inventory-wiring`，至少覆盖：ClientUi 进入主运行链、非空 Satellite 注册、真实库存打开/拖拽/释放/关闭回调、原生 `sendDragItem` 路径、统一设置中心以及 U3DS Headless 隔离。
- DEV-16 产生新 DLL 后，旧 CandidateBuild、DLL 哈希和运行证据全部失效，必须重新建立。

## 八、当前风险等级

**阻断级（Blocker）**：阻断 Better Item Interaction 实机功能测试与 DEV-15E 资格门禁；不阻断 BUE Bootstrap/公开 Host 注册骨架本身。

