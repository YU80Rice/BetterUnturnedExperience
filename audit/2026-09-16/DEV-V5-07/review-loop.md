DEV-V5-07 双轴独立评审链（output-review-loop）
================================================

票：换弹技能 0～2 级与技能菜单分区。判据源=票面验收条 + spec.md 技能段 + T7 Answer + R5 研究。
参数=用户当场裁定并落票面 Comments 具名常量（125/150/8/8/2；技术闸 1.5 沿用）。

## 红测先行（步 1）
- 编译红：红测文件对未实现类型 CS0246×7（IReloadSkillPersistence/ReloadSkillRecord×3/ILirSkillHooks/ReloadSkillUpgradeDecision×2）。
- 绿：Rebuild 0/0 + `--bue-v5-07-reload-skill-red` 六组 ALL GREEN + 7 套件全绿 + 6 门禁全绿（NoUiTokens：Core PASS 30 文件；Contracts 命中=V2-02 基线逐字相同，git diff 本票 0 改动）。
- 突变 M1-M8 各证红（详见 red-evidence.txt），其中 M7 首版 `if(false)` 触发 CS0162 构建失败=不计证据（06 假证据教训），按可达突变重做证红；M3 同理。
- 具名接缝缺口（随 08 实机验收，非本票缺陷）：真机 Glazier 注入几何 / askSpend 各角色扣减复制 / 引擎身份真值（characterName·equipment.state）/ 表面 A 探测真机命中 / P2P 升级与冷却回包定向。

## R1（双轴，各自全新实例）
### Standards 轴（agent_231a4e22）— verdict：CLEAN（0 硬违规 / 4 判断性气味）
- S1 Duplicated Code：`LirRepackNetwork.cs` pendingRequestOrder 与 pendingUpgradeOrder 的「限长驱逐+入队」8 行×2 重复 → **具名递延**（重复度低，可提炼 BoundedPendingSet，随后续）。
- S2 Data Clumps：`InPlaceReloadModule` 三组（在册 bool + 诊断 string）+ 三处 Install/Uninstall 结构相似 → **具名递延**（延续 06 先例，非本票发明；四面共存互撤纪律要求三组并行）。
- S3 Speculative Generality：`ReloadAutoRoundScheduler.Cancel(ulong)` 无调用点 → **采纳即删**（本票内已删除该方法）。
- S4 Feature Envy：`ExecuteUpgradeFor`（网络回执）与 `HandleSkillUpgradeRequest`（服务器直执行）均「校验→toast→镜像→行复位」三段编排 → **具名递延**（评审自判合理路径分岔，文案/状态已单源于 Policy/Mirror）。

### Spec 轴（首派 agent_b283700d 空返回=已跑未出 verdict，按 fresh-instance 规则作废不续用；补派 agent_d1831efd）— verdict：CLEAN（九判据逐条成立，1 非阻断偏离）
- 判据 1-9 全部「成立」（逐条文件:行 + 红测组号证据，见 subagent 报告）。
- D5 非阻断偏离：判据 5「恢复路径不触发 A」红测只反证「功能 A 不读技能窗」（2g），未在 04/05 恢复 adapter 路径新增断言（04/05 本票未改动=既有语义继承，风险低）→ **记为随 08 集成检查的具名接缝缺口**。

## R2（步 3 增量复核，各自全新实例）
### Standards 轴 R2（agent_5a32f220）— verdict：CLEAN
- S3 `Cancel` 删除确认干净：全仓 grep 无该方法残留引用（同名命中的均为不相关符号：`MainThreadDispatcher.Cancel`/`PanelConfirmChoice.Cancel` 等），红测 4h 只用 Schedule/Tick/PendingCount，调度器四成员自洽（一轮为限靠 Tick 摘除+异常消耗，代际清靠 ResetForGeneration，均不依赖 Cancel）。
- S1/S2/S4 递延具名充分性：R1 具名行号与结构可在当前源码逐一定位（S1 `LirRepackNetwork.cs:320-324/489-493`；S4 `:522` vs `InPlaceReloadModule.cs:635` 路径分岔有据），属合规「判断题延后」非无依据搬运。
- 纯删除增量未引入新结构，无新硬违规。

### Spec 轴 R2（agent_ecb585c6）— verdict：CLEAN（+1 非阻断微观察）
- D5 定性实证成立：`AutoReloadAfterTidyAction.cs` 不在本票 diff（`git diff --stat` 核对），`Consumer = new TidyCompletedConsumer(new AutoReloadAfterTidyAction(this), …)` 非新增行——恢复路径零改动=既有语义继承，随 08 集成检查的定性合理。
- 三抽查非敷衍确认：判据 2 askSpend/askAward 真引擎调用（NoInlining 行 159-168）+全有全无回补（行 96-101）+外层 fail-closed；判据 6 技术闸拒绝纯计数静默（`:626-628`），`MakeCooldownToast` 只在技能窗拒绝两处调用，两路径完全隔离；判据 8 权威装配（Start 行 203-223）先于且独立于 headless 画面裁决（行 563-577），红测 6a 正面钉。
- **R2 新增非阻断观察 E1**：`ExecuteUpgrade` 若退款 `askAward` 本身抛异常，外层 catch 记「账零改」但经验已扣未退——低概率真机边缘+注释轻微失实 → 随 08 实机标注（票面接缝缺口清单已补记）。

## 关单（步 4）
- 双轴链：R1 Standards CLEAN（0 硬/4 气味：S3 采纳即删，S1/S2/S4 具名递延）+ R1 Spec CLEAN（九判据逐条成立，首派空返回具名作废、补派 fresh 出判决）→ R2 Standards CLEAN + R2 Spec CLEAN（E1 非阻断微观察入 08 清单）。全程每轮全新实例、无续用。
- 终态：Rebuild 0/0 + 7 套件全绿（Delete 后复跑）+ 6 门禁全绿（Contracts 命中=V2-02 基线逐字同）。
- 具名接缝缺口（随 08）：真机 Glazier 分区注入几何/交互、askSpend/askAward 各角色扣减复制、退款异常边缘（E1）、引擎身份真值（characterName/equipment.state）、表面 A 真机命中/降级路径、P2P 升级与冷却回包定向。
- 候选纪律：不产候选/不更 RELEASES/不授 CaseId（diff 无 publish/DLL/RELEASES）。
