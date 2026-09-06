# DEV-16：真实运行时 ClientUi、原生库存接线与 BUE 管理面板

> 作者：GPT
> 状态：ready-for-agent
> 规格类型：多阶段生产实施规格
> 前置：DEV-15A～DEV-15E 纯 C# Seam、ADR-0001、ADR-0002
> 参考来源：`35117/UnturnedPluginManager`，采用提交 `9b75730`

## Problem Statement

当前 BUE 主 DLL 可以在 BepInEx 中加载、完成官方功能登记并进入 `RuntimeReady`，但真实的 Better Item Interaction 尚未进入游戏运行链：官方注册仍没有 ClientUi Satellite，主插件没有接入 Unity/Glazier/Unturned 库存回调，统一设置面板也没有显示 BUE 功能设置。因此玩家看不到绿色/红色占据投影、浮动物品图标和“更好的 UN 体验”设置项。

现有 `BetterUnturnedExperience.ClientUi` 工程已经包含纯 C# 生命周期、坐标转换、预览、原生适配器和投影中继，但它仍是测试/开发层的 Seam，不消费真实的 `PlayerDashboardInventoryUI`、`SleekItems`、`PlayerInventory`、Glazier 和 Unturned 设置 UI 对象。

同时，玩家需要一个只部署 `BetterUnturnedExperience.dll` 的统一管理面板，用于查看 BUE 功能和已加载 BepInEx 插件，并编辑安全范围内的配置。`UnturnedPluginManager` 已提供可吸收的插件列表、配置编辑、入口注入和 UI 重建处理经验；BUE 必须在保留作者、仓库、提交号和致谢的前提下，将能力内化为自己的面板，而不是依赖另一个运行时 DLL。

## Solution

将 BUE 从“已完成纯 C# Seam 的 Bootstrap 骨架”推进为“可在真实客户端消费的单 DLL 运行时”：

1. 将 BUE Core、官方 Better Item Interaction、管理面板和客户端 UI 适配代码编入最终 `BetterUnturnedExperience.dll`。
2. 由 BUE 主插件创建唯一 Runtime Composition Root，执行 Client/Headless 分流、功能注册、设置绑定、管理面板生命周期和 UI 重建恢复。
3. 通过受控 Harmony/访问桥连接 Unturned 原生库存 UI 和库存数据；原生对象只停留在 Adapter 层，候选计算、状态机和投影继续消费已有纯 C# Seam。
4. 将 `UnturnedPluginManager` 的列表与配置编辑能力改写到 BUE 自有命名空间和生命周期，移除其独立 BepInEx 插件身份、外部配置和运行时依赖。
5. BUE 管理面板同时显示 BUE Feature Catalog 和已加载 BepInEx 插件。BUE 功能使用 FeatureId/Settings Facet；普通插件使用 BepInEx GUID/公开 ConfigEntry。
6. BUE 面板支持收藏优先、收藏顺序、A→Z/Z→A 排序、主菜单入口和游戏内暂停菜单入口，并在 UI 树重建后安全重挂载。
7. BUE 自身设置以 `SettingsRuntime` 为唯一权威；普通插件仅允许安全编辑 bool、数字和字符串配置，不支持类型只读，需重启的变更明确提示。
8. U3DS Headless 使用相同主 DLL，但在创建 UI、解析客户端库存 Hook 和初始化 Glazier/Sleek 之前熔断；U3DS 只保留安全的核心/服务端路径。

## User Stories

1. 作为玩家，我希望只安装一个 `BetterUnturnedExperience.dll`，就能使用 BUE 框架和官方 Better Item Interaction。
2. 作为玩家，我希望没有其它功能插件时 BUE 仍能正常启动。
3. 作为玩家，我希望在主菜单中打开 BUE 插件管理面板。
4. 作为玩家，我希望在游戏内暂停菜单中打开同一个 BUE 插件管理面板。
5. 作为玩家，我希望面板列出所有已成功加载的 BepInEx 插件。
6. 作为玩家，我希望面板同时列出 BUE 官方功能和通过 BUE 注册的第三方功能。
7. 作为玩家，我希望每个条目显示稳定身份、显示名称、版本和运行状态。
8. 作为玩家，我希望收藏某个条目后它始终排在未收藏条目前面。
9. 作为玩家，我希望收藏条目按照加入收藏的先后顺序排列。
10. 作为玩家，我希望取消收藏后再次收藏时该条目进入收藏序列末尾。
11. 作为玩家，我希望选择未收藏条目按 A→Z 排序。
12. 作为玩家，我希望选择未收藏条目按 Z→A 排序。
13. 作为玩家，我希望排序方式、收藏列表和收藏顺序在重启游戏后保留。
14. 作为玩家，我希望看到 Better Item Interaction 的“增强交互开关”设置。
15. 作为玩家，我希望看到 Better Item Interaction 的“自动旋转”设置。
16. 作为玩家，我希望修改 BUE 设置后由 BUE 设置运行时统一校验和保存。
17. 作为玩家，我希望关闭增强交互后立即恢复原生拖拽。
18. 作为玩家，我希望重新开启增强交互后恢复绿色/红色预览和自动旋转能力。
19. 作为玩家，我希望在玩家背包网格中拖动地面物品时看到候选占据框。
20. 作为玩家，我希望在箱子、普通容器和车辆后备箱网格中拖入物品时看到候选占据框。
21. 作为玩家，我希望合法候选显示绿色占据框。
22. 作为玩家，我希望局部无效候选显示红色占据框而不提交非法操作。
23. 作为玩家，我希望浮动物品图标使用真实物品资产并跟随抓取点旋转。
24. 作为玩家，我希望容器滚动时占据框跟随网格内容，而浮动物品图标不被网格视口错误裁剪。
25. 作为玩家，我希望拖拽过程中切换容器、关闭容器或重开 UI 时旧预览被立即清理。
26. 作为玩家，我希望最终库存提交仍由 Unturned 原生权威链处理。
27. 作为玩家，我希望 BUE 接线失败时游戏仍保留原生拖拽，不因 BUE 崩溃而失去库存操作。
28. 作为玩家，我希望普通 BepInEx 插件的 bool 配置可以在 BUE 面板中编辑。
29. 作为玩家，我希望普通 BepInEx 插件的数字和字符串配置可以在 BUE 面板中编辑。
30. 作为玩家，我希望不支持或高风险配置类型只读显示。
31. 作为玩家，我希望需要重启的配置明确显示“需要重启”，而不是被强制热重载。
32. 作为玩家，我希望同时安装外部 `UnturnedPluginManager` 时 BUE 不崩溃、不重复修改其状态，并提示存在重复管理器。
33. 作为功能作者，我希望通过 BUE Settings Facet 暴露复杂设置、校验和服务器策略，而不依赖 BUE 私有字段。
34. 作为功能作者，我希望功能的 FeatureId、设置身份和收藏身份稳定，不因显示名称变化而丢失偏好。
35. 作为功能作者，我希望 BUE 的统一面板消费静态设置描述和不可变快照，而不是注入任意 Sleek/Unity 控件。
36. 作为功能作者，我希望官方 Better Item Interaction 与第三方功能使用同一注册、生命周期、设置和隔离 Seam。
37. 作为服务器运营者，我希望 U3DS 加载同一 BUE 主 DLL 时不创建 UI 或安装客户端库存 Hook。
38. 作为服务器运营者，我希望 U3DS Headless 中的 UI 缺失不会被误报为客户端功能成功。
39. 作为维护者，我希望原生 API 类型、方法签名和 Hook 失败时产生结构化诊断。
40. 作为维护者，我希望单个 UI 或功能异常只隔离该功能，不影响 BUE 其它功能和原版库存。
41. 作为维护者，我希望管理面板的 UI 树被 Unturned 重建后能够安全重挂载。
42. 作为维护者，我希望所有运行时接线都能通过单元、集成和真实环境门禁验证。
43. 作为贡献者，我希望 BUE 仓库明确声明吸收 `UnturnedPluginManager` 的来源、作者、提交号和许可记录。
44. 作为贡献者，我希望 BUE 不伪装成 `com.trae.pluginmanager`，而是拥有自己的 BepInEx 身份和配置空间。
45. 作为发布审核者，我希望真实客户端功能证据与框架静态测试证据严格分开。
46. 作为发布审核者，我希望新 DLL 哈希产生后旧 CandidateBuild 和旧运行证据自动失效。

## Implementation Decisions

### 1. 产品与程序集边界

- 最终玩家部署物为单一 `BetterUnturnedExperience.dll`。
- BUE Core、Contracts、官方 Better Item Interaction、管理面板和真实 ClientUi 适配代码进入该程序集。
- 原有 ClientUi 工程可以保留为测试/开发层，但不能作为官方运行时必需 DLL。
- 不保留外部 `UnturnedPluginManager` 的 BepInEx 插件身份、配置文件或运行时入口。
- `UnturnedPluginManager` 本地来源快照保留在工作区根目录，作为来源审计材料，不进入 BUE 玩家发布包。

### 2. 来源、许可与贡献

- BUE 仓库保留作者 `35117+Deepseek-v4-falsh-0731`、仓库链接、采用提交 `9b75730` 和致谢。
- 当前来源仓库未发现独立 LICENSE 文件；BUE 依据 2026-08-27 作者 `35117` 对“借用插件管理面板”明确回复“用吧用吧”的聊天许可记录实施。
- 吸收代码必须改写到 BUE 自有命名空间和生命周期，不复制外部插件身份或造成 ABI 混淆。
- 仓库保留来源说明和许可记录；如果作者后续提供正式许可证，以正式许可证补充或替换口头许可记录。

### 3. Runtime Composition Root

- 主插件只拥有一个 BUE Runtime Composition Root。
- Composition Root 负责 Bootstrap、Client/Headless 决策、BUE Host 绑定、官方功能注册、管理面板创建、ClientUi 组合、设置运行时绑定、异常隔离和销毁。
- `RegistrationOpen → CatalogFrozen → RuntimeReady` 仍由 BUE Host 控制；官方功能不使用私有特权路径。
- 只有客户端环境、非 BatchMode、非 Headless 且目标原生类型/方法探测通过时，才允许安装 ClientUi 和库存 Hook。
- 组件创建和 Hook 安装必须是幂等的；失败时输出结构化 FeatureId、DiagnosticId、Decision 和 Status，并进入功能级原生回退。

### 4. 统一管理面板

- 面板以 BUE 自有 UI 生命周期运行，参考并吸收 `UnturnedPluginManager` 的列表、设置编辑、按钮注入和 UI 重建处理方式。
- 主菜单和游戏内暂停菜单都提供“BUE 插件管理”入口。
- 面板只显示 BepInEx 已成功加载的插件，不扫描未加载 DLL、不主动加载任意程序集、不执行任意代码。
- BUE 功能条目消费 Feature Catalog、FeatureState、FeaturePresentationState、Settings Facet 和不可变 Snapshot。
- 普通插件条目消费 BepInEx GUID、Metadata、Assembly 信息和公开 ConfigEntry/ConfigFile。
- 普通插件没有 BUE FeatureId 或 Settings Facet 时，标记为普通 BepInEx 插件，不伪造 BUE 功能状态。
- 面板不提供运行时强制启用、禁用、卸载或热重载按钮。
- 检测到外部 `UnturnedPluginManager` 时只显示兼容提示，不主动卸载或修改外部插件。

### 5. 收藏与排序

- BUE 管理面板偏好使用 BUE 自己的持久化配置。
- BUE 功能使用 FeatureId 作为收藏键；普通插件使用 BepInEx GUID。
- 收藏条目始终优先于未收藏条目。
- 收藏条目按加入收藏顺序排列。
- 未收藏条目按玩家选择的 A→Z 或 Z→A 排列。
- 取消收藏后再次收藏，该条目进入收藏序列末尾。
- 显示名称变化、程序集文件名变化或列表刷新不得改变稳定身份。

### 6. 设置模型与外部配置编辑

- Better Item Interaction 的“增强交互开关”和“自动旋转”必须注册为 BUE Settings Facet，并继续由 `SettingsRuntime` 维护快照、revision、策略覆盖和生命周期语义。
- 若为兼容 PluginManager，BUE 可提供对应 BepInEx ConfigEntry 映射，但 ConfigEntry 不是第二事实源。
- 普通 BepInEx 插件仅允许通过公开 ConfigEntry/ConfigFile API 编辑 bool、整数/浮点数字和字符串。
- 对不支持、复杂或高风险类型只读显示，不反射调用私有字段/私有方法。
- 输入必须进行类型、长度、范围和枚举约束校验；失败时保留旧值并显示诊断。
- 配置提交成功后调用公开保存接口；无法保证运行时生效的配置显示“需要重启”，不进行热卸载。
- BUE 不执行任意配置路径、命令、脚本或代码。

### 7. 原生库存与 UI Adapter

- 真实引擎适配层允许引用当前 Unturned、Glazier、Unity 和 Harmony 运行库；这些引用不得泄漏到 Contracts 或纯 C# Core Seam。
- 原生接线必须通过受控 Harmony patch/访问桥，不修改 U3-SDK 源码和原生 `PlayerInventory`/`PlayerDashboardInventoryUI`。
- 接线目标包括库存 UI 构造/open/close、原生抓取/放置回调、每帧拖拽更新、Storage/Trunk 投影和 `sendDragItem` 提交路径。
- 私有原生对象（库存 UI 容器、网格 Items、itemsPanel、grid 等）只能在 Adapter 内访问，并通过明确的 `IInventorySurfaceContext` 转换为纯值输入。
- 普通网格拖入继续使用现有 `NativeInventoryInteractionAdapter` 分流；快捷槽、装备、AREA、交换、拖出地面和其它未裁决分支保持原生 Pass-Through。
- 原生提交必须继续沿用 `sendDragItem → ReceiveDragItem`，BUE 不建立平行库存 RPC 或客户端权威写入。
- 真实物品图标必须从 `ItemJar`/`ItemAsset` 资产身份进入 `SleekItemIcon.Refresh` 或等价的原生图标路径；纯值 `BoundAsset` 不得被误报为已经渲染真实纹理。
- UI 重建、容器关闭、切换容器、连接代际变化和功能关闭都必须对称清理图元、上下文和拖拽状态。

### 8. Headless 与运行环境

- 单人：启用完整管理面板、设置和 Better Item Interaction 客户端接线。
- SteamP2PFriends Host/Client：启用客户端 UI 与库存拖入路径，库存提交仍由原生权威收敛。
- U3DS Headless：加载同一主 DLL 的核心路径，但不得创建 Glazier/Sleek/UI 实例，不得安装客户端库存 Hook，不得执行客户端面板逻辑。
- Client/Headless 分流必须在任何 UI 组件工厂、原生 UI 类型访问或静态初始化之前完成。
- 目标类型/方法签名不匹配时只隔离该功能并保持原生回退；不得把 Hook 失败误报为功能可用。

### 9. 版本、依赖和发布边界

- 编译基线继续为 .NET Framework 4.7.2 与 C# 10。
- 生产客户端编译可引用当前固定的 `Assembly-CSharp.dll`、`SDG.Glazier.Runtime.dll`、Unity 模块和 `0Harmony.dll`；Contracts/Core 不得引用这些类型。
- 采用显式注册和显式组合，不使用 `Assembly.GetTypes()`、任意 DLL 扫描、类名猜测或全局 `PatchAll()` 作为功能发现机制。
- 新主 DLL 产生新的 CandidateBuild、LoadSetIdentity、DLL SHA-256 和 CaseId；旧 DEV-15E 候选证据不得继承。
- DEV-16 静态编译/测试通过不等于真实玩法通过；必须分别完成客户端、P2P 和 U3DS Headless 运行证据。

## Testing Decisions

### 测试原则

- 优先测试最高可用的 Runtime Composition Root 和公开 Adapter Seam，不测试私有字段枚举等实现细节。
- 每个测试必须断言外部可观察行为、状态、调用次数、持久化结果或安全隔离结果。
- 所有测试都必须区分客户端 UI、原生库存、设置兼容和 Headless 安全边界。

### 计划测试层

1. **Composition Root 集成测试**：客户端创建管理面板和官方 UI 组件；BatchMode/Headless/不可用条件下工厂调用为零。
2. **管理面板排序测试**：收藏优先、收藏顺序、取消后重新收藏、A→Z、Z→A、重启持久化和稳定 GUID/FeatureId 键。
3. **管理面板发现测试**：只展示 Chainloader 已加载插件；不加载未声明程序集；BUE 功能与普通 BepInEx 条目正确分层。
4. **ConfigEntry 兼容测试**：bool、数字、字符串的合法提交、非法输入拒绝、持久化、需重启标记和不支持类型只读。
5. **BUE Settings Facet 测试**：增强交互开关与自动旋转由 SettingsRuntime 唯一维护，快照、revision 和关闭后原生回退一致。
6. **原生 UI 生命周期测试**：构造/open/close、暂停菜单、主菜单、进入/离开地图和 UI 重建后的安全重挂载。
7. **库存接线测试**：普通网格抓取、拖入、拖拽更新、释放、容器切换、Storage/Trunk 投影、代际失配和关闭清理。
8. **原生分流测试**：普通网格合法候选进入 `sendDragItem`；快捷槽、装备、AREA、交换、拖出地面和未知页面均 Pass-Through。
9. **真实图标测试**：ItemJar/ItemAsset 身份转换到原生图标刷新路径，异步回调过期时不污染新拖拽代际。
10. **投影收敛测试**：提交后进入 AwaitingProjection，原生更新到达后收敛；超时只影响视觉等待，不伪造回滚。
11. **功能隔离测试**：单个 Hook、面板组件、图标回调或配置提交异常只隔离对应功能，BUE 和原生行为继续运行。
12. **Headless 测试**：U3DS 载入同一主 DLL 时不构造 ClientUi、不安装客户端 Hook、不解析或实例化 Glazier/Sleek 对象。
13. **程序集与静态门禁**：单 DLL 运行时闭包、Contracts/Core 零 UI/Native/LMN 类型泄漏、显式注册和无任意扫描。
14. **真实环境验证**：单人、SteamP2PFriends Host/Client 和 U3DS Headless 分别记录版本、部署来源、DLL 哈希、日志和时间窗。

### 现有测试先例

- 复用 DEV-05 ClientUi CompositionRoot 的生命周期和异常隔离测试模式。
- 复用 DEV-15A～DEV-15D 的 Native Adapter、Preview、Projection、Settings 和 SafeMode 测试模式。
- 复用 DEV-12 单 DLL Assembly Identity 与闭包断言。
- 复用 DEV-15E EvidencePackage/QualificationGate 的同候选、同哈希和 CaseId 绑定规则。

## Out of Scope

- 不修改 U3-SDK 源码、Unturned 原生源码或 SteamP2PFriends 源码。
- 不建立新的平行库存 RPC、客户端库存权威或自定义服务端库存写入。
- 不增强拖出到地面、自动整理、自动交换、批量移动或新的快捷键。
- 不支持尸体或其它未在 V1 规格中裁决的特殊库存页面。
- 不提供运行时强制卸载、启用、禁用或热重载第三方插件。
- 不扫描或主动加载未被 BepInEx Chainloader 成功加载的任意 DLL。
- 不允许普通插件通过私有反射注入 BUE 设置面板或任意 Sleek/Unity 控件。
- 不把 `UnturnedPluginManager.dll` 打包为 BUE 运行时依赖。
- 不把 BUE 框架证据当作 Better Item Interaction 的玩法资格或发布授权。
- 不在 DEV-16 中宣称三环境 ReleaseReady/Stable；真实资格证据属于 DEV-16E 及后续发布门禁。

## Further Notes

- 当前最重要的实现风险不是纯 C# 算法，而是当前 Unturned 版本中私有 UI 容器、SleekItems 网格、Glazier 图元和 Harmony Hook 的运行时稳定性。
- `PlayerDashboardInventoryUI` 的私有 `container`、`items`、`clothingBox`、`areaBox` 与 `SleekItems.itemsPanel/grid` 不得被假定为公开 API；必须由 Adapter 以版本绑定和失败隔离方式访问。
- 真实物品图标需要走原生 `ItemTool.getIcon`/`SleekItemIcon.Refresh` 等路径；预览数据中的纯值资产身份只是绑定输入，不是渲染完成证明。
- `UnturnedPluginManager` 的实现吸收必须同时满足功能一致性、BUE 生命周期、安全隔离和来源署名；不能把外部项目的全局静态状态原样带入 BUE。
- 实施顺序建议为：DEV-16A 主 DLL/Composition Root → DEV-16B 管理面板与设置 → DEV-16C 原生 UI/库存生命周期 Hook → DEV-16D 拖拽预览/提交/投影接线 → DEV-16E 真实环境资格证据。
- DEV-16A～D 任一阶段产生新的主 DLL 时，必须更新 CandidateBuild/LoadSetIdentity 和对应哈希；不得沿用 DEV-15E 的 `A13695A1...` 证据。
- 规格批准后下一步使用 `/to-tickets`，把每个阶段拆成带阻塞边的独立工单；拆票完成后再逐票使用 `/implement` 和 `/tdd`。
