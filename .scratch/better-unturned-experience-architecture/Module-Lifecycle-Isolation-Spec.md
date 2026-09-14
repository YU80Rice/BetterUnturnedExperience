# GPT-Module-Lifecycle-Isolation-Spec：模块生命周期与故障隔离状态机

> **SUPERSEDED / 历史资料，不是现行契约**（2026-09-14 落标）：本文是 V1 期九态状态机草案（实现与运行未验证）；现行生命周期语义（状态投影、TryTrack、代际、隔离）以 SDK 附录 A.3 为准，不得按本文实施。
> 现行生态契约唯一事实源 = `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；人类开发者入口 = `docs/developer/README.md`。本文件保留仅作历史决策资料，不删除。

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-09 决策基线；实现与运行未验证**  
**输入确认:** 人工开发者授权；Gemini 前端建议无阻断异议

## 1. 目标

为核心框架、功能模块和客户端 UI 扩展定义确定的生命周期、异常隔离、依赖级联和清理义务。单个第三方模块失败必须局部降级；只有 Contracts/Core 不变量损坏才触发核心安全降级。

## 2. 功能模块状态

`ModuleRuntime/Lifecycle` 是 `FeatureState`、`StateRevision` 与 `FeatureStatusChangedEvent` 的唯一写入 seam。Runtime Feature Admission 只返回判别联合：全局 `CoreEscalation`，或产物可信时的不可变 `AdmissionEvaluationBatch`。可信批次中只有 `Admit(handle)` 与 `RejectIncompatible(reason)`；Admission 不读取动态设置/政策，也不直接修改或发布生命周期状态。

```csharp
public enum FeatureState : byte
{
    Discovered,
    Incompatible,
    Disabled,
    Starting,
    Running,
    Isolating,
    Isolated,
    Stopping,
    Stopped
}
```

### 2.1 合法转换

| 当前状态 | 触发条件 | 下一状态 | 必须动作 |
| --- | --- | --- | --- |
| Discovered | 契约/环境/依赖不兼容 | Incompatible | 记录稳定错误码，不调用 Start |
| Discovered | 用户关闭或默认关闭 | Disabled | 不注册运行资源 |
| Discovered | 所有前置检查通过 | Starting | 创建模块作用域与资源登记簿 |
| Disabled | 用户启用且依赖可用 | Starting | 重新执行前置检查 |
| Starting | Start 成功 | Running | 发布状态 revision |
| Starting | Start 抛异常或返回失败 | Isolating | 关闭新回调，进入强制清理 |
| Running | 用户关闭/插件退出 | Stopping | 关闭新回调，执行正常清理 |
| Running | 任一模块边界出现未处理异常 | Isolating | 立即熔断该模块 |
| Stopping | Stop、lease Dispose 或静默失败 | Isolating | 保留同一 DiagnosticId，继续强制清理，不得发布 Stopped |
| Isolating | 强制清理结束 | Isolated | 发布 DiagnosticId 与错误码 |
| Stopping | 正常清理结束 | Stopped | 发布最终状态 |

非法转换必须被核心拒绝并记录 `InvalidStateTransition`，不能由模块直接改写状态。

初始化顺序固定为：若 Admission 返回 `CoreEscalation`，不得从不可信产物建立任何功能状态记录，核心直接进入 SafeMode。只有可信 `AdmissionEvaluationBatch` 才允许 ModuleRuntime 为其中全部功能建立 `Discovered` 并发布初始 revision；随后 `Admit(handle)` 只保存 handle，`RejectIncompatible(reason)` 执行 `Discovered → Incompatible` 且不实例化模块。SettingsRuntime 再按静态 facet 完成持久值迁移、校验并产出只读 enablement snapshot；ModuleRuntime 结合该快照和当前动态依赖/政策执行 `Discovered → Starting/Disabled`。`Disabled → Starting` 使用已保存 handle，并重新校验最新动态事实。

### 2.2 V1 重试策略

- `Incompatible`：当前进程内不可重试；引用、契约或环境变化后随下次插件启动重新发现。
- `Isolated`：当前进程内不可自动重启，也不允许 UI 一键重试；防止异常模块进入重启风暴。
- `Stopped`：只允许插件整体重新初始化后重新发现。
- `Disabled`：允许用户启用，但必须重新校验依赖和环境。

## 3. 核心状态与安全降级

```csharp
public enum CoreRuntimeState : byte
{
    Initializing,
    Running,
    SafeMode,
    Stopping,
    Stopped
}
```

### 3.1 模块局部故障

以下只隔离所属功能：

- 模块 `Start`/`Stop` 或注册回调抛出异常。
- 模块自己的 Harmony patch、UI 回调、事件订阅或 LMN handler 抛出异常。
- 模块依赖在启动前不可用。
- 模块资源清理不完整。

其他模块、统一设置外壳和原版游戏继续运行。

### 3.2 核心故障

以下进入 `SafeMode`：

- Contracts 类型/版本自检失败。
- 核心状态机或资源登记簿不变量损坏。
- 唯一模块身份表、核心调度器或持久化根无法安全初始化。
- 无法保证模块回调已被隔离，继续运行可能污染原版状态。

进入 SafeMode 的顺序：

1. 停止接收新模块注册和新业务 command。
2. 使全部前端 interaction generation 失效。
3. 对所有已启动模块执行隔离式清理。
4. 保留最小日志与一次性状态投影；不继续运行功能逻辑。
5. 客户端在前端 fallback adapter 可安全使用时显示一次本地化提示；U3DS 只写日志。

SafeMode 在当前进程中不可恢复。框架不主动退出原版游戏，但只能尽可能停止自定义功能，不能保证核心损坏后原版体验完全不受影响；清理结果可能部分失败并必须记录。

## 4. 模块依赖与启动顺序

`FeatureDependency` 的逻辑共享类型定义位于 `Shared-Contract-Spec.md`，其事实由构建期 Dependency fragment compiler 与 Definition Linker 裁决。本规范只消费 Lifecycle facet 中已链接的 required DAG，并决定运行状态级联；运行时不得重新解析 manifest 或让模块通过 `Start` 声明依赖。

- 只有 required dependency 边参与启动 DAG、循环检测和阻塞拓扑；optional 边不参与拓扑排序。
- 构建期完成唯一 id、required DAG、缺失/版本与循环裁决；Runtime Feature Admission 验证产物绑定和当前环境适用性并返回 decision。Lifecycle 在调用任何 Start 前只对 admitted handles 和已链接拓扑做防御性不变量检查，并由其唯一状态写入 seam 投影拒绝或启动状态。
- 构建期 required cycle 阻止 Definition Artifact/CandidateBuild 产生；若可信产物中的单功能关系记录可归属地损坏，则 Runtime Feature Admission 局部拒绝该功能；影响范围不可确定时进入 Core SafeMode。Lifecycle 不把正常运行时 cycle 作为可到达业务分支。
- 启动顺序为 required DAG 的稳定拓扑序；同层按 `FeatureId` 字典序，保证确定性。
- 已链接 required dependency 在当前进程未获准或非 Running 时，依赖模块进入 `Disabled/DependencyUnavailable`；静态 `MinimumFeatureVersion`/`MinimumContract` 不满足应在构建期或 Runtime Feature Admission 阶段进入局部拒绝，不由 Lifecycle 建立第二套版本解释。
- optional dependency 不可用时模块仍可启动。optional 能力可见性在依赖进入/离开 Running 后更新；调用方必须在使用点通过 `IDependencyCapabilityView` 的已声明 dependency id 查询，不得缓存永久可用假设。跨网络能力按 GPT-11 的服务器裁定视图更新。
- 运行期 required dependency 被隔离时，依赖者按逆拓扑序进入 `Isolating`；级联只沿 required 边传播，每个 lifecycle generation 中每个节点最多处理一次。
- 清理顺序与启动顺序相反。

## 5. 资源登记与清理义务

### 5.1 双保险清理

模块必须在 `Stop(reason)` 内执行自身语义清理；核心同时维护资源登记簿，保证即使 Stop 抛异常也能撤销已登记资源。

```csharp
public interface IFeatureLifetime
{
    bool TryTrack(IDisposable registration);
}

public interface IFeatureBootstrap
{
    FeatureScopeIdentity Identity { get; }
    ulong LifecycleGeneration { get; }
    IScopedFeatureSettings Settings { get; }
    IFeatureEventSubscriber Events { get; }
    IOwnedFeatureEventPublisher OwnedEvents { get; }
    IFeatureLogger Logger { get; }
    IDependencyCapabilityView Dependencies { get; }
    IFeatureLifetime Lifetime { get; }
}
```

GPT-14 废弃模块运行时 `Describe()` 与任意 FeatureId 设置/能力访问。上述 bootstrap 只在 Runtime Feature Admission 成功后创建，并绑定当前功能身份；能力只能经当前功能已声明的 dependency id 查询，事件发布只能使用当前功能拥有的 declared event id。

必须登记：

- UI 元素挂载 lease。
- 输入/按键监听。
- 游戏事件与进程内事件订阅。
- 协程和定时任务取消句柄。
- LMN handler/频道注册。
- 每模块 Harmony id 的补丁 lease。
- 其他会跨过当前回调存活的资源。

### 5.2 清理顺序

正常停止与故障隔离都执行：

1. 将 per-feature dispatch gate 从 Open 原子切换为 Closing，拒绝新的 callback entry。
2. 递增 lifecycle generation，使模块相关 UI/拖拽结果和已排队工作失效。
3. 清除尚未开始的模块主线程队列项；网络/事件 wrapper 在进入和提交状态变更前都要重新校验 gate、state 与 generation。
4. 等待静默点：当前已进入的 wrapper 在 `finally` 中递减 in-flight；若隔离由 wrapper 异常触发，清理任务必须排到该 wrapper 退出之后执行。
5. 静默点达成条件为队列为空且 in-flight 为 0；随后调用 `Stop(reason)` 一次，捕获并记录异常。
6. 按登记逆序 Dispose 所有 lease；单个 Dispose 失败不阻断后续清理，但正常 Stopping 必须转入 Isolating。
7. 按模块专属 Harmony id 撤销补丁，清空缓存与状态引用，将 gate 置 Closed。
8. 无清理错误时发布 `Stopped`；出现任一 Stop/Dispose/撤补丁错误时发布 `Isolated/CleanupIncomplete`。

模块不能通过缓存原生全局对象绕过登记簿继续接收回调。

- `TryTrack(null)` 必须拒绝。
- gate 已为 Closing/Closed 时，`TryTrack` 必须立即在异常边界内 Dispose 传入资源并返回 false，禁止清理后重新登记。
- 后台线程不得直接运行模块业务回调或修改 Unity/Unturned 状态，只能通过带 gate/generation 校验的有界主线程队列提交。
- 若主线程 wrapper 退出后 in-flight 仍不能归零，视为核心调度不变量损坏并进入 Core SafeMode；不得一边有回调运行一边宣称模块已隔离完成。

### 5.3 时间预算

- 所有生命周期调用在游戏主线程执行，不创建后台线程修改 Unity/Unturned 状态。
- 单个 Start/Stop/Dispose 超过 50 ms 记录性能警告；累计清理超过 250 ms 记录严重诊断。这些是初始观测阈值，不是成功条件，后续由运行数据校准。
- V1 不尝试强行中断同步回调；因此第三方生命周期代码必须有界且不得阻塞等待网络或文件锁。
- 超时诊断不改变清理顺序，也不把未完成清理伪装为成功。

## 6. 异常边界与隔离阈值

- 任一异常若越过模块外部 interface、事件 handler、Harmony patch wrapper、UI callback 或网络 handler 边界，首次即进入 `Isolating`。
- 不采用“连续 N 次异常后再隔离”；首版优先保护原版游戏和其他模块。
- 核心捕获模块异常后生成一次 `DiagnosticId`，后续清理错误附着同一 id。
- 日志包含 feature id/version、状态 revision、环境角色、错误码和异常；前端只消费本地化错误码与 DiagnosticId。
- 不捕获 `StackOverflowException`、进程终止或 CLR 无法安全继续的灾难性条件并伪装为模块隔离成功。

## 7. 状态投影与事件顺序

```csharp
public readonly struct FeatureStatusView
{
    public FeatureId Feature { get; }
    public FeatureState State { get; }
    public FrameworkErrorCode Error { get; }
    public FeatureStopReason StopReason { get; }
    public string DiagnosticId { get; }
    public ulong StateRevision { get; }
}
```

- 每次状态转换先写入核心状态，再递增 `StateRevision`，最后在主线程发布一个 `FeatureStatusChangedEvent`。
- 同一 FeatureId 的 revision 单调递增；前端丢弃较旧或重复 revision。
- `Isolating`/`Stopping` 发布后，UI 立即禁止新输入和设置提交。
- 最终 `Isolated`/`Stopped` 只在静默点达成且资源登记簿完成一次全量清理尝试后发布。
- 进程内投影与网络投影共用状态语义；网络只允许 server→client 投影当前客户端可见的服务器/协商状态，范围见 GPT-11。

## 8. 前端表现契约

| 状态 | 设置中心 | 功能 UI/HUD | 玩家提示 |
| --- | --- | --- | --- |
| Discovered/Starting | 只读“正在加载” | 不创建交互 UI | 无 |
| Running | 可按设置权限交互 | 正常 | 无 |
| Disabled | 置灰；可启用时保留开关 | 必须卸载 | 本地说明 |
| Incompatible | 置灰 | 不创建 | 版本/能力提示 |
| Isolating/Stopping | 立即禁用输入 | 立即隐藏并开始清理 | 不重复弹窗 |
| Isolated | 置灰 + 警示徽章 | 必须已卸载 | 本地化错误码 + DiagnosticId |
| Stopped | 置灰或隐藏 | 必须已卸载 | 通常无 |

普通玩家界面不得显示异常堆栈、文件路径或敏感 payload。

核心 SafeMode：

- 卸载全部自定义功能 UI。
- 在客户端 fallback adapter 可用时仅显示一次温和提示：“《更好的未转变者体验》自定义功能已停止，游戏将尽可能继续运行；建议重启并查看日志”。
- 提示关闭后本会话不重复弹出；详细信息只写日志。

## 9. U3DS 与 UI 防穿透

`!Application.isBatchMode` 是前端 adapter 的必要运行门禁，但不是完整类型隔离方案。

- Contracts/Core 不引用 Glazier/Sleek 或前端具体类型。
- U3DS 不扫描、不反射创建、不注册 `IClientUiFeatureExtension`。
- 前端 bootstrap 只有在 `ClientUiAvailable` 能力成立且非 batch/headless 时才解析和实例化 UI 扩展。
- 第三方 `IFeatureModule.Start` 不接收 UI root，也不得直接创建 UI；UI 扩展通过前端 adapter 的独立注册路径运行。
- 单 DLL 必须通过 U3DS 冻结引用集检查和真实加载验证，证明未在装载/JIT 阶段解析 UI 专属依赖。

## 10. 测试不变量

至少覆盖：

- 每个合法/非法状态转换。
- required dependency 缺失、循环和运行期隔离级联。
- Start、Stop、Dispose、UI callback、Harmony wrapper、LMN handler 首次异常隔离。
- Stop 抛异常后登记资源仍逆序清理。
- 多个 Dispose 抛异常仍继续清理。
- 重复 Stop/隔离请求幂等，Stop 只调用一次。
- 状态 revision 单调及前端丢弃旧投影。
- U3DS 不创建 UI extension。
- Core SafeMode 清理全部模块且提示最多一次。

## 11. 非目标与待后续票

- 不承诺热更新或当前进程内自动重启 Isolated 模块。
- 不冻结具体 Glazier 类型、Z-order、红框、对象池或动画。
- 不定义设置持久化 schema（GPT-10）。
- 网络能力握手和状态投影语义已由 GPT-11 冻结；实现与运行仍未验证。
- 不决定候选算法参数（GPT-12）。
- AwaitingProjection 的视觉超时与迟到投影语义已由 GPT-13 冻结；本规范不重复定义。

