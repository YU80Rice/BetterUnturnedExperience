# DEV-15C：Projection Relay + AwaitingProjection

Type: task
Status: resolved
Owner: GPT（原生投影中继与等待态 Seam）
Required reviewer: Gemini（前端消费与视觉状态复核）
Parent: DEV-15
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Specification: `../spec-DEV-15-better-item-interaction.md`
Dependency: DEV-15B（resolved）

## 目标

实现原生库存模型 callback 栈结束后的只读投影中继，并为 Better Item Interaction 提供 generation/session/fingerprint 守卫的 `AwaitingProjection` 视觉等待 Seam。

## 本票范围

- 原生观察回调只入队，不在 callback 栈内完成 UI 收敛；
- 绑定 `DragGeneration`、`SessionGeneration` 与物品指纹；旧代际/旧容器静默丢弃，指纹失配不得作为 ACK，但允许交给 latest-fact 观察路径；
- 只读快照按序消费，异常与队列溢出 fail-closed；
- 2 秒仅作为视觉预算，到期清除等待遮罩，不推断服务端拒绝、不伪造回滚；
- 迟到但匹配的投影静默刷新原生事实；同代际指纹歧义只刷新最新原生事实，不生成拒绝结论；
- 不新增库存 RPC，不复制库存权威，不修改 Contracts/LMN/Unturned。

## 明确不做

- 不实现真实 Unity/Harmony callback Hook；
- 不把 `onInventoryAdded`/`onInventoryRemoved` 单独解释为提交 ACK；
- 不执行本地乐观库存写入、伪回滚或错误弹窗；
- 不实现 DEV-15D 设置、生命周期隔离或 DEV-15E 三环境证据。

## 预先冻结测试 Seam

- `INativeInventoryProjectionSource`：callback 栈后提交只读快照/观察事件；
- `NativeInventoryProjectionRelay.Pump()`：主线程受控消费队列；
- `AwaitingProjectionController`：开始等待、匹配投影收敛、预算到期视觉清理；
- `InventoryItemFingerprint`：确定性指纹相等性与代际绑定。

## 验收条件

- [ ] TDD Red → Green：回调只入队，外部 Handler 不在 callback 栈执行；
- [ ] 同 Drag/Session/Fingerprint 的投影可收敛；
- [ ] 任一代际或容器失配均静默丢弃；指纹失配不完成 ACK，只能走 latest-fact 观察；
- [ ] 预算到期只移除等待视觉状态，不生成拒绝/回滚；
- [ ] 迟到匹配投影可收敛，歧义投影不产生拒绝结论；
- [ ] 队列容量固定、溢出 fail-closed，Pump 串行化且 consumer 异常后立即失效绑定；
- [ ] Release 0 errors / 0 warnings；ClientUi/Contracts/Core token 扫描通过；
- [ ] 独立 GPT 审计 PASS；
- [ ] Gemini 前端消费复核 ACCEPT（`Gemini-DEV-15C-Projection-Relay-Review.md`），正式关闭本票。

## GPT R1 审计阻断与修复（2026-08-25）

- 修复 Pump 与 Bind/Invalidate 的 TOCTOU：绑定变更与 Pump 串行化。
- 修复并发 Pump 乱序：单一 `pumpSync` 串行消费。
- 修复 consumer 异常继续消费：异常立即 Invalidate 并清空队列。
- 增加 `NativeRevision` 单调过滤，增加 `INativeInventoryProjectionSource` seam。
- 澄清指纹语义：指纹失配不能完成 ACK，但可作为 latest-fact 观察，不解释为拒绝。

## 证据边界

本票通过仅证明纯 C# 投影中继与等待态 Seam；不证明真实 Unturned callback、单人、SteamP2PFriends、U3DS 或发布资格。
