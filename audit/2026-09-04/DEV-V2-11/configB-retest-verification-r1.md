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

---

## 附记（2026-09-05，用户质询「dropped outbound target=0」触发的补充调查）

**现象**：主机与 U3DS 服务器各 4 条 `[Error :LaunchMultiplayerNet] [ModTransport] dropped outbound
server->client message: transport not found (target=0, channel=250)`（07 三份归档中为 0）。

**机制（已实证）**：该 Error 是 **LMN 自己的出站守卫**——`ModTransport.SendToClient(CSteamID target,…)`
按 steamId 查 `ITransportConnection`，查不到即丢帧并记 Error（`ModTransport.cs:488`）。BUE 的接管只拦截
入站（`NetMessages.Receive*` 前缀），出站路径不经 BUE。直接触发链：fixture 的 server handler 以
**sender=0** 收到 ping（每会话 20 条，仅 VM 客户端入网后开始，每 ping 周期一条）→ 排队 pong 给 target=0
→ LMN 拒发。同一会话真实 id 的送达与 pong（45 条中 25 条）全部正常，V1/V2 双向 seq 对齐，pong 1:1 无重复
投递——**功能无损，P5 判据（BUE 错误行）不受影响**（此为 LMN 自己的 Error 级日志）。

**新发现 F-E（立案 DEV-V2-12）**：sender=0 的送达是本轮镜像生效后才出现的第二投递路径——同一 seq 的
ping 既以 sender=0 到达一次、又以真实 id 到达一次（V1 与 V2 named 两条协议路径同 pattern）。即镜像活了
之后，存在一条**丢失发送者身份的送达路径**（候选：BUE 前缀 `TryGetConnectionSteamId` 反射对某类连接/
第二传输副本解析失败回 0；或 SteamP2PFriends 中继副本）。对 fixture 无害（pong 被 LMN 安全拒发），但对
**依赖 sender steamId 的真实旧插件是行为级风险**（回调收到 0），且「同帧两次到达」与接管「恰好消费一次」
的设计语义不符。需要归因后修复（候选方向：BUE 前缀对 sender 解析失败的帧放行给 LMN 原生路径，而非以 0
派发）。

**本票（DEV-V2-11）结论不变**：其 Scope（类型名/静默解析/委托锚）与验收全数达成，resolved 维持；F-E 走新票。
