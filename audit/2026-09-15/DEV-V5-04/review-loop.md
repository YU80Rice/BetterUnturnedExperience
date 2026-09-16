# DEV-V5-04 入包恢复 — 双轴评审链

工作区 = D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验。判据 = 票
`.scratch/bue-v2-phase5-official-optimization/issues/DEV-V5-04-passive-insert-recover.md`（含设计定案节）
+ spec V5-T5 节 + V5-T5 Answer（05-t5-passive-tidy.md）。规则 = docs/agents/output-review-loop.md
（每轮两全新实例；发现→修复→增量复审；判断性气味须具名递延方可放行）。

## 会话插曲（2026-09-15 晚，不计轮次）

首次 R1 派发因双轴子代理提供方故障未获任何答复（standards-reviewer=`insufficient balance`，
Spec-Reviewer/general-purpose=`Provider authentication failed`，各重试一轮同错；详见
memory/bue-v5-04-pending-dual-axis-review.md）。用户恢复 provider 配置后重派，R1 起算。

## R1（冻结包 review-freeze-r1-*；全套绿基线见 green-*.txt 同时点重跑）

- **Standards 轴（standards-reviewer 全新实例）**：`Model returned no text, no tool calls` ——
  空返回，未产出评审。**本轮无效**（Fresh-instance 规则：不作废后续，但 R1-Standards 计为未跑成，
  其增量并入 R2 复审面）。
- **Spec 轴（Spec-Reviewer 全新实例）**：`SPEC: FINDINGS 2缺口/0偏差`
  1. 「普通拾取 `tryAddItem(item,true)` 不进 `tryAddItemAuto`，postfix 行为位断链」（引用 PI.cs:464-489）。
  2. 「测试直驱 postfix，未走真实 RPC 失败链，故未暴露 1」。

### R1 处置

- **Finding 1 = 误读，具名反驳（源码逐字）**：`PlayerInventory.cs:464`
  `tryAddItem(Item,bool)`→`tryAddItem(item,auto,true)`；`:468` 三参重载→`tryAddItemAuto(item,auto,auto,auto,playEffect)`；
  评审引用的 473-489 行是**私有 helper `tryAddItemEquip`**（被 tryAddItemAuto 内部调用），不是 2 参重载的目标；
  `ItemManager.cs:361-364` to_page==255 普通拾取实证走 `tryAddItem(item,true)`；
  合成 `forceAddItem`(597)→`forceAddItemAuto`→`tryAddItemAuto`(609)。两条票内路径都收敛于
  `tryAddItemAuto` = 行为位正确。U3-SDK 行号已写入 `InsertRecoverAutoAddPatch` 注释与票设计节。
- **Finding 2 = 部分成立且引发真缺陷发现（采纳其方向，超出其字面）**：宿主确实不能端到端跑 RPC（具名
  缺口随 08），但「绑定形状是否真能在机器上解析」是可宿主化的——补 `1f 目标真实` 断言时**实证发现
  R1 冻结件自身有一个真机静默失效缺陷**：
  - 捆绑 0Harmony 2.9 实测：`in ServerInvocationContext` 首参反射形状=`T&`，
    `AccessTools.Method(type,name,[typeof(SIC),...])` **plain 型永不相配**（probe：plain-SIC binding=False），
    byref 型又不可写进 attribute 常量；
  - **名绑定遇多重载在该 fork 直接抛 `Ambiguous match in Harmony patch`**（probe 实证）——
    `PlayerCrafting.ReceiveCraft` 恰有 [Obsolete] ushort 转发 stub 同名重载 → R1 的名-only craft
    开合器在真机将令 `InstallRecoverPatches` 抛错→整体回滚→恢复面静默不登记（拾取面同被连坐）。
  - **修复（R2 增量）**：三处补丁改由 `InsertRecoverBinder` 统一显式解析 MethodBase（byref-aware 扫描 +
    craft 只绑 GUID 重载）+ `harmony.Patch(original,prefix,postfix,transpiler,finalizer,ilmanipulator)`
    手动绑定（5 参重载 [Obsolete]→用 6 参）；`[HarmonyPatch]` attribute 退役（绑定唯一事实源=binder，
    且 binder 宿主可证）。测试 1d 重钉：面恰三条+序固定、每目标 declaringType/名/参数形状逐一核实、
    plain-SIC 不相配留反证断言、未知类解析 null。R1 的 `SPEC: FINDINGS` 由此真正闭环：其误读被驳回，
    但它的追问逼出了一个真机级缺陷。
- 票设计节补「具名范围限定（R1-Spec 追问澄清）」段：委托链逐字行号 + 指定坐标五参重载=票外具名排除。
- 突变补 **M7**（binder GUID 过滤器摘除→错绑 [Obsolete] stub）：1d 合成断言证红
  （red-mutation-M7-run.txt），还原复绿。
- R1 后的静态验证全量重跑（R2 基线，同刻留证）：sln Rebuild exit=0（0 错误）；V5-04 六组 ALL GREEN
  （green-v504-group-run.txt）；7 套件 exit=0（green-fullsuite-*.txt）；6 门禁绿——Contracts 命中=
  `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` 的 V2-02 既有注释自述禁词，与 DEV-V5-03
  基线逐字同文件（如实双记，非本票增量）。

## R2（冻结包 review-freeze-r2-*；Standards=对 R1+R2 合并增量全新评审；Spec=对修复增量复审）

- **Standards 轴（standards-reviewer 全新实例）**：`STANDARDS: FINDINGS 0硬/5气味`——硬纪律 1-8
  逐条核对无硬违规（不复制算法/不扩面/生命周期唯一开关/宿主可测/事务手术刀=pending 五处侵入无漏项
  +CommitPreparations 纯搬移/候选纪律零痕迹）。判断性气味五条，全部具名递延（见下节），按
  output-review-loop §3 不阻塞。
- **Spec 轴（Spec-Reviewer 全新实例）**：`SPEC: CLEAN`——R1 finding1 反驳经其独立逐字复核
  （PI.cs:464-471、597-609、ItemManager.cs:361-365）判定误读关闭；finding2 方向经 binder 重做闭环
  （面恰三条+GUID 真身+byref 形状核实+M7 证红）；验收四回归面未破；具名缺口未冒充已验；新发现=无。

### 判断性气味具名递延（R2-Standards 五条，均不阻断）

1. pending 三态判定（Tag is ItemJar / pendingTag 引用相等 / continue-vs-return-false）在
   `ValidateTagConsistency`/`ValidateFingerprintMultiset`/`CommitPage` 三处形态微差地重复——
   递延（强行抽 helper 牺牲三处各自的语义清晰度；邻居 LocalTidyExecutor 亦就地展开风格）。
2. `InsertRecoverAdapter` 拒绝原因=string 常量九枚而非 enum:byte（邻居 LitContainerTidyReason 用枚举）——
   递延（拒绝身份只入日志/红测不进线协议，无字节域冻结需求；FailureReason 同域先例=string）。
3. ManualTidyService 单次 diff 双原因（pending 扩展+CommitPreparations 提取）——评审自判可接受：
   提取是 pending 复用的必要前置，非独立变化。
4. `InsertRecoverEngine.EquipUsableTail` 以嵌套循环按引用相等定位落点（轻微 Message Chain）——递延
   （规模小、目标明确、引擎面只跑真机）。
5. 测试 Group 闭包 8 字段 save/restore 样板重复——如实记录：DEV-V5-03 既定写法，非本票新增坏味道。

## 终态判定

- 双轴 R2 皆 CLEAN（Standards 0硬+递延具名 / Spec 0缺口0偏差）。评审链：R1-Spec 2 缺口→
  1 误读驳回（双实例独立复核一致）+1 方向采纳→真机级 binder 缺陷发现与修复（M7 证红）→ R2 双 CLEAN。
- 验证基线（R2 同刻全量重跑）：sln Rebuild 0 错误；V5-04 六组 ALL GREEN；7 套件 exit=0；6 门禁
  绿（Contracts 命中=V2-02 既有注释基线同文件，非本票增量）；突变 M1-M7 各证红+还原复绿。
- 候选纪律：本票不产候选 DLL、不更 RELEASES、不授 CaseId（08 唯一对外）。
