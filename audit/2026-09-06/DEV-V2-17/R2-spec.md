# DEV-V2-17 R2 Spec 轴判词

## 自我声明
我是 DEV-V2-17 R2 的全新上下文 Spec Reviewer；未继承任何前轮实例上下文，独立重读规约、工单、规格、领域语义与 R2 增量，并核对被改文件全文。仅审查遗漏/部分、越界、错实现。

## 第一层：R1 BLOCKER 修复核验
**通过，R1 BLOCKER 删除。** Spec 明确要求“Ack 按 peer + 代际 / nonce 匹配”（`spec.md:123`），工单要求“Ack 按 peer + 代际 / nonce 匹配”（工单 `:17`）。`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:701-716` 的 `HandleAck` 以帧头 `sender` 与 nonce 同时匹配，未使用 payload steamId；`:619-625` 的 `HandleHello`、`:746-762` 的 `HandleReject` 同样以帧头 sender 为 peer 权威。修复红证据 `red-fixround-transcript.log:1-5` 在 R1 代码上对伪造 payload 身份失败，R2 测试 `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:3917-3927` 覆盖后绿。不存在该 BLOCKER。

## 第二层：Scope 终审
实现覆盖自动 Hello/Ack/Reject、pending 不公开、去重、peer+nonce 匹配、断线清理、重连换代际、Reject fail-closed、锁外回调与 1s→8s 退避：`BueNetworkRuntime.cs:619-790, 454-503`；夹具及断言见 `Program.cs:3719-4085`。未发现越界行为。`IBueNetworkApi` 无成员变更；SDK 明确当前契约 2.0 且仅冻结面破坏性变更升 Major（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:46-50`），故登记豁免成立。identity 两轮均为 `2e767be8…ac3a`（`identity-rebuild1.txt:1`、`identity-rebuild2.txt:1`）。

### INFO（具名延期，不计 GAP）
工单/Spec 要求运行时自动握手及超时重探（工单 `:15-16`；`spec.md:121-123`）。但生产适配器仍只声明事件，`ConnectedPeers` 恒为空，泵接线明确标注 DEV-V2-18：`src/BetterUnturnedExperience.Core/Network/HostNetworkTransportAdapter.cs:30-41`、`src/BetterUnturnedExperience.Transport/LmnTransportAdapter.cs:22-31`；退避泵也明确写“production pump wiring lands with DEV-V2-18”（`BueNetworkRuntime.cs:479-486`）。这是已具名延期，非本轮 GAP；DEV-V2-18 前不可宣称生产自动握手闭合。

## 验收条件
| 条件 | 结论 | 证据 |
|---|---|---|
| 红先行四类握手行为 | 通过 | `red-compile-build.log` 编译红；`red-runtime-transcript.log` 桩级 26 红；7 个 green 日志均 PASS |
| 握手前 Connected 不触发、pending 不可见 | 通过 | `Program.cs:882-927`、`BueNetworkRuntime.cs:160-165` |
| 回调锁外 | 通过 | `BueNetworkRuntime.cs:675-689,737-740,788-789`；`Program.cs:3974-4007` |
| 0 警告、全套 PASS、双轴 CLEAN | 静态通过；本报告仅 Spec 轴 | `fix-round-build.log`；7 个 green 日志；identity 两轮一致 |

## Rebuttal 记录
- `aSession→aRearmedSession` 并非偏离：Spec `:104` 要求重建新代际且订阅无需重挂；`Program.cs:3049-3064` 正确保留该语义。
- 响应方复位 Reject→发起方自愈可由 Spec `:104` 的重新启用自动握手重建推出，不越界（`BueNetworkRuntime.cs:175-180,778-789`）。
- 契约/SDK 升级疑点被实现事实反驳：`IBueNetworkApi` 零变更，故删除。

## 最终 verdict
CLEAN（0 BLOCKER，0 GAP，0 越界，1 INFO）。
