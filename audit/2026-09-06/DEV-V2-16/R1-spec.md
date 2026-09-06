# DEV-V2-16 Spec 轴 R1 审查报告（2026-09-06，fresh 实例）

我声明：这是全新的 Spec 轴 R1 审查实例，没有任何先前审查上下文。

## GAP / BLOCKER

### 1. 验收条件①未满足"各语义先红后绿"的证据要求

Spec 明确要求：

> "红测先行：发送结果五值（含 `PartialFailure`）/ established-only 快照 / 外来会话与过期代际被拒 / 无会话 → `NoSession`——先红后绿"

工单原文位于 `.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-16-session-driven-multicast-send-results.md:23`。

实现及测试覆盖面基本完整，但 `red-runtime-transcript.log` 仅记录 10 条失败，其中没有"过期代际被拒"失败项；结单报告明确将其归类为：

> "红轮已绿的回归保持断言"

见 `audit/2026-09-06/DEV-V2-16/DEV-V2-16-closing-report.md:21`。

该归类本身诚实：当前代码确实在停用后清空会话，断言自然保持绿色。但它不能证明该新验收项曾经针对旧行为先红，因此按验收条件的字面要求仍有证据 GAP。相同问题也影响 `Sent` 等既有语义若要求"五值"逐项先红；红 transcript 未记录其失败。

证据文件：`audit/2026-09-06/DEV-V2-16/red-runtime-transcript.log`

### 2. `SendToClient` 的参数检查违反冻结的频道优先级

冻结原文：

> "频道未注册仍优先返回 `ChannelNotRegistered`"

见 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:72-73`。

但实现先处理空 session：

```csharp
if (session == null) return NetworkSendResult.NoSession;
```

随后才检查频道是否注册，见 diff：`audit/2026-09-06/DEV-V2-16/round1-increment.diff:160-167`。

因此对"未注册频道 + null session"输入返回 `NoSession`，而冻结优先序要求 `ChannelNotRegistered`。现有测试只验证了 `SendToClients` 的频道优先级，未钉住 `SendToClient` 该边界。

## Scope / 越界

未发现确凿的 Scope 越界。`SendToServer` 的 established 门控及锁外传输虽未列在 Scope 五条中，但可由规格"所有发送按既有枚举返回"及"发送不持状态锁"直接推出，不计为越界。

## 其余核验

- `Sessions` established-only：实现与 SDK 条目④一致。
- SDK 条目③/④编号及语义：与规格冻结表一致。
- 逐目标发送、四种聚合结果、归属/established/代际校验、无 SteamId 重载、发送不持锁：实现均有对应代码及 green 测试。
- 停用时 `NoSession` 且无新错误码：有 green 断言；但该项属于回归保持，不应冒充本票新语义红证据。
- 7/7 green 日志、构建 0 警告及身份摘要均已留盘；这不能弥补上述 Spec 证据与优先级问题。

**Spec 轴 R1 判词：NOT CLEAN**

- **BLOCKER**：`SendToClient` 未注册频道优先级错误。
- **GAP**：验收条件①要求的过期代际（及可能的既有五值）先红后绿证据不完整。
