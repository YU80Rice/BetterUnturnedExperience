# PT-RT05-001A 第 2 轮独立审计报告

> 审计者：独立审计 Agent  
> 日期：2026-08-24 21:03（Asia/Shanghai）  
> 前轮报告：`Implementation-PT-RT05-001A-2058.md`  
> 判定：**PASS**

## 一、重审目标

只读确认第 1 轮唯一阻断项已修复：replay tracking 必须按 `ConnectionGeneration` 隔离、严格有界，跨代相同 RequestId 合法，同代重复保持幂等，容量满时 fail-closed 且不继续增长。本轮未修改原型实现。

## 二、源码审查结论

| Requirement | Result | Evidence |
| --- | --- | --- |
| generation-scoped replay window | PASS | `ReplaceBinding` 先调用 `RotateToGeneration`；新 generation 清空旧 replay entries，随后替换 current binding |
| 跨代同 RequestId 合法 | PASS | 旧代 `RequestId=55` Applied；切换 generation 后新代相同 ID 也 Applied，当前 replay entry 仅 1 |
| 同代重复幂等 | PASS | 相同 RequestId+payload 返回 Duplicate；mutation 仍为 1 |
| 同代冲突语义 | PASS | digest 不同返回 RequestIdConflict；逻辑顺序位于容量检查之前 |
| 容量上界 | PASS | constructor 强制 capacity > 0；新增请求在 count 达上限时返回 ReplayWindowFull，字典不增长 |
| 满载后已知重复 | PASS | duplicate/conflict 查询先于 capacity gate，已记录终态仍可重放 |
| stale generation 双重防御 | PASS | fence 在 mutation 前拒绝；mutation gate 也独立核对 active generation |
| 单线程原型边界 | PASS（限定） | 原型继续明确为单线程顺序模型；生产线性化仍列为后续义务 |

第 1 轮指出的全局、永久且无界 `Dictionary<RequestId,...>` 已改为“当前 active generation 的严格有界 replay window”。未发现阻断性回归。

## 三、独立构建与测试

```text
dotnet build PT-RT05-001A.csproj --configuration Release --no-restore
0 warnings, 0 errors

dotnet run --project PT-RT05-001A.csproj --configuration Release --no-build
SUMMARY | total=10 passed=10 failed=0
```

SDK 输出 `NETSDK1057` 预览版支持策略 message，不是编译 warning。

新增关键测试均通过：

- `same request id is legal in a new connection generation`
- `replay window is strictly bounded and fails closed`

原有 stale Action、三个单项 mismatch、组合 mismatch、乱序、同代重复及 RequestId 非身份测试全部回归通过。

## 四、哈希复核

| Artifact | SHA-256 | Result |
| --- | --- | --- |
| `PT-RT05-001A.csproj` | `CF19C1B6423166BA0FC866658A498DBC85929C45DE192489AFA946590A41B02E` | MATCH |
| `Program.cs` | `1A86B8C8420EC564A263CBD83E42431317C19B3D2BFD6F889EC7F40A88EE96EB` | MATCH |
| `ReadyFrameFence.cs` | `C3E580D56BD69E0923C2C7BCF6AA7D3BCDEFD487D3E68B7FFD8F18A758513468` | MATCH |
| Release DLL | `2769FD0D75A2C440905D77CDDBDB251A8F384C1C5F458F3901757B7D66147F3D` | MATCH |

## 五、非阻断残余风险

1. 严格容量满后 fail-closed 会限制长连接的唯一写请求数量；窗口轮换/序列规则属于生产设计，不影响本 spike 的有限 safety-property。
2. `ReplaceBinding` 与 `Handle` 未做并发同步；本原型有意限定单线程，生产仍须在 game-thread gate 上证明线性化。
3. nonce 不是 MAC、认证、授权或防伪机制；报告已正确保留该边界。
4. 仍建议增加显式 disconnect/no-ready invalidation 测试，但不阻断本票所问的“旧 Action 在新 Ready 后执行”结论。

## 六、最终结论

第 1 轮唯一阻断项已完整修复，新增测试直接覆盖跨代 RequestId 复用和严格容量上界，Release 构建、10 项测试及全部新哈希均可独立复现。判定 **PASS**。

该 PASS 只使 PT-RT05-001A 具备作为 SCR 方案比较证据的资格；不等于接受 `SCR-RT05-001`、不证明生产实现、运行环境或发布通过。
