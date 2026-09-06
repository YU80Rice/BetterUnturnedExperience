# BUE 原生库存注入研究报告对比与采信边界

**日期**：2026-09-01  
**范围**：只读对比；未修改生产代码、未构建、未生成 DLL。  
**目的**：比较 Gemini 与 DeepSeek 产出的两份研究报告，回溯 U3-SDK 与 UnturnedPluginManager（UPM）原码，判断哪些结论可直接采信、哪些只是二手总结、哪些与 DEV-16D/R13 冻结结论冲突，以及哪些仍须真实客户端验证。

## 1. 来源与证据等级

| 来源 | 类型 | 固定版本/位置 | 证据等级 |
|---|---|---|---|
| `D:\Agent-工作目录\docs\unturned-native-inventory-and-grid-injection-analysis.md` | Gemini 报告 | 报告日期 2026-09-01 | 二手分析；关键结论须回到源码 |
| `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\docs\research\2026-09-01-native-grid-rendering-boundaries.md` | DeepSeek 报告 | 报告日期 2026-09-01 | 二手分析；引用较完整，但“已实证/完全走通”等措辞需降级 |
| `D:\Agent-工作目录\U3-SDK` | 官方公开源码 | HEAD `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb` | Primary / source-confirmed |
| `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\UnturnedPluginManager` | 参考插件源码 | HEAD `9b75730a6240c9e1c41d7ddb492b176380d5904c` | Primary / implementation reference |
| BUE DEV-16D/R13 规格、冻结快照与工单 | 项目约束 | 当前仓库 `master` HEAD `e187b24` | 项目规范事实；不替代运行证据 |

### 1.1 采信规则

- “源码确认”只指在 U3-SDK/UPM 原码中可定位到的行为，不代表 BUE 在目标客户端已成功运行。
- “静态实现确认”只指 BUE 源码、测试或审查记录存在，不代表颜色、鼠标命中、纹理、旋转和页面重建在客户端表现正确。
- “实机验证”必须由新的 DLL、SHA-256、CandidateBuild/CaseId 和全新运行日志证明；历史静态 CLEAN 不能前移为玩法 PASS。

## 2. Primary source 回溯

### 2.1 原生层级、滚动与页面

U3-SDK 的 `SleekItems` 构造为 `SleekItems → horizontalScrollView → grid → itemsPanel`：

- `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Sleek\SleekItems.cs:229-255` 创建并挂载三层结构；
- 同文件 `:196-207` 将每个 `SleekItem` 加到 `itemsPanel`，位置为 `jar.x * 50`、`jar.y * 50`；
- 同文件 `:219-227` 从 `grid.GetNormalizedCursorPosition()` 计算网格点击格点并调用公开 `onPlacedItem`；
- 同文件 `:126-132` 的 `clear()` 调用 `itemsPanel.RemoveAllChildren()`，因此挂在其内的插件元素会随清理被移除。

页面动态换父由 `PlayerDashboardInventoryUI.updateBoxAreas` 完成（`D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\UI\Player\PlayerDashboardInventoryUI.cs:1814-1932`）；Storage 与 Area 在宽屏/窄屏之间会在 `clothingBox` 与 `areaBox` 间重挂。该事实支持“必须跟随 live surface/父链重建”，不支持把绝对屏幕坐标视为永久有效。

### 2.2 根窗口与原生驱动

- `D:\Agent-工作目录\U3-SDK\Assets\Runtime\Assembly-CSharp\Unturned\Player\PlayerUI.cs:2173-2182` 的 `PlayerUI.Update` 每帧调用 `PlayerDashboardInventoryUI.updateDraggedItem()`；
- `PlayerDashboardInventoryUI.cs:2408-2416` 用 `dragPivot` 与 `PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition)` 刷新原生 `dragItem`；
- `PlayerUI.cs:1284-1290` 将 `PlayerUI.window` 设为 `Glazier.Get().Root`；
- `PlayerDashboardInventoryUI.cs:2820-2824` 与 `:3099-3100` 将 `selectionFrame`、`dragItem` 直接挂到 `PlayerUI.container`。

这证明 `PlayerUI.container` 是原生可用的顶层注入父级，且拖拽视觉的官方跟手语义是 viewport-normalized position + drag pivot，而不是任意独立 GameObject 的 Update。

### 2.3 委托与占据权威

- `SleekItems.cs:10-18` 公开 `onSelectedItem`、`onGrabbedItem`、`onPlacedItem`；
- `PlayerDashboardInventoryUI.cs:2804-2806` 将这些委托接到原生处理器；
- `PlayerDashboardInventoryUI.cs:1084-1151` 保存 `dragFromPage/x/y/rot`、计算 `dragOffset/dragPivot` 并启动拖拽；
- `PlayerDashboardInventoryUI.cs:1154-1325` 执行目标夹取、`checkSpaceDrag`、交换与原生提交；
- `Items.cs:41` 的私有 `slots[,]` 是原生逐格占据状态，公开 `Items.items` 只是 `ItemJar` 列表；`Items.cs:441-659` 的 `checkSpaceEmpty/checkSpaceDrag/checkSpaceSwap/tryFindSpace` 体现 footprint 语义。

结论：委托包装和只读反射是可行 seam；把压缩 `Items.items` 当作逐格数组不是原生语义。

### 2.4 渲染元素与射线

- `Glazier_uGUI\GlazierBox_uGUI.cs:219` 将 `Image.raycastTarget` 设为 `true`；
- `Glazier_uGUI\GlazierImage_uGUI.cs:201` 默认将 `RawImage.raycastTarget` 设为 `false`；
- `SDG.Glazier\Sleek.cs:181-185,228` 公开 `AddChild` 与 `SetAsFirstSibling`；
- `Glazier_uGUI\GlazierElementBase_uGUI.cs:253-296,331-376,451-479` 说明移除会销毁元素、AddChild 可换父、first-sibling 会映射到底层 RectTransform；
- `Glazier_uGUI\Glazier_uGUI.cs:302,746-801` 说明默认 uGUI 支持深度并在 LateUpdate 递归更新。

因此，“CreateImage 比 CreateBox 更不易吞掉网格射线”是源码确认的默认属性；但最终是否遮挡点击、颜色是否可见、兄弟顺序是否稳定仍需客户端验证。

### 2.5 UPM 成功的驱动模式

`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\UnturnedPluginManager\PluginManagerMod.cs`：

- `:27-45` 在 `Awake` 尝试 Harmony，并明确在失败时保留轮询兜底；
- `:47-50` 由插件自身 `BaseUnityPlugin.Update()` 每帧直调 `PluginManagerUI.Tick()`；
- `:280-319` 每帧检查 UI 重建、入口按钮、暂停按钮和关闭条件；
- `:406-451`、`:458-503` 反射获取 `MenuWorkshopUI.container`/`PlayerPauseUI.container`，创建按钮并直接 `container.AddChild(button)`。

UPM 的关键不是“用了 Harmony”，而是已加载插件自身 Update 的可靠轮询；Harmony 只是快速路径。该模式可作为 BUE 运行驱动的参考，但不能证明 BUE 的 Harmony/Postfix 在当前客户端一定命中。

## 3. 两份报告的结论对照

| 主题 | Gemini 报告 | DeepSeek 报告 | 回溯结论 |
|---|---|---|---|
| 是否可在原生网格加东西 | “绝对确定、完全可行” | “能；BUE 已走通” | **能力层 source-confirmed；BUE 运行层不可由报告单独证明**。可行不等于已显示。 |
| 网格层级 | `scroll → grid → itemsPanel` | 同 | **一致、可采信**（`SleekItems.cs:229-255`）。 |
| 顶层浮动图标 | `PlayerUI.container` | `PlayerUI.container`，仿原生 `dragItem` | **一致、可采信**（`PlayerDashboardInventoryUI.cs:3099-3100`）。 |
| 拖拽驱动 | `updateDraggedItem` Postfix | `updateDraggedItem` 或 `PlayerUI.Update` Postfix | 方法存在和原生调用链可采信；Harmony 命中/稳定性仍 **待实机**。UPM 还证明应保留插件自身 Update 兜底。 |
| 放置拦截 | 公开 `onPlacedItem` 重绑定最佳 | 同 | **一致、可采信**；需处理每次 surface 重建后的恢复和原生 Pass-Through。 |
| CreateImage/CreateBox | CreateImage 必须；CreateBox 会吞点击 | CreateBox 为严重缺陷，Image 默认穿透 | `raycastTarget` 默认值 **源码确认**；“一定吞点击/一定能穿透”属于 **需实机验证**，但 CreateBox 是高风险选择。 |
| itemsPanel 挂载 | 最优，天然滚动剪裁 | 同时指出 `clear()`/遮罩/变暗风险，建议顶层 | **部分冲突，实际是条件结论**：itemsPanel 坐标最自然，但生命周期、遮罩和禁用态风险真实存在（`SleekItems.cs:126-132`、`PlayerDashboardInventoryUI.cs:213-218,246-257`）。不能称“无风险最优”。 |
| 页面范围 | 建议扩展到所有网格/槽位 | 当前 BUE 仅 3/7，可扩展 | 与 DEV-16D 规格冲突。当前冻结矩阵明确只增强 Backpack 与 Storage/Trunk；Hands/Vest/Shirt/Pants/AREA/槽位保持 Pass-Through。扩大范围必须另立需求。 |
| 占据算法 | 以 `ItemJar` footprint 为准 | 同时称 BUE 当前已生产级对齐 | footprint 原则 **可采信**；“当前 BUE 已生产级对齐”只能算静态描述，不能替代真实运行证据。 |
| BUE 当前状态 | 较少强调运行失败边界 | 称“逐项核对走通” | 与 R13 冻结不完全一致：R13 记录了静态 CLEAN 但颜色、鼠标跟手、纹理、旋转和页面覆盖仍 `RUNTIME_UNVERIFIED`。应以冻结快照为准。 |
| UPM/插件 Canvas | 未突出 | 补充官方可容忍独立 Canvas | 可作为备用 seam，但会失去原生 scroll/换父联动；不应因此替换当前原生树方案。 |

## 4. 一致项、新增项与冲突项

### 4.1 与既有分析一致且可采信

1. 原生网格真实挂载链是 `horizontalScrollView → grid → itemsPanel`。
2. `PlayerUI.container` 是官方自身用于 `dragItem`/`selectionFrame` 的顶层挂载点。
3. `SleekItems.onPlacedItem` 等公开委托适合包装；不应 Harmony patch 170 行的 `PlayerDashboardInventoryUI.onPlacedItem` 大方法。
4. 原生 Update → `updateDraggedItem` → `dragPivot`/viewport 形成拖拽视觉链。
5. `ItemJar` 的位置、旋转和资产 footprint 才是占据计算语义；`Items.items` 不是逐格数组。
6. Storage 与 Trunk 共用 STORAGE page 7 网格；二者差异主要在来源状态和面板标题/车辆附加 UI。

### 4.2 对既有分析有价值的新增细节

1. `GlazierBox_uGUI` 与 `GlazierImage_uGUI` 的默认射线属性，为“预览框不能阻塞原生 grid”提供了更具体的风险证据。
2. `SetAsFirstSibling()` 可用于背景层级，但其在物品增量创建/池化后的长期稳定性仍需真机观察。
3. `SleekItems.clear()` 与 `RemoveAllChildren()` 的销毁语义，要求任何 itemsPanel 子元素都必须在新 surface 上重建；R13 已将页面级 detach/discard 纳入生命周期测试。
4. UPM 的实现展示了可靠的“插件自身 Update 直驱 Tick + Harmony 快速路径”模式，可用于解释为何只依赖 Harmony 命中不足。
5. U3-SDK 明确有 `CanvasSortOrders`/`SleekWindow.hackSortOrder` 等插件 UI 兼容考虑；这只是备用方案，不代表独立 Canvas 自动获得网格滚动语义。

### 4.3 与既有结论冲突或需要降级的内容

1. **“完全可行/已走通”**：只能证明注入能力和静态触点，不应升级为真实客户端成功。当前 R13 仍要求 DEV-16E 实机资格验证。
2. **“扩展所有页面是后续蓝图”**：不适用于当前 DEV-16D；当前 spec 明确固定 Backpack + Storage/Trunk 增强矩阵，其他页面原生 Pass-Through（`spec-DEV-16D-native-conformance-remediation.md:17,26,82-94,196-200`）。
3. **“itemsPanel 是无条件最优”**：应改写为“坐标/剪裁最自然，但受 clear、遮罩和禁用态约束”；R13 当前仍使用该 seam 并通过页面级重建防护处理生命周期。
4. **“BUE seam 已实证”**：BUE 测试和静态审查可以证明代码路径存在；只有真实客户端日志/录屏才能证明 `preview-evaluated`、颜色、图标纹理、鼠标跟手和自动旋转。

## 5. 对 DEV-16D/R13 当前实现的影响

### 5.1 保持不变的部分

- 继续使用 native `SleekItems` live surface 的父链探测和 page-local attach/detach；
- 占据快照统一由 `ItemJar` footprint/rotation 构建，preview 与 swap guard 共用；
- 浮动图标挂 `PlayerUI.container`，坐标复用 native viewport/dragPivot 语义；
- 委托包装 `onPlacedItem`，提交仍走原生 `sendDragItem` 适配；异常路径回退原生；
- 保持单 DLL、Headless 隔离和固定 Pass-Through 页面矩阵。

### 5.2 应列为后续运行资格验证的重点

1. BUE 自身 heartbeat 是否每帧执行，不能只看安装/初始化日志；应记录 `PlayerUI.Update`/`updateDraggedItem`/surface dispatch 计数。
2. `CreateBox` 预览框是否因 raycastTarget 或遮罩阻塞 grid；必要时以 `CreateImage` 或明确禁用 raycast 的原生元素替代，但需先有回归测试和规格裁定。
3. itemsPanel 子元素在 `clear()`、Storage 关闭、双栏换父和增量建格后的存活、层级及颜色。
4. `GetNormalizedCursorPosition`、UI scale、viewport、scroll 只转换一次；网格内外指针都要有真实日志。
5. `ItemTool.getIcon` 异步回调是否刷新真实纹理、quality/state/rotation；纯值 `BoundAsset` 不能单独证明纹理显示。
6. 自动旋转必须与设置快照、顺时针候选搜索和原生 `dragJar.rot` 生命周期一致；随机触发不能由静态报告排除。

## 6. 明确分类

### 可采信（源码确认）

- U3-SDK 网格层级、50px 局部坐标、grid 命中和公开委托；
- `PlayerUI.window → Glazier.Root`、`PlayerUI.container` 顶层挂载；
- 原生 Update/拖拽/rotation/dragPivot/icon refresh 调用链；
- `Items.slots` 与 `ItemJar` footprint 语义；
- Storage/Trunk 共用 STORAGE page 7；
- UPM 的插件自身 Update 轮询兜底和反射 `container.AddChild(button)` 注入；
- Glazier 工厂元素、AddChild、换父、移除销毁和默认 raycast 属性。

### 不可单独采信（仅二手总结或静态描述）

- “BUE 已完全走通/生产级、所有触点已经实证”；
- “CreateImage 一定解决点击/颜色/可见性”；
- “itemsPanel 挂载绝对不会被清理或遮罩影响”；
- “Harmony postfix 在目标发行版必然命中”；
- “扩展到所有页面无需新的范围审批”。

### 待真实客户端验证

- 新候选 DLL 是否实际加载并持续驱动；
- Backpack、普通 Storage、车辆 Trunk 的真实预览颜色、z-order、跟鼠标和 scroll/scale 对齐；
- 真实物品纹理、quality/state、异步刷新和旋转显示；
- 自动旋转触发稳定性与关闭开关后的原生回退；
- 预览框是否阻塞原生点击、拖出是否正确落入原生丢弃路径；
- 页面重建、换父、关箱和会话代际后的 stale preview 清理；
- 单机、SteamP2PFriends Host/Client、U3DS 三环境资格。

## 7. 推荐下一步

1. 不废弃 DEV-16A/B/C/E，也不扩大 DEV-16D 页面范围；继续以 R13/CLEAN 候选作为 DEV-16E 实机资格基线。
2. 用新的 CaseId 先做“驱动计数 + surface attach + preview state/颜色 + pointer domain”诊断包；日志必须能区分 Hidden、Candidate、LocallyInvalid 和 Pass-Through。
3. 若运行证据确认 CreateBox 的 raycast 或视觉层问题，再单独建立红测，评估把占据框换成 `CreateImage` 或受控原生元素；不要仅凭报告文字直接重写。
4. 若需 Hands/Vest/Shirt/Pants/AREA 或槽位增强，先走需求变更/规格/工单，不得把报告建议直接当作 DEV-16D 授权。
5. 把 UPM 的自身 Update 兜底作为运行驱动审查项；Harmony 仅作加速，不能是唯一心跳。

## 8. 最终判定

两份报告对 U3-SDK 的核心层级、委托、拖拽和挂载事实具有较高参考价值，且大部分可由 primary source 复核。DeepSeek 报告对 `clear()`、raycast、层级和 BUE 差异的风险补充尤其有用；Gemini 报告的调用链和页面整理也有价值。

但两份报告都不能单独证明 BUE 在真实客户端已经实现绿色/红色预览、自动旋转、真实图标刷新或稳定输入。凡是“绝对确定”“生产级走通”“完全符合”等措辞，均应降级为“静态可行/待运行验证”。当前 DEV-16D 的正确工程结论是：**原生注入 seam 可行，R13 静态闭环已完成，但真实玩法资格仍未由这两份报告闭合；下一步是基于新 CandidateBuild/CaseId 的 DEV-16E 运行证据，而不是据此直接扩大范围或重写架构。**
