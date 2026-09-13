# V4-R2 LIT 背包整理标题栏与设置现状

- **Ticket**: V4-R2
- **日期**: 2026-09-11
- **方式**: 只读对照一手源码。未改生产代码、未构建、未提交。
- **范围**: T5 事实输入。不讨论 O-LIT-1 排序/放置算法。

## 0. 一手来源

| 编号 | 来源 | 身份 |
|---|---|---|
| LIT-UI | `src/BetterUnturnedExperience.Lit/Ui/InventoryTidyUiPatch.cs` | 标题栏注入、每页内存态、Ctrl+点击 |
| LIT-MOD | `src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs` | 设置 schema、enabled 读、RefreshSwitches、Start/Stop |
| LIT-REG | `src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs` | SettingDescriptors 接线、OnSettingsApplied、ClientUi=null |
| LIT-STR | `src/BetterUnturnedExperience.Lit/TidyStrategy.cs`、`DefaultGridV1Strategy.cs` | ITidyStrategy / StrategyId |
| LIT-MODE | `src/BetterUnturnedExperience.Lit/Solver/InventorySolver.cs` | `TidyMode` 枚举 |
| LIT-RT | `src/BetterUnturnedExperience.Lit/LitRuntime.cs`、`Tidy/HotkeySnapshot.cs` | FeatureId、AllPages、可整理页 2..6 |
| CTR | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` | `SettingDescriptor` 字段 |
| PANEL | `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs` | 设置 Toggle 控件 |
| HOST | `src/BetterUnturnedExperience.Plugin/BueFeatureStartRuntime.cs`、`NetworkSettingsEditor.cs`、`ClientUiCompositionRoot.cs` | 面板编辑路由与功能启停 seam |
| U3-PINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerInventory.cs` | 原生页常量 |
| U3-PDINV | `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs` | 原生 `headers[]` |

---

## 1. 每页画了哪些按钮

`InventoryTidyUiPatch` Harmony postfix 挂在 `PlayerDashboardInventoryUI` 构造函数上（`InventoryTidyUiPatch.cs:32-33,305`）。循环上限 `HEADER_INJECT_COUNT = 5`（`:131`），`i=0..4`，`currentPage = (byte)(i + 2)`（`:339-341`）。每页 **3** 个按钮，全部 `Glazier.CreateButton()` 后 `AddChild` 到 `headers[i]`（`:433-441`）。

类头注释仍写「两个按钮」（`:17-21`），与 postfix 实际注入三按钮不一致；以 postfix 为准（`:357-448,451`）。

### 1.1 三按钮尺寸、文案、注入几何

父容器右对齐：`PositionScale_X = 1`。高度一律 `BTN_SIZE_Y = 60f`（`:110`），与原生 header `SizeOffset_Y = 60` 同高（U3-PDINV `:2743`）。

从左到右视觉顺序（`:105`）：

| 按钮 | 文案 | 宽×高 | `PositionOffset_X` | Tooltip | 点击 |
|---|---|---|---|---|---|
| 模式 M | `同类` / `空间` / `大件` | 60×60 | `-240` | 「整理模式：同类=相同物品聚合 / 空间=剩余大矩形优先 / 大件=大件优先贪心」 | `HandleModeClick` |
| 方向 A | `↓` / `↑` | 40×60 | `-175` | 「切换排序方向（↓ 从大到小 / ↑ 从小到大）」 | `HandleDirectionClick` |
| 整理 B | `整理` | 60×60 | `-130` | 「左键点击：整理当前空间的物品。\nCtrl + 左键点击：一键自动整理全身背包。」 | `HandleTidyClick` |

常量：`MODE_POS_OFFSET_X/MODE_SIZE_X` `:106-107`；`DIR_POS_OFFSET_X/DIR_SIZE_X` `:108-109`；`TIDY_POS_OFFSET_X/TIDY_SIZE_X` `:111-112`；tooltip `:133-136`。右侧预留 70px，避让原版「100%」耐久文字与绿色品质角标（`:98-105`）。原生品质标签 `PositionOffset_X = -105`、品质角标 `-25`（U3-PDINV `:2770-2789`），整理按钮右缘在 `-70`，正好落在安全区左边界（LIT-UI `:101`）。

间隔：模式右缘 `-180` 与方向左缘 `-175` 差 5px；方向右缘 `-135` 与整理左缘 `-130` 差 5px（`:102-103`）。

注入时方向按钮文本写死 `"↓"`（`:395`），不读字典当前值；模式按钮读 `s_PageTidyMode[currentPage]`（`:369-370`）。

### 1.2 `headers[0..4]` 对应哪些物品栏

LIT 注释：`headers[0..4]` → page 2..6，SLOTS..PANTS 服装页（`:17-18,126-127,335`）。

原生页常量（U3-PINV `:64-79`）：

| 页 | 常量 | 名称 |
|---|---|---|
| 2 | `SLOTS` | Hands |
| 3 | `BACKPACK` | Backpack |
| 4 | `VEST` | Vest |
| 5 | `SHIRT` | Shirt |
| 6 | `PANTS` | Pants |
| 7 | `STORAGE` | Storage |
| 8 | `AREA` | Area |

原生 `headers` 长度 `PAGES - SLOTS + 3 = 10`（U3-PDINV `:2739`）。网格标题索引 = `page - SLOTS`：

| headers[i] | page | 物品栏 | 原生证据 |
|---|---|---|---|
| `[0]` | 2 | Hands（手中） | `headers[0].Text = "Hands"`（U3-PDINV `:2792`） |
| `[1]` | 3 | Backpack（背包） | `onBackpackUpdated` → `headers[1]`（`:2236`） |
| `[2]` | 4 | Vest（背心） | `onVestUpdated` → `headers[2]`（`:2258`） |
| `[3]` | 5 | Shirt（上衣） | `onShirtUpdated` → `headers[3]`（`:2161`） |
| `[4]` | 6 | Pants（裤子） | `onPantsUpdated` → `headers[4]`（`:2185`） |
| `[5]` | 7 | STORAGE（储物/后备箱） | `STORAGE - SLOTS = 5`（`:1757-1765`） |

可整理页硬编码与原生一致：`TIDYABLE_PAGE_MIN = 2`、`TIDYABLE_PAGE_MAX = 6`（`HotkeySnapshot.cs:55-58`）。

### 1.3 STORAGE 为何不注入

`HEADER_INJECT_COUNT = 5` 故意不含 `headers[5]`（`:126-131,335-336`）。STORAGE 专用偏移常量标 `[Obsolete]`，仅历史参考（`:114-124`）。

原因（源码原话，`:115-117,128-130`）：

1. V2 协议只允许 page 2..6 服装页。
2. 容器页涉及 `InteractableStorage` 生命周期、跨玩家并发、工坊虚拟容器等独立权限模型，不能与服装页共用规则。
3. 注入会造成「UI 可点、服务端确定性拒绝」的误导性 UI。

页 0/1（主/副手槽）与页 8（AREA）也不在循环内。帽子/口罩/眼镜标题是 `headers[7..9]`（U3-PDINV `:1888-1905`），同样不注入。

---

## 2. 模式与方向存在哪：内存字典，不是设置

**结论：每页内存字典。不是 `SettingDescriptor`。Stop 清零。默认同类 + 降序。**

### 2.1 存储位置

两个静态字典（`InventoryTidyUiPatch.cs:79-94`）：

| 字典 | key | value | 默认 |
|---|---|---|---|
| `s_PageSortDescending` | page 2..6 | `true`=降序↓，`false`=升序↑ | `true`（`:80,457-460`） |
| `s_PageTidyMode` | page | `TidyMode` | `SameType=0`（`:88-91,463-466`） |

配套按钮引用 `s_DirectionButtons` / `s_ModeButtons` 只用于改按钮文本，不持久化。

`TidyInput` 注释写明方向与模式是「per-page memory state, explicitly non-persisted」（`TidyStrategy.cs:30-32`）。模块类头：「静态页状态（方向/模式字典、按钮引用）是模块代际内存态，Stop 边界清零，永不跨代泄漏」（`InventoryTidyModule.cs:34-36`）。

`CreateSettingsDescriptors` 只返回一条 `inventorytidy.enabled`（`:67-69`）。模式/方向 **没有** SettingId。

### 2.2 Stop 是否清零

是。`Stop` 阶段 3 调 `InventoryTidyUiPatch.ResetStateForShutdown()`（`InventoryTidyModule.cs:272-275`），实现（`InventoryTidyUiPatch.cs:44-51`）：

- `s_PageSortDescending.Clear()`
- `s_PageTidyMode.Clear()`
- `s_DirectionButtons.Clear()`
- `s_ModeButtons.Clear()`

`RefreshSwitches`（设置 enabled 翻转）**不是**代际边界：只装/卸 Harmony 补丁，**不清**这两本字典（`InventoryTidyModule.cs:309-327`）。因此：关设置再开设置，若模块未 Stop，每页模式/方向会留下来；真正 Stop（含面板 `SetFeatureEnabled(false)` → `Stop(UserDisabled)`）才清零。

### 2.3 默认值

- 模式：`TidyMode.SameType`（同类）。枚举：`SameType=0`、`MaxRects=1`（空间）、`FFD=2`（大件）（`InventorySolver.cs:68-80`）。按钮文案 `同类/空间/大件`（LIT-UI `:470-478`）。循环：同类 → 空间 → 大件 → 同类（`:555-570`）。
- 方向：降序（`true`）。按钮 `↓` =「降序（大件优先）」；`↑` =「升序（小件优先）」（`:80,457-460,541-542`）。

点击整理时读 **当前页** 的两本字典，传给 `RequestTidy`（`:601-625`）。Ctrl+全身整理也用 **当前页** 的方向和模式作为统一参数（`:583-618`）。

---

## 3. `inventorytidy.enabled` 与功能生命周期：两套开

**结论：存在两套。** 设置 Toggle 控制补丁/请求；面板 `SetFeatureEnabled` 控制模块 Start/Stop。二者不是同一条缝。实机面板目前只画出设置 Toggle，生命周期按钮未画。

### 3.1 设置如何声明

`InventoryTidyModule.EnabledSettingId = "inventorytidy.enabled"`（`:43`）。

唯一 descriptor 由 `ToggleDescriptor` 构造（`:442-448`）：

| 字段 | 值 |
|---|---|
| SettingId | `inventorytidy.enabled` |
| DisplayNameKey | `inventorytidy.enabled`（与 Id 相同，无独立显示名） |
| DescriptionKey | `inventorytidy.enabled`（同上，无独立描述） |
| Kind | `SettingKind.Toggle` |
| Authority | `SettingAuthority.ClientLocal` |
| DefaultValue | `SettingValue.Toggle(true)`（默认开） |
| Minimum / Maximum / Step | `default(SettingValueOption)`（无） |
| AllowedValues | `null` |
| MaximumUtf8Bytes | `0` |
| ValidationRuleId | `null` |
| SchemaVersion | `1` |
| SortOrder | `0` |
| VisibilityRuleId / EnablementRuleId | `null` |

注册 facet：`SettingDescriptors` 返回该单元素列表；`OnSettingsApplied` = `module.RefreshSwitches`（`InventoryTidyFeatureRegistration.cs:109-117`）。契约：`IFeatureSettingsRegistration`（`ContractTypes.cs:75-86`）。

读值：`ReadToggle()`（LIT-MOD `:433-439`）。无 scoped view 时返回 `true`（descriptor 默认）；有 view 则 `TryGet(EnabledSettingId)` 且 `value.Boolean`。测试钉死：空 store 默认开、恰好一条 persisted toggle（`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:4279,4291-4295`）。

### 3.2 面板如何编辑该设置

目录路由：`CatalogRoutingBueSettingsEditor` 按 catalog 的 SettingDescriptors 解析到该功能自己的 `SettingsRuntime`，接受后触发 `OnSettingsApplied`（`NetworkSettingsEditor.cs:62-112`）。组合根把该 editor 交给管理面板（`ClientUiCompositionRoot.cs:50-60`）。

LIT 详情页走通用 `AddBueSettingControl`（`BueNativeManagementPanel.cs:960-967,991-1010`）：

- 标签：`setting.SettingId + " = " + FormatSetting(...)`，Toggle 显示「开启/关闭」（`:993,1227`）。**不读 DisplayNameKey / DescriptionKey。**
- 可编辑 Toggle：`Glazier.CreateToggle()`，40×30，`OnValueChanged` → `TryEditBueSetting(..., setting.SettingId, BooleanValue)`。
- 接受后立刻保存（无草稿），状态行「BUE 设置已保存。」并 `Render()`。

非 Toggle 走文本框。LIT 当前只有 Toggle，不会走到文本框。

### 3.3 设置 enabled 与功能状态

**套 A — 设置 `inventorytidy.enabled`（功能内开关）**

`RefreshSwitches`（LIT-MOD `:309-327`）：重读设置；`Enabled=false` 或正在 ShuttingDown → `UninstallPatches()`（Harmony UnpatchSelf，`ActiveModule=null`）；`Enabled=true` → `FaultGate.Reset()` + `InstallPatches()`。注释写明：**不是**模块代际边界，dispatcher 队列不动。

`EnsureStarted` 仅当 `Enabled && !ShuttingDown` 才装补丁（`:302-306`）。请求入口 `RequestTidy` / `RequestLocalTidy`：`!Started \|\| !Enabled \|\| ShuttingDown` → `NativeFallback`（`:329-331,354-356`）。点击时无 ActiveModule 直接 return（LIT-UI `:608-612`）。

效果：关设置 = 补丁卸掉（新开背包不再注入按钮）+ 请求原生回退。**模块仍 Started。** 已构造的 `PlayerDashboardInventoryUI` 上旧按钮不会被这条路径拆掉；只是 `ActiveModule==null` 后点击 no-op（方向/模式 `:535-536,560-561`；整理 `:608-612`）。

**套 B — 功能生命周期 `SetFeatureEnabled`（宿主状态机）**

`BueFeatureStartRuntime.SetFeatureEnabled`（`:182-213,226-285`）：

- `enabled=false` → `Stop(UserDisabled)`：静默 → dispatcher 关停 → Unpatch + `ResetStateForShutdown`（模式/方向字典清零）。
- `enabled=true` → **新代际** `factory.Create()` + `module.Start(bootstrap)`。

面板模型有 `TryToggleFeature` → 该 seam（`ManagementPanel.cs:400-418`），组合根已接线（`ClientUiCompositionRoot.cs:62-66`）。**实机按钮未画**：`BueNativeManagementPanel` 零调用 `TryToggleFeature`；详情只显示「功能状态：」「表现状态：」只读标签（`:968-969`）。注释：「The native button UI is the named deferral of this ticket (随 09 实机面)」（`ManagementPanel.cs:407-408`）。

### 3.4 两套关系（T5 要用的事实）

| | 设置 `inventorytidy.enabled` | 生命周期 `SetFeatureEnabled` |
|---|---|---|
| 面板控件（现状） | 有：详情页 Toggle | 无按钮，只读状态行 |
| 持久化 | SettingsRuntime / 文件 | 否（状态机） |
| 默认 | true | 目录启动即 Start |
| 关 | 卸补丁、请求 NativeFallback；模块仍 Started；字典不清 | 模块 Stop；补丁撤；字典清零 |
| 开 | 装补丁、熔断复位 | 新模块代际 Start |
| 与按钮 | 卸补丁后新 UI 不再注入；旧按钮残留但点击无效 | Stop 同样卸补丁并清内存态 |

因此：**设置 enabled 与功能状态是两套开。** 开图已定「LIT 设置页不再保留 enabled 总开关、启停进草稿/SetFeatureEnabled」——现状正好相反：设置页有 enabled，生命周期按钮还没画。

---

## 4. `ITidyStrategy` / `StrategyId=default-grid-v1`：无选择器 UI

**结论：无选择器 UI（与预期一致）。** 本票不展开算法。

- 接口 `ITidyStrategy`：`StrategyId` + `BuildPlan(TidyInput)`（`TidyStrategy.cs:15-27`）。
- 注释：「no strategy picker UI and no third-party dynamic loading exist in this phase (spec: 不做)」（`:12-13`）。
- 内置唯一实现 `DefaultGridV1Strategy.StrategyId` 返回 `"default-grid-v1"`（`DefaultGridV1Strategy.cs:12-17`）。
- 模块构造写死 `Strategy = new DefaultGridV1Strategy()`（LIT-MOD `:60`）。setter 是开发者 seam，`null` 是开发者错误（`:80-81,332`）。
- SettingDescriptors **不含** StrategyId。管理面板、标题栏均无策略下拉/循环按钮。
- `ClientUi` satellite = `null`（LIT-REG `:107`）；整理按钮是功能私有 Harmony/Glazier patch，不经 ClientUi。

---

## 5. 全身整理（Ctrl+左键）绑在「整理」按钮上

**结论：是。去掉模式/方向按钮后，这条手势仍在——它只挂在「整理」按钮，不依赖另外两颗。**

`HandleTidyClick`（LIT-UI `:582-627`）：

1. 读 `InputEx.GetKey(KeyCode.LeftControl) || RightControl`（`:600`）。
2. Ctrl：**全身** `module.RequestTidy(LitRuntime.AllPages, mode, desc)`（`:614-619`）。`AllPages = 0xFF`（`LitRuntime.cs:21-26`）。诊断：「Ctrl+点击 -> 一键整理全身」。
3. 非 Ctrl：**当前页** `RequestTidy(page, mode, desc)`（`:621-626`）。
4. 全身仍用 **当前页** 字典里的 mode/desc（`:583,601-604`）。

Tooltip 已写明该手势（`:133-134`）。方向/模式按钮只切字典、不发整理（`:533-579`）。因此本阶段标题栏只留「整理」时，Ctrl+左键只要整理按钮还在就还在。无独立「全身整理」按钮。无锁定页按钮（LIT-UI 无 lock 符号/字典）。

补丁未装或 `ActiveModule==null` 时点击无效，手势随整理按钮一起消失/失效（见 §3.3）。

---

## 6. LIT 当前 `SettingDescriptors` 完整清单

`CreateSettingsDescriptors` 返回 **恰好 1 条**（LIT-MOD `:67-69,442-448`）。测试钉死 `Entries.Count == 1`（Plugin.Tests `:4293-4295,10752`）。

| 字段 | 值 |
|---|---|
| Feature | `io.github.yu80rice.bue.inventory-tidy`（`LitRuntime.cs:16`） |
| SettingId | `inventorytidy.enabled` |
| DisplayNameKey | `inventorytidy.enabled` |
| DescriptionKey | `inventorytidy.enabled` |
| Kind | `Toggle` |
| Authority | `ClientLocal` |
| DefaultValue | Toggle `true` |
| Minimum | 无（`default(SettingValueOption)`） |
| Maximum | 无 |
| Step | 无 |
| AllowedValues | `null` |
| MaximumUtf8Bytes | `0` |
| ValidationRuleId | `null` |
| SchemaVersion | `1` |
| SortOrder | `0` |
| VisibilityRuleId | `null` |
| EnablementRuleId | `null` |

**没有** 模式、方向、StrategyId、锁定页、或其他 Choice/Integer 描述。面板因此只渲染这一颗 Toggle（§3.2）。`SettingDescriptor` 字段全集见 `ContractTypes.cs:303-307`。

---

## 7. T5 可直接用的现状摘要

1. 标题栏每页三按钮：模式 60×60 @ -240、方向 40×60 @ -175、整理 60×60 @ -130；注入 `headers[0..4]` = Hands/Backpack/Vest/Shirt/Pants（page 2..6）。STORAGE（headers[5]/page 7）不注入，协议与权限模型不同。
2. 模式/方向 = 每页静态内存字典，非设置；默认同类 + 降序；Stop 清零；仅关设置不清零。
3. 唯一设置 `inventorytidy.enabled`（Toggle，默认开）。面板用 Glazier Toggle 即时保存。它与 `SetFeatureEnabled` 生命周期是两套：前者卸补丁但模块仍 Started；后者 Stop 整模块。实机只有设置 Toggle，没有生命周期按钮。
4. `ITidyStrategy` / `default-grid-v1` 无选择器 UI。
5. Ctrl+左键全身整理绑在「整理」按钮上；去掉模式/方向后手势仍在。
6. SettingDescriptors = 仅 `inventorytidy.enabled` 一条；DisplayNameKey/DescriptionKey 都等于 SettingId；AllowedValues = null。

