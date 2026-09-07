# DEV-V2-17 R3 Spec 轴判词

## 自我声明
我是 DEV-V2-17 R3 的全新上下文 Spec Reviewer；不继承任何前轮实例上下文，独立重读审查规约、工单、规格、CONTEXT、R3 增量及证据。仅核查遗漏/部分、范围越界、错误实现。

## 第一层：R2 CLEAN 依赖复核
通过。`round3-increment.diff` 相对 R2 仅有两项 Standards 修复：`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:204-206,236-237,288-292,411-412` 将时钟采样移到锁外；`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:735-739` 将 `SubscriptionRecord.Removed` 改为 `volatile`。两者不改变握手、peer+nonce 匹配、帧头 sender 权威身份、会话状态、事件时序或重置语义；R1 BLOCKER 修复仍在 `BueNetworkRuntime.cs:628-633,712-727,757-763`。未发现 Scope 面或行为面破坏。

## 第二层：Scope 内终审
工单要求“连接建立后的会话握手……全部由网络模块自动完成”“公开会话集合里永远只有……已建立会话”（工单 `:11`）；规格要求自动流程、职责、pending 隐藏及 peer+代际/nonce 匹配（`spec.md:121-123`）。实现证据为 `BueNetworkRuntime.cs:234-306,324-375,628-799`，测试夹具/断言为 `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:879-1415`。覆盖流程、去重、精确匹配、断线清理、换代际、Reject fail-closed、退避、锁外回调及无 ghost session。`AssertBueV2DirectionalSubscribe` 的 re-arm 更新（`Program.cs:3049-3064`）保留 14 票“订阅无需重挂”原意；重置自愈可由规格 `spec.md:104`“重新启用后按自动握手重建会话并更换代际”推出。`IBueNetworkApi` 未变更；SDK 当前契约 2.0 且仅冻结面破坏性变更升 Major（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:46-50`），登记豁免成立。未发现越界或错误实现。

### INFO（具名延期，不计 GAP）
生产适配器仅声明生命周期 seam，`ConnectedPeers` 为空：`HostNetworkTransportAdapter.cs:30-41`、`LmnTransportAdapter.cs:22-31`；生产泵接线明确延期 DEV-V2-18：`BueNetworkRuntime.cs:279-283`。这是工单范围外的已具名延期，不构成本票 GAP；在 DEV-V2-18 前不得宣称生产 transport 自动握手闭合。

## 验收条件核对
| 验收条件 | 结论 | 证据 |
|---|---|---|
| 红测先行四类行为 | 通过 | `red-compile-build.log` 104 错；`red-runtime-transcript.log` 26 红；`red-fixround-transcript.log` 1 条；7 个 `green-BetterUnturnedExperience.*.Tests.log` PASS |
| 握手前 Connected 不触发、pending 不可见 | 通过 | `Program.cs:904-951`；`BueNetworkRuntime.cs:160-165` |
| 生命周期事件锁外 | 通过 | `BueNetworkRuntime.cs:686-704,749-751,799`；`Program.cs:3974-4007` |
| 0 警告、全套 PASS、双轴 CLEAN | 静态通过；本报告仅 Spec 轴 | `fix2-round-build.log:32873-32874`；7 个 green 日志；`identity-rebuild1.txt:1` 与 `identity-rebuild2.txt:1` 均为 `4f303e44…8ca2` |

## Rebuttal
自查后删除以下疑点：re-arm 使用新会话不是偏离（`Program.cs:3052-3064` 明确验证新代际与订阅保留）；响应方 Reject reset→发起方自愈由 `spec.md:104` 推出；契约/SDK 升级疑点被 `IBueNetworkApi` 零变更事实反驳。R2 的 1 INFO 继续保留为具名延期。

## Verdict
CLEAN（0 BLOCKER，0 GAP，0 越界，1 INFO）。
