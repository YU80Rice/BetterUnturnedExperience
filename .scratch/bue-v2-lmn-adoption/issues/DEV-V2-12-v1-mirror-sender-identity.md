# DEV-V2-12：V1 镜像路径 sender 身份丢失（sender=0 双到达）归因与修复

Type: task
Status: resolved（2026-09-05，agent；四端实机复测全绿 + 四角色资格门禁 TechnicallyQualified exit 0，四条验收全达成。**人工发布批准已入档**——用户授权批准 DEV-V2-12 正式关闭，候选 DEV-V2-12-CLEAN-20260905 获正式发布资格）
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

- [x] 归因结论入审计（两条到达路径的准确定位）。
- [x] 红测 observed red → green（sender 语义锚）。
- [x] 七运行器 exit=0；0 error/0 warning。
- [x] 实机复测零 sender=0 送达、零 target=0 Error。

## 不做

- 不动 LMN 仓库；不动 SteamP2PFriends；不改 fixture（其 sender=0 行为是被动的）。

## Comments

> 2026-09-05 建票（agent）：用户质询实机日志 `dropped outbound target=0` 触发调查，机制与影响见
> DEV-V2-11 复核记录附记。功能无损但属接管语义正确性缺陷，建议与 DEV-V2-07 gate 并行排期。

> 2026-09-05 用户拍板（agent 记录）：**非阻塞，延后处理**。理由：fixture 生态零功能损害（pong 1:1、
> 真实通路完好、P5 判据不受影响），影响面仅限未来「依赖 sender steamId 的真实旧插件」。归属判断：
> BUE 至少参与（镜像派发以 sender=0 进行属 BUE 语义，解析失败应放行而非派发），双到达源头未归因
> （候选含 SteamP2PFriends 中继副本——外部件）。与 DEV-V2-07 gate 解耦，恢复时机=真实旧插件接入前。

> 2026-09-05 认领开工（agent）：用户指令按冻结路线依赖图恢复本票（依赖图核实：Blocked by 无，
> 前置链 01–07/10/11 全 resolved，可开工）。路线：归因（读 LMN prefix/router/transport 源码 +
> BUE 兼容层/镜像源码）→ 红测先行（sender 语义锚 + 恰好一次消费锚）→ 最小修复 → 全量门禁 →
> 双轴独立审查至双 CLEAN → 新候选身份。实机复测（判据：零 sender=0 送达、零 target=0 Error、
> V1/V2 双向仍通、镜像/委托锚仍在）留给人工执行，通过前本票保持 claimed。注意：新候选不自动
> 继承 DEV-V2-11 的发布批准，须另走资格门禁+人工批准。

> 2026-09-05 实施+双轴闭环（agent）：**归因落定**。(1) 双到达=Harmony 语义：前缀返回 false 只跳过
> 原方法，LMN 自己的低优先级前缀（owner=`com.yu80rice.launchmultiplayernet`，与 BUE 挂同一
> `NetMessages.Receive*`，且调同一 `ModRouter`）仍会派发。实机形态吻合：V1 每 seq 一条 sender=0
> （BUE 兼容层派发）+ 一条真实 id（LMN 原生派发）；V2 每 seq 两条**相同**真实 id（两前缀各调一次
> router，DEV-V2-11 复测 u3ds-server 日志 56/57 行）。排除 SteamP2PFriends 线缆副本：副本假说预测
> V1 两条 sender=0 / V2 四条送达，与日志不符；且 07 归档（镜像死→BUE 从不派发）三项全 0，时序仅随
> DEV-V2-11 镜像/委托激活出现。(2) sender=0=BUE 反射 helper 按 `out CSteamID` 形状经
> `CSteamID.m_SteamID` FieldInfo 读 `TryGetSteamId` 的 out 值，SDK 真签名 `TryGetSteamId(out ulong)`
> （V2-T1 基线 L32），GetValue 对装箱 ulong 抛异常被吞→**恒 0**；真实 id 一直在 args[0] 被丢弃，
> LMN 直调接口故得真 id。
> **修复**（不动 LMN/SteamP2PFriends/fixture）：helper 直读 out ulong；接管决策核改两态契约——
> LMN 原生前缀 live（实机常态；per-direction 单调 latch + tick 节流重探，BUE 按文件名序先于 LMN
> 装补丁故启动时必 inert）→ BUE 放行（一次性锚 `v1/lmn2-frame-release result=released
> decision=lmn-native-dispatch`）；inert → BUE 独派发，但 fromClient 且 sender==0 的帧放行、绝不以 0
> 派发（一次性锚 `decision=unresolved-sender`）。
> **口径变更（入档）**：live 世界委托不再发生 → 工单复测判据「委托锚」由 release 锚替代；镜像锚
> `v1-table-mirror result=mirrored` 保留。v1compat 开关口径显式化：live 世界帧送达走 LMN 原生前缀、
> 不受开关约束（变更前亦然，非本票行为变更）；live 世界真丢弃 V1 需中和 LMN 派发=具名后续票。
> **红绿链**：red1（sender 解析恒 0）→ red2（live 未放行）→ red3（sender=0 仍派发）→ 绿；R2 red4
> （tick 重探缺失）→ R3 red5（server 晚装不重探）→ green6（EXIT=0），见
> `audit/2026-09-05/DEV-V2-12/red*-…log`。**门禁**：Release Rebuild 0 error/0 warning + 七运行器全
> exit 0 + NoUiTokens Core/ClientUi PASS（同目录）。**双轴**：R1（Standards 3 + Spec 7 findings）→
> R2 修复复审（1 blocking=per-direction 哨兵 + 2 should-fix）→ R3 修复复审**双 CLEAN**。
> **待人工**：实机复测（判据=零 sender=0 送达、零 dropped outbound target=0、V1/V2 双向 seq 对齐、
> 镜像锚+release 锚在场）→ case.json 填实 → QualificationGateRunner gate → 人工批准（新候选不继承
> DEV-V2-11 批准）；复测通过前本票保持 claimed。

> 2026-09-05 候选身份授予（agent）：源码提交 `b670474`（含全部源码/测试/票面/结单报告）后 Release 重建，
> 两轮 `-t:Rebuild` SHA-256 逐字节一致（确定性复核通过）；身份由 kit runner identity 模式实码计算
> （配方沿 DEV-V2-07 §5）。**CandidateBuild `DEV-V2-12-CLEAN-20260905`**（CaseId `DEV-V2-12-20260905`，
> SourceSnapshotId `b6704744012794b72a0293f2ae122b995b1f28c4`，DLL SHA-256 `B4E37FFA7581CD70CDC552242F3A01CD62FC86095C99C857AB5FC3E51B33E959`
> /270336 字节，**BuildIdentity `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272`**，
> DefinitionSetDigest `38D66989…B854` 与 DEV-V2-10/11 一致——官方定义集未动自洽，ReferenceSet/Toolchain 不变）。
> 归档 `audit/2026-09-05/artifacts/DEV-V2-12-20260905/{BetterUnturnedExperience.dll, candidate.json}` +
> `audit/2026-09-05/DEV-V2-12-dll-sha256.txt`；报告 §6 已回填。本候选**不自动继承** DEV-V2-11 发布批准——
> 复测采证 → 4×case.json → 四角色资格门禁 → 人工批准后方可关单。

> 2026-09-05 实机复测全绿 → **resolved**（agent；采集=用户，四端 UMM 诊断包×3 + U3DS LogOutput×1，
> 证据 `audit/2026-09-05/evidence/DEV-V2-12-20260905/retest-r1/`，复核记录
> `audit/2026-09-05/DEV-V2-12/retest-verification-r1.md`）。**判据实测**：四端部署指纹 `B4E37FFA…` 一致；
> 零 sender=0 送达（上轮 20 条/会话）；零 dropped outbound target=0（上轮 8 条）；**每 seq 恰一次**——
> recv (FIX,seq,kind,sender) 重复键四端 0（上轮 host 20 / u3ds-server 8，双投递消失）；pong 1:1（host
> 38/38、u3ds-server 32/32）；镜像锚 + v1/lmn2-frame-release release 锚各恰一条；delegated=0（无回归）；
> P5 干净；B5/B6 可逆链在；P3/P4a 对齐。具名观察 N-1（不阻塞）：u3ds-server 2 条 LMN 自身出站竞态
> Error（target=真实 id，seq=5 首 ping 早于 SteamPlayer transport 可解析，seq=6 起全中；出站路径 BUE 未
> 触碰，非 target=0 非 F-E 范围，如需根治=后续票）。具名观察 N-2：横幅时间戳不可信（已知坑），会话真实
> 性由部署指纹+新锚内容锚定。**四角色资格门禁 TechnicallyQualified（exit 0，canonicalDigest
> `9E731F7B5689DF258F85C236089575470BF32EAE841B4A77A447BE163EED915B`）**——SP/P2PHost/P2PClient/
> U3dsHeadless 全 Fulfilled（P2P 主客成对 case 共用 CaseId `DEV-V2-12-20260905-P2P`，与 11 先例一致）。
> 四条验收全达成，本票 resolved。**剩余独立动作 = 人工发布批准**（BuildIdentity `C9EEF3B8…` /
> DLL `B4E37FFA…` / canonicalDigest `9E731F7B…`，不自动继承 DEV-V2-11 批准）。

> 2026-09-05 人工发布批准入档（agent 记录）：用户（人工开发者）原话——**「作为人工开发，我授权批准
> DEV-V2-12正式关闭，感谢你的付出」**。批准对象 = **DEV-V2-12-CLEAN-20260905**（BuildIdentity
> `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272` / DLL SHA-256
> `B4E37FFA7581CD70CDC552242F3A01CD62FC86095C99C857AB5FC3E51B33E959` / 证据包 canonicalDigest
> `9E731F7B5689DF258F85C236089575470BF32EAE841B4A77A447BE163EED915B`）。至此 12 全链闭环：归因 →
> 红绿链 → 双轴双 CLEAN → 门禁 → 实机复测全绿 → 资格门禁 exit 0 → **人工批准**。ticket 状态维持
> resolved（批准前已达成）；本批准使该候选获得正式发布资格（与 8ccbf80 对 DEV-V2-11 候选的批准同性质，
> 不向下自动继承、不影响后续新候选）。
