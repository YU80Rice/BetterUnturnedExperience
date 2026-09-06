# GPT-07 原版库存移动与多人权威调用链研究

- 作者：GPT
- 日期：2026-08-24
- 研究类型：只读源码研究
- 基线：U3-SDK `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`
- 补充基线：SteamP2PFriends `90f1e3d291d29e54df8668b964669511fe4baca2`

## 结论摘要

原版拖放是“客户端 UI 计算候选位置并发送意图，服务端重新校验并变更权威 `Items`，再以可靠的 remove/add 消息刷新远端客户端”的模型。单人和 SteamP2PFriends 房主因为 `Provider.isServer == true`，同一请求通过 loopback 在本进程服务端路径执行；SteamP2PFriends 客机与 U3DS 客户端把请求发给远端服务端。`ReceiveDragItem` 才是权威写入点，客户端的 `checkSpaceDrag` 只是交互预检，不能视为安全边界。

“更好的物品交互”应只替换/增强客户端候选落点与绿色预览，最终仍调用原版 `sendDragItem`，并保留 `ReceiveDragItem` 的所有服务器校验。首版不得直接写 `Items`、不得在客户端预测 remove/add、不得绕过所有权校验。

## 一、源码确认的调用链

### 1. 客户端抓取与预览

1. `PlayerDashboardInventoryUI.onGrabbedItem` 从本地 `PlayerInventory` 读取 `ItemJar`，保存来源页、坐标、旋转和鼠标偏移，然后显示拖动物品。证据：`PlayerDashboardInventoryUI.cs:1083-1150`。
2. 拖动期间按旋转键仅修改本地 `dragJar.rot` 并刷新视觉。证据：`PlayerDashboardInventoryUI.cs:2418-2437`。
3. 点击目标网格后，UI 把光标/枢轴换算为网格左上角，裁剪至页面边界，并做客户端预检。证据：`PlayerDashboardInventoryUI.cs:1153-1206`。
4. 空位预检通过后，UI 先 `stopDrag()`，再调用 `Player.LocalPlayer.inventory.sendDragItem(...)`。证据：`PlayerDashboardInventoryUI.cs:1248-1255`。
5. 若落点被占用，原版 UI 会尝试 `sendSwapItem`；本项目首版已决定不实现自动交换，因此模糊落点算法应把占用候选判为失败，而不是进入原版交换分支。证据：`PlayerDashboardInventoryUI.cs:1274-1321`。

### 2. 客户端请求到服务端入口

1. `sendDragItem` 使用 `ServerInstanceMethod` 发送 `ReceiveDragItem`，可靠性为 `Unreliable`。证据：`PlayerInventory.cs:699-701,967-969`。
2. 生成的读取器先解析目标 `NetId`，要求调用方拥有该 `PlayerInventory.channel`；非所有者会被踢出，然后才反序列化参数并调用 `ReceiveDragItem`。证据：`PlayerInventory_NetMethods.cs:10-34,35-119`。
3. `ReceiveDragItem` 还声明 `ONLY_FROM_OWNER` 与 10 Hz 限流。证据：`PlayerInventory.cs:699-701`。

### 3. 服务端校验与最终状态变更

`ReceiveDragItem` 依次执行：

1. 若源或目标是当前装备，忙碌则拒绝，否则先卸下。证据：`PlayerInventory.cs:703-720`。
2. 校验源页范围、源页存在、源坐标确有物品。证据：`PlayerInventory.cs:722-737`。
3. 校验目标页范围、目标页存在、目标页物品数少于 200。证据：`PlayerInventory.cs:739-752`。
4. 获取服务端当前 `ItemJar`，校验非空。证据：`PlayerInventory.cs:754-759`。
5. 以服务端当前物品尺寸和旧旋转重新调用 `checkSpaceDrag`，校验边界、占用和同页自重叠。证据：`PlayerInventory.cs:761-764`；底层算法见 `Items.cs:474-524`。
6. 校验资产存在与装备槽兼容；装备槽强制旋转为 0。证据：`PlayerInventory.cs:766-781`。
7. 全部通过后才执行 `removeItem(source)` 与 `addItem(target)`。证据：`PlayerInventory.cs:783-784`。

### 4. 客户端投影更新

1. 服务端 `Items.removeItem/addItem` 触发事件和状态更新。证据：`Items.cs:297-314,363-376`。
2. 对远端拥有者，服务端通过可靠消息发送 item remove/add。证据：`PlayerInventory.cs:1742-1761,1371-1378`。
3. 本地 UI 订阅 `onInventoryAdded/onInventoryRemoved`，据权威 `ItemJar` 增删 `SleekItems` 投影。证据：`PlayerDashboardInventoryUI.cs:2057-2098`。

## 二、容器调用链

容器没有另一套拖放 RPC。服务端打开容器时将 `PlayerInventory.STORAGE` 页绑定到 `InteractableStorage.items`，然后通知客户端；关闭时解除该页。证据：`PlayerInventory.cs:1568-1588,1620-1649`。因此玩家背包与容器之间、容器内部的移动，都进入同一个 `ReceiveDragItem`，目标/来源页为 `STORAGE` 时直接操作当前服务端已授权打开的容器 `Items`。

容器访问本身另有服务端开箱门禁，包括允许打开、当前状态、距离、视线和插件回调等；成功后才 `openStorage`。证据：`InteractableStorage.cs:539-623`。拖放安全依赖“服务器当前 `STORAGE` 页是否仍绑定有效容器”，而不是客户端是否仍显示容器 UI。

## 三、三种环境的权威执行者

| 环境 | 请求路径 | 权威执行者 | 结论级别 |
|---|---|---|---|
| 单人 | UI → `sendDragItem` → server loopback → `ReceiveDragItem` | 本地进程中的原版服务端状态 | 源码确认。`ServerMethodHandle` 在 `Provider.isServer` 时走 `InvokeLoopback`：`ServerMethodHandle.cs:54-80,92-131`。 |
| SteamP2PFriends 房主 | UI → loopback → `ReceiveDragItem`；远端客机变更由房主发回 | 房主客户端内同时运行的原版服务端 | 源码确认其 listen-host 定义：`SteamP2PFriendsPlugin.cs:19-22`；库存具体运行仍需实测。 |
| SteamP2PFriends 客机 | UI → client transport → 房主 `ReceiveDragItem` | 房主客户端内的原版服务端 | 由原版发送分支与 listen-host 架构共同确认；当前哈希运行时未验证。 |
| U3DS | UI → client transport → U3DS `ReceiveDragItem` | U3DS 专用服务器 | 原版网络源码确认；当前项目尚无运行时证据。 |

`ServerMethodHandle.SendAndLoopbackIfLocal` 明确：未连接则忽略；非服务端用 `clientTransport.Send`；服务端走 loopback。证据：`ServerMethodHandle.cs:54-80`。

## 四、失败、回滚与一致性边界

### 源码确认

- 客户端预检失败时不发送请求；拖动可能保持或停止，取决于具体分支，但不会直接改权威 `Items`。
- 服务端任一校验失败都在 remove/add 之前返回，因此权威库存保持原样。这是“无状态变更”，不是显式事务回滚。
- UI 在发送前已经 `stopDrag()`，而请求采用 `Unreliable`。原版没有为 drag RPC 展示显式成功/失败响应；最终一致性依靠服务端权威库存事件/后续同步，而不是客户端提交确认。
- remove/add 不是一个带回滚日志的事务。当前源码在校验完成后顺序执行；`Items.addItem` 本身不重新检查占用，并会捕获 `onItemAdded` 订阅者异常后继续。证据：`Items.cs:297-313`。因此插件不得在两步之间插入可能抛异常或改变库存的逻辑。

### 推断

- 请求丢包或服务端拒绝时，因为客户端没有乐观 remove/add，物品仍在原位置；玩家看到的拖拽视觉结束，但库存投影应保持原样。这需要三环境运行时确认 UI 是否存在短暂残影。
- 若第三方 Harmony 补丁在 `removeItem` 后、`addItem` 前抛异常，可能造成物品丢失；这不是原版正常校验路径，而是扩展造成的原子性破坏风险。

## 五、安全挂接位置

### 推荐

1. **纯计算候选服务**：读取当前 `PlayerInventory`/`Items` 的只读快照，输出候选 `(page,x,y,rot)`、占用矩形与失败原因。不得持有或修改 `ItemJar`。
2. **客户端预览层**：在 `onGrabbedItem` 已确定来源后、`onPlacedItem` 发送前计算模糊候选并渲染绿色占位。候选必须用与服务端兼容的尺寸、旋转和边界语义。
3. **提交层**：只把已选候选交给原版 `sendDragItem`。不要直接调用 `ReceiveDragItem`，不要直接 `removeItem/addItem`，不要伪造 `Provider.isServer` 或 dedicated 状态。
4. **结果观察层**：订阅库存 added/removed/updated 事件，仅用于清理预览、记录诊断或显示失败提示；事件回调不得再次写库存，避免重入。
5. **容器安全**：每帧/提交前确认仍处于 `isStoring` 且 STORAGE 页有效；真正授权仍交由服务端当前绑定与 `ReceiveDragItem` 校验。

### 不推荐/禁止

- 用 Harmony Prefix 跳过 `ReceiveDragItem` 原方法。
- 客户端先移动物品再等待服务端确认。
- 直接调用 `Items.addItem`；该方法不做占用验证。
- 在 `removeItem` 与 `addItem` 之间附加外部逻辑。
- 把 SteamP2PFriends 房主视为普通客户端，或把 `Dedicator.IsDedicatedServer` 当作唯一服务端判断。

## 六、对“更好的物品交互”的契约约束

- 前端可决定光标到候选格的映射、绿色占位表现和自动旋转候选顺序。
- 后端纯算法必须返回确定性候选：优先当前方向，再尝试旋转；不自动交换；无候选则取消并保留原位。
- 最终提交必须复用原版 `sendDragItem` 参数与服务端校验。
- 预览结果是建议，不是授权；服务端拒绝必须被视为正常竞态结果（例如容器关闭、其他状态已变化）。
- 首版应为拖放请求增加本地关联 ID/时间戳日志，但不得改变原版 RPC 负载；通过后续权威 inventory events 关联观察结果。

## 七、运行时未验证项与验收建议

以下不能由静态源码替代：

1. 单人、SteamP2PFriends 房主、SteamP2PFriends 客机、U3DS 客户端各自实际命中的 thread/loopback/transport 路径。
2. `Unreliable` drag 请求丢失或被拒绝后的 UI 视觉恢复时间与是否出现残影。
3. 同时拖动、容器关闭、远程容器内容变化时的竞态表现。
4. 当前游戏版本 DLL 是否与 U3-SDK 基线完全一致。

建议用同一构建哈希分别采集：来源/候选/旋转、`sendDragItem`、服务端 `ReceiveDragItem` 前后库存快照哈希、added/removed 事件、最终两端库存快照；每个 Case ID 覆盖成功、无空间、自动旋转成功、容器中途关闭、并发占位五类场景。

## 证据边界

- “源码确认”仅指上述两个固定提交中的源文件。
- SteamP2PFriends 对库存 RPC 没有发现专用替代实现；因此其库存权威沿用原版服务端模型。这一结论是源码结构确认，但仍需实际房主/客机日志验证部署行为。
- 本研究未修改生产代码、未编译、未执行游戏运行时测试。
