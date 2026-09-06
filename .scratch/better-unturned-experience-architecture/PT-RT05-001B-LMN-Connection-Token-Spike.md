# GPT：PT-RT05-001B LMN connection-bound receive token 最小代码 spike

> 作者：GPT  
> 日期：2026-08-24  
> SourceSet：`BUE-SS-20260824-02`  
> EvidenceClass：`PROTOTYPE_ONLY`  
> 状态：独立审计第 1 轮 FAIL 的三项阻断已修复；等待第 2 轮独立审计，禁止作为生产或 SCR 选型 PASS

## 1. 问题与结论边界

本 spike 回答：LMN 能否让已排队 callback 捕获一个与**物理连接实例**绑定、不可 `default`、不可由消费者构造、且永久撤销的引用能力，并以同一同步域封闭 revoke/dequeue 竞态。

原型结论是**有条件可行**：

- capability 必须是带 `internal` 构造函数的引用对象，不能是 `struct`、普通 `bool`、SteamID 或 generation 数字；
- callback 的 mutation **准入计数**与 `CloseAdmission` 必须在 capability monitor 内线性化，但 consumer Action 必须在 capability 与 manager 锁外执行；
- `ConnectionSessionManager` 必须在 manager lock 内先 `CloseAdmission`、再移除 current map；锁外才可等待 active mutation 归零；
- 已在关闭前取得 admission 的 mutation 可以完成；完整 revoke 等待它退出，返回后旧 callback 永远不能进入 mutation；
- 若 owner 错误地先发布重连、执行旧 callback、最后 revoke，token 无法逆转已发生的 mutation。测试明确保留此反例，不能把 token 描述成自动消除生命周期错误。

这不是 LMN API 可实施性、真实网络并发、BUE 设置链或三环境运行证据。

## 2. 冻结证据与被模拟顺序

| Source | SHA-256 | 使用方式 |
| --- | --- | --- |
| `LaunchMultiplayerNet/Routing/NamespacedTransport.cs` | `5CE80DFC33C6B006231CAD66DB4CFE6AFF4487E1E31862539F565BF1D5FCE8A1` | 确认 payload/delegate/SteamID 被复制并作为 `Action` 排队 |
| `LaunchMultiplayerNet/Core/MainThreadDispatcher.cs` | `B5182181AA72395D65D7F0F08A4E98E068241DCC2AF265D77136CF924F30AE5A` | 模拟网络线程 enqueue、Unity 主线程稍后 dequeue/drain |
| `LaunchMultiplayerNet/Sessions/ConnectionSessionManager.cs` | `3DC5AF09B995EE9F65FC26DAFA10597659043EDB7AAB5807998BAAFC6634872A` | 确认现状是先从 map 移除、释放 manager lock，随后 Dispose |
| `LaunchMultiplayerNet/Sessions/ConnectionSession.cs` | `161A7458DE70FB58EB251C562967B06CA8014D230AE001451F3DA69F8E69DDBF` | 确认 session Dispose 当前没有 receive capability revoke |

`DeterministicDispatcher` 只复刻上述先入队、后出队且队列不自动取消的顺序，不模拟 Steam 或 Unity。

## 3. 公共测试 seam

原型只从以下 seam 验证可观察行为：

- `ConnectionCapabilityOwner.Open(diagnosticSteamId)`：创建新的物理连接 lease；SteamID 仅为诊断字段；
- `ConnectionLease.CaptureReceive(mutation)`：在接收时捕获该 lease 的 opaque capability；
- `ConnectionLease.Revoke()`：永久撤销；
- `DeterministicDispatcher.Enqueue/Dequeue/DrainOne`：重放 LMN dispatcher 顺序；
- mutation counter：唯一安全属性是旧 callback 是否进入 mutation。

测试不读取 capability 内部 `active` 字段，不按 SteamID 查询 current session，也不以 RequestId 代替连接身份。

## 4. TDD 执行记录

### 4.1 Red

首次只有测试与项目文件，尚无 `PtRt05.ConnectionTokens`：

```text
Program.cs(1,7): error CS0246: 未能找到类型或命名空间名“PtRt05”
生成失败
exit=1
```

最小实现后，第七项测试发现隐式构造函数仍是 public metadata surface：

```text
PASS 6
FAIL | default or caller-forged capability cannot be supplied | expected=0, actual=1
RESULT | passed=6 failed=1
exit=1
```

随后只增加显式 `internal ConnectionReceiveCapability()`。

### 4.2 Green

```text
PASS | revoke-before-dequeue rejects the queued mutation
PASS | dequeue-before-revoke completes before revocation
PASS | dequeued callback paused before token entry loses to revoke
PASS | same SteamID reconnect cannot revive the old capability
PASS | revoked token reuse remains rejected
PASS | disconnect-reconnect-old-callback-revoke exposes unsafe owner ordering
PASS | default or caller-forged capability cannot be supplied
PASS | revoke waits for an entered mutation and rejects every later mutation
PASS | safe current-map disconnect closes admission before reconnect publication
PASS | late revoke after reconnect publication leaves an exploitable window
PASS | consumer context exposes no revoke operation
PASS | null receive context rejects mutation fail-closed
RESULT | passed=12 failed=0
```

独立 consumer assembly 另行编译并运行：

```text
PASS | independent consumer cannot construct/revoke; null rejects fail-closed
```

Release build：`0 warnings / 0 errors`。

## 5. 线性化点与生命周期所有者

采用 manager lock 与 capability monitor 的分层、两阶段撤销：

1. callback 在 capability monitor 内检查 `admissionOpen` 并增加 `activeMutations`；这是成功准入的线性化点。
2. callback 立即释放 capability monitor，随后在**所有 manager/capability 锁之外**执行任意 consumer Action；`finally` 中重新持 capability monitor 递减计数。
3. disconnect 在 manager lock 内调用 `CloseAdmission`；它在 capability monitor 内把 admission 永久关闭，这是断开的准入线性化点。随后仍在 manager lock 内删除 exact lease 的 current mapping。
4. disconnect 释放 manager lock 后才调用 `WaitForQuiescence`。因此 consumer Action 即使调用 manager，也不存在 manager→capability 与 capability→manager 的嵌套锁环。
5. reconnect 可在 mapping 被删除后发布；旧 callback 若尚未获得 admission 必定失败，若关闭前已获得 admission，则可以完成，但完整 `Revoke` 返回前必须归零。

锁顺序不是“同时拿两把锁”：manager 临界区允许短暂调用不等待 consumer 的 `CloseAdmission`；consumer 从不持 capability lock 调用 manager；等待 active mutation 永远在 manager lock 外。生命周期 owner 仍唯一是按 `ITransportConnection` 建立 session 的 `ConnectionSessionManager`，consumer context 没有 `Revoke` 方法。

完整时序模型给出两条可执行证据：

- 安全：`publish old → enqueue old → disconnect(CloseAdmission + remove) → publish new → execute old`，旧 mutation 为 `0`；
- 反例：`publish old → enqueue old → remove without revoke → publish new → execute old → late revoke`，旧 mutation 为 `1`。

## 6. 建议的最小 LMN API / internal diff（未实施）

为了让 BUE 在后续异步边界仍能显式复验，推荐**加法式** API，而不是破坏现有 handler：

```diff
+ public sealed class ConnectionReceiveContext
+ {
+     public CSteamID Sender { get; }
+     public bool TryInvokeIfCurrent(Action mutation);
+     internal ConnectionReceiveContext(...);
+ }

+ public static void RegisterServerHandler(
+     string channelId,
+     Action<ConnectionReceiveContext, BinaryReader> handler);
  public static void RegisterServerHandler(
      string channelId,
      Action<CSteamID, BinaryReader> handler); // 保留兼容 overload
```

最小 internal 变化：

1. `ConnectionSession` 创建并唯一持有 opaque receive capability；对外 consumer context 不暴露 revoke。
2. `ModRouter.TryHandleNamespacedFromClient` 从实际 `ITransportConnection` 取得 session，并把该 session 的 receive context 传给 `NamespacedTransport`；不得在 callback 执行时按 SteamID 重查。
3. `NamespacedTransport.InvokeServerHandler` 的 queued Action 捕获 receive context；执行 handler/mutation 前调用 `TryInvokeIfCurrent`。
4. `ConnectionSessionManager.RemoveSession*` 与 `ClearAllSessions` 在 manager lock 内对所有待移除 session执行非阻塞 `CloseAdmission`，再清 mapping；释放 manager lock 后才等待 quiescence 与执行重资源 Dispose。
5. legacy overload 可由 LMN 在 capability gate 内适配，但 legacy 消费者拿不到 context，无法把能力带过自己的二次异步队列；BUE 必须使用新 overload。

公共 API 的 capability 只证明“此 callback 仍属于当前未撤销物理连接”。它不证明认证、授权、BUE Ready、nonce/SnapshotId、频道兼容、玩家权限或 mutation 合法性。

## 7. 兼容与 SourceSet 影响

- 保留旧 overload 时，现有源码消费者可继续编译；新增 overload 对使用无类型 lambda 的调用点可能产生 overload ambiguity，实施时应以不同方法名（如 `RegisterConnectionBoundServerHandler`）规避，或逐调用点编译验证。
- LMN 二进制 API 增加新公开类型/方法；所有消费程序集必须重新编译和做 API/IL 审计。
- 预计至少改动 `ConnectionSession.cs`、`ConnectionSessionManager.cs`、`ModRouter.cs`、`NamespacedTransport.cs` 及 LMN tests/docs。
- 任何 LMN 源码变化都会使 `BUE-SS-20260824-02` 的 44-file ordinal digest 失效，必须生成 successor SourceSet，记录新 base/worktree identity、逐文件 hash和新 ordinal digest；不得原地改写 `-02`。
- 需要新增真实并发测试、LMN net472 build、现有测试回归、BUE adapter 编译、SP/P2P/U3DS 同 DLL 运行证据。

## 8. 原型身份

| Artifact | SHA-256 |
| --- | --- |
| `prototypes/PT-RT05-001B/PT-RT05-001B.csproj` | `DE304CFA3561BF74C390F8AF1E985DDBAC3B8C1A3533F7E72A543EC6B4482E8C` |
| `prototypes/PT-RT05-001B/Program.cs` | `8E3E7E2D21AC79811C90DA218B2EE8097042B1E5722542A5020BB25918C61F06` |
| `prototypes/PT-RT05-001B/ConnectionTokens.cs` | `565760E3997F80B9789AB4B4559F7D57942D62C63C0C84C39713D45D9C76FF08` |
| `prototypes/PT-RT05-001B/ConsumerProbe/ConsumerProbe.csproj` | `8D277053ADBD9CE49D053AA9DFC93B622B8CEF902C34E89ADF285CA3C59B38F7` |
| `prototypes/PT-RT05-001B/ConsumerProbe/Program.cs` | `23875CFD38D21C906CAF9E547324C936D4EC1734829646138C9EE85B7B020CB9` |
| Release prototype DLL | `28DA1B9FD901954F505EEEF3414C04B17615FD5F8E2538EB78CAA9A43AEBA598` |
| Release consumer probe DLL | `144F9DF3BE19177D8B0549FE4469CC5ED1F4F6C8184308D925B1030981FCEF62` |

## 9. 残余风险与审计输入

- 原型包含一个受事件门控的真实 CLR 并发测试，但不是长时压力、弱内存或 Unity 主线程运行证据。
- 已进入的任意 consumer Action 会延迟完整 revoke/quiescence；它不持 capability lock，但生产仍需超时诊断与有界 callback 政策。
- 旧 handler overload 不能保护消费者自行排出的后续异步 work。
- client-side receive 当前没有等价的物理 server connection context，需另行设计或由 BUE frame fence 兜底。
- LMN 当前 `RemoveSession` 的 remove-before-dispose 顺序必须改变；否则测试中的 unsafe owner ordering 反例仍可出现。
- 本报告只允许提交独立审计；审计 PASS 前不得用于正式选择 SCR 方案 B。

## 10. 独立审计轮次

### Round 1：FAIL

阻断项及修复：

1. 缺真实并发：新增 `RevokeWaitsForEnteredMutation`，证明 mutation 已准入时 revoke 等待，返回后复用 callback 不再增加 mutation。
2. 锁反转风险：从“consumer 在 capability lock 内执行”改为 admission counter + 锁外 consumer + manager 锁外 quiescence wait，并在 §5 冻结分层规则。
3. 时序不完整：新增 `CurrentConnectionMap` 安全全序与 `UnsafeLateRevokeConnectionMap` 反例全序。
4. 加固建议：新增独立 `ConsumerProbe`，证明另一程序集无法 public construct/revoke context，且 null context fail-closed。

当前等待 Round 2，未自行宣告 PASS。
