# DEV-V5-05 快速转移恢复 — 双轴评审链

工作区 = D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验。判据 = 票
`.scratch/bue-v2-phase5-official-optimization/issues/DEV-V5-05-fast-transfer-recover.md`（含 2026-09-16
设计定案节）+ spec V5-T5 节 + V5-T5 Answer（05-t5-passive-tidy.md）+ V5-R4 研究报告。规则 =
docs/agents/output-review-loop.md（每轮两全新实例；发现→修复→增量复审；判断性气味须具名递延方可放行）。

## 红先行（先于一切生产绿）

- 编译红：`red-compile-errors.txt`（+中间消化记录 `build-pass1.txt`）——新红测文件引用未存在的
  05 面（CS0246 `FastTransferIntent` 起），证明测试先于实现落点。
- 行为红→绿：实现后首轮独立运行 FAIL（12 条），其中暴露两类真问题：
  (a) **`Items.getIndex` 触 `PlayerInventory.SLOTS` 静态初始化**——宿主必炸（04 同界教训在 05 的
      新形状），规划器改纯列表原点扫描（语义=原版 getIndex 原点等值）；
  (b) 两条测试自身缺陷（3a 把源侧恒等断言错加到已排版的接收侧箱子上；3c 用绝对计数打在共享
      recorder 上）。修复后六组 ALL GREEN。
- **自纠具名**：红测初版全链「线谎话」步骤在 pending 已被真结果消费后注入 → 实际命中「未发出」
  丢弃分支、真互斥分支未测（假绿）。发现后重写 = 服务器未派发的活 pending 窗口内投两种错 kind
  帧，随后真帧仍被消费 + 权威恰执行一次；该形状由 6e 第二请求承载。

## 突变证红（M1–M7，各自 CAUGHT_EXPECTED_FAIL=True）

| 突变 | 打什么 |
|---|---|
| M1-commit-order | 两 prep 序换 target-first → 3a「源先目标后（原版 remove-then-add 同序）」红 |
| M2-dest-gate | 容器目标 `>= DEST_JAR_LIMIT` 闸致敏 → 3h「200 件满=原版同闸拒绝」红（10×21 容量有余形，唯一拒绝理由=件数闸） |
| M3-scope-sent | NoteDragSent 失聪 → 组1「原版已发=撤回（成功转移不排）」红 |
| M4-candidate-order | 候选页 6→2 降序 → 组4「原版升序逐页试」红（出口计数 2≠1） |
| M5-codec-page | sourcePage 域放宽到 8 → 6a「AREA 永不入框」红 |
| M6-ledger-kind | MarkFastTransferResult 改打 IsContainerTidy → 6d 账本帧形标记 + 全链缓存重发形红 |
| M7-fault-gate | 模块入口熔断开闸摘除 → 2c「熔断开=拒绝（同一把闸）」红 |

脚本迭代如实记录：M2/M3/M6 首跑因突变写法（CS0162/CS0652 触发编译失败、复用旧二进制）或判据
编码（UTF-8/GBK 控制台输出）为假阴性/假阳性，修正后重跑（各证据文件为修正版，覆盖记录）；M1 首
轮真证红。M3 二次确认=判据改 GBK 解码；M6 证据红列含「账本缓存带快速转移帧形标记」断言。

## R1（冻结包 review-freeze-r1-*；静态基线同刻留证）

- **Standards 轴（standards-reviewer 全新实例）**：`STANDARDS: FINDINGS 0硬/3气味`——硬纪律
  1–8 逐项静态核验通过（唯一出口链、登记即开关三面同步归零、契约零扩面、吞物/半改禁令、
  权威零修改、假件不掩盖、csproj 显式 Include、候选零痕迹）。三气味：S1 死常量
  `FastTransferRecoverAdapter.RefusalPreference` 无引用（Speculative Generality）→ **本轮采纳**；
  S2 refusal 词汇表双份（代码注释已具名 deliberate）→ 具名递延；S3 engine-tail 与 04 近重复 →
  具名递延。
- **Spec 轴（Spec-Reviewer 全新实例）**：`SPEC: CLEAN`——触发面/候选序（含 7→7 具名排除=忠实
  裁决的独立确认）/一笔两页零修改/Q5/范围边界/红测四勾/03·04 回归（既有测试仅加 seam 与
  NotSupportedException 桩，无削弱）逐条一手核对通过。

### R1 处置

- S1 修复：删除死常量（`review-freeze-r2-adapter-fixed-full.txt` 为删除后全文；增量说明
  `review-freeze-r2-increment-note.txt`）。行为零变化（无引用点，测试不引用）；M1–M7 证据在
  删除前取得、删除不触及任何突变目标（如实注记）。
- S2/S3 + R2 新发现（下节）在票面设计定案节补「具名递延的判断性气味」三条清单。

## R2（Standards=对 R1+R2 合并增量全新实例复审；Spec=对修复增量全新实例复审）

- **Standards 轴（standards-reviewer 全新实例）**：`STANDARDS: FINDINGS 0硬/2气味`——S1 修复
  完整性核实（全库 grep 仅剩 04 自有符号，无连坐）；refusal 双词汇表判定**具名递延成立**；
  新发现：engine-tail 的 `CaptureLocalHotkeys/AfterFailed` 与 04 逐字近重复而票面当时只具名了
  整体形状（`ReadPages` 已复用为对照证据）→ 补记入票面递延清单（判断性气味，不阻塞，后续恢复面
  出现时随专门收敛票处理）；门控骨架平行判定=复制的是骨架非算法，spec「入口不得复制算法」指
  排版层，**不违例，作形状记录**。
- **Spec 轴（Spec-Reviewer 全新实例）**：`SPEC: CLEAN`——独立复核意图窗口对 vanilla 逐字忠实
  （选中短路/Ctrl/isStoring/AREA 排除/发包撤回/绑定形状）、候选序 2..6 对 U3-SDK 2..7 扫描的
  忠实性结论、两页一笔、Q5、范围、红测四勾、03/04 回归，全部通过。

## 终态静态验证（R1 后 + R2 修复后各一轮，文件同点留证）

- `green-sln-rebuild-final.txt` / `green-sln-rebuild-r2.txt`：Rebuild **0 警告 0 错误**。
- `green-v505-group-run.txt` / `-r2.txt`：六组 **ALL GREEN**（触发面与接线范围/生命周期登记即开关/
  决策矩阵与只整理接收侧/统一排版出口消费/事务层离场物品语义/不发整理完成与线协议全链）。
- `green-fullsuite-*` / `green-fullsuite-r2-*`：7 套件全 exit 0（Plugin/ClientUi/Contracts/Network/
  Placement/Release/Settings）。
- 6 门禁：NoUiTokens-core PASS（30 文件）、NoUiTokens-contracts exit=1 **命中=
  `src/BetterUnturnedExperience.Contracts/ContractTypes.cs:Glazier`——V2-02 既有注释自述禁词，
  与 DEV-V5-03/04 基线逐字同文件**（如实双记，非本票增量）；DeveloperHandbook /
  RefreshModelDeduped / SpecV4R9Ingested / TestFixturesTracked / TestRunnerHostDllsProvisioned
  全 PASS。
- 既有测试适配具名：04 `Group()` 补 `FastTransferPatchInstallerForTests` seam（Start 现装两面，
  缺 seam 会经本票自设的在册互撤纪律整体回滚——适配是生产新形状的正确镜像，非削弱）；03/04/Program
  三处假权威补 `ExecuteServerFastTransferRecover`（ NotSupportedException 显式跨界告警形）。

## 闭环判定

- Standards：R2 无硬违例；两残余气味 + 新补记一项全部具名递延（票面清单）→ **CLEAN**。
- Spec：R2 `SPEC: CLEAN`（R1 亦 CLEAN，R2 全新实例独立确认）。
- 链 = R1(0硬/3气味 + CLEAN) → S1 修复 + 具名补记 → R2(0硬/2气味具名 + CLEAN)：**闭合**。

## 候选纪律

不产候选 / 不更 RELEASES / 不授 CaseId（工作树无触碰；`changed-files.txt` 同记）。08 唯一对外。

## 具名接缝缺口（实机随 DEV-V5-08，03/04 同界）

真 Harmony 登记端到端（binder 形状宿主可证，装机解析随 08）；真提交写面（Items.addItem→Assets，
宿主同语义假事务钉计划消费形状）；真机 UI 意图探针（Ctrl/isStoring/选中守卫/容器观察）；客机
已发包而权威 `ReceiveDragItem` 竞态拒 = 原版静默不恢复；远端玩家快捷键重绑 = 原版同款遗留；
「右键菜单存放到按钮」与真拖放的碎洞不触发（票面范围）。「成功转移不排」的真机观察形状。
