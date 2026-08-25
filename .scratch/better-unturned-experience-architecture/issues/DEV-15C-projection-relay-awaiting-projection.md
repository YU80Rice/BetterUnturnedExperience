# DEV-15C：Projection Relay + AwaitingProjection

Type: task
Status: claimed
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
- 绑定 `DragGeneration`、`SessionGeneration` 与物品指纹，旧代际/旧容器/旧指纹静默丢弃；
- 只读快照按序消费，异常与队列溢出 fail-closed；
- 2 秒仅作为视觉预算，到期清除等待遮罩，不推断服务端拒绝、不伪造回滚；
- 迟到但匹配的投影静默刷新原生事实；歧义投影只刷新最新原生事实，不生成拒绝结论；
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
- [ ] 任一代际、容器或指纹失配均静默丢弃；
- [ ] 预算到期只移除等待视觉状态，不生成拒绝/回滚；
- [ ] 迟到匹配投影可收敛，歧义投影不产生拒绝结论；
- [ ] 队列容量固定、溢出 fail-closed，Pump 不产生未界定异常；
- [ ] Release 0 errors / 0 warnings；ClientUi/Contracts/Core token 扫描通过；
- [ ] 独立 GPT 审计 PASS；
- [ ] Gemini 前端消费复核 ACCEPT 后正式关闭本票。

## 证据边界

本票通过仅证明纯 C# 投影中继与等待态 Seam；不证明真实 Unturned callback、单人、SteamP2PFriends、U3DS 或发布资格。
