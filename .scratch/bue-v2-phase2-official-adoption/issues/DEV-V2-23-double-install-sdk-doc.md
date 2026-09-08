# DEV-V2-23：防双装自检（BUE-PLATFORM-001）+ 开发者契约文档八节

Type: task
Status: resolved（2026-09-08,双轴 R2 双 CLEAN;结单报告 audit/2026-09-08/DEV-V2-23/）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-14（契约版本演化登记条目须已建立；可与 DEV-V2-16..22 并行）
Spec: `../spec.md`（「开发者契约与 SDK 引用」「防双装」「开发者文档」三节）

## What to build

玩家误把 BUE DLL 拷进另一插件目录时，看到结构化诊断（诊断 id、双方路径、移除建议）而非静默异常，BUE 永不自动删文件；第三方开发者有一份可引用的稳定契约文档：什么身份冻结、什么不承诺、编译期怎么引用、双装怎么办、契约版本怎么演化。

## Scope

- 防双装：BUE 启动（Awake）注入式自检——取已进入 AppDomain 的程序集列表，检查与 BUE 同程序集名的冲突副本，输出结构化诊断 `BUE-PLATFORM-001`（字段至少含：检测到的程序集名、冲突副本 Location、当前 BUE 路径、「移除非官方副本」建议）；诊断集中平台模块；**不自动删除用户文件**；措辞冻结：不宣传为完整防重复加载系统、不替代 BepInEx GUID 去重、不保证捕获未加载/加载失败/隔离上下文副本。
- 分工：同 GUID 双装 = BepInEx 原生行为（留一跳一）+ 文档 FAQ；同程序集名不同 GUID = BUE 自检补强。
- 文档：扩写现有 SDK 程序集身份文档，**不新建第二文档目录**（避免双事实源）；八节大纲冻结——1 适用范围 / 2 承诺 / 3 不承诺 / 4 编译期引用指引 / 5 GUID·程序集名·DLL 文件名 FAQ / 6 双装诊断 BUE-PLATFORM-001 / 7 契约版本演化 / 8 实机验证清单。
- 文档措辞要点：GUID 与程序集名冻结（程序集名若改 = 破坏性公告 + 迁移事件）；文件名不是契约身份、路径与发现规则属部署前提；指引 = 引用主 DLL + CopyLocal=false + 禁捆绑；契约版本与目标 BUE 版本对齐；独立 SDK 拆分四条件暂缓措辞；第 7 节与 DEV-V2-14/16/19 的登记条目一致。

## 验收条件

- [ ] 红测先行（程序集列表注入 seam，不触文件系统）：无冲突 / 同程序集名冲突 / 不同程序集名 / 空路径 / 重复条目 / 诊断 id 与关键字段——先红后绿
- [ ] 诊断在日志与面板可见；全仓无文件删除路径
- [ ] 文档八节齐备，第 7 节登记条目与代码一致，第 8 节列出 DEV-V2-24 将执行的实机清单
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN

## 追加 Scope 注记(2026-09-07,主工作树会话;源自平台愿景对账注记 3)

本票开发者契约文档八节须按**两层模型**写:官方功能 = 源码模块、构建期聚合进单一主 DLL;生态功能 = 独立 DLL、BepInEx 原生发现、声明 BUE 前置(`io.github.yu80rice.betterunturnedexperience`)、引用主 DLL(CopyLocal=false,禁捆绑,防双装 BUE-PLATFORM-001)、经公开注册桥(`BueRuntimeHost.Register`,SCR-GPT18-001)接入并消费平台服务。生态路径先例:`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`(独立 GUID + HardDependency + 公开桥注册)。完整愿景基线:`.scratch/bue-v2-phase2-official-adoption/research/2026-09-07-bue-platform-vision-phase3.md` 对账注记 3。

## Comments

### 2026-09-08 实施会话：R1 Spec 裁定登记（不属 Scope 扩张的两项实现级裁定）

1. **`BUE-PLATFORM-002` = 自检扫描侧故障的内部隔离诊断 id**（R1 Spec DEVIATION-2 裁定落地）：R1 指出该 id 未在票面声明且被写入开发者契约文档。裁定：① 从 SDK 文档 §6 移除（开发者契约面只保留冻结的 `BUE-PLATFORM-001`）；② 代码保留 002 作为扫描隔离留痕 id（个别程序集元数据不可读/注入源异常时的 Warning 行）——沿仓库 per-event 诊断 id 惯例（BUE-CLIENTUI-001..005、BUE-INVENTORY-001..003 等同为实现级、不入开发者契约）；`BUE-PLATFORM-001` 语义保持排他（仅双装冲突）。
2. **红测锚收回票面冻结六例**（R1 Spec SMELL-3 裁定落地）：`--bue-v2-platform-red` 只含六例注入 seam 收集组；「诊断在面板可见」验收改由独立断言 `AssertPluginPanelNotice`（AssertPlatformPanelNotice）随套件常跑，不与锚点混装。

### 2026-09-08 结单

- **交付**：防双装自检 `BuePlatformDoubleInstallCheck`（BUE-PLATFORM-001，程序集列表注入 seam 零文件 IO，永不删文件永不阻塞 bootstrap）+ 日志 Warning 行（每冲突副本一条）+ 管理面板红色状态行（三处渲染，双装优先）+ SDK 文档八节重写（两层模型主体，SCR-GPT18-001 登记，①..⑦ 契约登记原样保留，§8=DEV-V2-24 实机清单）。
- **验证**：编译红 CS0246 → 锚点六组 ALL GREEN（`--bue-v2-platform-red`）+ `AssertPlatformPanelNotice` 常跑；全套 7/7 PASS 0 警告。候选 DLL `7d5dd3b5…c223`（533504 B，三轮 Rebuild 字节一致，CaseId `DEV-V2-23-CANDIDATE-20260908`）；RELEASES 换标随 DEV-V2-24 实机验收。
- **审查**：R1 Standards CLEAN / Spec NOT CLEAN(2D+1S) → F1 → R2 全新实例双 CLEAN；判词 `audit/2026-09-08/DEV-V2-23/review-rounds.md`。
- **实机自验**：T7 五项 + 防双装真机基线绑 DEV-V2-24（沿 17/18/19/20/21/22 惯例，不单独走 auto-rm SOP）。
