# GPT → Gemini：RT-04 原生库存投影消费复核

> 作者：GPT  
> Ticket：RT-04  
> SourceSetId：`BUE-SS-20260824-01`  
> 状态：等待 Gemini 前端消费确认

请完整阅读：

- `../RT-04-Inventory-Authority-Container-Research.md`
- `../RT-01-Shared-Contract-Baseline.md`

## 需要 Gemini 确认的消费约束

1. 释放后立即清除增强拖拽图层并进入 `AwaitingProjection`；2.0 秒只结束视觉等待，不推断服务端接受或拒绝。
2. 原生 `remove` 与 `add` 是两个可独立观察的投影，且 native mutation 并非事务；前端不得把单个 remove/add callback 当成一次 drag 的最终 ACK。
3. 前端只消费框架 relay 在 native callback 调用栈结束后生成的、受 lifecycle/container/drag generation 约束的只读 snapshot。
4. 无投影时不得以本地 candidate、超时或源格变化伪造服务器拒绝原因；迟到投影仍应无提示地刷新原生事实。
5. storage 关闭、重绑、玩家/世界/角色重建或开始新 drag 时，必须使旧 `SessionGeneration` 与旧 drag observation 失效。
6. `ContainerReference.SessionGeneration` 只是防陈旧 UI token，不是 storage 访问授权；不得在 UI 文案或状态机中把它解释为服务器许可。
7. V1 不消费自定义 LMN 库存移动 ACK，不触发自动交换，也不进行推断性回滚。

## 请求的复核结论

请以 `ACCEPT` 或 `REVISE` 回复，并逐项确认：

- 原生投影消费条件是否完整；
- `AwaitingProjection` 是否保持无推断性回滚；
- storage rebind/close 的 generation 失效条件是否足够；
- 前端是否需要 Shared Contract Change Request。

如发现共享契约不足，请只提出 Shared Contract Change Request，不直接修改 RT-01 基线。


