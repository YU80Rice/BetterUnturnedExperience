# DEV-V5-03 容器会话整理 — 双轴评审链（R1..R4）

日期：2026-09-15。规约：docs/agents/output-review-loop.md（红先行 + 双轴独立、每轮全新实例、Fresh-instance 规则）。
双轴类型：Standards=standards-reviewer，Spec=Spec-Reviewer（用户拍板，见记忆 [[bue-dual-axis-reviewer-types]]）。

## 终态绿证据链

- `red-compile-errors.txt`：红先行=17 项编译错（CS0246×9 类型缺失 + CS0103×2 + 其余，全为新容器 API 缺失）→ 实现后转绿。
- `red-mutation-M1..M6-run.txt`：六项突变各证红——M1 种类身份移除（3 红）/M2 版本闸移除（3 红）/M3 虚拟箱投影放行（1 红）/M4 事务先于重验（2 红）/M5 世界箱授权维度忽略（红）/M6 驾驶座检查移除（红）；全部还原后 `green-v503-group-run.txt` ALL GREEN exit=0。
- `green-fullsuite-{Plugin,ClientUi,Contracts,Network,Placement,Release,Settings}-run.txt`：7 套件 exit=0。
- `green-sln-rebuild-final.txt`：Rebuild 0 警告 0 错误。
- `gate-Verify-DeveloperHandbook/RefreshModelDeduped/SpecV4R9Ingested/TestFixturesTracked/TestRunnerHostDllsProvisioned-final.txt`：exit=0×5；`gate-Verify-NoUiTokens-core-final.txt` exit=0（Core 30 文件 PASS）。
  - 如实登记：`gate-Verify-NoUiTokens-contracts-final.txt` exit=1——唯一命中 = `ContractTypes.cs:425` 注释自述禁用词清单（V2-02 4a37d4b 引入，非本票产物；本票对 Contracts 零改动）。历史票 V5-02 的同名门禁记录实为缺参错误残留，本轮按 DEV-01/02 时代正确口径（Contracts+Core）执行并如实双记。
- 每轮冻结包：`review-freeze-r1/r2/r3/r4-*.diff.txt`（R1→R2、R2→R3 增量另存 `review-freeze-r2-increment-*.diff.txt`）。

## 轮次

### R1（双轴全新实例）
- **Standards CLEAN**（4 判断题）：①RefusedContainer 恒真死三元；②两处编辑粘连（签名与注释挤同行）；③ContainerProbeOverride 三目两处重复（域内 ForTests 缝先例，保留）；④PendingEntry 反推 IsContainerRequest 命名确认无漂移。
- **Spec NOT CLEAN**：
  - **A 缺口**：两 adapter 只按种类相等、未「分别处理」生命周期/权限事实（T4 Q1 要求箱子=单 opener/距离/锁/关箱、后备箱=驾驶座/离座/换座/车辆状态）；
  - **F 缺口**：容器标题栏几何/文案硬编码在 UI patch，未走「容器标题栏 adapter 提供布局」出口（T4 Q2）。
  - B/C/D/E/G/H/I/J 通过。

### R2（修复后复评，全新实例）
修复：LiveFacts 改按种维度（WorldOpenerIsRequester/WorldAccessAllowed/TrunkDriverAuthorized）+ CheckAccess 抽象（世界箱=opener+checkRot 锁组；后备箱=getVehicle+GetDriverPlayer 驾驶座；互不越界）；新增 LitContainerTitleBarAdapter 单源出口，UI patch 删本地容器常量；死三元/粘连修复；M5/M6 突变红证。
- **Standards 残留发现**：①SendHotkeyResult 参数行与 `{` 粘连未修（R1 漏修一处）；②RefusedContainer 死参数 commit 未删。
- **Spec NOT CLEAN**：①**离座/换座建模与事实链矛盾**——原版 revokeTrunkAccess→closeTrunk 先拆挂载，真链应呈现 ContainerClosed，测试却把它建成「会话在+驾驶座假→LostAccess」；②**虚拟箱「留下可诊断原因」未呈现**——UnsupportedDiagnostic 携带但从未进入日志/toast。

### R3（再修复，全新实例）
修复：R2 两项——驾驶座 cell 改措辞为「防御闸（正常时序先走 ContainerClosed）」+ 新增 kind=None/active=false 的离座/换座 ContainerClosed 事实链 cell；ContainerCapabilityAvailable 加 diagnostic 出参，投影拒绝日志行与 UI 藏起日志携带「（诊断=state-hooked）」。粘连/死参数清。
- **Standards CLEAN**（判断题 4 条具名递延 + 新增 3 条判断题：Data Clumps 五元组直传=域内 ManualTidyService 风格、Feature Envy 一次性转发、Message Chain 一层访问）。
- **Spec NOT CLEAN**：①**纯客户端把无参照挂载乐观投影成 WorldContainer 未如实标注**（虚拟箱状态钩子仅服务器可见，客户端物理不可识别——评审指出注释读起来像已验证）；②**toast 通道纯洁性需钉死**（诊断串不得进画面文案）。

### R4（终态，全新实例）
修复：探针远程分支改用原版同款参照（`PlayerInteract.interactable as InteractableStorage`——vanilla 仪表盘展示柜控件即此参照）做客户端分类（isDisplay 资产派生、客户端可见）；无参照时如实回退=声明照发 + 诊断 `remote-crate-identity-unverified:authority-revalidates` + 权威端 ClassifyCrate 实测 fail-closed（T4 Q4「按钮可见≠授权承诺」），注释不再声称已识别；新测试钉：toast 行无诊断串且仅六类中文原因、日志行含 state-hooked。
- **Standards CLEAN**：无硬违规。判断题汇总（具名递延，不阻断）：ContainerProbeOverride 三目×2（ForTests 缝先例）；LiveFacts 三布尔扁平；(LitContainerTidyKind)(byte) 双重转换×3（两枚举故意对齐）；五元组直传（域内风格）；titleBarPresent=true 一次性转发；view.Live 一层访问；admission 基建复用非 Divergent Change；「六类之外 7/8 两类」已注释区分（票面允许「至少能区分」）。
- **Spec CLEAN**：A–J 全通过（R1 两缺口、R2 两发现、R3 两发现的修复均判忠实）。

## 具名接缝缺口（宿主不可测面，实机随 DEV-V5-08）

1. **真机提交写面**：宿主 `Items.addItem` 触 `SDG.Unturned.Assets` 静态初始化（游戏外必抛）→ 事务成功路径经 `LitContainerTidyExecution.PageTransactionForTests` 同语义假事务钉死（计划消费/坐标落位/零修改结构序全真），真 removeItem/addItem+原版同步=实机验收。先例：DEV-V5-02 宿主 e2e 点击不可测。
2. **Glazier 容器按钮渲染**：宿主无 Glazier——按钮创建/显隐/拆除走既有 InjectButtonForTests/RemoveChildForTests 缝家族；真实画与翻转日志=实机。
3. **远程客机虚拟箱客户端识别物理限制**：状态钩子仅服务器可见（R3 §4.4）——投影按挂载事实+参照可得则分类（展示柜客户端可拒），不可得=声明照发+诊断+权威 fail-closed 带原因零修改（T4 Q4 允许面）；可靠适配器另票（T4 Q1 原文）。
4. **toast 渲染面**：`PlayerUI.message(NPC_CUSTOM)` 仅生产绑定（真实画按钮路径），宿主注入记录器断文案。

## 候选纪律

不产候选 DLL、不更新 audit/RELEASES.md、不授 CaseId（票面第 4 条）。对外契约仍 2.1 零扩面（全部 internal + 功能私有消息 7/8）；无新 FeatureId。

## 终态

Rebuild 0/0；7 套件 exit=0；新组 ALL GREEN；6 门禁按正确参数全过（NoUiTokens 双记含 Contracts 既有注释命中）；双轴 R4 CLEAN。
