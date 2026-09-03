# 交付报告 — DEV-V2-03：BueNetworkApi 运行时实现

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-03-buenetworkapi-runtime.md`
> 阶段：V2 第一阶段实施第 3 票（/implement + /tdd 红测 + 三轮双轴审查）
> 性质：运行时实现（纯 C# Host 内部；非发布授权）

## 1. 交付内容

`BueNetworkRuntime`（`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs`，纯 C#、`INetworkTransport` seam、经 Plugin 嵌入编译）：

| 能力 | 实现 |
|---|---|
| 频道路由 | `RegisterChannel(FeatureId, ContractVersion, ushort)`（Q1 频道=FeatureId）/ `UnregisterChannel` / `SendToServer|SendToClients|SendToClient` / `Subscribe` 接收 |
| 版本协商 | 注册期 `ContractIncompatible`（Q2）+ **Hello/Ack 对端握手**（F1 修复）：`StartSession` 发 Hello → 对端校验 Major → Ack（建会话+触发 Connected）/ Reject（不建） |
| 会话管理 | `IConnectionSession` 实现（SessionId/PeerSteamId/PeerContract/事件/Send）；`Connected` 真实触发（F2 修复） |
| 可靠性 | `NetworkSendResult` 显式枚举（本地失败信号，Q11）；解码 16KiB 预分配守卫 |
| 帧格式 | 内部：magic "BUE2" + kind（Data/Hello/Ack/Reject）+ 频道 + 载荷；不进 Contracts |

## 2. TDD 循环（红测真实抓到的缺陷）

| 红测阶段 | 抓到的问题 |
|---|---|
| 编译红 CS0234 | BueNetworkRuntime 不存在 → 实现 |
| 运行时红 #1 | 会话 ID 跨实例碰撞（两 runtime 都从 1 起号，绕过所有权检查）→ 静态计数器 |
| 编译红（构造签名） | ctor 改 3 参后测试调用点未同步 → 修复 |
| Spec R2 BLOCKED | F1 握手缺失 / F2 事件未触发 → 实现 Hello/Ack + Connected |
| Standards R2 BLOCKED | **处理器锁内调用**（违反"state protection never spans external code"）→ 锁外派发重构 |
| 红测（S3） | Reject 后幽灵会话未清理 → HandleReject 清扫 |

## 3. 验证矩阵

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| `--bue-network-runtime-red` | ✅ exit 0 |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 4. 双轴独立审查（三轮循环）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| R1 | CLEAN（4 smell） | CLEAN（5 smell） | 通过 |
| R2 | **BLOCKED**（处理器锁内调用） | **BLOCKED**（F1 握手缺失 + F2 事件未触发） | 修复：Hello/Ack 握手 + Connected 触发 + 锁外派发 |
| R3 | **CLEAN**（3 可延后：A 锁内 SendFrame/B Reject payload 未用/C 捕获引用竞态） | **CLEAN**（3 可延后：S1 全清扫假设/S2 reliable 未达帧/S3 首会话上下文） | **交付** |

- 可延后项全部列名（工单"双轴审查可延后项"节），不阻断。
- Spec 建议已落实：DEV-V2-04 票补两行移交范围（可靠透传 + 按帧来源解析分发）。

## 5. 解锁

- **DEV-V2-04（LMN 接管）** 阻塞已解。
- 交付边界：纯 C# seam 已验证；三环境实机（DEV-V2-07）前不宣称网络层运行通过。
