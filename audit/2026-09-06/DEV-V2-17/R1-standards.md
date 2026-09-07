# DEV-V2-17 Standards 轴 R1 判词

## 破例声明
本次 Standards 轴由 Spec-Reviewer 类型实例执行。standards-reviewer 类型连续 6 次派发失败（上游基础设施错误：Upstream response stream interrupted），而 Spec-Reviewer 同期多次成功；实施者按预先宣告的兜底路径执行，并将在审计轮次链具名登记此破例。本报告仍执行完整 Standards 检查清单，与 Spec 轴 Scope/GAP 分析无关。

## 自我声明与范围
我是全新上下文实例，无任何前轮上下文；已独立重读 `docs/agents/output-review-loop.md`、工单、相关 CONTEXT/spec 片段、`audit/2026-09-06/DEV-V2-17/round2-increment.diff` 及 6 个被改文件全文。仅审查锁纪律、并发、注释/契约、死代码、speculative 参数、重复过滤源和命名/基线 smell。

## Findings

### BLOCKING-1：锁内调用可重入的外部时钟 seam
- Spec 原文：“Connected/Disconnected 锁外执行。”（`DEV-V2-17...md:18`）
- 证据：`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:492-493, 543, 667` 在持有 `sync` 时调用注入的 `monotonicMilliseconds()`。
- 理由：该 `Func<long>` 是外部可替换代码，若回调重入 runtime 或等待 runtime 锁，会死锁/破坏锁外职责切分；即便当前默认实现纯函数，公开构造参数使该路径可达。时钟读取应先在锁外取得，或改为内部不可重入时钟 seam。

### SMELL-1：订阅移除标志存在无同步读
- Spec 原文：“handlers run outside the state lock”（`ContractTypes.cs:301-309` 的冻结契约注释；对应工单要求生命周期/派发锁外）。
- 证据：`BueNetworkRuntime.cs:329-332` 在锁内写 `record.Removed`，但 `817-820` 在锁外直接读。
- 理由：跨线程取消与派发构成数据竞态；`bool` 没有 volatile/锁外快照同步保证。应以锁内复制“仍有效”的记录，或用 `Volatile.Read/Write`（并保持生命周期语义明确）。

### DEFERRABLE-1：生产 transport 生命周期缝是不可触发的占位实现
- Spec 原文：“transport connected → 运行时发 Hello”（`spec.md:121-123`）。
- 证据：`HostNetworkTransportAdapter.cs:31-41` 与 `LmnTransportAdapter.cs:22-31` 声明事件但只能在类内部触发，且 `ConnectedPeers` 永远返回 `EmptyPeers`；注释却称 production binding 会 raise（分别 `:32-35`、`:23-25`）。
- 理由：当前 adapter 无法让外部绑定触发连接事件，也无法在重新启用时发现已连接 peer，自动握手在真实 seam 上不可运行。代码明确标注 DEV-V2-18，具名延期至 DEV-V2-18；在该票完成前不得宣称生产 transport 已具备自动握手。

## Rebuttal 记录
- 未将 `BUE2` 魔数、功能覆盖或验收完整性列为发现：这些属于 Spec 轴，不属于本次 Standards 轴。
- `EstablishedSnapshot` 的 Sessions/发送共用过滤源已自查确认（`BueNetworkRuntime.cs:250-259`），删除“重复过滤源”候选。
- 所有实际 transport 发送点均位于状态锁外；因此未保留“发送跨锁”候选。

## Final verdict
**NOT CLEAN**（1 BLOCKING、1 SMELL、1 DEFERRABLE）。BLOCKING-1 修复并重新审查后，另行处理具名 DEV-V2-18 延期项。
