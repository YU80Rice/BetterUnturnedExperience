# DEV-15D：Settings + Lifecycle + Isolation

Type: task
Status: resolved
Owner: GPT（后端与运行时协调）
Required reviewer: Gemini（前端消费与表现状态复核）
Parent: DEV-15
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Specification: `../spec-DEV-15-better-item-interaction.md`
Dependency: DEV-15C（resolved）

## 目标

把 Better Item Interaction 的设置快照、生命周期协调、局部故障隔离、Core SafeMode、UI 卫星缺失降级和原生拖拽回退接入现有 ClientUi Seam。设置变更只影响下一次拖拽；运行时异常不得污染 BUE Host、统一设置中心、其他功能或原生库存权威路径。

## 本票范围

- 消费 `FeatureSettingsSnapshot` 中 `Enabled` 与 `AutoRotate` 两个 `ClientLocal` 设置；默认值为 `true`；拖拽开始时捕获不可变策略，拖拽中设置变化不影响当前拖拽。
- 协调 `Discovered → Incompatible → Disabled → Starting → Running → Isolating → Isolated → Stopping → Stopped` 九态生命周期，状态 revision 单调递增且不把清理中状态伪装为完成。
- Better Item Interaction 发生异常时局部隔离：停止增强回调、卸载自定义 UI、结束增强拖拽、恢复原生可用路径；清理异常仍继续后续清理并保留隔离诊断。
- Core SafeMode 时卸载全部自定义 UI，仅保留最小安全 fallback，并且一次性输出温和诊断；不得实例化 ClientUi/Unity/Glazier/Sleek 类型。
- UI 卫星缺失时保持 Core 与 Settings Facet 可用，表现状态为 `PresentationDegraded` 或 `HeadlessOnly`，不实例化缺失卫星。
- 设置关闭、不可用、冲突、异常、隔离时统一走原生 Pass-Through；不新增库存 RPC、不伪造回滚、不修改 Contracts、LMN、Unturned。

## 明确不做

- 不实现真实 Unity/Harmony callback Hook、真实 Glazier 控件布局或三环境运行验收。
- 不重写 `SettingsRuntime`，除非测试证明现有快照接口不能满足本票；优先在 ClientUi 建立纯 C# 协调 Seam。
- 不引入后台线程修改游戏状态，不改变原生库存权威链。

## 预先冻结测试 Seam

- `BetterItemInteractionSettingsState`：从 `FeatureSettingsSnapshot` 读取设置并在 drag begin 原子捕获策略。
- `BetterItemInteractionLifecycle`：九态单调 revision、Start/Stop/Isolate/ SafeMode 与 cleanup 失败隔离。
- `BetterItemInteractionUiComponent`：设置门禁、代际守卫、UI satellite 缺失降级及 native fallback。

## 验收条件

- [x] TDD Red → Green：默认 Enabled/AutoRotate 均为 true。
- [x] Enabled=false 时增强预览与增强提交均不介入，原生拖拽可用；重新开启后下一次拖拽恢复增强路径。
- [x] AutoRotate=false 时当前拖拽只使用当前方向；设置在拖拽中改变仅下一次拖拽生效。
- [x] 九态生命周期可观测、revision 严格递增；Disabled/Isolated/Stopping/Stopped 不产生增强回调。
- [x] 组件异常只隔离 Better Item Interaction，UI 对称卸载，原生回退仍可用；清理异常不阻断剩余清理。
- [x] Core SafeMode 卸载全部自定义 UI，一次温和诊断，绝不报告为正常运行。
- [x] UI satellite 缺失不影响 Settings Facet；投影为 `PresentationDegraded`/`HeadlessOnly`，不调用卫星工厂。
- [x] ClientUi/Contracts/Core UI/native token 静态门禁通过；Headless 不实例化 ClientUi。
- [x] Release 构建 0 errors / 0 warnings；全套测试通过。
- [x] GPT 独立审计 R3 PASS（`audit/2026-08-26/DEV15D-Independent-Audit-R3.md`）。
- [x] Gemini 前端消费复核 ACCEPT（`DEV-15D-Settings-Lifecycle-Isolation-Review.md`），正式关闭本票。

## 证据边界

本票通过仅证明纯 C# 设置、生命周期、隔离与回退 Seam；不证明真实 Unturned UI Hook、单人、SteamP2PFriends、U3DS 或发布资格。

