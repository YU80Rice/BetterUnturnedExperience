# GPT-SCR-RT05-001：Ready 后消息绑定接收时连接上下文

> 作者：GPT  
> SourceTicket：RT-05  
> InitialSourceSetId：`BUE-SS-20260824-01`  
> DecisionSourceSetId：`BUE-SS-20260824-02`（LMN evidence hashes 经 successor manifest 重验一致）  
> 状态：`accepted-for-RT06-baseline`  
> 约束：本文件不修改冻结契约，也不授权生产实现

| Field | Value |
| --- | --- |
| ChangeRequestId | `SCR-RT05-001` |
| RaisedBy / SourceTicket | GPT / RT-05 |
| AffectedTokens | Ready 后 Contract envelope 或 LMN adapter receive context；`ConnectionHandshakeId`；`0x0101`～`0x0201` 的处理规则 |
| CurrentBaseline | 普通 envelope 只有 `contractMajor/minor`、`messageKind`、`payloadLength`、`payload`；设置/状态 DTO 不携带 `ConnectionGeneration`。`RequestId` 只在当前连接代际内关联与幂等，不是授权。 |
| Evidence | `Routing/NamespacedTransport.cs:424-465`（SHA-256 `5CE80DFC33C6B006231CAD66DB4CFE6AFF4487E1E31862539F565BF1D5FCE8A1`）只捕获 SteamID、payload 与 delegate；`Sessions/ConnectionSessionManager.cs:123-176`（SHA-256 `3DC5AF09B995EE9F65FC26DAFA10597659043EDB7AAB5807998BAAFC6634872A`）断开时 dispose session，但不撤销 dispatcher 中已排队 Action。 |
| Problem | 同一 SteamID 断开并快速重连时，旧 connection 已排队 Action 可在新 session 建立后执行；执行时按 SteamID 查询 current session 无法恢复接收时连接身份，可能把旧设置写误归新连接。 |
| ProposedChange | 裁定 A 为 BUE V1 必需基线：Ready 后 BUE application frame 携带并验证 `ConnectionGeneration + SnapshotId + handshake nonce binding`，mutation gate 在任何 SettingsRuntime 写入前 fail-closed；replay window 按 connection generation 分域、固定容量且切代清理。B 仅作为未来 LMN defense-in-depth，不阻塞 BUE V1，未授权修改 LMN。 |
| CompatibilityImpact | 方案 A 保持 10-byte Contract envelope，但修订 `0x0004` payload 并为 Ready 后网络 payload 增加 52-byte fence prefix；必须协商 `betterunturned.ready-frame-fence.v1`，不支持时只降级网络设置/状态，不影响原版连接或本地功能。精确 schema 见 `RT-06-Joint-Seam-Implementation-Readiness.md §2.1`。方案 B 仍需 LMN API 与 SourceSet 更新，V1 不实施。 |
| TestImpact | 覆盖同 SteamID 断线重连、旧包延迟、旧 Action 已入队、nonce/snapshot mismatch、SP/P2P Host loopback、P2P Client/U3DS remote；断言旧写永不到达 SettingsRuntime mutation。 |
| GPTDecision | `ACCEPT A FOR BUE V1`。A 修复审计阻断后 Release build `0/0`、`10/10 PASS`、第 2 轮独立审计 `PASS`。B 修复并发/锁序阻断后主原型 `12/12 PASS`、ConsumerProbe `1/1 PASS`、第 2 轮审计 `PASS`，但需修改 LMN 与 SourceSet，因此仅保留为后续加固。 |
| GeminiReview | `ACCEPT IN PRINCIPLE`：前端只接受当前 `SessionReady` generation/snapshot 的 settings/status projection；见 `handoffs/RT05-LMN-Boundary-Review.md`。 |
| HumanTrace | 人工授权按并行步骤继续工作与开发实施；A/B 两个 spike 均完成修复循环并通过第 2 轮独立审计。契约精确 wire 字段在 RT-06 实施就绪包中固化，仍需 Gemini 消费复核。 |

## 被拒绝的临时修补

- handler 执行时仅按 SteamID 查询当前 generation；
- 只检查非零 `RequestId`；
- 只依赖 LMN `IsHandshakeComplete`；
- 断开时只清 SettingsRuntime pending map；
- Ready 前缓存写请求并在未来连接代际重放。

