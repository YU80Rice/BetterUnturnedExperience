# DEV-V2-24 审查轮次判词（D0-b 修复轮）

> 标的 = 面板 BII 条目显示名中文化（spec story 3）；每轮全新实例、显式 subagent_type（standards-reviewer / Spec-Reviewer）。
> 增量：round1-increment.diff（e70b7c4，源码 1 串 + 红测断言 1 条 + 审计换绑）/ round2-increment.diff（aae2c99，文档残留清理）。

## R1（2026-09-08，两个全新实例）

- **Standards R1: FINDINGS**（agent_16989f20-25fd-4a4a-9e90-b14381e841d2；blocking=0 smell=2 info=1）
  - 哈希换绑全链核过（identity-sha256.txt v2 / 手册 §1 / kit/out / artifacts / 六模板一致；旧哈希 7d5dd3b5 仅存于「前身作废」语境与追加史）；红测打在既有 HasManagementEntry 缝（Program.cs:3918-3920），redtest-run.log:2 FAIL 于新断言；REG-ACCEPT 英文日志标签具名裁定合理（spec 冻结的是面板显示名，LIT/LIR/LHT 注册日志同为英文标签）。无硬违规。
  - SMELL-1：手册 §3 S2 步骤残留「Better Item Interaction 条目 + D0 判别点」，与 D0-b 后源码冲突（采集员会误触「不符即停」）。
  - SMELL-2：手册 §10 与 sp / p2p-client 两模板残留「D0 判别 / 未拍板不采集」开放措辞，与票面「手册、六模板已换绑」不符——不可延期（会把错误期望写进证据）。
  - INFO：修复提交 hash 未回填票面 Comments。
- **Spec R1: CLEAN**（agent_251a95a9-3ab6-47c3-aafa-8996699506ae；gap=0 deviation=0 smell=0）
  - 修复恰落 story 3 / 显示名冻结面缺口，无其它英文投射点；FeatureId/注册身份未动；spec 未冻结启动日志文本（日志标签裁定不冲突）；红绿链证据支撑红测先行；票面记录与增量一致。

## F1-F3（提交 aae2c99）

S2 改四件中文名齐口径；§10 与 sp/p2p-client 模板清开放措辞；e70b7c4 回填票面 Comments 与手册 §1。

## R2（2026-09-08，两个全新实例）

- **Standards R2: CLEAN**（agent_0435752b-9e6d-41c5-bca1-05f9a0b73c50；0/0/0）：三条发现逐条核落地（S2/§10/模板/快照回填），无新口径或哈希冲突，round2 增量封闭（仅 4 个 md，无 src/tests）。
- **Spec R2: CLEAN**（agent_26ab1090-8287-40c6-af8d-e1dea96f83e8；0/0/0）：story 3 口径一致无残留矛盾；哈希与 CaseId 链一致；票面仍 claimed、未提前关票。

## 闭环

**双轴最终 CLEAN（Standards=R2 / Spec=R2）**。候选 `3cbd6268…9e4d`（DEV-V2-24-CANDIDATE-20260908）为已审增量产物；实机采集按手册（换绑后）进行。

---

# 修复轮审查（F-A + F-B2，2026-09-08）

> 标的 = round3-increment.diff（F-A `3db75c0` + F-B2 `f89194f`）与 round4-increment.diff（F1 `03b661f` + F1b `fe3e3cc`）。每轮全新实例。

## R2（两个全新实例）

- **Standards R2: CLEAN**（agent_a3473cf0-cf77-4083-b1d8-258b779c28a0；0B+2S+3I，均具名可延期）
  - F-A 回滚与 B3 同构且 token 面更严；OpenPeerScope 同代 no-op；包装器转发全 7 成员。
  - S1：ObservePollSuccess 不清 Latched 与测试语义不符（→F1 采纳修复）。
  - S2：宽限窗不对称（Poll 异常宽限内 dispatchedSurfaces 不清理）→具名延期：Poll 逐帧全捕获，≤60 帧残留 overlay 风险可容忍，genuine 失败仍到阈值隔离。
  - INFO：litfb 拆除彻底 ✓；无「单帧立即隔离」冻结承诺；F-A 事件重复 += 与 B3 同构且守卫幂等。
- **Spec R2: NOT CLEAN**（agent_e354a55a-8d1b-4739-9e51-ed720f5b8922；2D+0S）
  - D1：探针分支锁存行双发（隔离分支+通用分支各一次）。
  - D2：生产 sink 无条件追加 BUE-INVENTORY-003，污染 004 首失败/恢复行的诊断 id。

## F1/F1b（`03b661f` + `fe3e3cc`）

- D1：隔离后跳过通用发射，恰一次。
- D2：锁存行(003)走 EmitDiagnosticOnce(Error)；首失败/恢复行(004)改走 BueRuntimeLog.Runtime(Debug)，与既有 004 行同通道，id 不再被污染。
- S1 采纳：ObservePollSuccess 同时清 Latched。
- F1b：ObservePollSuccess 的 XML 注释同步（Standards R3 唯一 SMELL 处置）。

## R3（两个全新实例）

- **Standards R3: CLEAN**（agent_3cdcd991-09eb-4cff-b5e3-1e46f89c1f5e；0B+1S+1I——唯一 SMELL 即过期 XML 注释，已在 F1b 修复；S2 延期维持）。
- **Spec R3: CLEAN**（agent_10540155-ebea-461b-a636-9a840ca1b271；0/0/0；首派发因基础设施零输出作废留痕，本判词来自重派的全新实例）。

## 闭环

**双轴最终 CLEAN（Standards=R3 / Spec=R3）**。修复轮候选 `c9b6b6e4…eb86`（DEV-V2-24-CANDIDATE-20260908，535552B 三轮 Rebuild 一致，identity-sha256.txt v3）；前身 7d5dd3b5…c223 与 3cbd6268…9e4d 作废。F-B1（幽灵覆盖层）未在本轮修复——H2 数据复制已排除，待新候选上提复现率压测后定根因。

# F-B1 修复轮（round5-increment.diff,未提交工作树）

## 背景

实机根因闭环（litfb7=FAF3C0A6…63E7,诊断包 225112/225127）：第三方 SPF 整页重建背包 UI 的环境下,OnDragUpdated catch（第 4 条静默路径）对单帧异常 runtime.Isolate() 永久隔离 BII。修复=预览车道故障闸（60 连续帧去抖+三态诊断 BUE-DRAG-004）+Sink EnsureMounted 自愈重挂。红测 --bue-v2-fb1-red（观测红=首断言,fb1-red-run.log）。

## R1（两个全新实例）

- **Standards R1: CLEAN**（agent_7e616320-3472-4267-8920-a31a4a3a8838；0B+3S+2I）
  - S1：诊断行双 id（载荷 BUE-DRAG-004 + 绑定追加 BUE-CLIENTUI-001）且 threw/recovered 全走 Error,与 F-B2 通道语义镜像变形。
  - S2：previewUpdateFaultFrames 跨拖拽代际不复位（59+1=假隔离）。
  - S3：测试两组件共享 emitted 列表,首帧一次性/2..59 静默/隔离行身份未独立钉。
  - INFO：EnsureMounted 裸 RemoveChild 无防护（无引擎实证,不升级）；EmitDiagnostic 薄转发/未复用 TransientIsolationGate 与程序集边界一致,不报。
- **Spec R1: NOT CLEAN**（agent_9306d2c4-050d-4739-8c69-dfe1c6533fb0；2D+1G+1S）
  - D1：票面冻结 threw/recovered=Debug 级,实现全走 Error。
  - D2：双 id 污染（与 F-B2 轮 D2 同类病）。
  - GAP：红测未断言 BUE-DRAG-004。
  - SMELL：全局 sink 未在票面登记（非阻塞）。
  - 补充核验：src/ 零 [DEBUG-litfb 残留 ✓;fb1-red-run.log 红→绿链 ✓;F-C 延期登记证据链自洽 ✓。

## F1（全部采纳）

- D1+S1：诊断缝升级 `DiagnosticSink(string, ClientUiDiagnosticLevel)`——threw/recovered=Debug、isolated=Error;Plugin 绑定按级路由,载荷含 diagnosticId= 时不再追加 BUE-CLIENTUI-001。
- S2：streak 复位=OnDragStarted（新代际清零）+OnInventoryClosed;红测紧判别：59 帧→新代际→1 帧（无复位=恰 60 隔离）。
- S3+GAP：测试分组 emitted;钉 BUE-DRAG-004 于三行+一次性语义（59 帧 threw 恰 1 行）+异常身份。
- INFO 采纳：EnsureMounted never-throw 防护。
- SMELL 采纳：全局 sink 本节+票面登记。

## R2（两个全新实例,标的=含 F1 的 round5-increment.diff 终稿）


- **Standards R2: CLEAN**（agent_7090cc67-f2ec-49e6-8d58-112957dcc544；R1 五项复核全部已修复；1 SMELL=测试 sink 丢弃 level 未断言路由/双 id 抑制；2 INFO=证据 identity.txt 头部滞留 v3 字样、EnsureMounted 防御枝）
- **Spec R2: NOT CLEAN**（agent_f2a6dcde-f9e1-40f5-a55e-d2caa9bcf509；R1 四项复核全部已修复；D1=票面新旧候选授予表述自相矛盾（旧 v4 授予段未标作废指针）；D2=证据 candidate/identity.txt 基线/前身行未随 v4-F1 更新；GAP=红测收 level 未断言 Debug/Error 路由与 BUE-CLIENTUI-001 排除）

## F2（全部采纳；纯测试+文档,候选 7448B0CE…64C9 不受影响）

- Spec D1：票面旧 v4 授予段标「（历史记录,v4 预授已被下方 F1 作废,现行候选=v4-F1）」；旧「下一步」行标被取代注记。
- Spec D2+Standards INFO：evidence candidate/identity.txt 头部改「v4-F1」、基线补 F-B1 含 F1/round5-increment.diff、前身列表补 v3 与 v4 预授；手册 §1 快照行同步。
- Spec GAP+Standards SMELL：红测 sink 捕获 (line, level) 元组；threw/recovered 断言 level==Debug、isolated 断言 level==Error，三行均断言 !Contains(BUE-CLIENTUI-001)。
- Standards INFO（EnsureMounted 防御枝）接受不改（Mount 为构造期正常路径）。
- F2 后旗标+默认套件 PASS；round5-increment.diff 已再刷新（R3 审查标的）。

## R3（两个全新实例,标的=含 F1+F2 的 round5-increment.diff 终稿）

- **Standards R3: CLEAN**（agent_eab22a95-4ba2-449c-827b-ceb452e94239；R1 三项+R2 SMELL 复核全闭环；1 SMELL=recovered 断言未对称排除 BUE-CLIENTUI-001（断言不齐,非行为回归））
- **Spec R3: NOT CLEAN**（agent_abeb55c0-c61d-47fd-bca1-bf30429f7daa；R1 四项+R2 三项复核全闭环；2 GAP=①本归档留「（待回填）」占位（本条即回填）；②recovered 断言缺双 id 排除——与 Standards SMELL 同一处）

## F3（两处机械修复）

- recovered 断言补 `!Contains(BUE-CLIENTUI-001)`（与 threw/isolated 对称）。
- 本归档 R2/R3 段落回填完成（消除占位）。
- F3 后旗标+默认套件 PASS；候选 7448B0CE…64C9 不变（纯测试+归档）。

## R4（两个全新实例,标的=含 F1+F2+F3 的 round5-increment.diff 终稿）——判词到达时回填,不预置占位

## R4（两个全新实例,标的=含 F1+F2+F3 的 round5-increment.diff 终稿）

- **Standards R4: CLEAN**（agent_530d902b-5f2c-42b3-b4f4-ae7368d0c020；R3 SMELL 复核已修复；2 INFO=①R2 标题下残留占位行（F4-1 处置）②两份身份头口径分叉 v4/v4-F1（F4-2 处置）；生产代码终稿无新 BLOCKING/SMELL,R1-R2 已具名项未回潮）
- **Spec R4: NOT CLEAN**（agent_c2a21fa4-1522-42c4-900b-d0fe7b6b19f7；R3 两项复核=①已修复 ②归档占位残留实锤；3 发现=GAP 本轮判词未回填+占位残留（F4-1/F4-3 处置）、DEVIATION 归档自述与内容矛盾（同源）、SMELL F-C 延期缺具名后续票（F4-4 处置=新建 DEV-V2-25））

## F4（簿记闭环；纯归档+文档,候选 7448B0CE…64C9 不变）

- F4-1：删除 R2 标题下残留占位行（grep 待回填 清零,本行除外=处置记录）。
- F4-2：identity-sha256.txt 头部对齐 evidence 侧「v4-F1」口径。
- F4-3：本段即 R4 判词回填（自指簿记沿 DEV-V2-16 先例:机械记账动作随落盘即执行,由 R5 新实例核验）。
- F4-4：F-C 具名后续票 DEV-V2-25（`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-25-lit-tidy-send-backoff-ratelimit.md`,Status: open），DEV-V2-24 票面 F-C 延期行补票号指针。

## R5（Spec 单轴核验,全新实例;Standards 已于 R4 CLEAN）

- **Spec R5: CLEAN**（agent_4a0e359c-38bb-4727-8524-47be53853ccf；R4 三发现复核全部闭环：占位清零（残留两处均系判词引文）、身份文件三处对齐 v4-F1、F-C 具名票 DEV-V2-25 存在且 Scope/Acceptance 完整；src/tests 相对 R4 被审状态零改动,零新发现）

## 闭环

**双轴最终 CLEAN（Standards=R4 / Spec=R5）**。F-B1 修复轮候选 v4-F1 = `7448B0CE14ECCED001E4E8B4DFCE09416DC66985AB063A904ACD681EC57464C9`（537088B 三轮 Rebuild 一致,identity-sha256.txt v4-F1）；前身 7d5dd3b5…c223/3cbd6268…9e4d(v2)/C9B6B6E4…EB86(v3)/40154219…8A26(v4 预授)均作废。F-C 延期已具名 DEV-V2-25（open）。全量重采绑 v4-F1。

# F-B1b 修复轮（round6-increment.diff;SP 首拖 NRE）

## 根因链（litfb8=BE3EB099…647F,诊断包 20260909_001648）

故障闸 stack= 首帧点名 `SDG.Unturned.GlazierBox_uGUI.set_BackgroundColor [0x00018]`；IL 核对（Assembly-CSharp.il :92517-92530）= `callvirt Graphic::set_color` 操作数 `ldfld imageComponent`=null。GlazierBox_uGUI 的 imageComponent 仅 ConstructNew() 赋值、ReleaseBoxToPool 入池主动置 null——sink 持有的 box 被原生池回收/未实例化，每次预览帧写全 NRE→故障闸 60 帧隔离（新诊断按设计工作，stack= 字段一发定位）。SP 环境首证（v3 后单人未实测过，缺陷在静默 catch 时代不可见）。

## 修复（红测先行）

- `InventoryPreviewVisualSink`：frameElement/iconElement 改可重建；ShowFrame/ShowIcon=try→Apply→catch→`RebuildElements()`（旧元素移除+双容器工厂全新创建+重挂）→Apply 重试一次；二次故障仍传播到预览故障闸（吸收/隔离语义不变）。
- 故障闸 threw/isolated 行追加 `stack=` 首帧（litfb8 转正，本轮入生产）。
- 红测 `AssertSinkRebuildsElementsOnNativeWriteFault`（RecordingVisualContainer+TestVisualElement.Poisoned 模拟池化 NRE）：观测红=第 3 断言「poisoned 元素被替换为全新挂载」+「故障横跨后 preview 仍 Candidate」；旗标绿+全套 7/7 PASS 0 警告。

## 候选 v5

`22BE7A3A613EA1F88F2866B0264F030E2A46D2717C63843F75976AEDF9F5C56B`（537600B 三轮 Rebuild 一致 0 警 0 错，CaseId 仍 DEV-V2-24-CANDIDATE-20260908）；v4-F1=7448B0CE…64C9 因 F-B1b 传导编译输入作废；identity/kit/手册/六模板换绑 round6。

## 双轴 R1（全新实例,标的=round6-increment.diff）

- **Standards R1: CLEAN**（agent_26d96a55-2ef8-4e5c-b6b2-69df31944044；0B+5S+1I）
  - S1 RebuildElements isMounted 先置后挂（AddChild 抛错则状态错位）→F2 采纳=先挂后置,镜像 Mount()。
  - S2 ShowFrame 成功后 ShowIcon 重建会抹掉本帧 frame（≤1 帧,下帧补画）→F2 采纳=注释具名契约（零分配约束下不做有状态缓存）。
  - S3 RebuildElements 裸 RemoveChild 未沿 never-throw→F2 采纳=try/catch 防护。
  - S4 ShowFrame/ShowIcon 重试结构复制→F2 裁定 won't-fix=共享委托 helper 会在零分配热路径逐帧分配闭包（ClientUi.Tests 零分配守卫实测抓获 1.6MB 分配回归）,注释具名。
  - S5 红测未钉重试成功/二次故障进闸/icon 中毒→F2 采纳=门零触发断言+新元素确被写入+icon 中毒重建场景+毒 all 60 帧进闸场景。
  - INFO 手册/identity 头部字样漂移→F2 采纳修正。
- **Spec R1: NOT CLEAN**（agent_c43eccfb-949d-408a-8c04-33d7a509bd2d；2D+1G）
  - D1 手册来源快照仍指 round5→F2 采纳=改 round6（含 F-B1b 终稿）。
  - D2 归档「观测红=第 3 断言」与实际断言序不符+强化断言无独立红观测→F2 采纳=归档改按实测描述+外科手术式红验证（摘除重建段→红恰落在「poisoned 元素被替换」断言,恢复后绿）。
  - GAP 红测未覆盖 ShowIcon 写失败→重建→重试路径→F2 采纳=icon 中毒场景断言。
- F2 后旗标+全套 7/7 PASS 0 警告;候选 v5 预授 22BE7A3A…5C56B 因 F2 传导编译输入作废,**v5-F2=2D3DE91D…762E1**（537600B 三轮一致）,identity/kit/手册/六模板换绑。

## 双轴 R2（全新实例,标的=含 F2 的 round6-increment.diff 终稿）

- **Standards R2: CLEAN**（agent_37dc54f5-6c72-49e9-9d49-78c4a13ff792；R1 五项复核全部已修复；2 INFO=票面双「下一步」哈希陈旧、evidence identity 头/前身未列 v5 预授——F3 采纳）
- **Spec R2: NOT CLEAN**（agent_c48ac55d-9d61-4a77-b7e3-893a1625b86d；R1 三项复核全部已修复；1 DEVIATION=票面历史「下一步」行被全局哈希替换扫成 v5 预授哈希未标作废——F3 采纳）

## F3（纯簿记;候选 2D3DE91D…762E1 不变）

- 票面历史「下一步」行标注=R1 时点记录,其 v5 预授哈希已作废（现行绑定见 v5-F2 段）。
- evidence candidate/identity.txt 头部对齐 v5-F2 口径+前身补 22BE7A3A…5C56B。
- 本段即 R2 判词回填。

## 双轴 R3（Spec 单轴核验,全新实例;Standards 已于 R2 CLEAN）

- **Spec R3: CLEAN**（agent_377870d9-ecc2-47b1-8644-3adb3250541b；R2 DEVIATION 闭环+两 INFO 处置确认+src/tests 相对 R2 零改动+零新发现）

## F-B1b 闭环

**双轴最终 CLEAN（Standards=R2 / Spec=R3）**。候选 v5-F2 = `2D3DE91D798F2B031BEB19BE59466390EAAB8E31A1E8A04411B517C1B20762E1`（537600B 三轮 Rebuild 一致,identity-sha256.txt v5-F2）；前身链 7d5dd3b5…c223/3cbd6268…9e4d(v2)/C9B6B6E4…EB86(v3)/40154219…8A26(v4 预授)/7448B0CE…64C9(v4-F1)/22BE7A3A…5C56B(v5 预授)均作废。待用户 SP 确认后全量重采绑 v5-F2。
