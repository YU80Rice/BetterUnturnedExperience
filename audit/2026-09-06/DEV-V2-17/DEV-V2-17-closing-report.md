# DEV-V2-17 实施结单报告——平台：自动握手与会话生命周期（Q5）

日期：2026-09-07（实施跨 2026-09-06/07）。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-17-auto-handshake-session-lifecycle.md`（Blocked by DEV-V2-16，已 resolved）。规约：`docs/agents/output-review-loop.md`（Fresh-instance 规则版）红测先行 + 双轴独立审查 CLEAN 才交付。规格：`.scratch/bue-v2-phase2-official-adoption/spec.md`「平台：会话建立 = 网络模块自动握手（T3 决策 5）」及「平台：网络模块停用时的方法级语义」（104 行）。

## 交付

- **传输缝生命周期扩员（Core 内部，非契约面）**：`INetworkTransport` 新增 `PeerConnected` / `PeerDisconnected` 事件与 `ConnectedPeers` 快照（`src/BetterUnturnedExperience.Core/Network/LocalLoopbackTransport.cs`）；三个实现者同步补成员——LocalLoopback 另备 `ConnectPeer`/`DisconnectPeer` 测试助手，HostNetworkTransportAdapter 与 Transport/LmnTransportAdapter 声明成员（生产绑定属 DEV-V2-18，pragma 0067 注记沿 ForeignSessionStub 先例）。
- **自动握手（`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs` 主体重写）**：transport connected → 运行时自动发 Hello（单帧 untargeted、可靠位）→ 服务器 Ack（响应方同步建立）/ Reject（版本 fail-closed）→ Connected 仅握手完成后触发——功能模块零握手负担。构造器新参：`handshakeInitiator`（默认 true=发起方；响应方传 false，角色决定连接事件是否触发主动握手）、`monotonicMilliseconds`（可注入时钟，默认 Stopwatch 基）。
- **控制帧线形（内部，帧从未上线零兼容代价）**：载荷 12→20 字节 `[steamId 8][major 2][minor 2][nonce 8]`；nonce=发起方 SessionId=连接代际，Ack/Reject 原样回带；Hello untargeted(0)，Ack/Reject 定向 target=发起方；**peer 归属权威=帧头 sender**（与 DEV-V2-16「按源解析」同源），payload steamId 字段仅声明性。
- **Ack/Reject 匹配语义冻结**：按「帧头 sender + nonce + 未建立 + FromServer 方向」精确匹配，废弃「第一个未建立会话」匹配法（跨会话误投 Ack 不建立）；发起方对契约 Major 不符的 Ack fail-closed（拆除 pending、不复活）；Reject 精确拆除匹配 pending（另一条握手不受影响、版本拒绝不重试）。
- **运行时职责全落**：重复 Hello 幂等重 Ack（不产第二会话）；断线清理（PeerDisconnected 移除该 peer 全部会话，established→Disconnected 锁外，pending 静默消失，无 ghost）；漏断线替换（新连接事件/新 Hello nonce 替换旧会话）；**重连换代际**（每 peer 单活会话不变式；被替换的死会话对象在后继建立时收到 `GenerationChanged(新代际)`——supersededByPeer 机制统一覆盖断线重连/漏断线替换/模块开关三路）；超时重探退避 1s 起步倍增 8s 封顶（`TickHandshake` 为 runtime 级公开驱动缝，沿 SetModuleActive 先例非契约面）。
- **模块重启用重建（规格 104 行）**：`SetModuleActive(true)` 后发起方按 `ConnectedPeers` 重探自动重握手；响应方对无会话的已连对端发**复位 Reject（nonce=0）**，发起方 HandleReject 落入 reset 分支拆除 stale established 并自愈重握手——双向收敛；模块开关本身仍零生命周期事件（DEV-V2-14 冻结保持）；订阅/频道表跨开关存活（红测钉住「已挂订阅无需重挂」）。
- **锁纪律（兑现 DEV-V2-16 具名延期 #9）**：握手控制帧（Hello/Ack/Reject）发送全部移出状态锁；Connected/Disconnected/GenerationChanged 回调锁外；可注入时钟采样四处全部锁外（R1-Standards BLOCKING 修复，nowMs 参数下传）；`SubscriptionRecord.Removed` 改 volatile（R1-Standards SMELL 修复）。
- **Contracts 仅注释**：`IConnectionSession` 补 17 事件时机语义（Connected 仅建立后/Disconnected 仅 established 后/GenerationChanged 于被替换对象）；`IBueNetworkApi` 契约面零变更，契约维持 2.0，SDK 演化节免登记（经 R2/R3-Spec 双轮核验成立）。
- 生产 DLL 零行为外溢面：LIT 单人路径不触网络会话（DEV-V2-15 红测既有断言保持）；既有测试全兼容（下节）。

## 红绿链（证据留盘 `red-compile-build.log` / `stub-stage-build.log` / `red-runtime-transcript.log` / `red-fixround-transcript.log` / `fix-round-build.log` / `fix2-round-build.log` / `green-*.log`×7）

- **红0（编译红）**：`--bue-v2-handshake-red` 测试先行（引用未存在的 `TickHandshake`/`handshakeInitiator` 参数/生命周期接口成员）→ 104 错（CS1061×84、CS1729×2、CS1739×18，`red-compile-build.log` 逐字）。
- **红1（运行时红，8 组收集式）**：仅加缝桩（接口成员+空 TickHandshake+空生命周期 handler）后，红测一次捕获 **26 条红**（`red-runtime-transcript.log`），覆盖验收五组+锁外+重启用：流程/时序、去重、匹配、断线与重连换代际、fail-closed、退避、锁外回调、重启用重建（含响应方复位自愈）。
- **修复轮红（R1-Spec BLOCKER 后）**：新断言「身份：Ack 的 peer 归属以帧头 sender 为权威（payload steamId 声明不可伪造归属）」在 R1 代码（payload 匹配）上红 1 条（`red-fixround-transcript.log`）→ 修复后绿。
- **修复轮2（R1-Standards 后）**：时钟采样移锁外 + volatile 属结构修复，无新可观察语义，无新红路径；全套 7/7 PASS 0 警告回归保持（`fix2-round-build.log`）。
- **绿**：修复后旗标 0 退出；全套 7/7 PASS（`green-BetterUnturnedExperience.{ClientUi,Contracts,Network,Placement,Plugin,Release,Settings}.Tests.log`），构建 0 警告 0 错误（`fix2-round-build.log`）。

## 与既有测试的相容性核查

- `AssertBueNetworkRuntime`（03 票 Q4/S3）：StartSession/Reject 流程在 nonce 匹配与 20 字节载荷下断言全部保持绿。
- `AssertBueV2SessionDrivenSendSemantics`（16 票）：hub 假传输按 target 路由，Ack 定向化（target 0→peer）后 trio/锁探针组全部保持绿（逐一定向 Data 路径未动）。
- `AssertBueV2DirectionalSubscribe`（14 票）re-arm 段一行更新：`a.StartSession(2002UL)` 对已 established 会话在 17 语义下=换代际替换并返回新 pending，断言改用 `aRearmedSession`（补「替换后恰一条会话」断言）；14 原意「订阅无需重挂」（disabledHits==1）忠实保留，经 R2/R3-Spec 双轮核验。
- `AssertBueV2NetworkInjection`（14 票）：停用语义/注入面零变化，保持绿。

## 修复轮（R1 → 修复 → R2/R3）

- **R1-Spec BLOCKER（帧头身份）**：HandleHello/HandleAck/HandleReject 原 peer 校验取自 payload steamId（可伪造）；修复为帧头 sender 权威（与 DATA 派发同源），payload steamId 降级声明性，新增红测钉子先红后绿。
- **R1-Standards BLOCKING（时钟锁内调用）**：`monotonicMilliseconds()` 在 `sync` 锁内被调用（TickHandshake/CreateInitiatorSessionLocked 两路四处）；修复为方法开头锁外采样、`nowMs` 参数下传（monotonic 提前采样仅使重探期限更保守）。
- **R1-Standards SMELL（Removed 竞态）**：`SubscriptionRecord.Removed` 锁内写/锁外读无同步；修复为 `volatile bool` 带契约注释。
- 两轮修复后全套 7/7 PASS 0 警告，候选身份刷新为最终值。

## 候选身份

`BetterUnturnedExperience.dll` SHA-256 `4f303e4426868a48daa62a6ef41056f4201bfa4543b1d38f0fd8305654ce8ca2`（356352 字节，修复后两轮 `-t:Rebuild` 逐字节一致，`identity-rebuild1.txt`/`identity-rebuild2.txt`）。不继承 14/15/16 发布批准；RELEASES 换标随实机验收（DEV-V2-24 或用户实机验收）。

## 具名延期 / 边界声明

1. **生产 transport 生命周期绑定**：HostNetworkTransportAdapter/LmnTransportAdapter 的事件 raise 与 `ConnectedPeers` 实装、`TickHandshake` 的生产泵接线 = **DEV-V2-18**（R1/R2/R3 三轮独立具名一致）。本票以假 transport 验证全部语义；DEV-V2-18 交付前不得宣称生产自动握手完成。
2. **`IConnectionSession.Send` 仍为 `NoSession` 桩**（沿 DEV-V2-16 具名延期 2，契约面未动）。
3. **生产会话创建路径**（宿主启动/模块 Start 接线）沿 DEV-V2-14 具名延期，属 DEV-V2-21/22。
4. **锁外探针覆盖面**：锁探针打 Connected/Disconnected 两事件（GenerationChanged 与其同构锁外，未单列探针）；探针 2s/5s 超时窗口的调度极差假红可能沿 DEV-V2-14/16 同模式具名。
5. **审查破例登记**：Standards 轴两轮由 Spec-Reviewer 类型的全新实例按完整 Standards 清单执行——standards-reviewer 类型连续 6 次派发失败（`Upstream response stream interrupted`×5 + `Model request failed`×1，其绑定模型上游持续不可用；同期 Spec-Reviewer 类型多次成功），按预先宣告并经用户认可的兜底路径执行，Fresh-instance 规则本体（每轮全新 spawn、独立重推导、禁续用）全程保持。
6. **派发故障留痕**：Standards 轴 R1 前有 5 次无效派发（未产出任何判词，不构成审查轮）；Spec 轴 R1 判词产出后修复轮与 R2/R3 均为 fresh 实例。

## 审查链

- **R1**（2026-09-06/07，双轴独立）：Spec（fresh，`R1-spec.md`）**NOT CLEAN**——BLOCKER 1（控制帧 peer 身份取自可伪造 payload steamId）+ INFO 1（生产接线延期 18）；Standards（fresh 兜底实例，`R1-standards.md`）**NOT CLEAN**——BLOCKING 1（时钟锁内调用）+ SMELL 1（Removed 竞态）+ DEFERRABLE 1（adapter→18）。两轴发现互不重叠，全部成立。
- **修复轮**（2026-09-07）：帧头权威（+修复轮红 1 条留证）、时钟采样移锁外、volatile Removed；修复后全套 7/7 PASS 0 警告，候选刷新 `4f303e44…8ca2`。
- **R2**（2026-09-07，双轴全新实例）：Standards（`R2-standards.md`）**CLEAN**（修复核验成立；DEFERRABLE 1 并入具名延期 1）；Spec（`R2-spec.md`）**CLEAN**（R1 BLOCKER 修复核验成立；INFO 1=延期 18）。
- **round3 增量**（时钟/volatile 结构修复）后 **R3**（2026-09-07，fresh，仅 Spec 终审：确认 R2 CLEAN 依赖面无语义破坏+验收四条逐条对证据）：`R3-spec.md` **CLEAN**（INFO 1=延期 18）。
- **双轴最终 CLEAN（Standards=R2，Spec=R3）**，最终候选 `4f303e44…8ca2` 全链一致。
