# BUE 网络 SDK 基线：`SDG.NetTransport` / `ITransportConnection`

> 性质：DEV-V2-01（SDK 网络基线锁定）产物——BUE 网络模块的依赖基线。
> 日期：2026-09-03
> 来源：T1 research（`research/V2-T1-itransportconnection-shape.md`）反编译证据 + 本次刷新。

## 1. 刷新记录

| 项 | 刷新前 | 刷新后 |
|---|---|---|
| `Libs\SDG.NetTransport.dll` SHA-256 | `9B4D27A820C134950018A927B629931C4B1CA2544E89C11FA2694C057FADC006`（mtime 2026/08/11） | `D512DB037B8431701B598CDC801799F4DD5F6C7A9FF48301DFA7D748B0DDB5D3`（mtime 2026/09/01） |
| 游戏安装版 SHA-256 | — | `D512DB037B8431701B598CDC801799F4DD5F6C7A9FF48301DFA7D748B0DDB5D3` |
| 一致性 | 接口一致、二进制不同 | **逐字一致**（复制自 `E:\Steam\...\Unturned_Data\Managed\`） |
| 旧版备份 | — | `Libs\SDG.NetTransport.dll.bak-20260811` |

## 2. 接口成员清单（BUE 依赖基线，固化）

`ITransportConnection : IEquatable<ITransportConnection>`（`SDG.NetTransport` 命名空间，`SDG.NetTransport.dll`）：

```csharp
bool TryGetIPv4Address(out uint address);
bool TryGetPort(out ushort port);
bool TryGetSteamId(out ulong steamId);
System.Net.IPAddress GetAddress();
string GetAddressString(bool withPort);
void CloseConnection();
void Send(byte[] buffer, long size, ENetReliability reliability);
```

关联类型（同程序集）：

```csharp
enum ENetReliability { Reliable, Unreliable }          // 唯一可靠/不可靠表达
interface IClientTransport                              // 客户端→服务器发送（Provider.clientTransport）
interface IServerTransport                              // 服务器接收（产出 ITransportConnection）
abstract class TransportBase
enum ESendType [Obsolete] { RELIABLE, RELIABLE_NODELAY, UNRELIABLE, UNRELIABLE_NODELAY }
```

## 3. 语义要点（T1 查证）

- `ITransportConnection` 是 **Unturned 原生接口**；LMN 是消费者（`using SDG.NetTransport;`）。
- 无连接管理（仅 `CloseConnection()`）；可靠/不可靠仅透传 `ENetReliability` 给 SteamNetworkingSockets send flags。
- 无背压/丢包/已送达反馈；命名/版本握手/可靠 ACK/RPC 全部需 BUE 在接口之上自实现。
- 客户端→服务器方向**不走** `ITransportConnection`：`Provider.clientTransport`（`IClientTransport`，internal，需反射）。
- 服务器端接收 `NetMessages.ReceiveMessageFromClient(ITransportConnection, byte[], int, int)` 参数才是 `ITransportConnection`。

## 4. 演进政策

- 接口演进风险：**删除/改签名成员 → BUE 编译断裂**（硬失败，编译期可见）；**新增成员 → 对纯消费方不破坏编译**，但 `--sdk-net-baseline-red` 只按名/签名锁定既有成员，不检测 absence——新增成员由大版本复核流程人工确认是否纳入基线。
- 本清单随 BUE 依赖基线固化；`Libs\SDG.NetTransport.dll` 与游戏安装版哈希须一致（每次 Unturned 大版本更新后复核）。
- 刷新流程：从游戏安装 `Unturned_Data\Managed\` 复制 → 核验哈希 → 更新本文件刷新记录。

## 5. 验收核对

- [x] `Libs\SDG.NetTransport.dll` 哈希与游戏安装版一致（`D512DB03...`）。
- [x] `ITransportConnection` 成员清单固化（本文件）。
- [x] 旧版备份留档（`bak-20260811`）。
- [x] 构建 0/0；七项目测试 PASS（刷新不破坏现有引用）；`--sdk-net-baseline-red` 绿（含 Send 参数类型绑定）。
