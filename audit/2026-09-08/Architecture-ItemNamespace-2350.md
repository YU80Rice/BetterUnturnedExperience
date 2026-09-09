# U3-SDK 物品命名空间与 Tab 补全可行性静态调查

- 调查时间：2026-09-08（Asia/Shanghai）
- 基准提交：`ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`
- 性质：只读静态调查；未修改源码、配置或构建产物；未编译、未运行游戏。

## 结论

方向可行，但不能把“创意工坊 ID + 物品 ID”直接替换现有 `ushort`。当前代码已经有 GUID 资产主键的局部基础，但物品实例、地图 `Items.dat`、刷怪表、库存、网络和大量 gameplay helper 仍把 `ushort` 当物品身份。

推荐采用三层身份：

1. 稳定的 `namespace:key`（官方可用 `unturned:*`，Workshop 默认候选可为 `workshop_<publishedFileId>:<item-key>`）。
2. GUID 作为资产加载、编辑器引用和迁移锚点。
3. `ushort legacyId` 只作为旧地图/旧存档/旧网络的兼容输入，不再作为跨模组全局主键。

Tab 补全可做，但应接在 Glazier 字符串字段 seam，并由当前资产映射的不可变目录提供候选；补全不能替代服务端权威解析。

## 1. 冲突根因：legacy ID 未按 Workshop 命名空间隔离

- `Assets/Runtime/Assembly-CSharp/Unturned/Bundles/Asset.cs:27-46`：`Asset` 同时有 `ushort id` 与 `Guid GUID`。
- `Asset.cs:77-80`：同 ID 或 GUID 被替换时设置 `hasBeenReplaced`。
- `Bundles/Assets.cs:177-208`：`AssetMapping` 同时维护 `legacyAssetsTable`（按 `EAssetType` + `ushort`）、GUID 字典和列表。
- `Assets.cs:480-514`：`Assets.find(EAssetType, ushort)` 只按类型和数字查找。
- `Assets.cs:860-925`：同 legacy ID 非覆盖加载会报错并拒绝；覆盖加载会移除旧映射、标记旧资产，再让新资产取得该 ID。
- `Bundles/AssetOrigin.cs:21-40`、`Assets.cs:436-447`：Workshop 来源只有 `workshopFileId` 和名称，未参与 legacy-ID 复合键。
- `Assets.cs:179-182`：源码已明确将该表称为 legacy，并建议新代码不要依赖 16 位 ID。

因此两个 Workshop 物品使用同一数字 ID 时，最终解析取决于资产映射、加载顺序和覆盖策略，足以造成地图刷新或任务道具解析成另一个模组物品。

## 2. 物品刷新表仍是数字 ID，不是 GUID

- `Level/LevelItems.cs:193-232`：`Spawns/Items.dat` 读取条目为 `ushort`，随后调用 `Assets.find(EAssetType.ITEM, item)`。
- `Level/LevelItems.cs:378-410`：保存仍 `writeUInt16(item.item)`。
- `Level/ItemTable.cs:85-118`：`addItem`、`getItem` 均以 `ushort` 工作。
- `Level/ItemSpawn.cs:7-17`：结构只有 `ushort item`。
- `UI/Edit/EditorSpawnsItemsUI.cs:402-417,562-567`：编辑器使用 `ISleekUInt16Field`，添加前按数字 ID 查找。
- `UI/Edit/EditorSpawnsZombiesUI.cs:402-430,618-623`：僵尸服装掉落表也使用 `ushort`。

结论：本仓库存在 GUID 化的其他地图对象路径，但不能据此断言物品刷新表已 GUID 化。

## 3. 迁移断点：不能只改输入框

- `Inventory/Item.cs:77-164`：物品构造函数核心参数是 `ushort`，从 `ItemAsset` 构造也立即降级到 `asset.id`。
- `Tools/ItemTool.cs:213-306`：给予物品和模型入口大量接受 `ushort id`。
- `Managers/ItemManager.cs`、`Inventory/Items.cs`、`VehicleManager`、`InteractableStorage` 等路径存在 `new Item(ushort)`、`Item.id` 和 `read/writeUInt16`。
- 容器、车辆箱、世界掉落、资源/动物/僵尸奖励都可能把数字 ID 写入存档或网络状态；只改编辑器会留下运行时碰撞。

已有可复用 seam：

- `Bundles/CachingBcAssetRef.cs:7-13,48-65,225-291` 支持 GUID 与 legacy ID，但不支持 namespace/key。
- `Assets.cs:735-744` 的 `FindItemByGuidOrLegacyId<T>` 可作为新解析器适配器基础。
- `ModHooks/ItemSpawner.cs:12,35-75` 已接受字符串 `DefaultAsset` 并解析 GUID/legacy，是最接近新字符串引用的现成入口。

## 4. 推荐架构

### 4.1 `ItemIdentifier` 深模块

小接口建议为 `TryParse(string)`、`TryResolve(out ItemAsset)`、`ToCanonicalString()`。内部封装 namespace/key、GUID、legacy fallback、歧义和错误分类；调用者不应自行拼 Workshop ID 或访问 `legacyAssetsTable`。

### 4.2 `ItemIdentityCatalog` / `ItemResolver`

资产加载完成时构建不可变目录：`namespace:key -> ItemAsset/GUID`、`GUID -> canonical name`、`legacy ushort -> 候选列表`。同 namespace:key、GUID 或声明所有权冲突必须 fail-closed，不能由加载顺序静默覆盖。

### 4.3 地图格式迁移

新增版本化 Items 格式，保存 canonical `namespace:key` 与 GUID（最好双写）。读取顺序为新引用 -> GUID -> 旧 `ushort`。旧数字若对应多个 Workshop 候选，必须拒绝自动选择并提示地图作者修复；迁移需备份和可回滚。

### 4.4 实例、存档和网络

逻辑层使用不可变 `ItemIdentity`/`ItemAsset` 引用；连接建立或资产清单阶段协商 identity 到 endpoint-local compact handle，网络只传协商后的短句柄。句柄不能跨连接、跨模组集合复用；服务端保持权威校验。存档使用 canonical identity/GUID，旧数字只作为迁移输入。

## 5. Tab 补全

`GlazierStringField_IMGUI.cs`、`GlazierStringField_UIToolkit.cs`、`Glazier_uGUI/GlazierStringField_uGUI.cs` 都实现 `ISleekField`、`OnTextChanged` 和现有 Return/Escape 处理，因此可以新增可选的 `ISleekCompletionField`，或在编辑器模块用包装模块监听键盘事件。

候选来自 `ItemIdentityCatalog`，按 canonical ID、物品名和来源过滤，并显示 `namespace:key — FriendlyName — GUID/Workshop 来源`。Tab 接受唯一候选；多候选应循环/展开列表，不能静默选第一项。Enter/添加必须再次走同一个解析 seam；无效、缺失、pro 或歧义都应显示错误。旧数字输入可短期保留并标记为 legacy。

## 6. 分阶段路线

1. Phase 0：建立 namespace/key 声明、冲突诊断和只读 catalog，不改存档/网络。
2. Phase 1：实现三路 `ItemIdentifier` 解析，先接入 `ItemSpawner`、编辑器预览和命令。
3. Phase 2：Items.dat 新版本双写稳定引用，旧格式只读迁移并阻断歧义。
4. Phase 3：改造库存、掉落、容器、配方/任务和存档，保留版本化迁移。
5. Phase 4：资产清单/能力协商后分配 endpoint-local handle，覆盖 SP、listen-host、U3DS、Steam P2P。
6. Phase 5：接入 Glazier 三后端 Tab 补全和焦点/键盘测试。

## 7. 风险与边界

- 仅把 `ushort` 改成 `string` 不足以修复冲突；所有查找、序列化、网络和逻辑比较必须经过新身份模块。
- Workshop file ID 可作为默认 namespace 候选，但不是完整作者所有权或包内 key；展示名、文件夹名和临时路径不可靠。
- GUID 能避免当前资产表碰撞，但本身不提供可读命令、作者声明或完整迁移语义。
- Tab 补全只是输入体验；服务端仍需校验 namespace/key、GUID、来源和权限。
- 本调查未验证 Steam 元数据是否始终提供作者级稳定命名空间，也未运行游戏验证网络/存档字段；这些必须作为后续设计与运行验收门禁。

## 审计门禁记录

- 源码/配置修改：无。
- 编译：未执行（只读调查）。
- 运行验证：未执行。
- 独立审核：报告冻结后执行 Standards 与 Spec 双轴审查。
