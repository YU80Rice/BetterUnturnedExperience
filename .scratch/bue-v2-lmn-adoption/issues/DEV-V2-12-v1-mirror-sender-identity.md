# DEV-V2-12：V1 镜像路径 sender 身份丢失（sender=0 双到达）归因与修复

Type: task
Status: open（2026-09-05 建票；DEV-V2-11 候选实机复核附记触发，见 `audit/2026-09-04/DEV-V2-11/configB-retest-verification-r1.md` 附记）
Parent: spec-V2-phase1-lmn-adoption（DEV-V2-11 后续）
Blocked by: 无（证据已归档）
Blocks: 无（不阻塞 DEV-V2-07 gate；但属接管语义正确性缺陷，建议 gate 前或下轮一并修）

## 发现（DEV-V2-11 候选四端实机复核）

**F-E**：镜像生效后，服务端（主机/U3DS 服务器）对 VM 客户端的每个 V1 ping 出现**两次到达**：一次
`[V1FIX] recv-from-client sender=0 kind=ping`（每会话 20 条），一次真实 id。V2 named 路径同 pattern
（20 个 seq 双 sender）。sender=0 的副本使 fixture 往 target=0 回 pong，被 LMN 出站守卫拒发并记
Error（`dropped outbound … transport not found (target=0, channel=250)`，`ModTransport.cs:488`）。
07 归档（镜像死）中该 Error 与 sender=0 送达均为 0。

影响评估：对 fixture 无功能损害（pong 1:1、真实通路完好、P5 不受影响——LMN 自己的 Error 非 BUE 误报）；
但对**依赖 sender steamId 的真实旧插件**，sender=0 回调是行为级缺陷；且「同帧两次到达」与接管
「恰好消费一次」语义不符（BUE 消费后 LMN 前缀不应再收到——现两条路径各送达一次，或存在第二传输副本）。

## 待归因（红测先行）

1. 双到达的两个来源各是什么：BUE 前缀（镜像注册表派发）+ LMN 原生前缀？还是同一前缀收到两份网络副本
   （SteamP2PFriends 中继/loopback 副本）？
2. sender=0 的解析点：`NetworkModuleAdapter.TryGetConnectionSteamId`（`TryGetSteamId` 反射）对哪类
   连接失败回 0？
3. 红测锚（宿主可测）：模拟「steamId 解析失败（返回 0）」的连接——期望行为待定：BUE 前缀对 sender
   解析失败的 V1 帧**放行**（交 LMN 原生以真 id 处理）而非以 0 派发；同时不得造成 LMN 侧双投递。

## Scope

1. 归因（读 LMN 前缀/SteamP2PFriends 行为 + 复核已有证据日志定位两条到达路径）。
2. 红测先行修复 sender 身份语义（放行 vs 派发的决策锚 + 去重）。
3. 全量门禁 + 双轴 CLEAN + 新候选 + 实机复测（判据：零 sender=0 送达、零 dropped outbound target=0、
   V1/V2 双向仍通、镜像/委托锚仍在）。

## 验收条件

- [ ] 归因结论入审计（两条到达路径的准确定位）。
- [ ] 红测 observed red → green（sender 语义锚）。
- [ ] 七运行器 exit=0；0 error/0 warning。
- [ ] 实机复测零 sender=0 送达、零 target=0 Error。

## 不做

- 不动 LMN 仓库；不动 SteamP2PFriends；不改 fixture（其 sender=0 行为是被动的）。

## Comments

> 2026-09-05 建票（agent）：用户质询实机日志 `dropped outbound target=0` 触发调查，机制与影响见
> DEV-V2-11 复核记录附记。功能无损但属接管语义正确性缺陷，建议与 DEV-V2-07 gate 并行排期。
