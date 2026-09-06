# DEV-V2-16 Standards 轴 R1 报告（2026-09-06，fresh 实例）

**审查者**：Standards 轴 R1 全新实例，无先前审查上下文。
**范围**：`audit/2026-09-06/DEV-V2-16/round1-increment.diff`；对照工单 Scope、`docs/agents/output-review-loop.md`、SDK 冻结登记、同文件 DEV-V2-14 锁/注释惯例。下游票只作越界标注。

## 锁纪律（公开发送路径成立）

`SendToServer` / `SendToClients` / `SendToClient` 均在 `lock (sync)` 内只做频道检查与快照（或拷贝 `PeerSteamId`），`SendFrame`/`SendTargets` 在锁外。`PeerSteamId`/`SessionId` 为构造后 `get`-only，锁外读取安全。

`SendToClient` 用 `TryGetValue` + `ReferenceEquals` + `Established`，身份/代际/已建立谓词都在锁内求值后才放锁。公开发送路径满足「发送不持状态锁」。

**不成立的是 Established 谓词本身的锁覆盖**（见 BLOCKING）。

## 线程安全

- `nextSessionId`：`Interlocked.Increment`，跨实例唯一，无问题。
- 快照—发送间隙：锁外仍向已拷贝的 steam id 发帧。这是不持锁的代价，SDK 未承诺「消失即失败」。内部注释与代码不符，见 SMELL。
- Connected 锁外触发：约定仍在；错误是把公开快照位与事件绑在同一把锁外写入。

## 测试与红绿链

- 无参主路径 `AssertBueV2SessionDrivenSendSemantics()` 默认 `collectAllFailures: false`，`Check` 直接抛，**未污染 fail-fast**。收集模式仅 `--bue-v2-send-semantics-red`。
- `ForeignSessionStub` 属性只读、无静态可变状态。
- 锁探针与 DEV-V2-14 同构（`Task.Run` + 2s/5s），超时窗口宽，**非 flaky 阻断**。
- 红绿证据真实：`red-compile-build.log` CS0117 `PartialFailure`；`red-runtime-transcript.log` 10 条收集红与断言文案逐字对应；`green-stage-build.log` 0 警告 0 错误；7/7 `green-*.Tests.log` PASS。

## 冻结面

SDK ③④ 与实现一致：`PartialFailure=204`、逐会话定向、空快照 `NoSession`、全送达 `Sent`、全失败 `LocalTransportUnavailable`、混合 `PartialFailure`、频道优先、载荷一次性预检。`IBueNetworkApi.Sessions`/`SendTo*` 无对等冻结注释（`Subscribe` 有），见 SMELL。

## 越界（不计本票 BLOCKING）

- `StartSession`/`HandleHello` 锁内 `SendFrame`（Hello/Ack/Reject）→ DEV-V2-17。
- `16*1024`、魔数 `BUE2` → DEV-V2-18。
- 生产传输绑定 → DEV-V2-21/22。
- `IConnectionSession.Send` 仍为 `NoSession` 桩：结单已具名延期。

---

### BLOCKING

1. **公开快照谓词在状态锁外写入（数据竞争）**
   `src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:414-415,436,552,559-568` 与 `108-125,139-145,171-172,196`。
   `HandleAck`/`HandleHello` 释放 `sync` 后 `MarkEstablished()` 才写 `Established=true` 并触发 `Connected`。`Sessions`/`EstablishedSnapshot`/`SendToClient` 在锁内读该位。锁释放不发布随后的写入：`Connected` 已触发时，并发 `Sessions` 仍可为空、`SendToClients` 可 `NoSession`、对已返回的 session 对象 `SendToClient` 可拒。本票把 `Established` 收成公开快照门；「事件锁外」只要求回调锁外，**不要求把状态位带出锁**。修法：锁内置位，锁外再 fire `Connected`。

### SMELL（判断项，不单独阻断）

1. **注释说谎**：同文件 `163-165`「lock is taken twice」实为一次；「vanishes … simply fails that target」实为仍向 `PeerSteamId` 发送，transport 成功则计 `Sent`（`239-251`）。
2. **Speculative Generality**：`SendTargets(..., bool untargeted)`（`235,244,176`）唯一调用 `untargeted: false`。
3. **Duplicated Code**：`Established` 过滤在 `Sessions`（`118-121`）、`SendToServer`（`141-143`）、`EstablishedSnapshot`（`214-216`）三处同形。
4. **冻结注释密度不一致**：`ContractTypes.cs:304-307` 的 `Sessions`/`SendTo*` 无 established-only / 组播聚合说明，SDK ③④ 与 `Subscribe` 长注释不对齐。

### DEFERRABLE

1. 锁探针超时依赖（`Program.cs:3623-3630`），与 DEV-V2-14 同模式。
2. `Sessions` 快照列表未按 `sessions.Count` 预容量（微分配）。
3. 未覆盖 `SendToServer`/`SendToClient` 的锁外探针（结构对称）。

---

Standards 轴 R1 判词：NOT CLEAN
