# DEV-V2-11：LMN 类型名修正（镜像+LMN2 委托真生效）+ 解析静默化 + 面板四步补采

Type: task
Status: resolved（2026-09-05，agent；实机复核 `audit/2026-09-04/DEV-V2-11/configB-retest-verification-r1.md` 全绿闭环）
Parent: spec-V2-phase1-lmn-adoption（DEV-V2-10 后续）
Blocked by: 无（DEV-V2-10 已 resolved，候选 C3A35B07… 已实机四端部署验证）
Blocks: DEV-V2-07（面板四步补采 + P3 口径更正后的实机确认 → gate → 人工批准）

## 发现（DEV-V2-10 候选四端实机复核，证据见 configB-retest-verification-r1.md）

**F-C（error 级）**：BUE 常量 `ModTransportTypeName="LaunchMultiplayerNet.Routing.ModTransport"`、
`ModRouterTypeName="LaunchMultiplayerNet.Routing.ModRouter"` 均多写一级 `.Routing`。LMN 源码实证真名
`LaunchMultiplayerNet.ModTransport`（public static）与 `LaunchMultiplayerNet.ModRouter`（internal static）。
后果：(a) V1 表镜像从未成功——重试永久 pending，且每 5s 一次 `AccessTools.TypeByName` 失败被 HarmonyX
记 Warning，四端每会话 63~169 条刷屏；(b) LMN2 帧委托从未发生（恒走 router-null 放行），V2 named 互通
一直是 LMN 原生前缀扛的——07 复核 P3「经 BUE 接管决策点委托」口径需更正为「放行+LMN 原生送达」。

**F-D（证据缺口）**：面板四步（两条目/接管卡/B5/B6）无留证——LogOutput 未开 Debug 且 UMM 包无 Player.log。
随本候选复测一并补采，非代码缺陷。

## Scope

1. 红测先行：(a) 类型名锚——断言生产常量与 LMN 真名逐字一致（LMN 源码为 authority，防再抄错）；
   (b) 静默解析 helper——不存在类型返回 null 且零日志输出（宿主可测）。
2. 修复 F-C：两常量去掉 `.Routing`；LMN 类型解析弃用 `AccessTools.TypeByName`（其失败路径被 HarmonyX
   记 Warning），改为遍历已加载程序集的静默解析 helper。
2b. 口径固化（Spec R1 GAP-1）：修复后 LMN2 帧语义 = **BUE Priority.First 前缀命中 LMN2 → 反射调用
    `LaunchMultiplayerNet.ModRouter.TryHandleFromClient/Server`（真签名 ITransportConnection/byte[]/int/int），
    返回 true 则短路消费（LMN 自身前缀不运行）；false/null/异常 → 放行自愈链**。因 LMN 无逐帧日志、
    两条路径在 LMN 侧不可区分，delegate 增加一次性 Debug 记录
    `event=lmn2-delegate result=delegated decision=consume`（首帧成功委托恰一条），作为下轮复测
    「委托真实发生」的正向锚；`result=mirrored` 同理为镜像正向锚。
3. 全量门禁：Release `-t:Rebuild` 0/0、七运行器、NoUiTokens。
4. 双轴独立审查至双 CLEAN → 新候选身份 → 人工复测补采（开 Debug）：面板四步、B5/B6、
   mirrored/deferred 行、`lmn2-delegate result=delegated` 一次性锚、零 HarmonyX 警告、
   P3/P4a/P4b 仍通（P3 证据须含委托锚，仅 [V2FIX] seq 对齐不再足以证明 BUE 委托路径）。

## 验收条件

- [ ] 红测 observed red → green（F-C 至少一锚点）。
- [ ] 七测试运行器 exit=0；全解决方案 0 error/0 warning。
- [ ] 类型名锚证明生产常量与 LMN 真名一致；解析静默化证明缺失类型零警告。
- [ ] 新候选身份（新 DLL/新 BuildIdentity）；实机复测由人工执行后本票方可 resolved。

## 不做

- 不动 BueNetworkApi 契约面；不动 LMN 仓库（类型名以 LMN 现源码为准）。
- 不重采 DEV-V2-10 已归档证据；07 归档证据保持原样。

## Comments

> 2026-09-04 建票并认领（agent）：DEV-V2-10 候选四端复核触发。功能面（V1/V2 双向、BII、P5 字面）全过，
> 本票修守护层真根因与警告噪音，并把面板四步证据缺口并入下轮复测。

> 2026-09-05 resolved（agent）：新候选 `DEV-V2-11-CLEAN-20260904`（DLL `5B4E948E…5BCD`，BuildIdentity
> `01BFF640…`，提交 6907a1a+c468858）四端实机复核**全绿**：部署指纹四端一致；零「BUE 错误」行；**零
> TypeByName 警告**（上轮 63~169 条/会话——唯一存留的宽匹配警告来自 SteamP2PFriends 自身探测，非 BUE）；
> 镜像锚 `result=mirrored channels=2`（bootstrap + 可逆钮重臂各一）与委托锚
> `event=lmn2-delegate result=delegated` 各恰一条；`unknown-channel-dropped` 归零（V1 首次真正经 BUE
> 兼容层消费）；B5/B6 `installed→removed→installed` 行为级证明面板条目/接管卡/可逆钮可达且工作；V1/V2
> seq 双向仍通。**机制勘误**：BepInEx 发现阶段预载全部程序集，时机窗口不存在——类型名是 07 以来唯一根因
> （defer/重试保留为无害纵深防御）。具名遗留（gate 前，可选）：「BUE V1 兼容层」条目无单独截图（「网络
> 模块」条目已由 B5/B6 行为证明）。剩余 DEV-V2-07 链条：case.json 填实 → gate → 人工批准。
