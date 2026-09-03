# DEV-V2-01：SDK 网络基线锁定

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: 无（先行票，可立即开始）
Spec: `../spec-V2-phase1-lmn-adoption.md`（实施依赖清单第 1 项）

## Scope

把 BUE 网络模块的 SDK 依赖基线锁死，消除 `Libs\SDG.NetTransport.dll` 与游戏安装二进制的漂移。

- 用游戏安装版（`E:\Steam\...\Unturned_Data\Managed\SDG.NetTransport.dll`，mtime 2026/09/01）刷新仓库 `Libs\SDG.NetTransport.dll`（当前 mtime 2026/08/11，接口一致但哈希不同）。
- 固化 `ITransportConnection` 成员清单为 BUE 依赖基线文档（T1 research 已反编译：`TryGetIPv4Address/TryGetPort/TryGetSteamId/GetAddress/GetAddressString/Send(buffer,size,ENetReliability)/CloseConnection` + `IEquatable`），写入 `.scratch/bue-v2-lmn-adoption/research/` 或 `docs/`。
- 记录刷新后哈希与基线日期；明确"接口演进属编译期断裂，成员清单随基线固化"。

## 验收条件

- [ ] `Libs\SDG.NetTransport.dll` 哈希与游戏安装版一致（刷新完成）。
- [ ] `ITransportConnection` 成员清单文档落盘，含反编译签名 + 日期 + 刷新前后哈希。
- [ ] 构建仍 0/0（刷新不破坏现有引用）；七项目测试 PASS。

## 不做

- 不实现任何网络逻辑；不修改 Contracts 类型。
