# 交付报告 — DEV-V2-02：BueNetworkApi 契约类型写入

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-02-buenetworkapi-contract-types.md`
> 阶段：V2 第一阶段实施第 2 票（/implement + /tdd 红测 + 双轴审查）
> 性质：契约类型（纯 .NET；非发布授权）

## 1. 交付内容

| 项 | 结果 |
|---|---|
| **BueNetwork 命名空间 5 类型** | `NetworkSendResult`（枚举）/ `ChannelRegistrationResult` / `ChannelVersionEntry` / `IConnectionSession` / `IBueNetworkApi`（`ContractTypes.cs` 追加） |
| **契约面** | RegisterChannel(FeatureId, ContractVersion, ushort)（Q1/Q2）；Reason 复用 `FeatureRegistrationReason`；SendToServer/SendToClients/SendToClient(channel, session, payload, reliable)（Q9）；会话含 SessionId/PeerSteamId/PeerFeatureVersion/Channels（Q4/Q10）；`BueNetwork` 命名空间（Q12）；纯 .NET 4.7.2 零引擎引用（Q6） |
| **红测** | `--bue-network-contract-red`（先 CS0234 编译红 → 转绿；红测自抓一处断言参数位置错误并修正） |
| **仓库修复** | `src/BetterUnturnedExperience.Contracts/` 首次纳入 git 跟踪（csproj + ContractTypes.cs；bin/obj 排除）——此前 Plugin 靠嵌入编译但源码未入库 |

## 2. 验证矩阵

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| `--bue-network-contract-red` | ✅ exit 0 |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 3. 双轴独立审查

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 Contracts 目录未跟踪（**已修复入库**）；S2 新类型块状体 vs 基文件单行风格；S3 红测重复 GetProperty 调用 |
| **Spec** | **CLEAN** | 无 | S1 会话多 `PeerContract` 成员（T3 Q10 未列，追加无冲突）；S2 `Sessions` getter 与 Q4 事件驱动张力（DEV-V2-03 需确认事件能传递 session）；S3 `UnregisterChannel` 额外成员；S4 红测未穷尽部分签名细节；S5 FeaturePresentationState 命名（跨票，归 SCR-GPT18-001 所有者） |

> Spec 轴逐项确认 T3 Q1-Q12 **12/12 全部满足**；FeaturePresentationState 已在基命名空间（L69）不重复添加。

## 4. 解锁

- **DEV-V2-03（BueNetworkApi 运行时）**——其 Blocked by 01+02 现已全部满足。
- frontier：DEV-V2-03（阻塞已解，可认领）。

## 5. 交付边界

- 契约类型进 Contracts 源码（Plugin 嵌入编译）；运行时实现（帧编解码/IClientTransport 反射/Steam 细节）留 DEV-V2-03。
- 非发布授权；三环境验证（DEV-V2-07）前不宣称运行或发布通过。
