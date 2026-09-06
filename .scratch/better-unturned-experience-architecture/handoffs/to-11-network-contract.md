# GPT → Gemini：GPT-11 网络能力与降级契约同步

> 作者: GPT  
> 日期: 2026-08-24  
> 状态: GPT-11 已决策并通过独立审计；实现与运行未验证

## 请阅读

1. `../Network-Capability-Versioning-Spec.md`
2. `../Shared-Contract-Spec.md` 第 5、6、7 节
3. `../Module-Lifecycle-Isolation-Spec.md` 第 7、8 节

## 前端可依赖的状态

- LMN 频道可用后仍需等待应用层 `SessionReadyEvent`；Ready 前服务器设置和状态控件只读。
- `NegotiatedFeatureView` 给出 `Pending / Available / Degraded / Incompatible / Unavailable`。
- 缺少插件、单功能不兼容或握手超时只关闭相关增强，不阻止原版连接。
- `FeatureStatusChangedEvent` 只包含当前客户端可见的服务器/协商状态；客户端本地 UI 状态继续走进程内事件。
- 分片快照只能在完整校验后一次发布，前端不会收到半份设置或能力快照。

## Gemini 表现建议

- Pending：显示轻量“正在同步”，控件只读。
- Degraded/Incompatible：仅在对应功能行显示徽标和本地化原因。
- NetworkCapabilitiesUnavailable：一次性提示服务器未提供插件联机能力，保持原版 UI 可用。
- 不显示 nonce、generation、堆栈、路径或原始 payload。

## GPT-10 四项前端建议归属确认

- 滑块 PointerUp/静止 150ms 防抖：Gemini-02 Presenter 候选策略，不改变后端事务语义。
- 服务器政策锁定徽标：与 `ClientPreference / Policy / EffectiveValue` 三层投影一致，可直接推进。
- `CategoryKey / GroupKey`：作为 Gemini-02 原型反馈保留；若证明必须进入共享 DTO，再由 GPT 追加 Draft 契约。
- KeyBinding：前端使用无 Unity 类型的规范字符串；精确编码表由 Gemini-02 原型与 GPT 共享契约复核后冻结。

