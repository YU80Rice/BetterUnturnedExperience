# V4-T3 描述行、循环切换与配置控件

- **Ticket**: V4-T3
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-11 Q35–Q43 定音）
- **Blocked By**: V4-T1, V4-R1
- **Map**: [map.md](../map.md)

## Question

开图已收：描述、配置行描述+合适控件、循环切换（UPM `Unturned.Cycle` / 契约 `SettingKind.Choice`）。物品/配方/生物选择器、分类导航、路径信息不进本阶段。本票裁控件语义：

1. **功能/插件描述从哪来**：BUE 功能今天没有 FeatureDescription 字段。是加契约成员（如 `IFeatureRegistration` 描述键）、只用现有 `SettingDescriptor.DescriptionKey`、还是面板硬编码官方文案 + 外部插件用 BepInEx 元数据？空描述时隐藏行还是显示占位？
2. **配置行长什么样**：DisplayName（不是 SettingId）、描述行、控件、当前值。描述截断长度是否对齐 UPM 的 120？`DescriptionKey` 今天被填成 SettingId——本阶段官方条目是否必须改成真人能读的中文？
3. **循环切换**：左键下一档、右键上一档是否照 UPM？`SettingKind.Choice` + `AllowedValues` 与外部 `Unturned.Cycle` / `AcceptableValueList` 是否同一控件？没有 AllowedValues 的 Choice 如何降级（只读 / 文本框 / 隐藏）？
4. **其它 Kind**：Toggle / Integer / Float / Text 本阶段是否只补描述、控件形状维持现状？KeyBinding 现状如何处理（R1 输入）？
5. **外部插件标签**：本阶段识别哪些 UPM 标签（至少 Cycle）？未识别标签是否静默当字符串，还是标「不支持」只读（现状 `HasDiscreteConstraint` 已有只读路径）？
6. **契约**：`SettingDescriptor` 已有 DescriptionKey / AllowedValues / Choice——本票是「填数据 + 面板去读」，还是还要加字段？

事实输入：V4-R1。官方先行消费预期：LIT 模式/方向成为第一对真实 Choice（与 T5 衔接，本票只定控件，不定 LIT 设哪些键）。

## Answer

2026-09-11 用户 Q35–Q43 全部定音。本票交付=配置行与 Cycle 的 ClientUi 语义；契约 2.1 只消费不扩。T5 定 LIT 键、T6 定官方字、T7 定外部同等升级细节，本票不填文案、不拆 LIT 设置。

### Q35 — 功能级长描述

不进公开契约，不改 `IFeatureRegistration`。官方（含 NoOp FeatureId）用面板 chrome 对照表一句话，T6 填字。无对照表的生态条目**不画**功能级描述、不占位。配置行描述走已有 `DescriptionKey`。生态作者 API 若以后要功能级描述，另开可选 facet（Minor 2.2），本票不偷渡。

### Q36 — Key 的本阶段语义

`DisplayNameKey` / `DescriptionKey` **当字面展示文本**，不是资源键。官方必须填人读中文，不得继续填 SettingId。无 i18n 表。真正国际化留后续图。

### Q37 — 配置行结构

固定：**显示名 → 描述 → 控件**。显示名=DisplayNameKey 字面量；描述=DescriptionKey 字面量，空则不画、不占位；截断 **120**（对齐 UPM）；当前值在控件上，不再画 `SettingId = 值`。

### Q38 — Cycle

同一控件覆盖：① `SettingKind.Choice` 且 `AllowedValues` 非空；② 外部 `Unturned.Cycle` / `AcceptableValueList`。左键下一档、右键上一档，改草稿。档位字面量展示。无非空档位的 Choice → **只读**，不降级文本框。

### Q39 — 其它 Kind

Toggle / Integer / Float / Text 维持现有形状，只补显示名和描述。KeyBinding 本阶段无专用按键捕获；若出现则文本框或只读，不录制快捷键、不扩契约。

### Q40 — 外部标签

本阶段只把 `Unturned.Cycle` 与 `AcceptableValueList` 映射为 Cycle，**不再因离散约束整行只读**。ItemList / BlueprintList / CreatureList / Category 不识别为选择器。未识别标签静默忽略；底层仍是 bool/数字/字符串则可编进草稿；其它类型继续 Unsupported 只读。不画「标签不支持」除非类型本就不支持。

### Q41 — 到头循环

最后档左键回第一档，第一档右键回最后档。单档可编、左右键不变；最终值=权威值则不脏。

### Q42 — 外部配置行

同一套显示名→描述→控件。显示名用现有 `DisplayName`（多为 Key，本阶段不强制中文化）；描述用 `ConfigDescription.Description`，空不画，截断 120。模型必须采集这两项。`Section > Key` 可作身份上下文，不替代显示名、不画 `Key = value`。不把 ConfigEntry 变成 Settings Facet。

### Q43 — 只读行

`CanEdit=false`、Unsupported、无档位 Choice、客户端 ServerAuthority：画显示名 + 非空描述 + 当前值只读文本。不画灰掉的开关/Cycle/假输入框。不进草稿、不脏、不标未保存、不参与保存。

### 契约结论（票面第 6 问）

「填数据 + 面板去读」。`SettingEntryView` 必须能把 DisplayNameKey / DescriptionKey / AllowedValues / Kind 投影到面板（实现细节，不加 2.1 公开成员）。零契约版本变化。

## Impact

- `CONTEXT.md`：新增「循环切换」。
- SDK：本票零成员增减；官方须改填人读 DisplayNameKey/DescriptionKey，属数据而非契约。
- ADR-0002 可编辑类型（bool/数字/字符串）沿用；Cycle 是这三类之上的离散投影，不是新类型。

## Comments

- 2026-09-11：用户 Q35–Q43 按推荐定音后结票。事实输入 V4-R1（视图丢掉键、官方填 SettingId、生产零条 Choice、外部离散约束被标只读）。
