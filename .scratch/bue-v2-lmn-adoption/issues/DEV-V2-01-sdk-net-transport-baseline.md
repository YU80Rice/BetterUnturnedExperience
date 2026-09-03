# DEV-V2-01：SDK 网络基线锁定

Type: task
Status: resolved（2026-09-03 交付，双轴审查 CLEAN，提交）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: 无（先行票，可立即开始）
Spec: `../spec-V2-phase1-lmn-adoption.md`（实施依赖清单第 1 项）

## Scope

把 BUE 网络模块的 SDK 依赖基线锁死，消除 `Libs\SDG.NetTransport.dll` 与游戏安装二进制的漂移。

- 用游戏安装版（`E:\Steam\...\Unturned_Data\Managed\SDG.NetTransport.dll`，mtime 2026/09/01）刷新仓库 `Libs\SDG.NetTransport.dll`（当前 mtime 2026/08/11，接口一致但哈希不同）。
- 固化 `ITransportConnection` 成员清单为 BUE 依赖基线文档（T1 research 已反编译：`TryGetIPv4Address/TryGetPort/TryGetSteamId/GetAddress/GetAddressString/Send(buffer,size,ENetReliability)/CloseConnection` + `IEquatable`），写入 `.scratch/bue-v2-lmn-adoption/research/` 或 `docs/`。
- 记录刷新后哈希与基线日期；明确"接口演进属编译期断裂，成员清单随基线固化"。

## 验收条件

- [x] `Libs\SDG.NetTransport.dll` 哈希与游戏安装版一致（`D512DB03...`，旧版备份 `bak-20260811` 留档）。
- [x] `ITransportConnection` 成员清单文档落盘（`research/V2-NET-BASELINE-sdg-nettransport-20260903.md`，含签名 + 日期 + 刷新前后哈希）。
- [x] 构建 0/0（刷新不破坏现有引用）；七项目测试 PASS；token 扫描零命中；`git diff --check` 通过。
- [x] 基线红测 `--sdk-net-baseline-red`（反射断言接口成员清单 + `ENetReliability` 两值）已加入全套——SDK 接口漂移时先在此红。

## 交付记录（2026-09-03）

- 刷新 `Libs\SDG.NetTransport.dll`：`9B4D27A8...`（08/11）→ `D512DB03...`（09/01，与游戏安装逐字一致）。
- 基线文档：`.scratch/bue-v2-lmn-adoption/research/V2-NET-BASELINE-sdg-nettransport-20260903.md`。
- 红测锚点：`--sdk-net-baseline-red`（Plugin.Tests）+ 全套调用。
- 测试 csproj 新增 `SDG.NetTransport` 引用。
- 解锁：DEV-V2-03（BueNetworkApi 运行时）。

## 不做

- 不实现任何网络逻辑；不修改 Contracts 类型。
