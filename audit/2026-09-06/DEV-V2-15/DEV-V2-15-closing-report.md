# DEV-V2-15 结单报告：LIT 迁入·单人全路径（ITidyStrategy + 本地整理 + 面板）

日期：2026-09-06。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-15-lit-singleplayer-path.md`（V2 第二阶段先行票，无阻塞）。规约：`docs/agents/output-review-loop.md` 红测先行 + 双轴独立审查 CLEAN 才交付。

## 交付内容

1. **迁入形态**：LIT 领域源码迁入 `src/BetterUnturnedExperience.Lit/`（13 文件），由 Plugin 工程 EmbeddedLit Compile Include 聚合进单一 `BetterUnturnedExperience.dll`；`InventoryTidyFeatureRegistration`（IFeatureRegistration + IFeatureModuleFactory）经 `BueRuntimeHost.Register` 宿主注册面进入；`[BepInPlugin]`/`[BepInDependency(LMN,Hard)]`/`LmnDependencyGuard` 随源码不迁而消失（生产程序集零 `LaunchInventoryTidy*` 命名空间、零 BepInEx 插件身份，红测反射断言钉死）。
2. **整理策略 seam**（T4 决策 1）：`ITidyStrategy`（`StrategyId` + `BuildPlan(TidyInput)`，输入输出纯 C# 类型，入 Contracts 外的功能内 seam）+ 唯一内置 adapter `default-grid-v1` 包住 InventorySolver（算法原样迁移，排序与放置零改动——O-LIT-1 勘误口径）；`ManualTidyService.TidyPage/TidyAllPlayerPages` 增显式 strategy 参数（null = 开发者错误 fail-fast，无隐藏默认）；策略选择器 UI 与第三方动态加载不做（spec 不做项）。
3. **单人本地路径**：UI 点击（`InventoryTidyUiPatch`，迁移改写）→ `RequestLocalTidy` 门（Started/Enabled/ShuttingDown/FaultGate 四闸，显式枚举结果）→ `MainThreadDispatcher.TryEnqueue` → 宿主 Update 泵 `Tick()` → `LocalTidyExecutor.Execute`（主线程）：捕获热键 → 事务化整理（Prepare/Commit/Verify/回滚/mutation journal 原样）→ 提交成功按新坐标恢复热键（指纹校验十步链自旧 ManualTidyNetwork ACK 恢复移植）、CriticalFailure 已验证回滚按原坐标恢复、ConcurrentMutationAfterCommit 拒绝回滚保护并发变更。零网络调用。
4. **UI**：整理按钮 Harmony postfix 留模块内，Harmony ID = FeatureId（`Start` 装 / `Stop` UnpatchSelf）；页范围玩家页 2–6；每页方向/模式保留内存态、标记非持久化，Stop 代际清零。
5. **设置**：只持久化 `enabled`（`inventorytidy.enabled`，ClientLocal，SettingsRuntime 唯一权威，FileSettingsPersistence 持盘）；关闭 → 补丁即撤 + 请求显式 NativeFallback（原生回退）；重开 → 补丁重装 + 熔断闸复位。
6. **生命周期**：静态表绑功能代际——dispatcher 队列 Stop 关停/Start 代际重开（EnsureOpen）、每页方向/模式与按钮引用 Stop 清零、日志缝 Stop 解绑；三阶段卸载（静默 → dispatcher 关停 drain+cancel → 完全关停）映射 `IFeatureModule.Stop`。
7. **面板**：目录投影条目身份 = FeatureId `io.github.yu80rice.bue.inventory-tidy`、显示名「背包整理」、快照 = 实时 enabled toggle；`RoutingBueSettingsEditor` 增 LIT 路由（面板开关即时生效）。
8. **署名**：`docs/third-party/LaunchInventoryTidy-attribution.md`（按仓库 attribution 先例格式：作者 YU80Rice、MIT、本地 Archive 快照、纳入方式与身份替换说明）；6 个迁移文件 + TidyDiagnosticLog 带 MIT 版权头。
9. **硬规则**：TIDY_TEST_HARNESS 与全部实机夹具类型、旧插件身份类型、死代码（ItemsTryAddItemPatch 空类）排除出生产编译列表；红测反射断言生产程序集零夹具/旧类型（19 名单）。

## 红绿链（证据留盘 `red-compile-build.log` / `stub-stage-build.log` / `red-anchor-transcript.txt` / `fix-round-build.log`）

- **红0（编译红）**：锚点测试先行（`--bue-v2-lit-red`，引用未存在的 Lit 契约面）→ 14 错：CS0234（命名空间 Lit 不存在）×2、CS0246（TidyPlan×6/TidyInput×2/PackableItem×2/ITidyStrategy×2）。
- **红1（运行时红）**：Lit 契约面 + 抛桩落地编译绿后，锚点运行时红：`System.NotSupportedException: DEV-V2-15: default-grid-v1 strategy not implemented yet`（`DefaultGridV1Strategy.get_StrategyId()`，transcript 逐字留盘）。
- **绿**：真实现落地——锚点 exit 0；锚点折入默认套件（`AssertLitSingleplayerPath()`）；全套 7/7 PASS、0 警告 0 错误（`green-sln-build.log` + `green-BetterUnturnedExperience.*.Tests.log`×7）。

## 锚点覆盖面（红测面 → 验收条件对照）

- **策略替换**：default-grid-v1 计划输出（StrategyId 携带/逐项 Tag 保真/越界断言）；换 adapter（替身返回固定计划）改变同一输入的计划输出；service 层跟随注入策略（全未放置计划 → Rejected 零副作用；null 策略 ArgumentNullException）。
- **InventorySolver 纯算法直测**（工单字面，不经策略层）：确定性（同输入两轮逐坐标一致）；放置项边界内 + 平面无重叠不变量（候选评分不定序，按项断言）；超尺寸物品按求解器原语义（非合法物品，pack 仍成功且 Placed=false——service 层 unplaced 计数才是拒绝点，由策略层节钉死）；FFD 模式直测（按 Tag 定位，覆盖候选排序不定）；空页平凡成功。
- **enabled=false 原生回退**：模块级——构造零补丁；启动前请求 NativeFallback；设置权威关闭（ScopedSettingChangeRequest）→ RefreshSwitches → 补丁撤 + 请求 NativeFallback；熔断开 → RejectedFaultCircuit；Stop 后 → NativeFallback（三阶段可观察：ShuttingDown + 队列清空 + 补丁撤）。
- **夹具排除（编译期断言）**：反射生产程序集断言 19 个夹具/旧插件类型名不存在 + 零 `LaunchInventoryTidy*` 命名空间（编译器生成类型 null Namespace 防护）。
- **注册/面板/设置身份**：定义 payload 逐字 "BUE-LIT-V1" + 真实注册运行时接受 + MinimumBueContract (2,0)；宿主桥注册后目录投影「背包整理」条目；设置面恰一枚举 toggle 默认开。

## 双轴独立审查链（standards-reviewer / Spec-Reviewer 专属智能体）

| 轮 | Standards | Spec | 处置 |
| --- | --- | --- | --- |
| R1 | NOT CLEAN——**BLOCKING 2**：①dispatcher 关停粘滞跨代际（`_shuttingDown` 进程级不复位，违反「静态表绑功能代际」）+ 方向/模式点击无模块守卫 + 日志缝不随 Stop 解绑；②实现来源署名缺失（MIT 版权/许可文本未随迁）。SMELL 8 项 | NOT CLEAN——**GAP 1 + DEVIATION 1**：①`InventorySolver 纯算法直测`未覆盖（测试只经策略层）；②Register 先 `EnsureStarted` 后注册，注册被拒时补丁已装、不受宿主注册结果控制 | 全部修复（见下） |
| ~~R2~~ | **程序无效，判词作废**：R2 误用 `SendMessage` 续用 R1 同一审查实例——违反 output-review-loop「Two fresh contexts, one per axis, **every round**」；自审自复存在锚定/自证偏差，其 CLEAN 不具证据效力 | 同左（同批作废） | 处置：见结单会话过程记录；重派零上下文 R2' |
| **R2'（零上下文，权威）** | **CLEAN**（BLOCKING 0；SMELL 6 项具名延期：HandleTidyClick 在模块守卫前播种页字典（并入延期 1）/ ShuttingDown 不复位同构 Network 先例 / 拒绝路径残留日志缝绑定 / 两处注释与事实偏差 / 迁移注释 Codex 审计体例 / LICENSE permission notice 未写入副本） | 先 NOT CLEAN（**GAP 2**）→ 处置后 **CLEAN**：①夹具排除仅产物级反射、缺「编译期断言」→ **修复**：新增编译列表级断言（上溯定位 sln→读 Plugin csproj 原文→26 夹具/旧源文件名零入 Compile Include + TIDY_TEST_HARNESS 零出现 + 正向对照 Lit 源必须在列）+ 保留产物级反射，双层钉死；②未发布 TidyCompleted → **rebuttal 成立判非 GAP**：工单规格引用集四节不含 TidyCompleted 节；事件类型归 DEV-V2-19 入 Contracts、发布点归 DEV-V2-21（服务器权威事务完成处）、消费归 DEV-V2-22，15 无法引用契约面上尚不存在的类型（21 被 block 在 19 即此因）；移交点 = `LastLocalOutcome`（代码头+结单已登记） | 环路闭合（报告归档 `R2'-standards.md`/`R2'-spec.md`） |
| **R3（Fresh-instance 追加验证轮，主会话裁定传达+用户指定）** | **CLEAN**（全新实例自我声明+独立重推导：R1 两项 BLOCKING 处置逐条核实真实落地且语义正确；T4 决策符合性全过；Solver/LayoutCandidate 零算法 diff 实证；[DEFERRABLE] 1 项=HandleTidyClick 守卫前播种页字典并入延期 1，[INFO] 2 项） | 初始 **BLOCKED**（1 项 [BLOCKER]=单人实机验收未完成）→ 同轮补证 → 重归类 [INFO] 后 **CLEAN**：实机自验=用户侧下游门禁（派发 brief 第 6 条原文预定双 CLEAN 与实机待办并存；RELEASES 两门禁分离；DEV-V2-14 先例；loop 规约 CLEAN 语义不含用户实机验收）；其余 R1 处置/R2' 复核/RELEASES 行 8 身份一致性全部 INFO 级确认 | 环路双重确认（报告归档 `R3-standards.md`/`R3-spec.md`；候选身份不变，实机自验+RELEASES 行 8 人工批准照旧待用户） |

### R1 修复明细

- S1：`MainThreadDispatcher.EnsureOpen()`（代际重开缝，EnsureStarted 代际边界调用）；Stop 阶段 3 解绑 `LogSink/ErrorLogSink`；`HandleDirectionClick/HandleModeClick` 加 `ActiveModule==null` 守卫。
- S2：attribution 文档 + 7 文件 MIT 头。
- P1：锚点 1b 节 InventorySolver 直测（断言全部按求解器真实契约校准——超尺寸物品不 fail pack 属原语义，service 层 unplaced 计数才拒绝；候选排序不定处按 Tag 定位）。
- P2：`Register()` 改「Born-inert 注册 → Accepted 才装配」；拒绝路径零补丁零装配；Factory 惰性装配只在宿主已接受后的启动路径。
- SMELL 就地修：Lit 面 17 型 public→internal（功能私有，测试经 InternalsVisibleTo）；InstallPatches 失败半装回滚（UnpatchSelf + 清 ActiveModule）；LogWarning 改走 ErrorLogSink（警告可见性，前缀 "WARN "）；陈旧网络层注释清理（指向 DEV-V2-21）；Register/Factory 构造去重（ArmNewModule）。

## 具名延期（不阻塞，均登记）

1. **enabled=false 已注入按钮残留 + 关闭前已入队事务仍执行一次**（Standards R2-SMELL）：UnpatchSelf 不移除已注入按钮、点击已守卫短路；在飞工作项窗口 ≤ 一个插件 tick。实机自验观察项（「原生回退」观感以实机为准），按钮实体移除若需另立视觉票。
2. **LastLocalOutcome 预留字段**（Standards R2-SMELL）：TidyCompleted 发布属 DEV-V2-19（契约件）/DEV-V2-21（发布点），本票只存最后一次事务结果。
3. **LocalTidyFaultGate 最小化边界**（Standards R2-SMELL，代码头已声明）：无磁盘持久化统计、无 per-peer 准入、无管理命令——完整 TidyFaultCircuit/TidyRateLimiter/RequestLedger/SecurityLogLimiter/熔断代际绑定属 DEV-V2-21（联机路径才有多玩家准入面）；单人恢复路径 = 面板关闭→重开（代际复位）。
4. **TidyDiagnosticLog.Windows 不随 Stop 清空**（Standards R2-SMELL）：节流窗口上限 64 类、无增长路径、非跨功能泄漏。
5. **MainThreadDispatcher.Enqueue(Action) 旧入口保留**（Standards R2-SMELL）：原样迁移保留的旧调用形状，当前 DLL 内无调用点，注释已如实标注。
6. **R2' 就地处置的注释/顺序级 SMELL**（R2 与 R2' 两轮评审者点名）：dispatcher 类注释陈旧提法、ResetForTests 文档矛盾、Stop 解绑日志顺序、TidyDiagnosticLog 缺 MIT 头、RefreshSwitches 措辞——按评审者指名修复（零行为面，唯一行为变化=Stop 收尾日志可见性）；修复后全套 7/7 PASS 复跑留证。
7. **R2' 新增具名延期（Standards）**：①HandleTidyClick 在模块守卫前播种页字典（并入延期 1，实机自验观察）；②ShuttingDown 从不复位（同实例 Stop→Start 语义，与 Network/BII `default(FeatureStartResult)` 同构，宿主启动路径属后续票）；③拒绝路径残留 BindProductionLog 静态缝；④LICENSE permission notice 未写入迁移文件副本（attribution 文档 + 版权头满足 CONTEXT 署名，与 UPM 署名先例同形）；⑤迁移文件保留原「Codex 审计」注释体例（原样迁移不重写注释）。
8. **R2' 新增具名延期（Standards，测试缝）**：①csproj `Contains` 为原文子串匹配非 MSBuild 求值——通配/条件 Include 可绕过文件名断言，正向对照把当前显式列表形态钉死；②编译列表文件名清单与产物类型名清单双份需人工同步。若未来引入通配 Include，应升级为 MSBuild 求值级静态门。
9. **R2' Spec 复判记录**：夹具排除「编译期断言」以编译列表级+产物级双层断言满足；TidyCompleted 发布边界以工单分解证据（引用集/19/21/22 依赖链）rebuttal 成立——完整链条保留在本表与结单会话记录。

## 迁移保真说明（对照基线：Archive LaunchInventoryTidy）

- 原样迁移（仅命名空间/日志接缝改写）：InventorySolver、LayoutCandidate、ManualTidyService 事务主体、MainThreadDispatcher、HotkeySnapshot、TidyDiagnosticLog、UI 补丁反射注入骨架。
- DEV-V2-15 明示改写：ManualTidyService 策略参数化（TryPack 调用点改 BuildPlan，Validate* 签名 IReadOnlyList 放宽——行为不变）；UI 点击路径改走模块（原网络请求）；故障注入钩子（#if TIDY_TEST_HARNESS）随夹具排除删除。
- 拆分登记：`HotkeyRestoreEntry` 自旧 `TidyTransaction.cs`（联机事务件）拆入 `Tidy/HotkeySnapshot.cs`——单人热键恢复链需要它，事务管理器（PendingHotkeyRestore/TidyTransactionManager）随 DEV-V2-21。
- 不迁（夹具/联机/死代码）：AutoTestDriver、CommandTidy*、FaultInjectionTestRunner、FixtureValidator、TestFixtureSession、NetworkTestProbe、ShutdownTestProbe、ShutdownBarrier、ConvergenceCheckBehaviour、HotkeyResultWaitBehaviour、IndependentSnapshot、ManualTidyNetwork、ClientSessionNonce、ServerSessionRegistry、RequestAdmissionStore、PlayerOperationGate、TidyTransaction（TidyTransactionManager 部分）、TidyFaultCircuit(.Persistence)、TidyAdminAuth、LmnDependencyGuard、LaunchInventoryTidyPlugin、ItemsTryAddItemPatch、Properties/AssemblyInfo。

## 身份（双 CLEAN 后授予）

- 产物：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- SHA-256：`cacfa527bb4e593bd09885cbfa12997f4a192d601c6d08fc4270317b9b03b040`（350208 字节）
- 确定性：同源两轮 `-t:Rebuild` 逐字节一致（`identity-rebuild1.txt`/`identity-rebuild2.txt`/`identity-sha256.txt` 留盘）
- R2' 后身份复核：R2' 唯一生产外增量=测试工程断言（Program.cs 编译列表级排除断言），生产源零改动——R2' 后复测 SHA-256 与授予值逐字节一致，身份持续有效
- 绿测证据：`green-BetterUnturnedExperience.*.Tests.log`×7（全 PASS，2026-09-06，R2' 修复后复跑）
- 边界：不继承 13/14 发布批准；候选采纳由用户决定。**实机自验（点按钮 → 本地整理事务完成；关闭 → 原生回退；面板「背包整理」条目）待用户执行**——工单验收第 2/3 项的实机部分未在本票闭环，列入 RELEASES 行的待验收项。

## 实机验收（2026-09-06，验收后补记）

**用户单人实机验收通过**，原话留档：「我验证LIT功能无异常」。UMM 诊断包 `UMM-诊断包_20260906_183015` 留证：部署物 identity 行 sha256 与本候选逐字一致（`LogOutput.log:136`）；日志证据覆盖注册经宿主面（:165 accepted）/补丁安装（:139）/5 组按钮注入页 2–6（:535-540）/多次事务提交指纹守恒通过（:1333、:1561、:1787）/面板关闭原生回退（:2160）；全日志唯一 Exception 来自无关旧插件 SteamP2PFriends（:558）。完整证据表与观察项（降序 SameType 首次整理被安全拒绝=迁入前既有算法行为，fail-closed 按设计零副作用，移交策略治理票）见 **`acceptance-singleplayer-20260906.md`**。RELEASES 行 8 状态更新为**当前发布物**（单人环境范围；P2P/U3DS 绑 DEV-V2-21/24）。
