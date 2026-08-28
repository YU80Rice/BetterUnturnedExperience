# DEV-16B 运行时按钮未注入修复报告（R10）

日期：2026-08-28
版本：DEV-16B R10
状态：ready-for-human（等待真实 Unity/BepInEx 实机日志）

## 一、问题定位与修复策略

根因：此前运行入口虽已统一到 Dispatcher，但按钮注入异常在 TryAddDashboardButton、TryAddMainButton、TryAddPauseButton 内被吞掉，无法触发统一隔离；回归测试也只验证 Dispatcher 计数，未经过面板的按钮注入 Seam。

修复：
- BueRuntimeTickDispatcher 统一承接 Plugin Update、RuntimePump、Harmony 与 HostUi 路径，提供同帧同来源去重、重入拒绝、帧源异常隔离和首次失败 Fail-Closed。
- 按钮注入失败重新抛回 Dispatcher，由 OnTickFailure 触发面板幂等销毁、UI 清理、Harmony UnpatchSelf 与运行泵清理。
- 新增 IBueButtonInjectionSeam，测试直接验证 Dispatch → TickCore → button injection，并覆盖同帧去重、异常隔离和后续回调拒绝。
- 纯 C# 测试宿主无 Unity 原生帧号时使用稳定哨兵，真实 Unity 仍使用 Time.frameCount。

## 二、核心代码变更

- src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs
  - 维持插件对象与运行泵跨场景存活；公开 Unity Start/Update 消息入口；统一调用面板 Dispatcher；面板隔离后销毁 RuntimePump。
- src/BetterUnturnedExperience.Plugin/BueRuntimePump.cs
  - 新增 BueRuntimeTickDispatcher，实现去重、重入保护、异常隔离。
- src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs
  - 新增按钮注入 Seam；所有运行来源统一进入；注入异常上抛至隔离边界；安全帧号提供器。
- 	ests/BetterUnturnedExperience.Plugin.Tests/Program.cs
  - 新增运行驱动与面板注入回归测试。
- 	ests/BetterUnturnedExperience.ClientUi.Tests/Dev15CTests.cs
  - 保留工作区已有的 projection source seam 测试变更，不作为本轮生产修复依据。

## 三、编译与自测状态

- dotnet build BetterUnturnedExperience.sln -c Release --no-restore: 0 errors / 0 warnings
- 7 个测试程序：全部 PASS
- Verify-NoUiTokens.ps1：ClientUi/Contracts/Core 全部 PASS
- git diff --check：通过

产物：$artifact\BetterUnturnedExperience.dll
SHA-256：$hash

## 四、独立审查记录

| 审核项 | 判定 | 说明 |
| :--- | :--- | :--- |
| Standards | PASS | 无规范、单 DLL、Headless 隔离或类型泄漏阻断 |
| Spec（本轮运行时修复范围） | PASS | Dispatcher、按钮注入 Seam、Fail-Closed 与回归测试满足 DEV-16B 修复目标 |

审查轮次：R10。R9 曾发现按钮注入异常吞掉及测试未触达面板 Seam；R10 已修复并通过复审。

## 五、真实验证边界

静态构建与自动化测试不能替代真实客户端证据。请部署该 DLL 后确认 UMM 日志包含：plugin-update 或 untime-pump-tick、host-ui-tick、surface-opened、create-button-begin、dd-child-success。在获得新的真实日志前，DEV-16B 保持 eady-for-human，不得标记 resolved。

## 六、偏离与妥协

无偏离。
