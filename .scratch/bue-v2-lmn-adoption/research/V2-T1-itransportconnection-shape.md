# V2-T1: ITransportConnection 真实形态查证

- 调查对象：`ITransportConnection` 的真实存在位置、成员表面、LMN 消费关系、BUE 接管网络层的可行性。
- 调查性质：只读查证，无任何代码改动。
- 结论可信度评估：(A) 已在本地主源（游戏程序集 + LMN 源码）直接取证；(B) 通过游戏程序集反编译获取，为主源；（C）待格审确认。

---

## 结论

（核心答案）

**`ITransportConnection` 是 Unturned 原生（vanilla）的传输连接接口，不是 LMN 定义的类型。** 它定义在游戏自带的 `SDG.NetTransport.dll` 程序集里，命名空间为 `SDG.NetTransport`，与 Unturned 本体程序集 `Assembly-CSharp.dll`（`SDG.Unturned` 命名空间）协同运作。LMN（LaunchMultiplayerNet V5）是它的**消费者**，只 `using SDG.NetTransport;` 编译期引用、运行时通过 `SteamPlayer.transportConnection`（public 属性）或 `NetReflectionHelper` 拿到该接口实例来收发数据；LMN 从不定义或实现 `ITransportConnection`。因此 LMN 的 `NamespacedTransport` / V2 命名频道、以及 V1 数字频道，全部是**构建在 `ITransportConnection.Send(...)` 之上的应用层协议封装**（魔数帧 + GUID 路由 + LMN_HELLO/LMN_ACK 频道版本握手），接口本身并不提供频道命名、版本握手或可靠发送语义——这些由 LMN 在自己的协议层实现。若 BUE 要在 `ITransportConnection` 之上自建网络层，它暴露的能力很薄：只有 4 个查询方法（IPv4 / 端口 / SteamID / 地址字符串）、`Send(buffer, size, ENetReliability)`、`CloseConnection()`，以及 `IEquatable<ITransportConnection>` 的相等/哈希；可靠/不可靠只靠一个两值枚举 `ENetReliability` 标注，实际的可靠投递、缓冲、背压、重传全部依赖底层 SteamNetworkingSockets 后端，接口本身不承诺任何交付语义。可靠性、频道版本化、命名、握手都必须在 `ITransportConnection` 之上由上层自己实现——这正是 LMN 已经做的事。

### 一图定位

```
Unturned 进程内
  游戏本体
    Assembly-CSharp.dll (SDG.Unturned)
      Provider[clientTransport: IClientTransport]      ← 客户端→服务器发送链路
      NetMessages.ReceiveMessageFromClient/FromServer  ← 接收分发（internal）
      SteamConnectedClientBase.transportConnection     ← public 属性，类型 ITransportConnection（SteamPending.cs:73）
      SteamPlayer : SteamConnectedClientBase
        每个连接一个 ITransportConnection 实例
      (实现类) SDG.NetTransport.SteamNetworkingSockets.TransportConnection_SteamNetworkingSockets
               SDG.NetTransport.SteamNetworking.TransportConnection_SteamNetworking   (struct, 旧 SteamNetworking 通道)
               SDG.NetTransport.SystemSockets.TransportConnection_SystemSocket
               SDG.NetTransport.Loopback.TransportConnection_Loopback                 (struct, 本地回环)

    SDG.NetTransport.dll (命名空间 SDG.NetTransport)
      interface ITransportConnection : IEquatable<ITransportConnection>   ←【本题对象】
      enum ENetReliability { Reliable, Unreliable }
      enum ESendType [Obsolete] { RELIABLE, RELIABLE_NODELAY, UNRELIABLE, UNRELIABLE_NODELAY }
      interface IClientTransport   (客户端传输, Provider.clientTransport 的类型)
      interface IServerTransport   (服务器传输, 负责 Receive 并输出 ITransportConnection)
      abstract class TransportBase
      class ClientTransport_Null

  LMN 插件 (BepInEx, LaunchMultiplayerNet.dll)
    路由层 ModTransport / NamespacedTransport / ModRouter
      → 编译期 using SDG.NetTransport; 引用 ITransportConnection
      → 发送: transport.Send(packet, len, reliable)
      → 接收: Harmony Prefix 拦截 NetMessages.ReceiveMessageFromClient/FromServer,
            参数签名 ITransportConnection transportConnection（服务器端起 4 元组 Prefix）
      → 在 ITransportConnection 之上实现魔数帧路由 + 频道版本握手
```

---

## 证据

### 证据 1 — 接口定义（游戏安装程序集反编译，主源 A）

文件：`E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\SDG.NetTransport.dll`
用 ilspycmd 反编译 `SDG.NetTransport.ITransportConnection`：

```csharp
using System;
using System.Net;

namespace SDG.NetTransport;

public interface ITransportConnection : IEquatable<ITransportConnection>
{
    bool TryGetIPv4Address(out uint address);
    bool TryGetPort(out ushort port);
    bool TryGetSteamId(out ulong steamId);
    IPAddress GetAddress();
    string GetAddressString(bool withPort);
    void CloseConnection();
    void Send(byte[] buffer, long size, ENetReliability reliability);
}
```

### 证据 2 — 接口所在程序集与命名空间（主源 A）

- 程序集名：`SDG.NetTransport`（`SDG.NetTransport.dll`，len=15072，游戏安装版 mtime 2026/09/01）。
- 命名空间：`SDG.NetTransport`。
- 该程序集全部类型（Mono.Cecil 枚举，确认无嵌套遗漏）：

  ```
  SDG.NetTransport.ClientTransportReady             (delegate, 事件回调)
  SDG.NetTransport.ClientTransportFailure            (delegate)
  SDG.NetTransport.IClientTransport                  (interface)
  SDG.NetTransport.ClientTransport_Null              (class)
  SDG.NetTransport.ESendType                         (enum, [Obsolete])
  SDG.NetTransport.ENetReliability                   (enum)
  SDG.NetTransport.ServerTransportConnectionFailureCallback (delegate)
  SDG.NetTransport.IServerTransport                  (interface)
  SDG.NetTransport.TransportBase                     (abstract class)
  SDG.NetTransport.ITransportConnection              (interface)  ← 本题对象
  ```

`ESendType` 已标 `[Obsolete]`，当前发送路径只用 `ENetReliability`。

### 证据 3 — 关系接口（主源 A）

`IClientTransport`（客户端→服务器发送链路，`Provider.clientTransport` 的类型）：
```csharp
void Initialize(ClientTransportReady cb, ClientTransportFailure err);
void TearDown();
bool Receive(byte[] buffer, out long size);          // 客户端接收
void Send(byte[] buffer, long size, ENetReliability r);
bool TryGetIPv4Address(out IPv4Address address);     // 用 Unturned.SystemEx.IPv4Address
bool TryGetConnectionPort(out ushort port);
bool TryGetQueryPort(out ushort queryPort);
bool TryGetPing(out int pingMs);
```

`IServerTransport`（服务器接收，产出一个 `ITransportConnection`）：
```csharp
void Initialize(ServerTransportConnectionFailureCallback cb);
void TearDown();
bool Receive(byte[] buffer, out long size, out ITransportConnection transportConnection);
```

`ENetReliability { Reliable, Unreliable }` —— **仅两值**，是接口层唯一的可靠/不可靠表达。
`UNRELIABLE_NODELAY` 等旧四态在 `ESendType`（`[Obsolete]`）。

### 证据 4 — 具体实现类（主源 A，游戏安装 Assembly-CSharp.dll）

Mono.Cecil 扫描 Assembly-CSharp.dll，直接实现 `ITransportConnection` 的类：

```
SDG.NetTransport.SystemSockets.TransportConnection_SystemSocket
SDG.NetTransport.SteamNetworkingSockets.TransportConnection_SteamNetworkingSockets
SDG.NetTransport.SteamNetworking.TransportConnection_SteamNetworking        (struct)
SDG.NetTransport.Loopback.TransportConnection_Loopback                       (struct)
```
另：`SDG.Unturned.PooledTransportConnectionList : System.Collections.Generic.List<SDG.NetTransport.ITransportConnection>`

`TransportConnection_SteamNetworkingSockets`（现代 U3-SDK 后端）反编译要点：
- 字段 `internal HSteamNetConnection steamConnectionHandle;`、`internal SteamNetworkingIdentity steamIdentity;`、`internal bool wasClosed;`。
- `Send(...)` 实现为 `SteamGameServerNetworkingSockets.SendMessageToConnection(hConn, ..., nSendFlags由ReliabilityToSendFlags(reliability)算出)` —— **可靠/不可靠真正落在 Steam 底层 send flags**，接口本身只是把标志透传下去，不做排队/背压。
- `Equals/hashCode` 基于 `steamConnectionHandle`（同一连接可比对）。

### 证据 5 — `SteamPlayer` 持有它（主源 A + LMN 注释）

- Mono.Cecil：`SDG.Unturned.SteamConnectedClientBase.transportConnection : SDG.NetTransport.ITransportConnection`（public 属性，SteamPlayer 从该父类继承）。
- LMN 源码注释 `Core\NetReflectionHelper.cs:17-18`：`SteamPlayer.transportConnection 是 public 属性（定义在父类 SteamConnectedClientBase，SteamPending.cs:73），mod 可直接编译期访问，无需反射。`（NetReflectionHelper.cs:68-72 再次确认 `GetSteamPlayerTransport(sp) => sp.transportConnection`）。

### 证据 6 — LMN 是消费者，不是实现者（主源 B）

LMN 源码（`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\`）：

- `Core\LaunchMultiplayerNetPlugin.cs:17` — `V5 架构：vanilla ITransportConnection + 双协议路由`
- `Routing\ModTransport.cs:6` — `using SDG.NetTransport;`（编译期引用原生类型）
- `Routing\ModTransport.cs:714-726` `SendViaTransport(ITransportConnection, int virtualChannel, byte[] payload, bool reliable)`：
  ```csharp
  byte[] packet = ModRouter.BuildModPacket(virtualChannel, payload);
  ENetReliability reliability = reliable ? ENetReliability.Reliable : ENetReliability.Unreliable;
  transport.Send(packet, packet.Length, reliability);
  ```
- `Routing\ModTransport.cs:728-743` `FindClientTransport(CSteamID)`：遍历 `Provider.clients` 用 `NetReflectionHelper.GetSteamPlayerTransport(sp)` 拿到目标客户端的 `ITransportConnection`。
- `Routing\NamespacedTransport.cs:402-413` `SendViaTransport(ITransportConnection, pluginGuid, byte[] payload, bool reliable)`：同一模式，把 pluginGuid 包成帧再 `transport.Send(...)`。
- `Sessions\ConnectionSession.cs:20` — `public ITransportConnection Connection { get; }`；`ConnectionSessionManager.cs:19-20` 用 `Dictionary<ITransportConnection, ConnectionSession>` 以连接为 key 存会话。
- `Patches\NetMessagesReceiveClientPatch.cs:56` — Harmony Prefix 签名 `(ITransportConnection transportConnection, byte[] packet, int offset, int size)`，拦截服务器端起 `NetMessages.ReceiveMessageFromClient`，命中 mod 帧则 `return false` 跳过 vanilla。
- 客户端→服务器发送链路不是 `ITransportConnection` 而是 `IClientTransport`：`NetReflectionHelper.GetClientTransport()` 反射 `Provider.clientTransport`（`Provider` 来自 `SDG.Unturned`），见 `Routing\ModTransport.cs:436-437`、`Routing\NamespacedTransport.cs:183`。

LMN 程序集引用确认（Mono.Cecil 读 `Libs\LaunchMultiplayerNet.dll` 的 AssemblyReferences）：
`mscorlib, BepInEx, 0Harmony, com.rlabrecque.steamworks.net, SDG.NetTransport, Assembly-CSharp, UnityEngine.CoreModule, System` —— **直接引用 `SDG.NetTransport` 与 `Assembly-CSharp`**，且 `LaunchMultiplayerNet.csproj` 用 `HintPath ..\Libs\SDG.NetTransport.dll` 编译期绑定（csproj 主源 B）。LMN 从未定义 `ITransportConnection` 或其他 `SDG.NetTransport` 类型。

### 证据 7 — 频道版本握手是 LMN 协议层，非接口能力（主源 B）

位置：`Protocol\HandshakeProtocol.cs`
- `ControlChannelId = "lmn.control.handshake"`
- `enum EHandshakeType : byte { Hello = 1, Ack = 2 }`
- `BuildHelloPacket(IDictionary<string,int> channels)`：写 `channelId(string) → version(int)` 列表；`ProcessServerHello` 求交集后返回 `Ack`。会话层 `GetChannelVersion(id)` 未知频道返回 0。
- 测试 `LaunchMultiplayerNet.Tests\HandshakeProtocolTests.cs:72-131` 与 `SessionLifecycleTests.cs:127-131` 确认版本交集语义（客户端 `com.mod.tidy → v1`、`com.mod.reload → v2`）。

→ `ITransportConnection` 自身**没有**任何频道/命名/版本成员；Hello/Ack 握手和命名路由全部由 LMN 在预览之上编码进 `byte[]` 载荷实现。（`ESendType` 里的 `RELIABLE_NODELAY` 才是与“立即发送”相关，但它 `[Obsolete]` 且不涉及版本。）

### 证据 8 — 仓库复制与游戏安装版本一致（主源 A，交叉核验）

- 仓库 SDK 参考集 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\SDG.NetTransport.dll`（mtime 2026/08/11）与游戏安装 `...\Unturned_Data\Managed\SDG.NetTransport.dll`（mtime 2026/09/01）：
  - 长度相同（均 15072 字节）；
  - **类型清单完全相同**（无 game-only / repo-only 差异）；
  - **`ITransportConnection` / `IClientTransport` / `IServerTransport` / 两枚举成员表面逐字相同**；
  - 但 SHA-256 不同（`SAME_HASH: False`）→ 二进制不同，推测仅构建元数据戳差异。
- 结论：LMN 编译所用 SDK 副本与最新游戏二进制在接口层面兼容，接口近期稳定（至少自 08-11 至 09-01 无成员变更），BUE 按接口编程不会因小版本漂移破裂。但仍建议发布前用最新游戏 DLL 复核该 DLL 哈希（见待确认 T4）。

---

## 待确认（供格审/gilling ticket）

- **T1 — 接收面所有权边界**：服务器端接收，LMN 是通过 Harmony Prefix 拦截 `NetMessages.ReceiveMessageFromClient`（参数即 `ITransportConnection`）实现双协议分发。若 BUE 接管网络层，是否要**替换掉 LMN 拦截点**、还是叠加在 `ModRouter.TryHandleFromClient` 之上？判断点：BUE 是否仍需要与 vanilla/LMN 共存（同时跑其他 mod）决定采用 prefix 叠加 vs 独占 prefix。
- **T2 — 客户端发送走 `IClientTransport` 而非 `ITransportConnection`**：BUE 若“在 `ITransportConnection` 之上自建网络层”，客户端到服务器方向并不经由 `ITransportConnection`，而是 `Provider.clientTransport`（`IClientTransport`，需要反射或 InternalsVisibleTo）。这一不对称是否纳入 BUE 设计/验收（“网络层”必须是双向但两条链路接口不同）？
- **T3 — 可靠性的灰色地带**：接口只把 `ENetReliability` 透传给 SteamNetworkingSockets 的 `nSendFlags`。`Reliable` 的实际保障（重传/顺序/背压）完全由 Steam 底层负责，`ITransportConnection` 不暴露“已送达/积压/丢包”反馈。BUE 自建层若要感知交付失败，需靠应用层超时或 LMN 的 RPC 语义，接口本身提供不了。是否接受该限制、还是需要引入应用层 ACK？
- **T4 — SDK 校验基线**：仓库 `Libs\SDG.NetTransport.dll` 与最新游戏 DLL 哈希不同（但接口一致）。格审时可确认是否要把 `Libs` 拷贝刷新到与游戏安装一致，从而消除二进制漂移，并记录一份接口成员基线供 BUE 依赖锁定。
- **T5 — 版本演进暴露面**：T1 查证的是语义绑定（using SDG.NetTransport 引用的就是那个接口）。若未来 Unturned 改动 `ITransportConnection`（新增成员/改签名），对 BUE 的影响是编译期断裂而非行为静默改变；是否需要把接口成员清单作为 BUE 的依赖文档锁定（可在 T4 基线里固化）。

---

## 附录 — 调查路径与方法

- 主源 A（游戏安装程序集）：`E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\{SDG.NetTransport.dll, Assembly-CSharp.dll}`，用 `ilspycmd`（`C:\Users\The New Age\.dotnet\tools\ilspycmd.exe`）反编译单个类型 + 用仓库自带 `Libs\Mono.Cecil.dll` 枚举类型/字段/属性/方法签名。
- 主源 B（LMN V5）：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\`（路由层 `Routing\{ModTransport, NamespacedTransport, ModRouter}.cs`；`Patches\NetMessagesReceive{Client,Server}Patch.cs`；`Sessions\{ConnectionSession, ConnectionSessionManager}.cs`；`Protocol\HandshakeProtocol.cs`）。
- 仓库 SDK 参考集：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\`（`SDG.NetTransport.dll`、`Assembly-CSharp.dll`、`LaunchMultiplayerNet.dll`、`Mono.Cecil.dll` 等）。
- 注：`启动器\UnturnedModManager\` 与 `启动器\LaunchMultiplayerNet\` 两条路径均不存在该工程；实际 LMN 源码在根级 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\`。
- 本报告为纯只读查证，未改动任何源码；未关闭 ticket。