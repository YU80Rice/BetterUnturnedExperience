# POST-P4-08 审查链：测试运行器宿主 DLL 构建期 CopyLocal，克隆可直跑全套

- 票：`.scratch/bue-post-phase4-closure/issues/08-test-runner-host-dlls.md`（bug：干净克隆 `git clone` 后全解构建 exit 0，但直跑 7 exe 时 Plugin.Tests 在 `AssertSingleDllAssemblyClosure` 抛 `FileNotFoundException: BepInEx, Version=5.4.23.5`——宿主 DLL 不随构建进 bin）。
- 增量：`tests/BetterUnturnedExperience.Plugin.Tests/*.csproj`（对 `..\..\..\Libs` 的 7 个宿主 `<Reference>` 由 `Private=False` 改 `Private=True`）+ 新增 `eng/Verify-TestRunnerHostDllsProvisioned.ps1`（本票红测缝）。固定点 HEAD=`85f8e6a`；实现提交 `b5ce0af`（仅两文件）。契约仍 2.1，不授候选，`audit/RELEASES.md`/`publish/` 零触碰。
- 机制选型：`Private=True`（CopyLocal）经 RAR 连 `Libs` 内传递依赖一次拷齐，克隆 fresh bin 自足。**不**用 `<None CopyToOutputDirectory>`——仓库外路径会被夹具门禁 `Verify-TestFixturesTracked.ps1` 判「逃逸/未跟踪」；且该路线才不动 `*.dll` 全局禁令、零 DLL 入库。

## 验收对照

- [x] **干净克隆（worktree 法）构建后不做任何手工拷贝，7 exe 全绿**：`bue-p4-08-clone` 置于 `DevelopMyUNMultiplayerModAndModloader/` 同层使 `..\..\..\Libs` 如开发环境解析；构建前 `bin/Release/*.dll` 计数=0（无手工预置），全解 Rebuild exit 0 后自足 27 个宿主/传递 DLL，7 exe 直跑 EXIT=0（`clone-fullsuite-*-run.txt`）；Plugin 运行含此前失败的 `DEV-14/DEV-16B plugin runtime tests: PASS`（非空洞）。克隆已 `worktree remove`+`prune`，主树仅余本票跟踪件。`clone-green-summary.txt`。
- [x] **`git status` 无新增 DLL 入库；`*.dll`/`*.log` 规则零放宽**：未触 `.gitignore`；克隆工作树无额外 DLL 跟踪；`git status` 无 `*.dll`。
- [x] **全套 7/7 在工作区同样成立；夹具门禁同步跑绿**：工作区 Rebuild 0/0（`green-sln-rebuild.log.txt`）、7 exe 全绿（`green-fullsuite-*-run.txt`+`green-fullsuite-summary.txt`）、`Verify-TestFixturesTracked.ps1` 工作区 PASS 2 项（`green-gate-fixtures-workspace-run.txt`）、新门禁工作区 PASS 7 引用 CopyLocal（`green-gate-run.txt`）。
- [x] **双轴 CLEAN**（见下）。
- [x] **副作用评估（票面点名）**：`AssertSingleDllAssemblyClosure`（Program.cs:10865）只核 plugin 程序集 `GetReferencedAssemblies()` 的**引用名**（禁 Core/Contracts 外泄），与输出目录文件数无关；改动只在测试运行器侧，候选程序集引用不变，工作区+克隆 Plugin.Tests 皆绿，闭包语义未被「装满的 bin」破坏。

## 红绿链（红测缝=eng 门禁）

- 红=门禁对未 CopyLocal 的 `Plugin.Tests.csproj`：`red-gate-run.txt` exit=1、7 违例（7 外部宿主引用全 `Private=False`）。
- 绿=7 引用改 `Private=True` 后：`green-gate-run.txt` exit=0（7 CopyLocal）。
- 突变：M1 单引用（UnityEngine.CoreModule）回退 `Private=False`=`red-mutation-M1-run.txt` exit=1、恰 1 违例/6 CopyLocal，证逐引用粒度非全有全无；已还原。
- 候选洁净：src Plugin/NoOpFixture 宿主引用刻意留 `Private=False`（发布 DLL 禁携宿主 DLL），新门禁只扫 `tests/` 故不触 src；工作区 Rebuild 后候选 bin 宿主 DLL 数=0。
- 范围外（票面）：CI 基础设施、`Libs` 版本升级（BepInEx 5.4.23.5 钉未动）——均未尝试。

## 双轴链（每轮全新实例，standards-reviewer / Spec-Reviewer）

- **R1**：Standards **CLEAN**（硬性 0；4 条判断型气味具名，见下）。Spec 报 **1 gap**——纯证据完整性：工作区验收 (c) 只由 `clone-green-summary` 汇总声称夹具门禁绿，缺独立工作区 transcript；代码无偏差。
- **修复**：补跑并落盘工作区 `Verify-TestFixturesTracked.ps1` PASS transcript（`green-gate-fixtures-workspace-run.txt`）+ 全套汇总（`green-fullsuite-summary.txt`）；代码增量字节不变。
- **R2（全新实例）**：Standards **CLEAN**（R1 四条经当前仓库实况复核均不升格为硬违规，无新增）+ Spec **CLEAN**（R1 gap 经新 transcript 闭合，(a)(b)(c) 逐项有据、克隆非空洞）。

链=R1→（证据补齐）→R2 双轴全 CLEAN。

## 具名递延项（Standards 判断型气味·不阻塞）

1. 自闭合 `<Reference .../>` 带 HintPath/Private 属性形态会被 `[^/>]` 开标签漏检——现仓库无此形态，与 sibling 门禁同为 regex-on-XML 惯例。
2. `<Private>FALSE</Private>` 全大写不匹配 `[Ff]alse`——活体全用 `False`，MSBuild 大小写不敏感是已知缝。
3. 注释「test-runner projects」对扫描集「所有 `tests/*.csproj`」略宽——今日 7 个 tests 工程皆 `OutputType=Exe`，扫描集=runner 集。
4. 0 条外部引用时 PASS 仍称「self-sufficient」——绿路径实为 7 条，措辞精度判断题。

## 结论

红测先行（门禁红 7→绿→M1 逐引用粒度）+ 全套工作区 7/7 + 干净克隆 7/7 无手工拷贝 + 双轴两轮（R1→补证→R2 全 CLEAN）闭环。不授 SHA-256 / CaseId / RELEASES 行。
