# V4-T5 背包整理标题栏与全局模式/方向设置

- **Ticket**: V4-T5
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-11 Q54–Q59 定音）
- **Blocked By**: V4-T3, V4-R2
- **Map**: [map.md](../map.md)

## Question

开图已定：标题栏本阶段只留「整理」；模式/方向改为全局一份，进 LIT 插件设置，用循环切换改；锁定按钮本阶段不画；STORAGE 仍不注入；排序规则不进本图。本票裁 LIT 表面：

1. **「整理」按钮**：尺寸、位置（仍在 `headers[0..4]` 右侧预留带？）、文案、tooltip。Ctrl+左键全身整理还保不保留？功能停用后按钮消失还是灰掉？
2. **全局模式/方向**：SettingId、DisplayName、Description、Choice 档位文案（同类/空间/大件；升序/降序）。默认值是否仍为同类+降序（大件优先）？从「每页内存态」迁到全局设置后，旧每页差异直接丢弃？
3. **与功能级启停**：总开关退役后，未启用的 LIT 是否整页不画「整理」按钮？
4. **锁定页**：只登记为后续表面（位置在「整理」旁、跳过该页）。本票是否还要预留空白，还是本阶段标题栏只放一颗按钮、后续再挤？
5. **官方先行消费**：LIT 必须成为 T3 Choice 控件的真实消费者——本票是否把「模式、方向两条 Choice 描述写清」定为验收门禁？
6. **契约**：整理按钮仍是功能私有 Glazier patch，不经 ClientUi satellite——维持现状还是顺便登记 satellite？开图倾向维持现状（BII 才有 satellite）。

事实输入：V4-R2、V4-T3。不讨论放置算法、不讨论 StrategyId 选择器。

## Answer

2026-09-11 用户 Q54–Q59 定音。本票交付=LIT 标题栏只留「整理」+ 两条全局 `ClientPreference` Choice。三个 module 分缝：patch 只画/拆按钮；设置拥有模式与方向权威；生命周期决定按钮是否存在。patch 不读面板草稿，不自持 `isEnabled`。

产品语言用 **ClientPreference**（与 T2 同一套）；若契约枚举字面是 `ClientLocal`，实施时仍按 ClientPreference 语义接线，不另造 scope 名。

### Q54 — 整理按钮

仍注入 `headers[0..4]`（Hands / Backpack / Vest / Shirt / Pants），不注入 STORAGE。尺寸 60×60，右侧预留带，`PositionOffset_X = -130`（右缘约 -70，避让耐久「100%」）。文案「整理」。Tooltip（玩家用语）：**左键：整理当前栏；Ctrl+左键：按全局模式和方向整理全身（不含仓储栏）**。Ctrl+左键保留。不进独立窗口，不进 satellite 契约。坐标与 tooltip 是 ClientUi 实现约束，不是 SDK。点击处理前必须再确认模块当前可用，不能只因「按钮曾被画出」。

### Q55 — 九态与按钮存在性

按钮可用性由生命周期状态事实决定，不由 patch 私有布尔。

| 当前状态 | 新开页面 | 已打开页面 |
|---|---|---|
| Running | 注入 | 保留且可用 |
| Disabled / Stopped | 不注入 | 停用成功后**移除** |
| Isolated | 不注入 | 隔离后**移除** |
| Incompatible | 不注入 | 移除 |
| Starting | 不新增 | 已存在则暂留，点击须安全回退 |
| Isolating / Stopping | 不新增 | 暂留，点击走原生回退，**不报假成功** |

「拆掉」= 移除按钮对象、解绑回调、清 patch 自持引用；不破坏原生标题栏；移除失败不得把停用伪装成成功。过渡态暂留不是允许按钮继续服务。

### Q56 — 两条 Choice

| SettingId | Kind | 作用域 | 显示名 | AllowedValues 字面量 | 默认 |
|---|---|---|---|---|---|
| `inventorytidy.mode` | Choice | ClientPreference | 整理模式 | `同类` / `空间` / `大件` | `同类`（SameType） |
| `inventorytidy.direction` | Choice | ClientPreference | 整理方向 | `降序` / `升序` | `降序` |

映射冻结：`降序` = 现有大件优先；`升序` = 现有相反方向。不进 ServerAuthority。旧每页内存字典直接丢弃、不迁移。完整描述正文由 T6 写。本阶段中文档位同时是机器值与显示值；后续 i18n 须另开 machine/display 分离，不得把这些中文值当成跨语言稳定协议。

官方先行消费门禁：LIT 设置页必须真实出现这两条 Cycle（T1 Q16 ②）。描述句子本身归 T6，本票把门禁钉在「两条 Choice 键+档位+默认+作用域」。

### Q57 — 锁定不留空

本阶段标题栏只画「整理」。不预留空白、不画隐藏控件。锁定后续再重新计算布局。

### Q58 — 不登记 satellite

LIT 标题栏继续私有 Glazier patch，`ClientUi = null`。不属于管理面板 chrome，不进公开 ClientUi 契约。U3DS 不武装该 patch（T1）。当前没有第二种 adapter，不预建 satellite。

### Q59 — 点击只读已保存快照

「整理」只读当前客户端 **ClientPreference 已保存设置快照**。不读：管理面板草稿、ServerAuthority、旧每页内存、上一次点击缓存。未保存的模式/方向不影响按钮；保存成功后**下一次点击**用新快照，不要求重画标题栏。模式与方向必须从**同一 revision** 一次读取，禁止拼出从未存在过的组合。

`inventorytidy.enabled` 按 T4 legacy alias 退役，本票设置页只留模式与方向两条（外加 T6 文案）。

## Impact

- `CONTEXT.md`：新增「整理按钮」。
- 契约：两条新 SettingDescriptor（数据，非 `IFeatureRegistration` 扩员）；零 SDK 成员增减。
- 不重开 O-LIT-1 / StrategyId / STORAGE。

## Comments

- 2026-09-11：用户对 Q54–Q59 做九态映射、ClientPreference 对齐、同一 revision 快照收紧后结票。事实输入 V4-R2。
