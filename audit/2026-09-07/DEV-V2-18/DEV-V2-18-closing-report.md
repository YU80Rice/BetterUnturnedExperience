# DEV-V2-18 实施结单报告——平台：BUE 帧生产消费绑定 + LMN 接管 seam 拆分 + 魔数 BUE1（Q3）

日期：2026-09-07（/implement 会话）。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-18-bue-frame-binding-takeover-split-bue1.md`（Blocked by DEV-V2-17 已 resolved）。规约：`docs/agents/output-review-loop.md`（Fresh-instance 规则版）红测先行 + 双轴独立审查 CLEAN 才交付。规格：`.scratch/bue-v2-phase2-official-adoption/spec.md`「平台：BUE 帧消费 seam 与 LMN 接管 seam 拆分」（106-112 行）及「平台：线帧魔数与频道政策」（125-129 行）。

## 交付

- **决策核六步冻结（`src/BetterUnturnedExperience.Plugin/NetworkModuleAdapter.cs` ShouldConsumeInbound 主体重写）**：① 识别 BUE 帧 → ② 网络模块开 → BUE 消费（BUE seam，门=模块开关本身）→ ③ 识别 MOD/LMN2（LMN 接管 seam，armed=模块开+探针 true）→ ④ LMN live → 放行原生（每帧恰一次由 LMN 派发）→ ⑤ LMN inert → BUE 处理（V1 兼容路由/LMN2 路由器委托）→ ⑥ 非目标帧交还 vanilla。**两 seam 零共享**：无公共 takeover 布尔、无公共分支，BUE 帧永不落入 live 释放；live/inert 保持 DEV-V2-12 每方向独立判断。
- **BUE 帧识别单源（`src/BetterUnturnedExperience.Core/Network/BueFrameClassifier.cs` 新建）**：`FrameMagic="BUE1"` 公开常量为帧魔数唯一源，`MagicBytes` 由常量串派生（无并行字节常量漂移面）；`IsBueFrame` offset-aware 零拷贝窗口语义与 LmnFrameClassifier 同构；`BueNetworkRuntime` 的编码（SendFrame→MagicBytes）与接收（OnReceive→FrameMagic 比对）全部归并此源。
- **patch 安装门解耦（规格旧规则改写落地）**：`ApplyNetworkPatches` 门 = `networkEnabled` 单一开关（原 `&& coordinator.TakeoverActive` 废除）——裸 BUE（探针 false）时 BUE 自己的 patch 照装；探针 false → LMN seam 零 patch/零反射/零镜像：镜像与 live 探测的全部三个调用面（ActivateCore/RefreshSwitches/ApplyNetworkPatches 安装成功路径）均以 `coordinator.TakeoverActive` 加门，测试以计数探针钉「零探测调用」。开关关闭 → `UnpatchSelfSafe`（诊断 token 按世界分支：`hand-back-to-lmn`/`hand-back-to-vanilla`）+ 运行时停用。
- **异常 hand-back 不吞原生流量（决策核结构性保障）**：ShouldConsumeInbound 顶层级 try/catch——任何异常浮出为 `event=inbound-decision result=failed decision=hand-back` 故障行并返回 false（帧交还 vanilla，游戏接收泵永不因 BUE 决策中断）。
- **生产传输绑定（本轮核心新增，衔接 DEV-V2-16/17 语义）**：
  - `src/BetterUnturnedExperience.Plugin/BueEngineNetBinding.cs` 新建：`BueEngineNetBinding`（五委托+可选注入时钟的引擎绑定面，tests 桩/production 静默反射二选一）+ `BueEngineNet`（按名静默反射解析器：Provider.clientTransport 私有字段（LMN NetReflectionHelper.cs:31 权威）、Provider.isServer/isConnected/server/client/clients、SteamPlayer.playerID→SteamPlayerID.steamID→CSteamID.m_SteamID 链、SteamPlayer.transportConnection（基类公共属性））。**五个解析器+Send 全部 never-throw**（TryGetConnectionSteamId 先例）：引擎不可解析/游戏发送崩溃一律降级安全默认值（false/0/空/send-failed）；SDG.NetTransport 接口编译期（IClientTransport.Send/ITransportConnection.Send，ENetReliability 两值可靠性），免 Steamworks/免 Assembly-CSharp 编译引用的政策保持。
  - 出站：target=0 → clientTransport.Send（客户端→服务器 untargeted，与入站 ReceiveMessageFromServer 前缀同管）；target≠0 → 逐 clients 找 SteamPlayer.transportConnection 定向发送（服务器→客户端）。入站：决策核 BUE 分支物化帧字节喂 `HostNetworkTransportAdapter` 队列。
  - `HostNetworkTransportAdapter` 生命周期实装（Core，纯 C#）：`PeerStateSource`（引擎对端快照源，tests 直绑/production 绑引擎解析器）、`PollPeerState()` 差分（lastPolledPeers 对比→raise PeerConnected/PeerDisconnected；raise 抛出则快照不前进，下轮重raise，运行时按 peer 幂等自愈）、`ConnectedPeers` 活源查询（服务重启用 reconcile 重探面）、`RaisePeerConnected/Disconnected` 测试缝（公开面正当性=Plugin.Tests 只见 Core 公开面，沿 LocalLoopbackTransport.ConnectPeer 先例；pragma 0067 撤除）。
  - 泵与武装（`TickNetwork`，插件 Update 驱动，headless 含）：四级各带 try/catch 故障隔离（arm/peer-state/dispatch/handshake），故障浮出 `event=bue-runtime-pump result=failed stage=…` 结构化行不逃逸 Update 链。武装一次成型：角色首决即决（server=isServer 即决；client=有服务器对端即决；菜单不决）；本地身份 0 → fail-closed 不武装（一次性 `reason=local-identity-unresolved` 故障行，帧头 sender 0 中毒防御）；`handshakeInitiator=!serverRole`、注入时钟下传（握手退避测试缝）；武装成功行 `event=bue-runtime-arm result=armed role=… localSteamId=…`。
  - 开关联动：`RefreshSwitches` off→`DisarmBueRuntime`（SetModuleActive(false)，DEV-V2-14 停用语义）；on→`RearmBueRuntime`（泵级隔离：PollPeerState 刷新快照→SetModuleActive(true) 按 ConnectedPeers 重建，DEV-V2-17 重启用语义）。
- **魔数 BUE2→BUE1 全仓清扫**：常量/注释/测试 pin（BuildControlFrame '2'→'1'）全改；token 扫描 `src/tests/docs/CONTEXT.md` 零残留（audit/.scratch 历史档案按票面裁量不扫，Spec 轴 R1 裁定正当）。数字频道只兼容不注册重申：无任何新数字频道注册入口（Spec 轴 R2/R3 独立复核确认）。

## 红绿链（证据留盘 `red-compile-build.log` / `stub-stage-build.log` / `red-runtime-transcript.log` / `green-stage-build.log` / `green-frame-binding-transcript.log` / `fix-round-build.log` / `fix-round-transcript.log` / `round3-build.log` / `round3-transcript.log` / `round3-*.Tests.log`×7）

- **红0（编译红）**：`--bue-v2-frame-binding-red` 测试先行（引用未存在的 `BueFrameClassifier`/`BueEngineNetBinding`/`NetworkApi`/`TickNetwork`/adapter 新参）→ CS0246（`red-compile-build.log` 逐字）。
- **红1（桩级运行时红，收集式 9 条）**：仅加缝桩（分类器 IsBueFrame=false、NetworkApi=null、TickNetwork 空、patch 门维持旧规则）后，红测一次捕获 **9 条红**（`red-runtime-transcript.log` 逐字），覆盖五组：帧分类×2、六步顺序（探针 false 不阻 BUE seam——旧决策核全分支压一个 takeover 布尔处必红）、patch 门（探针 false 安装仍被尝试——旧规则零输出处必红）、异常隔离×3、生产绑定E2E×2；「live 恰一次」组为 DEV-V2-12 冻结语义回归钉子（桩阶段即绿，非新语义）。
- **中间修正（转绿路上）**：①实现缺陷——Arm 传 binding 时钟而非 adapter 注入时钟（退避在真实 Stopwatch 上永不到期→自愈红），修正为 `injectedMonotonicMilliseconds ?? engineBinding.MonotonicMilliseconds`；②测试缺陷——六步组缺 RegisterChannel/会话建立（派发按帧头 sender 找不到会话）、E2E 组 client 未武装即 RegisterChannel，补 `BuildBue1HelloFrame` 直驱握手并重排组内顺序。
- **绿**：`--bue-v2-frame-binding-red` 旗标 0 退出；全套 7/7 PASS（`green-*.Tests.log`×7）；构建 0 警告 0 错误（`green-stage-build.log`，「0 个警告」汇总行）。
- **修复轮（R1 双轴后）**：探测加门/Rearm 隔离/never-throw/MagicBytes 单源 + 测试增补（网络关闭组/计数探针/绿态成功行）→ `fix-round-build.log` 0 警告 + `fix-round-transcript.log` ALL GREEN（成功行列七组名，兑现 Spec GAP 正向证据）+ 7/7 PASS。
- **round3（R2 SMELL 后）**：token 分支/latch 注释/patch 门注释+组末 IsolateAndDetach → `round3-build.log` 0 警告 + `round3-transcript.log` ALL GREEN + 7/7 PASS（`round3-*.Tests.log`×7 全 EXIT=0）。

## 审查链（Fresh-instance：每轮全新 spawn 双实例，续命=违规）

- **R1**（2026-09-07，双轴独立）：Spec（fresh，`R1-spec.md`）**NOT CLEAN**——DEVIATION 1（ApplyNetworkPatches 安装成功路径无条件 RefreshLmnNativeDispatchLive→探针 false 仍有 LMN patch-info 反射）+ GAP 1（绿态旗标 transcript 空文件，缺正向逐组证据）+ INFO 1（清扫范围裁量正当）；Standards（fresh 兜底实例——首次派发上游流中断未产出判词不构成审查轮，重派后产出，`R1-standards.md`）**NOT CLEAN**——BLOCKING 3（同探测面/RefreshSwitches→RearmBueRuntime 无隔离+解析器未 never-throw/「网络关闭不消费」无红测钉）+ SMELL 2（魔数并行字节常量漂移面/静态 latch 与 Raise* 注记不诚实）。两轴发现高度收敛，全部成立。
- **修复轮**（2026-09-07）：五组修复 F1-F5——安装路径探测加门、RearmBueRuntime 泵级隔离（stage=rearm）、BueEngineNet 五解析器+Send never-throw（TryGetConnectionSteamId 先例，注释契约同步改写）、MagicBytes 由 FrameMagic 派生单源化、测试增补（「网络关闭」组：不消费/零会话/NoSession/重开恢复；计数探针钉零探测调用；绿态成功行）。修复后旗标 ALL GREEN + 7/7 PASS 0 警告。
- **R2**（2026-09-07，双轴全新实例）：Standards（`R2-standards.md`）**CLEAN**（SMELL 3 列名不阻断：Unpatch token 双 seam 语义/patch 门组注释与残留隔离/latch 注记准确性；INFO 2 保持原状）；Spec（`R2-spec.md`）**CLEAN**（验收五条全 PASS，零发现）。
- **round3 增量**（三条 SMELL 全按建议修复：token 按 `coordinator.TakeoverActive` 分支、latch-before-populate 注记、patch 门注释改写+组末 IsolateAndDetach）后 **R3**（2026-09-07，双轴全新实例）：Standards（`R3-standards.md`）**CLEAN 零发现**（并核验 token 分支正确选用了 coordinator 布尔而非 adapter 布尔——选错会把有 LMN 世界误标 hand-back-to-vanilla）；Spec（`R3-spec.md`）**CLEAN 零发现**（验收五条全 PASS）。
- **双轴最终 CLEAN（Standards=R3，Spec=R3）**，最终候选 `0f2336e8…c7b` 全链一致。

## 候选身份

`BetterUnturnedExperience.dll` SHA-256 `0f2336e83b8a927ba3f7c75a08fa50e59dd893297262b20db8ef9e382c489c7b`（364544 字节，round3 后两轮 `-t:Rebuild` 逐字节一致，`identity-rebuild1.txt`/`identity-rebuild2.txt`）。不继承 14/15/16/17 发布批准；RELEASES 换标随实机验收（DEV-V2-24 或用户实机验收）。

## 与既有测试的相容性核查

- DEV-V2-04..06 V1 接管面（AssertBueTakeover/AssertBueConfigMigration/AssertBueV1MirrorTiming/AssertBueV2SenderIdentity）：决策核六步重写在 MOD/LMN2 分支保持原判定结果与诊断行；probe-false 适配器对 MOD/LMN2 帧仍放行（LMN seam 整体门控）——全绿。
- DEV-V2-12/16/17 网络面（AssertBueV2DirectionalSubscribe/AssertBueV2SessionDrivenSendSemantics/AssertBueV2AutoHandshakeLifecycle）：运行时主体仅魔数归并（BueFrameClassifier.FrameMagic/MagicBytes）与文档变化；17 的 BuildControlFrame 测试 pin 魔数字节随清扫同步 '2'→'1'，握手全链绿。
- DEV-V2-10 kill-switch 生命周期（AssertBueNetworkKillSwitchLifecycle）：patch 门新规则下既有断言（off 零动作/重开 re-arm）语义保持，全绿。
- DEV-V2-15 LIT 单人路径：零接触网络会话，全绿。

## 具名延期 / 边界声明

1. **同进程角色翻转**：运行时一次成型、角色首决固定（client 先连服后开房保持 initiator，响应方职责永不触发→握手不收敛）。冻结的「同 API 实例」身份约束不可换运行时；每进程一角色的实机面随 DEV-V2-24 三环境矩阵验证。
2. **`IFeatureBootstrap.Network` 生产交付**：adapter 以内部 `NetworkApi` 持有运行时；FeatureBootstrap 生产构造与 Start 接线属 DEV-V2-21/22（DEV-V2-14 具名边界沿袭）。
3. **patch 真装/移除的生产证据**：测试宿主 TypeByName 对 Assembly-CSharp 的可解析性不保证，patch 安装在宿主内 fail-closed；「网络关闭 patch 被移除」的 Harmony 真卸载证据（`result=removed` 诊断）随 DEV-V2-24 实机采集；测试宿主钉行为面（不消费/停用语义/重开恢复）。
4. **引擎绑定的实机核验清单（随 24）**：客户端侧 `Provider.client`=本机 id、`Provider.server`=对端服务器 id、`isConnected` 连接真值、clientTransport 非空窗口、server→client transportConnection 查找——均以 LMN 源码为权威推断，实机帧头 sender/会话归属日志为准。
5. **发送频率与主线程模型**：引擎访问（peer 快照/发送）声明为 main-thread-only（泵/Update/面板路径）；未声称 off-main send（R2 SMELL 注记修正）。
6. **`IConnectionSession.Send` 仍为 `NoSession` 桩**（沿 16 具名延期，契约面未动）。

## 派发故障留痕

- Standards 轴 R1 前有 1 次无效派发（`Upstream response stream interrupted`，未产出判词，不构成审查轮）；重派后产出。
- 全部审查轮（R1 双轴/R2 双轴/R3 双轴）均为全新实例 spawn，无 SendMessage 续用、无持久会话复用；standards-reviewer 与 Spec-Reviewer 类型本轮均可用（17 的兜底破例未再触发）。

## 验收条件对账（票面五条）

1. 红测先行六面（四类帧分类/六步顺序/网络关闭不消费且 patch 移除/live 每帧恰一次/探针 false 零 LMN 动作/异常 hand-back）——红0 编译红+红1 运行时红 9 条→绿；「网络关闭」组与计数探针为 R1 审查后补强，随修复轮先红后绿路径核验成立。**达成**。
2. 生产传输绑定端到端：假 transport 下双向收发全链绿（订阅→帧上线→派发，「生产绑定E2E」组）。**达成**。
3. 魔数清扫零残留（token 扫描 src/tests/docs/CONTEXT.md 零命中）；帧编解码回归通过（17 握手线形 pin+E2E 往返）。**达成**。
4. V1 兼容层与既有接管测试零回归：全套 7/7 PASS。**达成**。
5. 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN（Standards=R3、Spec=R3）。**达成**。





