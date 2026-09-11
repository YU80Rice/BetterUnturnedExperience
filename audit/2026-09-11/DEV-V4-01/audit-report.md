# DEV-V4-01 审计·未保存草稿与配置保存模型

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-01-draft-save.md`
- 规格：`spec.md`「未保存草稿与配置保存（V4-T2 → DEV-V4-01）」+「共享规则（V4-T1）」；裁决 `issues/02-t2-draft-save.md`（Q21–Q34）
- 日期：2026-09-11；会话：Phase-4 实施票 01（前沿票，无阻塞）
- 构建：MSBuild（VS 18 Insiders）Release `-t:Rebuild` → **0 警告 / 0 错误**
- 测试：**全套 7 运行器全 PASS**（`green-fullsuite.txt`）；静态门：`Verify-NoUiTokens.ps1` Core 28 文件 PASS、模型两文件 UI/native token = 0、`git diff --check` exit 0
- **候选纪律（本票 01）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。本审计不给本增量赋 CandidateBuild/CaseId；中间构建无身份。契约仍 2.1（本票零契约变化）。

## 交付摘要

把「改一项立刻写权威源」的面板写入路径改为**未保存草稿 → 点保存才交给权威源**，只交付模型 + 命令缝（保存 / 放弃 / 确认），原生 Glazier 先接到模型命令。

- 模型（`ManagementPanel.cs`）：新增内部类型 `PanelConfirmChoice` / `DraftSaveOutcome` / `DraftSaveReport` 与私有 `DetailDraft`；新增 `OpenDetail / HasOpenDetail / OpenStableId / IsDirty / DraftEditBueSetting / DraftEditPluginConfig / DraftSetFeatureEnabled / IsBueSettingDirty / IsPluginConfigDirty / IsFeatureEnableDirty / EffectiveBueSettingValue / EffectivePluginConfigValue / SaveDraft / DiscardDraft / TryLeaveDetail`。
- 原子提交缝（`IBueSettingsEditor.ApplyBatch`，internal）：一次 `SettingsRuntime.Submit` 承载整条草稿字段（`SettingsRuntimeBueEditor`、`CatalogRoutingBueSettingsEditor`、`BetterItemInteractionSettingsEditor` 三处实现 + 两处测试假件）。
- 原生面板（`BueNativeManagementPanel.cs`）：设置/外部配置行编辑改走草稿；始终可见的「保存配置」+ 脏时的「放弃修改」；脏时换条/刷新/关闭/Escape 经「修改尚未保存，要保存吗？」[保存/不保存/取消]；真实关面板结束会话丢草稿、重挂不丢。

冻结文案逐字对齐：`修改尚未保存，要保存吗？`、`没有需要保存的修改。`、`配置已保存。`、`未保存：设置已在别处变更。`、`未保存：插件已卸载。`。

## 红测先行

- 先写 `DevV4DraftTests.cs`（草稿/保存 10 组）+ `Settings.Tests` `RunV4DraftSubmitAtomicity`（原子性/ExpectedRevision 依赖锚），引用尚不存在的模型 API。
- 观测 RED：ClientUi.Tests 编译失败 `CS0246 DraftSaveReport`（及后续缺失成员），证明测试驱动出该 seam。Settings.Tests 锚仅用既有 `SettingsRuntime` API，作为草稿依赖属性先绿。
- 实现模型 + `ApplyBatch` 后转 GREEN；Plugin.Tests 新增官方锚组（真实 `BueSettingsRuntime` 上 `inventorytidy.enabled` 草稿→保存）。

## 双轴独立审查链（每轮全新实例）

### Round 1
- Standards（standards-reviewer）：**CLEAN**，4 项 deferrable smell（见下）。
- Spec（Spec-Reviewer）：**NOT CLEAN**，F1–F7：
  - F1 Q31 不脏时「保存配置」不可见 → 修：改为始终渲染「保存配置」（`AddDraftActionButtons`）。
  - F2 Q22 关面板未全走确认 → 修：Escape 前缀改 `RequestClose`；`Close()` 结束会话丢草稿。
  - F3 Q22 确认「保存」自造文案、丢报告 → 修：`TryLeaveDetail` 增 `out DraftSaveReport`，原生 `RenderDraftReport` 复用同一报告。
  - F4 Q30 顶栏「配置已保存。」被追加「（部分项需要重启）」→ 修：Success 用 `PrimaryMessage` 逐字，RequiresRestart 徽章归 DEV-V4-08。
  - F5 Q25 插件卸载却报成功（模型 bug）→ 修：`SaveDraft` 缺插件 → PartialFailure +「未保存：插件已卸载。」+ 留草稿；新增 `PluginVanishedMidSessionIsNotASuccess` 红锚。
  - F6 Q24 校验失败缺原因 → 修：过期文案保持逐字「未保存：设置已在别处变更。」，校验/其它追加工具类原因。
  - F7 接管钮即时写与注释矛盾 → 修：注释限定「行编辑」，接管记为 Q21 独立行动命令（不进草稿）。

### Round 2（修 F1–F7 后，全新实例）
- Standards：**CLEAN**（3 项 deferrable，未升级）。
- Spec：**NOT CLEAN**，F1–F7 已闭合，新增：
  - N1：`RenderDetails` 末尾会恢复 DoubleInstall/Compatibility 平台通知，冲掉刚发的冻结横幅 → 修：所有保存路径改为 `Render()/RenderDetails()` 先、`RenderDraftReport()` 后。
  - N2 Q27：外部 ConfigEntry 按编辑字典序写（非稳定顺序）→ 修：`SaveDraft` 改按 `plugin.ConfigEntries` 下标逐条写；新增 `ExternalConfigWritesInStableOrder` 红锚（乱序条目表断言 `zeta,alpha,mid`）。

### Round 3（修 N1/N2 后，全新实例）
- Standards：**CLEAN**（无阻塞）。
- Spec：**CLEAN**（无遗漏、无范围蔓延、无错误实现）。

**结论：双轴链于 Round 3 全 CLEAN。** 中间提交无 CaseId；身份只授予已审产物，本票按 01 纪律不赋候选身份。

## Deferrable smells（命名、不阻塞）

1. `pendingNavigation` 用 `null` / `""` / id 表达 无/关/切换，`pendingRefresh` 独立布尔——原始编码。后续如做原生面板重构可换小类型 `{None,Close,To(id),Refresh}`。
2. `BueNativeManagementPanel.AddBueSettingControl` 草稿改写后 `row` 形参未用（C# 不警告，不违反 TreatWarningsAsErrors）。
3. `TryLeaveDetail` 在「不脏」时 Cancel 仍导航——规格相容（不脏可走；确认框仅在脏时弹），保留。

## Seam gaps（红测场景的纯宿主可测边界）

- 无「纯宿主无法构造」的缝缺口：草稿/保存/确认/跨源/原子/过期/无自动保存全部经 `ManagementPanelModel` + 真 `SettingsRuntime` 模型缝断言（ClientUi.Tests + Plugin.Tests + Settings.Tests）。原生 Glazier 确认框外观/横幅落位属 UI，本票不在单元层断言控件树（Testing Decisions「只测外显行为」），实机面归 DEV-V4-09 画面验收。
- 启停开关外观、九态投影、Cycle 档位、外部 Cycle 识别、外部失败三类短文案：分属 DEV-V4-05 / 02 / 08，本票不实现（范围红线）。

## 变更文件

见 `changed-files.txt` / `changed-files-stat.txt`；增量 diff `dev-v4-01-tracked.diff`（`DevV4DraftTests.cs` 为新增文件，内容随提交入库）。构建/测试与静态门证据：`green-fullsuite.txt`、`static-gates.txt`。
