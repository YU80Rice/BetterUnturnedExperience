# PT-RT05-001B 第 2 轮独立审计报告

## 【需求执行概述】

对 `PT-RT05-001B` 的第 1 轮三项阻断修复进行只读复审，独立复跑主原型与独立 ConsumerProbe 的 Release 构建和行为测试，并核对全部声明哈希。最终判定：**PASS**，可作为 `SCR-RT05-001` 方案 B 的 `PROTOTYPE_ONLY` 选型输入；不等于 LMN 实施、生产安全或三环境运行通过。

## 【源码溯源清单（Traceability Matrix）】

| 原阻断/验收点 | 修复证据 | 判定 |
| --- | --- | --- |
| B-01：mutation/revoke 真实并发 | `Program.cs:137-162` 使用两个事件门和两个 Task，证明 mutation 已准入时 revoke 不提前返回；退出后 callback reuse 不再 mutation | PASS |
| B-02：manager/capability 锁反转 | `ConnectionReceiveContext.TryInvokeIfCurrent` 仅在 monitor 内做 admission 计数，consumer Action 在锁外运行；`CurrentConnectionMap.Disconnect` 在 manager lock 内只 CloseAdmission+remove，锁外 WaitForQuiescence | PASS |
| B-03：精确 current-map 全序 | `SafeCurrentMapOrdering` 覆盖安全顺序；`LateRevokeOrdering` 覆盖 remove→reconnect publication→old callback→late revoke 的污染反例 | PASS |
| revoke-before-dequeue | `Program.cs:37-49` | PASS |
| dequeue-before-revoke | `Program.cs:51-63`；并由真实并发测试补齐 in-flight 情形 | PASS |
| token reuse / 同 SteamID reconnect | `Program.cs:80-108` | PASS |
| opaque public surface | `ConnectionReceiveContext` public sealed、internal ctor、无 revoke；主测试与独立 ConsumerProbe 双重验证 | PASS |
| null/default fail closed | `ConnectionBoundDispatch.TryInvoke` 与 ConsumerProbe | PASS |
| lifecycle owner | `CurrentConnectionMap` 模拟 manager 唯一管理 publication、CloseAdmission、exact lease removal、锁外 quiescence | PASS（原型范围） |
| 最小 LMN API / SourceSet 影响 | 结果报告 §6～§7 明确加法 API、overload ambiguity、涉及文件及 successor SourceSet 义务 | PASS |

## 【并发与线性化复核】

修订实现形成两阶段撤销：

1. callback 在 capability monitor 内检查 `admissionOpen` 并增加 `activeMutations`，随后释放 monitor。
2. 任意 consumer Action 在 capability 与 manager 锁之外执行，避免 consumer 持 capability lock 重入 manager。
3. disconnect 持 manager lock 时只执行非阻塞 `CloseAdmission`，然后移除 exact current mapping；新 callback 从此无法取得 admission。
4. manager lock 释放后才等待 `activeMutations == 0`，因此不存在第 1 轮发现的 manager→capability 等待 consumer、consumer→manager 的锁环。
5. 已在断开线性化点之前取得 admission 的操作允许完成；`Revoke` 返回代表 quiescence。该语义在报告中已明确，未误称为强制中止已运行 Action。

安全 current-map 测试严格执行：publish old → enqueue old → disconnect（CloseAdmission + exact remove）→ publish new → execute old，mutation 为 `0`。反例严格执行：publish old → enqueue old → remove without revoke → publish new → execute old → late revoke，mutation 为 `1`。

## 【独立编译与测试记录】

### 主原型

- 命令：`dotnet build PT-RT05-001B.csproj -c Release`
- 结果：exit `0`，`0 warnings / 0 errors`
- 行为测试：Release EXE，exit `0`
- 结果：`12/12 PASS`

### ConsumerProbe

- 命令：`dotnet build ConsumerProbe/ConsumerProbe.csproj -c Release`
- 结果：exit `0`，`0 warnings / 0 errors`
- 行为测试：Release EXE，exit `0`
- 结果：`1/1 PASS`
- 独立程序集确认：消费者不能通过 public constructor 创建 context、没有 public revoke surface，null context fail closed 且 mutation 为零。

两次构建均使用 `.NET SDK 10.0.400-preview.0.26322.102`，输出包含预览 SDK 的 `NETSDK1057` message，不是 warning。

## 【哈希核对】

| Artifact | 独立实测 SHA-256 | 结果报告 |
| --- | --- | --- |
| `PT-RT05-001B.csproj` | `DE304CFA3561BF74C390F8AF1E985DDBAC3B8C1A3533F7E72A543EC6B4482E8C` | MATCH |
| `Program.cs` | `8E3E7E2D21AC79811C90DA218B2EE8097042B1E5722542A5020BB25918C61F06` | MATCH |
| `ConnectionTokens.cs` | `565760E3997F80B9789AB4B4559F7D57942D62C63C0C84C39713D45D9C76FF08` | MATCH |
| `ConsumerProbe.csproj` | `8D277053ADBD9CE49D053AA9DFC93B622B8CEF902C34E89ADF285CA3C59B38F7` | MATCH |
| ConsumerProbe `Program.cs` | `23875CFD38D21C906CAF9E547324C936D4EC1734829646138C9EE85B7B020CB9` | MATCH |
| Release prototype DLL | `28DA1B9FD901954F505EEEF3414C04B17615FD5F8E2538EB78CAA9A43AEBA598` | MATCH |
| Release ConsumerProbe DLL | `144F9DF3BE19177D8B0549FE4469CC5ED1F4F6C8184308D925B1030981FCEF62` | MATCH |

## 【阻断项】

无。第 1 轮 B-01、B-02、B-03 均已关闭。

## 【非阻断建议与残余风险】

1. 原型证明的是 CLR monitor/owner 顺序的有条件可行性，不证明 LMN 当前代码已经接线；正式实现仍需真实 LMN net472 build、回归与并发压力测试。
2. 已准入的 consumer Action 会延迟完整 revoke；生产实现应限制 Action 时长、禁止无限等待外部 I/O，并增加 quiescence 超时诊断，但不能在超时后错误地把旧 lease 重新开放。
3. legacy handler overload 无法保护消费者自行创建的二次异步队列；BUE 必须使用 connection-bound API，并在每个 mutation 边界复验 context。
4. client-side receive 没有等价的物理 server connection context，不能将本结论外推到客户端接收链；需方案 A frame fence 或单独设计。
5. 实施新 API 后 `BUE-SS-20260824-02` 摘要必然失效，必须发布 successor SourceSet，不得原地改写。

## 【偏离与妥协说明】

无实现修改。只新增本轮审计报告，保留第 1 轮 FAIL 报告以维持完整审计链。

## 【最终结论】

**PASS。** `PT-RT05-001B` 已满足票据定义的最小代码 spike 验收，可作为 SCR 方案 B 的原型级选型证据。该 PASS 不授权修改 LMN、不接受 SCR 契约变更，也不构成 SP/P2P/U3DS 运行或发布 PASS。
