# DEV-V2-11 候选实机复核记录 — R1（2026-09-05 凌晨采集，05 复核）

> 采集人：用户（UMM 诊断包 ×3 + U3DS LogOutput ×1，本轮已按清单开启 `[Logging.Disk] LogLevels …,Debug`）；复核：agent。
> 证据归档 `../evidence/DEV-V2-11-20260904/configB-retest/`（17 文件）。
> 候选：DEV-V2-11-CLEAN-20260904（DLL `5B4E948E…5BCD`，BuildIdentity `01BFF640…`，提交 6907a1a）。

| 环境 | 包 |
|---|---|
| VM 客机 | `vm-client-235508/` |
| 房主主机 | `host-235544/` |
| U3DS 客户端 | `u3ds-client-000024/` |
| U3DS 服务器 | `u3ds-server-0000_LogOutput.log` |

## 判据核对（DEV-V2-11 结单报告 §6）

| 判据 | 期望 | 实测（四端） | 结论 |
|---|---|---|---|
| 部署指纹 | assembly-identity = `5B4E948E…` | 四端 LogOutput 全部 `5B4E948E5A81FB75B05D017BF0958C11B4B0C75279DB6F21D2151CA8C84C5BCD` | ✅ |
| P5 零误报 | 无「BUE 错误：」行 / 无 result=failed | 0 / 0 | ✅ |
| 零警告（F-C 噪声） | 无 `AccessTools.TypeByName … LaunchMultiplayerNet` 警告（上轮 63~169 条/会话） | **0**（四端；每会话仅存的 1 条宽匹配警告来自 SteamP2PFriends 自身探测 `TransportConnection_SteamNetworkingSockets`，非 BUE） | ✅ |
| 镜像正向锚 | `event=v1-table-mirror result=mirrored channels=N` | host/VM/U3DS客户端 = 2 条（bootstrap + 可逆钮重臂，均 `channels=2`）；U3DS服务器 = 1 条 | ✅ |
| 委托正向锚（GAP-1） | `event=lmn2-delegate result=delegated decision=consume` 恰一条 | **四端各恰 1 条** | ✅ |
| 反证锚 | 镜像生效后 `unknown-channel-dropped` 归零 | 0（上轮该行成对出现） | ✅ |
| B5/B6 可逆 | 面板关→开：`takeover-patch removed` → `installed` 重现 | host/VM/U3DS客户端 = `installed→removed→installed`（末条 removed 为退出清理者不计）；U3DS服务器 = `installed→removed`（退出清理） | ✅（行为级证明「BUE 网络模块」条目+接管卡+可逆钮可达且工作） |
| P3/P4a 功能面 | V1/V2 seq 双向对齐 | 四端 V1FIX/V2FIX recv/send 全成对（host 77/77、U3DS服 16/32 等）；LMNFIX ready 四端 1 | ✅ |
| 面板两条目截图 | UI 截图 | 未单独留截图；「BUE 网络模块」条目由 B5/B6 序列行为级证明 | ⚠ 具名缺口（gate 前若需截图可补，功能链已证） |

## 机制勘误（推翻 07 复核的「时机」归因）

本轮实锤：主机日志 **L14 `Loading [LaunchMultiplayerNet 5.0.0.0]`（发现阶段预载）早于 L142 BUE 实例化**——
BepInEx 在插件发现阶段加载全部程序集，Awake 顺序才按文件名排。因此「BUE 先载、LMN 程序集晚到」的时机窗口
**不存在**，类型名错误才是 07 以来镜像/LMN2 委托失效的唯一根因（deferred 路径在本轮四端均为 0 触发，
defer/重试机制保留为无害纵深防御）。已向 `../DEV-V2-07/configB-verification-r1.md` 追加勘误注记。

## 判定

**DEV-V2-11 验收四条全部达成（红→绿、门禁 0/0+七运行器、类型名/静默解析锚、新候选身份），实机复核全绿 →
本票 resolved。** real-machine-test-loop 修复链（DEV-V2-10 + DEV-V2-11）闭环：零错误行、零警告、
镜像/LMN2 委托首次真实生效且有正向锚。剩余 DEV-V2-07 链条收尾步骤：
① 两条网络条目截图（可选补强，功能链已证）；② case.json 填实（用候选 `DEV-V2-11-CLEAN-20260904` 的
candidate.json）→ QualificationGateRunner gate → 人工批准。
