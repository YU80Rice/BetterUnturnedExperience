# DEV-16D Native Conformance Remediation 实施规格

**Status:** `ready-for-agent`  
**Parent ticket:** DEV-16D 拖拽预览、真实图标、原生提交与投影收敛  
**Freeze source:** `DEV-16D-FREEZE-20260901-1048`  
**责任变更：**相关前端实现原由 Gemini 负责，现由 GPT 接手。  
**Scope:** DEV-16D 的符合性修复增量；不废弃 DEV-16A、DEV-16B、DEV-16C 或 DEV-16E。

## Problem Statement

DEV-16D 已经具备与 U3-SDK 原生库存 UI 接线相近的结构：真实 `SleekItems` 层级探测、`itemsPanel` 内容层挂载、顶层浮动图标、`PlayerUI.Update` 主线程驱动、`dragPivot`、受控 `onPlacedItem` 包装以及原生 `sendDragItem` 提交路径。

但冻结审计发现，当前实现不能证明与原生库存占据语义完全一致：

1. 占据适配器把 `Items.items` 的压缩 `ItemJar` 列表误当成逐格数组，使用线性索引判断占用；这不等价于 U3-SDK 的旋转 footprint 与 `slots[,]` 语义。
2. 拖拽预览和 native swap guard 各自维护一套占据算法，存在事实源分叉。
3. 当前玩家侧增强范围必须与 DEV-16D 规格保持一致：增强 Backpack 与 Storage/Trunk；装备页、AREA 和其它未裁决页面保持原生 Pass-Through。未接入装备页不是本规格要求的扩大范围。
4. R12 的静态构建、测试和双轴 CLEAN 只证明当轮增量，不能证明上述符合性问题已经关闭，也不能直接产生新的运行资格。

## Solution

在 DEV-16D 内增加一个 Native Conformance Remediation 增量：

- 建立唯一的 `ItemJar footprint → occupancy snapshot` 原生适配 seam，并让预览候选与 swap guard 共同消费；同一容器内拖拽物品的来源 footprint 必须被排除，无法安全识别时采用原生回退。
- 保持已经验证的原生渲染与输入 seam：`SleekItems → horizontalScrollView → grid → itemsPanel`、`PlayerUI.Update`、`dragPivot`、`SleekItems.onPlacedItem`、`itemsPanel` frame、顶层 icon 和原生 `sendDragItem`。
- 固定 DEV-16D 页面矩阵：Backpack 与 Storage/Trunk 进入增强路径；Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出地面和未知页面保持原生 Pass-Through。
- 保持已有坐标域策略：优先消费原生 grid-content-local 指针；scroll 只应用一次；viewport、UI scale、抓取偏移和旋转全部来自同一坐标采样 seam。
- 保持自动旋转、真实图标、投影等待、功能隔离、Headless 隔离和单 DLL ABI；不修改原生库存权威，不建立平行 RPC。

## User Stories

1. 作为玩家，我希望从地面把物品拖入 Backpack 时，预览占据范围与原生真实占据一致，以便不会看到错误的可放置位置。
2. 作为玩家，我希望把 Backpack 中的物品拖入普通容器时，预览能正确避开容器内已有物品，以便放心释放。
3. 作为玩家，我希望把物品拖入车辆后备箱时，后备箱使用与普通容器一致的增强预览体验。
4. 作为玩家，我希望从普通容器或车辆后备箱把物品拖回 Backpack 时，目标网格使用同一套占据规则。
5. 作为玩家，我希望旋转后的 1×2、2×1、2×2 等物品按旋转后的 footprint 显示，而不是按未旋转尺寸判断。
6. 作为玩家，我希望同一网格内移动物品时，原物品自己的来源格不会被错误地当成阻塞物。
7. 作为玩家，我希望预览与原生交换判断对同一格给出一致结果，不会出现预览允许但释放走另一套规则的情况。
8. 作为玩家，我希望光标在有效网格内移动时，占据框准确跟随鼠标，不因滚动或 UI 缩放产生双重偏移。
9. 作为玩家，我希望光标离开有效网格或超出 viewport 时，预览立即隐藏，不会在错误位置残留。
10. 作为玩家，我希望合法候选显示绿色半透明占据框。
11. 作为玩家，我希望局部无效候选显示红色占据框且不会提交非法操作。
12. 作为玩家，我希望浮动物品图标使用真实 ItemAsset/ItemJar 身份，并沿原生图标刷新路径显示。
13. 作为玩家，我希望浮动物品图标的旋转和抓取点与原生拖拽一致。
14. 作为玩家，我希望开启自动旋转后，当前方向不能放置时尝试顺时针 90°方向。
15. 作为玩家，我希望关闭自动旋转后，系统绝不偷偷尝试其它方向。
16. 作为玩家，我希望正方形物品不会进行无意义的旋转搜索。
17. 作为玩家，我希望关闭增强交互后立即恢复原生拖拽和原生提交。
18. 作为玩家，我希望重新开启增强交互后，下一次拖拽恢复增强预览与自动旋转。
19. 作为玩家，我希望切换容器、关闭背包、死亡、换服或重连时旧预览被清理，不影响新容器。
20. 作为玩家，我希望原生 UI 重建后增强图元重新挂到新的 `itemsPanel`，而不是继续使用旧引用。
21. 作为玩家，我希望增强功能发生异常时只关闭 Better Item Interaction，并保留原生库存和其它 BUE 功能。
22. 作为玩家，我希望装备槽、Hands、Vest、Shirt、Pants、AREA 和未知页面继续使用原生交互，不被增强层误拦截。
23. 作为玩家，我希望拖出到地面或丢弃物品继续使用原生行为。
24. 作为玩家，我希望合法释放最终仍由原生库存权威校验和收敛。
25. 作为玩家，我希望服务端拒绝、延迟或库存更新顺序变化不会被客户端伪造成成功或回滚。
26. 作为插件作者，我希望占据规则位于一个明确的原生适配 seam，便于验证和替换，而不是分散在多个 UI/拖拽类中。
27. 作为插件作者，我希望 Contracts/Core 不依赖 Unity、Glazier、Sleek 或 Unturned 类型。
28. 作为插件作者，我希望 BUE 继续以单一 `BetterUnturnedExperience.dll` 交付，不要求玩家安装 BUE 的额外核心 DLL。
29. 作为服务器维护者，我希望 U3DS Headless 加载同一主 DLL 时不创建 ClientUi、不访问原生 UI 类型、不安装客户端库存 Hook。
30. 作为维护者，我希望每个修复后的 DLL 都有新的 CandidateBuild、BuildIdentity、SHA-256 和 CaseId，旧证据自动失效。
31. 作为后续 Agent，我希望从冻结快照、这份规格和 DEV-16D 工单即可继续工作，不需要依赖当前对话历史。

## Implementation Decisions

### 1. 工单与版本边界

- 本规格是 DEV-16D 的 remediation 增量，不创建替代版 DEV-16，也不删除历史 DEV-16D 提交。
- DEV-16A 单 DLL/Composition Root、DEV-16B 管理面板/设置、DEV-16C 原生库存生命周期继续有效。
- DEV-16E 三环境资格工单继续存在，并继续阻塞于 DEV-16D；只有本规格完成后产生的新候选 DLL 才能进入 DEV-16E。
- 旧 R12 CandidateBuild、CaseId、DLL hash 和运行证据不得继承给 remediation 产物。

### 2. 唯一占据事实源

- 原生 `Items` 数据是唯一输入；每个 `ItemJar` 由其左上角坐标、资产尺寸和旋转值展开为 footprint。
- 适配器生成不可变 occupancy snapshot；snapshot 暴露网格尺寸、每格是否被占据以及必要的来源身份信息。
- Preview evaluator 与 native swap guard 必须消费同一个 snapshot，不得各自遍历 `Items.items` 或维护第二个布尔网格。
- 当拖拽物品与目标容器是同一容器时，snapshot 必须排除当前拖拽物品自身的来源 footprint；跨容器拖入不得排除目标容器中的任何物品。
- 来源排除必须使用可验证的页面、坐标、旋转和物品 fingerprint；身份不确定时不得猜测，直接进入原生 Pass-Through 或局部降级。
- 不直接写入原生 `slots[,]`，不修改 `Items.items`，不创建客户端库存副本。

### 3. 页面范围与原生回退

固定增强目标：

- 玩家 Backpack 网格（`BACKPACK` page）；
- 普通容器与车辆后备箱共享的 `STORAGE` page。

固定 Pass-Through：

- Primary/Secondary 装备槽；
- Hands、Vest、Shirt、Pants 页面；
- AREA/附近地面物品页面；
- 拖出到地面、丢弃、未知页面、未识别的第三方 UI seam。

若未来要增强装备页或 AREA，必须另立需求变更和独立工单，不得在本规格中顺手扩大。

### 4. 原生 UI 注入 seam

- 只读取当前 live `SleekItems` 的 `horizontalScrollView`、`grid`、`itemsPanel`，并验证 owner 和完整父子链。
- 占据框是 content overlay，挂到当前 `itemsPanel`，跟随原生 scroll 和 viewport clip。
- 浮动物品图标挂到当前玩家 UI 顶层容器，避免被网格 viewport 裁剪。
- overlay 不抢占 grid 命中；不得覆盖或替换原生 `grid.OnClicked` 命中层。
- 不替换 `Glazier.Root`，不创建与原生 `SleekWindow` 竞争的全局根。
- 公开 `SleekItems.onPlacedItem` 只能通过保存 exact original delegate、幂等包装、按 surface/session 重新绑定和可逆 detach 接入。
- `PlayerUI.Update` 是主线程 heartbeat；Harmony 只能作为受控观察/快速路径，不能成为唯一驱动保障。
- 所有反射、坐标读取、图元创建、AddChild、delegate 写入和原生提交均必须在游戏主线程执行。

### 5. 坐标与滚动

- 统一坐标采样必须记录 pointer domain、viewport、grid/content 原点、UI scale、cell size、scroll、dragPivot 和 grab offset。
- 当前原生 pointer 已处于 `GridContentLocal` 时不得再次补偿 scroll；Screen/Viewport 输入最多补偿一次。
- viewport 不可用、数值非有限、父链失效或 pointer 无法安全映射时，预览隐藏并回退原生，不使用猜测坐标。
- 旋转抓取点使用原生 forward 规则；候选计算不重新发明 grab offset，也不依赖时间、帧率、随机数或集合迭代顺序。

### 6. 预览与自动旋转

- 候选优先级沿用已冻结的 placement algorithm：当前方向局部候选优先，其次自动 90°局部候选，再到扩展候选。
- 当前方向可放置时不强制旋转；只有当前方向无合法候选且 `AutoRotate` 快照开启时才尝试顺时针 90°。
- 正方形 footprint 跳过无意义的旋转搜索。
- `Candidate` 显示绿色 frame；`LocallyInvalid` 显示红色 frame；`Hidden`/越界/代际失配隐藏 frame 和 icon。
- 预览只生成视觉和原生提交参数，不拥有库存权威。

### 7. 真实图标与表现层

- 浮动图标必须从当前 `ItemJar`/`ItemAsset` 身份进入原生 `SleekItemIcon.Refresh` 或等价路径。
- quality、state、asset identity 和 rotation 的传递必须保持一致；纯值 `BoundAsset` 只表示输入绑定，不可单独宣称纹理已渲染。
- 图元必须池化或复用；异步图标回调必须受 DragGeneration/SessionGeneration 保护，过期回调不得污染新拖拽。

### 8. 原生提交、投影与回退

- 合法普通网格候选沿原生 `sendDragItem → ReceiveDragItem` 路径提交。
- 原生 swap、装备、AREA、拖出、未知页面和增强关闭路径必须保持 Pass-Through。
- 提交后进入 `AwaitingProjection` 只表示等待原生模型投影；超时不得伪造拒绝、成功或客户端回滚。
- UI close、surface rebuild、container change、connection change、feature disable、exception 和 native drag end 都必须对称清理 frame、icon、delegate、session 和 generation。

### 9. 单 DLL、Headless 与隔离

- 玩家侧仍只部署 `BetterUnturnedExperience.dll`；ClientUi/Core/Contracts 继续按现有构建聚合策略处理。
- Client/Headless 分流必须早于任何 Glazier/Sleek/UI 实例化或客户端原生类型访问。
- Hook、occupancy、preview、icon、delegate 或 projection 任一环节失败，只隔离 Better Item Interaction 并恢复原生；不得拖垮 BUE Host、设置中心或其它功能。
- 不引入新第三方运行时依赖，不扫描或主动加载任意 DLL，不使用全局 `Assembly.GetTypes()`/未知 `PatchAll()` 作为发现机制。

## Testing Decisions

### 测试原则

- 先红后绿；每个回归测试必须在修复前真实失败，再在最小修复后转绿。
- 优先通过最高可用 seam 验证外部行为、状态、调用次数、挂载关系和回退结果，不测试私有反射实现细节本身。
- 所有测试必须区分 ClientUi、原生库存、SettingsRuntime、Headless 和运行资格证据；纯 C# PASS 不得冒充真实客户端 PASS。

### 必须新增或补齐的测试

1. **Occupancy 红测**：用稀疏 `ItemJar` 列表、不同左上角坐标、1×2/2×1/2×2 footprint 证明线性 `Items.items[index]` 算法失败。
2. **旋转 footprint 测试**：证明 `rot` 交换宽高后所有覆盖格与原生参考模型一致。
3. **来源排除测试**：同页移动时排除被拖拽物品自身 footprint；跨容器移动时不排除目标容器已有物品；fingerprint 不确定时进入回退。
4. **单一事实源测试**：preview evaluator 和 swap guard 使用相同 snapshot，在同一组占据输入上结果一致。
5. **页面分流测试**：Backpack 与 Storage/Trunk 进入增强；Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出和未知页面均 Pass-Through。
6. **native hierarchy 测试**：确认 `scroll → grid → itemsPanel` 父链、itemsPanel content mount、top-level icon mount 和重建后的重新绑定。
7. **坐标测试**：GridContentLocal 不重复 scroll；Screen/Viewport 只补偿一次；UI scale、viewport、边界和非有限输入 fail-closed。
8. **拖拽与旋转测试**：原生 dragPivot、连续 0→1→2→3→0 旋转、非中心抓取点和自动旋转开关均保持确定性。
9. **视觉测试**：Candidate/LocallyInvalid/Hidden 的绿色、红色、隐藏分流；真实 ItemAsset 身份、quality/state、rotation 和过期异步回调保护。
10. **delegate 与提交测试**：exact original delegate 保存/恢复、幂等 attach/detach、合法候选调用原生 action、非法候选不提交、原生分支不拦截。
11. **生命周期测试**：关闭、重建、容器切换、代际失配、功能关闭、native drag end、异常和 SafeMode 均清理自定义状态并保留原生行为。
12. **Headless 测试**：BatchMode/U3DS 不创建 ClientUi、不安装客户端库存 Hook、不解析或实例化 Glazier/Sleek 对象。
13. **性能测试**：placement/preview 热路径和池化图元在 Release 下满足既定 0 GC 门禁；不得以引擎内部不可消除分配冒充整体 0 GC。
14. **静态闭包测试**：单 DLL 运行时引用闭包、Contracts/Core 零 UI/Native/LMN 泄漏、无任意程序集扫描。

### 验证顺序

1. 运行新增 occupancy 红测并保存失败证据。
2. 实现最小 canonical occupancy seam，运行相关绿测。
3. 补齐页面分流和来源排除测试，运行完整测试套件。
4. Release 编译、`git diff --check`、UI/native token 门禁和单 DLL 闭包门禁。
5. 以当前 remediation 增量为冻结基准，重新派发全新的 Standards + Spec 双轴审查。
6. 任一轴 FAIL 时，记录阻断，修复后重新执行红测/绿测/全量验证/双轴审查。
7. 双轴 CLEAN 后，才生成新的 DLL、SHA-256、CandidateBuild、BuildIdentity、LoadSetIdentity 和 CaseId。
8. 新候选产物交给 DEV-16E 采集单人、SteamP2PFriends Host/Client 和 U3DS Headless 证据；不提前宣称玩法完成或发布授权。

## Acceptance Criteria

### A. 占据与算法

- 不再使用 `Items.items[index]` 作为逐格占据来源。
- Preview 与 swap guard 共享同一个 footprint occupancy snapshot。
- 旋转 footprint、同页来源排除、跨容器不排除和未知身份回退均有红绿证据。
- 候选结果与原生 footprint 参考模型一致，自动旋转遵守设置和既定优先级。

### B. 原生渲染与输入

- frame 只挂当前 live `itemsPanel`，icon 只挂当前 live 顶层容器。
- 坐标域和 scroll 只转换一次；UI scale、viewport、dragPivot 和抓取点均有 seam 测试。
- overlay 不改变原生 grid 命中；`Glazier.Root` 不被替换。
- 原生 delegate 可逆、幂等，surface/UI 重建后不残留 stale 引用。

### C. 页面范围与分流

- Backpack、普通 Storage、车辆 Trunk 进入增强拖入路径。
- Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出和未知页面保持原生 Pass-Through。
- 页面覆盖范围不得在本工单中扩大；扩大范围必须另立需求变更。

### D. 提交与隔离

- 合法候选只通过原生 `sendDragItem` 适配路径提交。
- 不新增平行库存 RPC、不改客户端库存权威、不伪造 projection ACK。
- 任何不确定性或异常只隔离 Better Item Interaction，原生拖拽继续可用。
- Headless 不创建 UI 或客户端库存 Hook。

### E. 工程与交付

- Release 编译 0 errors / 0 warnings。
- 全套测试、静态门禁、单 DLL 闭包门禁通过。
- Standards 与 Spec 双轴全新审查均为 CLEAN。
- 新 DLL 具有新的 SHA-256、CandidateBuild、BuildIdentity、LoadSetIdentity 和 CaseId；不得继承 R12 身份。
- DEV-16D 工单在双轴 CLEAN 和新产物生成前不得标记 `resolved`；DEV-16E 仍保持阻塞。

## Out of Scope

- 废弃或重写 DEV-16A、DEV-16B、DEV-16C、DEV-16E。
- 修改 U3-SDK、Unturned 原生源码、BepInEx 或 SteamP2PFriends。
- 替换 `Glazier.Root`、重写原生 `SleekItems` renderer、覆盖 grid 命中层。
- 使用 `Items.items` 线性索引、客户端直接写 `slots[,]` 或建立第二库存数据库。
- 增强 Primary/Secondary、Hands、Vest、Shirt、Pants、AREA、拖出、丢弃、自动整理、自动交换、批量移动或堆叠规则。
- 新增库存网络协议、平行 RPC、LMN 传输或服务端自定义库存写入。
- 运行时扫描/主动加载未被 Chainloader 加载的 DLL。
- 在双轴 CLEAN 前生成供玩家实机测试的新正式 DLL。
- 将静态测试、构建成功、插件无报错或 R12 旧候选证据写成玩法资格或发布授权。

## Further Notes

- 原生机制依据：`research/U3SDK-inventory-rendering-injection-research.md`，来源 U3-SDK commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`。
- 当前状态依据：`snapshots/DEV-16D-implementation-state-freeze-20260901.md` 与 DEV-16D 工单的冻结评论。
- 当前仓库实现基线：BUE HEAD `c1919c4a705304227e68cc6f7decfbe1c839f8d4`；稳定审查基线 `43d05ef91bf61f87dcba38774f78dcc9b4b60bc3`；最近 DEV-16D 实现提交 `b698562f5d3fb4cff56b1eecd665b28a4548269f`。
- R12 候选 `142AC39F769EF23EE75406F3DE84F21B59597B8F1BA72191650B128CF624C5E4` 只作为历史静态候选参考；本规格完成后必须生成新身份。
- `UnturnedPluginManager` 可复用的只是“插件自身 Update 轮询 + Harmony 快速路径 + UI 重建重绑”的生命周期模式；BUE 不依赖其 DLL，也不复制其全局身份。
- 本规格完成后下一步应使用 `/to-tickets` 拆出 occupancy、页面分流/回退、真实图标与验证门禁等独立工单，再按阻塞顺序执行 `/implement` + `/tdd`。
