# DEV-16D 实现状态与修复方向冻结报告

> FreezeId: `DEV-16D-FREEZE-20260901-1048`
> 报告类型：文档/工单冻结，不是生产缺陷修复交付。
> 责任变更：原由 Gemini 负责，现由 GPT 接手。

## 一、冻结目的

将 DEV-16D 当前实现、U3-SDK/UnturnedPluginManager 对照结论、阻断项和下一步修复方向写入仓库，供后续 Agent 从同一事实基线继续工作。此次不修改生产源码，不构建，不生成 DLL。

## 二、固定来源

| 来源 | 固定值 |
|---|---|
| 稳定审查基线 | `43d05ef91bf61f87dcba38774f78dcc9b4b60bc3` |
| 当前 BUE HEAD | `c1919c4a705304227e68cc6f7decfbe1c839f8d4` |
| 最近 DEV-16D 实现提交 | `b698562f5d3fb4cff56b1eecd665b28a4548269f` |
| U3-SDK HEAD | `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb` |
| UnturnedPluginManager HEAD | `9b75730a6240c9e1c41d7ddb492b176380d5904c` |
| 原生调研报告 | `.scratch/better-unturned-experience-architecture/research/U3SDK-inventory-rendering-injection-research.md` |
| DEV-16D 工单 | `.scratch/better-unturned-experience-architecture/issues/04-dev-16d-drag-preview-native-projection.md` |

## 三、当前状态

DEV-16D 状态冻结为 `ready-for-agent`，未标记 `resolved`。R12 增量本身已有静态 Standards/Spec CLEAN、Release 构建和 7/7 测试记录，但本次调研发现的既有实现差异仍未关闭；因此 R12 DLL 不能被解释为完整 Better Item Interaction 玩法通过。

## 四、已符合的原生注入 seam

- 真实读取 `SleekItems.horizontalScrollView → grid → itemsPanel` 并校验父链。
- `PlayerUI.Update` 主线程轮询与 Harmony postfix 作为运行时接线入口。
- frame 挂载内容层 `itemsPanel`，floating icon 挂载顶层 `PlayerUI.container`。
- 复用原生 `dragPivot`、公开 `SleekItems.onPlacedItem` delegate，支持可逆 detach。
- `GridContentLocal` 坐标不重复加 scroll；Screen/Viewport 只补偿一次。
- 普通网格沿 `sendDragItem` 提交；装备、AREA、拖出和未知分支原生 Pass-Through。
- UI 重建、关闭、容器切换、代际失配和功能隔离存在清理路径；Headless 不创建 ClientUi。

## 五、阻断项与覆盖记录

### B1：`Items.items` 线性索引不是原生逐格占据

`UnturnedGridOccupancyView.IsOccupied` 以 `y * width + x` 索引 `Items.items`。U3-SDK 的 `Items.items` 是压缩的 `ItemJar` 列表，仅记录左上角；真实占据由 `ItemJar.x/y`、`rot`、资产尺寸和 footprint/`slots[,]` 决定。拖拽适配器又有独立的 footprint 遍历，造成两套事实源；当前也未明确排除拖拽物品自身来源 footprint。

### B2：玩家库存网格覆盖事实（范围记录，非当前实现阻断）

当前 surface 选择固定为 Backpack，并覆盖共享的 Storage/Trunk page 7。U3-SDK 的玩家 Dashboard 创建 page 2～8 的七个 `SleekItems`（Hands、Backpack、Vest、Shirt、Pants、Storage、Area）；Hands、Vest、Shirt、Pants 尚未建立同等增强接线。DEV-16D 规格已明确装备页保持原生 Pass-Through（`spec-DEV-16-runtime-clientui-management-panel.md:141`），因此该项是实现覆盖事实和未来范围入口，不是当前实现阻断；AREA 仍应保持原生回退。

## 六、证据边界

### 已确认

- U3-SDK 原生层级、命中、拖拽、旋转、图标刷新、Storage/Trunk 生命周期和 footprint 语义：源码确认。
- BUE R12 增量：已有红测→绿测、Release 0/0、7/7 测试、静态门禁和增量双轴 CLEAN 记录。

### 尚未确认

- 真实客户端颜色/alpha、z-order、viewport 裁剪、raycast、异步纹理刷新。
- Hands/Vest/Shirt/Pants 全页面接线是否在目标客户端稳定运行。
- G 背包、F 容器、车辆 trunk、地面拖入和旋转行为的真实运行证据。
- SteamP2PFriends Host/Client 与 U3DS Headless 资格证据。

## 七、下一步冻结方向

1. 建立唯一 `ItemJar footprint → occupancy snapshot` 适配器，供 preview 和 native swap guard 共用，并排除当前拖拽物品来源 footprint。
2. 按现有规格保持 Backpack 与 Storage/Trunk 增强、Hands/Vest/Shirt/Pants/AREA Pass-Through；若未来扩大页面范围，先创建独立需求变更和工单。
3. 对需要的真实图标 quality/state 传递补充最小测试，避免扩大非必要范围。
4. 每轮执行红测→最小修复→绿测→Release 编译→全套测试→静态门禁。
5. 每轮重新派发全新的 Standards + Spec 审查；任一阻断项存在时不得生成可测试新 DLL。
6. 双轴 CLEAN 后才重新生成 DLL、SHA-256、CandidateBuild 和 CaseId，再安排人工实机验证。

## 八、禁止事项

- 不修改 U3-SDK/Unturned 原生库存权威，不建立平行库存 RPC。
- 不替换 `Glazier.Root`，不覆盖 grid 命中层，不把 frame/icon 挂到错误层级。
- 不再使用 `Items.items[index]` 作为逐格 occupancy。
- 不继承 R12 的运行 CaseId 或哈希作为修复后证据。
- 不将静态 CLEAN、编译成功或插件无报错写成玩法通过。

## 九、审查与提交说明

本报告仅覆盖本次文档/工单冻结变更。生产源码、测试源码、构建目录和 DLL 均不在变更范围内。提交必须使用白名单，仅包含本报告、冻结快照、仓库交接文档和 DEV-16D 工单变更；工作区其它无关未跟踪文件必须保留。

## 十、给后续 Agent 的入口

先阅读：

1. `snapshots/DEV-16D-implementation-state-freeze-20260901.md`
2. `research/U3SDK-inventory-rendering-injection-research.md`
3. `spec-DEV-16-runtime-clientui-management-panel.md`
4. `issues/04-dev-16d-drag-preview-native-projection.md`

随后按第七节顺序进入新的实现工单；不要直接从 R12 DLL 开始实机测试。
