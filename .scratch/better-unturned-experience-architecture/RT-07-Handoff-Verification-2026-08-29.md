# RT-07：交接文档核验（BUE-Project-Handoff-2026-08-29）

- 日期：2026-08-29（Asia/Shanghai）
- 核验对象：`C:\Users\The New Age\AppData\Local\Temp\BUE-Project-Handoff-2026-08-29.md`（231 行）
- 核验基线：本仓库工作树 @ `master`，HEAD = `2bf61ce`（核验期间工作树未做任何修改）
- 核验方式：只读。每条结论附一手证据（文件:行号 / git 输出 / SHA-256）。
- 说明：本报告原计划由后台研究 agent 产出；该 agent 在证据闭合后、写盘前失败，由主会话补完并留档。

## 一句话结论

交接文档与仓库现状**高度吻合**（Git 状态、事实源、未提交代码、测试与 DLL 产物全部证实），但存在一处高优先级偏差：**交接文档声称的「`AGENTS.md` 含编译闭环、独立审核、冻结和报告归档硬性规则」与仓库现状不符——该规则未在任何文档中成文落盘**。

## 核验汇总表

| 条目 | 核验内容 | 结论 | 关键证据 |
|---|---|---|---|
| A | Git 提交列表与 7 个未提交修改文件 | ✅ 证实 | `git log --oneline -12` 与文档第 4 节一致；`git diff --name-only` 恰为文档所列 7 个文件 |
| B | 第 1 节 8 项事实源 + 2 份 freeze-diff | ✅ 证实 | 12 项全部存在（含 `audit/2026-08-28/` 三份与 `eng/Verify-NoUiTokens.ps1`） |
| C | 「AGENTS.md 含编译闭环/独立审核/冻结/归档硬性规则」 | ❌ **不符** | 仓库根 `AGENTS.md` 仅 13 行（Agent skills 块）；`CONTEXT.md`/`docs/`/`eng/` 中「冻结、编译闭环、报告归档、独立审核」零命中；规则散落 `.scratch` spec，无成文流程规则文档 |
| D | DEV-01～16E 工单状态对照 | ⚠️ 部分证实 | DEV-16A `resolved`、DEV-16B `ready-for-human`、DEV-03/04 `ready-for-agent`、DEV-16C/D/E `ready-for-agent` 均与文档一致；但 **DEV-05～DEV-16 工单文件均无 `Status:` 行**，resolved 判定无法从工单一手证实 |
| E | 未提交代码内容（交接文档 5.1/5.2） | ✅ 证实 | `BueRuntimePump.cs:11` `BuePluginUpdateDriver`；`BetterUnturnedExperiencePlugin.cs:45,47,131-133,197-203` Awake 创建 driver/`CanBindNativeUi` 门禁/Update 转发/OnDestroy 清理；`BueNativeManagementPanel.cs:29,54-56` 三路入口 + `BueButtonInjectionCoordinator` |
| F | 测试项目数与构建产物新旧 | ✅ 证实（附注） | `tests/` 下恰好 7 个测试项目；`Plugin.Tests.exe`（20480 bytes, 08-28 15:38）晚于全部未提交源码（最晚 15:37），基线是新的；全套测试日志未保存，交接文档要求重跑仍然成立 |
| G | r9 DLL 产物 | ✅ 证实 | `artifacts/DEV-16B-management-panel-runtime-fix-r9-20260828/BetterUnturnedExperience.dll` 恰为 **159744 bytes**；SHA-256 `0C15885698D0762491B9C39C8E5578B5C50EBECFD71C2EC7200AA375B8DA7A9C`，与 R9 诊断记录一致；**r10/20260829 目录不存在**（符合预期） |
| H | 部署边界声称 | ✅ 证实 | `CONTEXT.md:89`「运行时不依赖外部 PluginManager DLL」、`:93`「署名不等于运行时程序集依赖」；Headless 禁 UI 规则落盘于 `Feature-Definition-Pipeline-Spec.md:115,133`、`issues/01-dev-16a-...md:13`、`issues/14-contribution-and-release-gates.md:49` |

## 逐条核验详情

### A. Git 状态

`git log --oneline -12`：`2bf61ce` → `e883f04` → `478b0e0` → `fc77b07` → `d7b9204` → `8bbad69` → `d3c5c51` → `e1cf116` → `611af08` → `aad3f06` → `4b031aa` → `5d80b25`。交接文档所列 10 条提交全部在列（文档未列 `aad3f06`、`5d80b25`，但其表述为「包括」，不构成偏差）。

`git diff --name-only` 输出恰为交接文档所列 7 个文件：

```
src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs
src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs
src/BetterUnturnedExperience.Plugin/BueRuntimePump.cs
src/BetterUnturnedExperience.Plugin/ClientUiCompositionRoot.cs
src/BetterUnturnedExperience.Plugin/LoadedPluginCatalogAdapter.cs
tests/BetterUnturnedExperience.ClientUi.Tests/Dev15CTests.cs
tests/BetterUnturnedExperience.Plugin.Tests/Program.cs
```

另：`git status --porcelain` 共 251 行，其中大量 `??` 未跟踪条目（`.scratch/` 大部分 spec/RT 报告、`audit/`、构建输出）——见偏差清单第 3 条。

### B. 事实源存在性

逐项确认存在（绝对路径省略仓库前缀）：

1. `CONTEXT.md` ✅ 2. `AGENTS.md` ✅（但内容与文档描述不符，见 C）
3. `.scratch/better-unturned-experience-architecture/spec-DEV-16-runtime-clientui-management-panel.md` ✅
4. `.scratch/better-unturned-experience-architecture/issues/DEV-16-runtime-clientui-management-panel.md` ✅
5. `.../issues/02-dev-16b-bue-management-panel-settings.md` ✅（`Status: ready-for-human`，已亲读）
6. `audit/2026-08-28/RuntimeDiagnosis-DEV16B-142408.md` ✅（已亲读，R9 结论与文档第 2/8 节一致）
7. `audit/2026-08-28/DEV16B-R9-freeze-diff.txt` ✅、`DEV16B-R10-freeze-diff.txt` ✅
8. 参考实现 `UnturnedPluginManager/PluginManagerMod.cs:47-50`：`Update()` 每帧直驱 `PluginManagerUI.Tick()` ✅（grep 证实）

### C. AGENTS.md 规则定位（重点疑点）

- 仓库根 `AGENTS.md` 共 **13 行**，仅含 `## Agent skills` 块（issue tracker / triage labels / domain docs 三小节）。
- 在 `CONTEXT.md`、`docs/`（含 adr/sdk/third-party/agents）、`eng/` 中以正则「冻结|编译闭环|归档|独立审核|独立审查」检索：**零命中**。
- 父目录 `DevelopMyUNMultiplayerModAndModloader/AGENTS.md` 含「架构铁规」（LaunchMultiplayerNet 命名频道、架构实验与稳定版隔离）与语言规范，但**无**编译闭环/冻结/报告归档流程规则。
- `.scratch/` spec 中存在大量「已冻结」字样，但均为**架构决策冻结记录**（如 `Backend-Architecture-Spec.md:222-230` 列 GPT-09～GPT-14 决结），并非交接文档所指的**流程硬性规则**（编译闭环、独立审核、冻结基准、报告归档）。

**结论**：交接文档第 1 节对 `AGENTS.md` 的描述不符合仓库现状；所述流程规则当前**未在任何单个文档中成文**，实际以交接文档本身 + `.scratch` 审计/handoffs 惯例承载。

### D. 工单状态对照

`issues/` 下带 `Status:` 行的 DEV 系工单实测：

| 工单 | 实测 Status | 交接文档 | 一致？ |
|---|---|---|---|
| `01-dev-16a-...` | `resolved` | resolved/静态完成 | ✅ |
| `02-dev-16b-...` | `ready-for-human` | ready-for-human | ✅ |
| `03-dev-16c-...` | `ready-for-agent` | 未开始正式实现 | ✅ |
| `04-dev-16d-...` | `ready-for-agent` | 未开始正式实现 | ✅ |
| `05-dev-16e-...` | `ready-for-agent` | 未开始正式实现 | ✅ |
| `DEV-01` | `resolved` | resolved | ✅ |
| `DEV-02` | `resolved` | resolved | ✅ |
| `DEV-03` | `ready-for-agent` | 文档状态仍为 ready-for-agent（文档特别提醒勿只看状态） | ✅ |
| `DEV-04` | `ready-for-agent` | 同上 | ✅ |
| `DEV-05`～`DEV-16` 各工单 | **无 `Status:` 行** | resolved 等 | ⚠️ 无法从工单自证，需依赖 `handoffs/` 审计文件 |

### E. 未提交代码抽查

- `BueRuntimePump.cs:11`：`internal sealed class BuePluginUpdateDriver` ✅（文档 5.1）
- `BetterUnturnedExperiencePlugin.cs:45`：`pluginUpdateDriver = new BuePluginUpdateDriver(OnPluginUpdateTick);`（Awake 内）✅
- `BetterUnturnedExperiencePlugin.cs:47`：`Initialize(isBatchMode, isBatchMode, BueNativeManagementPanel.CanBindNativeUi())` 原生门禁 ✅
- `BetterUnturnedExperiencePlugin.cs:131-133`：`Update()` → `pluginUpdateDriver.Update()` ✅；`:197-203` `OnDestroy()` 清理 ✅
- `BueNativeManagementPanel.cs:29`：`new BueButtonInjectionCoordinator(...)`；`:54-56`：`TryInject("MenuDashboardUI"/"MenuWorkshopUI"/"PlayerPauseUI", ...)` 三路入口 ✅（文档 5.2）

### F. 测试与构建

- `tests/` 下测试项目恰为 7 个：Contracts / ClientUi / Network / Placement / Plugin / Release / Settings Tests ✅
- `tests/BetterUnturnedExperience.Plugin.Tests/bin/Release/BetterUnturnedExperience.Plugin.Tests.exe`：20480 bytes，mtime 08-28 15:38 ✅
- 未提交源码 mtime 最晚为 15:37（`BueNativeManagementPanel.cs` 15:37:03、`BetterUnturnedExperiencePlugin.cs` 15:37:12、`LoadedPluginCatalogAdapter.cs` 15:37:43），早于 exe 的 15:38——**最近一次编译晚于全部当前未提交修改，基线是新的**。
- `eng/Verify-NoUiTokens.ps1` 存在 ✅。
- 附注：全套 7 个测试的运行日志未保存归档；交接文档第 6 节「仍需执行」清单（完整 Release 构建、7 个测试 exe 全跑并存日志、门禁、`git diff --check`、单 DLL ABI 闭包检查）仍然有效。

### G. DLL 产物

```
artifacts/DEV-16B-management-panel-runtime-fix-r9-20260828/BetterUnturnedExperience.dll
大小：159744 bytes（与交接文档「约 159744」完全一致）
SHA-256：0C15885698D0762491B9C39C8E5578B5C50EBECFD71C2EC7200AA375B8DA7A9C
（与 audit/2026-08-28/RuntimeDiagnosis-DEV16B-142408.md:16 记录一致）
```

`artifacts/` 下无 r10 / 20260829 目录（符合「下一位 Agent 须新建目录」的预期）。

### H. 部署边界

- 「客户端只部署主 DLL」「UPM 仅作参考、署名不等于依赖」：`CONTEXT.md:89`（管理面板词条）与 `:93`（实现来源署名词条）直接支撑 ✅。
- 「U3DS Headless 不创建 UI/Glazier/Sleek、不装客户端库存 Hook」：落盘于 `Feature-Definition-Pipeline-Spec.md:115`（「U3DS 路径不得解析或实例化 UI 类型」）、`:133`、`issues/01-dev-16a-...md:13`、`issues/14-contribution-and-release-gates.md:49` ✅。
- 注意：`CONTEXT.md` 的「U3DS 环境」词条本身不含该禁令，禁令靠 spec 与工单承载（引用时勿只引 CONTEXT.md）。

## 偏差与风险清单（按严重度）

1. **【高】流程硬性规则未成文**：交接文档声称 `AGENTS.md` 含「编译闭环、独立审核、冻结、报告归档」规则，实际 `AGENTS.md` 仅 13 行 Agent skills 块，且全仓无任何成文流程规则文档。规则目前只活在交接文档与惯例里——换一份交接材料就会失传。
2. **【中】纸面 trail 大部分未入版本控制**：`git status` 共 251 行未跟踪条目，`.scratch/better-unturned-experience-architecture/` 的全部 spec、RT-01～06、handoffs、change-requests 以及 `issues/DEV-15-...formal-implementation.md` 均为 `??` 状态。仓库的"唯一事实源"在 git 之外，一旦工作树受损即丢失。
3. **【中】DEV-05～DEV-16 工单缺 `Status:` 行**：交接文档的 resolved 判定需依赖 `handoffs/` 审计文件，工单自身不可自证；违反本仓库 `docs/agents/issue-tracker.md`「Triage state is recorded as a `Status:` line」的约定。
4. **【低】未提交修改跨多轮**：7 个修改文件的 mtime 跨 08-26 00:18（Dev15CTests.cs）到 08-28 15:38（Program.cs），说明未提交改动累积了不止一轮修复；`Dev15CTests.cs` 的修改与最近 R10 方案是否同属一个逻辑变更，提交时需甄别拆分。
5. **【信息】重复特性目录**：`.scratch/better-un-experience-architecture/`（3 个文件，整目录未跟踪）与 `.scratch/better-unturned-experience-architecture/` 并存，疑似历史遗留重复，未核验其内容是否已被后继目录取代。

## 接手建议（建议，不实施）

1. **先固化基线再诊断**：按交接文档第 6 节「仍需执行」跑完整 Release 构建 + 7 个测试 exe + `Verify-NoUiTokens.ps1` + `git diff --check`，日志归档到 `audit/2026-08-29/`，确认未提交 R10 修改的可编译基线。
2. **按 `/diagnosing-bugs` 建红色反馈回路**：以 R9 排名假设 #1 为首验——部署新 CandidateBuild 后第一个判据就是 UMM 日志中 `plugin-update` 是否 >0（区分加载→驱动→容器→AddChild→可见性五边界）。
3. **把流程规则落盘**：将「编译闭环、独立审核、冻结基准、报告归档」写成 `AGENTS.md` 的正式章节（或 `docs/agents/` 下新文件），消除交接文档与仓库现状的偏差，避免规则失传。
4. **提交时把纸面 trail 纳入版本控制**：至少将本轮涉及的工单、RT-07、spec 纳入提交范围（交接文档要求提交仅纳入「本轮明确的生产源、测试、交付报告和需要的工单更新」，RT-07 属于此类）。
5. **新证据不继承**：新 DLL 必须新目录（如 `artifacts/DEV-16B-management-panel-runtime-fix-r10-20260829/`）、新 CandidateBuild/LoadSetIdentity/CaseId、新 SHA-256；旧 r9 证据仅作对照。
