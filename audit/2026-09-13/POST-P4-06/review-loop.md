# POST-P4-06 审查链：V4-R9 实时注入裁决并入 Phase-4 规格正文

- 票：`.scratch/bue-post-phase4-closure/issues/06-v4-r9-spec-ingest.md`（enhancement·纯文档票：裁决正文并入 spec + 地图指针，不改产品行为）
- 增量：`.scratch/bue-v2-phase4-visual-experience/spec.md`（LIT 节 V4-R9 段 + Testing Decisions §6 泵路径五锚）、`.scratch/bue-v2-phase4-visual-experience/map.md`（V4-R9 决策尾句改指 spec 正文）、新增 `eng/Verify-SpecV4R9Ingested.ps1`（本票红测缝=spec↔地图↔Plugin.Tests 三向区域绑定逐字锚）。**src/ 与 tests/ 零改动**（`git diff --exit-code -- src/` 实证 + 终态 `git status` 空），不触契约（仍 2.1），不授候选，`audit/RELEASES.md`/`publish/` 零触碰。

## 验收对照

- [x] **spec 正文含 V4-R9 与静默短路，与 v5 行为一致**：五条期望行为逐段落进 LIT 节（构造路径覆盖在册不去重 ↔ `InventoryTidyUiPatch.cs:424-442`；既有 Tick 泵 16 拍节流首拍即试幂等补注入 ↔ `InventoryTidyModule.cs:561-582`+`InventoryTidyUiPatch.cs:449-461`；非 Running 不新增 ↔ 同上 Running 门禁+`Program.cs:11951` 锚；拆除失败页引用保留→泵跳过→下次拆除重试 ↔ `InventoryTidyUiPatch.cs:104-142,525-529`；全页在册静默短路真做事才打日志 ↔ `InventoryTidyUiPatch.cs:453-460`+`Program.cs:11934`）。Spec 轴 R2/R3 两轮逐条对码 PASS。
- [x] **不把 60×60/-130 升格为全局 UI 契约**：原「坐标与 tooltip 是 LIT 实现约束」条目一字未动，新段无任何全局尺寸措辞（spec:107）。
- [x] **不授候选、不改 RELEASES**：范围实证如上；地图 Decisions 指向 spec 对应段=同行四锚（已并入 spec/POST-P4-06/门禁名/节名）并由 M3 红证锁死。

## 红绿链（红测缝=eng 门禁）

- 红=门禁 R1 版对未并入正文的 spec/map：`red-spec-gate-run.txt` exit=1、16 违例（测试锚当时已在=红恰指文档缺口）。
- 绿=正文并入后：`green-spec-gate-run.txt` exit=0。
- 突变矩阵（临时树三/四目标文件副本，正文不回潮）：R2 版 M1 条款出块/M2 旧口径追加/M3 决策行删节名指向=`red-mutation-M1/M2/M3-run.txt` 各 exit=1；R3 终态复跑+新增 M4 组名锚漂移 Testing 节外=`red-mutation-M1..M4-rerun-r3.txt` 各 exit=1，各红在对应检查；终态绿=`green-spec-gate-rerun-r3.txt` exit=0。
- 全套基线：Rebuild 0 警 0 错（`green-sln-rebuild.log.txt`）；7 exe R1 全绿（`green-fullsuite-*.txt`）+终态复跑 7/7 exit=0（`final-fullsuite-*.txt`）；`Verify-TestFixturesTracked.ps1` PASS（`gate-verify-fixtures.txt`）；`Verify-NoUiTokens.ps1` 留档为参考件（`gate-verify-nouitokens.txt`，既有基线漂移=POST-P4-04 已具名仓库级另案，本票 src 零改动不引入不加重）。

## 双轴链（每轮全新实例，standards-reviewer / Spec-Reviewer）

- **R1**：Standards CLEAN（3 条判断类气味具名=作用域写法混用、Test 版本缺文件死分支、头注释 verbatim/five-clauses 名不副实）；Spec **NOT CLEAN**——gap=门禁仅子串存在检查，无段落/同行绑定，旧口径「待并入」残留无禁用。
- **R2**（修复=门禁重写：V4-R9 条款锚绑 LIT 块切片、Testing 锚绑节起点、map 四锚绑决策同一行、双文件禁用『spec 冻结正文不动/时并入正文』；顺带闭 R1 三气味——期间自抓 `Test-Missing` 分支写反致三文件误报 missing，已正方向并红绿复证）：Standards CLEAN（2 条具名=禁用表重复、Testing 节扫到 EOF 绑定略松）；Spec **NOT CLEAN**——余 1 gap=Testing §6 锚未绑节边界。
- **R3**（修复=Testing 节切片到下一 `#` 标题行、禁用表归并单数组；M4 突变证明节边界有牙）：**Standards CLEAN + Spec CLEAN**，双轴均无未闭合具名项（R2 两条具名气味均在本轮闭合，无须递延）。

链=三轮（R1→R2→R3），每轮修复后全新实例复审，终态双轴 CLEAN。

## 具名说明

- 本轮 R2/R3 仅改 eng 门禁与审计留档，spec/map 正文自 R1 定稿后零改动——全套基线在 R1 终态跑过，src/tests 此后字节不变，R3 终态复跑 7/7 再证。
- 留档命名 `changed-files.txt`（本票单轮快照含三文件清单）先例为 `changed-files-r1.txt` 系 R1 版遗留名；Standards R1 具名为命名小差不阻塞。
- 门禁锚为故意冻结的脆锚（措辞漂移即红）：设计意图=spec 条款/地图指针/测试组名三侧任何一侧被改写都要显式过闸，不视为缺陷。
