# 交付报告 — DEV-V2-01：SDK 网络基线锁定

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-01-sdk-net-transport-baseline.md`
> 阶段：V2 第一阶段实施首票（/implement + /tdd 红测 + 双轴审查）
> 性质：SDK 依赖基线（非功能变更；非发布授权）

## 1. 交付内容

| 项 | 结果 |
|---|---|
| `Libs\SDG.NetTransport.dll` 刷新 | `9B4D27A8...`（08/11）→ `D512DB03...`（09/01，与游戏安装 `E:\Steam\...\Unturned_Data\Managed\` **逐字一致**） |
| 旧版备份 | `Libs\SDG.NetTransport.dll.bak-20260811`（留档） |
| 基线文档 | `research/V2-NET-BASELINE-sdg-nettransport-20260903.md`——`ITransportConnection` 7 成员 + 关联类型清单 + 演进政策 + 刷新流程 |
| 基线红测 | `--sdk-net-baseline-red`（`AssertSdkNetTransportBaseline`）——反射断言成员按名存在 + `IEquatable` + `Send` 3 参 + **参数类型绑定**（`byte[],long,ENetReliability`）+ `ENetReliability` 恰 2 值 |
| 测试 csproj | 新增 `SDG.NetTransport` 引用（`..\..\..\Libs\SDG.NetTransport.dll`） |

## 2. 验证矩阵

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings |
| 七项目测试运行器 | 全 PASS |
| `--sdk-net-baseline-red` | ✅ exit 0（强化后含参数类型绑定） |
| UI/native token 扫描 | Core + ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 3. 双轴独立审查

| 轴 | 判定 | 阻断项 | 已处置 smell |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | S1 csproj 引用排序（可延后）；S2 Send 参数类型未绑定（**已修**）；S3 transportType null 断言死检查（可延后）；S4 文档验收勾选留空（**已修**） |
| **Spec** | **CLEAN** | 无 | A 文档验收核对不一致（**已修**）；B 红测未 bind 参数类型（**已修**）；C "新增成员→编译失败"措辞夸大（**已修**） |

> 两轴共同点名的 Send 参数类型绑定已补（`sendParams[0]==byte[] && [1]==long && [2]==ENetReliability`），基线锁从"按名"强化到"按签名"。

## 4. 解锁

- **DEV-V2-03（BueNetworkApi 运行时）**——其 Blocked by 之一即本票，现已满足。
- frontier：DEV-V2-02（契约类型，无阻塞）、DEV-V2-03（阻塞 01+02）。

## 5. 交付边界

- `Libs\` 在 BUE 仓库外（`D:\Agent-工作目录\...\Libs\`，非 git 跟踪）；仓库内提交的是基线文档 + 红测 + csproj 引用 + 票。
- 非发布授权；V2 第一阶段三环境验证（DEV-V2-07）完成前不宣称运行或发布通过。
