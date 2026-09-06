# GPT-RT-04：库存原生权威链与容器状态研究

> 作者：GPT  
> Ticket：RT-04  
> Initial SourceSetId：`BUE-SS-20260824-01`  
> Migrated SourceSetId：`BUE-SS-20260824-02`（2026-08-24；U3-SDK commit 与库存 anchors 未变化，原生权威结论重验无影响）  
> 研究状态：resolved；独立审计 PASS；Gemini 消费复核 ACCEPT；SourceSet successor 迁移 PASS  
> 证据边界：本文只有 `SOURCE_CONFIRMED` 与 `UNRESOLVED`；没有生产实现、候选 DLL、IL、构建、运行或发布 PASS

## 1. 结论先行

1. V1 应继续把 `PlayerInventory.sendDragItem(page0,x0,y0,page1,x1,y1,rot1)` 作为唯一原生提交入口。它把请求以 `ENetReliability.Unreliable` 发送到 `ReceiveDragItem`；非专服的 authority 走同步 loopback，远端客户端走 transport、服务端 `InvokeMethod`、每玩家限速、生成的 owner check，再进入 `ReceiveDragItem`。`[SOURCE_CONFIRMED: Assets/Runtime/Assembly-CSharp/Unturned/Player/PlayerInventory.cs:L699-L701,L967-L970; Assets/Runtime/Assembly-CSharp/NetInvokable/ServerMethodHandle.cs:L54-L80,L92-L153]`
2. `ReceiveDragItem` 是最终原生规则执行者，但它不是“先完成全部验证、再一次性提交”的事务：如果源或目标是当前装备，`dequip()` 可能在页面、坐标、空间与 asset 验证之前发生；库存移动又依次执行 `removeItem` 和 `addItem`。`[SOURCE_CONFIRMED: PlayerInventory.cs:L701-L720,L722-L784]`
3. 原生 drag 不是可回滚原子事务。`Items.removeItem` 在清空占用格后、从 list 删除前同步调用 `onItemRemoved`，而 `PlayerInventory.onItemRemoved` 又会同步发投影并调用公开 `onInventoryRemoved`；任何观察者都可能看见 remove/add 中间态，异常甚至可能阻断 list 删除。`Items.addItem` 则先写 slots/list，再在 `try/catch` 内调用 `onItemAdded`。`[SOURCE_CONFIRMED: Assets/Runtime/Assembly-CSharp/Unturned/Inventory/Items.cs:L23-L27,L297-L313,L363-L376; PlayerInventory.cs:L1742-L1771]`
4. 因此框架不得把任意功能模块直接挂到 native inventory delegate；必须由框架拥有一个捕获异常、按 generation 失效、仅排队快照的 relay。单独的 remove 或 add 事件都不能被当成“服务端已完成本次 drag”的 ACK。
5. 原生 storage session 只有 `isStoring`、`isStorageTrunk`、`storage` 和当前绑定的 `items[STORAGE]`，没有 session generation。关闭时 page 7 被替换为新的 0×0 `Items`。共享契约的 `ContainerReference.SessionGeneration` 必须由 adapter 派生，只用于拒绝陈旧 UI token，不构成访问授权。`[SOURCE_CONFIRMED: PlayerInventory.cs:L123-L138,L1302-L1333,L1571-L1641]`
6. 远端无效 drag 没有明确拒绝消息：`ReceiveDragItem` 的失败口都是静默 `return`，V1 又不注册平行库存 ACK。因此 `AwaitingProjection` 的 2 秒预算只能结束增强等待，不能推断拒绝；迟到的原生投影仍是事实。
7. 本研究未发现必须修改 RT-01 共享契约的缺口；不提交 Shared Contract Change Request。

## 2. 固定源码身份

仓库根：`D:\Agent-工作目录\U3-SDK`  
Git commit：`ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`  
工作树：tracked files clean；未跟踪 `audit/` 不属于 SourceSet。

| 相对路径 | SHA-256 |
| --- | --- |
| `Assets/Runtime/Assembly-CSharp/Unturned/Player/PlayerInventory.cs` | `8485CBF8D4EC75A35D43A20F4BE8D6B401A58A68017B7EBA0E111D663F890FAC` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Inventory/Items.cs` | `8CEEB962BE413F4858BC904AA06FE6B3B4B05EE1211791F91EE4E847B44E0D22` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Inventory/ItemJar.cs` | `0151BADA82C4A7EEB018294EBBCAB15BC190DD285A0A01C36632719FD65DCD54` |
| `Assets/Runtime/Assembly-CSharp/NetGen/NetInvokable/PlayerInventory_NetMethods.cs` | `B2DDB70F1ED8E8E8751FDD9C9E0E4B8C3E16E53B9CA7426E0A801BB5C982EA04` |
| `Assets/Runtime/Assembly-CSharp/NetInvokable/ServerMethodHandle.cs` | `C1201961CD6D0239C8AFC5C87F41BCA8EBDD4B8EB7DA90C25D67C9D940513779` |
| `Assets/Runtime/Assembly-CSharp/NetInvokable/ServerInstanceMethod.cs` | `E27FC164A31CC1657E4FDCAECB3FA107B7AA6549AA1619019FED768771DE758D` |
| `Assets/Runtime/Assembly-CSharp/NetInvokable/ServerInvocationContext.cs` | `CA0744D278A4A8137CCBDDFA61372AA5BCE89FFD5795BEE1C5AB0B0675EB5E76` |
| `Assets/Runtime/Assembly-CSharp/NetInvokable/NetReflection.cs` | `61E81125516DC7E0EA76F7C1C58DC8FBCBA44A2627B2943ED2F7696916D34C2E` |
| `Assets/Runtime/Assembly-CSharp/NetMessaging/ServerMessageHandler_InvokeMethod.cs` | `569A42BC9F349F79B816DCB1757A78739E1700E4D45225F809A37DC925C93BD7` |
| `Assets/Runtime/Assembly-CSharp/NetMessaging/NetMessages.cs` | `957201E19534E2158259C867F0928A573FE123932ECE72EE8DC30B5B3F9E1AB3` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Provider/Provider.cs` | `3B94E16DB3B06DA39163785BE95FF1824BE64780729F70C245B3D1C0EBCFB646` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Provider/SteamChannel.cs` | `8926BDEAF53B674940F094C8E70D973EF9D64816980087E4119179BBD3522E65` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Interactable/InteractableStorage.cs` | `ACC7B89E2CBC0291EE2D272E669312DDF7D8802D535439C1A412FCF33260ED05` |
| `Assets/Runtime/Assembly-CSharp/Unturned/Interactable/InteractableVehicle.cs` | `23F421E7AF90AADC61F5FCE03FF92D19D160E14D9BBA279E6546D5AB7A42C6EE` |

独立 U3DS `Assembly-CSharp.dll` 仍为 `UNRESOLVED`，所以本文对 U3DS 只复用同一固定 U3-SDK source 的服务器代码路径，不声明 U3DS `IL_CONFIRMED`、二进制兼容或运行 PASS。

## 3. 四环境权威调用链

### 3.1 单人（SP authority）

```text
owner UI / native submission adapter
  → PlayerInventory.sendDragItem(...)
  → SendDragItem.Invoke(..., Unreliable, ...)
  → ServerMethodHandle.SendAndLoopbackIfLocal
  → Provider.isServer == true → InvokeLoopback
  → ServerInvocationContext(origin=Loopback, callingPlayer=Provider.clients[0])
  → generated ReceiveDragItem_Read
  → context.IsOwnerOf(netObj.channel)
  → PlayerInventory.ReceiveDragItem
  → native mutation + local inventory delegates
```

- `sendDragItem` 明确使用 `Unreliable`。`[SOURCE_CONFIRMED: PlayerInventory.cs:L967-L970]`
- 非专服编译中，未连接会丢弃；客户端会走 `clientTransport.Send`；`Provider.isServer` 时直接 loopback。`[SOURCE_CONFIRMED: ServerMethodHandle.cs:L65-L80]`
- loopback 调用者在非专服编译中是 `Provider.clients[0]`，随后同步进入生成 reader。`[SOURCE_CONFIRMED: ServerMethodHandle.cs:L92-L153]`
- 生成 reader 在读取 7 个参数之前解析实例 `NetId`、确认目标类型，并执行 `context.IsOwnerOf(netObj.channel)`；失败会 kick。`[SOURCE_CONFIRMED: PlayerInventory_NetMethods.cs:L10-L35,L119-L130; ServerInvocationContext.cs:L25-L28,L57-L63]`
- loopback 没有经过 `ServerMessageHandler_InvokeMethod` 的远端限速分支，所以源码不能证明 SP drag 被 10 Hz 限速；它仍经过 owner check。`[SOURCE_CONFIRMED: ServerMethodHandle.cs:L92-L153; ServerMessageHandler_InvokeMethod.cs:L51-L73]`

### 3.2 SteamP2PFriends Host 本地玩家（Host loopback）

只要 Host 的运行态满足 `Provider.isServer == true`，调用链与 SP 的 loopback 相同；U3-SDK 根据 server/client role 选择 loopback，而不根据底层 transport 名称选择。`[SOURCE_CONFIRMED: ServerMethodHandle.cs:L65-L80]`

`UNRESOLVED`：本 SourceSet 没有把 SteamP2PFriends 的具体 Candidate DLL/运行配置纳入 RT-04，因此“该产品中的 Host 确实在所有目标启动路径保持 `Provider.isServer` 且 `Provider.clients[0]` 是 owner”必须用 P2P Host 运行日志验证，不能由 U3-SDK 源码替代。

### 3.3 SteamP2PFriends Client → Host authority

```text
P2P Client owner UI
  → sendDragItem → clientTransport.Send(Unreliable)
  → Host Provider.Update → listenServer
  → serverTransport.Receive
  → NetMessages.ReceiveMessageFromClient
  → EServerMessage.InvokeMethod handler
  → transportConnection → Provider.findPlayer(caller)
  → per-player 10 Hz rate gate
  → generated ReceiveDragItem_Read owner check
  → Host PlayerInventory.ReceiveDragItem
  → server remove/add
  → reliable SendItemRemove / SendItemAdd to owner
  → Client Provider.Update → ReceiveMessageFromServer
  → ReceiveItemRemove / ReceiveItemAdd
  → client Items mutation + inventory delegates
```

- 客户端 branch 调用 `Provider.clientTransport.Send(..., reliability)`。`[SOURCE_CONFIRMED: ServerMethodHandle.cs:L65-L76]`
- 服务端在 `Provider.Update` 中同步调用 `listen()`；`listenServer` 循环调用 `serverTransport.Receive` 并把 packet 交给 `NetMessages.ReceiveMessageFromClient`。`[SOURCE_CONFIRMED: Provider.cs:L3545-L3561,L6291-L6315]`
- `NetMessages` 解析 `EServerMessage` 并分派到 `ServerMessageHandler_InvokeMethod.ReadMessage`。`[SOURCE_CONFIRMED: NetMessages.cs:L123-L165,L249-L260]`
- handler 由 transport connection 解析 `SteamPlayer`；没有关联玩家则记 bad packet 并停止。它构造 `Remote` context，然后应用每玩家 rate-limit。`[SOURCE_CONFIRMED: ServerMessageHandler_InvokeMethod.cs:L17-L45,L51-L73]`
- `ReceiveDragItem` 的 `[SteamCall(ONLY_FROM_OWNER, ratelimitHz=10)]` 被 `NetReflection` 编译成 server method 与 `0.1s` 间隔；remote handler 在超限时丢弃，达到配置阈值可 kick。`[SOURCE_CONFIRMED: PlayerInventory.cs:L699-L701; NetReflection.cs:L403-L446; ServerMessageHandler_InvokeMethod.cs:L51-L72]`
- 调用者身份不是 payload 自报，而是 `Provider.findPlayer(transportConnection)`；生成 reader 再比较该 `callingPlayer` 与目标 inventory channel owner。`[SOURCE_CONFIRMED: ServerMessageHandler_InvokeMethod.cs:L32-L45; ServerInvocationContext.cs:L25-L38; PlayerInventory_NetMethods.cs:L21-L34]`
- authority 的 remove/add 投影分别以 `ENetReliability.Reliable` 发送给 owner。`[SOURCE_CONFIRMED: PlayerInventory.cs:L1371-L1379,L1742-L1769]`

### 3.4 U3DS authority

U3DS 进程内没有玩家 UI 起点；正确的服务器起点是：

```text
remote client sendDragItem
  → U3DS serverTransport.Receive
  → NetMessages.ReceiveMessageFromClient
  → InvokeMethod remote rate-limit/caller binding
  → generated owner check
  → ReceiveDragItem validation/mutation
  → reliable remove/add projection to remote owner
```

`[SOURCE_CONFIRMED: Provider.cs:L3545-L3561; NetMessages.cs:L123-L165; ServerMessageHandler_InvokeMethod.cs:L17-L83; PlayerInventory_NetMethods.cs:L10-L34,L119-L130; PlayerInventory.cs:L701-L795]`

禁止把客户端 `sendDragItem` 写成 U3DS 进程内起点。独立 U3DS assembly/reference 与实际 dedicated build 尚未登记，故专服二进制 type intersection、Harmony 可解析性和线程日志均为 `UNRESOLVED`。

## 4. RPC 属性、身份与失败口

| 项目 | 源码事实 | 证据 |
| --- | --- | --- |
| Request reliability | drag request 是 `Unreliable` | `SOURCE_CONFIRMED` `PlayerInventory.cs:L967-L970` |
| Projection reliability | item remove/add 是两个独立 `Reliable` client RPC | `SOURCE_CONFIRMED` `PlayerInventory.cs:L1371-L1379` |
| Ownership | generated reader 要求 transport/loopback caller 等于目标 inventory channel owner；不匹配会 kick | `SOURCE_CONFIRMED` `PlayerInventory_NetMethods.cs:L21-L34`; `ServerInvocationContext.cs:L25-L28,L57-L63` |
| Caller identity | remote 由 `Provider.findPlayer(transportConnection)` 取得；不是 RPC 参数 | `SOURCE_CONFIRMED` `ServerMessageHandler_InvokeMethod.cs:L32-L45` |
| Rate limit | remote 每玩家 10 Hz；过快调用被丢弃，累计到阈值可 kick | `SOURCE_CONFIRMED` `PlayerInventory.cs:L700`; `ServerMessageHandler_InvokeMethod.cs:L51-L72` |
| Loopback rate limit | loopback 直接调用 generated reader，未进入 remote handler，因此没有该 10 Hz gate | `SOURCE_CONFIRMED` `ServerMethodHandle.cs:L92-L153` |
| Packet/method failures | 无 method index、越界 method、无关联玩家、超限、无法读 NetId、目标不存在/类型错误、非 owner 会在 `ReceiveDragItem` 前终止；七个 byte 参数的读取失败提前返回只存在于 editor/development/debug-net-invokable 条件编译中 | `SOURCE_CONFIRMED` `ServerMessageHandler_InvokeMethod.cs:L17-L73`; `PlayerInventory_NetMethods.cs:L1-L119` |
| Native validation failures | `ReceiveDragItem` 全部以静默 `return` 结束，没有返回值和拒绝事件 | `SOURCE_CONFIRMED` `PlayerInventory.cs:L701-L795` |

注意：七个 `ReadUInt8` 的成功检查、错误日志和提前 `return` 整段都受 `UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_NETINVOKABLES` 条件控制；release 编译会忽略这些读取函数的返回值，随后仍可能以默认值或部分值调用 `ReceiveDragItem`，再由 native validation 兜底。不能把开发构建的参数读取提前终止推广到 production client/U3DS；必须用目标二进制 IL 与四环境畸形包测试单独确认。`[SOURCE_CONFIRMED: PlayerInventory_NetMethods.cs:L1-L3,L35-L119]`

## 5. `ReceiveDragItem` 验证与 mutation 矩阵

### 5.1 验证顺序

| 顺序 | 检查/动作 | 失败行为 | 是否可能已发生 mutation | 证据 |
| ---: | --- | --- | --- | --- |
| 0 | generated reader 已完成 NetId、类型与 owner check | return 或 kick | 否 | `SOURCE_CONFIRMED` `PlayerInventory_NetMethods.cs:L10-L34` |
| 1 | 如果 source/target 命中当前 equipment selection，忙则 return，否则 `dequip()` | return | **是：dequip 早于其余验证** | `SOURCE_CONFIRMED` `PlayerInventory.cs:L703-L720` |
| 2 | source page 必须 `< PAGES-1`，source `Items` 非 null | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L722-L730`; page 常量 `L64-L71` |
| 3 | `getIndex(x0,y0)` 找源 index；必须非 255 | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L732-L737`; `Items.cs:L110-L120` |
| 4 | target page 必须 `< PAGES-1`，target `Items` 非 null | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L739-L747` |
| 5 | target count 必须 `< 200` | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L749-L752` |
| 6 | source jar 必须非 null | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L754-L759` |
| 7 | `checkSpaceDrag` 验证 target bounds/occupancy；same-page 可忽略旧 footprint | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L761-L764`; `Items.cs:L474-L525` |
| 8 | `jar.GetAsset()` 必须非 null | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L766-L771`; `ItemJar.cs:L21-L24` |
| 9 | 目标 equipment page 时 asset slot 必须可装备 | return | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L773-L776` |
| 10 | 目标 equipment page 强制 `rot1=0` | 不失败 | 可能已 dequip | `SOURCE_CONFIRMED` `PlayerInventory.cs:L778-L781` |
| 11 | `removeItem(page0,index)` | 无 rollback | 是 | `SOURCE_CONFIRMED` `PlayerInventory.cs:L783`; `Items.cs:L363-L376` |
| 12 | `items[page1].addItem(...)` | 无 rollback | 是 | `SOURCE_CONFIRMED` `PlayerInventory.cs:L784`; `Items.cs:L297-L313` |
| 13 | equipment slot projection | 无 transaction boundary | 是 | `SOURCE_CONFIRMED` `PlayerInventory.cs:L786-L794` |

### 5.2 各验证维度裁定

- **Page**：page 0/1 是 primary/secondary，page 2～6 是 player grids，page 7 是 storage，page 8 `AREA` 被 `page >= PAGES-1` 排除。`[SOURCE_CONFIRMED: PlayerInventory.cs:L64-L79,L722-L747]`
- **Source coordinates/item**：普通 grid 的 `Items.getIndex` 会拒绝越界；equipment page 直接返回 index 0，因此 equipment 的 x/y 不是独立边界校验维度。`[SOURCE_CONFIRMED: Items.cs:L110-L120]`
- **Capacity**：target `getItemCount()` 必须小于 200；同页已含 200 个 item 时，源码也会在 source remove 前拒绝。`[SOURCE_CONFIRMED: PlayerInventory.cs:L749-L752]`
- **Occupancy/bounds**：`checkSpaceDrag` 按新 rotation 奇偶交换 footprint，逐格拒绝越界/占用；same-page 仅允许与旧 footprint 自身重叠。`[SOURCE_CONFIRMED: Items.cs:L474-L525]`
- **Footprint/asset**：用于空间判断的 `jar.size_x/size_y` 在 `ItemJar` 构造时来自 `ItemAsset`；之后又显式拒绝当前 asset 解析失败。`[SOURCE_CONFIRMED: ItemJar.cs:L31-L63; PlayerInventory.cs:L754-L771]`
- **Rotation**：grid placement 只按 `rot % 2` 解释 footprint，源码未把 `rot1` 限定为 0～3；equipment 强制 0。adapter 必须只提交契约合法的 0～3，不能把 native 的宽松输入当作公共契约。`[SOURCE_CONFIRMED: Items.cs:L482-L496; PlayerInventory.cs:L778-L781]`
- **Equipment slots**：`Items.checkSpaceDrag` 对 page `< SLOTS` 只检查空或 same-page，随后 `asset.slot.canEquipInPage(page1)` 决定是否可装备。`[SOURCE_CONFIRMED: Items.cs:L475-L480; PlayerInventory.cs:L773-L781]`
- **Storage access**：`ReceiveDragItem` 没有单独检查 `isStoring` 或 `storage.opener`；访问范围由 owner inventory 当前 page 7 `Items` 绑定决定。关闭 storage 后 page 7 变为 0×0 container，后续空间/索引检查自然失败。`[SOURCE_CONFIRMED: PlayerInventory.cs:L1302-L1333,L1623-L1641]`

### 5.3 原生修改前“最后安全点”

必须区分两个边界：

1. **任何 native side effect 之前**：`ReceiveDragItem` 方法入口。因为 `dequip()` 位于页面/空间等验证之前，不能把 line 782 称为全局最后安全点。`[SOURCE_CONFIRMED: PlayerInventory.cs:L701-L720]`
2. **inventory remove/add 之前**：完成 asset/slot 验证且 equipment target rotation 已规范化后的 line 782；但此前可能已经 dequip。`[SOURCE_CONFIRMED: PlayerInventory.cs:L761-L784]`

框架不能插入“额外验证后自行 remove/add”来模拟原生事务。候选 evaluator 只是前端预览；server 最终仍由原方法完整复验。

## 6. `Items` 空间函数与 V1 no-swap

| 方法 | 源码语义 | V1 使用裁定 |
| --- | --- | --- |
| `checkSpaceEmpty` | equipment 只允许空；grid 按 rotation 奇偶交换宽高，逐格检查边界与 slots | 可用于只读候选 snapshot 对照；不是 server 授权。`SOURCE_CONFIRMED` `Items.cs:L440-L472` |
| `checkSpaceDrag` | 可忽略 same-page 的旧 footprint，其他占用/越界拒绝 | 原生 drag 的最终空间验证。`SOURCE_CONFIRMED` `Items.cs:L474-L525` |
| `checkSpaceSwap` | 判断旧 footprint 是否容纳新 item；equipment 永远 true | **不得由 Better Item Interaction 自动调用**。`SOURCE_CONFIRMED` `Items.cs:L527-L570` |

`PlayerInventory` 只是做 page 范围检查后委托给对应 `Items`。`[SOURCE_CONFIRMED: PlayerInventory.cs:L633-L665]`  
V1 只生成 drag candidate，不生成 swap candidate；若局部或扩大搜索均无空位，取消并保留原位置。

## 7. Mutation、callback 与投影事实

### 7.1 非事务性顺序

成功 drag 的可观察顺序是：

```text
(optional) equipment.dequip
→ source Items.fillSlot(false)
→ source Items.onItemRemoved
   → PlayerInventory.onItemRemoved
      → remote owner SendItemRemove(Reliable), if applicable
      → public onInventoryRemoved
      → incrementUpdateIndex
→ source items.RemoveAt
→ source onStateUpdated → onInventoryStateUpdated
→ target Items.fillSlot(true)
→ target items.Add
→ target Items.onItemAdded (try/catch)
   → PlayerInventory.onItemAdded
      → remote owner SendItemAdd(Reliable), if applicable
      → public onInventoryAdded
      → incrementUpdateIndex
→ target onStateUpdated → onInventoryStateUpdated
```

`[SOURCE_CONFIRMED: Items.cs:L297-L313,L363-L376; PlayerInventory.cs:L1371-L1379,L1742-L1771,L1809-L1813]`

关键风险：

- remove callback 发生时 slots 已清空，但 item 仍在 list；源码注释明确警告该顺序。`[SOURCE_CONFIRMED: Items.cs:L23-L27,L363-L376]`
- `Items.removeItem` 没有像 `addItem` 那样包裹 `onItemRemoved` 的 `try/catch`。不可信订阅者异常可阻断 `items.RemoveAt`，留下 slots/list 不一致；原方法无 rollback。`[SOURCE_CONFIRMED: Items.cs:L297-L313,L363-L376]`
- `onInventoryRemoved` 发生在 remove/add 中间；`onInventoryAdded` 发生在 target slots/list 已写入之后。二者不是事务完成消息。`[SOURCE_CONFIRMED: PlayerInventory.cs:L1742-L1771]`
- source 与 target 的 `Items.onStateUpdated` 都没有 `try/catch`，并同步进入公开 `PlayerInventory.onInventoryStateUpdated`。source 侧异常发生在 item 已从 list 删除之后，可阻断后续 target add；target 侧异常发生在 item 已加入之后，可逃逸并阻断 `ReceiveDragItem` 的后续 equipment slot 刷新。它们同样是不可直接暴露给功能模块的不安全 callback 点。`[SOURCE_CONFIRMED: Items.cs:L313,L376; PlayerInventory.cs:L1809-L1813]`
- remote owner 收到两个独立 reliable RPC。源码确认发送顺序，未提供一个 move transaction id 或完成事件；跨帧 delivery/handler 细节必须运行验证。`[SOURCE_CONFIRMED: PlayerInventory.cs:L1371-L1379,L1742-L1769]`

### 7.2 AwaitingProjection 消费规则

1. drop 后立即隐藏增强图层并进入 `AwaitingProjection`。
2. 禁止以 `onInventoryRemoved` 单独结束为“成功”；它可能是中间态，也可能因异常未完成 remove。
3. framework relay 只安排后续快照，不在 native callback 内调用功能代码或读取“最终”库存。
4. 对 local/host，可在原生调用栈返回后的主线程队列读取 snapshot；对 remote client，应在 remove/add 投影处理后读取并匹配目标 `(page,x,y,rot,item identity)`，同时 generation 必须仍有效。
5. rejection 没有 native reason event；2 秒仅结束视觉等待，不推断失败、不回滚、不弹网络错误。迟到 projection 仍刷新原生事实。
6. view close、storage rebind、role change、module isolation 或新 drag 必须使旧 observation generation 失效。

## 8. Storage/container session 与授权

### 8.1 原生状态机

```text
Closed
  isStoring=false
  items[7]=new Items(STORAGE), size 0×0
       │ openStorage(crate)
       ▼
CrateOpen
  isStoring=true, isStorageTrunk=false
  storage=InteractableStorage
  items[7]=storage.items
       │ close/destroy/death/distance/open another
       ▼
Closed

Closed
       │ openTrunk(trunkItems)
       ▼
TrunkOpen
  isStoring=true, isStorageTrunk=true
  storage=null
  items[7]=vehicle trunkItems
       │ closeTrunk/revoke
       ▼
Closed
```

`[SOURCE_CONFIRMED: PlayerInventory.cs:L123-L138,L1302-L1333,L1536-L1649]`

### 8.2 Crate 授权与失效

- `ReceiveInteractRequest` 从 `ServerInvocationContext.GetPlayer()` 获取 caller，拒绝不可打开、null/dead、trunk 优先、被捕、距离平方 >400、LOS 遮挡、locked/owner/group/busy 失败和 plugin veto；成功才 `player.inventory.openStorage(this)`。`[SOURCE_CONFIRMED: InteractableStorage.cs:L539-L630]`
- `checkStore` 在 `Provider.isServer && !Dedicator.IsDedicatedServer` 时直接 true；否则要求 unlocked/owner/group 且 `!isOpen`。该 branch 对 SteamP2PFriends Host 是否符合期望必须单独运行验证，不能从“SP”注释推广。`[SOURCE_CONFIRMED: InteractableStorage.cs:L326-L334]`
- crate destroy 会 `closeStorageAndNotifyClient()`；远离、死亡、destroy、显式关闭和打开另一 storage 都可使 binding 失效。`[SOURCE_CONFIRMED: InteractableStorage.cs:L490-L531; PlayerInventory.cs:L1536-L1659,L1816-L1819]`
- `openStorage` 把 storage 的同一个 `Items` 实例绑定到 page 7；`closeStorage` 解绑定并用新的空 `Items` 替换。`[SOURCE_CONFIRMED: PlayerInventory.cs:L1571-L1587,L1623-L1641]`

### 8.3 Trunk 授权

`InteractableVehicle.grantTrunkAccess` 只在 server 且 trunk 非空时调用 `player.inventory.openTrunk(trunkItems)`，revoke 则调用 `closeTrunk()`；`openTrunk` 以 `storage=null` 标记 trunk，并绑定传入 `Items`。`[SOURCE_CONFIRMED: InteractableVehicle.cs:L2181-L2194; PlayerInventory.cs:L1593-L1617]`

### 8.4 Framework `SessionGeneration`

原生没有 generation 或 capability token。adapter 应在以下每次 transition 单调递增本地 `SessionGeneration`：

- Closed→CrateOpen / Closed→TrunkOpen；
- 任意 open→Closed；
- open A→open B（即使同为 page 7）；
- role/world/player instance 重建。

提交前必须验证 token 与当前 adapter state 相等；随后仍调用原生 `sendDragItem`，让 authority 以当前 page 7 binding 复验。该 generation **不**证明锁、组、距离、LOS、opener 或服务器授权。

## 9. 线程与排队边界

源码确认 remote 网络读取是在 `Provider.Update → listen → serverTransport.Receive → NetMessages → handler → ReceiveDragItem` 的同步调用栈内；loopback 也在 `sendDragItem → InvokeLoopback → generated reader → ReceiveDragItem` 同步栈内。`[SOURCE_CONFIRMED: Provider.cs:L3545-L3561,L6291-L6315; ServerMethodHandle.cs:L92-L153; NetMessages.cs:L123-L165]`

```text
Unity Provider.Update call stack
  ├─ remote receive → rate gate → native authority mutation
  └─ client receive → native projection mutation

Native delegate callback (middle state; do not run feature code)
  → framework-owned catch-all relay
  → enqueue immutable dirty marker + lifecycle/container generation
  → framework main-thread drain after native stack
  → rebuild bounded read-only snapshot
  → publish process-local projection through owned framework event seam
```

`SOURCE_CONFIRMED` 仅能证明调用栈位于 `Provider.Update`；“具体 SteamP2PFriends transport 是否在任何 worker thread 先触发额外 callback”与 Candidate 中 Harmony/LMN callback 的线程身份是 RT-05/运行采证事项。生产测试必须在 adapter 入口记录 Unity main-thread id，并拒绝 off-thread native access。

## 10. 推荐的内部 backend adapter seam

这些是 Contracts 之外的实现 seam，不新增共享 token，也不是生产代码：

| 内部接口 | 最小职责 | 禁止事项 |
| --- | --- | --- |
| `INativeInventorySubmissionAdapter` | owner/local client 将冻结 candidate 映射为一次 `PlayerInventory.sendDragItem(...)`；提交前检查 drag/container generation | 不调用 `ReceiveDragItem`；不自行 remove/add；不发送 LMN inventory message |
| `INativeInventoryContextReader` | 在主线程读取 page dimensions、item jars、current storage binding，生成 bounded immutable occupancy snapshot，并排除当前 dragged footprint | 不把 `PlayerInventory`/`Items` 泄漏到 Contracts；不把 snapshot 当授权 |
| `INativeInventoryProjectionRelay` | 框架唯一订阅 native delegates；callback 内 catch/隔离、只标 dirty；在安全 drain 点重建 snapshot | 不把任意 feature delegate 直接挂 `onInventoryRemoved`、`onInventoryAdded` 或 `onInventoryStateUpdated`；不在任一 native callback 中读取最终态 |
| `IContainerSessionTracker` | 从 `isStoring/isStorageTrunk/storage/items[7]` transition 派生 generation 与 `ContainerKind` | generation 不授予访问权；不得缓存旧 `Items` 作为可写引用 |
| `INativeInventoryAuthorityDiagnostics` | 在不改变 original 返回/异常的前提下记录受限 diagnostic id、role、generation 与 snapshot hash | 不向 UI暴露原生对象/堆栈；不制造 rejection ACK |

### Harmony 裁定

- **首选：无需 backend Harmony 即可提交。** `sendDragItem` 是 public native API，projection 可通过 framework-owned relay 观察 public delegates。`[SOURCE_CONFIRMED: PlayerInventory.cs:L140-L146,L967-L970]`
- 如后续实现必须区分 authority 调用栈完成，只允许 `ReceiveDragItem` 的只读 Prefix/Postfix/Finalizer 作为内部 diagnostic/排队 seam；patch 必须永远运行 original，不修改参数、不跳过验证、不吞 native exception，且所有功能 callback 延后执行。
- 不把 line 782 的“验证结束”当通用 Prefix seam，因为之前已可能 `dequip`。

## 11. 明确拒绝的方案

| 被拒方案 | 理由 |
| --- | --- |
| Patch `ReceiveDragItem` 并 return false/跳过 original | 绕过 owner、page、space、asset、equipment 与当前 storage binding；建立客户端/插件权威 |
| 直接调用 `ReceiveDragItem` 作为客户端提交 | 绕过 network caller binding、remote rate limit 与 generated owner check；U3DS 进程也不存在 UI 起点 |
| 插件自行 `removeItem` + `addItem` | 重复原生非事务步骤、扩大中间态/异常风险，并创建第二套库存事实 |
| 用 LMN 定义 Commit/Accepted/Rejected inventory RPC | 与 RT-01 冻结的 V1 原生权威路径冲突，造成平行 ACK 与回滚语义 |
| 调用 `ReceiveSwapItem` 或 `checkSpaceSwap` 自动交换 | 产品明确 V1 no-auto-swap；会改变玩家未授权的第二个 item |
| 直接把 feature handler 订阅 `onInventoryRemoved/onInventoryAdded/onInventoryStateUpdated` | remove callback 位于 list 删除前，state callback 也未 catch；模块异常可破坏或中断 native mutation |
| 把 `SessionGeneration` 当 storage 授权 | 它是 adapter 派生的 stale-token 防护，不能证明锁/组/距离/LOS/opener |
| 2 秒无投影即显示“服务器拒绝” | 原生没有 rejection event；Unreliable request、rate drop、延迟和真实 validation reject 无法区分 |

## 12. 稳定错误投影边界

- generated non-owner、malformed packet、rate kick 属于 transport/security diagnostic，不应显示原始 reason 给普通玩家。
- native page/occupancy/asset/equipment/storage 失败没有结构化返回，V1 不伪造精确 rejection reason。
- 前端在 submit 前可显示 evaluator 的 `OutsideGrid`/`Occupied`/`NoCandidate`，但 submit 后只跟随原生 projection；不能把 preview reason 当 server result。
- framework 可记录稳定的内部族，例如 `NativeSubmissionUnavailable`、`StaleContainerContext`、`ProjectionNotObservedWithinVisualBudget`，但后者不是 `PlacementRejected`。若这些名称要进入共享 `FrameworkErrorCode`，必须另提 Shared Contract Change Request；本票不修改契约。

## 13. 四环境运行证据义务

所有运行证据必须来自同一 Candidate DLL SHA-256，并分别记录 CaseId、游戏/U3DS build、BepInEx/SteamP2PFriends/LMN 版本、角色、时间戳与双方日志。

| 角色 | 必测行为 | 必要指纹 |
| --- | --- | --- |
| SP authority | local loopback drag；合法/非法/自动旋转 candidate；storage open/close；disable/isolate 回原版 | branch=`Loopback`、owner check reached、native pre/post snapshot hash、无平行 LMN inventory kind |
| P2P Host local | Host local drag 与 storage；验证实际 `Provider.isServer`/caller owner；验证 10 Hz remote gate 不被误称为 local gate | Host Candidate hash、Host role flags、loopback origin、generation transitions |
| P2P Client + Host authority | Client Unreliable request；Host remote rate/owner/native validate；两个 reliable projection；延迟/丢包下不误报 rejection | 同 CaseId Host/Client logs、双方同 DLL hash、transport connection/calling SteamID 安全摘要、projection sequence |
| U3DS + client | U3DS 从 server transport entry 起；无 client UI type load；storage authority；异常 feature 隔离；迟到 projection | U3DS 与 client 同 DLL hash、独立 U3DS reference identity、server entry/owner/rate/main-thread assertions、无 Glazier token load |

专项测试：

1. 通过 framework-owned relay 的测试替身调用会抛异常的 feature handler，分别覆盖 remove/add/state dirty observation，证明异常被 relay 捕获且不会逃逸到 native delegate；禁止直接向原生 multicast delegate 注入破坏性异常。
2. same-page 和 cross-page drag 都验证 remove/add 中间态不会被发布为最终 snapshot。
3. storage A 关闭后立即打开 B，旧 A generation 的提交必须在 client adapter 拒绝；绕过 adapter 的原生请求仍由 current page 7 binding 复验。
4. remote >10Hz controlled test验证丢弃/kick policy；不得在公共服务器施压，只在授权测试环境执行。
5. request 丢失、projection 延迟超过 2 秒时，UI 只结束增强等待，不显示“拒绝”，迟到 projection 仍应用。
6. Candidate source/assembly 改变后，旧运行证据全部失效并重新采集。

## 14. UNRESOLVED 清单

| 项目 | 原因 | 关闭义务 |
| --- | --- | --- |
| 独立 U3DS `Assembly-CSharp.dll` 与 BepInEx reference | SourceSet 未登记 | RT-04/RT-05 successor SourceSet 登记绝对来源、identity、SHA-256；之后才能做 IL/type-intersection claim |
| SteamP2PFriends Host role flags 与 loopback caller | 本票只固定 U3-SDK，不含 Candidate/runtime | 同 DLL Host 日志记录 `Provider.isServer`、`Dedicator.IsDedicatedServer`、context origin 与 owner identity |
| `checkStore` 非 dedicated server 直接 true 对 P2P Host 的实际策略影响 | source 条件明确，但目标 transport/host mode 未运行验证 | 授权 P2P Case 验证锁、group、isOpen 与多客户端行为；必要时另开产品/SteamP2PFriends issue，不在本票改原版 |
| reliable remove/add 是否总在同帧/严格同序到达 UI | source 只有发送顺序与 reliability enum | 网络延迟/抖动运行测试；adapter 不依赖同帧假设 |
| Unity main-thread identity | source 确认 `Provider.Update` 同步栈，未有 Candidate thread assertion | Candidate adapter 记录启动主线程 id，并在 submit/snapshot/assertion 处验证 |
| native rejection 的精确原因 | API 无结构化结果 | 保持不可知；不得通过超时猜测。除非未来原生 API 提供结果，否则不新增 V1 ACK |
| release 构建参数读取失败行为 | generated reader 的七个 byte read 检查与提前返回受条件编译控制 | 对目标 client/U3DS DLL 做 IL 核对，并在授权隔离环境进行畸形/截断消息测试；不得依赖开发构建日志 |

## 15. Acceptance 对账

| RT-04 验收项 | 结果 |
| --- | --- |
| 四环境 `sendDragItem`/dispatch/`ReceiveDragItem` 链 | PASS（source；P2P/U3DS runtime 义务保留） |
| reliability/ownership/caller/rate/failure | PASS |
| page/item/coordinates/rotation/capacity/occupancy/asset/equipment/storage | PASS |
| mutation 顺序、最后安全点、原子性、callback | PASS；明确结论为非事务/有中间态 |
| `checkSpaceEmpty/Drag/Swap` 与 no-swap | PASS |
| storage session/授权/失效 | PASS；generation 为 adapter 派生 |
| projection/events/thread | PASS（main-thread runtime assertion 保留） |
| 推荐/拒绝 hook | PASS；V1 backend 首选无 Harmony submit hook |
| adapter seam 与四环境证据义务 | PASS |
| 共享契约变更 | 无需变更 |

## 16. 给 Gemini 的消费结论

Gemini 的 `AwaitingProjection` 必须按以下条件实现：

1. release 后立即清增强层；
2. 不把 remove event、2 秒超时或本地 candidate 当成 server rejection/acceptance；
3. 只消费 framework relay 在 native callback 结束后形成的 generation-gated snapshot；
4. 目标 item 投影可确认成功，但无投影只能结束等待，不能解释原因；
5. storage UI close/rebind 必须使旧 `SessionGeneration` 与 `DragGeneration` 同时失效；
6. 迟到的原生 projection 继续刷新 native UI，且不弹“迟到”通知。

以上与 RT-01 冻结的 2.0 秒视觉预算、原生事实源和无推断性回滚完全一致。
