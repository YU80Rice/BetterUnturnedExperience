# V2-T1: ITransportConnection 真实形态查证（U3-SDK 权威源码版）

- 调查对象：`ITransportConnection` 的真实存在位置、成员表面、LMN 消费关系、BUE 接管网络层的可行性。
- 调查性质：只读查证，无任何代码改动。
- 权威主源：**U3-SDK 源码** `D:\Agent-工作目录\U3-SDK`（联网游戏源码）+ **LMN 源码仓库** `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`。游戏安装程序集（`E:\Steam\...\Unturned_Data\Managed\*.dll`）仅作交叉核验，非结论依据。
- 路径勘误：初始任务给的 `启动器\UnturnedModManager\` 与 `启动器\LaunchMultiplayerNet\` 两条路径均不存在该工程；真正权威 LMN 源码在根级 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\`（本文件采用前者即「更正后」的权威路径）。游戏安装可能只有编译产物，`D:\...\U3-SDK` 才是源码。

---

## 结论

**`ITransportConnection` 是 Unturned 原生（vanilla）接口，源码定义在 U3-SDK 的 `Assets\Runtime\SDG.NetTransport\TransportConnection.cs`（程序集 `SDG.NetTransport`，命名空间 `SDG.NetTransport`），不是 LMN 定义的类型。** 它是「服务器 ↔ 单个客户端」之间的一条传输连接的抽象：只暴露 4 个身份/地址查询方法（`TryGetIPv4Address`/`TryGetPort`/`TryGetSteamId`/`GetAddress`/`GetAddressString`）、一个 `Send(byte[], long, ENetReliability)` 发送、一个 `CloseConnection()` 关闭，并继承 `IEquatable<ITransportConnection>` 以支持把连接当字典 key / 集合元素做相等比对。可靠语义只在两值枚举 `ENetReliability{Reliable, Unreliable}` 上表达，且在现代 SteamNetworkingSockets 后端里只是被翻译成 Steam 的 `k_nSteamNetworkingSend_Reliable / _Unreliable` 两个 send flag 透传给底层——**接口本身不提供排队、背压、丢包/已送达反馈、频道命名或版本号**。LMN（V5）是这个接口的**消费者**：它在所有相关源文件里只 `using SDG.NetTransport;`，从不定义或实现 `ITransportConnection`（源码全仓 grep 找不到任何 `interface ITransportConnection` 声明）。LMN 的 V1 数字频道与 V2 命名频道、以及 `LMN_HELLO/LMN_ACK` 频道版本握手，全部是**构建在 `transport.Send(...)` 之上、LMN 自己编码进 `byte[]` 载荷的应用层协议**；命名、版本、可靠确认这些能力是 LMN 自实现，不是 `ITransportConnection` 赋予的。若 BUE 要在 `ITransportConnection` 之上自建网络层，它只能拿到很薄的管道：一条字节流 + 两值可靠标注 + 连接身份查询，其余（命名路由、版本协商、应用层 ACK/RPC、可靠交付反馈）都必须由 BUE 在接口之上自行实现——这正与 LMN 已经实践的路径一致。另注意双向不对称：客户端→服务器方向不经 `ITransportConnection`，而是 `Provider.clientTransport`（`IClientTransport`，Provider.cs:1690，internal，需反射或依赖其 public 接口类型），LMN 已用反射加载（`NetReflectionHelper.cs`）。

---

## 证据（权威源码位置 + 行号）

### 1. 接口定义 — U3-SDK 源码（结论的根证据）

文件：`D:\Agent-工作目录\U3-SDK\Assets\Runtime\SDG.NetTransport\TransportConnection.cs`
- L5 `namespace SDG.NetTransport`
- L10 `public interface ITransportConnection`
- L11-12 `: System.IEquatable<ITransportConnection>`（注释说明：因 `ServerTransport_SteamNetworking` 每条消息返回新的 struct 连接，故需可相等比较）
- L18 `bool TryGetIPv4Address(out uint address);`
- L24 `bool TryGetPort(out ushort port);`
- L32 `bool TryGetSteamId(out ulong steamId);`
- L38 `System.Net.IPAddress GetAddress();`
- L44 `string GetAddressString(bool withPort);`
- L50 `void CloseConnection();`（注释 L48：游戏端依赖实现先把可靠消息 flush 完再 dispose）
- L55 `void Send(byte[] buffer, long size, ENetReliability reliability);`

所属程序集/项目：U3-SDK 的 `SDG.NetTransport.csproj`（产物 `SDG.NetTransport.dll`），asmdemof 见 `Assets\Runtime\SDG.NetTransport\SDG.NetTransport.Runtime.asmdef`。

### 2. 同组类型 — U3-SDK 源码（`Assets\Runtime\SDG.NetTransport\` 目录）

- `SendType.cs`：
  - L10-17 `[System.Obsolete] enum ESendType { RELIABLE, RELIABLE_NODELAY, UNRELIABLE, UNRELIABLE_NODELAY }`（已废弃；L7-8 注释：理想上会变成纯 `unreliable`，消息构建交给上层 transport）
  - L19-23 `enum ENetReliability { Reliable, Unreliable }` —— **可靠性只有两值**
- `ClientTransport.cs`：
  - L15 `interface IClientTransport`（客户端↔专用服务器，`Provider.clientTransport` 的类型）
  - L31 `bool Receive(byte[] buffer, out long size);`（客户端接收）
  - L36 `void Send(byte[] buffer, long size, ENetReliability reliability);`
  - L42/L49/L56/L63 `TryGetIPv4Address(out IPv4Address)`（用 `Unturned.SystemEx.IPv4Address`）/`TryGetConnectionPort`/`TryGetQueryPort`/`TryGetPing(out int)`（传输层 ping）
  - L66-106 `ClientTransport_Null`：空实现占位
- `ServerTransport.cs`：
  - L12 `delegate void ServerTransportConnectionFailureCallback(ITransportConnection, string debugString, bool isError)`
  - L17 `interface IServerTransport`
  - L23 `void Initialize(ServerTransportConnectionFailureCallback)` / L28 `TearDown()` / L34 `bool Receive(byte[] buffer, out long size, out ITransportConnection transportConnection)` —— **服务器接收回合产出一个 `ITransportConnection`**
- `TransportBase.cs`：抽象基类，仅提供 `GetMessageText(key, ...)` 本地化文案辅助。

### 3. 具体实现类 — U3-SDK 源码（既有连接身份等价，又是下层后端的选择面）

`Assets\Runtime\Assembly-CSharp\NetTransport_*\`（全部 `ITransportConnection` 实现）：
- `NetTransport_SteamNetworkingSockets\TransportConnection_SteamNetworkingSockets.cs` —— **现代默认后端**
  - L14 `internal class ... : ITransportConnection`
  - L126-149 `Send(...)` → `SteamGameServerNetworkingSockets.SendMessageToConnection(handle, ...)`；L128 `sendFlags = serverTransport.ReliabilityToSendFlags(reliability)`；L141-148 依据 `result==k_EResultOK` 记日志（错误不抛异常，静默在日志）
  - L121-124 `CloseConnection()` → `serverTransport.CloseConnection(this)`；L151-154 字段 `wasClosed`/`steamConnectionHandle:HSteamNetConnection`/`steamIdentity`/`serverTransport`
  - L96-114 `Equals`/`GetHashCode` 基于 `steamConnectionHandle`
- `NetTransport_SteamNetworkingSockets\ServerTransport_SteamNetworkingSockets.cs` —— 服务器链路
  - L154-203 `Receive(...)` 轮询 poll group，L181 把每个连接包装成 `TransportConnection_SteamNetworkingSockets` 输出
  - L205-225 `CloseConnection(...)` 用 `CloseConnection(hConn, 0, null, bEnableLinger=true)`，标记 `wasClosed` 并移除
  - 连接生命周期状态机 L280-437（Connecting/Connected/ClosedByPeer/ProblemDetectedLocally → 触发 `connectionFailureCallback`）
- `NetTransport_SteamNetworkingSockets\TransportBase_SteamNetworkingSockets.cs`
  - L105-117 **`ReliabilityToSendFlags(ENetReliability)`**：`Reliable → k_nSteamNetworkingSend_Reliable`；`Unreliable → k_nSteamNetworkingSend_Unreliable`。L107 注释：Nagle 默认 5ms，nodelay 暂忽略。
  - L151-158 `clSendBufferSize`（`-SNS_SendBufferSize` 覆盖 `k_ESteamNetworkingConfig_SendBufferSize`）—— **发送缓冲/背压属 Steam 层配置，接口不可见**
  - L169-179 默认超时 `TimeoutInitial/TimeoutConnected = 30s`
- `NetTransport_SteamNetworking\TransportConnection_SteamNetworking.cs` / `ServerTransport_SteamNetworking.cs` —— 旧 SteamNetworking 通道（struct 连接）
- `NetTransport_SystemSockets\TransportConnection_SystemSocket.cs` / `ServerTransport_SystemSockets.cs` —— 纯系统 socket 通道
- `NetTransport_Loopback\TransportConnection_Loopback.cs` —— 本地回环（SP/本机调试）
- `NetTransport_UNetLLAPI\...` —— 遗留 UNet 通道
- 关联工具：`Assets\Runtime\Assembly-CSharp\NetInvokable\TransportConnectionListPool.cs` + `Unturned\Provider\PooledTransportConnectionList`（`GatherClientConnections*` 系列返回连接池，避免为每条消息分配连接列表）。

### 4. 游戏如何持有/分发该接口 — U3-SDK 源码

- `Unturned\Provider\SteamPending.cs`：
  - L17 `public class SteamConnectedClientBase`
  - **L73 `public ITransportConnection transportConnection`** —— 每个客户端连接实例（LMN 注释也标注此处为 public 属性、mod 可编译期直取）
  - L276 / L778 构造时赋值；L195 `NetMessages.SendMessageToClient(EClientMessage.Verify, ENetReliability.Reliable, transportConnection, ...)`
- `Unturned\Provider\SteamPlayer.cs`：L25 `public class SteamPlayer : SteamConnectedClientBase`；L578 `transportConnection.TryGetIPv4Address(...)`、L605 `GetAddress()`、L621 `GetAddressString(...)`、L657 `ToString()`、L786 `IsLocalServerHost = transportConnection != null && !Dedicator.IsDedicatedServer`
- `Unturned\Provider\Provider.cs`：
  - L30 `public ITransportConnection transportConnection;`（服务器发起方把对端连接存于此）
  - L479 `_transportConnectionToPlayerMap : Dictionary<ITransportConnection, SteamPlayer>`；L620 `_transportConnectionToPendingPlayerMap` —— **以接口实例为连接身份 key**
  - L1690 `internal static IClientTransport clientTransport;`；L1695 `private static IServerTransport serverTransport;`
  - L1810-1812 `clientTransport = NetTransportFactory.CreateClientTransport(...); clientTransport.Initialize(...)`
  - L3549 `while (serverTransport.Receive(buffer, out size, out clientId))`（服务器接收循环，`clientId` 即 `ITransportConnection`）
  - L3582 `while (clientTransport.Receive(buffer, out size))`（客户端接收循环）
  - L985-1028 连接身份/限流通过 `TransportConnectionRateLimiter`、`ITransportConnection` 相等与 `TryGetIPv4Address` 做比较

### 5. `NetMessages` 接收签名（LMN 拦截点）— U3-SDK 源码

文件：`Assets\Runtime\Assembly-CSharp\NetMessaging\NetMessages.cs`
- L23 `public delegate void ServerReadHandler(ITransportConnection transportConnection, NetPakReader reader);`
- L25/L50/L79 `SendMessageToClient(s)(EClientMessage, ENetReliability, ITransportConnection | List|IEnumerable<ITransportConnection>, callback)`；L47/L74 `transportConnection.Send(writer.buffer, writer.writeByteIndex, reliability);`
- L110-120 客户端→服务器：`Provider.clientTransport.Send(...)`（**不经 `ITransportConnection`**）
- **L123 `public static void ReceiveMessageFromClient(ITransportConnection transportConnection, byte[] packet, int offset, int size)`** —— 服务器端每包入口，LMN 的 Harmony Prefix 正拦这里
- L167 `public static void ReceiveMessageFromServer(byte[] packet, int offset, int size)` —— 客户端入口（无 `ITransportConnection` 参数）

### 6. LMN 是消费者、不是实现者 — LMN 权威源码仓库

LMN 仓库：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`
- 全仓 `.cs` grep：**不存在 `interface ITransportConnection` 声明**（只有 `using SDG.NetTransport;`），且以下文件 `using SDG.NetTransport;`：`ConnectionSession.cs`、`ConnectionSessionManager.cs`、`ModRouter.cs`、`ModTransport.cs`、`NamespacedTransport.cs`、`NetMessagesReceiveClientPatch.cs`、`NetReflectionHelper.cs`（测试文件亦然）。
- `Core\LaunchMultiplayerNetPlugin.cs:17` — `V5 架构：vanilla ITransportConnection + 双协议路由`
- `Core\NetReflectionHelper.cs:17-18` — 注释：`SteamPlayer.transportConnection 是 public 属性（定义在父类 SteamConnectedClientBase，SteamPending.cs:73）`；L73-85 `GetSteamPlayerTransport(sp) => sp.transportConnection`（与 U3-SDK `SteamPending.cs:73` **逐字吻合**）
- `Routing\ModTransport.cs:714-726` `SendViaTransport(ITransportConnection transport, int virtualChannel, byte[] payload, bool reliable)`：`transport.Send(packet, packet.Length, reliable ? ENetReliability.Reliable : ENetReliability.Unreliable);`
- `Routing\ModTransport.cs:728-743` `FindClientTransport(CSteamID)`：遍历 `Provider.clients` 用 `GetSteamPlayerTransport(sp)` 找目标连接；L554-555 服务器发时 `GetSteamPlayerTransport(sp)`；L436-437 客户端发时用 `NetReflectionHelper.GetClientTransport()`（IClientTransport）
- `Routing\NamespacedTransport.cs:402-413` `SendViaTransport(ITransportConnection, string pluginGuid, byte[] payload, bool reliable)`：包 GUID 帧后 `transport.Send(...)`
- `Sessions\ConnectionSession.cs:20` `public ITransportConnection Connection { get; }`；`ConnectionSessionManager.cs:19-20` `Dictionary<ITransportConnection, ConnectionSession>`
- `Patches\NetMessagesReceiveClientPatch.cs:56` Harmony Prefix 签名 `(ITransportConnection transportConnection, byte[] packet, int offset, int size)`，拦截 `ReceiveMessageFromClient`（U3-SDK `NetMessages.cs:123`），命中 mod 帧 `return false` 跳过 vanilla
- LMN 程序集对 `SDG.NetTransport` 是编译期引用：`LaunchMultiplayerNet.csproj` `HintPath ..\Libs\SDG.NetTransport.dll`；Mono.Cecil 读 `Libs\LaunchMultiplayerNet.dll` 的 AssemblyReferences 含 `SDG.NetTransport` 与 `Assembly-CSharp`

### 7. 频道版本 Hello/Ack 是 LMN 协议层，非接口能力

文件：`Protocol\HandshakeProtocol.cs`（LMN）
- `ControlChannelId = "lmn.control.handshake"`；`enum EHandshakeType : byte { Hello = 1, Ack = 2 }`
- `BuildHelloPacket(IDictionary<string,int> channels)`：序列化 `channelId → int version` 列表；`ProcessServerHello` 求交集回 `Ack`；会话层 `ConnectionSession.GetChannelVersion(id)` 未知频道返回 0
- 测试：`LaunchMultiplayerNet.Tests\HandshakeProtocolTests.cs:72-131`、`SessionLifecycleTests.cs:127-131` 确认版本交集语义（如客户端 `com.mod.tidy → v1`、`com.mod.reload → v2`）
- → 命名频道、版本号、握手全部为 LMN 在 `ITransportConnection.Send` 之上编码进 `byte[]` 的应用层方案；`ITransportConnection` 自身无这些成员。

### 8. 交叉核验（游戏安装程序集 vs U3-SDK 源码，非结论依据）

`E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\{SDG.NetTransport.dll, Assembly-CSharp.dll}` 用 ilspycmd/Mono.Cecil 反编译，其 `ITransportConnection` / `IClientTransport` / `IServerTransport` / 两枚举 / 四个实现类成员表面，与 U3-SDK 源码逐字一致。仓库 SDK 复制 `Libs\SDG.NetTransport.dll`（mtime 2026/08/11）与游戏最新（mtime 2026/09/01）类型清单与接口成员完全相同但 SHA-256 不同（仅构建戳差异）——接口近期稳定。

---

## 待确认（供格审 ticket）

- **T1 接收面所有权边界**：BUE 接管网络层时，是否替换 LMN 拦截的入口（`NetMessages.ReceiveMessageFromClient`，U3-SDK `NetMessages.cs:123`；客户端 `ReceiveMessageFromServer` L167），还是叠加在 `ModRouter.TryHandleFromClient` / `NamespacedTransport` 之上？共存需求决定 prefix 叠加 vs 独占。注意服务器端每包入口参数就是 `ITransportConnection`，客户端入口没有该参数——两端的可观测面不对称。
- **T2 客户端发送方向接口不对称**：BUE「自建网络层」若面向 `ITransportConnection`，客户端→服务器方向实际走 `Provider.clientTransport`（`IClientTransport`，Provider.cs:1690，`internal static` 字段，LMN 靠反射访问）。是否纳入设计/验收，明确逐包校验该 internal 字段获取方式（反射 vs 未来玩法）。
- **T3 可靠性的灰色地带**：`ENetReliability` 仅透传成 Steam `k_nSteamNetworkingSend_Reliable/_Unreliable` 两个 flag（`TransportBase_SteamNetworkingSockets.cs:105-117`）；`Reliable` 的实际重传/顺序/缓冲由 Steam 底层负责，接口不暴露已送达/积压/丢包反馈；发送失败只记日志不抛异常（`TransportConnection_SteamNetworkingSockets.cs:141-148`）。BUE 若要感知交付失败，需应用层超时 / ACK，接口提供不了。
- **T4 连接生命周期观测缺口**：断线通知经由 `IServerTransport` 的 `ServerTransportConnectionFailureCallback(ITransportConnection, debugString, isError)`（`ServerTransport.cs:12`），而 `Provider.clientTransport`/`serverTransport` 是 internal/private 字段；LMN 目前不主动监听 `connectionFailureCallback`（靠 vanilla `SteamConnectedClientBase`/`Provider` 生命周期）。BUE 若要在移除连接的瞬间响应，需确认订阅入口。
- **T5 SDK 依赖基线**：置 `Libs\SDG.NetTransport.dll` 与游戏最新 DLL 哈希漂移（接口一致）。BUE 是否把 `ITransportConnection` 成员清单固化为依赖基线，并确认 U3-SDK 版本（git 提交）与发布二进制对齐。

---

## 附录 — 调查路径与方法

- 结论主源为 **U3-SDK 源码**（`D:\Agent-工作目录\U3-SDK`）：基于 `Assets\Runtime\SDG.NetTransport\`（接口/枚举/客户端服务器抽象）与 `Assets\Runtime\Assembly-CSharp\NetTransport_*\`（各后端实现）+ `Unturned\Provider\{SteamPending,SteamPlayer,Provider}.cs` + `NetMessaging\NetMessages.cs`。
- LMN 主源为 **`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`**（V5）。
- 游戏安装程序集（`E:\Steam\...\Managed\`）与仓库 `Libs\` 复制仅作交叉核验（ilspycmd + Mono.Cecil）。
- 本报告为纯只读查证，未改动任何源码；未关闭 ticket。