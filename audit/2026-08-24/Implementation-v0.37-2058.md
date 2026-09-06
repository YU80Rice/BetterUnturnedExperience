# PT-RT05-001B 独立审计报告

## 【需求执行概述】

对 `PT-RT05-001B` LMN connection-bound receive token 抛弃式 spike 进行只读源码审计、Release 独立构建、行为测试复跑和 SHA-256 核对。审计结论为 **FAIL**：确定性顺序测试与产物身份可信，但尚未证明真实的 in-flight mutation/revoke 并发线性化，建议的 manager-lock/capability-lock 接线还存在未封闭的锁顺序与重入死锁路径，因此当前不得作为 SCR-RT05-001 方案 B 的选型 PASS。

## 【源码溯源清单（Traceability Matrix）】

| 审计要求 | 证据位置 | 判定 |
| --- | --- | --- |
| revoke-before-dequeue | `Program.cs:32-44` | PASS |
| dequeue-before-revoke | `Program.cs:46-58` | 条件 PASS；仅顺序执行，不覆盖 mutation 持锁期间 revoke 等待 |
| dequeued、尚未进入 token 时 revoke | `Program.cs:60-73` | PASS |
| disconnect→reconnect→old callback→revoke 精确反例 | `Program.cs:105-120` | 条件 PASS；证明错误顺序会 mutation，但模型没有 session current map/disconnect 状态机 |
| 同 SteamID 新连接与 token reuse | `Program.cs:75-103` | PASS |
| opaque surface | `ConnectionTokens.cs:21-79`、`Program.cs:122-130` | 条件 PASS；内部 capability 无 public ctor，但测试没有覆盖建议的 public `ConnectionReceiveContext` surface |
| 线性化点 | `ConnectionTokens.cs:61-78` | 局部 PASS；单 capability monitor 内成立 |
| capability lifecycle owner | 结果报告 §5、§6 | FAIL；建议接线未解决 manager lock 与 capability lock 的锁顺序/重入问题 |
| 最小 LMN API diff / SourceSet | 结果报告 §6、§7 | 文档完整，但实现可行性受上述并发阻断 |

## 【独立构建与行为验证】

- 命令：`dotnet build PT-RT05-001B.csproj -c Release`
- SDK：`10.0.400-preview.0.26322.102`（构建输出含 `NETSDK1057` message，不计 warning）
- 结果：`0 warnings / 0 errors`，exit `0`
- 行为测试：直接执行 Release EXE
- 结果：`passed=7 failed=0`，exit `0`

复核 SHA-256：

| Artifact | 实测 SHA-256 | 与结果报告 |
| --- | --- | --- |
| `PT-RT05-001B.csproj` | `CF19C1B6423166BA0FC866658A498DBC85929C45DE192489AFA946590A41B02E` | MATCH |
| `Program.cs` | `518CBD47E89E62833BFAF5FCFD8EBAF55E3B77031EAE775CDD5D629BC083F0F8` | MATCH |
| `ConnectionTokens.cs` | `395218FEAD066529FEA51A12573B7CE277B5CD583537DD3920A55FB52BC3977F` | MATCH |
| Release DLL | `1440642907A57CF88037422A3ED813840F3FA0CEF83A36C1822E75CF2C563AA0` | MATCH |

## 【阻断项】

### B-01：没有测试 mutation 已取得 capability gate 时 revoke 的真实并发交错

`DequeueBeforeRevoke` 在 mutation 完全返回后才调用 `Revoke`；`InterleavedRevokeWins` 则在 callback 尚未进入 `TryInvoke` 前完成 revoke。两者之间缺少关键 interleaving：callback 已持有 capability monitor 并进入 mutation，另一线程开始 revoke，revoke 必须等待 mutation 退出且返回后旧 callback 永不再进入。当前 `DeterministicDispatcher` 和七项测试均为单线程顺序执行，不能验证票据要求的“callback 先出队、二者交错”完整并发语义，也没有压力/重复测试。

修复建议：用 `ManualResetEventSlim`/`Barrier` 建立可重复的双线程测试，在 mutation 内暂停，断言 revoke 在 mutation 释放前不能返回；随后复用 callback 多次并做并发循环，确认 revoke 返回后的零 mutation 性质。

### B-02：建议的 LMN owner 接线存在 manager-lock → capability-lock 与 callback capability-lock → manager-lock 的潜在死锁

结果报告 §5/§6 要求 `ConnectionSessionManager` 在 manager lock 内 revoke；而 `TryInvoke` 在 capability monitor 内执行任意 consumer mutation。若 mutation 或其同步调用链进入 `ConnectionSessionManager`，锁顺序为 capability → manager；并发 disconnect 则为 manager → capability，并等待 mutation 完成，形成锁反转。结果报告只把“评估重入”列为残余风险，但该路径直接影响建议 API 和 lifecycle owner 的可实施性，应在选型证据前封闭。

修复建议：spike 必须加入 manager/current-map 最小模型和重入测试，并给出无锁反转的明确协议，例如 closing 状态与 publication fence、受控 active-operation 计数，或禁止在 manager lock 内等待任意 consumer mutation；同时证明旧 session revoke 与同 SteamID 新 session publication 的线性顺序。

### B-03：所谓精确 owner-ordering 反例未建模 disconnect/current mapping

`UnsafeOwnerOrdering` 仅创建第二个 lease、执行旧 callback、再 revoke；`ConnectionCapabilityOwner` 没有 current map、disconnect 状态或“新 session 已发布”的可观察断言。因此它证明“未撤销 capability 仍可执行”，但没有独立证明报告所称的 `disconnect→reconnect publication→old callback→revoke` lifecycle 顺序。

修复建议：最小模型加入由唯一 owner 管理的 physical-connection→lease 与 SteamID→current-session mapping，显式执行 disconnect begin、publish reconnect、execute stale callback、revoke，并断言错误顺序污染、正确顺序拒绝。

## 【非阻断建议】

1. `CapabilitySurfaceIsOpaque` 只检查内部实现类型不是值类型且无 public ctor；后续应针对建议公开 API 做独立 consumer assembly 编译测试，验证消费者不能构造或替换有效 context。避免使用“不可 default”这种对引用类型过强的措辞，准确表述为“消费者不能构造有效 capability；null/default 必须 fail closed”。
2. `ConnectionLease.Revoke()` 当前为 public spike seam。报告应强调生产 public receive context 不得暴露 revoke，撤销权只属于 session manager。
3. 不应把 monitor 内执行任意 `Action` 直接带入生产。除死锁外，它还放大断线延迟；后续 API 应约束同步、短时、无阻塞 mutation，或改为明确的 operation lease 协议。
4. SourceSet 影响描述正确：任何 LMN 修改都必须生成 successor，不得原地改写 `BUE-SS-20260824-02`；仍需 net472、真实 LMN 回归、BUE adapter 编译和 SP/P2P/U3DS 同 DLL 运行证据。

## 【偏离与妥协说明】

无实现修改。只新增本独立审计报告；未触碰 spike、LMN 或稳定发布 DLL。

## 【最终结论】

**FAIL。** Release 构建、7/7 现有行为测试和所有已声明哈希均复核通过；但 B-01～B-03 使其尚不足以满足 PT-RT05-001B 的并发模型与 lifecycle owner 验收条件。修复后必须重新构建、复跑并再次独立审计，方可作为 SCR-RT05-001 方案 B 的选型输入。
