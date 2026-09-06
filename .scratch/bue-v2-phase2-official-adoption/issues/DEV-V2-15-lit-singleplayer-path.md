# DEV-V2-15：LIT 迁入·单人全路径（ITidyStrategy + 本地整理 + 面板）

Type: task
Status: **resolved（2026-09-06 全链闭环：红绿链+双轴 R1→R2(作废)→R2'→R3 全 fresh 链双 CLEAN + 单人实机验收通过（用户原话「我验证LIT功能无异常」）；候选 `cacfa527…b040` 升 RELEASES 当前发布物（单人范围）；结单+验收记录 `audit/2026-09-06/DEV-V2-15/`）**
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: 无（先行票，可立即开始；不触网络，可与 DEV-V2-14 并行）
Spec: `../spec.md`（「三插件迁入形态」「功能身份与显示名」「LIT：整理策略 seam 与算法」「LIT：设置、熔断与夹具」四节）

## What to build

裸 BUE 单人环境：玩家在背包界面点「背包整理」，本地完成整理事务，排序与放置行为与旧独立插件一致；管理面板出现中文名「背包整理」，条目身份为新 FeatureId；玩家关闭后恢复原生整理行为，可再次开启。BepInEx 插件身份与 LMN 硬依赖从源头消失。

## Scope

- 迁入形态：领域项目源码迁入 `src/`，由 Plugin 工程 EmbeddedOfficial Compile Include 聚合进单一玩家 DLL；`IFeatureRegistration` + `IFeatureModule` 经宿主注册面进入；删除 `[BepInPlugin]` / `[BepInDependency(LMN,Hard)]` / `LmnDependencyGuard`。
- 整理策略：算法原样迁移（排序与放置都不改——O-LIT-1 勘误口径），立即立 `ITidyStrategy`（`StrategyId` + `BuildPlan` 纯 C# 类型）+ 唯一内置 adapter `default-grid-v1` 包住 InventorySolver；策略选择器 UI 与第三方动态加载不做。
- UI：整理按钮 Harmony postfix 留模块内，Harmony ID 收编 FeatureId（Start 装 / Stop UnpatchSelf）；页范围玩家页 2–6；每页方向/模式保留内存态、标记非持久化。
- 设置：只持久化 `enabled`（ClientLocal，SettingsRuntime 唯一权威；关闭 → 原生回退）。
- 生命周期：静态表绑功能代际禁止跨功能泄漏；三阶段卸载（静默 → dispatcher 关停 → 完全关停）映射到 `IFeatureModule.Stop`。
- 硬规则：TIDY_TEST_HARNESS 与全部实机夹具类型排除出生产编译列表（旧 harness 是旧候选包的验收 adapter，seam 已换，归档不迁）。

## 验收条件

- [x] 红测先行：策略替换（换 adapter 影响计划输出）/ `enabled=false` 原生回退 / InventorySolver 纯算法直测 / 夹具类型不在生产编译（编译期断言）——先红后绿（编译红 14 错 → 桩运行时红 NotSupported → 绿；锚点 `--bue-v2-lit-red` 折入默认套件）
- [x] 单人实机自验：点按钮 → 本地整理事务完成；关闭 → 原生回退（**2026-09-06 用户验收通过**，原话「我验证LIT功能无异常」；UMM 诊断包留档，部署物哈希=候选，验收记录 `audit/2026-09-06/DEV-V2-15/acceptance-singleplayer-20260906.md`；观察项：降序 SameType 首次整理被安全拒绝=迁入前既有算法行为，移交策略治理票）
- [x] 面板条目：FeatureId `io.github.yu80rice.bue.inventory-tidy` 身份 + 显示名「背包整理」（宿主测试断言 + 实机功能链路随用户验收一并确认）
- [x] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN（R1 双 NOT CLEAN → 修复 → R2' 双 CLEAN；R3 追加 fresh 验证双 CLEAN——链条见结单与 Comments）

## Comments

- **轮次链登记（2026-09-06，Fresh-instance 规则 425c2aa）**：`R1`（双 NOT CLEAN：Standards=dispatcher 粘滞跨代际+署名缺失；Spec=solver 直测缺失+先装配后注册）→ 修复 → `R2`（**继承轮，作废，不计 CLEAN 链**——续用 R1 实例，违反 "Two fresh contexts, one per axis, every round"，主会话裁定）→ `R2'`（fresh，双 CLEAN：Standards CLEAN 6 项具名延期；Spec NOT CLEAN 2 项→编译列表断言修复+TidyCompleted 边界 rebuttal→CLEAN；报告归档 `audit/2026-09-06/DEV-V2-15/R2'-*.md`）→ `R3`（用户指定的追加 fresh 验证轮，双 CLEAN：Standards CLEAN；Spec 初始 BLOCKED 1 项→同轮补证重归类 [INFO]（实机自验=用户侧下游门禁，非增量保真缺陷）→CLEAN；报告归档 `R3-*.md`）。候选身份 `cacfa527…b040` 全程不变（R2'/R3 增量均在测试工程与审计文档，生产零改动）。**待办：用户单人实机自验 + RELEASES 行 8 人工批准。**
