# GPT PT-RT05-001A：BUE Ready-frame fence 最小代码 spike 结果

> 作者：GPT  
> 日期：2026-08-24  
> SourceSetId：`BUE-SS-20260824-02`  
> 类型：throwaway prototype / `PROTOTYPE_ONLY`  
> 独立审计：第 1 轮 `FAIL`；已修复唯一阻断；第 2 轮 `PENDING`；本文不宣告审计 PASS

## 1. 问题与受控 seam

本 spike 仅回答：不修改 LMN 时，BUE Ready-frame 在 mutation 前绑定 `ConnectionGeneration + SnapshotId + 32-byte handshake nonce`，能否在纯内存对抗测试中拒绝同 SteamID 快速重连前已入队的旧 Action。

公开可观察 seam：

```text
ReadyFrameFence.Handle(frame)
  -> FrameDecision
  -> InMemorySettingsMutationGate.MutationCount
```

handler 调用次数和 mutation 次数分开记录，因此测试能区分“旧 callback 确已执行”与“旧写入到达 SettingsRuntime mutation”。

## 2. TDD 记录

### RED

先创建 8 个公开行为测试，在尚无 fence implementation 时执行：

```text
dotnet run --project prototypes/pt-rt05-001a-bue-frame-fence/PT-RT05-001A.csproj
CS0246: 未能找到类型或命名空间名“Bue”
CS0246: 未能找到类型或命名空间名“FrameDecision”
exit code: 1
```

### GREEN

加入最小 fence 实现后执行：

```text
dotnet run --project prototypes/pt-rt05-001a-bue-frame-fence/PT-RT05-001A.csproj --configuration Release
PASS | old queued action is handled but cannot mutate after reconnect
PASS | generation mismatch is rejected
PASS | snapshot mismatch is rejected
PASS | nonce mismatch is rejected
PASS | combined mismatch is rejected
PASS | out-of-order stale frame is rejected after current frame
PASS | duplicate current request mutates at most once
PASS | request id cannot substitute for connection identity
SUMMARY | total=8 passed=8 failed=0
exit code: 0
```

### 审计修复轮 RED

第 1 轮独立审计发现 replay tracking 永久无界，且仅按 RequestId，会污染后续 `ConnectionGeneration`。先新增跨代复用和容量测试，未实现时编译按预期失败：

```text
CS1729: InMemorySettingsMutationGate 不包含采用 1 个参数的构造函数
CS1061: 未包含 ReplayEntryCount 的定义
CS0117: FrameDecision 未包含 ReplayWindowFull 的定义
exit code: 1
```

### 审计修复轮 GREEN

```text
PASS | old queued action is handled but cannot mutate after reconnect
PASS | generation mismatch is rejected
PASS | snapshot mismatch is rejected
PASS | nonce mismatch is rejected
PASS | combined mismatch is rejected
PASS | out-of-order stale frame is rejected after current frame
PASS | duplicate current request mutates at most once
PASS | same request id is legal in a new connection generation
PASS | replay window is strictly bounded and fails closed
PASS | request id cannot substitute for connection identity
SUMMARY | total=10 passed=10 failed=0
exit code: 0
```

Release 构建：

```text
dotnet build prototypes/pt-rt05-001a-bue-frame-fence/PT-RT05-001A.csproj --configuration Release --no-restore
0 warnings, 0 errors
```

## 3. 覆盖矩阵

| 行为 | 观察结果 |
| --- | --- |
| 旧 Action 已入队，断线后同一逻辑用户新 Ready，最后执行旧 Action | handler `1`，mutation `0`，`StaleGeneration` |
| 仅 generation mismatch | mutation `0`，`StaleGeneration` |
| 仅 SnapshotId mismatch | mutation `0`，`StaleSnapshot` |
| 仅 nonce mismatch | mutation `0`，`NonceMismatch` |
| 三者组合 mismatch | mutation `0`，在首个 fence 处拒绝 |
| 当前帧先执行，旧帧后到 | 当前 mutation `1`，旧帧拒绝 |
| 当前连接重复同 RequestId/同 payload | 第一次 `Applied`，第二次 `Duplicate`，mutation 总数 `1` |
| 新 `ConnectionGeneration` 复用旧 RequestId | 代际切换先清理旧 replay 域，新代请求 `Applied`，仅保留当前代条目 |
| 同代请求超过 replay 容量 | 已知重复仍返回 `Duplicate`；新唯一请求返回 `ReplayWindowFull`，条目数不增长 |
| 旧帧使用与新帧相同 RequestId | 仍因 generation mismatch 拒绝，RequestId 不是连接身份 |

## 4. 原型身份

| 文件 | SHA-256 |
| --- | --- |
| `prototypes/pt-rt05-001a-bue-frame-fence/PT-RT05-001A.csproj` | `CF19C1B6423166BA0FC866658A498DBC85929C45DE192489AFA946590A41B02E` |
| `prototypes/pt-rt05-001a-bue-frame-fence/Program.cs` | `1A86B8C8420EC564A263CBD83E42431317C19B3D2BFD6F889EC7F40A88EE96EB` |
| `prototypes/pt-rt05-001a-bue-frame-fence/ReadyFrameFence.cs` | `C3E580D56BD69E0923C2C7BCF6AA7D3BCDEFD487D3E68B7FFD8F18A758513468` |
| Release prototype DLL | `2769FD0D75A2C440905D77CDDBDB251A8F384C1C5F458F3901757B7D66147F3D` |

## 5. 限定结论

在本纯内存模型中，方案 A 的三重绑定能确定性地保证：旧 Action 即使已进入 dispatcher 且之后真正调用 handler，也会在 mutation gate 之前因旧连接上下文被拒绝。这是方案 A 的 safety-property 证据，不是生产可行性、协议冻结或发布证据。

## 6. 残余风险与后续义务

- 本模型未接入 LMN，不证明真实 payload 在 receive-time 能完整、不受篡改地携带三重绑定。
- nonce 来自密码安全 RNG 并做 fixed-time comparison，但没有密码学认证/MAC；本 spike 不证明防伪造、防重放或玩家授权。
- `ulong` generation/SnapshotId 的分配、持久化、溢出和进程重启语义未由本 spike 解决。
- 内存模型为单线程顺序执行；生产尚需证明 receive queue、binding replacement 和 game-thread mutation 的线性化边界。
- 本 spike 已以 generation-scoped 严格有界窗口修复内存与跨代污染；满载后选择 fail-closed，因此长连接高频写入会有可用性上限。生产需在不重开重放窗口的前提下冻结容量、窗口轮换或请求序列规则。
- 同 RequestId 不同 payload 已返回 `RequestIdConflict`，但完整终态投影仍属生产设计义务。
- 本产物必须先经独立审计 PASS，再与 PT-RT05-001B 比较；当前不得单独用于接受 `SCR-RT05-001`。
