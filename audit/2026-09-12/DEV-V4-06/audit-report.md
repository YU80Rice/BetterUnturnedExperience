# DEV-V4-06 审计·LIT 标题栏与 mode/direction Choice

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-06-lit-header-mode-direction.md`
- 规格：`spec.md`「LIT 标题栏与全局模式/方向（V4-T5 → DEV-V4-06）」+「共享规则（V4-T1）」；裁决 `issues/05-t5-lit-header-and-settings.md`（Q54–Q59）
- 日期：2026-09-12；会话：Phase-4 实施票 06（依赖 02✓ 已 resolved；05✓ 并行已合入）
- 构建：dotnet MSBuild Release `-t:Rebuild`，七测试工程全 CLEAN（TreatWarningsAsErrors 下 **0 警告 / 0 错误**，`final-build-console.txt`）
- 测试：**全套 7 运行器全 PASS**（`run-{Contracts,Network,Placement,Settings,Release,ClientUi,Plugin}.txt`）；静态门：`Verify-NoUiTokens.ps1` Core 30 文件 PASS、`git diff --check` exit 0、退役面 grep 0 命中（`static-gates.txt`）
- **候选纪律（本票 06）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。本审计不给本增量赋 CandidateBuild/CaseId；中间构建无身份。契约仍 2.1（本票零契约变化、零公开成员增减——`TidyUiLifecycleAction`/`RejectedPreferenceUnavailable`/`ServerRoleProbeForTests` 等全部 internal）。

## 交付摘要

LIT 标题栏从每页三按钮（模式/方向/整理）收敛为**一颗「整理」**；模式与方向退役每页内存字典，成为两条全局 `ClientPreference` Choice（`inventorytidy.mode` / `inventorytidy.direction`），在 LIT 设置页用 02 的 Cycle 控件改。三条分缝按 T5 定音：patch 只画/拆按钮；设置拥有模式与方向权威；生命周期决定按钮是否存在。

- 模块（`InventoryTidyModule.cs`）：
  - schema：`CreateSettingsDescriptors` 返回恰两条 Choice（键/档位/默认/ClientLocal 作用域/显示名=Q56 冻结；描述键留空——完整描述句子归 07，T3 空不画；`MaximumUtf8Bytes=16` 覆盖中文档位 6 字节）；
  - 生命周期事实门：`DecideTidyUiAction(FeatureState)` 九态纯函数（仅 Running→Inject；Disabled/Stopped/Isolated/Incompatible→RemoveExisting；Starting/Stopping/Isolating/Discovered→KeepWithoutNew）；`CurrentTidyUiAction` 读 `IFeatureLifetime.CurrentStatus`（无 view=fail-closed），**不由 patch 私有布尔**；
  - 点击缝：`RequestTidyFromUiClick(page, allPages)`——先再确认生命周期可用（Q54：按钮曾被画出≠有资格），再 `TryReadSavedTidyPreference` 经**一次 `GetSnapshot(ClientPreference)`** 同 revision 读出 mode+direction（Q59：不读面板草稿/每页内存/上次点击缓存；未知档位/条目缺席/view 缺席=显式拒绝 `RejectedPreferenceUnavailable`，不发明未保存过的组合），最后走既有 `RequestTidy`；每次点击现读快照（保存后下一次点击即新值）；
  - enabled 退役：`Enabled`/`ReadToggle`/`RefreshSwitches`/`EnabledSettingId`/`ToggleDescriptor` 全部移除，请求门禁只剩代际事实（`!Started || ShuttingDown`）+ 生命周期事实；
  - 代际复位：`Start` 把 `ShuttingDown=false`（修复同实例停用→再启用永不复装的粘滞缺陷——面板 enable 新代际经工厂复用 WiredModule）；
  - U3DS 不武装（T1 Q17）：`InstallPatches` 头部 headless 决策门（`BueRuntimeCompletionChain.HeadlessDecision`）→ `StartGateDiagnostics="headless-ui-not-armed"`，决策门禁非异常门禁；
  - 隔离拆除：模块向 `IFeatureLifetime.TryTrack` 登记 `UiTeardownHandle`，生命周期机 `Withdraw`（隔离与完全停止都会走）Dispose 即触发拆除（机不调 module.Stop 的隔离路径也拆干净）。
- UI patch（`InventoryTidyUiPatch.cs`）：Postfix 注入门=`ActiveModule.ShouldInjectTidyButtonForNewPage`；单按钮 60×60 @ -130（Q54 冻结）、tooltip「左键：整理当前栏；Ctrl+左键：按全局模式和方向整理全身（不含仓储栏）」；仅 headers[0..4]，STORAGE 延续不注入；点击走 `RequestTidyFromUiClick`；`RemoveInjectedButtons` 拆除（解绑回调→RemoveChild→清引用，幂等）——**Q55 红线「移除失败不得把停用伪装成成功」**：失败页引用保留、`BUE-LIT-TEARDOWN` 留痕、下次 Stop/撤回重试自愈；宿主缝 `TrackButtonForTests`/`RemoveChildForTests`（与模块 `NetServiceFactoryForTests` 同构）。
- 注册（`InventoryTidyFeatureRegistration.cs`）：Registration 恢复 `IFeatureSettingsRegistration`（两条 Choice；`OnSettingsApplied=null`——mode/direction 无需工作态刷新，点击时现读）。
- 必要修复（Core，06 的直接后果）：`SettingsRuntime.TryRead` 移除 `FileSettingMissing` 缺键即损坏门。facet 回归后宿主 runtime 先于迁移加载同一 per-feature 文档，旧 legacy 文档（只含 enabled）会被判定损坏而**隔离**，迁移 adapter 随后读到 null→静默跳过→生产升级静默丢「已停用」偏好（破坏 04 自愈保证）。digest 仍是完整性门（future-schema 与损坏文件隔离不受影响，Settings.Tests 全绿佐证）；缺键由 `LoadScope` 默认值兜底、未知键被忽略；「legacy 迁移六项」组加回归锚（legacy 文档在宿主首读后原样在位）。

## 红测先行

- 先写红测引用尚不存在的缝（`TryReadSavedTidyPreference`/`RequestTidyFromUiClick`/`RejectedPreferenceUnavailable`/`DecideTidyUiAction`/`TidyUiLifecycleAction`/`HasTrackedButtons`/`TrackButtonForTests`/`RemoveChildForTests`/`RemoveInjectedButtons`/`ServerRoleProbeForTests`）；观测 RED：Plugin.Tests 编译失败 **94 个错误（CS1061×24 + CS0117×70）**（`red-build.txt`）。
- 实现后编译过、运行红暴露两处真缺陷：①同实例再启用粘滞 `ShuttingDown`（`Start` 代际复位修复）；②两条 Choice 默认值被旧 schema/UTF-8 上限拒绝导致 registry 无 runtime→`CreateView` NRE（`MaximumUtf8Bytes=16` 修复）→继而暴露 `FileSettingMissing` 迁移窗口冲突（见上）。
- Plugin.Tests 新增六组 + 三处既有锚翻转（04 退役锚按 06 翻回）：「DEV-V4-06 九态决定按钮存在性」（九态纯表 + Stopping 点击原生回退零派发 + Running 恢复服务 + 无生命周期事实 fail-closed + U3DS headless 不武装）、「点击读同一 revision 快照」（一次 GetSnapshot 双值、未知档位/缺项/无 view 拒绝、每次点击现读）、「点击经真实协议用已保存快照」（LitMultiplayerHarness 全链：点击→快照→RequestTidy→线→服务端权威收到的 mode/desc=快照值；保存新值后下一次点击即新值、Ctrl=allPages 腿）、「停用/隔离拆除已注入按钮」（Stop 五页拆除+幂等、失败腿引用保留+重试自愈、隔离经 TryTrack 句柄 Dispose 同路径拆除）、「LIT 设置页真实消费两条 Choice（T1 检验点②）」（真实注册/目录/组合根：两行 Cycle、档位/默认/显示名/描述空不画、DraftCycle→SaveDraft→WiredModule.SettingsView 同源读到 空间）、「legacy facet 退役」翻转为「facet 回归+enabled 保持退役」；「面板目录路由」LIT 零行→两行；AssertLitSingleplayerPath enabled 腿重写为生命周期腿（含 re-arm 腿）。

## 验收条件对照

- [x] 红测先行：仅 Running 注入（九态组）；停用后拆除（拆除组 Stop 腿）；mode/direction 两条 Choice 默认与档位（消费锚组）；点击读同一 revision 快照、不读草稿（快照组+接线组）；STORAGE 不注入（循环上限 5 沿用+拆除组在册范围=页 2..6）。
- [x] 标题栏不再注入模式/方向按钮（patch 单按钮；退役面 grep 0 命中：s_PageTidyMode/s_PageSortDescending/HandleModeClick/HandleDirectionClick/布局常量/ButtonKind）；全套测试 0 警告 0 错误。
- [x] 官方先行消费锚（T1 检验点②）：真实 LIT 注册→面板两行 Cycle→草稿循环→保存→模块注入 view（点击将读的同一权威源）读到新值；enabled 不在任何行。
- [x] 双轴独立审查 CLEAN（见评审链）。
- [x] 候选纪律：不授候选、不加 RELEASES、不授 CaseId；契约仍 2.1。

## 双轴评审链（三轮闭环；每轮两全新实例）

- **Round 1**：Standards **CLEAN**（4 deferrable：死方法 ResetStateForShutdown/未用测试面/错位摘要/文档 ride-along）；Spec **FINDINGS**（1 blocking：`RemoveInjectedButtons` 失败后仍清全部引用、无重试——「移除失败不得把停用伪装成成功」未闭合；1 deferrable：归属文档 ride-along）。
- **Round 1 修复**：失败页引用保留+`BUE-LIT-TEARDOWN` 留痕+下次拆除重试自愈（测试失败腿/自愈腿）；删 `ResetStateForShutdown`；补接线级点击组（消化未用测试面）；注释归属复位。全套复跑 7/7 绿。
- **Round 2**：Standards **FINDINGS**（1 blocking：三处日志/注释与失败路径相反——Warmup「退化为仅清引用」、内层 catch「引用已清空」、Stop「静态表已清」无条件；4 deferrable）；Spec **FINDINGS**（1 blocking：显示名「整理模式/整理方向」是 Q56 冻结列、本票义务，「完整中文**描述句子**归 07」不豁免显示名；1 deferrable：归属文档）。
- **Round 2 修复**：三处日志/注释如实化（按 `HasTrackedButtons` 区分收尾日志）；`DisplayNameKey=整理模式/整理方向` + `DescriptionKey=空`（07 落描述句子）+ 消费锚组补显示名与「描述空不画」断言。全套复跑 7/7 绿。
- **Round 3**：Standards **CLEAN**（3 deferrable：两 catch 缺同一 diagnosticId、死 Obsolete 常量、NativeFallback 文案略窄）；Spec **0 blocking**（1 deferrable：归属文档 ride-along）。**双轴 0 blocking=CLEAN 链闭合。**
- **Round 3 后 deferrable 落地**（零行为变更，全套复跑 7/7 绿后收尾）：两 catch 补 `BUE-LIT-TEARDOWN`、删死 `STORAGE_TIDY_POS_OFFSET_X`、NativeFallback 文案改「功能当前不可用（生命周期非 Running 或已停止，原生回退）」。Spec deferrable=归属文档与 CONTEXT.md phase-4 挂起条目拆分为独立 docs 提交（不入本票 feat 提交）。

## 遗留与移交

- **画面级验收归 09**：宿主无法构造 Glazier——按钮几何/tooltip/注入与拆除的实机表现由 DEV-V4-09 实机面验收（规格 Testing Decisions 第 9 条；本票以常量冻结+在册范围+宿主缝测试覆盖可测部分）。
- **07 落描述句子**：`inventorytidy.mode`/`inventorytidy.direction` 的 DescriptionKey 现为空（行不画）；07 按 T6 设置文案表补「同类：把相同物品聚在一起；……」两句。
- deferrable 未做（具名）：`SettingsRuntime.TryRead` 的 `descriptors` 形参在缺键门移除后不再使用（签名兼容保留）；patch 三张同页表未合并结构体；接线组临时 fault 目录沿用既有 harness 泄漏模式。
- 本票零公开契约成员增减；`ClientUi satellite=null` 维持（Q58）。

## 审计文件

`red-build.txt`（编译红 94）`final-build-console.txt`（0/0）`run-*.txt`（7/7）`static-gates.txt`（NoUiTokens/diff-check/退役面 grep）`changed-files.txt`/`changed-files-stat.txt`（diff 文件本地留存，.gitignore 排除 .diff）
