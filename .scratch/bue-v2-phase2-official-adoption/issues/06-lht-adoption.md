# T6：LHT（更好的尸潮播报）纳入方式

Type: grilling
Status: resolved（2026-09-06,四问+非问题项拍板,主会话 grilling 闭环）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 02, 03（均已 resolved,本票处于前沿）

## Question

LHT（LaunchHordeTracker）官方纳入的方式决策：API 重写映射、Hard 依赖摘除、项目落位、设置/面板接入（官方中文名「更好的尸潮播报」）、以及 T2 盘点出的特有风险点（如信标生命周期、(epoch,seq) 广播幂等、不可靠通道 1:1 语义——08 已实机实锤的行为基线）。

产出：LHT 纳入设计决策，可交 `/to-spec`。

## Answer（2026-09-06,四问+非问题项全决;用户逐条定稿）

### 决策 1(Q1)——FeatureId = `io.github.yu80rice.bue.horde-tracker`

**面板身份 = FeatureId = 网络频道身份**,显示名「更好的尸潮播报」;旧频道 `io.github.yu80rice.launchhordetracker.horde-status` 退役。与 BII/LIT/LIR 统一身份规则;FeatureId 是稳定身份,显示名只是表现层文本;未来新功能拥有自己的 FeatureId,不复用 LHT 身份。

### 决策 2(Q2)——LHT 自有补丁保留 + 上下文守卫原则（含勘误）

**勘误注记**:答复清单误列了 LIR 的补丁(`UseableGun.ReceiveAttachMagazine`/`forceAddItem` 属 T5 已记录范围);**LHT 的自有补丁 = `InteractableBeacon.spawnRemaining/despawnAlive` Postfix**(`BeaconCounterPatches.cs`)。已按正确事实入账。

- 两枚信标 Postfix 保留为 LHT 自有 implementation,Harmony ID 收编 `io.github.yu80rice.bue.horde-tracker`。
- 冻结原则:**补丁可共享原生调用点,但不能共享业务上下文**——LHT 补丁 adapter 只捕获尸潮追踪上下文,非相关路径立即放行,不把 BII 物品交互状态当成自己的事实源。
- 冲突事实(本票已核):BUE/BII 现无任何信标相关补丁,目标零交集;叠加点红测仍按同款样式钉(context=false → 立即放行)。

### 决策 3(Q3)——表现状态分工 + 内部双件结构

- 主机/U3DS:权威追踪 + 广播 HordeSnapshot,**不给本地主机 loopback**(会话驱动组播天然保证)。
- 客户端:接收快照 + HUD 展示,10Hz 宿主时钟驱动。
- U3DS:功能运行状态 = Available,表现状态 = **HeadlessOnly**——不阻塞 U3DS 功能验收(追踪与广播仍在,缺的只是客户端 UI 表现;对照 CONTEXT「功能表现状态」)。
- **内部双件结构**(一个 FeatureId 内部组合,非两个注册功能):`HordeTrackingModule`(信标追踪/epoch/seq/广播) + `HordePresentationAdapter`(HUD 注入/10Hz 更新)——HUD 失败只降级表现,不影响服务器权威追踪。

### 决策 4(Q4)——设置与命令:本期只持久化 `enabled`(ClientLocal)

关闭 = **完整停摆**:服务器停追踪停广播、客户端停 HUD、`/horde` 不再产生 LHT 行为、回退原生"无额外尸潮播报"状态。implementation 常量:HUD 10Hz、`/horde` 冷却 1.5s、原版 admin 权限门(**权限判断继续交给 ChatManager 守门,LHT 不复制权限事实源**)。

### 决策 5(非问题项)——传输初始化上收 + 守卫删除 + 业务一致性保留

- 形态:`BUE 网络 module → 初始化传输 → 建立连接/会话 → 提供 BueNetworkApi;LHT → 注册频道 → 订阅会话 → SendToClients`。
- 删除:LHT 的 `ModTransport.Initialize()` 直接调用(三件唯一显式调用者)、`RuntimeDependencyGuard`(LMN Major==5 ABI 守卫)、对 LMN 类型与传输实现的硬依赖。
- **保留**(LHT 自己的业务一致性 implementation,不因传输上收而删):`epoch`、`sequence`、单槽 mailbox、dirty 标记、Clear 可靠发送、Update 不可靠发送、`ReceiveGate` 停止闸门。

### 决策 6(广播语义,承接 T3)——`SendToClients` 会话驱动组播

- 手写循环(`Provider.clients` 遍历+跳过本地)整体替换;Update `reliable:false`、Clear `reliable:true` 不变。
- 行为基线 = 08 实机实锤:epoch 完整生命周期、Update seq 连续无重复键、不可靠通道 1:1;实施红测新增 **BUE 帧不可靠 1:1 复验**(T2 §7.12 移交点)。
- 结构性改善注记:会话寻径下出站目标必然已握手——N-1 那类出站竞态面在新路径不存在(N-1 本身仍是 LMN 侧挂起项)。

### 决策 7(宿主时钟契约补充)——T5 六条不变性重申并加一条

主线程触发、频率与阶段固定、单调序号+时间增量、停止自动注销、单功能异常不扩散、**LHT 不得自建 Unity Update 泵**;未来官方与生态功能订阅同一 seam——不会出现"官方能用、生态拿不到时钟"的不一致。

### 扩展三分法(用户定稿)

1. **LHT 内部策略** → `IHordeTrackingPolicy` → `DefaultHordeTrackingPolicy`(未来:不同触发规则/波次统计/广播节流/快照聚合);
2. **表现 adapter 变体** → `HordePresentationAdapter` 下 NativeHud / ChatAnnouncement / FuturePanel(加面板、统计页、可访问性显示不动服务器权威追踪);
3. **真正独立的新功能** → 独立 FeatureId/设置/频道/生命周期/隔离需求者,立新官方功能模块(如未来尸潮统计 `io.github.yu80rice.bue.horde-statistics`),不扩大 LHT。

一句话:**LHT 本期冻结"尸潮追踪与播报"的稳定 seam,但不冻结当前实现细节**——以后可替换追踪策略、增加表现方式、增加独立功能,不必重写网络、生命周期和权威状态。

可交 `/to-spec`。
