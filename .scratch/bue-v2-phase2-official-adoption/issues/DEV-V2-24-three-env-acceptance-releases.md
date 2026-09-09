# DEV-V2-24：三环境实机验收 + RELEASES 加行 + 真机手册（终票）

Type: task
Status: claimed（2026-09-08，/implement 会话；前置 20/21/22/23 四票已核实 resolved；采集 kit + 手册就绪后用户就 D0 拍板 = **D0-b 修复轮**——BII 面板显示名改「更好的物品交互」，红测先行 + 全套 7/7 PASS 0 警告，新候选 `3cbd6268…9e4d`（前身 7d5dd3b5…c223 作废）；待实机采集 → 复核 → 双轴 → RELEASES/真机手册/关票）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-20（LHT）、DEV-V2-21（LIT 联机）、DEV-V2-22（LIR）、DEV-V2-23（防双装+文档）
Spec: `../spec.md`（Solution「到达标准」、Testing Decisions「实机验收面」两处）

## What to build

目的地四条在实机闭环：玩家只装一个 `BetterUnturnedExperience.dll`（无独立 LMN、无三插件 DLL）就能在单人 / SteamP2PFriends / U3DS 获得四个官方功能；BUE 成为玩家侧唯一推荐交付入口；RELEASES 出候选行；真机手册落盘；证据包绑定身份后授予 CaseId。

## Scope

- 裸 BUE 单 DLL 三环境验收：四官方功能（更好的物品交互 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报）全部可用；面板显示四件官方中文名；三插件 = BueNetworkApi 生产绑定第一批真实消费者实证。
- 共存承诺：BUE + 独立 LMN 部署下未知 V1 旧插件继续收发（裸 BUE 无独立 LMN 除外，为已承认边界，随证据记录）。
- T7 实机五项清单：改名实机对照；Mono `LoadFile` 二次探测；同版本程序集最终谁保留；**不同 GUID + 同程序集名（红测 + 实机双证，红测不能替代实机结论）**；Preloader `AssemblyResolve`。
- RELEASES：候选行（绑 LoadSetIdentity，延续未公开分发 + 每票候选节奏）。
- 真机手册：玩家安装/升级注意事项（含从「BUE+LMN+三插件」旧部署的升级要点与独立 LMN 保留语义——旧 V1 插件共存仍需独立 LMN 在场）。
- 证据门禁：三环境证据包 + T7 实机记录统一绑定 LoadSetIdentity；双轴审查 CLEAN 后授予 CaseId 并关闭本票（output-review-loop 第 4 步）。

## 验收条件

- [ ] 三环境证据包齐全且绑定同一 LoadSetIdentity（裸 BUE 单 DLL）
- [ ] T7 五项实机记录落盘，其中「不同 GUID + 同程序集名」红测 + 实机双证
- [ ] V1 共存证据（BUE + 独立 LMN 部署下旧插件收发）
- [ ] RELEASES 候选行 + 真机手册落盘
- [ ] 双轴独立审查 CLEAN；CaseId 授予；地图/规格同步终态

## Comments

### 2026-09-08 认领 + 采集 kit/手册就绪（/implement 会话）

- **认领**：前置 20/21/22/23 四票核实 resolved；本票源码零修改（沿 DEV-V2-07 先例 = 验证与证据票）。候选 = DEV-V2-23 候选 `7d5dd3b5…c223`（533504B），本票审计轮两度 `-t:Rebuild` 字节一致（`audit/2026-09-08/DEV-V2-24/identity-rebuild1/2.log`）；采集 CaseId = `DEV-V2-24-20260908`，RELEASES 换标随实机验收。
- **采集 kit**：`audit/2026-09-08/DEV-V2-24/kit/out/` 六件——候选 BUE、独立 LMN、LmnEcosystemFixture（V1 旧插件替身）、NoOpFixture（T7-1 改名对照的第三方消费方）、**BueSameAsmProbe A/Z 双变体**（T7-4 双证仪器：AssemblyName=BetterUnturnedExperience、版本 0.0.0.0 与 BUE 相同，独立 GUID `io.github.yu80rice.aaprobe/zprobe.sameasm`——GUID 拓扑序分别排在 BUE 前/后，控制副本程序集进入 AppDomain 相对 BUE Awake 的时序；证据仪器非生产代码）。
- **证据包骨架**：`audit/2026-09-08/evidence/DEV-V2-24-20260908/`（candidate/cases×4/t7/coexistence，六份 case 模板 TODO 占位 fail-closed，沿 08 轻量链 + 07 结构）。
- **采集手册**：`audit/2026-09-08/DEV-V2-24/DEV-V2-24-three-env-acceptance-handbook.md`（十节：配置 A 裸 BUE 三环境四功能 + 防双装真机基线 / 配置 B V1 共存 / 配置 C T7 五项独立会话；全部锚行为源码现行串逐一核实）。
- **D0 采集前拍板点（待用户）**：BII 面板条目显示名为英文 `"Better Item Interaction"`（`ClientUiCompositionRoot.cs:126`），spec story 3 冻结四件中文名含「更好的物品交互」。处置二选一：(a) 接受现状→story 3/规格显示名措辞随结单修正登记；(b) 修复轮（改一处常量→红测+双轴→新候选→届时再采集）。未拍板不采集（中途换候选=全量重采）。
- **seam 缺口具名（沿 07 先例）**：本票无新生产 seam、无实现缺陷可红，红测面不变；kit 探针/fixture 的正确性判据 = 实机日志行为本身（证据仪器）。全套 7 测试运行器复跑归结单轮。

### 2026-09-08 D0 拍板 = D0-b 修复轮（用户拍板「B」）——BII 面板显示名中文化

- **判别内容**：spec story 3 与「功能身份与显示名」节冻结 BII 显示名 =「更好的物品交互」，面板实况为英文 `"Better Item Interaction"`（`ClientUiCompositionRoot.cs:126`，Legacy DEV-15B 期遗留）。
- **修复**：该一处显示名常量改「更好的物品交互」。REG-ACCEPT 启动日志标签 `Better Item Interaction featureId=…` 非面板显示名，不属显示名冻结面、维持原样（具名裁定，避免顺手扩大改动面）。
- **红测先行**：Plugin.Tests `AssertLitSingleplayerPath` 面板块新增断言（面板目录以官方中文名投射 BII 条目）→ **观测红**（FAIL 于新断言，`redtest-run.log`）→ 改串 → **绿**（`greentest-run.log` exit=0）。
- **全套门禁**：解决方案 Release 重建 0 错误 0 警告（`sln-rebuild-fullsuite.log`）+ 七运行器 7/7 exit=0（`fullsuite-run.log`）。
- **候选重授**：新候选 `3cbd62687bf765c618eaa5b6762c1172c7de022b6a50dc64ae1b0bdd0d399e4d`（533504B 两轮 Rebuild 字节一致，CaseId **`DEV-V2-24-CANDIDATE-20260908`**）；前身 `7d5dd3b5…c223`（DEV-V2-23-CANDIDATE-20260908）作废，kit/out 与归档、手册、六模板已换绑。采集 CaseId 不变 = `DEV-V2-24-20260908`。
- 增量 diff：`audit/2026-09-08/DEV-V2-24/round1-increment.diff`（双轴审查标的）。修复轮提交 = **`e70b7c4`**（候选来源快照 = e102935 + 本增量）。
- **双轴审查闭环（判词存档 `audit/2026-09-08/DEV-V2-24/review-rounds.md`）**：R1 Standards FINDINGS（2 SMELL——手册 S2/§10 与 sp/p2p-client 模板残留 D0 开放措辞；1 INFO——提交 hash 回填）/ Spec R1 CLEAN → F1-F3（aae2c99，仅文档）→ **R2 双轴全新实例双 CLEAN（Standards 0/0/0 / Spec 0/0/0）**。D0-b 修复轮审查面闭环；候选 `3cbd6268…9e4d` 为已审增量产物，可按手册开始实机采集。

### 2026-09-08 P2P 实机发现两起 LIT 真机缺陷（F-A/F-B）——采集停止，转修复轮

- **进展**：sp 单人 case 已通过并归档（8853cf8）；P2P 双端（本机 Host + 用户 VM Client）实测 **LIR/LHT 通过、LIT 两项 FAIL**（证据 `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-{host,client}/`，含幽灵贴图截图）。
- **F-A（客机整理全程不可用）**：Host :2889 采纳客机会话（gen=2）即拍签发 challenge → :2890 定向发送 `result=LocalTransportUnavailable` 失败，**全程无重试**（恰此一条发送失败）；Client 侧 80 条「尚未收到有效服务端 session challenge」拒绝，RequestTidy 从未发出。反证：同会话 LIR 定向发送（Host :4610 `-> 客机 RepackSuccess`）与 LHT 组播（`广播 Update result=Sent`）均成功——失败是暂时性/状态性，一次重试即可恢复。候选 `3cbd6268…9e4d`。
- **F-B（主机本地整理幽灵贴图堆叠）**：Host :5134-:5137 本地路径提交成功（`placed=3 指纹守恒验证通过`），但 UI 多容器幽灵贴图堆叠（截图存档）；SP 同路径无此现象——本地提交后的界面刷新/预览清理路径 P2P 差异，根因待查（修复轮代码定位）。
- **处置（手册 §10 协议）**：停止采集、现场已保留（两端 LogOutput.log + 截图归档）；修复走 real-machine-test-loop：红测先行（LIT 挑战签发失败重臂缝 + F-B 根因）→ 双轴 CLEAN → 新候选 → **换绑后全量重采（含已通过的 sp/P2P LIR/LHT 项，不拼接）**。本票关票顺延。

### 2026-09-08 修复轮闭环（F-A + F-B2 双轴 R3 双 CLEAN）——候选 v3 重授，F-B1（幽灵）转压测定根因

- **F-A 已修**（`3db75c0`）：challenge 签发失败回滚采纳（DropSession 孤儿 token + liveSessions.Remove）→ 下一拍重发现重发，镜像异常路径 B3 语义；红测新组「挑战发送失败重臂」三断言（红=恰②③，`fixfa-red-run.log` → ALL GREEN 6 组）。
- **F-B2 已修**（`f89194f`+F1 `03b661f`+F1b `fe3e3cc`）：BII surface 隔离锁存去粘滞+可见化——根因=三条静默路径任一单帧瞬态失败即永久锁存 `isolated=true` 且零日志（第二次整理后强化渲染全灭）；`TransientIsolationGate` 去抖门（连续 60 帧持续失败才隔离，健康帧复位自愈）+ 首失败/恢复/锁存三态一次性诊断行（003 走 EmitDiagnosticOnce/Error，004 走 Runtime/Debug——F1 修正 D2 通道污染）；红测 `AssertTransientIsolationGate`（编译红→绿）。
- **F-B1（幽灵覆盖层）未在本轮修复**：诊断数据排除数据层复制（插桩计数整理前后每页一致），幽灵=BII 陈旧覆盖层，与触发时序相关（原始会话撞上关开过渡窗口+ESC 暂停）；转新候选压测定根因。
- **审查链**（判词存档 `audit/2026-09-08/DEV-V2-24/review-rounds.md`）：R2 Standards CLEAN（2S 均具名，S1 采纳/S2 具名延期）+ Spec NOT CLEAN（2D：锁存行双发/004 行被 003 sink 污染）→ F1+F1b → **R3 双轴全新实例双 CLEAN**（Standards 0B（唯一 SMELL 已在 F1b 修复）/ Spec 0/0/0；Spec R3 首派发基础设施零输出作废留痕，判词来自重派新实例）。
- **候选 v3 重授**：`c9b6b6e40684df04b6141bef1f220a84d49dd96dfa7fae228ff51696e526eb86`（535552B **三轮** Rebuild 一致，CaseId 仍 `DEV-V2-24-CANDIDATE-20260908`）；前身 7d5dd3b5…c223 与 3cbd6268…9e4d 作废；kit/out、归档、identity-sha256.txt v3、手册 §1、六模板换绑；历史 case（sp/p2p×2）加「已归档待重采」横幅。
- **下一步**：新候选部署（含 VM）→ F-B1 幽灵压测（提复现率：多次关开+整理+ESC 暂停时序）→ 全量重采（手册十节，绑 v3 哈希）。

### 2026-09-08 F-B1 根因突破——SPF InventoryUI-Reconcile = 死亡触发器 + 幽灵清除者；F-C 登记；litfb7 决定性插桩部署

- **重大勘误（13 包 assembly-identity 矩阵核实）**：litfb5 构建（58a5dc42）**从未在任一端运行**——210433 主机实跑 litfb4（CFE5080F）、210425 客机仍是候选 v3；195933/195934 两端实为候选 v3（无探针）、204042 主机为 litfb3。**「litfb5 全零=适配器无辜」结论作废**；仍有效结论：litfb3（组合层零异常零跳过）与 litfb4（拖拽适配器 catch 零异常）为真实阴性。诊断包统一落盘于 UMM publish 目录（启动器工作目录），已全部重挖。
- **死亡链钉死（210433 主机日志 + SPF 源码 `SteamP2PFriends/Patches/ListenHostInventoryUiProjectionPatch.cs` 互证）**：拖#1 放置拍点（日志 :3985-:3995）正中 SPF `[InventoryUI-Reconcile] repaired page=2 authoritative=3 renderedBefore=5 pendingBefore=1`（:3986）与 page=3（:4017）——SPF 在主机 ReceiveDragItem/ReceiveSwapItem/ReceiveDropItem Postfix 上比对原生 SleekItems 投影与权威 Items，不一致即 `clear()+resize()+重加` **整页重建**。三症状同源：幽灵层=原生 listen-host 投影缺陷的多余元素（SPF 注释自证：pending 队列延迟创建+按坐标移除；LIT 整理批量移动为放大器，与 BUE 无关）；丢物瞬间幽灵消失=SPF 修复重绘（用户实测吻合）；BII 死亡=重建不换 SleekItems 实例→adapter 现行 `IsDispatchedSurfaceCurrent` 实例级比对全部通过→**零重派发**（日志仅一次 surface 派发爆发），组件侧经 `TryCreateCandidateInput` 持久失败→`HidePreview()`（default=Hidden+**None**，与 :3996-:3997 `state=Hidden placementReason=None` 逐字吻合）静默死亡。重开背包恢复（重新派发重绑）、不重开即死，与用户观测一致。
- **F-C 登记（独立缺陷）**：LitTidyNetService.cs:420 `定向发送未送达` WARN 在 210433 主机刷 **1199 条**（generation=2, LocalTransportUnavailable，:1301 起）——会话级定向发送失败无重试/无熔断/无限额。修复轮处理（与 F-A 同域不同路径：F-A 修的是采纳拍 challenge，F-C 是既成会话的持续发送）。
- **litfb7 决定性插桩**（`faf3c0a6…63e7`，诊断构建非候选；`diag/litfb7-identity.txt`）：`TryCreateCandidateInput` 全部 6 类拒绝谓词具名（`LastCandidateRejectReason`）+ OnDragUpdated 每代际入口标志行（passThrough/open/sink/enhanced/canRun/cur）+ flag-hide/guard-hide/occupancy-failed 探针 + Hidden+None 时每代际一行 `hidden-detail rejectWhy=…`；全套 7/7 PASS 0 警告；已部署本机（哈希两端核验一致）。
- **下一轮协议（决定性一轮）**：用户同前序步骤复现（进世界→开背包→客机加入→再开→整理→拖放）→ 日志 `hidden-detail rejectWhy=` 直接命名持久失败谓词 → 修复轮（内容签名检测+页级重派发重绑，红测先行）→ 双轴 → 候选 v4 → 全量重采。

### 2026-09-08 F-B1 修复轮闭环（litfb7 决定性数据）——候选 v4 重授；F-C 具名延期

- **决定性数据（litfb7=FAF3C0A6…63E7 两端核验;host 225112/client 225127）**：入口标志全好（open=True sink=True）→ `previewPresenter.Update` 内部抛出异常 → `OnDragUpdated` 的 catch（**第 4 条静默路径,litfb3/4/5 均未覆盖**）→ `runtime.Isolate()` → 清理链 `CleanupUiAndDrag`（卸 sink+open=False+清 surface,同帧 hidden-detail 实证翻转）→ 生命周期隔离=BII 本会话永久死亡（之后 BUE-DRAG/INVENTORY 全零=轮询门关闭实测确认）。排除项：SAFEMODE/open-threw/close-threw/open-skipped 全零;flag-hide/guard-hide/occupancy-failed 全零=异常不在早退分支;**rejectWhy=空=不在坐标谓词**（此前"持久拒绝"假设被否——拖#2/#4 在 SPF 修复后照样放置成功,surface 数据读数未坏）。SPF rebuild=死亡前置环境（其整页 clear() 会连 BUE 挂进 items panel 的预览框一起拆掉）,但致死机制=BUE 呈现车道对单帧异常的"永久死刑"反应（F-B2 同病,更深一层）。异常原文类型/消息由新故障闸的常驻诊断行在重采轮自动带出,不再需要专门一轮。
- **修复（红测先行）**：①预览车道故障闸——单帧瞬态异常吸收（HidePreview+一次性 Debug 诊断行含异常类型/消息 `event=preview-update-threw diagnosticId=BUE-DRAG-004`）,**60 连续帧**才 `runtime.Isolate()`（镜像 F-B2 TransientIsolationGate 语义;隔离拍发 `event=preview-update-isolated` Error 行）,健康帧复位+`event=preview-update-recovered` 一次性行;②Sink 自愈重挂——`EnsureMounted()`（isMounted 但子元素被第三方 clear 拆走时,下次拖拽开始 RemoveChild+AddChild 重挂框与图标,零接口变更）。红测 `--bue-v2-fb1-red`（ThrowOnce/ThrowAlways 评估器+RecordingVisualContainer）：**观测红=恰第 1 断言「单帧异常不得隔离」**（`fb1-red-run.log`）→实现→**绿**;两断言常驻默认套件。全套 7/7 PASS 0 警告。
- **Phase 6 完成**：全部 `[DEBUG-litfb3/4/5/6/7]` 插桩拆除,grep 清零（litfb7 的 DiagnosticSink 通道转正为 ClientUi 永久诊断缝,故障闸为其消费者）。增量=`round5-increment.diff`（5 文件 +242/−2,净变化=修复本体）。
- **候选 v4 重授（历史记录,v4 预授已被下方 F1 作废,现行候选=v4-F1）**：`40154219365C9F47BDFBC020D32CBB7B64B1EBD9B81BAD65D04F9745C96A8A26`（537088B **三轮** Rebuild 字节一致 0 警 0 错,CaseId 仍 `DEV-V2-24-CANDIDATE-20260908`）;**前身 v3=C9B6B6E4…EB86 作废**（连同更早 7d5dd3b5/3cbd6268）;kit/out、identity-sha256.txt v4、手册 §1、evidence 模板全部换绑;历史 case 横幅仍有效（绑定作废候选待重采）。**F-B1 死亡路径的用户可见症状（BII 静默死亡）已修;幽灵层/黑格=原生 listen-host 投影缺陷（SPF 注释自证）+SPF 对账修复域,BUE 无责亦无法根治,已在 SPF 项目侧留观察（对账可提前到仪表盘打开拍）**。
- **F-C 具名延期（新发现,不阻塞验收;**后续票=DEV-V2-25** `.scratch/…/issues/DEV-V2-25-lit-tidy-send-backoff-ratelimit.md` Status: open）**：LitTidyNetService.cs:420 定向发送失败 WARN 无限频/无退避——host 225112 刷 **1057 条**（generation=2,LocalTransportUnavailable=transport.Send 返回 false）;证据链=challenge 客机端**曾送达一次**（client 225127:1111 应用 gen=2）→链路中途劣化（出向可靠帧持续被拒、断线事件不上抛,SPF/LMN P2P 层域）+F-A 重臂循环（丢弃→重发现→再挑战）在传输持续不可达时逐拍自旋。拟修（=DEV-V2-25 Scope）：重臂退避（沿握手 reprobe 1s→8s 先例）+同代际告警限频+传输持续失败面诊断。210433 轮的 1199 条同案归档。
- **下一步（已被本节末「下一步（更新）」取代,保留作 R1 时点记录）**：双轴全新实例审查 round5 增量 → CLEAN 后提交 → v4 部署本机+VM → **全量重采手册十节**（绑 40154219…A826,该绑定要求已被 v4-F1 作废）→ RELEASES 行+真机手册 → 关票。
- **R1 双轴判词（全新实例,判词存 review-rounds.md）**：Standards **CLEAN**（3 SMELL：诊断行双 id/threat——BUE-DRAG-004 载荷与绑定追加的 BUE-CLIENTUI-001 同行双 id 且 threw/recovered 全走 Error 与 F-B2 通道语义变形；fault streak 跨拖拽代际不复位；测试两组件共享 emitted 列表钉不全。1 INFO：EnsureMounted 裸 RemoveChild 无防护）/ Spec **NOT CLEAN**（2 DEVIATION：票面冻结 threw/recovered=Debug 级而实现全走 Error；双 id 污染与 F-B2 那轮同类病。1 GAP：红测未断言 BUE-DRAG-004。1 SMELL：全局 sink 未在票面登记）→ **F1 全采纳**：①诊断缝升级 `DiagnosticSink(string, ClientUiDiagnosticLevel)`——组件自带级别（threw/recovered=Debug、isolated=Error,票面语义兑现）,Plugin 绑定按级路由且**载荷已含 diagnosticId= 时不再追加** BUE-CLIENTUI-001（双 id 根除）；②streak 复位点=OnDragStarted（新代际清零）+OnInventoryClosed——红测新增紧判别（59 帧→新代际→1 帧:无复位=恰 60 隔离,有复位=存活）；③红测钉 BUE-DRAG-004 于 threw/recovered/isolated 三行+一次性语义（59 帧内 threw 恰 1 行）+异常身份；④EnsureMounted 加 never-throw 防护（沿宿主事件惯例）；⑤全局 sink 在票面本节登记。F1 后旗标+全套 7/7 PASS 0 警告。
- **候选 v4-F1 重授**：`7448B0CE14ECCED001E4E8B4DFCE09416DC66985AB063A904ACD681EC57464C9`（537088B 三轮 Rebuild 一致 0 警 0 错,CaseId 仍 DEV-V2-24-CANDIDATE-20260908）；**v4 预授 40154219…8A26 因 F1 传导编译输入作废**；identity-sha256.txt/kit/手册/模板已换绑终稿;round5-increment.diff 已刷新为含 F1 终稿（R2 审查标的）。
- **下一步（更新）**：R2 双轴全新实例复审含 F1 终稿增量 → 双 CLEAN 后提交 → v4-F1 部署本机+VM → 全量重采十节（绑 7448B0CE…64C9）→ RELEASES 行+真机手册 → 关票。

### 2026-09-09 F-B1b 修复轮（SP 首拖 NRE·池化自愈）——候选 v5 重授

- **采集中断（用户 SP 轮,诊断包 20260909_000640+视频）**：单人环境 BII 完全不工作。litfb7 故障闸新诊断一发定位：首拖第 1 帧 `previewPresenter.Update` 内 NRE，逐帧复现，60 帧后闸隔离（设计内行为；v3 后单人从未实测，此缺陷在旧静默 catch 时代不可见=非 v4-F1 回归）。
- **根因（litfb8=BE3EB099…647F 追加 stack= 首帧,用户 10 秒轮 20260909_001648）**：`SDG.Unturned.GlazierBox_uGUI.set_BackgroundColor [0x00018]`；IL 核对=`callvirt Graphic::set_color` 打在 null 的 `imageComponent` 上——Glazier 池化 box 的 uGUI 组件仅 `ConstructNew()` 赋值、`ReleaseBoxToPool` 入池主动置 null。sink 持有的 box 被原生池回收/未实例化（SDG 内部域,BUE 不可修,只能自愈）。
- **修复**：①`InventoryPreviewVisualSink` 元素可重建——ShowFrame/ShowIcon 写失败即 `RebuildElements()`（旧移除+容器工厂全新创建+重挂）+重试一次，二次故障仍交预览故障闸（吸收/隔离语义不变）；②故障闸 threw/isolated 行追加 `stack=` 首帧（litfb8 转正）。红测 `AssertSinkRebuildsElementsOnNativeWriteFault`（Poisoned 元素模拟池化 NRE,观测红→绿）入旗标+默认套件；全套 7/7 PASS 0 警告。
- **候选 v5 预授（已被下方 v5-F2 作废）**：`22BE7A3A613EA1F88F2866B0264F030E2A46D2717C63843F75976AEDF9F5C56B`（537600B 三轮 Rebuild 一致,CaseId 仍 DEV-V2-24-CANDIDATE-20260908）；**v4-F1=7448B0CE…64C9 因 F-B1b 传导编译输入作废**；identity/kit/手册/六模板换绑（增量=round6-increment.diff）。
- **双轴 R1（F-B1b）**：Standards **CLEAN**（5S：isMounted 时序/icon 重建抹帧/裸 RemoveChild/结构复制/红测钉不全——全部 F2 采纳,其中结构复制=零分配契约 won't-fix 具名;1 INFO 文档字样）/ Spec **NOT CLEAN**（2D+1G：手册 round5 指针/归档红观测描述/ShowIcon 红测缺）→ **F2 全采纳**（含外科手术式强化红验证）→ 旗标+全套 7/7 PASS 0 警。
- **候选 v5-F2 重授**：`2D3DE91D798F2B031BEB19BE59466390EAAB8E31A1E8A04411B517C1B20762E1`（537600B 三轮 Rebuild 一致 0 警 0 错）；v5 预授 22BE7A3A…5C56B 作废；identity/kit/手册/六模板换绑终稿。
- **下一步**：R2 双轴复审含 F2 终稿 → 双 CLEAN 后提交 → 用户 SP 一轮确认 BII 存活（v5-F2 已部署本机）→ 全量重采十节（绑 2D3DE91D…762E1）→ RELEASES 行+真机手册 → 关票。
- **下一步（R1 时点记录,已被本节末 v5-F2 版取代;其哈希绑定的 v5 预授 22BE7A3A…5C56B 亦已作废）**：双轴全新实例审 round6 增量 → CLEAN 后提交 → v5 部署 → **用户 SP 一轮确认 BII 存活** → 全量重采十节 → RELEASES 行+真机手册 → 关票。
- **双轴 R2/R3（F-B1b）**：Standards **R2 CLEAN**（五项全闭环;2 INFO 簿记）/ Spec R2 NOT CLEAN（1 DEVIATION=历史「下一步」哈希陈旧）→ **F3 簿记修复**（历史行标注+evidence identity 对齐）→ **Spec R3 CLEAN（零发现）**。**F-B1b 双轴最终 CLEAN（Standards=R2/Spec=R3）**,判词全存 review-rounds.md。
- **F-B1b 闭环状态**：候选 v5-F2=`2D3DE91D798F2B031BEB19BE59466390EAAB8E31A1E8A04411B517C1B20762E1`（537600B 三轮 Rebuild 一致,已部署本机核验）;前身链全六代作废留痕。**待用户 SP 一轮确认 BII 存活 → 全量重采十节（绑 2D3DE91D…762E1）→ RELEASES 行+真机手册 → 关票**。
- **SP 存活确认（2026-09-09 08:17,用户实测+诊断包 20260909_081731）**：身份=2D3DE91D…762E1 ✓;REG-ACCEPT×5;拖 18/预览可见 50/放置 17;**故障闸零触发（threw/isolated/recovered 全 0）**;零错误;用户原话「单人模式下的功能恢复了」。注意=本轮未含整理执行锚（指纹守恒/热键重绑零行）,sp 正式 case 仍按手册 §4 全流程重采。
- **P2P 存活确认（2026-09-09 08:33,用户实测+双端包 主机 083303/客机 083313+视频 08-29-09）**：双端身份=2D3DE91D…762E1 ✓;**主机 25 次拖拽（代际 1→25）/预览求值 62/可见 53,故障闸两端零触发,BUE 零错误**——BII 穿透 SPF 全页重建风暴全程存活,F-B1/F-B1b 修复实机生效。整理执行=主机 3 页 placed=3/2/5 指纹守恒验证通过;MergeA 整理后自动压弹命中冷却窗口按设计跳过;LHT 心跳正常+HUD 双端注入;客机重进=会话代际 2→3 事件拍即清（DEV-V2-21 机制实机验证）+HUD 重注入,退出链完整（模块停止 1/3→3/3+hand-back-to-vanilla）。SPF 修复行 `page=2 authoritative=4 renderedBefore=9`（5 幽灵元素）=原版 listen-host 投影缺陷铁证。
- **F-B1c 立项（2026-09-09,用户拍板「这个问题不修了吗？就这么放在这了？」——BUE 侧修复,不再归 SPF 域挂起）**：症状=新玩家加入后主机背包物品重叠+整理后幽灵图层（视频 0 秒即见:旅行包左上黄物与荧光衣同格叠画,手中物品 x30 黑格）。**机制与责任重构**：原版 listen-host 投影缺陷（pendingItems 延迟创建+按坐标移除留陈旧元素）是底座,但**BUE LIT 整理=带外批量变更,是制造「整理后幽灵」的直接动作方**——自己作废的 UI 投影自己修;SPF 只在 Receive* 事件后修复,天然盖不住带外变更与开包时刻。**修法（与 SPF 互补幂等,零冲突）**：BUE 侧投影对账器（listen-host+本地玩家门+反射契约 fail-closed）,两触发=①仪表盘开包（覆盖「加入后开包见重叠」）②LIT 整理提交完成（覆盖「整理后幽灵」）;算法=权威 Items vs 渲染+pending 引用精确匹配,不一致才 clear+resize+重加（SPF 同款幂等语义,先查后修双跑互不干扰）。红测先行（纯决策核+投影 seam,宿主测试路径零引擎调用）→双轴→候选 v6→全量重采。
- **F-B1c 实现轮闭环（2026-09-09 上午,红测先行）**：新文件 `src/BetterUnturnedExperience.ClientUi/ListenHostProjectionReconciler.cs`（EmbeddedClientUi 并入 Plugin 单 DLL;命名空间 .ClientUi.Internal 沿惯例）——①决策核 `ProjectionReconcileDecision.IsExact`（引用级多重集精确匹配,SPF ProjectionIsExact 同语义,null fail-closed 到不精确）;②页缝 `IInventoryProjectionPageView`（引擎实现包 SDG,测试绑 fake）;③纯编排 `ReconcileRange`（逐页查/精确静默跳过/陈旧重建一次/每修一条 BUE-LIT-001 Debug 行「[Tidy] listen-host 投影对账修复 page=/authoritative=/renderedBefore=/pendingBefore=」,null 视图零修零抛）;④**引擎路径全 NoInlining**（ReconcileRangeEngine+IsEligibleLocalHostEngine=Provider.isServer&&isClient+本地玩家门/EnsureReflectionContractEngine 懒解析 PlayerDashboardInventoryUI.items+SleekItems.pendingItems 契约/BuildEnginePageView 页界钳制 SLOTS..PAGES-2/修复体镜像 SPF=clear+resize+权威序重加+普通页热键重投照 storage 排除;引擎异常→BUE-LIT-002 Warn 中止行,永不打断调用方）;⑤**分派器 test-safe 设计**（ReconcileHook 测试观察缝先行→EngineDispatcher 仅 Plugin.Awake 绑定→双空=静默 no-op,宿主测试路径永不 JIT 触 SDG——harness 全链跑通全靠此形制）。接线三点=LIT `PublishTidyCompleted` 发布同拍 OnTidyPagesCommitted（任何 outcome,补偿回滚同样产生带外变更;firstPage/lastPage 已展开）+Plugin.Awake 开包回调 OnDashboardSurfaceOpened（TIDYABLE 2..6 全域,单源 HotkeySnapshotUtil）+Awake 绑定 EngineDispatcher。红测 `--bue-v2-fb1c-red` 五组收集式（决策核矩阵 7 断言/分派路由+no-op 契约/纯编排含 fail-closed/BUE-LIT-001 诊断行+无修零行/harness 全链:整理提交同拍路由 (3,3) 且 TidyCompleted 发布语义不变）,入旗标+默认套件。**观测红=编译红 CS0246（fb1c-red-build.log）→实现一次写入→旗标绿（fb1c-red-run.log exit=0）→全套 7/7 PASS（fb1c-fullsuite-*.log）→全方案 Rebuild 0 警告（fb1c-sln-rebuild.log）**。坑:Player.LocalPlayer 属性大写;.Internal 命名空间惯例;-flp 开关与值须分开传参。**待=双轴审查（fresh 实例）→候选 v6 重授**。
- **F-D 观察（2026-09-09,agent U3DS 自动试启动抓到,疑似真缺陷待诊断）**：v5-F2 部署 U3DS（哈希核验一致）→分离式启动（PID 21804）→assembly-identity/takeover installed/runtime-gate Headless/BootstrapReady/REG-ACCEPT×5 全出且零 PLATFORM-001 零错误,**但 08:55 起 BepInEx 日志冻结 27 行（关卡 08:57:28 已 100%）:「加载成功（无界面）」/「模块已启动」/bue-runtime-arm/心跳全部未出现**——运行时完成链断裂。日志尾部=`host-destroyed state=preserved patches-kept=true BUE-CLIENTUI-005`早于关卡加载完成;代码链=OnDestroy(非退出)→UnsubscribeSceneLoaded+pluginUpdateDriver.Clear+**BueRuntimeHost.Clear()**→场景加载后 CompleteRuntimeOnce 拿到 null runtime 恒 false→屏障永断;且 R19 设计的 MenuUI.Update postfix 续驱面在无头不存在。13 代候选干跑（2026-09-07）同机同实例 AnnounceReady 正常=13..23 间引入/暴露。进程 09:12 前已退出（退出原因未捕获,重启复现时补）。**与 F-B1c 同轮处理**：litfb9 探针（若需）→修复=无头完成驱动缝（扫毁后仍可完成注册）→红测→双轴→随 v6 重授→U3DS U1 自动重采。
- **F-D 实现轮闭环（2026-09-09 上午,红测先行,无探针——日志+代码链已足够定位）**：新文件 `src/BetterUnturnedExperience.Plugin/BueRuntimeCompletionChain.cs`（两个静态类）。①`BueRuntimeCompletionChain`=完成链静态化：OnSceneLoadedCore（无头泵 healer 缝→TryCompleteRuntimeCore,纯引擎零引用=测试面）+TryCompleteRuntimeCore（静态屏障+RuntimeReadyLogged 守卫+AnnounceReady(HeadlessDecision) 恰一次）+CompleteRuntimeOnceCore（原 CompleteRuntimeOnce 语义平移:RuntimeReady 门+模块启动+刷新 hook）+TeardownForQuit（真退出才摘链:退订+清缝+清屏障+**BueRuntimeHost.Clear()**）;**引擎触碰成员全部经 Awake 绑定的委托缝**（SceneLoadedUnsubscriber/HeadlessPumpHealer/CompletionRefreshHook,null=静默 no-op,测试路径永不 JIT 触 Unity）。②`BueRuntimeTickChain`=共享每帧链（镜像重试→TickNetwork→TickOnce→tidy 泵→完成驱动）+**帧去重**（FrameProvider 缝,生产绑 Time.frameCount,多驱动源同帧只执行一次）。③插件类改造：Awake=绑 HeadlessDecision/退订缝/帧提供器+静态场景订阅（EnsureSceneLoadedSubscribedStatic）+**无头分支 EnsureHeadlessSurvivalPump**（独立 DDOL 普通 GameObject「BUE.RuntimePump」——3.26.3.9 实证 HideAndDontSave 对象被禁毁而普通对象存活,回调指向静态链=组件死后继续走;附 BUE-BOOTSTRAP-003 诊断行,失败 Warn 不阻 bootstrap）;Update/客户泵/Start=全部路由进共享链（原五段 Update 链原序平移,帧去重防双驱动）;**OnDestroy 非退出分支=保留运行时宿主与链条（不再 Clear/不再退订）**,仅断客户端驱动;真退出分支=DestroyHeadlessPump+TeardownForQuit。**语义守恒**：完成时机不早于旧实现（场景/Start/Update/泵驱动面全保留,第三方注册屏障不变）;完成不再退订场景驱动（幂等+兼作 heal 路径,有意变更具名）。红测 `--bue-v2-fd-red` 三组收集式（场景驱动在宿主死亡后完成+healer 被调+模块启动路径到达+AnnounceReady 恰一次/帧去重+时钟单调+Phase 冻结/退出 teardown 摘链+摘后驱动无公告）,入旗标+默认套件**末位**（其前置运行时+清宿主,后位无状态依赖——首轮误插中部杀伤面板目录测试的状态依赖,已移正）。**观测红=编译红 18×CS0103（fd-red-build.log）→实现→旗标绿（fd-red-run.log）→全套 7/7 PASS（fd-fullsuite-*.log）→全方案 Rebuild 0 警告（fd-sln-rebuild.log）**。坑:Plugin csproj 白名单编译新文件须显式 Include;List.Count 属性坑再踩一次（CS1955）;HostTick 属性名=Phase 非 TickPhase;CompleteRuntimeOnceCore 须返 bool（屏障 Func<bool>）。**待=双轴审查（F-B1c+F-D 同轮,fresh 实例）→候选 v6 重授**。
- **双轴 R1（F-B1c+F-D 同轮）+ F1 修复轮（2026-09-09 上午）**：R1 Standards **CLEAN**（4 SMELL）/Spec **NOT CLEAN**（6 GAP）→ **F1 全采纳**：①S-healer 未绑=真缺陷（缝合了生产没接）→Awake 无头分支补绑 `HeadlessPumpHealer=EnsureHeadlessSurvivalPump`；②S-空 catch→heal 失败/teardown 退订失败各补 Warn 留痕（BUE-BOOTSTRAP-003/001）；③S-Isolated 行 diagnosticId 误用 featureId→回改 BUE-BOOTSTRAP-001；④S-Reset 命名+帧哨兵→生产 `Reset()`（teardown 调用）+`ResetForTests()` 降级为测试别名单源委托+**BueRuntimeTickChain.Reset()**（teardown 同调）+哨兵初值 -1（真第 0 帧可 tick）；⑤G-门真值表→纯函数化 `IsEligibleLocalHostDecision(isServer,isClient,hasLocalPlayer)`（引擎路径以引擎状态调用）+组4 真值表钉死（专用服务器/纯客机/缺本地玩家全拒）；⑥G-TIDYABLE 全域→纯函数 `IsReconcilablePage` 单源 HotkeySnapshotUtil 常量（引擎钳制改用之,与开包触发域永不漂移）+组3b 全域逐页 2..6 编排断言+页域真值表；⑦G-证据空文件→双旗标补 ALL GREEN 成功行+fd/fb1c-red-run.log 与 f1-sln-rebuild.log 重生成有内容；⑧组5 重设计=判别性断言（同帧号 Reset 后可再执行,不 Reset 则被去重跳过）。**具名延期（Spec GAP-1/2 处置）**：独立 DDOL 泵真实存活与真实扫毁生命周期（Unity MonoBehaviour 生命周期宿主测试不可构造——引擎类型域不可加载,沿 15/17/18/21/22「实机自验绑 24」先例）→**实机证明=U3DS U1 锚行重采**（headless-survival-pump-attached BUE-BOOTSTRAP-003+「加载成功（无界面）」+模块启动全套+host-destroyed 后链条仍完成）。F1 后复验：双旗标绿（fb1c/fd-red-run.log 有 ALL GREEN 行）+全套 7/7 PASS（f1-fullsuite-*.log）+全方案 Rebuild 0 警告（f1-sln-rebuild.log）。**待=R2 双轴全新实例复审 → 候选 v6 重授**。
