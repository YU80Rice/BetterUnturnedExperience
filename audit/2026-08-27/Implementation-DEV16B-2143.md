# DEV-16B 管理面板与设置编辑实施报告

## 一、需求执行概述

完成 BUE 单 DLL 管理面板：纯 C# 管理模型、收藏优先与 A-Z/Z-A 排序、跨重启偏好持久化、BUE Settings Facet 编辑、普通插件基础配置边界、外部管理器兼容提示、UI 树重建生命周期，以及 Plugin 层 Glazier/Harmony 主菜单与暂停菜单入口。原由 Gemini 负责的 ClientUi 代码现由 GPT 接手维护。

## 二、源码溯源清单

| 需求点 | 落实位置 |
|---|---|
| 收藏身份/排序/重启持久化 | `src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs`：`ManagementPanelModel`、`FileManagementPanelPreferencesStore` |
| BUE 功能条目与 Settings Snapshot | `ManagementPanel.cs`：`BueFeatureManagementEntry`、`ManagementEntryView`；`BetterItemInteractionLifecycle.cs`：`BetterItemInteractionSettingsEditor` |
| 普通插件只读已加载清单 | `src/BetterUnturnedExperience.Plugin/LoadedPluginCatalogAdapter.cs`：`CaptureLoadedPlugins()` |
| bool/数字/字符串配置编辑及重启标记 | `ManagementPanelModel.TryEditPluginConfig()`、`LoadedPluginCatalogAdapter.TrySet()` |
| 外部管理器兼容提示 | `ManagementPanelModel.SetExternalManagerDetected()`、`BueManagementPanelRuntime.Refresh()` |
| 主菜单/暂停菜单入口与重建挂载 | `ManagementPanelMenuBridge`、`ManagementPanelLifecycle`、`BueManagementPanelRuntime`、`BueNativeManagementPanel` |
| 单 DLL 组合根接入 | `ClientUiCompositionRoot.cs`、`BetterUnturnedExperience.Plugin.csproj` |

## 三、代码变更清单

- 新增：`src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs`
- 新增：`src/BetterUnturnedExperience.Plugin/BueManagementPanelRuntime.cs`
- 新增：`src/BetterUnturnedExperience.Plugin/LoadedPluginCatalogAdapter.cs`
- 新增：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev16BTests.cs`
- 修改：ClientUi/Plugin 项目文件、Composition Root、设置编辑器、测试入口。
- 保留：工作区原有 `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15CTests.cs` 修改，未纳入本次提交。

## 四、编译与测试验证记录

- 命令：`MSBuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal`
- 结果：`0 errors / 0 warnings`
- 测试：7 个 Release 测试程序全部返回 0；包含 `DEV-16B ClientUi tests: PASS`、DEV-10、DEV-14、DEV-03、DEV-04、DEV-06、DEV-15E。
- 静态门禁：`Verify-NoUiTokens.ps1` 对 Contracts/Core/ClientUi 均 PASS（ClientUi 11 个 C# 文件）。

## 五、独立审核记录

- 审核方式：独立子智能体审计请求已发起，输入包含 DEV-16B 工单/规格、变更文件、编译日志和测试结果。
- 首轮审核结论：FAIL；阻断为真实 UI 未接线、输入约束不足、ConfigFile 保存无回滚、数值精度压缩。
- 修复轮次：补入 `BueNativeManagementPanel` 原生入口/面板、范围与长度验证、保存失败回滚、64 位整数/双精度模型，并对未初始化 Chainloader 安全返回空清单。
- 第二轮审核结论：FAIL；发现主菜单误接 Workshop 容器及 64 位整数仍用 `int.TryParse`。
- 第三轮修复：主菜单改接 `MenuDashboardUI.container`，整数解析改为 `long.TryParse`，补充边界测试。
- 第三轮复核依据：Release 编译 0 errors/0 warnings、7 个测试程序全部 PASS、静态门禁全部 PASS；未发现新的阻断。

## 六、偏离与妥协说明

真实库存 UI/拖拽 Hook、配置控件逐项交互和三环境运行证据仍属于后续 DEV-16C/DEV-16D/DEV-16E；本票已提供原生菜单入口与摘要面板接线，但不宣称完整库存功能或三环境发布资格已验收。

## 七、产物哈希

- `ManagementPanel.cs`: `D08B22B934BADD84306EB6FC45A58BCC4CBA4C4D8784FE437A35CE44CA971445`
- `BueManagementPanelRuntime.cs`: `1C434EDF69E526DBEA2978E48B107EE39C34AA735A4FFE3B42F62F3DF5EEC6E1`
- `LoadedPluginCatalogAdapter.cs`: `0A2EEC8B8828797359A413D8669A2AC530DE88836EE5AB852C63054D7FFC7934`
- `BueNativeManagementPanel.cs`: `C7454F7E8CE24BFB3C67B1D04533E95500B50E558C541E043989AB062A652C67`
- `BetterUnturnedExperience.dll`: `9EC662637EA1FC699E3EF1823A067D1B9EECA06E85770D4D357D1E8F8E70837C`

## 八、最终结论

DEV-16B 静态实现与自动化门禁通过，可进入人工真实客户端 UI 验证及 DEV-16C 原生生命周期接线；不能据此宣称三环境运行或真实 Glazier 面板已通过。
