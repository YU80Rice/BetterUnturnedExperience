# DEV-V4-07 审计·官方文案、设置中文与 NoOp Choice

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-07-official-copy-noop.md`
- 规格：`spec.md`「官方文案与 NoOp（V4-T6 → DEV-V4-07）」节 +「共享规则（V4-T1）」锚③；裁决 `issues/06-t6-official-copy.md`（Q60–Q64）
- 日期：2026-09-12；会话：Phase-4 实施票 07（依赖 02/06 已 resolved，按冻结依赖图领取）
- 构建：MSBuild（VS 18 Insiders）Release `-t:Rebuild` → **0 警告 / 0 错误**（最终轮 `full-build-r4.log`；r2/r3 同）
- 测试：**全套 7 运行器全 PASS**（`green-fullsuite-r4.txt` 汇总；逐 runner `green-fullsuite-*-run-r4.txt`）；静态门：Core 30 文件 NoUiTokens PASS、模型三文件（ManagementPanel/PanelChromeCopy/BetterItemInteractionLifecycle）UI/native token=0、`git diff --check` exit 0
- **候选纪律（本票 07）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。契约仍 2.1（Contracts/SDK 零入 diff）。

## 交付摘要

官方与样板功能的「能读懂」三缝：chrome 对照表（功能级一句话）、设置行中文显示名+描述、NoOp 样板 Toggle+Choice。全部落面板 chrome 与既有 schema 缝，零契约成员变化。

- **chrome 对照表**（新 `PanelChromeCopy.cs`，ClientUi internal，非契约）：Q60 七句逐字（BII/LIT/LIR/LHT/Network/v1compat/NoOp），键=FeatureId；管理面板自身无条目；`TryGetDescription` 是唯一查询面（Round 2 删除了仅为测试设的 `KnownFeatureIds` 键集合暴露）。
- **模型投影**（`ManagementPanelModel.GetFeatureDescription`）：只对目录内 BUE 功能条目回答；无对照表生态条目=空串（不画不占位）、外部插件=空串、未知 stableId=空串。
- **原生详情页**（`BueNativeManagementPanel.cs`）：BUE 功能分支在设置行之前画一句话（空串跳行）；外部插件分支不画；网络接管文案逐字未动（Q63）。
- **LIT**（`InventoryTidyModule.cs`）：两条全局 Choice 描述符补 Q61 冻结描述句（整理模式/整理方向），显示名（Q56）与档位（同类/空间/大件、降序/升序）不变。
- **BII**（`BetterItemInteractionLifecycle.cs`）：`GetDescriptors` 由诚实空表改为恰一条 AutoRotate 描述符（Q62 显示名「自动旋转」+描述）；退役 Enabled 永不回 schema；快照/编辑缝零改动（AutoRotate 仍是普通可编辑设置）。
- **NoOp**（`NoOpFeaturePlugin.cs`）：probe-toggle 保留并补 Q64 文案（探针开关/默认开）；新增 `noop.probe-choice`（探针档位，甲/乙，默认甲，MaximumUtf8Bytes=16——runtime 字节门对 Choice 精确生效，0 会拒一切提交）；两者均非生命周期代理、不登记 legacy alias；探针 `RunSettingsStep` 由 `Entries[0]` 改为按 id 定位 probe-toggle（facet 变两行后链判据与顺序解耦，冻结链序未动）。
- **工程接线**：`PanelChromeCopy.cs` 显式 Include 两处（ClientUi.csproj + Plugin.csproj EmbeddedClientUi 链接嵌入）；测试文件注册两处。

## 红测先行与测试缝

红→绿链与评审驱动的测试缝收敛全程见 `red-first-evidence.md`。要点：

- 编译红（CS0103 PanelChromeCopy / CS1061 GetFeatureDescription，两工程）驱动出 chrome 投影缝；运行时红= BII 显示名回退、06 交接断言（LIT 描述为空）翻转。Plugin.Tests 首败即停口径下的组级红限制已在证据中如实注记。
- 测试断言全部经面板外显缝（`GetFeatureDescription` / `GetSettingRows`），不直读描述符/注册面/内部表（Testing Decisions「只测外显行为」）；先例=NoOpFixture 契约 probe（锚③走真实宿主路由：注册→组合根→OpenDetail→Cycle 改草稿→保存→宿主 runtime 读到乙、revision 单次推进→空操作保存文案）。
- 退役 enabled 对拍（与 04）全在行级：LIT 两行恰 mode/direction、BII 恰 AutoRotate、NoOp 恰两探针行。

## 双轴独立审查链（每轮全新实例，无 SendMessage 续用）

### Round 1
- Standards（新实例）：**CLEAN**，无硬违规、无递延项。
- Spec（新实例）：**FINDINGS** — 1 blocking：测试断言 `PanelChromeCopy.KnownFeatureIds.Count` 直读内部表键集，违反 spec Testing Decisions「只测外显行为…不断言…私有字典」。

### Round 2（修复：删除 KnownFeatureIds 内表面与断言，七句全经 GetFeatureDescription）
- Standards（新实例）：**CLEAN**。
- Spec（新实例）：**FINDINGS** — 1 blocking（新）：LIT/NoOp/enabled 组直读描述符/注册面断言文案，未经面板行投影缝（Round 1 blocking 确认解除）。

### Round 3（修复：三组直读断言删除，文案断言全部收敛到 GetSettingRows 行缝；BII 对拍改行级；锚③补默认开；06 交接断言移交 07 组）
- Standards（新实例）：**CLEAN**，2 项 deferrable（命名：测试方法名残留 Descriptors 字样；`IBueSettingsEditor.GetDescriptors` 注释与 BII 现状不一致）。
- Spec（新实例）：**CLEAN**，零 gap、零蔓延。

### Round 4（修复两条 deferrable：方法改名 `BiiRetiredEnabledStaysOutOfPanelRows`；接口注释与 BII 单描述符现状对齐）
- 终轮双轴合审（新实例）：**Standards CLEAN / Spec CLEAN**，零 blocking、零递延；硬门抽验（契约 2.1 零变化、候选纪律、外显缝、Q60–Q64 逐字、范围）全过。

**结论：四轮评审链于 Round 4 双轴 CLEAN 且零递延。** 中间构建无身份；本票按 07 纪律不赋候选身份、不授 CaseId。

## Deferrable smells

无（Round 3 命名的两条已在本票内修复并经 Round 4 复审确认）。
