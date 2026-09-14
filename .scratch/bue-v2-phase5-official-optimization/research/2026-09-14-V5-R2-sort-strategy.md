# V5-R2 整理排序与 O-LIT-1 现状

- **Ticket**: V5-R2
- **日期**: 2026-09-14
- **方式**: 只读对照一手源码 / 结单 / 规格。未改生产代码、未构建、未提交、不设计新算法。
- **范围**: T3 事实输入。回答票面 5 问。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| LIT-STR | `src/BetterUnturnedExperience.Lit/TidyStrategy.cs` | `ITidyStrategy` / `TidyInput` / `TidyPlan` |
| LIT-AD | `src/BetterUnturnedExperience.Lit/DefaultGridV1Strategy.cs` | 唯一内置 adapter，`StrategyId=default-grid-v1` |
| LIT-SOL | `src/BetterUnturnedExperience.Lit/Solver/InventorySolver.cs` | `TidyMode`、排序比较器、MaxRects/FFD 放置 |
| LIT-CAND | `src/BetterUnturnedExperience.Lit/Solver/LayoutCandidate.cs` | 多候选评分 |
| LIT-MOD | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` | 模块持有策略、模式/方向 schema、默认值、面板文案 |
| LIT-SVC | `src/BetterUnturnedExperience.Lit/Tidy/ManualTidyService.cs` | 事务层消费 `BuildPlan`，不自己排序 |
| LIT-EXE | `src/BetterUnturnedExperience.Lit/Tidy/LocalTidyExecutor.cs` | 把模块策略交给 service |
| LIT-UI | `src/BetterUnturnedExperience.Lit/Ui/InventoryTidyUiPatch.cs` | 标题栏只留「整理」；每页字典已退役 |
| LIT-REG | `src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs` | 设置 facet = 两条 Choice，无 StrategyId |
| CTX | `CONTEXT.md:153-155` | 领域词「整理策略」= 排序规则 + 放置规则 |
| ATTR | `docs/third-party/LaunchInventoryTidy-attribution.md` | 迁入「排序与放置行为不变」 |
| T4 | `.scratch/bue-v2-phase2-official-adoption/issues/04-lit-adoption.md` | O-LIT-1 勘误 + 不做选择器 |
| P2-SPEC | `.scratch/bue-v2-phase2-official-adoption/spec.md:152-156` | 纳入规格：两者都不改 |
| V215 | `.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-15-lit-singleplayer-path.md` | 「排序与放置都不改」工单原文 |
| V215-CL | `audit/2026-09-06/DEV-V2-15/DEV-V2-15-closing-report.md` | 结单：seam + 零改动算法 |
| V215-ACC | `audit/2026-09-06/DEV-V2-15/acceptance-singleplayer-20260906.md` | 单人验收观察：降序 SameType 首次拒绝 |
| V208-CASE | `audit/2026-09-05/evidence/DEV-V2-08-20260905/cases/lit/case.md` | O-LIT-1 原始观察 |
| V208-CL | `audit/2026-09-05/DEV-V2-08/DEV-V2-08-closing-report.md` | 结单具名事项 |
| V4-T6 | `.scratch/bue-v2-phase4-visual-experience/issues/06-t6-official-copy.md:41-48` | Q61：文案不承诺算法 |
| V4-06 | `.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-06-lit-header-mode-direction.md:17-21` | 全局 Choice；不做 StrategyId 选择器 |
| TEST | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` | `AssertLitSingleplayerPath` + Q61 文案钉 |

行号以 2026-09-14 工作树为准。

## 1. 职责切分：`ITidyStrategy` / `default-grid-v1` / `InventorySolver.TidyMode`

**结论：排序与放置都在 `InventorySolver` 内部；`ITidyStrategy` 是计划缝，不自己排序；`TidyMode` 是求解器入参，不是独立策略。**

领域词把两者拆开：整理策略「包含排序规则（物品进入计划的先后与分组）与放置规则（网格占位与旋转）」；「避免：把排序规则调整当作放置算法重写……在纳入阶段同时更换排序与放置规则」（CTX `:153-155`）。

结构（T4 决策 1 原文）：

```text
背包整理功能 → ITidyStrategy → CurrentTidyStrategy(default-grid-v1) → InventorySolver
```

（T4 `:27-28`；P2-SPEC `:154`）

| 层 | 文件 | 做什么 | 不做什么 |
|---|---|---|---|
| 模块 | LIT-MOD `:86`、`:188-189`、`:447-466` | 构造写死 `Strategy = new DefaultGridV1Strategy()`；把已保存 mode/direction 传给请求 | 不排序、不装箱。setter 是开发者缝，`null` = 开发者错误（`:453`） |
| 事务 | LIT-SVC `:739-742` | `strategy.BuildPlan(new TidyInput(width, height, sortDescending, mode, packList))`；按 `Placed`/`AllPlaced` fail-closed | 不直呼 `TryPack`（`:334-336`：「装箱计划不再直呼 InventorySolver」） |
| 策略缝 | LIT-STR `:15-26` | `StrategyId` + `BuildPlan(TidyInput)` → `TidyPlan` | 接口注释：「no strategy picker UI and no third-party dynamic loading exist in this phase (spec: 不做)」（`:12-13`） |
| 内置 adapter | LIT-AD `:12-26` | 原样转发 `InventorySolver.TryPack(..., input.SortDescending, input.Mode)`，`StrategyId` 恒为 `"default-grid-v1"` | 零自己的比较器/放置循环。注释：「排序与放置都不改 — O-LIT-1 勘误口径」（`:6-10`） |
| 求解器 | LIT-SOL | **排序**在 `SortByGeometry` / SameType 分组比较器；**放置**在 `TryPackMaxRects`（BSSF+分裂）与 `TryPackFFD`（行主序 First-Fit） | 零 Unity 依赖。`TidyMode` 是它的入参枚举（`:71-81`） |

`TidyInput` 同时携带 `SortDescending` 与 `TidyMode`（LIT-STR `:37-52`）。注释仍写「both per-page memory state, explicitly non-persisted」（`:30-32`）——这是 DEV-V2-15 原文；DEV-V4-06 已把模式/方向升为全局 ClientPreference（LIT-MOD `:63-69`、LIT-UI `:34-35`）。输入形状没变，存储位置变了。见 §5。

调用链（单人）：UI 点击 → `RequestTidyFromUiClick` 读已保存快照（LIT-MOD `:480-488`）→ `RequestLocalTidy` 入队（`:447-466`）→ `LocalTidyExecutor.Execute(..., mode, sortDescending)` 把 `module.Strategy` 交给 `TidyPage`/`TidyAllPlayerPages`（LIT-EXE `:65-67`）。联机路径把同一 `(page, mode, sortDescending)` 打上私有线协议，服务端仍走同一 service + 同一模块策略，不另选策略。

生产里实现 `ITidyStrategy` 的类型只有 `DefaultGridV1Strategy`。测试替身 `FixedPlanStrategyAdapter`（TEST `:6624-6658`）只证明缝可替换，不进玩家 DLL。

## 2. 同类 / 空间 / 大件 × 降序 / 升序：今天实际怎么排

**结论：三档是 `TidyMode` 的玩家标签，不是三条 `ITidyStrategy`。默认 = 同类 + 降序。面板文案只讲玩家可见效果，明确不承诺算法。**

### 2.1 枚举与默认

```csharp
internal enum TidyMode : byte
{
    SameType = 0,  // 同类优先
    MaxRects = 1,  // 空间优先
    FFD = 2,       // 大件优先
}
```

（LIT-SOL `:71-81`。类注释：「v2.0.0 起 SameType=0 为默认」。）

`TryPack` 签名默认 `sortDescending = true`、`mode = TidyMode.SameType`（LIT-SOL `:111-113`）。

面板档位是机器值也是显示值（LIT-MOD `:118-124`）：

| 玩家标签 | 机器值 | 求解器 |
|---|---|---|
| 同类 | `ModeSameTypeLabel` | `TidyMode.SameType` |
| 空间 | `ModeMaxRectsLabel` | `TidyMode.MaxRects` |
| 大件 | `ModeFfdLabel` | `TidyMode.FFD` |
| 降序 | `DirectionDescendingLabel` | `sortDescending = true` |
| 升序 | `DirectionAscendingLabel` | `sortDescending = false` |

映射：`TryMapModeLabel` / `TryMapDirectionLabel`（LIT-MOD `:524-543`）。未知字面量拒绝读取，不发明默认组合（`:497`）。schema 默认：`SettingValue.Choice("同类")`、`Choice("降序")`（`:105`、`:111`）。空店读默认：TEST `:4368-4372`「同类+降序」。DEV-V4-06 工单原文：「默认同类」「默认降序=大件优先」（V4-06 `:17`）。

### 2.2 排序实际做什么

几何排序 `SortByGeometry`（LIT-SOL `:412-444`），用于 MaxRects/FFD 候选与 SameType 的几何兜底候选 C：

1. 非法尺寸沉底；
2. 面积（`size_x * size_y`）；
3. 长边 `max(size_x, size_y)`；
4. `size_x`；
5. `GroupKey` 然后 `StableOrder`。

`sortDescending=true` 时 2–4 大者在前；`false` 小者在前。`GroupKey`/`StableOrder` 始终升序收尾。

SameType 额外分组（LIT-SOL `:237-376`）：

- `GroupKey` 通常 = `jar.item.id`（LIT-SVC `:713`；`PackableItem` 注释 LIT-SOL `:49`）。
- 候选 A：组按总面积，方向跟 `sortDescending`；并列按首次出现。组内再按面积方向 + `StableOrder`。
- 候选 B：组按首次出现；组内仍按面积方向 + `StableOrder`。
- 然后一律走 MaxRects 放置（`:368-369`）。

### 2.3 放置实际做什么

| 模式 | 排序 | 放置 | 多候选 |
|---|---|---|---|
| 同类 `SameType` | 组/组内比较器，见上 | 三个候选都用 MaxRects | 最多 3：A 总面积组序、B 首次出现组序、C 几何 MaxRects（LIT-SOL `:237-258`） |
| 空间 `MaxRects` | `SortByGeometry` | BSSF + 矩形分裂（`:556-650`） | 2：主方向 + 反向（`:144-146`、`:149-187`） |
| 大件 `FFD` | 同上 | 行主序 First-Fit（`:450-508`） | 2：主方向 + 反向 |

评分 `LayoutCandidate.CompareTo`（LIT-CAND `:47-56`）：未放置数 → 同类连通块 → 行主序段数 → 曼哈顿移动 → 旋转变化；全少者优。因此**玩家选的降序/升序是主候选方向，不是最终布局的保证**：几何模式会再试反向并按指标挑选；SameType 的 A/B 才吃 `sortDescending`，候选 C 也吃，但最终可能是另一候选赢。

旋转：优先 `PreferredRotation`（Prepare 时 = 原 `jar.rot`，LIT-SVC `:718`），失败再 `xor 1`；正方形跳过（LIT-SOL `:479-504`、`:574-631`）。

### 2.4 面板文案是否承诺算法

不承诺。Q61 原文：「描述解释玩家可见效果，不承诺排序算法或 O-LIT-1」（V4-T6 `:48`）。代码注释逐字复述（LIT-MOD `:125-129`）。冻结句子：

- 模式：「同类：把相同物品聚在一起；空间：优先保留大块空位；大件：优先放置大件。对当前栏整理和全身整理都生效。」（`:132-133`）
- 方向：「降序：大件优先；升序：小件优先。与整理模式共同决定整理顺序。」（`:134`）

测试经面板行投影钉死逐字（TEST `:12173-12176`）；档位顺序钉死 `同类/空间/大件`、`降序/升序`（TEST `:12027-12031`）。

文案与求解器的差距（给 T3，不在本票改）：

- 「大件」标签绑的是 FFD（First-Fit 行主序），不是「只按面积放」。面积大者在前是**降序**在几何比较器里做的，FFD 与 MaxRects 共用。升序 + 大件 = 小件先走 First-Fit。
- 「空间」= MaxRects BSSF，文案「优先保留大块空位」是意图不是证明。
- 「同类」三个候选里有一个纯几何 MaxRects 兜底，所以同类模式也可能交出非聚合布局。
- 方向文案「大件优先 / 小件优先」对 SameType 候选 A 实际是**组总面积**优先，不是单件。

## 3. O-LIT-1 原始观察：抱怨了什么，有没有可复现案例

**结论：原始抱怨是主观「排列算法质量有问题」，拍板「后续再说」；无物品清单、无截图、无坐标前后对照。迁入后唯一可复现的邻近观察是 DEV-V2-15 单人验收：降序 SameType 首次整理被静态验证拒绝。**

### 3.1 DEV-V2-08 原文

LIT case.md SP 段（V208-CASE `:7-10`）：

- 行为确认（用户口头）：「打乱背包后按键看到物品实际重排，功能可正常使用」
- 「观察项 O-LIT-1（非本票 finding）：排列算法质量用户主观认为有问题，用户拍板「后续再说」→ 记入官方纳入票议程；与 V2 迁移无关（网络链完整）」
- 截图：「未采集（gap，以日志锚 + 口头确认代偿）」

结单（V208-CL `:34`）：「**O-LIT-1**：用户认为 LIT 排列算法质量有问题（拍板「后续再说」）——非 V2 迁移缺陷（网络链完整），记官方纳入票议程。」后续建议含「O-LIT-1 排列算法质量复议」（`:47`）。

工单评论同一句（DEV-V2-08 `:42`）：「具名观察项 O-LIT-1（非本票 finding）：用户认为排列算法质量有问题（拍板后续再说）」。人工验收原话只确认「确实看到了重排」（V208-CL `:28`），没有写「差在哪」。

P2P/U3DS 段不再提排列质量，只记网络闭环通过。

### 3.2 08 没有可复现案例

一手材料里找不到：物品 id 列表、页号与网格尺寸、模式/方向、整理前后坐标、失败截图。case 自己标截图 gap。因此 **O-LIT-1 原观察不能按失败用例找回**，只能当主观质量挂账。

纳入盘点把 `InventorySolver.cs` 标成「排列算法（O-LIT-1 复议对象）」（`.scratch/bue-v2-phase2-official-adoption/research/2026-09-06-three-plugin-source-inventory.md:40`），没有补案例。

### 3.3 勘误把「质量」收成「排序规则」

T4 决策 1（T4 `:23`）：「**O-LIT-1 澄清(用户原话勘误)**:要改的是**排序规则**(物品进入整理计划的先后与分组),不是整套放置算法(占位/旋转/坐标)」。P2-SPEC `:155` 同句，并加「本阶段两者都不改」。这是 08 之后的用户勘误，不是 08 现场记录。08 原文没有区分排序 vs 放置。

### 3.4 迁入后可复现的邻近观察（不是 08 原案）

DEV-V2-15 单人验收（V215-ACC `:32-38`）：

- 日志：`page 3 静态验证失败（重叠）→ Prepare 失败 → Rejected`（`:1253-1266`）
- 用户切到升序后同页提交成功；大件模式两次提交成功（`:1561`、`:1787`）
- 判定：与旧插件逐字节相同，迁入前既有算法行为；`ValidateNoOverlap` 对重叠与越界打同一句「重叠」
- 处置：「移交未来「设置与策略治理/排序规则变体」票……本票按 O-LIT-1 勘误口径不改算法」

这是**降序 + SameType + page 3** 的 fail-closed，有日志锚，无物品清单。T3 若要验收用例，这是目前唯一带环境的观察，仍缺复现包。

Phase-4 明确不碰 O-LIT-1（V4-06 `:21`；第四阶段 spec 永久不变量「不把 O-LIT-1 排序规则混入本阶段视觉」）。算法从迁入到今天未改。

## 4. DEV-V2-15「排序与放置都不改」落在哪些注释/测试；改排序后哪些会红

**结论：口径落在工单、规格、attribution、adapter/service 注释；测试钉的是缝身份、确定性、边界/无重叠、文案逐字，不是一份黄金坐标表。改排序后必然红的少；「旧计划不再字节级复现」主要靠注释与 R3 零算法 diff 实证，不靠逐坐标断言。**

### 4.1 口径原文落点

| 位置 | 原文 |
|---|---|
| V215 Scope `:16` | 「算法原样迁移（排序与放置都不改——O-LIT-1 勘误口径）」 |
| V215 What to build `:11` | 「排序与放置行为与旧独立插件一致」 |
| P2-SPEC `:154-155` | 「排列算法原样迁移（行为不变）」+「本阶段两者都不改」 |
| T4 `:21-23` | 「原样迁移」+ 排序规则 vs 放置算法勘误 |
| ATTR `:14` | 「行为按「算法原样迁移」口径迁移（排序与放置行为不变）」 |
| LIT-AD `:6-10` | 「the plan output is byte-for-byte the old plugin's behavior (排序与放置都不改 — O-LIT-1 勘误口径)」 |
| LIT-SOL `:4-8`、LIT-CAND `:4-8`、LIT-UI `:12-13` | 「Behavior migrated as-is unless a DEV-V2-15 note says otherwise」 |
| LIT-SVC `:334-336`、`:739-740` | 「default-grid-v1 包住原求解器，行为不变」 |
| V215-CL `:8` | 「算法原样迁移，排序与放置零改动——O-LIT-1 勘误口径」 |
| V215-CL `:60-63` | Solver/LayoutCandidate 原样迁移；service 只改调用点，行为不变 |
| R3-standards `:15` | 「Solver/LayoutCandidate 相对 Archive 仅命名空间/可见性/版权头，零算法 diff」 |

### 4.2 测试实际钉了什么

全部在 `AssertLitSingleplayerPath`（TEST `:4188-4339`）+ 文案对拍（`:12173-12176`）+ schema 默认（`:4368-4372`）。入口：默认套件 `:468` 与 `--bue-v2-lit-red` `:288`。

**改排序/放置后仍绿（不钉具体坐标）：**

- `StrategyId == "default-grid-v1"`（`:4199-4200`、`:4209`、`:4356-4357`）
- 松网格上 `AllPlaced` + Tag 保真 + 旋转后脚印在界内（`:4210-4221`）。输入 6×5、三件 2×1/2×1/1×1，几乎任意合法装箱都过
- 换 adapter 改变计划（`:4224-4243`）——证明缝，不证明默认布局
- 同输入两轮 `ResultX/Y/Rot` 两两相等（`:4246-4258`）——钉**确定性**，不钉绝对坐标。比较器改了但稳定，这组仍绿
- 放置项在界内且平面无重叠（`:4260-4276`）——任何合法装箱都过
- 超尺寸 4×4 在 3×3 上 `Placed=false`，1×1 仍放置（`:4277-4284`）
- FFD 4×2 上 2×2 + 1×2：两件都放置、在界内、不重叠（`:4285-4302`）。注释写明「candidate scoring may pick either sort direction, so locate by Tag」——**故意不钉谁在左**
- 空页 `Count==0`（`:4303-4305`）
- 全未放置计划 → service `Rejected` 零副作用；`null` 策略 `ArgumentNullException`（`:4319-4339`）
- Q61 文案逐字、档位顺序、schema 默认 同类+降序（`:12027-12031`、`:12173-12176`、`:4368-4372`）——改算法文案不变则仍绿

**改排序后必然红，或高概率红：**

1. **确定性组（TEST `:4255-4258`）**——当且仅当新比较器对同一输入两次输出不同（丢掉 `StableOrder` 收尾、引入非确定次序）。稳定地换规则 → 这组仍绿。
2. **超尺寸组（`:4283`）**——若新规则把「永远塞不进的合法尺寸」也标 `Placed=true`，或连 1×1 都不放。
3. **FFD 组（`:4289-4302`）**——若 FFD 不再能把 2×2+1×2 放进 4×2，或允许重叠/越界。只换放置顺序、两件仍不重叠 → 仍绿。
4. **`AllPlaced` 松网格（`:4212`、`:4251`）**——新规则在 6×5 / 6×3 上放不下现有夹具。
5. **Q61 / 档位钉**——仅当改面板句子或档位名。纯求解器改不动它们。
6. **`StrategyId` 钉**——仅当改 `"default-grid-v1"` 字符串或拆第二条默认策略却不改测试。

没有测试断言「这三件必须落在 (0,0)/(2,0)/…」。因此 **「旧计划字节级复现」没有回归网**。R3 用 Archive diff 钉零算法变化（R3-standards `:15`），那是迁入当时的审查，不是活测试。

### 4.3 改排序但测试可能仍绿的含义

T3 若改 `default-grid-v1` 内部比较器：`AssertLitSingleplayerPath` 大概率继续绿，只要仍确定、不重叠、夹具仍放得下。玩家可见布局可以已经变。要锁旧布局必须另写黄金坐标/黄金顺序测试——本票不设计。

## 5. 有无策略选择器、第三方策略加载、每页不同策略

**结论：无选择器 UI、无第三方加载、无每页不同 `ITidyStrategy`。模式/方向曾是每页内存，DEV-V4-06 起是全局已保存 Choice。残留是注释，不是活字典。**

### 5.1 选择器 / 第三方

- 规格不做：「策略选择器 UI、第三方 DLL 动态加载；`StrategyId` 进设置 = 后续「设置与策略治理」票」（T4 `:33`；P2-SPEC `:156`；V215 `:16`；V215-CL `:8`）。
- 代码：`ITidyStrategy` 注释同句（LIT-STR `:12-13`）。生产唯一实现 `DefaultGridV1Strategy`。模块构造写死（LIT-MOD `:86`）。`CreateSettingsDescriptors` 只有 `inventorytidy.mode` / `inventorytidy.direction`（`:100-116`），**没有 StrategyId**。LIT-REG `:86-92` 同。
- DEV-V4-06 不做清单含「StrategyId 选择器」（V4-06 `:21`）。
- 无 `Assembly.Load` / `LoadFrom` 加载策略。`LitTidyFaultScopeBook.LoadFromDiskLocked` 是熔断 JSON，与策略无关。
- 管理面板循环的是模式/方向 Choice（TEST `:12027-12038`），不是策略。标题栏 DEV-V4-06 后只留「整理」（LIT-UI `:17-23`）。

### 5.2 每页不同策略？没有。每页不同模式？曾经有，已退役

当前：一次点击读**一份**全局快照，当前栏与全身共用（LIT-MOD `:480-488`、`:133`「对当前栏整理和全身整理都生效」）。`ITidyStrategy` 实例也是模块级单例。

DEV-V2-15 曾：每页方向/模式内存字典，非持久化（V215 `:17`；T4 `:44`）。V4-R2 当时仍在（`.scratch/bue-v2-phase4-visual-experience/research/2026-09-11-V4-R2-lit-header-settings.md` §2）。DEV-V4-06：「旧每页内存丢弃」（V4-06 `:17`）。现网：

- `s_PageTidyMode` / `s_PageSortDescending` **已删除**（LIT 源码零命中）。
- LIT-UI `:34-35`：「每页内存字典（方向/模式）退役」。`:52-53`：「the retired per-page dictionaries never come back」。
- 点击「never the panel draft, never per-page memory」（LIT-MOD `:473`）。

残留注释（不是活状态）：

- `TidyInput` 仍写「per-page memory state, explicitly non-persisted」（LIT-STR `:30-32`）——与 V4 全局设置矛盾，属过时注释。
- Phase-2 spec `:160` 仍写「每页方向 / 模式保留内存态」——那是纳入规格，已被 V4 实施取代，T3 应以现网源码为准。

联机请求带的是该次 `(mode, sortDescending)`，不是 `StrategyId`。服务端用自己模块上那一个 `default-grid-v1`。

## 6. 给 T3 的事实摘要（不裁决）

1. 改排序 = 动 `InventorySolver` 比较器/分组；改放置 = 动 MaxRects/FFD。`ITidyStrategy` 只是缝。CONTEXT 禁止「纳入阶段同时换两套」；纳入已结束，本阶段是否动放置由 T3 裁决。
2. 今日只有一条策略 `default-grid-v1`。换内部行为会使「byte-for-byte 旧插件」注释作废，但现有测试大多不红。新增第二条策略需要选择器或静默替换——选择器是 Phase-2 明确不做，T3 若重开必须显式写。
3. 三档同类/空间/大件是 `TidyMode` 标签，文案不承诺算法。改算法后若仍用旧档位名，玩家理解可能漂移。
4. O-LIT-1 原观察不可按案例复现。最接近的失败是 V215 验收：降序 SameType page 3 静态验证拒绝，升序/大件成功。无物品清单。
5. 当前栏与全身已经共用同一套 mode/direction + 同一策略。容器/被动整理尚未接入（T4/T5 另票）。
6. 本报告不设计新算法、不废止勘误口径。废止或收窄是 T3 的权。

