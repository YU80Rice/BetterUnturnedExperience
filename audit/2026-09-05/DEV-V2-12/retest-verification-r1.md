# DEV-V2-12 候选实机复核记录 — R1（2026-09-05 下午采集，同日复核）

> 采集人：用户（UMM 诊断包 ×3 + U3DS LogOutput ×1，已开 `[Logging.Disk] LogLevels …,Debug`）。
> 证据归档 `../evidence/DEV-V2-12-20260905/retest-r1/`（4 包）。
> 候选：DEV-V2-12-CLEAN-20260905（DLL `B4E37FFA…E959`，BuildIdentity `C9EEF3B8…`，提交 b670474）。

| 环境 | 包 |
|---|---|
| VM 客机（P2P 客户端） | `vm-client-143829/` |
| 房主主机（单人阶段 + P2P 主机） | `host-143901/` |
| U3DS 客户端 | `u3ds-client-144421/` |
| U3DS 服务器 | `u3ds-server-1444_LogOutput.log` |

## 判据核对（DEV-V2-12 结单报告 §7 / 工单验收 4）

| 判据 | 期望 | 实测（四端） | 结论 |
|---|---|---|---|
| 部署指纹 | assembly-identity = `B4E37FFA…` | 四端全部 `B4E37FFA7581CD70CDC552242F3A01CD62FC86095C99C857AB5FC3E51B33E959` | ✅ |
| **零 sender=0 送达** | `[V1FIX] recv-from-client sender=0` = 0 | **0 / 0 / 0 / 0**（上轮 host+u3ds-server 每会话 20 条） | ✅ |
| **零 dropped outbound target=0** | `dropped outbound … target=0` = 0 | **0**（四端；上轮 u3ds-server 8 条 target=0） | ✅ |
| **每 seq 恰一次送达** | recv 行 (FIX,seq,kind,sender) 无重复键 | **四端 0 重复**（同口径上轮 host 20 / u3ds-server 8 个重复键——双投递消失） | ✅ |
| pong 1:1 | server send-pong = recv-from-client | host 38/38（V1 19+V2 19）、u3ds-server 32/32（16+16） | ✅ |
| 镜像正向锚 | `v1-table-mirror result=mirrored channels=N` | 四端在场（host bootstrap `deferred=true` + 可逆钮重臂多次；channels=2） | ✅ |
| **release 正向锚（新）** | `v1-frame-release` / `lmn2-frame-release result=released decision=lmn-native-dispatch` 各恰一条 | 四端各恰 1 条（一次性，跨重臂不重发） | ✅ |
| **delegated 回归信号** | `lmn2-delegate result=delegated` 不应出现 | **0**（四端；live 世界委托已由放行替代，符合口径变更） | ✅ |
| P5 零误报 | 零「BUE 错误」行 / result=failed | 0 / 0（四端） | ✅ |
| 零 TypeByName 警告（BUE 侧） | 无 BUE 引发的 HarmonyX 警告 | 每客户端仅 1 条 `TransportConnection_SteamNetworkingSockets` 宽匹配警告 = **SteamP2PFriends 自身探测，非 BUE**（与上轮相同）；u3ds-server 0 条 | ✅ |
| B5/B6 可逆 | installed→removed→installed 行为链 | vm 2i/2r、host 4i/3r（多轮可逆钮循环）、u3ds-client 3i/2r、u3ds-server 1i/1r（退出清理） | ✅ |
| P3/P4a seq 对齐 | 客户端 send-to-server ↔ 服务端 recv-from-client 衔接 | u3ds：client send seq 4… ↔ server recv 5…20；P2P：vm send …34-38 ↔ host recv 34…38 | ✅ |

## 具名观察（不阻塞，供批准知情）

**N-1：u3ds-server 2 条 `dropped outbound … transport not found (target=76561199030780228)`**（V1+V2 各 1，
均在 seq=5 = 客户端入网后**首个 ping**；seq=6 起 32 发 pong 全部送达）。定性：

- 非 target=0——判据字面满足（target=0 为 0）。
- 非 F-E 范围：出站路径全程是 **LMN 自己的**（BUE 只拦入站、不碰出站，本票未改出站任何代码）。机制 =
  LMN 出站 `FindClientTransport(steamId)` 遍历 `Provider.clients`，首个 mod ping 相对入网更早到达时
  SteamPlayer transport 尚不可解析 → 该 pong 被拒。上轮日志无此现象 = 竞态未触发（上轮首 ping seq=8，
  本轮 seq=5 更贴近入网），属**连接时序竞态的既有 LMN 行为**，本轮首次露出。
- 影响：单次连接首 ping 的 pong 丢失 1:1（V1/V2 各 1），之后全部正常；fixture 被动方无功能损害。
- 处置：具名遗留——如需根治属 LMN 出站健壮性（后续票，不在 BUE V2 第一阶段范围）。

**N-2：BepInEx 横幅时间戳不可信（已知坑）**：四端横幅均显示 9/4 时点（VM 时钟漂移）。会话真实性由
**内容锚**定：四端部署指纹 = 新候选 `B4E37FFA…` + 日志含旧 DLL 不可能产生的 `v1-frame-release` 新锚。
case.json 时间窗以 UMM 采集时刻（2026-09-05 14:38/14:39/14:44 +08:00）作保守界。

## 判定

**DEV-V2-12 验收第 4 条达成：四端零 sender=0 送达、零 target=0 Error；双投递消失（每 seq 恰一次，
四端 0 重复键，上轮 28 个）；sender 身份全程真实；release 锚齐、delegated 无回归、P5 干净、可逆链在。
实机复核全绿 → 本票代码与验收闭环，进入 case.json×4 → 四角色资格门禁 → 人工批准 → resolved。**
