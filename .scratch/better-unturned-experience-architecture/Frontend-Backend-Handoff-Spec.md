# GPT-Frontend-Backend-Handoff-Spec：前后端交接、失败与超时表现

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-13 决策基线；实现与运行未验证**  
**输入确认: 人工开发者于 2026-08-24 全部接受七项政策**

## 1. 目标与边界

本规范冻结后端/共享层向 Gemini 前端提供的稳定投影、状态顺序、超时语义和玩家可见降级规则。它跨越两个范围：

- 公共框架：模块生命周期、设置事务、网络协商、错误展示与通知去重。
- “更好的物品交互”：候选预览、原生库存提交后的 `AwaitingProjection`、投影关联和迟到结果。

V1 不为原生库存移动另造成功/拒绝网络协议。最终物品移动继续使用 Unturned 原生 `sendDragItem → ReceiveDragItem` 权威链路；插件只观察原生库存投影并管理增强 UI。

## 2. 前端只消费稳定只读投影

前端不得直接读取后端可变实体、持久化对象、LMN 原始封包或异常对象。允许消费的稳定输入为：

| 输入 | 来源 | 前端用途 |
| --- | --- | --- |
| `FeatureStatusView` | 生命周期运行时 | 模块状态、错误码、DiagnosticId、revision |
| `CoreRuntimeStatusView` | 核心运行时（仅进程内） | Running/SafeMode、核心错误与单调 revision |
| `FeatureSettingsSnapshot` | 设置运行时 | 已确认值、偏好、政策、有效值、revision |
| `NegotiatedFeatureView` | 网络能力运行时 | Pending/Available/Degraded/Incompatible/Unavailable |
| `ItemPlacementPreview` | 本地候选计算器 | Hidden/Candidate/LocallyInvalid、格位、朝向、原因 |
| `DragInteractionView` | 物品交互协调器 | Dragging/AwaitingProjection/Idle 与 generation |

核心 SafeMode 通过共享契约登记的进程内 `CoreRuntimeStatusView/CoreRuntimeStatusChangedEvent` 投影，不通过 LMN 广播本机核心状态。`Pending/Confirmed/Rejected/Unavailable` 是 Settings Presenter 根据 RequestId、计时器及 changed/rejected/snapshot 事件推导的本地状态；`ProjectionExpectation` 是物品交互协调器内部只读记录，二者均不属于共享契约。

所有带 revision 或 generation 的投影必须单调消费：旧值或重复值不得重新驱动 UI。普通 UI 不显示 nonce、封包、堆栈、文件路径或原始 payload。

## 3. “更好的物品交互”交接状态机

```text
Idle
  -> Dragging
  -> Hovering(Candidate | LocallyInvalid | Hidden)
  -> Dropping
  -> AwaitingProjection
  -> Idle
```

取消拖拽、原版界面关闭、容器会话变化、功能停止或 generation 失效均可直接清理增强层并回到 `Idle`。前端不得把视觉状态当作库存权威状态。

### 3.1 提交与视觉等待

调用原生 `sendDragItem` 后：

1. 立即隐藏拖拽物体、绿/红占据框和目标动画。
2. 保存只读 `ProjectionExpectation`，进入 `AwaitingProjection`。
3. 禁止同一拖拽 generation 再次提交。
4. 视觉等待预算为 2.0 秒，使用单调时钟。

2 秒超时只终止增强 UI 的等待指示并回到可安全交互状态。它不表示服务端拒绝，不触发插件重试、库存回滚、客户端补写或“放置失败”提示。

### 3.2 原生投影关联

一次观察到的库存变化只有同时满足以下条件，才可被标记为当前提交的高置信确认：

- `DragGeneration` 与当前期待一致。
- 玩家身份及源/目标 `ContainerReference` 属于同一库存会话。
- 源格发生符合本次移动的减少、移除或状态变化。
- 目标格出现匹配的 asset id、数量和状态指纹；指纹比较规则必须忽略原生系统合法改写的非稳定字段。

高置信确认可以结束 `AwaitingProjection`，但 V1 默认不播放夸张成功通知。若变化存在歧义、合并、拆分、并发整理或指纹不足，则接受原生刷新作为唯一事实，结束或失效当前期待，不宣称插件提交成功。

### 3.3 超时与迟到结果

- 超时后到达的原生库存投影仍正常渲染。
- 不恢复旧绿框、不弹“迟到成功/失败”、不把物品移动回源位置。
- 新拖拽必须分配新 generation；任何旧期待或旧计时器不得改变新 generation 的 UI。
- 容器关闭、页面切换、角色重生或库存会话重建会使旧期待立即失效。
- 原版拒绝或纠正最终由新的原生库存状态体现；插件不根据“目标未出现”自行推断拒绝原因。

## 4. 设置提交交接

### 4.1 客户端本地设置

`ClientLocal` 可在本地事务成功后发布新确认快照。前端的滑块/文本可先做本地视觉预览，并在 PointerUp 或静止 150 ms 后提交；该防抖属于 Presenter 策略，不改变后端原子事务语义。

### 4.2 服务器权威设置

1. 提交后相关控件进入 `Pending`，显示待确认值但保留最后确认快照。
2. 3 秒未收到终态时，以相同 `RequestId` 和相同预期 revision 重发一次，保证服务端幂等去重。
3. 从首次提交起总计 8 秒仍无终态时，将提交标记为 `Unavailable`，使用新的非零 RequestId 发送一次 `RequestModuleConfigSnapshotCommand` 请求完整设置快照。
4. 不进行第三次重试，不建立离线写队列，不在重连后偷偷重放。
5. 快照请求失败时继续显示最后确认值，并标注当前会话不可用；不得把待确认值伪装成已保存。
6. 收到旧 revision、错误 RequestId 或前一连接 generation 的响应必须丢弃。

若原设置提交的迟到响应与快照响应先后到达，Presenter 只采用 revision 更高的完整快照；revision 相同则内容必须一致并视为重复，内容不一致触发诊断并请求会话重新同步。较低 revision 不得覆盖当前确认状态。

明确的校验/政策拒绝应通过完整确认快照回滚控件，并可显示一次本地化行级原因。`ServerPolicyWithClientPreference` 必须同时展示持久偏好和本会话有效值，服务器政策不得静默覆盖本地偏好。

## 5. 网络状态的玩家表现

| 协商状态 | 默认表现 |
| --- | --- |
| Pending | 服务器权威控件只读，轻量“正在同步” |
| Available | 正常消费协商能力 |
| Degraded | 功能行显示降级徽标及本地化说明 |
| Incompatible | 仅对应功能禁用；显示可行动的版本说明 |
| Unavailable（插件缺失/超时） | 静默保持原版体验；设置打开或尝试使用时才显示行级说明 |

网络能力不可用不触发通用弹窗，不阻止原版连接，也不推断服务端恶意或故障。底层握手细节只写诊断日志。

## 6. 模块与 UI 呈现矩阵

| 情况 | 设置外壳 | 功能 UI/HUD | 提示 |
| --- | --- | --- | --- |
| 未安装/未协商 | 默认隐藏；仅服务器明确要求展示时给说明行 | 不创建 | 默认无 |
| Incompatible | 禁用并显示版本徽标 | 不创建/卸载 | 可行动时一次提示 |
| Disabled | 保留权限允许的启用开关 | 卸载 | 行级说明 |
| Starting/Stopping/Isolating | 只读 | 不接受输入并清理 | 不重复弹窗 |
| Isolated | 禁用，显示本地化错误码与 DiagnosticId | 必须卸载 | 状态中心保留；仅满足第 8 节主动通知条件时弹一次 |
| 服务器关闭 | 显示“由服务器关闭”，保留本地偏好 | 卸载会影响规则的交互层 | 通常无弹窗 |
| Core SafeMode | 仅保留安全 fallback 能力 | 卸载全部自定义 UI | 一次温和核心提示 |

“启动失败”不是新的 FeatureState：它表现为 `Starting → Isolating → Isolated`，并以稳定错误码和同一 DiagnosticId 说明来源。前端不得在 `Isolating` 尚未完成清理时宣称功能已安全卸载。

## 7. UI 清理责任

- Gemini 前端 adapter/扩展拥有其 Glazier 节点、HUD、输入监听和动画的语义清理责任。
- 所有跨回调存活资源同时登记到核心 `IFeatureLifetime`，由核心登记簿提供故障清理兜底。
- 收到 `Stopping` 或 `Isolating` 后立即阻止新输入并隐藏交互层；最终销毁按 GPT-09 的静默点和逆序 Dispose 规则完成。
- U3DS 不解析或实例化 UI 扩展；Contracts/Core 不引用 Glazier/Sleek 类型。

## 8. 通知与诊断去重

主动玩家通知的会话去重键为：

```text
FeatureId + FrameworkErrorCode + DiagnosticId
```

- 同一键每会话主动通知最多一次，状态中心和日志可持续保留记录。
- 单功能重复异常不得形成弹窗风暴。
- 网络波动、握手超时和库存 2 秒视觉超时保持静默。
- 主动通知仅用于 Core SafeMode、玩家明确操作被拒绝、或玩家能够采取行动的版本问题。
- 所有玩家文案由稳定本地化 key 与安全参数生成；普通玩家不得看到异常堆栈。
- `DiagnosticId` 只用于支持与日志关联，不作为安全令牌或网络身份。

## 9. 顺序与竞态不变量

- 先提交后端状态与 revision，再发布前端投影。
- 前端卸载后到达的回调必须由 feature gate 和 generation 双重拒绝。
- 设置 RequestId 只保证同一事务幂等；连接 generation 变化后不得跨会话复用响应。
- 库存 generation 只关联本地交互，不替代原版库存 revision 或服务端权威。
- 前端动画、计时器和提示的完成回调必须重新核对当前状态/generation。
- 任何超时都是 UI/传输等待政策，不能自行制造领域事实。

## 10. 验收矩阵

实现阶段至少覆盖：

- 2 秒前确认、恰逢超时、超时后迟到、旧 generation 迟到及容器会话切换。
- 原生目标指纹明确、歧义、合并/拆分、源变化但目标不确定。
- 设置立即确认、3 秒同 RequestId 重试、8 秒快照恢复、旧连接响应丢弃。
- 插件缺失、握手超时、单功能 Major 不兼容和能力降级的无弹窗行为。
- Start 异常、运行时隔离、服务器关闭和 Core SafeMode 的 UI 清理矩阵。
- 相同诊断重复、不同 DiagnosticId、不同功能及重新连接时的通知去重。
- U3DS 路径不实例化任何 UI 类型。

这些测试须分别标注纯逻辑、静态集成、客户端运行、SteamP2PFriends Host/Client 和 U3DS 证据。任何 Markdown、HTML 原型或协作者报告都不能替代生产 DLL 的三环境运行验收。

## 11. 非目标

- 不定义生产 Glazier 布局、动画时长或视觉主题。
- 不添加插件自定义库存提交/回滚网络消息。
- 不承诺根据投影差异还原原版拒绝原因。
- 不实现离线设置写队列、无限重试或当前进程内自动重启 Isolated 模块。
- 不在 Wayfinder 阶段声明构建、DLL 或三环境运行通过。
