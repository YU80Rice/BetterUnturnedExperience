# T5：LIR（更好的换弹体验）纳入方式

Type: grilling
Status: resolved（2026-09-06,五问拍板,主会话 grilling 闭环）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 02, 03（均已 resolved,本票处于前沿）

## Question

LIR（LaunchInPlaceReload）官方纳入的方式决策：API 重写映射、Hard 依赖摘除、项目落位、设置/面板接入（官方中文名「更好的换弹体验」）、以及 T2 盘点出的特有风险点（如队列派发/回包语义、冷却闸门等已实机的行为保持）。

产出：LIR 纳入设计决策，可交 `/to-spec`。

## Answer（2026-09-06,五问全决;用户逐条定稿）

### 决策 1(Q1)——FeatureId = `io.github.yu80rice.bue.in-place-reload`

**面板身份 = FeatureId = 网络频道身份**,显示名「更好的换弹体验」。旧频道 `com.yu80rice.launchinplacereload.repack` 不承担新身份(历史传输实现 ≠ BUE 稳定功能身份)。未来 LIR 新增子能力**不新建频道身份**,除非它实际成为独立功能模块。

### 决策 2(Q2)——三补丁保留,建立 ReloadContextGuard 缝

```text
LIR lifecycle → ReloadContextGuard → 三个 Harmony adapter → 换弹行为
```

- 保留:`UseableGun.ReceiveAttachMagazine` Prefix/Postfix、`PlayerInventory.forceAddItem` Prefix——属 LIR module 内部 implementation,非公开 interface。
- 红测钉死叠加点:**BII 拖入 → forceAddItem Prefix 被调用 → reload context = false → LIR 不执行换弹逻辑**(把"补丁重叠"变成可验证 seam,不靠经验判断)。
- `Stop` 只撤销 LIR 自己的 Harmony ID,不影响 BII 或其它功能。
- 冲突审查事实(本票已核):BUE/BII 现有补丁目标与 LIR 三补丁零交集(forceAddItem/ReceiveAttachMagazine 全仓零命中;PlayerInventory 引用均为会话追踪/字段探针)。

### 决策 3(Q3)——TidyCompleted 消费 = 无条件自动压弹,经 Consumer→Action 两段缝

```text
TidyCompleted → TidyCompletedConsumer → ReloadAction adapter(本期:整理后自动压弹)
```

- 不加"整理后自动压弹"开关(LIR 已有 enabled 总开关;避免把行为迁移变成行为重设计)。
- 消费方必须验事件结果为成功 + 范围符合,保持自身幂等与异常隔离;不见事件就盲执行。
- 未来 ReloadAction 变体:整理后只补同 ID 弹匣/按武器类型处理/手动快速压弹等——经 adapter 扩展,不重写生命周期。
- 事件类型落位(TidyCompleted 共享类型放官方内 vs Contracts)→ /to-spec 定稿。

### 决策 4(Q4)——周期驱动 = 宿主 Tick 事件(窄事件,非万能总线)

```csharp
struct HostTick { ulong TickNumber { get; } float DeltaTime { get; } TickPhase Phase { get; } }
```

- 冻结不变性:Tick 由 Host 统一产生,**功能模块不得各自创建 Unity Update 泵**;频率与阶段明确;回调主线程执行;功能停止自动注销;单模块 Tick 异常不拖垮其它模块;载荷只含时间与序号,不携带功能逻辑。
- LIR 订阅后负责:双击检测、`ControlsSettings.reload` 轮询、换弹状态推进、超时判断。T6 HUD 消费同一 seam。

### 决策 5(Q5)——设置 = 本期只持久化 `enabled`(ClientLocal,关→原生回退)

- 常量集中到内部策略对象 `ReloadRuntimePolicy { DoubleClickWindow=0.3s, QueueLimit=64, DiagnosticInterval=5s }`,不散落补丁;本期非公开设置,未来需求明确再映射 SettingsRuntime 描述符。

### 扩展分类(用户定稿,供后续票引用)

1. **换弹策略扩展** → LIR 内部 `IReloadAction` adapter(MagazineReload/AutoReloadAfterTidy/未来变体);
2. **独立能力** → 拥有独立 FeatureId/设置/生命周期/频道/隔离需求者,立**新的官方功能模块**(如未来武器状态提示 `io.github.yu80rice.bue.weapon-status`),不塞进 LIR;
3. **跨功能协作** → 只走公开契约(TidyCompleted、HostTick),**不恢复**跨功能直接调用、反射寻类型、Harmony postfix。

一句话:**本期保持行为不变,但把换弹逻辑放到可替换的 action module 后面**——以后换规则、加策略、加设置,不必重写生命周期、补丁和网络接线。

### 移交与注记

- 注册延迟到首帧游戏线程 + 半注册回滚语义保持(对齐 LMN 时的实现习惯,落在新生命周期内)。
- 主线程 dispatcher 队列(MaxPendingRequestSenders=64)保留为 LIR 内部实现。
- 可交 `/to-spec`。
