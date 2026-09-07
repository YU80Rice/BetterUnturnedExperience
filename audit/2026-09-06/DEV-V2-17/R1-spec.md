# DEV-V2-17 R1 Spec 轴判词

## 自我声明与范围
我是 DEV-V2-17 R1 的全新上下文 Spec-Reviewer；未继承任何前轮 reviewer 上下文或结论。仅审查 `audit/2026-09-06/DEV-V2-17/round1-increment.diff` 对工单、规格及验收条件的忠实度，范围为遗漏/部分实现、越界、错实现；不审查格式或命名。

审查对象：`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs`、`LocalLoopbackTransport.cs`、`HostNetworkTransportAdapter.cs`、`src/BetterUnturnedExperience.Transport/LmnTransportAdapter.cs`、`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`、`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`，及 R1 红绿证据。

## 验收条件逐条核对
| AC | 核对结论 | 证据 |
|---|---|---|
| 红测先行：全流程、去重、peer+代际/nonce、断线重连、Reject fail-closed | 通过 | `red-compile-build.log` 末尾 52 errors；`red-runtime-transcript.log` 记录 26 条红；`Program.cs:3690-4084` 覆盖各组；`green-BetterUnturnedExperience.Plugin.Tests.log:1` PASS。 |
| 握手前 Connected 不触发、pending 不可见 | 通过 | `Program.cs:3717-3763`；运行时 `BueNetworkRuntime.cs:157-163, 689-735`。 |
| 生命周期回调锁外执行 | 通过 | `BueNetworkRuntime.cs:668-687, 732-735, 779-780, 924-932`；`Program.cs:3963-3997` 锁外断言。 |
| 0 警告构建、全套 PASS、双轴 CLEAN | 静态部分通过；双轴非本报告范围 | `green-stage-build.log:32770-32772` 为 0/0；7 个 green 测试日志均 PASS。 |

## 发现

### INFO（具名延期，不计 GAP）—生产生命周期泵接线留至 DEV-V2-18
工单明确要求“transport connected → 运行时发 Hello”及断线清理（工单第 15-16 行；规格第 121-123 行）。但 `HostNetworkTransportAdapter.cs:31-41` 与 `LmnTransportAdapter.cs:23-31` 仅声明事件且 `ConnectedPeers` 恒为空，事件由注释标为 DEV-V2-18 接线；因此真实生产 transport 在本票增量中不会触发自动握手/重启用重建。该缺口已明确具名为 DEV-V2-18，且本票以假 transport 验证 seam（`Program.cs:4086-4160`），故记为可追踪延期 INFO，不另计 GAP；DEV-V2-18 交付前不能宣称生产自动握手完成。

### BLOCKER —Ack 的 peer 身份取自可伪造 payload，未校验 transport sender
规格要求“Ack 按 peer + 代际 / nonce 匹配”（`.scratch/.../spec.md:123`；工单第 17 行）。`BueNetworkRuntime.cs:571-609` 解析帧头 `sender` 却不传入控制处理；`HandleAck` 在 `:689-710` 仅用 Ack payload 的 `peerSteamId` 与 nonce 匹配。因此任意可到达的 Ack 帧可声明目标 peer 并在已知 nonce 时建立该 pending 会话；实现没有按实际 transport 来源校验 peer。红测 `Program.cs:3794-3827` 只覆盖 nonce 不符的误投，未覆盖“payload peer 与帧 sender 不符”。这不满足 peer 绑定的黑盒语义，且会造成错误会话建立，必须修复并补测。

## Rebuttal 记录
- **“响应方复位 Reject → 发起方自愈”越界？** 撤回该疑点。规格明确要求停用后重新启用按自动握手重建并更换代际（`.scratch/.../spec.md:104`）；在响应方无会话而发起方仍持旧会话时，`BueNetworkRuntime.cs:767-780` 的复位与自愈是该语义在非对称角色下的必要实现，不是额外产品面。
- **`aSession` 改为 `aRearmedSession` 是否偏离 DEV-V2-14？** 撤回该疑点。规格第 104 行要求重建新代际且“已挂订阅无需重挂”；`Program.cs:3049-3064` 正确验证新会话发送并保留原订阅，忠实保留原测试意图。
- **契约/SDK 是否需升级登记？** 撤回该疑点。本 diff 对 `IBueNetworkApi` 无成员变更，仅在 `ContractTypes.cs:265-280` 增加注释；SDK 文档规定的破坏性契约升级门槛未触发，identity 两次重建一致（`identity-rebuild1.txt:1-2`、`identity-rebuild2.txt:1-2`）。

## 最终 verdict
NOT CLEAN（1 BLOCKER；INFO 1）。
