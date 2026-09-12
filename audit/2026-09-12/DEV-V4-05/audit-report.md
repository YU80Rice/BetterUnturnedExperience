# DEV-V4-05 审计·功能级启停详情页表面

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-05-feature-toggle-ui.md`
- 规格：`spec.md`「功能级启停表面（V4-T4 → DEV-V4-05）」+「共享规则（V4-T1）」；裁决 `issues/04-t4-feature-toggle.md`（Q44–Q53）
- 日期：2026-09-12；会话：Phase-4 实施票 05（依赖 02✓03✓04✓ 已 resolved）
- 构建：dotnet MSBuild Release `-t:Rebuild`，七测试工程全 CLEAN（TreatWarningsAsErrors 下 **0 警告 / 0 错误**）
- 测试：**全套 7 运行器全 PASS**（`green-fullsuite.txt`）；静态门：`Verify-NoUiTokens.ps1` Core 30 文件 PASS、`git diff --check` exit 0（`static-gates.txt`）
- **候选纪律（本票 05）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。本审计不给本增量赋 CandidateBuild/CaseId；中间构建无身份。契约仍 2.1（本票零契约变化、零公开成员增减）。

## 交付摘要

功能详情页获得**功能级启停表面**：一颗「启用」开关=保存后的目标状态（进 01 草稿，点「保存配置」才经 03 目标提交缝交给生命周期机）；当前功能状态仍是只读中文投影（面板不再 `FeatureState.ToString()`）。

- 模型（`ManagementPanel.cs`）：新增 `PanelFeatureStatusView`（ShowsEnableToggle / EnableToggleTarget / EnableTogglePending / PendingEffectText / StateText / PresentationText / IsolationReason）与 `GetFeatureStatusProjection(stableId)`；`BueFeatureManagementEntry` 增配 `StopReason` / `StatusDiagnostic`（可选参数，既有调用点不动）；`DraftSetFeatureEnabled` 加门禁——无开关状态（Incompatible/未映射）拒绝启停意图（表面与模型同一门禁）。
- 状态投影（Q46/Q50 九态）：Discovered=待启动、Starting=启动中、Running=运行中、Stopping=停用中、Isolating=隔离处理中、Isolated=已隔离、Incompatible/未映射=不可用；Disabled/Stopped 结合停用原因——UserDisabled=已停用，其余=安全文案「已停止」（不伪装成用户停用）。表现状态独立一行（不适用/可用/表现降级/仅主机端/失败）。隔离原因仅 Isolated 且有值才投影（不占空位）。
- 组合根（`ClientUiCompositionRoot.cs`）：`ToManagementEntry` 与 `OfficialManagementEntry` 把宿主状态机的 `StopReason`/`DiagnosticId` 随条目喂给面板（BII 状态行保持 DEV-15D 组件源，「谁停的」归机器事实；无机器记录→安全文案）。
- 原生面板（`BueNativeManagementPanel.cs`）：BUE 功能详情渲染「功能状态：」中文行→隔离原因（有值才画）→「表现状态：」→「启用（保存后生效）」开关（草稿目标，OnValueChanged→`DraftSetFeatureEnabled`→重渲染）→待生效提示（有值才画）。外部插件条目分支零改动（无开关）。
- 冻结文案形：待生效提示=「{状态}，保存后将停用/启用」；Isolated+启用=「已隔离，保存后将尝试启用」（Q44 原文，不承诺一定成功）。

## 红测先行

- 先写 `DevV4FeatureToggleSurfaceTests.cs`（10 组）引用尚不存在的 `GetFeatureStatusProjection`/`PanelFeatureStatusView`；观测 RED：ClientUi.Tests 编译失败 `CS1061 ManagementPanelModel 未包含 GetFeatureStatusProjection`（`red-build.log`）。
- 实现模型后转 GREEN；补组合根/原生面板后 ClientUi.Tests 保持 PASS。
- Plugin.Tests 新增「功能级启停表面投影 05」组（组合级官方先行消费锚，T1 检验点 ①）：真实目录+StartCatalog 上投影 Running=运行中+有开关 → 面板停用受理 → 投影 Stopped+UserDisabled=已停用（停用原因随组合根走）→ 再启用=运行中。

## 验收条件对照

- [x] 红测先行：有开关 iff 可停止 seam（BueFeature 条目×八映射态有开关；Incompatible/未映射无开关；外部插件/未知条目无开关）；九态中文映射（待启动 ≠ 启动中 显式断言）；草稿目标与只读状态分离（改开关状态行不动、拨回不脏）；保存走 03 目标提交缝（handler 恰一次、目标值断言、拒绝留草稿）。
- [x] 外部插件详情无启停开关；全套测试 0 警告 0 错误。
- [x] 官方先行消费锚（T1 ①）：ClientUi.Tests 模型链 + Plugin.Tests 组合级组（真实 BueClientUiCompositionRoot）——面板自身完成草稿+保存+功能级启停，BII 经 `OfficialManagementEntry` 同一表面。
- [x] 双轴独立审查 CLEAN（见下）。
- [x] 候选纪律：不授候选、不加 RELEASES、不授 CaseId。

## 双轴独立审查链（每轮全新实例）

### Round 1
- Standards（standards-reviewer）：**CLEAN**，3 项 deferrable smell（组合根重复 TryGetStatus 块 / ProjectFeatureStateText 与 StateHasEnableToggle 重复映射 / StopReason+StatusDiagnostic 与 Pending 双通道 data clumps）。
- Spec（Spec-Reviewer）：**NOT CLEAN**，F1：
  - F1（Q45 iff seam）：开关门禁按状态枚举而非「拥有可停止生命周期 seam」的所有权事实——组合根对机器未跟踪条目兜底 Running，会让无 seam 条目画出保存必失败的死端开关。审查建议其投影为「不可用」。

### Round 2（修 F1 后，全新实例）
- 修复：`BueFeatureManagementEntry` 增配显式所有权事实 `HasStoppableLifecycle`（可选参数默认 true=目录 BUE 功能先例）；组合根抽取 `TryReadMachineLifecycleFacts` 单点读取（State/StopReason/Diagnostic/跟踪与否，同时消除 Standards smell #1），`ToManagementEntry` 与 `OfficialManagementEntry` 均喂入真实 seam 所有权；模型投影与 `DraftSetFeatureEnabled` 门禁改为 `HasStoppableLifecycle && StateHasEnableToggle(state)`。
- 审查建议的「无 seam 投影为不可用」未采纳：Q46 九态状态表与 Q45 seam 判据是两条独立裁决——`不可用` 保留给 Incompatible/未映射状态；无 seam 条目的状态行仍按九态映射如实显示（组合根 Running 兜底为既有 DEV-V3-06 先例，本票不改），仅去掉开关与启停意图路径。
- 红测先行（修复轮）：`red-build-r2.log` 观测 CS1729（条目构造缺第 9 参）；ClientUi.Tests 新增 `SeamOwnershipGatesToggleEvenWhenStateMapped`；Plugin.Tests 新增「无缝条目不开关 05」组（登记但刻意不 StartCatalog 的真实目录条目→无开关、模型拒意图）。修复中曾发现 `ToManagementEntry` return 漏传第 9 参被该组当场拦截（红→修→绿）。
- Standards（standards-reviewer）：**CLEAN**（报告 `standards-round2.md`；deferrable：Repeated Switches / Data Clumps / `hasStoppableLifecycle=true` 缺省的 fail-open 张力——生产路径已显式喂入，均不升级）。
- Spec（Spec-Reviewer）：**CLEAN**（报告 `spec-round2.md`；F1 闭合确认、Q46 状态表 vs Q45 seam 判据独立裁定成立、具名判断 1–4 无越权）。

**结论：双轴链于 Round 2 全 CLEAN（Round 1：Standards CLEAN / Spec NOT CLEAN F1 → 修复 → Round 2 双轴 CLEAN）。** 中间提交无 CaseId；身份只授予已审产物，本票按 05 纪律不赋候选身份。

## 范围红线核对

- 不做立即启用按钮（面板无立即启停路径；唯一写路径=SaveDraft→TryToggleFeature）；目标差异投影不进 SDK（纯 ClientUi 内部类型）；外部插件无进程级启停（分支未触碰）。
- `bue.network` 良性隔离文案未改（`RenderNetworkTakeoverDetails`/adapter 文案原样）。
- 管理面板自身与核心 Host/Contracts 非目录条目，天然无开关（Q45 由条目种类门禁覆盖）。

## 具名判断（deferrable / 相容解释）

1. **表现状态中文映射**（不适用/可用/表现降级/仅主机端/失败/未知）：裁决 Q46 仅钉「表现状态独立一行」未钉文案；属 ClientUi chrome（public≠契约），与「功能状态仍是只读中文」同语义域，故同轮中文化。未映射表现态给「未知」防伪。
2. **Discovered/Stopping/Isolating 的开关基线**：沿用 DEV-V4-01 冻结推导 `IsFeatureCurrentlyEnabled`（Running/Starting=开，其余=关）；Discovered 保存启停在现网机器语义下属 invalid-state 拒绝→意图留草稿+失败文案，属机侧诚实拒绝，不在本票扩机器语义。
3. **PendingEffectText 对 Isolated+停用**同样给「保存后将停用」句形：机侧解释为空操作成功（保持隔离），但停用意图照常落盘（03 事实），提示描述的是目标差异而非过渡承诺（Q44 原文形）。
4. 无开关键时 `EnableTogglePending` 仍可= true（草稿持有旧意图而条目转为不可停状态）——原生层不画开关也不画提示，保存路径由机裁决，模型不静默丢弃用户编辑。

## Seam gaps

- 无「纯宿主无法构造」缺口：表面/投影/门禁/保存链全部经 `ManagementPanelModel` 模型缝断言（ClientUi.Tests），组合级经真实 `BueClientUiCompositionRoot`（Plugin.Tests）。Glazier 开关外观与页面布局属 UI，单元层不断言控件树（Testing Decisions「只测外显行为」），实机面归 DEV-V4-09 画面验收。

## 变更文件

见 `changed-files.txt` / `changed-files-stat.txt`；增量 diff `dev-v4-05-tracked.diff`（`DevV4FeatureToggleSurfaceTests.cs` 为新增文件，内容随提交入库）。构建/测试与静态门证据：`green-fullsuite.txt`、`static-gates.txt`、`red-build.log`。

注：工作区另有两处**开工前已存在**的未提交改动（`CONTEXT.md` 词汇入典、`docs/third-party/UnturnedPluginManager-attribution.md` 技能包补记），属开图轮/attribution 工作，不属本票，未纳入本票提交。
