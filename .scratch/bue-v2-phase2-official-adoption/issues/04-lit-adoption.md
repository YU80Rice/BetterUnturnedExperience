# T4：LIT（背包整理）纳入方式

Type: grilling
Status: resolved（2026-09-06,六问拍板,主会话 grilling 闭环）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 02, 03（均已 resolved,本票处于前沿）

## Question

LIT（LaunchInventoryTidy）官方纳入的方式决策（「吃掉并消化」：以 BUE 自己的契约/生命周期重新表达，原项目停维护）：

1. LMN 命名频道调用面 → `BueNetworkApi` 的重写映射（按 T2 盘点的调用点逐个定）;
2. `[BepInDependency(LMN,Hard)]` 摘除方式与项目落位（迁入本仓库后的 csproj 形态、单 DLL 装配）;
3. 设置/面板接入（Settings Facet、官方中文名「背包整理」、条目身份）;
4. **O-LIT-1（独立拍板）**：排列算法质量——原样移植保行为 vs 借机重设计;若重设计，验收口径怎么定（行为兼容 vs 新行为）。

产出：LIT 纳入设计决策，可交 `/to-spec`。

## Answer（2026-09-06,六问全决;用户逐条定稿）

### 决策 1(Q1)——算法:原样迁移,但立即建立 `ITidyStrategy` 可插拔策略 seam

**O-LIT-1 澄清(用户原话勘误)**:要改的是**排序规则**(物品进入整理计划的先后与分组),不是整套放置算法(占位/旋转/坐标)——两者都是"策略"的内部维度,seam 把它们与网络/模块层隔离。

结构(本期不重算算法逻辑,先深化承载方式):

```text
背包整理功能 → ITidyStrategy → CurrentTidyStrategy(default-grid-v1) → InventorySolver
```

- 接口最小形:`ITidyStrategy { string StrategyId { get; } TidyPlan BuildPlan(TidyInput input); }`;纯 C# 输入/输出类型(InventorySolver 本就零 Unity 依赖,顺理成章)。
- 本期交付:接口 + 唯一内置 adapter(`default-grid-v1`)+ StrategyId + 纯类型红测/回归测试。
- **不做**:策略选择器 UI、第三方 DLL 动态加载;`StrategyId` 进 SettingsRuntime = 后续「设置与策略治理」票,不阻塞纳入。
- 未来策略变体候选:compact-items-v2 / large-first-v3 / minimal-moves-v4 / 按类别分组(排序规则变体,复用放置逻辑)。
- 收益:深模块(上层只要"整理计划")、locality(优化只动策略实现)、leverage(手动整理/预览/夹具复用同一接口)、可测试(纯 C# 替换 adapter)、可归因(网络迁移与算法问题分离)。

### 决策 2(Q2)——官方 FeatureId = `io.github.yu80rice.bue.inventory-tidy`

**面板身份 = FeatureId = 网络频道身份,一词一贯**;显示名「背包整理」。不沿用 `com.yu80rice.launchinventorytidy.net`——那是旧插件的网络频道,不是纳入后的官方功能身份;无已发布部署基座、旧 LMN2 帧与新 BUE 帧本不兼容,沿用零收益(身份重命名,非互操作迁移)。

### 决策 3(Q3)——设置面 = 本期只持久化 `enabled`

- `enabled` 属 `ClientLocal`,SettingsRuntime 唯一权威;关闭 → 回退原生整理行为(原生回退语义)。
- 每页方向/模式:内存态保留,标记为非持久化临时状态;**后续持久化时不得把旧静态字典直接升格为设置事实源**。
- 未来 seam 预留:SettingsRuntime.enabled + 未来的 strategyId/pageDirection/mode。

### 决策 4(Q4)——熔断 scope 改绑 BUE 连接代际

- 语义:Connected/新 connection generation → 开始本次会话 fault scope;Disconnected/generation changed → 关闭旧 scope + 清内存态 + **保留磁盘持久化统计**。
- 两件事分立:scope 生命周期 = BUE 网络会话代际决定;熔断记录持久化 = 背包整理功能私有(JSON 键结构不变,不复制进第二事实源)。
- `"p2p"` 不再由 SteamP2PFriends 调用(BUE 仓库无该类型引用的老问题就此消掉)。
- 否决理由在案:全局 scope 会让一次 P2P 故障污染单人/下次连接;LIT 自推 Provider 事件会造第二个连接事实源并与自动握手时序竞态。

### 决策 5(Q5)——TIDY_TEST_HARNESS 归档,不进玩家 DLL

- 定性:旧 harness 是旧候选包的验收 adapter,不是产品 module;seam 已换(IFeatureModule/Bootstrap/IBueNetworkApi/session lifecycle/ITidyStrategy),迁入只会造浅层兼容壳。
- 本期红测面(新 seam 重写):策略替换测试、`enabled=false` 原生回退测试、连接代际切换下的 fault scope 测试、纯算法测试保留(InventorySolver 直测)。
- 夹具类型从生产 csproj 排除(硬规则)。

### 决策 6(Q6)——LIT/LIR 缝 = LIT 发布「整理完成」功能事件

```text
LIT → IOwnedFeatureEventPublisher → TidyCompleted 事件
LIR → IFeatureEventSubscriber → 验结果为成功 → 执行自动压弹
```

- 消除 `TypeByName("LaunchInventoryTidy…")`、跨功能 Harmony postfix、异常静默吞;官方功能间走统一公开契约,可独立隔离/停用/测试;未来换整理策略 LIR 无感。
- 事件载荷至少含:FeatureId、整理对象范围、完成结果、连接代际(如适用)、事务/操作标识;LIR 不得见事件就盲执行,须验成功并保持自身幂等与异常隔离。

### 移交与注记

- 页范围维持玩家页 2–6;UI 按钮 Harmony postfix 留在模块内(Harmony ID 收编到 FeatureId 下,Start 装/Stop UnpatchSelf)。
- 三阶段卸载(BeginQuiesce → dispatcher Shutdown → CompleteShutdown)映射到 `IFeatureModule.Stop`,静态表绑功能 generation,禁止跨功能泄漏。
- 排序规则变体(compact/类别分组/最少移动)= 后续独立票(ITidyStrategy 下加变体),超出本图目的地,记入地图 Out of scope/backlog。
- 可交 `/to-spec`。
