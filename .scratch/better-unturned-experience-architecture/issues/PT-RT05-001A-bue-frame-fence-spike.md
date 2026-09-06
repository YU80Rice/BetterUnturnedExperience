# PT-RT05-001A：BUE Ready-frame fence 最小代码 spike

**Owner:** GPT  
**Status:** resolved  
**SourceSet:** `BUE-SS-20260824-02`  
**Type:** throwaway prototype；禁止进入生产程序集

## Question

在不修改 LMN 的前提下，BUE application envelope 携带 `ConnectionGeneration + SnapshotId + handshake nonce binding`，能否确定性阻止同 SteamID 快速重连后的旧排队 Action 到达 SettingsRuntime mutation？

## Acceptance

- 使用纯内存 fake transport/dispatcher，不引用生产 UI 或写盘。
- 覆盖旧 Action 已入队、断线、新连接 Ready、旧 Action 后执行。
- 覆盖 generation、SnapshotId、nonce 各自 mismatch、组合 mismatch、乱序和重复。
- nonce 使用不可预测随机输入；测试不把 RequestId 当连接身份。
- 明确 handler 可以被调用，但 mutation gate 必须拒绝旧帧。
- 输出执行日志、原型 hash、结论与残余风险。
- 独立审计 PASS 后才可作为 SCR 选型证据。

## Non-goals

- 不实现生产 BUE 网络层。
- 不证明加密认证、玩家授权或库存权威。

## Execution record

> 执行者：GPT  
> 执行日期：2026-08-24  
> 证据等级：`PROTOTYPE_ONLY`  
> 独立审计：第 1 轮 `FAIL`（replay tracking 无界且跨代污染）；修复后第 2 轮 `PASS`

- [x] 纯内存 fake dispatcher 与 mutation gate，无 LMN/UI/写盘引用。
- [x] 覆盖“旧 Action 入队 → 断线 → 新 Ready → 旧 Action 执行”；handler 执行 1 次，mutation 为 0。
- [x] 分别覆盖 generation、SnapshotId、nonce 不匹配与组合不匹配。
- [x] 覆盖跨代乱序和当前代重复 RequestId。
- [x] nonce 由 `RandomNumberGenerator.GetBytes(32)` 生成，且使用 fixed-time comparison。
- [x] 同 RequestId 的旧连接帧仍被 generation fence 拒绝；RequestId 未被当作连接身份。
- [x] replay tracking 按 `ConnectionGeneration` 分域；代际切换清理旧域，同 RequestId 可在新代际合法复用。
- [x] replay window 容量严格有界（默认 128，测试容量 2）；满载后对新唯一请求 `ReplayWindowFull` fail-closed，不淘汰旧记录制造重放窗口。
- [x] Release 构建 `0 warnings / 0 errors`，修复后行为测试 `10/10 PASS`。
- [x] 结果、哈希、RED/GREEN 日志与残余风险已写入 `PT-RT05-001A-BUE-Frame-Fence-Spike-Result.md`。
- [x] 独立审计第 1 轮：`FAIL`；唯一阻断为 replay tracking 永久无界且仅按 RequestId。
- [x] 第 1 轮阻断已按 TDD 修复，新增跨代 RequestId 复用、同代重复和超容量 fail-closed 测试。
- [x] 独立审计第 2 轮 `PASS`：Release build `0 warnings / 0 errors`，`10/10 PASS`，新哈希全部匹配。
- [x] 审计记录：`../../../../audit/2026-08-24/Implementation-PT-RT05-001A-2103.md`。本结论仅为 `PROTOTYPE_ONLY` 选型证据。

