# DEV-V2-16 Standards 轴 R2 复审报告（2026-09-06，fresh 实例）

**审查者声明**：本实例无任何先前审查上下文，未读取 R1 报告，不重构 R1 分析。结论仅来自当前 diff、工单 Scope、生产全文与证据日志的静态推演。

**审查对象**：`audit/2026-09-06/DEV-V2-16/round2-increment.diff`；生产全文 `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs`、`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`；测试 `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`；工单 DEV-V2-16；证据 `red-compile-build.log` / `red-runtime-transcript.log` / `red-fix-round-transcript.log` / `fix-round-build.log` / `green-*.Tests.log`×7。

## 特别核验（当前代码本身）

**Established 锁内发布 / Connected 锁外 / DEV-V2-14 停用重检**

`HandleHello`（`BueNetworkRuntime.cs:381-407`）与 `HandleAck`（`:414-431`）均在 `lock (sync)` 内先重检 `moduleActive`，再写入会话表并调用 `MarkEstablishedLocked` / `EstablishWithContractLocked`。`Established` setter 为 `private set`（`:547`），仅这两条 `internal` 路径可写。`FireConnected`（`:407`、`:431`、`:562-566`）在锁外，只读事件副本后调用，不回写状态。DEV-V2-14 停用重检语义未被破坏。

**Sessions 协变返回类型安全**

`Sessions`（`:108-114`）声明 `IReadOnlyList<IConnectionSession>`，实现返回 `List<BueNetworkSession>`。`BueNetworkSession : IConnectionSession`，`IReadOnlyList<out T>` 协变，C# 赋值合法。快照是新建 list，调用方拿不到字典本身；`IReadOnlyList<T>` 无 `Add`。无类型安全问题。

**快照预容量 / SendTargets 无 speculative 残留**

`EstablishedSnapshot`（`:200-208`）`new List<BueNetworkSession>(sessions.Count)` 已预容量，且是 Sessions / SendToClients / SendToServer 的单一过滤源。`SendTargets`（`:224`）签名为 `(byte kind, string channelId, byte[] payload, bool reliable, List<BueNetworkSession> targets)`，无 speculative 参数；`kind` 实参恒为 `KindData`，属发送帧种类而非投机开关。

## 锁纪律 / 线程安全

- 三条公开发送路径：锁内只做频道门 + 快照/身份校验，transport 调用在锁外（`:122-196`、`:224-245`）。符合 Scope「发送不持状态锁」。
- 快照后会话消失仍尝试发送：注释与实现一致（`:148-150`），属冻结语义，不是竞态缺陷。
- `PeerSteamId` 为 init-only（`:538`），锁外读安全。
- `Established` 为普通 `bool`，所有读写均在 `sync` 下，无锁外发布窗口。
- `StartSession` / `HandleHello` 仍在锁内发 Hello/Ack（`:330`、`:394`、`:402`）。这是握手控制帧，不在本票 Scope 的 Data 发送路径。标为越界/既有形态，不计入 BLOCKING。

## Smell 轴（Mysterious Name / Duplicated Code / Feature Envy / Data Clumps）

- 命名：`EstablishedSnapshot`、`IsEncapsulatable`、`SendTargets`、`MarkEstablishedLocked`、`EstablishWithContractLocked`、`FireConnected` 意图清楚。无 Mysterious Name。
- 过滤逻辑已收敛到 `EstablishedSnapshot` 一处。SendToServer 用 `.Count > 0` 会多分配一个 list，是单源复用的代价，不是三处复制。无实质 Duplicated Code。
- 发送路径读自身 `sessions`/`channels`/`sync`，经 `SendFrame` 调 transport。无 Feature Envy。
- `SendTargets` 五参数对应帧字段 + 目标快照，不是总是一起传递的独立概念团。无新 Data Clumps。

契约面冻结注释已在 `ContractTypes.cs:227-228, 304-322` 与 SDK 登记条目③④ 对齐。注释与代码一致：SendToClient 频道门先于 null；聚合表与实现一致。

## 测试质量与红绿链

红 0：`red-compile-build.log` CS0117 `NetworkSendResult` 无 `PartialFailure`。
红 1：`red-runtime-transcript.log` 收集 10 条（快照/NoSession/pending 拒/逐一定向/PartialFailure/全失败/伪造身份/锁自由）。
修复轮红：`red-fix-round-transcript.log` 1 条（SendToClient 未注册频道须先于 null 检查）。
绿：`fix-round-build.log` / `green-stage-build.log`「0 个警告 / 0 个错误」；`green-*.Tests.log`×7 全部 PASS。Plugin 套件走 `AssertBueV2SessionDrivenSendSemantics()`（fail-fast）。

覆盖面：established-only、五值聚合、身份/代际/pending 拒、ChannelNotRegistered 优先（含 null）、DEV-V2-14 停用仍 `NoSession`、超限载荷不并入聚合、SendToClients 锁外探针。与验收条件对齐。

## 分级清单

### BLOCKING
无。

### SMELL
无硬违规。下列为判断型、不阻断：

1. **DEFERRABLE** — `tests/.../Program.cs:3612,3631,3634,3637`：锁探针依赖 5s/2s 超时。失败会红，但在调度极差环境下可能假红。本票已用该探针证明 Data 发送不持锁；强化为确定性握手不在 Scope。
2. **DEFERRABLE** — `BueNetworkRuntime.cs:122-196` / 测试 `:3596-3638`：锁外探针只打 `SendToClients`，未覆盖 `SendToServer`/`SendToClient`。三条路径结构对称（锁内快照/校验，锁外 `SendFrame`），缺探针不是锁纪律违反。
3. **DEFERRABLE** — `BueNetworkRuntime.cs:128`：`SendToServer` 为取「是否有 established」仍物化整表。正确且预容量，热路径可改为短路扫描，属微优化。

### 越界（不计入本票 BLOCKING）

- 握手 Hello/Ack/Reject 仍在 `lock (sync)` 内 `SendFrame`（`:330,394,402`）。Scope 冻结的是功能模块 Data 发送。自动握手/Ack 匹配属 DEV-V2-17。
- 魔数 `16 * 1024` / `"BUE2"` 属 DEV-V2-18。
- 生产接线 / `IConnectionSession.Send` 桩属 DEV-V2-21/22 与结单具名延期。
- 同 peer 多 established 不去重：结单延期 5，DEV-V2-17。

---

Standards 轴 R2 判词：CLEAN
