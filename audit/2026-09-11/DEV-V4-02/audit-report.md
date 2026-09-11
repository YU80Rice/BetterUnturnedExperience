# DEV-V4-02 审计·描述行、基础控件与循环切换

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-02-panel-controls-cycle.md`
- 规格：`spec.md`「描述行与循环切换（V4-T3 → DEV-V4-02）」+「共享规则（V4-T1）」；裁决 `issues/03-t3-panel-controls.md`（Q35–Q43）
- 日期：2026-09-11；会话：Phase-4 实施票 02（依赖 01=aaf7271，已解除阻塞）
- 构建：MSBuild（VS 18 Insiders）Release `-t:Rebuild` → **0 警告 / 0 错误**
- 测试：**全套 7 运行器全 PASS**（`green-fullsuite.txt`，含 DEV-V4-02 十三组新锚）；静态门：`Verify-NoUiTokens.ps1` Core 28 文件 PASS、模型两文件 UI/native token=0、原生面板退役标签 `SettingId + " = "` 残留=0（`static-gates.txt` 中该行为 grep 计数）、`git diff --check` exit 0
- **候选纪律（本票 02）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。契约仍 2.1（Contracts/SDK 零入 diff）。

## 交付摘要

把配置行从「SettingId = 值」升级为 **显示名 → 描述（空不画、截断 120）→ 控件**，并接入**循环切换**（左键下一档 / 右键上一档 / 到头循环），改动只进 01 的草稿。

- 面板投影（`ManagementPanel.cs`，全 internal）：新增 `PanelSettingControlKind { Toggle, TextEditor, Cycle, ReadOnly }`、`PanelSettingRowView`（含 `DescriptionDisplayLimit=120`）、`PanelConfigRowView`；新增 `GetSettingRows / GetPluginConfigRows / DraftCycleBueSetting / DraftCyclePluginConfig`；草稿捕获 feature schema 与有序 id 清单；`DraftEditBueSetting` 对 Choice 关闭通用编辑缝（只走 Cycle；无档位 Choice=只读）。
- schema 联表缝（`IBueSettingsEditor.GetDescriptors`，internal，先例=01 `ApplyBatch`）：`SettingsRuntimeBueEditor`（构造注入冻结 catalog 描述符表）、`CatalogRoutingBueSettingsEditor`（逐调用自 catalog 投影回答；BII 走组合编辑器）、`BetterItemInteractionSettingsEditor`（诚实空表→回退 SettingId/值 Kind）；三处测试假件同步。
- 外部 Cycle 控件缝（`PluginConfigEntryView` + `Description` / `AllowedChoices`，默认空）：缝开在本票，**采集与失败分类归 08**（`LoadedPluginCatalogAdapter` 未动）。
- 原生面板（`BueNativeManagementPanel.cs`）：`AddBueSettingRow` / `AddPluginConfigRow` 消费行投影；Cycle 按钮 `OnClicked`(+1)/`OnRightClicked`(-1) 改草稿；只读行画「当前值：」文本、不画灰掉假控件；两处旧 `Add*Control`（含 `SettingId = 值` 主标签）退役。
- 冻结语义逐字对齐：档位优先级 policy?:descriptor 镜像 `SettingsRuntime` 校验口径；Q41 末档左键→首档 / 首档右键→末档 / 单档可编不变值不脏 / 最终值=权威值不脏；Q37 截断对齐 UPM `Truncate`（前 120 字 + `...`）。

## 红测先行

- 先写 `DevV4PanelControlsTests.cs`（13 组，引用尚不存在的投影/命令 API）并注册（csproj 显式 Include + Program.cs）。
- 观测 RED：`CS0246 PanelSettingRowView / PanelConfigRowView`（`red-run.txt`），证明测试驱动出该缝。
- 实现转绿时观测到一处**测试自身缺陷**（`CycleWrapsAtBothEnds` 以首档值断言末档行为）：修断言（先首档右键→末档、再末档左键→首档），非实现回归。之后 13 组全绿。
- 覆盖=验收判据全集：Cycle 改草稿不立刻写盘、无档位只读不降级文本框、到头循环、空描述不占位（投影空串）、截断 120、单档可编不变值不脏、策略收窄档位优先、ServerAuthority/CanEdit=false/Unsupported 只读不进草稿、Toggle/Integer/Text/KeyBinding 形状维持、显示名回退 SettingId、保存提交 Choice-kind mutation（夹具 Choice 穿控件缝=本票官方先行消费锚，LIT 真实 Choice 按票延至 06）。

## 双轴独立审查链（每轮全新实例）

### Round 1
- Standards（standards-reviewer 新实例）：**CLEAN**，无硬违反；4 项 deferrable smell（见下）。
- Spec（Spec-Reviewer 新实例）：**CLEAN**，Q35–Q43 逐条无 gap、无范围蔓延、无错误实现；边界（05/06/07/08 面、契约 2.1、候选纪律）确认未越。

**结论：双轴链于 Round 1 全 CLEAN。** 中间构建无身份；本票按 02 纪律不赋候选身份。

## Deferrable smells（命名、不阻塞）

1. `BueNativeManagementPanel.AddBueSettingRow` 与 `AddPluginConfigRow` 的 `ControlKind` switch 同构（Duplicated Code / Repeated Switches）——两值类型（SettingValue/PluginConfigValue）不同暂未合并；面板原生面重构时提取。
2. `GetDescriptors` 扩缝落到 3 实现 + 3 假件（Shotgun Surgery）——接口缝税，与 01 `ApplyBatch` 同型。
3. `CatalogRoutingBueSettingsEditor.GetDescriptors` 与 `Resolve` 各自扫一遍 catalog entries（Duplicated Code）——保持 Resolve 不动（01 消费方），扫描是既有模式。
4. `PanelSettingRowView` 与 `PanelConfigRowView` 共享 名/描述/形状/脏 字段（Data Clumps）——两权威源的 Kind 词表不同，留待 08 落地后视需要合并。

## Seam gaps（红测场景的纯宿主可测边界）

- 无「纯宿主无法构造」的缝缺口：投影、形状规则、循环数学、截断、草稿命令全部经 `ManagementPanelModel` 行投影缝断言（ClientUi.Tests）。原生 Glazier 的行视觉排布（描述行占位跳行、按钮文本）属 UI 面，Testing Decisions 不在单元层断言控件树；行结构正确性由投影缝 + 源码退役扫描（`SettingId = 值` 残留=0）双重锁定，画面面归 DEV-V4-09。
- 外部 Cycle 的采集（`Unturned.Cycle`/`AcceptableValueList`/`ConfigDescription`）与失败三类文案：DEV-V4-08 范围，本票只开 `Description`/`AllowedChoices` 字段缝与控件命令。

## 变更文件

见 `changed-files-stat.txt`（9 改 + `DevV4PanelControlsTests.cs` 新增）；增量 diff `dev-v4-02-tracked.diff`（826 行；CONTEXT.md 与 attribution 的既有未提交改动**不属本票**、未入 diff 未提交）。构建/测试与静态门证据：`green-fullsuite.txt`、`static-gates.txt`、`red-run.txt`。
