# 定义模块生命周期与故障隔离状态机

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-05, GPT-15

## Question

功能模块从发现、兼容检查、注册、启用、运行、失败隔离到卸载应有哪些状态和转换？核心损坏与模块局部失败如何被可靠区分并投射给前端？

## Answer

人工开发者已授权并确认 Gemini 的四项前端输入。完整决策见 `../Module-Lifecycle-Isolation-Spec.md`。

- 功能状态冻结为 `Discovered / Incompatible / Disabled / Starting / Running / Isolating / Isolated / Stopping / Stopped`，非法转换由核心拒绝。
- 任一未处理异常首次越过模块边界即隔离；V1 不自动重启 Isolated 模块。
- 模块自身 Stop 与核心 `IFeatureLifetime` 资源登记簿形成双保险，按逆序撤销 UI、输入、事件、协程、LMN handler 和 Harmony lease。
- 清理前关闭 per-feature dispatch gate，并在主线程 wrapper 退出、队列清空、in-flight 归零后进入静默点；迟到 `TryTrack` 立即撤销资源。
- required dependency 不可用时依赖模块禁用；运行期依赖隔离时按逆拓扑级联隔离。
- 只有 required 边参与 DAG；循环、缺失与版本不足使用独立错误码，optional 能力按依赖 Running 状态动态可见。
- Contracts/Core 不变量损坏进入核心 SafeMode，清理全部模块并尽可能保持原版游戏运行；客户端仅显示一次温和提示，U3DS 只记录日志。
- U3DS 不扫描或实例化 UI 扩展；`!Application.isBatchMode` 只是前端 adapter 门禁，不替代类型依赖隔离。
- `FeatureStatusView` 增加 StopReason 与单调 StateRevision，前端以本地化错误码和 DiagnosticId 呈现，禁止展示堆栈。
- `FeatureStatusChangedEvent` 当前仅保证进程内投影；`0x0201` 在 GPT-11 定义 authority/scope 前保持 reserved。

该结论是规划决策，尚无实现、编译或三环境运行证据。

