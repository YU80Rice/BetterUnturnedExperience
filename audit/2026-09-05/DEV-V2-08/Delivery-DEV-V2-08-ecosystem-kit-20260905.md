# 交付报告 — DEV-V2-08：LIT/LIR/LHT 生态迁移验证（交付 kit + 采集准备）

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-08-lit-lir-lht-migration-verification.md`（claimed 2026-09-05）
> 阶段：V2 第一阶段实施第 8 票（/implement；性质 = 验证与证据，**BUE 与三插件源码零修改**）
> 对照：spec「Implementation Decisions：生态与前置」（`../spec-V2-phase1-lmn-adoption.md` L105-108）；DEV-V2-07 交付先例；RELEASES 行 6 注记「DEV-V2-08 注意」

## 1. 交付内容

本票不做功能（01–07、09、12、13 已完成），交付三件事：

1. **交付 kit 五件**（`audit/2026-09-05/DEV-V2-08/kit/`，身份见 §2）：
   - `BetterUnturnedExperience.dll` = **RELEASES 行 6 当前发布物**（DEV-V2-13 候选，`35670269…aef6`，275456 字节）——本票零修改复用，不产新候选；
   - `LaunchMultiplayerNet.dll` = LMN 仓库 `bin\Release` 权威 v5.0.0.0（`06d8a454…3055`，与 DEV-V2-07 kit 记录逐字节一致）；
   - `LaunchInventoryTidy.dll` / `LaunchInPlaceReload.dll` / `LaunchHordeTracker.dll` = 归档验收候选源码（`Archive\2-未闭环验证项目`）本票 Release 重建（哈希与确定性见 §2/§3）。
2. **实机验证手册** `DEV-V2-08-ecosystem-handbook.md`：三 CaseId（每插件一个）绑定行 6 身份；部署矩阵（客户端 + U3DS，五件套逐一 certutil 核对）；三环境分步表格（LIT/LIR/LHT 各自的触发操作与日志锚）；预期日志行速查表；证据提交结构与失败处理协议。
3. **构建环境重建记录**（§5）：归档区引用路径经 junction + Libs 装配恢复，未触碰任何插件源码文件。

## 2. 身份（kit 五件套；三环境必须绑定同一份）

| 件 | SHA-256 | 字节 | 来源 |
|---|---|---|---|
| `BetterUnturnedExperience.dll` | `3567026930757ffbc5d40b2da12b148f470dba806b5ca08abf14ac6e4a2caef6` | 275456 | RELEASES 行 6（DEV-V2-13 候选，两轮重建一致已有归档） |
| `LaunchInventoryTidy.dll` | `7e35d7c7b90c6e25003f0644f746451d6bb75c193e539e86114b37b0793a5417` | 151040 | 归档源码本票重建（轮 A/B 逐字节一致） |
| `LaunchInPlaceReload.dll` | `6653035be15f5d88649e97442d9f41a55d9b4bc20bd96a37ac1434d25c68ac90` | 52736 | 同上 |
| `LaunchHordeTracker.dll` | `6b935f5c47572004ffc8c0c1c448bae8d0a60c840b757faa5d263a75925ef995` | 36864 | 同上 |
| `LaunchMultiplayerNet.dll` | `06d8a45438c09fea65f3800bf01a7efb9302f2421bd76aa386f8828701a63055` | 68096 | LMN 仓库 `bin\Release`（v5.0.0.0 权威；构建全程哈希未扰动，见 §3） |

**CaseId（每插件一个，采集期间不得更换）**：

| CaseId | 插件 | 命名频道 |
|---|---|---|
| `DEV-V2-08-LIT-20260905` | LaunchInventoryTidy | `com.yu80rice.launchinventorytidy.net` |
| `DEV-V2-08-LIR-20260905` | LaunchInPlaceReload | `com.yu80rice.launchinplacereload.repack` |
| `DEV-V2-08-LHT-20260905` | LaunchHordeTracker | `io.github.yu80rice.launchhordetracker.horde-status` |

**绑定口径（按 RELEASES 行 6 注记具名）**：行 6 为轻量视觉链候选，身份锚 = DLL SHA-256 + 两轮确定性重建，**不立 BuildIdentity、无 candidate.json**；本票三 CaseId 的「绑定 V2 网络层 DLL 的 LoadSetIdentity」义务按该注记执行 = 绑定 `35670269…aef6` 全值 + 三环境部署指纹逐件 certutil 核对（手册 §2）。不得绑旧候选拼接证据。

**版本口径（票面 vs 归档源码，具名以免误读）**：票面「LIT v3.0.1 / LIR v3.0.0 / LHT v3.0.0」指各自**已冻结的历史发布线**；验证对象是归档区**验收候选构建**，BepInEx 元数据均为 `0.0.0` 占位——LIT 显示名「LaunchInventoryTidy [验收候选 / LMN V5 命名频道]」、LIR 显示名「LaunchInPlaceReload [未发布开发构建]」、LHT 显示名为**「LaunchHordeTracker」**（无括号注记；「未编号验收构建」是其 CHANGELOG 的阶段标题，非 BepInEx 显示名）。各自 CHANGELOG 明文：正式版本号待三环境验收通过后授予。本票的验证正是解锁版本号的验收环节。

## 3. 构建矩阵（本轮门禁，log 在 `audit/2026-09-05/DEV-V2-08/`）

| 项 | 结果 |
|---|---|
| LIT Release `-t:Rebuild`（`build-LIT-rA.log`/`-rB.log`） | 双轮 0 error / 0 warning；DLL 哈希逐字节一致 |
| LIR Release `-t:Rebuild`（`build-LIR-rA.log`/`-rB.log`） | 同上 |
| LHT Release `-t:Rebuild`（`build-LHT-rA.log`/`-rB.log`，`-p:BuildProjectReferences=false`） | 同上 |
| 确定性 | 三件相邻两轮（rA/rB，均留 log）逐字节一致（csproj `/deterministic+`）；同日另有两次复建未留 log，哈希亦一致（过程注记，不作证据） |
| LMN 权威二进制未扰动 | 构建前后 `bin\Release\LaunchMultiplayerNet.dll` 哈希均为 `06d8a454…3055`（LHT 的 ProjectReference 已用 `BuildProjectReferences=false` 规避重编译覆盖） |
| 工具链 | MSBuild 18.9.1.35102 \| .NETFramework 4.7.2 \| CSharp 10 |

## 4. seam 缺口具名（本票无红测的说明）

output-review-loop 要求红测先行。本票 BUE 与三插件源码零修改、无新生产 seam，主体是生态验证装备：三插件重建件的正确性判据 = 确定性复建哈希一致 + 实机日志行为本身（插件是有意被观察的被测对象，其源码不在本票修改范围）；kit 身份判据 = §3 门禁。**未做的红测：无实现缺陷可红**；此缺口具名登记，不静默跳过。若实机验证发现三插件缺陷，按票面口径另立修复票（红测先行 + 双轴 CLEAN 后出新构建），不在本票内修。

## 5. 构建环境重建记录（可复算）

三插件 csproj 的相对引用在其原仓库布局下有效，归档搬运后路径悬空。本票**未改任何源码/csproj 文件**，以目录供给恢复：

1. junction `Archive\2-未闭环验证项目\LaunchMultiplayerNet` → `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`（LMN 仓库；满足 LIR HintPath `..\LaunchMultiplayerNet\bin\Release\…` 与 LHT ProjectReference）。
2. 新建 `Archive\2-未闭环验证项目\Libs\`：9 件引擎/BepInEx 引用（0Harmony、BepInEx、Assembly-CSharp、UnityEngine、UnityEngine.CoreModule、UnityEngine.TextRenderingModule、SDG.Glazier.Runtime、com.rlabrecque.steamworks.net、Newtonsoft.Json——自外层 `Libs\` 复制）+ `LaunchMultiplayerNet.dll`（**自 LMN 仓库 `bin\Release` 复制的权威件**），共 10 文件；外层 `Libs\LaunchMultiplayerNet.dll` 是旧 V5 构建，已知不可用，故不取。
3. LIT/LIR 编译命令行（build-rA.log 内 csc 参数）可见全部引用解析到上述供给路径，`/deterministic+ /langversion:10`。

## 6. 票面缺口具名（Spec R1-H1：「不再需要独立 LMN DLL」本票不可验证，不改写验收语义）

票面 Scope 第二条「确认三个插件作为 BUE 官方功能（吃掉消化）工作：不再需要独立 LMN DLL，走 BUE 注册/生命周期/隔离路径」与票面「不做：不修改三个插件源码」**在票内互斥**：三插件均为 `[BepInDependency(LMN_GUID, HardDependency)]` 且 IL 直接调用 `LaunchMultiplayerNet.ModTransport` 的 `RegisterNamed*/SendNamed*`——不重写插件源码就不可能去除 LMN DLL、也不可能改走 BUE 注册/生命周期/隔离路径。对 Spec R1-H1 的处置（如实具名，不静默改口径）：

1. **本票交付并按验收条件闭环的是票面「验收条件」三条**：①三插件在 BUE 网络模块下实机收发正常——按 **DEV-V2-12 定案的接管决策核两态契约**执行：live 方向（LMN 原生前缀存活 = 实机常态）BUE 决策核对每个 MOD/LMN2 帧行使**放行**决策、由 LMN 原生路径**恰好一次**派发（正向锚 `lmn2-frame-release result=released decision=lmn-native-dispatch`，一次性；`takeover-patch result=installed` 证决策核在场；`lmn2-delegate` 出现 = 回归信号）；inert 方向 BUE 独派发兜底。LMN 保留为编译/JIT 依赖 shim 与帧编解码/派发器（live 方向）；②T4 Q4「已知生态」维度验证义务解除；③证据归档。
2. **「不再需要独立 LMN DLL + 走 BUE 注册/生命周期/隔离路径」= 官方纳入终态**（规格「生态与前置」+ 方向指令：LMN/LIT/LIR/LHT 并入 BUE 内部、玩家只部署单 DLL；对应 User Story 18）。它需要按 BueNetworkApi 重写三插件网络层并并入 BUE 单 DLL——**超出本票「不做」边界，应另立「三插件官方纳入实施」票承接**（红测先行 + 双轴 CLEAN），建议在本票闭环后建票。
3. 该缺口**不是迁移缺陷**：T8 盘点口径「已迁 V2」= 已迁 LMN V2 命名频道（三插件 3/3 完成）；官方纳入是下一阶段实施，非本次验证发现的功能错误。若实机验证发现真正的功能缺陷，仍按票面口径另立修复票。

## 7. 双轴独立审查（R1 → 修复 → R2）

**R1（两个独立子代理：standards-reviewer / Spec-Reviewer，2026-09-05）**：**双双 FINDINGS**。

- Standards 轴：**[M]×4**——手册 P3 把 LIR dispatcher 汇总窗口误写 15 秒（源码 `DiagnosticIntervalSeconds=5f`）；`network-session-not-ready` 是 LIT 诊断类别名不进日志（实际行 = `[Tidy] 客户端尚未收到有效服务端 session challenge；本次整理请求未发送。`）；U4 的 `/horde` 在专用服务器仅管理员有回复（非 admin 静默），手册未写 admin 门；LHT BepInEx 显示名张冠李戴（实际 = `LaunchHordeTracker`，「未编号验收构建」是 CHANGELOG 阶段标题）。**[L]×2**——「前置两轮复建」无对应 log（改为仅以留证轮次作证据）；Libs 件数 8/9/10 计数错误。判断题 J1–J4 不阻断（LIT 模板 `…` 缩写、S1/§6 锚集合不一致、委托锚双端漏采、归档区 git 相对 HEAD 的历史脏态说明）。
- Spec 轴：**[H]×1**——「不再需要独立 LMN DLL」被交付报告 §6 重解释为「shim 口径」，属以评论改写验收语义；应如实记为票内不可验证缺口 + 另立官方纳入票（→ 本报告 §6 已按此重构）。**[M]×2**——委托锚为会话级一次性、无法分别绑定三 CaseId（→ 手册 P5 改双端各恰一条 + 每插件组合判据 + 可选严格加采）；U3DS 连接/管理员授权/LHT 信标道具与满月前提未写明（→ 手册 §4/§5 补齐）。观察 O1–O3 不阻断。

**修复（本轮，全部文档侧；kit 与源码零变更）**：Standards 六条全改（P3 窗口 5 秒、session challenge 实际行、U4 admin 门 + §5 管理员授权准备、LHT 显示名更正、确定性留证口径收窄为 rA/rB、Libs=9+1=10）；J2/S1 锚集合按源码序补齐 `[Network]`/`[Runtime]`；J3 采纳为 P5/U5 双端锚。Spec H1→§6 重构、M（委托锚）→P5 组合判据+严格可选、M（操作前提）→§4/§5 补齐。J1（LIT 模板 `…` 缩写）具名保留：模板行为子串匹配提示，非逐字断言。

**R2 复审（两个全新独立子代理，紧凑范围 = 修复核验 + 增量封闭性）**：**Standards CLEAN**（六条修复逐条落盘核实、五件哈希复算全对、J1 具名保留属实；具名残余 = 速查表 LHT 行压缩与「每会话恰一条」简写——已随本轮顺手修复：速查表补全 LHT 五锚按 Awake 序、委托锚行改「每端每会话恰一条 + 缺行判不通过」）。**Spec FINDINGS（两条）**：[H] 工单首条认领评论仍残留「shim 口径执行」的语义改写，与 §6 缺口定性矛盾；[M] P5 组合判据未堵「首帧委托自 LIT、他件原生扛」反例空间，会话级锚支撑不了每 CaseId 独立结论。

**R2 修复（本轮，全部文档/工单侧）**：[H]→认领评论第 (2) 条订正为「票内互斥、本票不可验证、缺口表述」，并加注初版表述作废；[M]→P5 判据重构为「**会话门**（takeover 在场 + 委托锚 ≥1，缺失则该会话三件全判不通过，不得以行为锚单独放行）+ 组合判定」，并写明互斥原理（委托是按帧统一路径、解析失败为会话级持续态：锚缺席 ⇔ 全部放行原生扛；锚在场 ⇔ 已注册频道帧均在决策点消费，两者不可并存）；速查表同步。严格逐插件加采仍为可选项。

**R3 复核（两个全新独立子代理，紧凑范围 = [H]/[M] 修复核验 + 增量封闭性）**：**Spec CLEAN**（[H] 终判：工单三处定性一致、无验收语义漂移，本票严格落在验收条件三条；[M] 终判：会话门消除反例空间、与 DEV-V2-11 委托锚定义相容、逐插件独立锚显式可选化；增量封闭：无票外承诺/无弱化）。**Standards FINDINGS（一条）**：交付报告 §8.6 边界第 6 条仍是 R2 前旧「组合判据」表述，未同步会话门口径（采集者只读边界声明会漏 fail-closed 门）。

**R4 复核（Standards 单点）**：**CLEAN**（§8.6 已同步会话门 + 组合判定口径，与手册 P5/速查表逐点一致，无旧口径残留）。

**R5 阶段（部署后证据准备复核中，agent 自查发现材料性口径错误 → 修正 → 重审）**：R1–R4 的机制叙事建立在 **DEV-V2-11 的委托口径**上（LMN2 帧经 BUE 前缀反射委托后短路消费、「LMN 自身前缀不运行」）——但行 6 候选包含 **DEV-V2-12 修复**：live 方向（LMN 原生前缀存活 = 实机常态）BUE 决策核**放行**帧、由 LMN 原生路径恰好一次派发，正向锚 = `lmn2-frame-release result=released decision=lmn-native-dispatch`（一次性 latch，`NetworkModuleAdapter.cs` L455-468）；`lmn2-delegate result=delegated` 仅属 inert 兜底路径，live 会话出现 = 回归信号（12 复测已实证 delegated=0）。源码 grep 实证后修正：手册 P5/U5 会话门与速查表、交付报告 §6①/§8.6 全部改用 release 锚三件套（takeover installed + release 恰一条 + delegated 零出现），并补「无重复派发」判据（12 修复的恰好一次语义）。缺口具名（§6 第 2/3 条）不受影响。**R5 双轴重审（两个全新独立子代理）**：源码锚五处会话门对齐、kit 未扰动获确认；**仍 FINDINGS**——Standards [M]×3+[L]×1（手册注 2 与交付报告 §8.2 仍把委托当远端正向锚、手册 §1 LMN 角色行仍写「路由被 BUE 短路」、§2.3 措辞）+ 无重复派发判据对 LHT 不可操作；Spec [H]×2（同两处委托残留属 Wrong Implementation/报告内部矛盾；恰好一次判据未与 DEV-V2-12「每 seq 恰一次」同构——LHT epoch/seq 缺判据、LIR dispatcher 汇总不能证唯一）+[M]（§8.2 委托残留）。

**R5 修复（本轮，全部文档侧）**：手册注 2/§8.2 改 release 决策点锚；手册 §1 LMN 角色行改 12 两态契约表述；§2.3 措辞改「决策点」；组合判定②按插件细化——LIT=reqId 唯一、LIR=toast 恰一次+dispatcher 相称辅证、LHT=广播行 (epoch,seq) 单调无重复+客户端 (epoch,sequence) 快照幂等回退拒绝，并明写结构保证（delegated 零出现 ⇒ live 唯一派发者=LMN 原生）；P6 行同步。**R6 复核**：**Standards CLEAN**（六处落盘与源码锚一致、全文 grep 无委托/短路活口径残留、LHT epoch/seq 与源码/README 同构）；**Spec 一条**——LHT「无重复派发」判据不可操作（服务器广播行只记发送一次、客户端 HUD 幂等掩盖重复），建议改用客户端「收到 Update/Clear」日志 (epoch,seq) 重复键检查。

**R7 单点收口**：首版改写误称「重复包在 TryStoreReceivedSnapshot 静默拒绝早于日志、日志层观测不到重复」——Spec R7 源码纠正 + agent 亲证：`PendingHordeSnapshot.Store` 为 void 静默忽略旧包，但 `TryStoreReceivedSnapshot` 仅在插件关闭态返回 false，`收到 Update/Clear` Debug 行**每帧必打**，重复派发在客户端日志层**可检**。已按事实改写组合判定② LHT 子句（(epoch,seq) 重复键为零 = 可操作的重复派发检查 + mailbox 幂等辅证 + 会话门结构保证）与 P6 行。

**R8 双轴终核**：Spec CLEAN（幂等分层归因纠正后逐句对齐：Store 仅 pending 时拒、跨 drain 由 PublishIfNewer 拒、两层不影响日志）；Standards CLEAN（LHT 子句/P6 与源码逐句对齐无过度声明、R6/R7 记录与现文一致、kit 五件哈希未扰动）。R8 具名残余（速查表补客户端收到行、§7 收口补记）已随手修复。**审查循环闭环：R1（双 FINDINGS）→ 修复 → R2（S-CLEAN/Spec-2）→ 修复 → R3（Spec-CLEAN/S-1）→ 修复 → R4（S-CLEAN）→ agent 自查发现材料性机制口径错误（R5 段）→ 修复 → R5（双 FINDINGS）→ 修复 → R6（S-CLEAN/Spec-1）→ 修复（含 R7 对 agent 表述失误的源码纠正）→ R8（双 CLEAN）。双轴 CLEAN。**

## 8. 边界声明

1. 三环境实机证据只证明**同 kit 同哈希**下三插件命名频道功能在 BUE 网络模块（接管态）下正常；**不自动授予发布授权**，不改变行 6 状态；三插件版本号授予与官方纳入实施由后续票决策。
2. SP（单人/听主机）内 LIT 请求走 LMN 本地 loopback、LIR 主机路径本地事务、LHT 主机 HUD 本地权威——SP 证明插件功能与注册链，**不产生远端帧**；BUE 决策点锚（`lmn2-frame-release result=released decision=lmn-native-dispatch`）只在存在远端对端（P2P / U3DS+客户端）时出现。三环境分工见手册。
3. U3DS 无 UI：LHT 不进 HUD 路径；LHT 广播证据以服务器 Debug 日志 + 客户端 HUD 截图组合判定。
4. LHT 网络验证需真实尸潮信标事件（满月夜），采集节奏允许与 LIT/LIR 分离执行；三件齐备才闭环关票。
5. 本票不运行资格门禁 runner（无 candidate.json 可校验，见 §2 绑定口径）；证据完整性由部署指纹 + 日志锚 + 截图清单承担。
6. 每插件 CaseId 的「经 BUE 接管决策点」判定采用**会话门 + 组合判定**（手册 P5）：会话门先决（该端 `takeover-patch result=installed` 在场且 `lmn2-frame-release result=released decision=lmn-native-dispatch` ≥1 条且全程零 `lmn2-delegate` 行；缺门则该会话三件 CaseId 全部判不通过，不得以行为锚单独放行），会话门通过后按各插件自身行为锚 + 无重复派发 + 零丢弃信号逐件判定；逐插件独立 release 锚为可选严格加采。

## 9. 提交清单

票（含评论）+ 手册 + 本报告 + `identity-sha256.txt` 入库；构建 log、kit DLL 产物与实机证据数据不入库（随 04/05/06/07 先例，报告引用 audit 路径）。
