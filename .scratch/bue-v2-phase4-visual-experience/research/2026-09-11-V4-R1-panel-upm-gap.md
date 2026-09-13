# V4-R1 管理面板与 UPM 控件差集盘点

- 日期：2026-09-11
- 类型：research（只读；对照一手源码，不改生产码）
- 问题票：`.scratch/bue-v2-phase4-visual-experience/issues/09-r1-panel-upm-gap.md`
- 给：V4-T2 / V4-T3 / V4-T7

## 来源与边界

| 对象 | 路径 | 采用版本 |
|------|------|----------|
| BUE 面板模型 | `src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs` | 当前工作树 |
| BUE 实际画出的控件 | `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs` | 当前工作树 |
| 契约 | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` | 当前工作树 |
| 外部插件适配 | `src/BetterUnturnedExperience.Plugin/LoadedPluginCatalogAdapter.cs` | 当前工作树 |
| UPM 本体快照 | `docs/third-party/UnturnedPluginManager-snapshot/PluginManagerMod.cs` | **v26.8.11.3**（文件头 L4；`[BepInPlugin(..., "26.8.11.3")]` L19）。ADR-0002 记采用提交 `9b75730` |
| UPM 技能包 | `docs/third-party/unturned-plugin-dev/SKILL.md` | 文档写到 **v26.8.13.3**（CreatureList 标签） |
| 类型边界 | `docs/adr/0002-bue-owned-single-dll-management-panel.md` | accepted |
| 本阶段开图 | `.scratch/bue-v2-phase4-visual-experience/map.md` Notes 第 6、7、10 条 | 2026-09-11 |

**硬边界（本报告遵守）：** 技能包比快照新的控件（`Unturned.CreatureList`、`Unturned.Category`）只记为「技能包文档有 / 快照未实现」，**不**写成「UPM 快照已有」。快照 `PluginManagerMod.cs` 常量只有 `Unturned.ItemList` / `Unturned.BlueprintList` / `Unturned.Cycle`（L1127–L1129）。

状态标签（逐项必填，互不合并）：

```text
UPM 已有 / BUE 模型已有但未画 / BUE 契约已有但未填 / BUE 未有
本阶段开图已收（描述、配置行描述+控件、Cycle、功能级启停） / 开图已划后续
```

「UPM 已有」= 快照 v26.8.11.3 源码画出或识别。技能包新标签单独一行。

---

## 1. 差集总表

| # | 能力 | UPM 快照已有 | BUE 模型已有但未画 | BUE 契约已有但未填 | BUE 未有 | 开图 |
|---|------|-------------|-------------------|-------------------|----------|------|
| 1 | 条目显示名（功能中文名 / 插件 Name） | 是（插件 Name） | 否（已画） | 无功能级 DisplayName 字段；官方用 chrome 映射 | 契约无 FeatureDisplayName | 本阶段不改名体系 |
| 2 | 功能/插件**长描述** | 否（无独立描述字段；插件只有 Name/GUID） | 否 | `IFeatureRegistration` 无描述成员 | **功能级描述字段未有** | **本阶段已收「描述」** |
| 3 | 配置行 DisplayName（人读名，不是 Id） | 否（画 `Section > Key`） | 模型 `PluginConfigEntryView.DisplayName` 现填成 Key；`SettingEntryView` 无 DisplayName | `SettingDescriptor.DisplayNameKey` 官方填成 SettingId | 面板视图层丢了 DisplayNameKey | **本阶段已收「配置行」** |
| 4 | 配置行**描述**（UPM 截断 120） | 是（`ConfigDescription.Description`，`Truncate(..., 120)`） | 模型无 Description 字段，故谈不上「已有未画」 | `SettingDescriptor.DescriptionKey` 官方填成 SettingId；`SettingEntryView` 不携带 | 外部 ConfigEntry 描述未采集 | **本阶段已收「配置行描述」** |
| 5 | Bool / Toggle 开关 | 是（`CreateToggle`） | 否（已画） | 官方已填 Toggle | — | 控件形状维持；本阶段补描述 |
| 6 | 数字/字符串文本框 | 是（非 bool 一律 `CreateStringField`） | 否（已画；Integer/Float/Text/Choice/KeyBinding 都进文本框） | 生产官方**无** Integer/Float/Text 描述符 | — | 本阶段补描述，形状维持（T3 待裁） |
| 7 | **循环切换 Cycle**（左键下一档 / 右键上一档） | **是**（`Unturned.Cycle` + `AcceptableValueList<string>`） | 契约 Choice 未进模型视图；面板把 Choice 当文本框 | `SettingKind.Choice` + `AllowedValues` **生产官方零条** | 外部路径把 `AcceptableValueList` 标 Unsupported，**不认 Cycle 标签** | **本阶段已收** |
| 8 | 功能级启停按钮 | 否（UPM 只改 ConfigEntry，不启停插件进程） | **`TryToggleFeature` 已接线，原生面板零调用** | `FeatureState` / `SetFeatureEnabled` 已有 | 面板无启停控件 | **本阶段已收** |
| 9 | 未保存草稿 / 统一保存 | 否（改一项立刻 `Save()`） | 否 | 否 | **BUE 同样改一项立刻写权威源** | 开图已收草稿语义（T2 裁）；现状未实现 |
| 10 | `RequiresRestart` 提示 | 否（快照不读该标签） | 模型有，已画「（需要重启）」后缀 | — | 保存后黄条时机未做（现状改时就写） | 开图已收外部重启只提示（T7） |
| 11 | 物品选择器 `Unturned.ItemList` | **是** | 否 | 否 | **未有** | **后续** |
| 12 | 配方选择器 `Unturned.BlueprintList` | **是** | 否 | 否 | **未有** | **后续** |
| 13 | 生物选择器 `Unturned.CreatureList` | **否（快照无此常量）**；技能包 v26.8.13.3 才文档化 | 否 | 否 | **未有** | **后续** |
| 14 | 分类导航 `Unturned.Category` | **否（快照无此常量）**；技能包 v26.8.12.1 才文档化 | 否 | 否 | **未有** | **后续** |
| 15 | 程序集路径 / 配置文件路径 | **是**（`info.Location`、`ConfigFilePath`） | 否 | 否 | `LoadedPluginDescriptor` 无 Location/Path | **后续** |
| 16 | 收藏 / A-Z·Z-A | 否 | 否（已画） | — | — | 开图：收藏仍即时 |
| 17 | KeyBinding 专用控件 | 否 | 面板把 `SettingKind.KeyBinding` 当字符串文本框 | 契约有 Kind，生产官方未填 | 无按键捕获控件 | 本阶段控件形状待 T3；官方零条 |
| 18 | 离散约束（非 Cycle 的 `AcceptableValueList`） | Cycle 才专用；其它 list 当普通文本 | `HasDiscreteConstraint` → Unsupported → 只读无控件 | — | 不解析档位 | T7：本阶段是否只加 Cycle |
| 19 | ADR-0002 可编辑类型（bool / 数字 / 字符串） | 实际：bool 开关，其余一律文本（含非这三类） | 模型只允许这三类可编辑；其它 Unsupported 只读 | — | — | 维持边界 |
| 20 | 检测到 UPM 本体 | 无（UPM 不管别人） | 检测已有，底栏提示已画；**仍列出并可编其 ConfigEntry** | — | 不热卸载 | T7 再裁是否继续可编 |

---

## 2. BUE 面板模型（`ManagementPanel.cs`）

### 2.1 条目字段

`ManagementEntryView`（L125–151）字段：`Kind`、`StableId`、`DisplayName`、`Version`、`FeatureState`、`Presentation`、`BueSettings`、`PluginConfig`、`IsFavorite`。

- **无**功能/插件长描述。
- BUE 功能条目把 `FeaturePresentationView` 整包带上（L343–345），但详情页只画 `Presentation.State`（原生 L968–969）。
- 外部插件条目 `FeatureState` / `Presentation` 为零值（L349–351）。

`BueFeatureManagementEntry`（L23–41）同样无描述；显示名来自组合根 chrome，不是契约。

### 2.2 设置行

面板消费的是 `SettingEntryView`（契约 L310），不是 `SettingDescriptor`。视图字段：`SettingId`、`Authority`、`ClientPreference`、`HasPolicy`、`Policy`、`EffectiveValue`、`IsVisible`、`CanEdit`。

**不携带** `DisplayNameKey` / `DescriptionKey` / `AllowedValues` / `Kind`（Kind 只活在 `EffectiveValue.Kind`）。因此即便官方改填真人中文键，当前快照管道也会在 `SettingsRuntime.BuildEntry`（`src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs` L423–430）丢掉它们。

`VisibleSettings`（ManagementPanel L469–473）只过滤 `IsVisible`。

外部 `PluginConfigEntryView`（L81–105）：`Key`、`DisplayName`、`Kind`、`Value`、`RequiresRestart`、`CanEdit`、`Minimum`、`Maximum`、`MaximumLength`。**无 Description、无 Tags、无 Cycle 档位。** `CanEdit` 在 Unsupported 上强制 false（L101）。

### 2.3 `TryToggleFeature`

L400–419：命令适配器，转发 `FeatureToggleHandler`；未接线返回 false。注释写明「原生按钮 UI 是具名延期（随 09 实机面）」。

组合根已接线：`ClientUiCompositionRoot.cs` L66 → `BueFeatureStartRuntime.SetFeatureEnabled`（`BueFeatureStartRuntime.cs` L191–211：停用 = `UserDisabled`，启用 = 新生命周期代）。

**模型已有，原生面板未画、未调用。**

### 2.4 即时写入（给 T2）

- BUE 设置：`TryEditBueSetting`（L384–397）立刻 `bueSettingsEditor.Apply`。
- 外部配置：`TryEditPluginConfig`（L421–438）立刻 `pluginConfigEditor.TrySet`。
- 收藏：`ToggleFavorite`（L365–373）立刻存偏好。

无草稿、无「保存配置」命令。与 UPM 快照同为改一项写一项。

---

## 3. 实际画出的控件（`BueNativeManagementPanel.cs`）

### 3.1 壳

刷新 / A-Z / Z-A / 关闭（L804–840）；左列表右详情（L842–861）。列表行：`★/☆ + DisplayName`，tooltip = `StableId + 版本`（L889–890）。**不画 GUID 旁长描述、不画程序集、不画配置路径。**

详情公共头（L944–959）：显示名、身份、版本、收藏按钮。无描述行。

### 3.2 BUE 功能设置控件

`AddBueSettingControl`（L991–1023）：

- 标签：`setting.SettingId + " = " + FormatSetting(...)` —— **用人读名？否，用 SettingId。**
- **无描述行。**
- `CanEdit` 为 false 则只画标签。
- `SettingKind.Toggle` → `CreateToggle`，`OnValueChanged` 立刻 `TryEditBueSetting`。
- 其它 Kind → `AddTextEditor`（字符串框）。`TryConvertSetting`（L1104–1126）接受 Integer / Float / Text / **Choice** / **KeyBinding**，一律当字符串提交。

**没有 Cycle 按钮，没有 `OnRightClicked`。** Choice 若将来填了 AllowedValues，现状仍是文本框。

网络条目额外画接管状态 + 「让我改回独立 LMN」（L1031–1050），不是通用描述。

功能状态只是只读标签（L968–969），**没有启停按钮**，全文无 `TryToggleFeature` 调用。

### 3.3 外部插件配置控件

`AddPluginConfigControl`（L1053–1083）：

- 标签：`entry.DisplayName + " = " + 值 + 可选「（需要重启）」`。
- **无 ConfigDescription 描述行**（模型也没这个字段）。
- Boolean → Toggle；其它可编辑 → 文本框；`!CanEdit` 只画标签。

即时 `TryEditPluginConfig`。无 Cycle / ItemList / 分类。

---

## 4. 契约：`SettingDescriptor` / `SettingKind` / `IFeatureSettingsRegistration`

### 4.1 已有字段（契约已有）

`SettingKind`（ContractTypes.cs L283）：`Toggle, Integer, Float, Text, KeyBinding, Choice`。

`SettingDescriptor`（L303–307）含 `DisplayNameKey`、`DescriptionKey`、`AllowedValues`、Min/Max/Step、`MaximumUtf8Bytes`。Choice 在运行时强制非空且无重复 AllowedValues（SettingsRuntime.cs L496）。

`IFeatureSettingsRegistration`（L75–86）可选 facet：`SettingDescriptors` + `OnSettingsApplied`。**未**加入 `IFeatureRegistration`（L54–60 只有 Definition / Contract / Factory / ClientUi）。**无 FeatureDescription / DisplayName 成员。**

`SettingEntryView`（L310）是面板实际拿到的投影，**不含** DisplayNameKey / DescriptionKey / AllowedValues。

### 4.2 官方如何填（契约已有但未填人读内容）

生产官方描述符**全部是单条 ClientLocal Toggle**，三键都填 SettingId，`AllowedValues` 为 null 或空数组：

| 功能 | 文件 | SettingId | DisplayNameKey | DescriptionKey | AllowedValues |
|------|------|-----------|----------------|----------------|---------------|
| LIT 背包整理 | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` L442–448 | `inventorytidy.enabled` | = SettingId | = SettingId | null |
| LIR 换弹 | `src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs` L416–422 | `inplacereload.enabled` | = SettingId | = SettingId | null |
| LHT 尸潮 | `src/BetterUnturnedExperience.Lht/HordeTrackerModule.cs` L378–384 | `hordetracker.enabled` | = SettingId | = SettingId | null |
| Network | `src/BetterUnturnedExperience.Plugin/NetworkModuleAdapter.cs` L851–855 | `network.enabled` | = SettingId | = SettingId | `new SettingValue[0]` |
| V1Compat | 同上 CreateV1CompatDescriptors L342–344 | `v1compat.enabled` | = SettingId | = SettingId | 空数组 |
| NoOp 探针 | `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs` L240–244 | `noop.probe-toggle` | = SettingId | = SettingId | null |

条目中文显示名**不在描述符里**，在组合根 chrome：

- BII：「更好的物品交互」（`ClientUiCompositionRoot.cs` L104）
- Network：「BUE 网络模块」；V1：「BUE V1 兼容层」（L160–161）
- LIT / LIR / LHT：`LitRuntime.DisplayName` = 「背包整理」（`LitRuntime.cs` L19）；LIR 「更好的换弹体验」；LHT 「更好的尸潮播报」（`OfficialDisplayName` L157–165）
- 其它：退回 FeatureId 字符串（L165）

### 4.3 BII 特例

`BetterItemInteractionFeatureRegistration`（`OfficialFeatureRegistration.cs` L19）**只**实现 `IFeatureRegistration`，**没有** `IFeatureSettingsRegistration`。设置来自 `BetterItemInteractionSettingsState.GetSnapshot`（`BetterItemInteractionLifecycle.cs` L64–73）：两条 Toggle `Enabled` / `AutoRotate`，直接构造 `SettingEntryView`，绕过 `SettingDescriptor`，故无 DisplayNameKey / DescriptionKey 可填。

生产官方**零条** `SettingKind.Choice` / `KeyBinding` / `Integer` / `Float` / `Text`。Choice 只出现在设置运行时测试（`tests/BetterUnturnedExperience.Settings.Tests/Program.cs`）。

开图预期「LIT 模式/方向成为第一对真实 Choice」——**现状未填**，属 T5 范围；本报告只记空位。

---

## 5. 外部插件路径

文件：`src/BetterUnturnedExperience.Plugin/LoadedPluginCatalogAdapter.cs`。

### 5.1 采集范围

`CaptureLoadedPlugins`（L20–60）扫 `Chainloader.PluginInfos`：

- 条目：GUID、`Metadata.Name`、Version、ConfigEntry 列表。
- **不采集** `PluginInfo.Location`、`ConfigFile.ConfigFilePath`（对比 UPM 快照 L857–863）。
- Config 行 DisplayName = `definition.Key`（L45），**不是** `Description.Description`，也不是 Section。
- **不采集 Tags**（除下面 RequiresRestart / 离散约束探测）。

### 5.2 可编辑类型（对齐 ADR-0002 第 9 条）

`CanEdit`（L94–102）+ `ToValue`（L104–116）：bool / 整数族 / float·double·decimal / string。其它 → Unsupported → 只读。

`HasDiscreteConstraint`（L164–167）：`AcceptableValueList*` → **整行标 Unsupported**。因此带 `AcceptableValueList<string>` 的 Cycle 配置在 BUE **不可编辑**（UPM 快照会画循环按钮）。这是 T3/T7 的关键差。

`RequiresRestart`（L145–147）：Tags 里大小写不敏感等于 `"RequiresRestart"`。面板在值旁加「（需要重启）」（原生 L1055）。**改值仍立刻 `config.Save()`**（L82），没有「保存后再提示」。

数值 Min/Max 从 AcceptableValues 的 `MinValue`/`MaxValue` 反射读取（L150–161）；字符串 `MaximumLength=4096`。模型 `WithinBounds`（ManagementPanel L520–526）会拒绝越界。

### 5.3 UPM 本体检测

`BueManagementPanelRuntime.Refresh` L38：GUID `com.trae.pluginmanager` → `SetExternalManagerDetected`。底栏文案「检测到外部插件管理器；BUE 不会重复修改其状态。」（ManagementPanel L301；原生 L988）。

检测**不**把该插件的 ConfigEntry 设为只读；它仍在目录里，可按 ADR 类型编辑。ADR-0002 第 2 条：不主动卸载或修改其**状态**（进程/加载），不是「不改它的 cfg」。

---

## 6. UPM 快照 v26.8.11.3（提交 9b75730）——已实现控件

源：`docs/third-party/UnturnedPluginManager-snapshot/PluginManagerMod.cs`。文件头 L4「版本 v26.8.11.3」；CHANGELOG 同目录确认该版是 UI 重建修复，控件集自 v1.4.7 累积。

### 6.1 详情元数据（BUE 未有 / 开图后续）

`ShowPlugin` L855–863：名称、GUID、版本、**程序集 Location**、**配置文件 ConfigFilePath**。

### 6.2 配置行（本阶段要对齐的描述+控件）

`AddConfigRow` L920–1070：

1. 先分流 ItemList / BlueprintList / Cycle（L925–938）。
2. 键文案：`Section > Key`（L941）。
3. 描述：`entry.Description.Description` + `AcceptableValues.ToDescriptionString()`，**`Truncate(description, 120)`**（L943–962, L1104–1114）。
4. 描述非空则在键下方再画一行（L977–988），行高 58，否则 40（L1070）。
5. bool → Toggle，立刻写 `BoxedValue` + `Save()`（L991–1012）。
6. 其余 → StringField，回车 `SetSerializedValue` + Save，ESC 还原（L1014–1056）。
7. 右侧类型名（L1059–1068）。

### 6.3 Cycle（本阶段已收；快照已实现）

常量 `CycleTag = "Unturned.Cycle"`（L1129）。`HasCycleTag`：Tags 字符串 **StartsWith** `"Unturned.Cycle"`（L1167–1181），故 `"Unturned.Cycle:OFF|1|2|3|4"` 命中。

档位：冒号后 `|` 分割且 ≥2；否则 `AcceptableValueList<string>` 且 ≥2（L1184–1229）。不足则 `options==null`，按钮仍画，但 `CycleStep` 直接 return（L1314–1316）。

交互：左键 `OnClicked` direction+1，右键 `OnRightClicked` direction−1，环形，立刻 `SetSerializedValue` + Save（L1287–1294, L1311–1344）。Tooltip「左键：下一档；右键：上一档」（L1282）。

### 6.4 ItemList / BlueprintList（快照已有；开图后续）

L1127–L1128；`AddListRow` L1364 起：描述同样 Truncate 120，「+」打开选择器，已选项带删。BUE 完全未实现。

### 6.5 快照**没有**的标签

在 `PluginManagerMod.cs` 全文检索：无 `Unturned.CreatureList`、无 `Unturned.Category`、无分类导航按钮。不要把技能包 v26.8.12.1 / v26.8.13.3 写进「快照已实现」。

快照也**没有**插件进程级启停、没有收藏、没有未保存草稿。

---

## 7. 技能包 `SKILL.md`（写到 v26.8.13.3）——仅文档，非本快照

`docs/third-party/unturned-plugin-dev/SKILL.md` §2.2：

| 标签 | 技能包声称版本 | 快照 9b75730 / v26.8.11.3 |
|------|----------------|---------------------------|
| `Unturned.ItemList` | 有 | **已实现** |
| `Unturned.BlueprintList` | 有；v26.8.13.2 弹药配方回退 | **已实现**（无 13.2 回退） |
| `Unturned.Cycle` | 有 | **已实现** |
| `Unturned.Category:<名>` | **v26.8.12.1 新增**（SKILL.md L161） | **未实现** |
| `Unturned.CreatureList` | **v26.8.13.3 新增**（SKILL.md L124） | **未实现** |

开图后续三项选择器 + 分类导航时，实施源应以届时 UPM 版本为准，而不是把技能包段落当成当前快照。

---

## 8. ADR-0002 允许编辑的类型边界

`docs/adr/0002-bue-owned-single-dll-management-panel.md`：

- 第 3 条：分层 = BUE 功能（FeatureId / Settings Facet / FeatureState / PresentationState）vs 普通插件（GUID / 名称 / 版本 / 程序集 / 公开 ConfigEntry）。**程序集信息契约上要有，面板模型未采集。**
- 第 5 条：BUE 设置唯一权威 = SettingsRuntime；普通配置只走公开 ConfigEntry/ConfigFile。现状面板即时写这两条权威源（无草稿）。
- 第 9 条：允许编辑 **bool、数字、字符串**；不支持类型只读；需重启的明确提示；**不执行热卸载或任意代码**。

与代码一致：`LoadedPluginCatalogAdapter.CanEdit` 即这三类；Unsupported 只读。ADR **未**授权 ItemList 资产选择器，也未授权编辑 `AcceptableValueList` 离散档——后者被 BUE 标只读，而 UPM 用 Cycle 标签打开。本阶段若收 Cycle，是对 ADR 第 9 条「字符串」的控件升级（仍写 string），不是新类型。

---

## 9. 给 T2 / T3 / T7 的浓缩事实

**T2 草稿：** 现状 BUE 与 UPM 都是控件变更即 `Save`/`Apply`。模型无脏标记。收藏已经即时（开图保持）。`RequiresRestart` 只是标签后缀，写盘不等「保存按钮」。

**T3 控件：**

1. 功能长描述：契约与模型都没有字段；官方只有 chrome 显示名。配置描述：契约有 `DescriptionKey` 但官方填 SettingId，且 `SettingEntryView` 丢弃该键；外部连 Description 都没采。
2. 配置行现状 = `SettingId/Key = 值` + Toggle 或文本框。UPM 对照 = `Section > Key` + 截断 120 的描述 + 控件。DisplayNameKey 未当人读名用。
3. Cycle：快照已有完整左右键按钮。BUE 契约有 Choice+AllowedValues 但生产未填；面板把 Choice 当文本；外部 AcceptableValueList 被 HasDiscreteConstraint 锁死。
4. KeyBinding：契约有，官方未填，面板当文本。
5. 本阶段开图已收：描述、配置行描述+合适控件、Cycle、功能级启停。ItemList/BlueprintList/CreatureList/Category/路径 = 后续。CreatureList/Category **不是** 9b75730 快照能力。

**T7 外部同等升级：** 同等指描述/Cycle/草稿，不是把 UPM 选择器搬过来。外部今日缺口：无描述采集、Cycle 被当成离散约束只读、无草稿、检测 UPM 后仍可编其 cfg。ADR 类型边界保持 bool/数字/字符串。

---

## 10. 本阶段已收 vs 后续（对照开图 Notes 第 6 条）

**已收（要进 T2/T3/T7 规格，不是本报告实现）：**

- 描述（功能级从哪来尚未裁，R1 只证明今天没字段）
- 配置行描述 + 合适控件（Toggle 已有；Cycle 要对齐 UPM 快照而非技能包新件）
- 循环切换
- 功能级启停（模型+宿主 seam 已在，差画按钮；开图：进草稿，保存才 `SetFeatureEnabled`）

**已划后续：**

- `Unturned.ItemList` / `BlueprintList`（快照已有，BUE 未有）
- `Unturned.CreatureList` / `Unturned.Category`（**仅技能包**，快照无）
- 程序集路径、配置文件路径（快照已有，BUE 未有）

---

## 11. 证据索引（file:line）

| 主张 | 证据 |
|------|------|
| 条目无描述字段 | `ManagementPanel.cs` L125–151 |
| TryToggleFeature 有、原生未画 | `ManagementPanel.cs` L410–419；`BueNativeManagementPanel.cs` 无该符号；`ClientUiCompositionRoot.cs` L66 |
| 设置行画 SettingId | `BueNativeManagementPanel.cs` L991–993 |
| Choice/KeyBinding 当文本 | 同文件 L1104–1126 |
| DisplayNameKey=SettingId | LIT L444–448；LIR L418–422；LHT L380–384；Network L853–855 |
| BII 无 settings facet | `OfficialFeatureRegistration.cs` L19；`BetterItemInteractionLifecycle.cs` L64–73 |
| SettingEntryView 丢描述键 | `ContractTypes.cs` L310；`SettingsRuntime.cs` L423–430 |
| 离散 list → 只读 | `LoadedPluginCatalogAdapter.cs` L40, L164–167 |
| 外部无 Description/Path | 同文件 L45–51 vs UPM L857–863 |
| UPM 描述 Truncate 120 | `PluginManagerMod.cs` L962, L1104–1114 |
| UPM Cycle 左右键 | 同文件 L1167–1344 |
| 快照仅三标签 | 同文件 L1127–1129 |
| CreatureList 是技能包 13.3 | `SKILL.md` L124 |
| Category 是技能包 12.1 | `SKILL.md` L161 |
| ADR 类型边界 | `docs/adr/0002-bue-owned-single-dll-management-panel.md` 第 9 条 |
| 开图已收/后续 | `map.md` Notes 第 6 条 |
| 快照版本 | `PluginManagerMod.cs` L4, L19；ADR-0002 Context `9b75730` |
