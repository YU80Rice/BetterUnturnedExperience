# DEV-V2-23 结单报告：防双装自检（BUE-PLATFORM-001）+ 开发者契约文档八节

日期：2026-09-08 | 票据：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-23-double-install-sdk-doc.md` | 状态：**resolved（双轴 R2 双 CLEAN）**

## 1. 交付内容

- **防双装自检（平台模块）**：`src/BetterUnturnedExperience.Plugin/BuePlatformDoubleInstallCheck.cs`——BUE Awake 注入式自检，取已进入 AppDomain 的程序集列表（`DefaultAppDomainSource` 防御式映射：逐程序集 try/catch，动态程序集无 Location 属常态不告警），决策核 `Check` 检查与 BUE 同程序集名（大小写不敏感保守口径——fusion 绑定本就不分大小写，诊断宁可过报不可漏报）的冲突副本；**诊断 id = `BUE-PLATFORM-001`**，字段至少含检测到的程序集名（assembly）、冲突副本 Location（conflictLocation）、当前 BUE 路径（selfPath）、「移除非官方副本」建议（suggestion）；每冲突副本一条 Warning 结构化日志（`BueRuntimeLog.Warn` 新增 Warning 级通道，不受运行时静默门）；位置缺失以 `(路径不可用)` 占位仍报告；重复列举按路径去重（保留首观测写法）；**零文件 IO、永不删除用户文件、永不阻塞 bootstrap**（插件侧 `RunPlatformDoubleInstallSelfCheck` 隔离，扫描侧故障以实现级 id `BUE-PLATFORM-002` 留痕，见 §4 裁定）。
- **面板可见**：`ManagementPanelModel.SetDoubleInstallNotice/DoubleInstallNotice`（通知面，默认空、Refresh 不擦、条目集合不变）；`BueNativeManagementPanel` 三处渲染（详情尾/空目录/未选中早退，双装优先于兼容性通知），红色状态行文案与 SDK 文档 §6 引文一致（「错误：」前缀由红色状态通道统一附加）。
- **程序集列表注入 seam（spec Seam 总图第 7 条）**：决策核只消费 `BueLoadedAssemblyView` 视图记录（名/路径/是否自身），测试注入清单、不触文件系统；`LoadedAssembliesSource` 测试缝生产默认 null，try/finally 复位。
- **开发者契约文档八节**：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 按冻结大纲重写——1 适用范围（两层模型：官方=源码模块构建期聚合单 DLL / 生态=独立 DLL 由 BepInEx 原生发现，BUE 不自建扫描器加载器）/ 2 承诺（GUID·程序集名冻结、契约版本演化、公开桥唯一 SCR-GPT18-001、平台服务同权）/ 3 不承诺（含防双装完备性与「替你处置文件」）/ 4 编译期引用指引（接入六步、CopyLocal=false、禁捆绑、Contracts 边界、SDK 拆分四条件暂缓）/ 5 GUID·程序集名·DLL 文件名 FAQ（同 GUID=BepInEx 留一跳一）/ 6 双装诊断 BUE-PLATFORM-001（字段、可见位、边界措辞冻结、用户处置、分工表）/ 7 契约版本演化（①..⑦ 登记条目原样保留，与 DEV-V2-14/16/19/21 代码一致）/ 8 实机验证清单（DEV-V2-24 执行，T7 五项+防双装真机基线，双证标注）。生态路径主体化：NoOpFeaturePlugin 活样板贯穿。

## 2. 红绿链与审查链（全记录见 `red-evidence.md`、`review-rounds.md`）

- 红绿：编译红 CS0246（`BueLoadedAssemblyView` 缺失，红测先于实现）→ 实现一次写入直绿；无运行时红（LHT 同形，如实留痕）。全套 7/7 PASS、0 警告。
- 双轴独立审查（每轮全新实例）：
  - R1：Standards **CLEAN**（3 SMELL+1 INFO 具名可延期）；Spec **NOT CLEAN**（2 DEVIATION：SCR-GPT18-001 未登记、BUE-PLATFORM-002 写入开发者契约 + 1 SMELL：第七组混入六例锚）。
  - F1：SCR-GPT18-001 三处补登记；002 移出契约文档（裁定登记票面 Comments）；锚收回冻结六例（面板断言独立常跑）；面板两处早退分支补通知渲染；Recorder 恢复惯例；文档面板引文修正。
  - **R2 双轴双 CLEAN：Standards CLEAN（R1 三 SMELL 处置全接受，1 INFO 维持具名延期）/ Spec CLEAN（零 GAP/DEVIATION/SMELL，三项处置全接受）。**

## 3. 验证与身份

- 候选：`BetterUnturnedExperience.dll` **533504 B**，SHA-256 `7d5dd3b5ee740a7929f3b136477114d368e723db981da0604cffa90fdf21c223`，三轮 `-t:Rebuild` 字节一致（`identity-rebuild1/2/3.log`、`identity-sha256-r1/2/3.txt`）；CaseId **`DEV-V2-23-CANDIDATE-20260908`**。RELEASES 换标随 DEV-V2-24 实机验收（沿 17/18/19/20/21/22 惯例，不继承既往批准）。
- 冲突复核：本票零 Harmony 补丁、零引擎类型新引用（决策核纯 C# 视图 + 字符串），与全仓既有补丁面无交集面可言；全仓 `File.Delete` 仍仅设置 tmp 原子写清理两处（先于本票存在，非用户 DLL 删除路径）。
- 全套证据：`sln-rebuild-fullsuite.log`（0 错 0 警告）+ `fullsuite-run.log`（7/7 PASS）+ `impl-green-run.log`/`fix1-green-run.log`（锚点 ALL GREEN）+ `fix1-plugin-suite-run.log`。

## 4. 实现级裁定（票面 Comments 登记）

1. `BUE-PLATFORM-002` = 自检扫描侧故障的**实现级**隔离诊断 id（个别程序集元数据不可读/注入源异常的 Warning 留痕），不入开发者契约文档；`BUE-PLATFORM-001` 语义保持排他（仅双装冲突）。沿仓库 per-event 诊断 id 惯例（BUE-CLIENTUI-001..005 等同为实现级）。
2. 红测锚严格六例（票面冻结边界）；「诊断在面板可见」由独立断言 `AssertPlatformPanelNotice` 随套件常跑。

## 5. 具名延期 / 边界

1. **CONTEXT.md「三段式」glossary 未指向八节 SDK 文档**（Standards R1/R2 INFO，词汇冻结文件不动，随治理票收敛）。
2. **实机验证绑 DEV-V2-24**：T7 五项清单 + 防双装真机基线（无冲突部署零 001 行；不同 GUID+同程序集名红测+实机双证）——本票实机自验按票面与兄弟票惯例挂终票，不单独走 auto-rm SOP。
3. 同 GUID 双装 = BepInEx 原生行为（留一跳一）+ 文档 FAQ，BUE 不重复处理（spec 冻结分工）。
4. 大小写不敏感为**保守过报**口径（fusion 绑定本不分大小写）；如未来实测 Mono 某路径严格区分，属诊断面加严方向，不影响冻结语义。

## 6. 档案清单

`compile-red.log` / `impl-build1.log` / `impl-green-run.log` / `impl-plugin-suite-run.log` / `fix1-build.log` / `fix1-green-run.log` / `fix1-plugin-suite-run.log` / `sln-rebuild-fullsuite.log` / `fullsuite-run.log` / `round1-increment.diff` / `round2-increment.diff` / `identity-rebuild1..3.log` / `identity-sha256-r1..3.txt` / `red-evidence.md` / `review-rounds.md` / 本报告。
