# V3-T4 BueLifecycle 模块状态、代际与资源清理

- **Ticket**: V3-T4
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-09 七项裁决定音，方案 A）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

阶梯二级。风险级 **Strong**。按六问结构：

1. **模块状态机**：愿景基线 Registered → Accepted → Starting → Running → Disabled/Isolated → Stopped 与现行宿主模块 Start/Stop 实现的差值——状态投影要不要进公开契约？生态模块能否观测自身状态？
2. **代际**：连接代际（网络域）之外，模块自身的启用代际 / 重入语义如何统一（熔断 scope 绑连接代际先例）？
3. **资源清理**：停止时注销事件/网络/补丁、旧连接 / Tick / 状态不泄漏——现行 UnsubscribeAll 首公里到生态承诺之间差多少？资源注册表要不要建？
4. **失败隔离**：功能级隔离 vs CoreSafeMode 的边界对生态模块如何表述（生态模块失败不拖垮官方功能）？
5. **契约影响**：新状态面进 Contracts 的版本代价？
6. **同权影响**：官方模块与生态模块的生命周期规则是否同一条？

事实输入：V3-R1。主源：愿景阶段 2 节、DEV-V2-21 宿主模块首公里、CONTEXT「功能级故障隔离」。

## Answer

2026-09-09 用户+PM 七项裁决全部定音，**方案 A（接线统一状态机）通过**。deepening 目标=把「每模块各自维护几个布尔标志」深化为宿主拥有状态/代际/资源/隔离语义的统一 Lifecycle module；生态只经小型只读 interface 观察状态、经 TryTrack 交付资源所有权。

### 1. 统一状态机（方案 A）

`FeatureState` + `FeatureStatusView` + `StateRevision` = BUE 对外**唯一**功能状态投影。状态由宿主拥有：Discovered → Incompatible/Disabled/Starting → Running → Isolating/Isolated → Stopping → Stopped。要求：StateRevision 单调递增；每次合法变化生成新只读视图；面板/日志/生态查询读投影；模块不可改状态；散装布尔标志（runtimePumpIsolated/HooksInstalled 等）=implementation 内部细节逐步被吸收；单模块状态变化不改变其他模块状态。保留局部隔离语义：单功能失败→该功能 Isolating/Isolated→其他功能与原版继续。不采纳 B（只冻 Start/Stop 会留下「Contracts 已定义、运行时不可观察」的静态空壳）。

### 2. `IFeatureLifetime.TryTrack` 资源接线

宿主每功能维护 `FeatureId + LifecycleGeneration → owned IDisposable set`。生命周期冻结：TryTrack 登记 → Stop(feature) → **Stop 返回后按注册逆序 Dispose**。规则：Stop 先于 Dispose；Stop 抛异常也必须继续资源清理；单个 Dispose 异常不扩散、进结构化诊断；`TryTrack(null)`=false；超容量=false+明确拒绝；已停止/隔离的功能不能再登记；资源句柄只属登记它的功能与代际；不得因重复登记导致重复释放。false=显式登记失败，不抛跨模块异常。容量上限留给实施票，本票冻结「**必须有上限、必须可观察、必须可测试**」。兑现 T2 授权：IFeatureLifetime 恒 null→可用。

### 3. 状态查询缝 + Dependencies

①生态获得只读状态查询：自己 FeatureState / StateRevision / 最近视图 / 是否隔离 / 隔离原因 / 当前代际。落点由实施票在「扩 IFeatureLifetime」或「注入独立只读 view」间选择——本票冻语义不冻类型形状。禁止：暴露 Setter、模块自切状态、伪造 StateRevision、直调宿主 StopAll、把 Isolated 改回 Running。②`IDependencyCapabilityView` 接线=**冻结目录的只读能力查询**（Has/TryGet），不是依赖求解器：允许 Has/TryGet；不允许重解 BepInEx 依赖、建第二套依赖图、改写加载顺序、动态装卸依赖、模块自行裁决依赖。依赖事实来自已冻结 Catalog/capability projection。

### 4. 代际语义 + UserDisabled 面板启停

①**两条代际轴完全分离**：LifecycleGeneration=模块启用/停用/重启代际；ConnectionGeneration=网络会话代际——不复用、不互写、不共用失效判断、不把断线误判为模块停止、不把重启用误判为重连（LIT 熔断继续用 ConnectionGeneration）。②**再启用=新模块代际**：旧代际的事件/网络订阅、资源句柄、Tick 注册、状态快照全部失效不复用。③**Isolated 不自动重启**：保持 Isolated，仅用户经面板明确重启用才 Isolated→新代际 Starting。④**面板启停路径纳入本票**（补 UserDisabled 运行时 seam）：面板=command adapter，ModuleRuntime 负责真正状态转换，官方/生态同一状态机，UserDisabled 可观察，停止后不再收事件/网络/Tick，再启用新代际。范围=运行时启停 seam，非面板视觉重做。

### 5. CoreSafeMode 与隔离触发面（两条通道冻结）

**CoreSafeMode 只处理核心组合期不变量**：Catalog 构建/冻结失败、Contracts/Core 自检失败、共享核心 capability（BUE Network 等）无法组合、宿主无法建立不可缺少的核心运行时。处理序：EnterCoreSafeMode → StopAll(CoreSafeMode) → 不再启动新模块 → 发布核心状态与诊断；当前宿主代际内单向不自动恢复。**单功能运行失败永不升级**：Start/Stop/事件回调/Tick/Dispose 异常、adapter 不可用、Harmony 运行异常→一律功能级隔离（Isolating→撤资源与订阅→Isolated→其他功能继续）。Harmony 触面：越过宿主模块回调 seam 的按功能级隔离处理，patch 命中/冲突/撤销规则归 BuePatching/T10 裁决，本票不扩成补丁平台重构。

### 6. 契约影响 = Minor 加性

正式冻结十项：IFeatureLifetime 资源登记语义、IDependencyCapabilityView 只读查询语义、FeatureState 运行语义、FeatureStatusView 投影、StateRevision 单调规则、CoreSafeMode 触发范围、StopReason 全枚举语义、两代际分离、UserDisabled 运行时启停、Isolated 不自动重启。版本：与 T2/T3 同批共用 **2.1**，晚则顺延 **2.2**。不删成员、不改类型名、不把内部状态机 implementation 公开为稳定类型。

### 7. 同权检验（四条）

①官方与生态共用同一 ModuleRuntime 状态机（不允许生态自维护 enabled/isolated 布尔）；②**NoOp 契约 probe 扩展**：TryTrack 登记→停止逆序 Dispose→UserDisabled 停止→新代际重启用→状态可查询→Isolated 可观察→资源/订阅清理（生态契约可用性测试，非业务功能测试）；③官方 dogfooding：至少一个官方功能先验证面板停用→真实模块 Disabled/Stopping/Stopped→事件/网络/Tick 停→再启用新代际→重新运行成功（UserDisabled 不停留在枚举里）；④隔离不扩散红测：模块 A Start 抛→A Isolated→B/C 继续启动→BUE 不进 CoreSafeMode；回调异常只隔离该模块。

### 本票冻结 / 不做

冻结：见上 1-7。不做：第二套依赖求解器、自动重启、全局 ACL、Harmony 平台重构、完整 UI 重设计、把 ConnectionGeneration 当 LifecycleGeneration、让模块自行修改状态。

## Comments
