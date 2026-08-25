# DEV-15C Projection Relay + AwaitingProjection 实施报告

## 需求执行概述

完成纯 C# 原生库存投影中继与 `AwaitingProjection` 视觉预算 Seam；不新增库存 RPC、不修改 Contracts/LMN/Unturned。

## 源码溯源清单

| 需求点 | 落实位置 |
|---|---|
| callback 只入队，Pump 主线程消费 | `InventoryProjectionRelay.cs`：`TryEnqueue`、`Pump` |
| 固定容量与溢出 fail-closed | `NativeInventoryProjectionRelay` 环形队列 |
| Drag/Session/Container 代际过滤 | `ProjectionBinding`、`NativeInventorySnapshot.MatchesGenerationAndContainer` |
| Item fingerprint 高置信匹配与歧义观察 | `AwaitingProjectionController.Apply` |
| 2 秒仅视觉预算 | `AwaitingProjectionController.Tick` |
| 消费者异常隔离 | `NativeInventoryProjectionRelay.Pump` 捕获 `Apply` 异常 |

## 代码变更

- 新增 `src/BetterUnturnedExperience.ClientUi/InventoryProjectionRelay.cs`。
- 新增 `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15CTests.cs`。
- 接入 ClientUi 与测试项目文件，更新测试入口输出 DEV-15C。
- 新增工单 `.scratch/better-unturned-experience-architecture/issues/DEV-15C-projection-relay-awaiting-projection.md`，状态 `claimed`。

## 编译与测试验证

- 命令：`dotnet build BetterUnturnedExperience.sln --configuration Release --nologo`
- 结果：`0 errors / 0 warnings`
- 7 个测试程序：全部 PASS，返回码 0。
- `Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.ClientUi`：PASS（9 C# files）。

## 复核状态

- GPT 本轮实现自测：PASS。
- 独立审计与 Gemini 前端消费复核：待交接后执行。
- 当前不得宣称真实 Unity callback、单人/P2P/U3DS 或发布资格通过。

## 提交

`2cefd63 Implement DEV-15C projection relay and awaiting projection seam`

## 偏离与妥协

无共享契约变更；真实原生 callback Hook 留在后续运行时接入门禁。
