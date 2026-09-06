# DEV-09 Runtime Bootstrap + BepInEx Plugin Entry 实施报告

## 一、需求执行概述

完成 BUE 聚合 DLL 的最小 BepInEx 入口与启动守卫闭环；不实现库存 Hook、Glazier UI、LMN 网络或玩家功能。

## 二、源码溯源清单

| 需求点 | 落实位置 |
|---|---|
| BootstrapGuard 公共 seam 与四分支判定 | `src/BetterUnturnedExperience.Plugin/BootstrapGuard.cs`；`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` |
| 唯一 BepInEx 入口、固定 FeatureId、预发布版本 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs` |
| Headless/Batch/UI 可用性前置分流 | `BetterUnturnedExperiencePlugin.Awake` → `BootstrapGuard.Decide` |
| Fail-Closed 启动诊断 | `BetterUnturnedExperiencePlugin.Awake` 的异常捕获；正常/异常均记录 `featureId`、`diagnosticId`、`status` |
| 引擎依赖边界 | `BetterUnturnedExperience.Plugin.csproj` 仅由 Plugin 边界引用 BepInEx/Unity；Contracts/Core/Release 通过程序集引用复核 |

## 三、代码变更清单

- 已存在并验证：`BootstrapGuard.cs`、`BetterUnturnedExperiencePlugin.cs`、Plugin 工程引用与测试。
- 本轮未新增生产代码；R1 阻断修复已存在于当前源码：日志键值中显式包含永久 `FeatureId` 与结构化状态。
- 更新 DEV-09 工单为 `ready-for-human`，等待 Gemini 前端消费复核。

## 四、编译验证记录

命令：

```text
dotnet msbuild D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：PASS，0 errors / 0 warnings。

## 五、测试结果

Release 下现有 7 个测试项目全部 PASS：DEV-03、DEV-04、DEV-05、DEV-06、DEV-08、DEV-10、DEV-11；Plugin 测试覆盖 BootstrapGuard 四分支、No-op 外部注册、Catalog 冻结及晚注册拒绝。

## 六、独立审计记录

- R1：FAIL，阻断 `B-DEV09-01` 为诊断日志未显式记录 FeatureId/状态。
- R2：PASS，已确认正常/异常日志均包含 `featureId`、`diagnosticId`、`status`；报告：`audit/2026-08-25/DEV-09-Independent-Audit-R2.md`。

## 七、边界与未验证项

- 本交付不代表真实 BepInEx clean-install、U3DS Headless、单人、SteamP2PFriends Host/Client 或物品交互运行通过。
- 不授予 Stable、ReleaseReady 或三环境发布资格。

## 八、交付结论

GPT 后端实现与独立审计已 PASS，DEV-09 可交 Gemini 做前端消费与 Headless 边界复核；复核通过且人工确认后，下一步执行真实 BepInEx clean-install 加载冒烟。

