# PT-RT05-001A 独立审计报告

> 审计者：独立审计 Agent  
> 日期：2026-08-24 20:58（Asia/Shanghai）  
> 审计方式：只读源码审查、Release 独立构建、行为测试复跑、SHA-256 复核  
> 判定：**FAIL（第 1 轮，1 个阻断项）**

## 一、审计范围

- `issues/PT-RT05-001A-bue-frame-fence-spike.md`
- `change-requests/SCR-RT05-001-receive-time-connection-context.md`
- `PT-RT05-001A-BUE-Frame-Fence-Spike-Result.md`
- `prototypes/pt-rt05-001a-bue-frame-fence/` 下全部手写源码与工程文件

本审计未修改原型实现。

## 二、独立复跑结果

执行：

```text
dotnet build PT-RT05-001A.csproj --configuration Release --no-restore
dotnet run --project PT-RT05-001A.csproj --configuration Release --no-build
```

结果：

- Release build：`0 warnings / 0 errors`；SDK 输出预览版支持策略提示 `NETSDK1057`，不计作编译警告。
- 行为测试：`8/8 PASS`。
- 已确认旧 Action 会执行 handler，但 generation fence 在 mutation gate 前拒绝，测试观察为 handler `1`、mutation `0`。

哈希复核：

| Artifact | SHA-256 | Result |
| --- | --- | --- |
| `PT-RT05-001A.csproj` | `CF19C1B6423166BA0FC866658A498DBC85929C45DE192489AFA946590A41B02E` | MATCH |
| `Program.cs` | `BB0762455F11B1200CD6B004FF5933FF38EB7097E6A7DA62DB8B17FAC65D8C3E` | MATCH |
| `ReadyFrameFence.cs` | `5BDF10FDD749226BC3AAE318E3968CBE545A98D5D6DF5BBB1C4A6A57A6F2BBC4` | MATCH |
| Release DLL | `CD14ABF2FA534D73EED43BF3EAF2AAD3DE5417285C10FB171FF738135D912454` | MATCH |

## 三、阻断项

### B-01：replay 状态没有按 ConnectionGeneration 隔离或在 binding 替换时清理

- 涉及位置：
  - `ReadyFrameFence.cs:106-107`：`ReplaceBinding` 只替换 `currentBinding`。
  - `ReadyFrameFence.cs:133-150`：`acceptedRequests` 是永久、无界、仅以 `RequestId` 为键的字典。
  - `Program.cs:84-95`：所谓“RequestId 不能替代连接身份”测试只覆盖“新帧先写、旧帧后到并被 generation fence 拒绝”，没有覆盖“旧代已接受 RequestId，随后新代合法复用相同 RequestId”。
- 根因：replay ownership 没有绑定 `ConnectionGeneration`，binding 代际切换也没有轮换/清空 replay window。冻结基线规定 `RequestId` 只在当前 connection generation 内关联与幂等；当前实现会让上一代已经接受的 RequestId 污染下一代。
- 可复现影响：旧代先接受 `RequestId=88` 后切换到新 binding；新代合法提交同一 `RequestId=88` 时，会被全局字典误判为 `Duplicate` 或 `RequestIdConflict`。这不是旧帧穿透写入，但违反原型 Acceptance 中“测试不把 RequestId 当连接身份”的语义，也使 replay map 无界增长。
- 修复建议：让 replay window 明确归属于当前 Ready binding/ConnectionGeneration，并在 binding 替换或失效时原子轮换；增加容量上限和确定性淘汰策略。至少新增：
  1. 旧代已 `Applied` 后，新代相同 RequestId 可作为新请求 `Applied`；
  2. 同一当前代、相同 RequestId+同 payload 为 `Duplicate`；
  3. 同一当前代、相同 RequestId+不同 payload 为 `RequestIdConflict`；
  4. 超过 replay window 容量时行为有界且不允许旧代重新污染新代。

在此修复并重新独立审计前，PT-RT05-001A 不应作为 SCR 方案 A 的已审计选型证据。

## 四、已通过维度

| Dimension | Result | Evidence |
| --- | --- | --- |
| 旧 Action 跨重连 | PASS | handler 被调用，generation mismatch 在 mutation 前拒绝 |
| generation / SnapshotId / nonce 单项 mismatch | PASS | 三个独立行为测试均拒绝 |
| 组合 mismatch | PASS | 按首个 generation fence 确定性拒绝 |
| 乱序 | PASS | 当前帧应用后，旧代迟到帧拒绝 |
| mutation gate 顺序 | PASS | 三重 binding 检查先于 `TryMutate` |
| nonce 输入与比较 | PASS | 32-byte CSPRNG，fixed-time compare |
| nonce 语义边界 | PASS | 报告明确 nonce 不是 MAC、认证、授权或防伪证明 |
| 单线程边界 | PASS（限定） | 原型为单线程顺序模型，报告正确保留生产线性化义务 |
| 生产边界 | PASS | 纯内存、无 LMN/UI/写盘依赖，明确 `PROTOTYPE_ONLY` |

## 五、非阻断建议

1. 增加 `NoReadyBinding` 行为测试，以及 disconnect 时显式 invalidation seam，避免未来实现把“尚未 Ready”与旧 binding 混淆。
2. decoder 层后续应拒绝 `RequestId == 0`；本 spike 没有 decoder，因此本轮不列阻断。
3. 后续 PT-RT05-001B 比较时，应分别衡量安全性、可用性、内存上界和真实 LMN receive-time 身份可获得性，不能仅比较 8 个 happy/adversarial case 的通过数。

## 六、最终结论

三重 frame fence 的核心 stale-action safety property 在现有单线程内存模型中成立，且构建、8 项既有测试和哈希均可复现；但 replay 状态跨连接代际泄漏，违反 RequestId 的代际作用域并且无界。最终判定 **FAIL（第 1 轮）**。

