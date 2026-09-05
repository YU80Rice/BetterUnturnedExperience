# DEV-V2-12 交付与审计报告（2026-09-05，agent）

工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-12-v1-mirror-sender-identity.md`（V1 镜像路径
sender 身份丢失 / sender=0 双到达，归因与修复）。状态：**代码+门禁+双轴 CLEAN 完成，实机复测
待人工执行后凭 case.json → QualificationGateRunner gate → 人工批准关单**（新候选不继承
DEV-V2-11 批准）。

## 1. 归因（工单 Scope 1 / 验收 1）

### F-E/1 双到达：第二投递路径 = LMN 自己的 Harmony 前缀

- **机制**：Harmony 语义——任一前缀返回 false 只跳过**原方法**，不阻止其余（更低优先级）前缀
  运行。BUE 的 Priority.First 前缀「消费」一帧后，LMN 自己的前缀
  （owner=`com.yu80rice.launchmultiplayernet`，`NetMessagesReceiveClientPatch.cs:56-72`，调与 BUE
  委托完全相同的 `ModRouter.TryHandleFromClient`）照样派发该帧。07 以来的设计前提「LMN Prefix
  永不被调」不成立（`NetworkModuleAdapter` 旧注释 "returning false short-circuits both" 为错误
  断言，已随本票修正）。
- **实机形态吻合**（`audit/2026-09-04/evidence/DEV-V2-11-20260904/configB-retest/
  u3ds-server-0000_LogOutput.log:53-62`）：
  - V1：每 seq 恰两条——`sender=0`（BUE 兼容层派发，helper 恒 0）+ 真实 id（LMN 原生派发）。
  - V2：每 seq 恰两条**相同真实 id**（两个前缀各调一次同一 router；客户端方向同理，V2 pong ×4 =
    2 pong × 2 送达）。
  - 每帧伴随恰一条 BUE `lmn2-delegate result=delegated`（一次性锚，BUE 委托发生过的直接证据）。
- **排除 SteamP2PFriends 线缆副本**：若为传输层双副本，V1 应见 2×sender=0 + 2×真实 id（两副本 ×
  两前缀）、V2 应见 4 条——与日志（各 2 条）不符；且 07/10 归档（镜像死 + router 类型解析失败 →
  BUE 从不派发）中双到达与 sender=0 均为 0，双到达仅随 DEV-V2-11 修好类型名（镜像+委托首次真实
  生效）出现。线上每帧只有一次原始调用、两次前缀派发。
- **边界（如实记录）**：实机无逐前缀计数器，以上计数基于 fixture handler 逐次日志（handler 每次
  调用必打一行）；该推断可被复测证伪（复测判据含零 sender=0 / pong 不重复）。

### F-E/2 sender=0：反射 helper 形状错配，恒 0

- `TryGetConnectionSteamId` 假设 `ITransportConnection.TryGetSteamId` 的 out 参数为 CSteamID，
  Invoke 成功后用 `CSteamID.m_SteamID` 的 FieldInfo 读 args[0]。SDK 真签名
  **`bool TryGetSteamId(out ulong steamId)`**（研究基线
  `.scratch/bue-v2-lmn-adoption/research/V2-T1-itransportconnection-shape.md:32`；LMN 源码
  `ModRouter.ResolveSender` 直调同一成员得真实 id 可编译即为同形证据），args[0] 是装箱 ulong →
  FieldInfo.GetValue 抛 ArgumentException → catch 吞掉回 0 → **任何连接、每次都 0**。
- 真实 steam id 一直就在 args[0] 里被丢弃。DEV-V2-05~10 期间 BUE 从不派发 V1（镜像死），该
  生产-only 路径从未真实执行，缺陷不可见；DEV-V2-11 镜像激活后首次运行即全量 sender=0。
- host 红测以 `ITransportConnection` 假件（TryGetSteamId(out ulong) 返回真 id）复现恒 0。

## 2. 修复（不动 LMN 仓库 / SteamP2PFriends / fixture）

三件事，全部在 BUE（`NetworkModuleAdapter.cs` + 测试）内：

1. **sender 解析修正**：直读 out ulong（`(ulong)args[0]`），删除 CSteamID FieldInfo 读取；
   `AssertSdkNetTransportBaseline` 新增 `TryGetSteamId(out ulong)` 形状锚防 SDK 漂移。
2. **接管决策核两态契约（per-direction）**：
   - **live**（LMN 原生前缀存活=实机常态）：BUE 对每个 LMN 帧放行（return false → LMN 原生派发
     恰一次、真实 sender）——恰好一次由此构造性成立。一次性正向锚
     `v1-frame-release` / `lmn2-frame-release result=released decision=lmn-native-dispatch
     diagnosticId=BUE-V2NET-003`。
   - **inert**（LMN 补丁失效）：BUE 独派发（V1 走镜像兼容层、LMN2 委托 router，原语义保留），
     但 fromClient 且 sender==0 的帧放行、绝不以 0 派发；一次性边界锚
     `decision=unresolved-sender`（release 是有意的非送达：inert 世界落 vanilla 即丢弃，
     不以假身份派发）。
   - liveness 检测：生产按方向 `Harmony.GetPatchInfo` 匹配 LMN owner（LMN 安装非严格原子，
     两方向独立判定）；per-direction 单调 latch（一旦 live 不回退、不重探）；tick 节流重探
     （DeferredMirrorTickInterval≈5s，任一方向仍 inert 才探）——BUE 按文件名序先于 LMN 装补丁，
     启动时缓存 inert 是合法态，靠 tick 重探 latch；双方向均 live 后零反射。
3. **注释/口径修正**：前缀处 "short-circuits both" 错误断言改为 Harmony 真语义；ShouldConsumeInbound
   文档重写为两态契约；v1compat 开关口径显式化（live 世界帧送达走 LMN 原生前缀、不受开关约束
   ——变更前亦然，非本票行为变更；live 世界真丢弃 V1 需中和 LMN 派发=具名后续票）。

**口径变更（对照工单复测判据）**：live 世界委托不再发生 → 「委托锚 `lmn2-delegate result=delegated`」
由上述 release 锚替代；镜像锚 `v1-table-mirror result=mirrored` 保留。此为归因后的预期结果，非偏差。

## 3. 红测先行（工单 Scope 2 / 验收 2）

红测锚 `--bue-v2-sender-identity-red`（Plugin.Tests，随主流程常驻）。五轮 observed red（均 exit 1，
日志在同目录）→ 绿：

| 轮次 | observed red（失败断言） | 缺陷点 |
|---|---|---|
| red1 | sender identity: a resolvable connection yields its real steam id | helper 恒 0 |
| red2 | takeover: with LMN's native dispatch live the legacy frame is released | live 世界仍派发（双投递） |
| red3 | takeover: an unresolvable client sender is released — never dispatched as sender=0 | 以 0 派发 |
| red4（R2） | takeover: the throttled tick re-probe latches LMN's live dispatch | 启动缓存 inert 永不重探（Standards R1 blocking） |
| red5（R3） | takeover: the tick re-probe keeps probing while ANY direction is still inert | server 方向晚装不重探（双轴 R2 blocking） |

绿：`green6-flag-bue-v2-sender-identity-red.log`（EXIT=0）。另含 R2 补强断言（非红测）：
四象限 decision-composition（BUE 决策 ⊕ LMN 实机因果模型常数 = 恰 1）、per-direction partial
快照、server-late 动态场景、锚行完整格式锁定。

## 4. 门禁（工单 Scope 3 / 验收 3）

- `build-gate-release-rebuild.log`：Release `-t:Rebuild` 全解决方案，**0 error / 0 warning**。
- 七运行器全 **exit 0**：Plugin / Contracts / Network / Placement / ClientUi / Release / Settings。
- `Verify-NoUiTokens.ps1`：Core（18 文件）/ ClientUi（11 文件）双 PASS。

## 5. 双轴独立审查链（loop 第 2-3 步）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| R1 | 3 findings（1 blocking：启动探测早于 LMN 装补丁→缓存永久 false=实机未修） | 7 findings（4 blocking：归因入档/组合证明/v1compat 口径/绿日志空 + 3 should-fix） | R2 修复（tick 重探、per-direction、组合断言、文档化、green5 重采） |
| R2 | 3 findings（1 blocking：哨兵只看 client 方向，server 晚装永不重探；2 should-fix：注释死位/断言口径夸大） | 1 blocking（同哨兵缺口） | R3 修复（任一方向 inert 即重探 + 单调 latch + server-late 动态红测 + 注释迁移 + 消息降级） |
| R3 | **CLEAN**（无新 finding） | **CLEAN**（无新 finding） | 双 CLEAN，loop 闭合 |

Spec R1 两条 closed-by-documentation（复审认可拆分）：归因正式入档=本报告（结单文档动作）；
v1compat 面板口径注明=上文 §2.3（实现注释已显式，面板文案如需调整归下轮）。

## 6. 候选身份（提交后授予，2026-09-05）

候选从提交树 `b6704744012794b72a0293f2ae122b995b1f28c4`（`b670474`，含本票全部源码/测试/票面/本报告）
Release 重建，**确定性复核通过**（两轮 `-t:Rebuild` SHA-256 逐字节一致）。身份由 kit runner identity 模式
实码计算（`audit/2026-09-04/DEV-V2-07/kit/out/QualificationGateRunner.exe identity …`，配方沿 DEV-V2-07 §5）。

| 项 | 值 |
|---|---|
| CandidateBuild | `DEV-V2-12-CLEAN-20260905` |
| CaseId | `DEV-V2-12-20260905` |
| SourceSnapshotId | `b6704744012794b72a0293f2ae122b995b1f28c4`（b670474） |
| DLL SHA-256 | `B4E37FFA7581CD70CDC552242F3A01CD62FC86095C99C857AB5FC3E51B33E959`（270336 字节） |
| BuildIdentity | `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272` |
| DefinitionSetDigest | `38D66989136D008A1AE27732544F9680F35C5C75BB12BFDDA570BD5844C8B854`（与 DEV-V2-10/11 候选一致——官方定义集未动，交叉自洽） |
| ReferenceSet（Client+U3DS） | `Libs-ReferenceSet-951EFCD4E73C37E2D514B6B7D05AE8FDF2141F3C18A9C60D37192BD068775030` |
| ToolchainIdentity | `MSBuild-18.9.0.32302|.NETFramework-4.7.2|CSharp-10` |

归档：`audit/2026-09-05/artifacts/DEV-V2-12-20260905/{BetterUnturnedExperience.dll, candidate.json}`；
身份记录 `audit/2026-09-05/DEV-V2-12-dll-sha256.txt`。前置候选（DEV-V2-10 `C3A35B07…` / DEV-V2-11
`5B4E948E…`）归档原样未动。**本候选不自动继承 DEV-V2-11 的发布批准**——实机复测采证后须另走
四角色资格门禁 + 人工批准。

## 7. 待人工（关单前置）

1. 四端实机复测（开 Debug 采集），判据：
   - 零 `sender=0` 送达（V1FIX recv-from-client）；
   - 零 `dropped outbound … target=0`（LMN Error）；
   - V1/V2 双向 seq 对齐、pong 1:1（无重复送达）；
   - 锚：`v1-table-mirror result=mirrored` 在场；每会话恰一条 `v1-frame-release` 与
     `lmn2-frame-release result=released decision=lmn-native-dispatch`（替代旧委托锚）。
2. `case.json` 填实 → `QualificationGateRunner gate`（四角色）→ 人工发布批准（不继承 DEV-V2-11）。

## 8. 实机复核与资格门禁（2026-09-05 下午，闭环）

**四端实机复测全绿**（采集=用户；复核记录 `retest-verification-r1.md`，证据
`../evidence/DEV-V2-12-20260905/retest-r1/` 四包）：四端部署指纹 `B4E37FFA…` 一致；**零 sender=0 送达**；
**零 dropped outbound target=0**；**每 seq 恰一次送达**（(FIX,seq,kind,sender) 重复键四端 0，上轮
host 20 / u3ds-server 8——双投递消失）；pong 1:1（host 38/38、u3ds-server 32/32）；镜像锚 +
`v1-frame-release`/`lmn2-frame-release` 各恰一条；**delegated 无回归**（0）；P5 干净；B5/B6 可逆链在；
P3/P4a 对齐。具名观察 N-1（不阻塞）：u3ds-server 2 条 LMN 自身出站竞态 Error（target=真实 id，首 ping
早于 SteamPlayer transport 可解析；seq=6 起全中；出站路径 BUE 未触碰，非 target=0 非 F-E 范围）。
具名观察 N-2：横幅时间戳不可信（已知坑），会话真实性由部署指纹+新锚内容锚定。

**四角色资格门禁 TechnicallyQualified**（exit 0，`gate-final-r1.log`）：SinglePlayer / SteamP2PHost /
SteamP2PClient / U3dsHeadless = Fulfilled，U3dsClientUi = NotApplicable；包 canonicalDigest
`9E731F7B5689DF258F85C236089575470BF32EAE841B4A77A447BE163EED915B`（证据包
`../evidence/DEV-V2-12-20260905/`，P2P 主客成对 case 共用 CaseId `DEV-V2-12-20260905-P2P`）。

**本票四条验收全数达成 → resolved。剩余独立动作 = 人工发布批准**（对象 BuildIdentity `C9EEF3B8…` /
DLL `B4E37FFA…` + canonicalDigest `9E731F7B…`；不自动继承 DEV-V2-11 批准，由用户另行给出）。

## 9. 人工发布批准（2026-09-05，全链闭环）

用户（人工开发者）原话：**「作为人工开发，我授权批准DEV-V2-12正式关闭，感谢你的付出」**。

| 批准对象 | 值 |
|---|---|
| CandidateBuild | `DEV-V2-12-CLEAN-20260905` |
| BuildIdentity | `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272` |
| DLL SHA-256 | `B4E37FFA7581CD70CDC552242F3A01CD62FC86095C99C857AB5FC3E51B33E959`（270336 字节） |
| 证据包 canonicalDigest | `9E731F7B5689DF258F85C236089575470BF32EAE841B4A77A447BE163EED915B` |

12 链闭环：归因 → 红绿链（5 红→绿）→ 双轴 R1→R2→R3 双 CLEAN → 门禁×3 → 实机复测全绿 → 资格门禁
exit 0 → **人工批准**。该候选获正式发布资格；批准不向下自动继承（后续新候选仍须另走门禁+批准）。
