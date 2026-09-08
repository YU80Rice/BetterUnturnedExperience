# DEV-V2-20 结单报告：LHT 纳入——更好的尸潮播报（双件 + 信标守卫 + 组播广播）

日期：2026-09-08 | 票据：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-20-lht-adoption.md` | 状态：**resolved（双轴 R2 双 CLEAN）**

## 1. 交付内容

裸 BUE 的尸潮播报官方功能模块（旧独立插件 LaunchHordeTracker 迁入单 DLL，BepInEx 身份与 LMN 硬依赖按构造消失），作为 **BueNetworkApi 生产绑定的第一批真实消费者**全链走通：

- **稳定缝（T6 拍板）**：`HordeBeaconContextGuard` = 上下文守卫原则的具名化（补丁可共享原生调用点，不能共享业务上下文——只有被追踪信标的计数变更置脏，非相关路径立即放行零状态变更；脏标记唯一持有者）；`IHordeTrackingPolicy`+`DefaultHordeTrackingPolicy`（`default-horde-v1`，激活准入/outstanding 钳制/2s 兜底/60s 心跳常量收口）= 追踪策略扩展位；`IHordeHudSurface` = 表现 adapter 变体位（生产=PlayerLifeUI 反射注入，未来聊天播报/面板同缝）。
- **内部双件（一个 FeatureId 内组合）**：`HordeTrackingModule`（信标追踪/epoch/seq/广播，服务器权威；引擎触点全收口 `IHordeTrackingAuthority`——生产 `HordeProductionAuthority`、测试录制 fake，宿主测试零引擎依赖）+ `HordePresentationAdapter`（10Hz HUD，异常隔离只降表现不伤权威追踪）。
- **自有补丁（模块内部）**：`BeaconCounterPatches` 信标两枚 Postfix（勘误口径：是信标补丁，不是 LIR 补丁），Harmony ID=FeatureId（`io.github.yu80rice.bue.horde-tracker`），Stop 只 `UnpatchSelf`；`PlayerLifeUiHudPatches`（HUD 注入，CanUseClientUi 门=非 batch-mode）同 ID 同撤。
- **传输上收**：`ModTransport.Initialize` 直调、`RuntimeDependencyGuard`（LMN ABI 守卫）、全部 LMN 类型硬依赖删除；`Provider.clients` 手写循环+跳过本地身份+单机特判由 `SendToClients` 会话驱动组播**按构造替代**（established 快照不含本地主机，空快照=冻结 NoSession）；可靠度不变（Update 不可靠/Clear 可靠）。
- **业务一致性原样保留**：epoch（激活换代/旧 epoch 延迟包无法复活）、sequence（Update/Clear 共享序号空间，Clear 为 epoch 终结）、单槽 mailbox（按 (epoch,seq) 拒乱序）、脏标记（同帧击杀合并一帧）、双可靠度、停止闸门（LastBroadcastedActive 防重复 Clear + 接收闸门先关后拆）。
- **网络（功能私有协议）**：`HordeWireCodec`（负载协议 v1 自组 byte[]，fail-closed 边界校验：截断/版本/超长串/尾随字节全拒，替代旧 ModTransport.BuildNamedMessage）+ `HordeClientReceiver`（FromServer 订阅→解析→mailbox，AcceptFrames 闸门）。
- **设置与命令**：只持久化 `enabled`（ClientLocal，SettingsRuntime）；关闭=**完整停摆**（追踪退订+/horde 注销/接收闸关+mailbox·状态清/HUD 暗/频道注销），重臂全恢复；`/horde` 冷却 1.5s、HUD 10Hz 为实现常量（`HordeCommandLogic` 纯逻辑可测）；admin 权限继续交给原版 ChatManager，零权限事实源复制。
- **U3DS**：功能 Available（无头正常启动、追踪照常激活）、表现 HeadlessOnly（不装 HUD；`ReadBatchMode` NoInlining 缝隔离 ECall——Mono JIT 编译期解析引擎 ECall 的实测事实）。
- **宿主接线**：`HordeTrackerFeatureRegistration`（BORN inert，哨兵 DefinitionSetDigest 1,0,0,20，负载 "BUE-LHT-V1"）；面板条目 FeatureId 身份+中文名「更好的尸潮播报」+enabled 开关编辑路由+表现状态透传；帧工作只订冻结 `HostTick` 缝（mailbox drain→追踪周期→10Hz HUD，无自建 Update 泵），Stop 后由宿主 `UnsubscribeAll` 冻结交接。

## 2. 红绿链与审查链（全记录见 `red-evidence.md`、`review-rounds.md`）

- 红绿：编译红 32 错（独立锚点）→运行时红 4 组（ECall 环境缝：Mono JIT 编译期解析 `Application.isBatchMode`，ReadBatchMode NoInlining 修复）→ALL GREEN；CS0649 转正=HUD 隔离断言补入组 5。全套 7/7 PASS、0 警告。
- 双轴独立审查（每轮全新实例，无续用）：
  - R1：Standards **CLEAN**（3 SMELL+2 INFO）；Spec 第一次派发**作废**（模型零输出，fresh-instance 规则留痕）→重派 **NOT CLEAN**（GAP-1：HostTick 订阅句柄）。
  - F1：SMELL×3 全修（空 catch→LogWarning）；GAP-1 以冻结交接缝反驳（`BueFeatureStartRuntime.StopAll` Stop 返回后 `UnsubscribeAll(feature)`，DEV-V2-19 F3 定案，LIR 同形，DEV-V2-22 同 GAP 同反驳先例）。
  - **R2 双轴双 CLEAN：Standards CLEAN（反驳接受；4 INFO 具名可延期）/ Spec CLEAN（反驳接受，零 GAP/DEVIATION/SMELL）。**

## 3. 验证与身份

- 候选：`BetterUnturnedExperience.dll` **528384 B**，SHA-256 `3bb4452e9193f99a121bc94e086db9ebed7cef0ceb9b50fd43cff669583f6569`，三轮 `-t:Rebuild` 字节一致（`identity-rebuild1/2/3.log`、`identity-sha256-r1/2/3.txt`）；CaseId **`DEV-V2-20-CANDIDATE-20260908`**。RELEASES 换标随 DEV-V2-24 实机验收（沿 17/18/19/21/22 惯例，不继承既往批准）。
- 冲突复核：LHT 两枚信标 Postfix 目标 `InteractableBeacon.spawnRemaining/despawnAlive`——全仓 Harmony 目标扫描（BII 面板按钮/LIT 整理按钮/LIR 三补丁）**零交集**。
- 全套证据：`sln-rebuild-fullsuite.log`（0 错 0 警告）+ `fullsuite-run.log`（7/7 PASS）+ `impl-green-run.log`/`fix1-green-run.log`（锚点六组 ALL GREEN）。

## 4. 具名延期 / 边界（R2 判词载明，均不阻断）

1. **HordeTrackerModule 拆除路径空 catch**（半注册回滚/Dispose/UnregisterChannel/UnpatchSelf）——与 LIT/LIR 拆除隔离同形，非诊断吞异常类。
2. **OwnerResolver TraverseCreate NRE 边**——旧源保真（F1 前即有），已加 Warning 留痕，控制流未变。
3. **PlayerLifeHudSurface Middle Man**——表现 adapter 变体位的既有形态，变体增多时随治理票收敛。
4. **BindNetwork 失败无独立就绪观测位**——同构 LIT MultiplayerReady 降级语义，可后续补。
5. **实机自验**（HUD 注入真机/信标计数实时性/组播三环境/真实 Steam 传输不可靠通道）绑 DEV-V2-24 终票三环境验收，与本票候选身份对账。
6. 旧类型名随迁入消失（HordeStatusNetwork→HordeClientReceiver+HordeWireCodec、HordeStatusBroadcaster→HordeTrackingModule.Tick、HordeEventNotifier→折入 HordeProductionAuthority、RuntimeDependencyGuard/RuntimeEnvironment/ThreadContext→按 spec 与 LIT 先例删除/折入）；旧频道 `io.github.yu80rice.launchhordetracker.horde-status` 退役。

## 5. 档案清单

`compile-red.log` / `impl-build1..6.log` / `impl-green-run.log` / `fix1-build.log` / `fix1-green-run.log` / `sln-rebuild-fullsuite.log` / `fullsuite-run.log` / `round1/2-increment.diff` / `identity-rebuild1..3.log` / `identity-sha256-r1..3.txt` / `red-evidence.md` / `review-rounds.md` / 本报告。
