# DEV-V5-08 评审链（发布工件轮）

工单：`.scratch/bue-v2-phase5-official-optimization/issues/DEV-V5-08-three-env-acceptance.md` · 候选 `BB33C2DD…4E16B`（源基线 52bafa9，开发态身份）· 被审范围=audit/2026-09-16/DEV-V5-08/ 全工件（src 零改动轮）。

## R1（fresh×2，双轴独立）

- **Standards R1 = NOT CLEAN**（实例 agent_67b9b0d5）
  - 硬1：contracts-diff.txt 为 0 字节空文件，不构成可复核证据材料（结论真但形式断裂）→ **修**：补命令回显+执行确认+时间戳。
  - 硬2（评审自判按先例不阻断）：rebuild-1..3.txt 正文无 exit/警错回执行（POST-P4 同格式先例）→ **修（从严）**：每份日志追加 runner footer（command/exit 0/grep 计数/逐轮 DLL 哈希，标明执行期观察）。
  - 气味 4 条均免记（定性销账口径成立/Glazier 双记成立/日志行序并行噪声/DLL 哈希为权威确定性锚）。
- **Spec R1 = NOT CLEAN**（实例 agent_89ade703，grok 路由本轮正常出判决）
  - 缺口1（唯一阻断）：acceptance-plan P4「主机自己 toast 不串扰」判据字面未在票面追溯出处 → **修**：改写为显式出处（07 票 line46「红色剩余秒（主机为准）」+ 52bafa9「本机 toast/远端 kind 6 定向」+观察代理定义）。
  - 核验通过项：五格推进顺序无越权（无提前定身）/26 缺口与 02..07 票面逐条对表条数吻合/U3DS 双侧+P2P Isolated 纪律落判据/契约零扩面独立重跑复核为真/手册六条逐字兑现。
  - 非阻断观察：G04-4/G05-5 覆盖已在 P3 行（评审自证成立，不改）。

## 修复增量子集（R1→R2）

1. `contracts-diff.txt`：命令回显+runner 执行确认（含「重跑复验同为空」）。
2. `rebuild-1..3.txt`：各追加 runner footer（执行期 exit 码/0 警 0 错/本轮 DLL 哈希）。
3. `candidate-sha256.txt`：确定性声明措辞改为「每轮后候选 DLL SHA-256 逐字节一致」并指向 footer。
4. `acceptance-plan.md` P4 行：判据补出处，消除无源措辞。

## R2（fresh×2，复审修复增量）

- **Spec R2 = CLEAN**（实例 agent_c518c055）：P4 出处两锚独立核验为真（DEV-V5-07 实施票 line46 + 52bafa9 message 逐字）；同型抽查 SP-S6(G06-3)/U3DS(U2) 两行回票面一手出处无漂移；台账↔方案交叉引用闭合。非阻断建议=引用保留完整括注、票名消歧。
- **Standards R2 = NOT CLEAN**（实例 agent_faf3355c）：硬1=P4 引「07 票 line46」指向错对象。
  - **裁决（维护者实证驳回其事实前提，采纳其形式发现）**：该实例核验的是 wayfinder 决策票 `07-t7-reload-skill-hud.md`（line46=Q1 HUD 段，无「红色剩余秒」）；被引对象实为实施票 `DEV-V5-07-reload-skill-0-to-2.md` line46（Q3 冷却时序条，含「红色剩余秒（主机为准，剩余含窗头技术段…）」原文在场，`sed -n '46p'` 复验+Spec R2 独立核验双证）。引用**内容成立**；但其暴露「07 票」写法在存在同号两张票（决策票 NN-tx / 实施票 DEV-V5-NN）时可歧义=形式缺陷真实 → **修**：改写为完整文件名+完整括注原话（吸收 Spec R2 非阻断建议）。
  - 其三项核验通过：contracts-diff 回显经其独立重跑复核为空（构成有效证据；「runner 原始捕获 vs 人工转述」记为仓库级弱证据形式先例=免记随台账格式另票）；footer 三方哈希一致+诚实分层；candidate 措辞闭合。
- **R2 修复增量子集**：acceptance-plan P4 行→完整票名+原话括注（唯一改动行）。

## R3（fresh×2，复审 P4 行消歧改动；含对 R2-Standards 误读票面一事的裁决复验）

- **Standards R3 = CLEAN**（实例 agent_792c0b8c）：裁决复验公允（两票 line46 亲读比对，误读+歧义双成立）；P4 行终检通过（文件名/行号/两处引文逐字、无残留无源措辞）。气味 1 条=原话嵌套破折号可读性弱（逐字引用要求所致，免记）。
- **Spec R3 = CLEAN**（首派 agent 空返回作废具名=fresh-instance 规则补派 agent_ac79c25c 出判决）：P4 行三要素核验通过（「无关端不弹 toast」经其判定=合理外推且已如实标注为观察代理，非冒充一手引文，非阻断）；裁决记录与磁盘一致；G07-6↔P4 交叉引用闭合、全文无残留歧义「07 票」写法（review-loop 历史段留痕除外）；链完整性=第三格闭环成立。

## 终态

- 发布工件轮审查链：**R1 双 NOT CLEAN（4 项发现全修）→ R2 Spec CLEAN + Standards NOT CLEAN（误读驳回事实前提/采纳形式缺陷）→ R3 双 CLEAN**。
- 工单第三格「双轴独立审查 CLEAN（发布工件轮）」成立。实机验收发现缺陷若改二进制 → 重出候选 + 新审查轮（F1 先例），本链只对当前候选 `BB33C2DD…4E16B` 负责。
- 过程具名：R2-Standards 误读票面事件（决策票 07-tx 与实施票 DEV-V5-xx 同号歧义）→ 本票起审查简报/引用一律写完整文件名；06/07 先例的 Spec 轴空返回再复发 1 次（R3 首派），fresh 补派出判决、无需重启。

---

# 实机修复轮（F1/F2 → 候选 v2 d7549806）

被审增量=工作树 vs 52bafa9 代码改动（LirSkillEngineHooks 唯一 src 文件）+ 新红测组 + 修复轮工件（fix-round-F1F2.md 及 red/green/M1/rebuild/fullsuite/gate 证据）。

## FIX-R1（fresh×2）

- **Standards FIX-R1 = CLEAN**（agent_80ba85fd）：4 气味（①绿证 0 字节与红证不对称→与 Spec 缺口同源修；②三处身份链手工判空 Data Clumps——IL 守卫按类型限定豁免合理，记录；③扫描未处理泛型极端——异常路径已记红自证，记录；④InPlaceReloadModule:358 同名字符串=日志文案非代码，说明记录）。
- **Spec FIX-R1 = NOT CLEAN**（agent_6677ea8f）：缺口1=green-il-guard-run.txt/mutant-M1-restored-green.txt 0 字节不构成执行证据 → **修**：各补 runner footer（命令+exit 0+行为说明）。缺口2=acceptance-plan/fix-round 待办仍锚 v1 → **修**：方案头与采集纪律第 1 条改锚 v2 全哈希+「v1/探针证据不得续用」；fix-round 待办补复测硬门槛（assembly-identity=D7549806 全哈希）。非阻断确认：F1/F2 阻断定性成立、修复不越界、G07-4 走向销账、候选纪律完整。

## FIX-R2（fresh×2，复审修复轮增量）

- **Standards FIX-R2 = CLEAN**（agent_1a7b7c38）：footer 有效（与红证构成同旗标不同构建自洽）；v2 改锚复核（头行/纪律/待办闭合；P1 行漏锚=气味，建议补齐）；四气味处置无误；格式同律。
- **Spec FIX-R2 = NOT CLEAN**（agent_a22873ce）：缺口1=acceptance-plan P1 行仍锚 `bb33c2dd…`（与头行 v2 锚冲突，按 P1 执行会接受作废候选）→ **修**：P1 行改 v2 全哈希+不匹配停测。非阻断确认：四证链闭合、candidate 三方对账一致、RELEASES/publish 纪律守住。

## FIX-R3（fresh×2，复审 P1 行改锚）

- **Standards FIX-R3 = CLEAN**（agent_eb15638c）：P1=v2 全哈希+停测门槛与头行/纪律一致；全文无现行 v1 锚（作废声明豁免）；S1/U1 分层粒度可接受。气味 2 条=哈希书写颗粒度不统一（截断/全哈希并存，语义一致）、记录不阻断。
- **Spec FIX-R3 = CLEAN**（agent_be50733a）：P1 缺口闭合（工单「身份锚绑定候选哈希」兑现）；FIX-R1→R3 发现全销账；「v2 身份+双轴 CLEAN」成立=工单第四格前置达成（第四格本身仍须实测）；批准前不碰 RELEASES/publish 经磁盘核验仍成立。

## 修复轮终态

FIX-R1 Standards CLEAN（4 气味）+ Spec NC（2 缺口）→ 修 → FIX-R2 Standards CLEAN + Spec NC（1 缺口=P1 行漏锚）→ 修 → FIX-R3 双 CLEAN。候选 v2 `D7549806…326BA0` 审查链闭合；实机复测（SP 轮2 起）以 v2 全哈希为硬门槛。过程具名：LogDiagnostic 节流吞诊断行（探针轮教训，已记 fix-round 文档）。
