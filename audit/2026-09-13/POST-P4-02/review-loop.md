# POST-P4-02 输出审查链

票：`.scratch/bue-post-phase4-closure/issues/02-plugin-tests-log-fixtures.md`  
日期：2026-09-13  
固定点：HEAD `435e892` 工作区增量（提交前审查），冻结 diff = `review-freeze-r1.txt`（270 行，清单 `changed-files-r1.txt`）  
契约：仍 2.1（零契约面，`ContractTypes.cs` 不在增量内）  
候选：不授（非发布票，纯工程卫生）

## 红 → 绿

缺陷属性=「夹具文件未入版本库」，工作区内不可见（磁盘有文件→任何工作区单测断言恒绿），红只能构造在版本库/构建层。seam 取两层，均真实观察红→绿：

- 红-1（用户报告的原始失败面）：`git worktree` 从 HEAD `435e892` 做干净克隆（同级目录 `../Libs` 提示路径可解析），构建 `BetterUnturnedExperience.Plugin.Tests.csproj` → `MSB3030 ×2`（`dev16d-r36-no-preview.log` / `dev16d-fixed-preview.log` 找不到），exit=1。证据 `red-cleanclone-build-console.txt`（全量 flp 盘上 `red-cleanclone-build.log`，按 `*.log` 策略不入库）。
- 红-2（防回归门禁）：新增 `eng/Verify-TestFixturesTracked.ps1`——扫全部 csproj 的 `CopyToOutputDirectory` `None` 项，断言 ①盘上存在 ②未被 gitignore 吞（`git check-ignore`）③已被 git 跟踪（`git ls-files`）。修复前跑红 4 violations（两文件 × ignored+untracked），证据 `red-gate-run.txt`。
- 实现（票选「优先改后缀」）：两份夹具改名 `.log.txt`（audit 证据同族后缀，`*.txt` 入库惯例）；`Plugin.Tests.csproj` 两条 `None Include` 同步；`Program.cs:501/513` 文件名同步+一处边界注释；`.gitignore` 仅加两行注释（`*.log` 规则原样零放宽）；夹具 `git add` 入库。测试语义零改动（红/绿断言逻辑不动，仅文件路径字符串）。
- 绿：门禁 PASS 2 项（`green-gate-run.txt`）；全套 `-t:Rebuild` exit=0、真实告警/错误 0、MSB3030=0、输出目录含两份 `.log.txt`（`green-rebuild-summary.txt`；全量 flp 盘上 `green-rebuild-sln.log` 不入库）；7 exe 全绿（`green-fullsuite-*-run.txt`，PASS 行数与 DEV-V4-09 基线逐组一致）。
- 关单前终验（见下轮补充）：干净克隆从修复提交重构建+跑 Plugin.Tests。

## 双轴（Fresh-instance）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| R1 | CLEAN（硬性项无；3 条 deferrable 见下） | CLEAN（四条验收逐条满足；范围核查：Program.cs 仅文件名+注释、`*.log` 未放宽、RELEASES/publish 零 diff；新 eng 门禁判定=「夹具必须入库」边界的合理防回归面，非范围蔓延） | 关环 |

## 具名 deferrable（Standards R1，全部判定=理论缺口/当前跑法自洽）

1. 门禁正则只吃自闭合 `<None ... CopyToOutputDirectory ... />`：多行元数据/`Content` 项/通配 Include/ItemGroup 继承漏检——本仓库现存 csproj 全部单行显式风格，出现新写法时再扩 XML 解析。
2. `StartsWith($RepoRoot)` 未补尾分隔符：兄弟目录同名前缀才可能误判，且 Include 均为项目相对 `Fixtures\*`，实际打不中。
3. `git check-ignore --quiet` 退出码 0/1 语义在 PS7+`PSNativeCommandUseErrorActionPreference` 下会抛错：当前 `-NoProfile` 跑法（PS 5.1 系）自洽，门禁已验证 PASS。

## 边界记录（勿改项）

- `audit/2026-08-3x/*`、`.scratch/upm-sync-20260912/pr1-report.txt` 中的旧 `dev16d-*.log` 文件名=历史记录，不追溯改写。
- 不放宽全局 `*.log`；不 `git add -f` 救旧文件；不发布候选。

## 身份

不授 SHA-256 / CaseId / RELEASES 行。本票只动测试夹具入库形态与工程引用。
