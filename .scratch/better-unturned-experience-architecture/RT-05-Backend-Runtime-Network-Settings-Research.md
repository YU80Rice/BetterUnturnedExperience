# GPT-RT-05：后端运行时、网络与设置研究

> 作者：GPT  
> 研究票据：RT-05  
> 共享契约：`BUE-V1-RT01-20260824`（accepted-frozen）  
> Initial SourceSetId：`BUE-SS-20260824-01`  
> Migrated SourceSetId：`BUE-SS-20260824-02`（2026-08-24；U3DS BepInEx 5.4.23.5 references 已冻结；LMN 44-file ordinal digest 已纠正冻结）  
> 状态：研究完成；独立审计 PASS；Gemini 消费复核 ACCEPT；SourceSet successor 已批准；等待 `SCR-RT05-001` 原型与裁定  
> 证据边界（迁移后）：本文包含 `SOURCE_CONFIRMED`、client/U3DS reference `IL_CONFIRMED`、U3DS BepInEx 引导链 `RUNTIME_CONFIRMED` 与 `UNRESOLVED`；没有 BUE Candidate DLL、BUE/LMN 插件运行或发布证据。

## 1. 结论先行

1. 客户端固定 BepInEx 5.4.23.5 会扫描插件、按依赖拓扑排序、`Assembly.LoadFile` 后把插件类型 `AddComponent` 到常驻 manager object。LMN 自身在 `Awake` 初始化 transport、应用两个 Harmony Prefix，在 `Update` 排空主线程队列，在 `OnDestroy` 解除自身 patch 并清理 transport。
2. 独立 U3DS 的 BepInEx 5.4.23.5、Preloader 与 `Assembly-CSharp` 已进入 `BUE-SS-20260824-02`，可作为 U3DS `IL_CONFIRMED` 输入；BepInEx 引导链另有一次 `RUNTIME_CONFIRMED`。这仍不扩张 Contracts：任何 Unity、BepInEx、Harmony、Steamworks、LMN、SDG 或 UI 类型都不得越过 Contracts。
3. LMN V5 命名频道会验证 ASCII channel、限制业务 payload 为 `1..61440` bytes、把 `reliable=true` 映射为 `ENetReliability.Reliable`，服务端 sender 来自 `ITransportConnection.TryGetSteamId` 而非 payload。收包 payload 会复制后进入 `MainThreadDispatcher`，业务 handler 由插件 `Update` 调用。
4. LMN 的 handler 注销、session dispose 与全局 shutdown 都不能取消已捕获进 dispatcher 的 Action。更严重的是，命名 handler 入队只捕获 `CSteamID`，不捕获 connection/session identity；同一 SteamID 快速重连时旧 Action 可能被错误归到新连接。冻结的 Ready 后普通消息又不携带 connection generation。此问题需要 Shared Contract Change Request，不能在 adapter 内静默规避。
5. LMN 内置 handshake 只是 channel/version 交集原语：固定源码中没有生产调用 `BuildHelloPacket` 的发起链，也不实现 BUE 的 nonce、snapshot chunk、`SessionReadyEvent` 或状态机。BUE 必须实现已冻结的应用层能力握手，不能把 LMN `IsHandshakeComplete` 当成 `SessionReady`。
6. BepInEx `ConfigFile.Save()` 直接用 `StreamWriter(..., append:false)` 覆盖目标，不是原子替换；`Reload()` 直接 `ReadAllLines`。Unturned `ServerSavedata` 会先把旧文件移到 `~` 备份再写新文件，也不是单一步骤事务。SettingsRuntime 必须拥有版本化文档、同目录临时文件、flush、原子替换/回退、迁移、损坏隔离与完整快照发布，不能把 BepInEx Config 当权威事务存储。
7. `ClientPreference` 与服务器政策是两个事实。`ServerPolicyWithClientPreference` 的 effective 值只存在于当前 session overlay；服务端政策不得覆盖或持久化进客户端偏好。连接代际变化必须丢弃 overlay、pending request 和旧快照。
8. Runtime Admission、SettingsRuntime、Lifecycle、Negotiation/Connection 各自拥有独立事实与 generation。只有 Lifecycle 写 `FeatureState/StateRevision`；连接 generation 不得代替 lifecycle、settings revision 或 catalog generation。
9. 单模块启动、设置迁移或 callback 故障默认隔离单模块；catalog/核心契约/核心 dispatcher 无法建立才进入 Core SafeMode。网络插件缺失、握手超时或远端不兼容是能力降级，不是核心崩溃。

## 2. 冻结输入与 SourceSet 勘误

### 2.1 已核对身份

| Component | Identity | Evidence |
| --- | --- | --- |
| U3-SDK | `D:\Agent-工作目录\U3-SDK`; commit `ea7b4973af5ba10f62baad2bfde36ab2e5b060eb` | `SOURCE_CONFIRMED`; tracked source clean，只有未跟踪 `audit/` |
| Client `Assembly-CSharp.dll` | `Assembly-CSharp, Version=0.0.0.0`; SHA-256 `E1146353E5C9BFF901EE94829640D88919C5E89F6A6B90B22C73ABF5C1608F94` | `IL_CONFIRMED` 可用于客户端，不用于 U3DS |
| Client `BepInEx.dll` | `BepInEx, Version=5.4.23.5`; SHA-256 `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70` | `IL_CONFIRMED` 可用于客户端，不用于 U3DS |
| LMN V5 source | base commit `e50082f983ac7cd33e008cca1a1a1dc3d9498d3f`; 44-file legacy digest `7151D22EF361B560F44A32963F82CD973AF64D7721F9D109C5492D1D7D864DE6` | `SOURCE_CONFIRMED`; 必须逐文件 hash 引用，因为 worktree 有受保护演进 |
| U3DS refs | `Assembly-CSharp.dll` SHA `1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A`；`BepInEx.dll` 5.4.23.5 SHA `8255B28902886085C578B9E427D3073C97002DB85176D2090CDEDA90EF14CE70`；Preloader SHA `55D3895351A9D16B63B6F35F1C01B44AC650979E853D0BD3A442B92A082AF64F` | `BUE-SS-20260824-02` 冻结 IL references；BepInEx 引导日志另见部署复核报告 |

### 2.2 manifest 算法文本与历史实现漂移

RT-01 写明“ordinal 路径排序”，但冻结 digest `7151...` 实际可由当前 PowerShell `Sort-Object -CaseSensitive` 的文化排序复现。对相同 44 个文件、相同相对路径和逐文件 hash，使用真正的 `[Array]::Sort(..., [StringComparer]::Ordinal)` 得到：

```text
4F290955FCDA53BFF54E2983BECA4B08337D29266D5C3FB48BD75A4BFA62F2AF
```

这不是已证明的文件变化，而是 manifest 算法文字与历史计算实现不一致。初始研究按 `BUE-SS-20260824-01` 的实际冻结内容与 legacy digest执行；随后 `BUE-SS-20260824-02` 已按真正 ordinal digest 发布，旧 SourceSet 仍作为历史身份永久保留。

## 3. 客户端与 U3DS 初始化、Harmony、异常和关闭

### 3.1 客户端 BepInEx 固定 IL

客户端 `BepInEx.dll` 的 `BepInEx.Bootstrap.Chainloader.Start()`：

- 创建 `BepInEx_Manager` 并 `DontDestroyOnLoad`；
- Cecil 扫描 `Paths.PluginPath`，解析 metadata、process、dependency 和 incompatibility；
- 按依赖拓扑排序；
- 对每个插件 `Assembly.LoadFile(location)`、`GetType(typeName)`、`ManagerObject.AddComponent(type)`；
- 单插件加载异常被捕获并记录，不自动证明其他插件安全；外层异常被记录为 fatal。

`BepInEx.BaseUnityPlugin` 构造器要求 `[BepInPlugin]`，建立 scoped logger，并把默认 Config 指向 `Paths.ConfigPath/<GUID>.cfg`。`Paths.SetExecutablePath()` 把 ConfigPath 设为 `<game root>/BepInEx/config`。

这些是客户端固定 DLL 的 `IL_CONFIRMED` 事实。Unity 何时调用各插件 `Awake/Update/OnDestroy` 仍需客户端/U3DS 运行指纹确认；不能仅凭 Unity convention 升级为运行事实。

### 3.2 LMN 固定源码生命周期

| Phase | Fixed source | Direct fact | Boundary |
| --- | --- | --- | --- |
| Awake | `Core/LaunchMultiplayerNetPlugin.cs:50-74`, SHA `03016FC47252450730896093DA831E79E8120366E7ECC2C115F67C0A68BF574E` | 常驻 GameObject；`ModTransport.Initialize()`；建立专属 Harmony；应用 client/server receive Prefix；任一自检失败则 `IsOperational=false` 并 disable component | 没有覆盖整个 Awake 的总 try/finally；BUE 不应复制其失败结构 |
| Update | 同文件 `76-80` | 每帧 `ModTransport.Poll()` | Poll 才排空 LMN dispatcher |
| OnDestroy | 同文件 `82-86` | `_harmony?.UnpatchSelf()` 后 `ModTransport.Shutdown()` | 源码顺序不是 BUE lifecycle 的规范顺序 |
| Initialize | `Routing/ModTransport.cs:93-106`, SHA `8914DB0E23A7A24C17E0D1DCFF6B40BF9C76D1F5AE57ED1F0BEBBBA07B6BB2E5` | 幂等；初始化 reflection、session manager、namespaced transport | 不证明运行环境 API 都存在 |
| Shutdown | 同文件 `108-130` | 清 handlers/pending/warnings、named handlers、sessions、dispatcher、RPC | 这是 LMN 全局 shutdown，BUE 模块不得擅自调用以影响其他消费者 |

### 3.3 BUE 推荐阶段机（内部 seam，不修改 Contracts）

```text
BepInEx AddComponent
  -> Core entrypoint Awake
  -> role/reference probe
  -> immutable catalog integrity + Runtime Admission
  -> SettingsRuntime load/migrate/validate
  -> LMN adapter register + application handshake Pending
  -> apply only owned Harmony patches
  -> Lifecycle Starting -> module Start
  -> Running / Disabled / Isolated / Core SafeMode

OnDestroy / fatal core transition
  -> invalidate ConnectionGeneration and LifecycleGeneration
  -> stop accepting new queue work
  -> Stop modules in reverse dependency order (once/generation)
  -> dispose feature lifetimes
  -> unregister exact LMN delegates
  -> remove only owned Harmony patches
  -> discard BUE-owned queued work and pending requests
  -> finalize diagnostics
```

每阶段必须异常隔离并保持补偿动作幂等。单模块 `Start`/callback/`Stop` 异常只进入 `Isolating -> Isolated`，然后 lifetime lease 兜底清理。只有 catalog、核心 definition、核心 settings transaction engine 或核心 dispatch gate 不可建立时才发 `CoreRuntimeFailure` 并进入 SafeMode；SafeMode 不终止原版游戏。

### 3.4 双端引用交集和泄漏风险

| Surface | Client fixed | U3DS fixed | V1裁定 |
| --- | --- | --- | --- |
| .NET Framework 4.7.2 domain types | 已由项目 reference 确认 | U3DS references 已冻结 | Contracts 只使用经双端证明的交集；仍需 Candidate compile proof |
| BepInEx public API | Client 5.4.23.5 IL confirmed | U3DS 5.4.23.5 IL + 引导 confirmed | entrypoint adapter 只依赖最小共同 API；插件实际加载仍 `UNRESOLVED` |
| `Assembly-CSharp` core (`Provider`, `Dedicator`, `ServerSavedata`) | client ref/source 可见 | U3DS reference 已冻结 | 只能在 native adapter 内使用；不得进 Contracts |
| `SDG.NetTransport`, Steamworks, LMN | client/LMN compile surface 可见 | U3DS Assembly reference 已冻结，LMN 插件未运行 | 只能在 network adapter 内使用 |
| Glazier/Sleek/`Unturned.LiveConfig.Runtime` 等 client-only token | client 可见 | 双端扫描确认存在非交集 token | Core/U3DS path 不得静态触达；UI facet 独立且 role gate 前不得解析/实例化 |
| Harmony target signature | LMN runtime reflection + self-test | U3DS reference 可供静态 probe，运行未验证 | patch 失败必须能力降级或 SafeMode；不得假定签名相同 |

冻结的双端二进制暴露了 ABI 差异：客户端 `SDG.Unturned.Dedicator.IsDedicatedServer` 是静态 property getter，而 U3DS 程序集将它编译为 `public const bool IsDedicatedServer = true`；兼容旧成员 `isDedicated` 在两端均表现为静态 property。若统一 DLL 以客户端 reference 直接编译对 `IsDedicatedServer` 的调用，IL 会引用客户端 getter，而 U3DS 不一定存在该 method，执行时可能触发 `MissingMethodException`。因此 role adapter 不得直接引用这个不一致成员；应使用经双端证明的共同 ABI（`isDedicated`）或构建期隔离的 probe，并以 U3DS 实际插件加载测试收口。

禁止用 `Application.isBatchMode` 作为唯一隔离。它只能是环境信号；构建期 facet、入口绑定、reference intersection 编译与实际 U3DS 加载日志共同构成门禁。

## 4. LMN V5 能力表

| Requirement | Fixed source evidence | Finding |
| --- | --- | --- |
| named registration | `NamespacedTransport.cs:95-156`, SHA `5CE80DFC33C6B006231CAD66DB4CFE6AFF4487E1E31862539F565BF1D5FCE8A1`; `ModTransport.cs:246-295`, SHA `8914DB0E23A7A24C17E0D1DCFF6B40BF9C76D1F5AE57ED1F0BEBBBA07B6BB2E5` | 注册前把 channel Trim+lowercase，要求 ASCII `[a-z0-9._-]`、长度 1..96；重复注册抛异常；注销必须传入同一 delegate |
| reliable send | `NamespacedTransport.cs:159-236,475-478` | 默认 public named API 为 reliable；bool 精确映射 `Reliable/Unreliable`; client 通过 client transport，server 通过 connection；SP/host 有 loopback |
| sender context | `ModRouter.cs:109-127,174-185`, SHA `7B54910DD27DD562D712262B0DA4A1D099E5D1BAE1A93A6EF5458D8834554ED9` | server sender 来自 `ITransportConnection.TryGetSteamId`；失败为 Nil。BUE 必须拒绝 Nil，不能接受 payload 自报身份 |
| payload limits | `NamespacedTransport.cs:16-17,468-472`; `ModRouter.cs:53-70,137-171` | LMN named payload 1..61440 bytes，channel <=96 bytes；BUE 仍使用更窄 16 KiB/12 KiB/512 KiB 分片限制 |
| packet ownership | receive patches SHA `A3B4F6832B6026D1E89E510FF4966DCB824F85ACD542E85F0184DE75C27637EA` / `460AD0ECD7FB86AE533F42DAFB8D0D25E95BFB2D68805057FACA4CDA6F37E37B`；`NamespacedTransport.cs:424-465` | 原始 Provider buffer 会复用；router/transport 在延迟前复制 payload |
| handler thread | `MainThreadDispatcher.cs:16-64`, SHA `B5182181AA72395D65D7F0F08A4E98E068241DCC2AF265D77136CF924F30AE5A`; `ModTransport.cs:305-309` | named handler Action 入并发队列，由 LMN plugin `Update -> Poll -> DrainQueue(max 500)` 调用；单 Action 异常隔离 |
| cleanup | `NamespacedTransport.cs:131-156,391-395`; `ConnectionSessionManager.cs:56-71,123-176`, SHA `3DC5AF09B995EE9F65FC26DAFA10597659043EDB7AAB5807998BAAFC6634872A` | handler 可注销；断开 dispose session；全局 shutdown 清 dispatcher。已排队 Action 不按 handler/session撤销 |
| handshake | `HandshakeProtocol.cs:24-184`, SHA `9E15A19A9775299CF14D652CEFA9EFB53E212CF76116DEEB81AC8E69FD9A70BB`; 全仓 caller search | 能编码 Hello/Ack 并做 channel version min；生产源码无 Hello 发起 caller，不含 BUE nonce/chunk/SessionReady |

### 4.1 LMN 不是以下能力

- 不是身份认证或授权；sender context 只是 transport 关联身份输入。
- 不是库存权威或事务系统。
- 不是 BUE 应用握手；其 `IsHandshakeComplete` 不能替代共享 `SessionReadyEvent`。
- 不自动阻止未协商 named channel 的发送。
- 不保证某 handler 注销后已排队 callback 不再执行。
- 不提供 BUE `ConnectionGeneration`、`SnapshotId`、nonce 或 per-message generation fence。

## 5. 线程、排队与 generation 边界

```text
Unturned receive
  -> LMN Harmony Prefix
  -> validate LMN frame / copy payload / resolve transport SteamID
  -> LMN MainThreadDispatcher Action (captures SteamID, payload, handler)
  -> LMN plugin Update: DrainQueue(max 500)
  -> BUE named handler
       validate BUE size/version/kind/length before DTO allocation
       validate sender + application handshake context
       enqueue immutable BUE command tagged with captured connection context
  -> BUE game-thread gate (bounded work/frame)
       re-check connection + lifecycle generation
       SettingsRuntime / Negotiation / Lifecycle
       publish complete immutable projection
  -> frontend consumes snapshot/status/SessionReady
```

尽管 LMN 当前从 `Update` 调 handler，BUE 首次 callback 必须运行时断言 game-thread identity；不能把源码事实升级为所有部署的运行保证。所有 timer、Task continuation、file I/O completion 和 feature callback 都必须回到 BUE game-thread gate 后才能写 runtime state。持久化序列化/写盘可以在后台准备，但 commit 结果必须带 expected revision/generation 回主线程再发布。

### 5.1 四个独立排序域

| Domain | Owner | Created/invalidated | Must not mean |
| --- | --- | --- | --- |
| CatalogGeneration | immutable catalog loader | process load/restart | connection freshness or module callback freshness |
| LifecycleGeneration | Lifecycle per FeatureId | each accepted start cycle; invalidated before stop/isolate | network connection or setting revision |
| ConnectionGeneration | BUE connection coordinator | SP authority session、P2P connect/reconnect、disconnect | authentication, lifecycle, catalog |
| setting revision | SettingsRuntime per FeatureId + RevisionScope | successful atomic value transaction only | connection or lifecycle generation |

Runtime Admission 只读取 catalog/definition 静态事实并返回 allow/reject；它不读用户开关、不写 FeatureState。SettingsRuntime 完成 load/migration/validation 后提供完整 snapshot。Lifecycle 独占 FeatureState/StateRevision。Negotiation 独占 connection handshake、capability view 与 SessionReady。

### 5.2 已确认的 stale callback 缺口

`NamespacedTransport.InvokeServerHandler` 在收包后只捕获 `CSteamID sender`、payload copy 与 delegate。`ConnectionSessionManager` 断开时会移除并 dispose session，但不会删除 dispatcher 内 Action。如果旧连接 Action 尚未执行，同 SteamID 已重连，下游再查询 current session/generation 会错误把旧包绑定到新连接。

因此以下“修补”无效：

- handler 执行时按 SteamID 查询当前 generation；
- 只检查 RequestId 是否非零；
- 只依赖 LMN `IsHandshakeComplete`；
- 断开时仅清 SettingsRuntime pending map。

此问题进入 `SCR-RT05-001`，见 §10。SCR 未裁定前，Ready 后可写 settings 网络链不得进入生产。

## 6. 设置存储、迁移、损坏恢复与 overlay

### 6.1 固定存储事实

客户端 BepInEx 5.4.23.5 IL：

- `BaseUnityPlugin.Config` 默认路径是 `<BepInEx ConfigPath>/<plugin GUID>.cfg`；
- `ConfigFile` 用 `_ioLock` 串行进程内访问；
- `Save()` 直接创建 `StreamWriter(target, append:false)` 并覆盖；
- `Reload()` 直接 `File.ReadAllLines(target)`；
- 没有 temp+replace、文件 digest、schema migration 或自动损坏回退。

U3-SDK `ServerSavedata.cs`（SHA `1C6E699542C4856BA55D4F176134E2D021D41F555AC9947CCD58352C0D2A00B2`）：

- dedicated 根为 `/Servers/<Provider.serverID>`，非 dedicated 根为 `/Worlds/<Provider.serverID>`（lines 9-42）；
- JSON/data/block 写入会删除旧式 backup、把当前文件移动为 `file~`，再写新文件（lines 70-121）；
- 这是可恢复备份序列，但不是单一原子 replace transaction。

`PlayerSavedata.cs`（SHA `5FC77E9847C8C8107E4D73A36FD1492D8991A74920B477E3656158A9EBF97854`）按 SteamID、character、level 存玩家世界数据，不适合作为框架全局 ClientPreference；BUE 不应污染原版 player save。

### 6.2 三类 setting 的存储模型

| Authority model | Persisted root | Authority | Session behavior | Failure policy |
| --- | --- | --- | --- | --- |
| ClientPreference | client BepInEx config 子目录中的 BUE versioned document，按 FeatureId 分区 | 当前客户端 | 本地完整 snapshot；不经 LMN | 损坏文件 quarantine，使用 schema default，投影受限诊断；不把服务端值写回 |
| ServerAuthority | `/Servers/<serverID>/...` 或 `/Worlds/<serverID>/...` 下 BUE 专属 versioned document；具体 adapter 必须验证绝对路径边界 | SP/P2P Host/U3DS authority | Ready 后通过 0x0101/0102/0103/0104 同步 | migration/persist 失败使相关 feature disabled/isolated；旧确认 snapshot 保持事实，不能先发布未落盘值 |
| ServerPolicyWithClientPreference | 客户端偏好仍存 ClientPreference；政策存 ServerAuthority | server 决定本 session effective value | 内存 overlay = local preference + current server policy；带 ConnectionGeneration | disconnect/reconnect 立即丢 overlay，UI 恢复本地偏好；永不静默改写 local preference |

路径中只能使用 registry 已验证 FeatureId/slug 的安全映射；禁止把网络 payload 直接拼入路径。

### 6.3 原子事务与迁移要求

一次 setting transaction 的顺序：

```text
decode bounded request
 -> verify Ready connection / sender / RequestId replay
 -> verify one FeatureId + one RevisionScope + ExpectedRevision
 -> validate all SettingMutation against compiled Settings facet
 -> build complete candidate document in memory
 -> migrate sequentially (N -> N+1), validate after every step
 -> serialize canonical bytes to same-directory unique temp
 -> flush durable bytes
 -> replace target while retaining last-known-good backup
 -> only after durable commit: increment revision once
 -> publish complete immutable FeatureSettingsSnapshot
```

任一步失败都不得部分改变内存 authority、revision 或 UI projection。相同 RequestId+相同 payload 回放相同 terminal result；相同 RequestId+不同 payload 返回 `RequestIdConflict`。无变化、拒绝、重试、0x0104 读取都不增加 revision。

损坏恢复：读取 target 失败时验证 last-known-good backup；成功则恢复并记录内部原因；两者都失败则把原文件以 diagnostic id 隔离，加载 defaults，并按 scope/feature 风险选择 Disabled/Isolated。不得把 exception、绝对路径或原始内容投影给普通 UI。

schema migration 失败不能静默跳版本。迁移函数必须由 compiled definition 指定、确定性且逐版本；保留原文件和 diagnostic。当前共享 DTO 已有 `SettingSchemaIncompatible`/`SettingMigrationFailed`，无需为文件格式细节扩充公共错误码。

## 7. Capability、SessionReady、降级和 SafeMode

### 7.1 应用握手

BUE 在 capabilities named channel 上实现冻结链：

```text
connect generation N
 -> CapabilityHello chunks (client nonce, contract, framework, feature capabilities)
 -> authority verifies bounds/digest/generation/nonce
 -> CapabilitySnapshot chunks (server nonce, SnapshotId, negotiated views)
 -> client CapabilityAck
 -> server SessionReadyEvent(N, SnapshotId)
 -> settings/status network traffic allowed
```

`SessionReadyEvent` 是 BUE 状态机事件，不等于 LMN channel registration、transport connected 或 LMN handshake bool。Ready 前收到 0x0101..0x0201 必须拒绝/丢弃并受限记录；不能排队到未来 Ready 后执行。

### 7.2 环境行为

| Scenario | Result |
| --- | --- |
| SP authority | 建立进程内 connection generation 与 capability projection；LMN loopback 仍须走相同 BUE decoder/generation fence，不能开“可信快捷路径” |
| P2P Host local player | authority 与 local client 是两个逻辑角色；loopback 仍产生 SessionReady 后才可 settings/status |
| P2P Client, server no BUE/LMN | handshake timeout -> `Unavailable/NetworkCapabilitiesUnavailable`；静默保持原版库存与 UI；不弹网络错误 |
| server BUE but feature incompatible | 该 feature `Incompatible` 或 `Degraded`；其他 feature继续 |
| U3DS | 不解析/实例化 UI facet；能力、settings、lifecycle 只写日志和 server projections |
| LMN patch/self-test failure | network adapter unavailable；网络功能降级。若当前部署把 LMN 定义为 core required 且无法维持契约安全，再进入 Core SafeMode；不能让半工作 router发送 |

### 7.3 故障层级

| Failure | Framework projection |
| --- | --- |
| module Start/callback/cleanup | `ModuleStartFailed` / `ModuleRuntimeIsolated` / `CleanupIncomplete`; isolate one feature |
| dependency/capability missing | `DependencyUnavailable` / `CapabilityMissing`; disabled/degraded/incompatible according to compiled requirement |
| setting validation/revision/request conflict | `SettingRejected` family; complete current snapshot converges |
| setting migration/persistence | `SettingMigrationFailed` / `SettingPersistenceFailed`; feature-scoped unless core definition/settings store itself damaged |
| malformed/oversized/unknown wire | `MalformedPayload` / `PayloadTooLarge` / `UnknownMessageKind`; disconnect/rate policy is internal and bounded |
| stale connection/nonce/snapshot | existing `StaleConnectionGeneration`, `HandshakeNonceMismatch`, `SnapshotIncomplete/IntegrityFailed` |
| catalog/core dispatcher/shared contract unusable | `CoreRuntimeFailure`; Core SafeMode, no features, original game continues |

原始 exception 类型、Harmony target 名、file path、transport error 和 parse offset 只进入结构化内部诊断，以 `DiagnosticId` 关联。无需扩展公开 `FrameworkErrorCode` 以映射每个 native/LMN 异常。

## 8. 推荐内部 adapter seam

以下名称与签名不进入 Contracts，实施票可继续深化：

| Internal module | Responsibility |
| --- | --- |
| `IRuntimeRoleProbe` | 返回 client/SP authority/P2P host/P2P client/U3DS role facts；不把 UI type加载当探测手段 |
| `IRuntimeAdmissionReader` | 只读 compiled definition/catalog，返回静态准入 |
| `IFeatureLifecycleCoordinator` | 唯一 FeatureState writer；generation、start/stop、隔离和 lifetime cleanup |
| `ILmnNamedChannelAdapter` | exact delegate registration/unregistration、reliable send、sender context；不暴露 LMN type 给 feature |
| `IConnectionContextRegistry` | BUE connection generation、nonce、SnapshotId、Ready；要求 receive-time context fence（受 SCR-RT05-001 阻断） |
| `IBoundedMessageDecoder` | BUEB/bootstrap 与 Contract envelope 的先验长度、版本、kind、UTF-8、集合限制 |
| `IGameThreadCommandQueue` | 有界排队、每帧预算、generation recheck、shutdown reject |
| `IFeatureSettingsStore` | versioned document load/migrate/atomic commit/backup/quarantine；按 FeatureId+scope |
| `ISettingsRuntime` | 原子 mutation、RequestId replay、revision、完整 snapshot；不拥有 lifecycle state |
| `IServerPolicyOverlay` | 当前 connection generation 的 effective session view；不修改 local preference |
| `IRuntimeProjectionPublisher` | 只发布安全完整 snapshot/status/SessionReady，不泄漏实现异常 |

## 9. 被拒方案

| Rejected | Reason |
| --- | --- |
| 用 LMN `IsHandshakeComplete` 代替 BUE SessionReady | 没有 nonce/chunk/snapshot/ConnectionGeneration，且生产 Hello 发起链缺失 |
| handler 时按 SteamID 查询 current session 来处理 stale queue | 旧 Action 可跨重连，被误绑定到新 session |
| 在 feature callback 中直接改 FeatureState | 破坏 Lifecycle 单写者和 revision 顺序 |
| 用 BepInEx ConfigFile 做 ServerAuthority 原子事务 | direct truncate-write，无 schema/digest/recovery transaction |
| 把 server policy 写回 client preference | 破坏本地偏好事实与跨服务器隔离 |
| 依赖 `Application.isBatchMode` 保护 UI | 不证明类型加载安全或双端 reference 交集 |
| module 调用 LMN global Shutdown/Clear | 会破坏其他模块/插件消费者 |
| `Harmony.UnpatchAll()` 或解除他人 patch | 跨插件破坏；只能移除 own Harmony id/instance |
| Ready 前缓存 settings write 等待握手后执行 | 请求可能跨代际成为幽灵写入；应拒绝并由新代际重新发起 |
| 将源码/候选 U3DS 文件当 runtime PASS | 违反证据分类和同一 Candidate DLL hash 门禁 |

## 10. Shared Contract Change Request

### SCR-RT05-001：Ready 后消息必须绑定 receive-time connection context

| Field | Value |
| --- | --- |
| ChangeRequestId | `SCR-RT05-001` |
| RaisedBy / SourceTicket | GPT / RT-05 |
| AffectedTokens | Ready 后 Contract envelope 或 LMN adapter receive context；`ConnectionHandshakeId`; 0x0101..0x0201 processing rule |
| CurrentBaseline | 普通 envelope 只有 contractMajor/minor、messageKind、payloadLength、payload；settings/status DTO 不含 ConnectionGeneration。RequestId 只在 current connection generation 内相关，不授权 |
| Evidence | LMN `NamespacedTransport.cs:424-465` 只捕获 SteamID/payload/delegate；`ConnectionSessionManager.cs:123-176` disconnect dispose 不撤销 dispatcher Action；逐文件 hashes见 §4 |
| Problem | 同 SteamID 断开并快速重连时，旧 connection 的已排队 Action 可在新 session 建立后执行；下游 current lookup 无法恢复 receive-time identity |
| ProposedChange | 二选一并经实现原型验证：A) 所有 Ready 后 BUE wire frame 在外层携带并验证 `ConnectionGeneration + SnapshotId`（必要时 nonce binding）；或 B) LMN 新增不可伪造的 connection-bound receive context/token并在 disconnect 前使 queued callback失效。不得只携带 SteamID |
| CompatibilityImpact | wire major/minor、backend adapter、frontend readiness；方案 A 可能改变所有 Ready 后消息封装，方案 B 要求冻结新 LMN SourceSet/API |
| TestImpact | 同 SteamID disconnect/reconnect、旧包延迟、旧 Action 已入队、generation wrap/nonce、SP/host loopback、U3DS remote；断言旧写永不到 SettingsRuntime mutation |
| GPTDecision | `NeedsEvidence`：RT-05 不自行接受；必须由共享契约 owner 原型/审计后裁定 |
| GeminiReview | pending；需确认前端只接受当前 SessionReady generation/snapshot 的 projections |
| HumanTrace | 人工授权 RT-05；未授权契约变更 |
| Status | `proposed` |

### SCR-RT05-002：SourceSet manifest 排序语义勘误

| Field | Value |
| --- | --- |
| ChangeRequestId | `SCR-RT05-002` |
| AffectedTokens | `BUE-SS-20260824-01` manifest reproduction text |
| CurrentBaseline | 文本要求 ordinal；冻结 digest 实际由文化排序实现得到 |
| ProposedChange | 保留旧 SourceSet 不变；发布 successor，明确 UTF-8 `/` paths + `StringComparer.Ordinal`，记录 legacy digest 与 corrected digest，四张研究票统一迁移后再关闭 |
| Evidence | 同 44 文件：legacy `7151...`; true ordinal `4F2909...` |
| Status | `proposed`; 不代表内容变化或已批准 successor |

## 11. 运行与实施证据义务

所有运行用同一 Candidate DLL SHA-256、SourceSet successor 和 CaseId，分别取证：

1. Client/SP：BepInEx load order、Awake阶段、Harmony self-test、SP loopback handshake、settings原子写/恢复、shutdown cleanup。
2. P2P Host/Client：双方 DLL hash、连接/重连 generation、SessionReady 前后门禁、server missing、feature mismatch、3s retry/8s snapshot、同 SteamID stale queue adversarial case。
3. U3DS：冻结独立 `Assembly-CSharp`/BepInEx refs后先做 intersection build；实际启动证明不解析 UI type、LMN receive/send、server root partition、headless日志和 shutdown。
4. Settings fault injection：写盘失败、temp/backup 中断、主文件损坏、backup损坏、逐版本 migration失败、并发 ExpectedRevision冲突、同 RequestId不同 payload。
5. Lifecycle fault injection：Start、callback、Stop、late registration、dependency cascade；证明一个 feature isolation 不影响其他 feature或原版。
6. Queue/thread：记录 Unity main thread id；网络 callback、timer、I/O completion 和 feature callback 的执行 thread；disconnect 后旧 generation work全部被拒绝。
7. UI消费（Gemini）：Pending 显示同步状态；SessionReady 后才解锁 server-authority setting；完整 snapshot/revision 驱动；Unavailable 静默原版；SafeMode一次性提示；不显示 nonce、payload、path、stack。

## 12. Acceptance 对账

| RT-05 criterion | Result |
| --- | --- |
| client/U3DS BepInEx init、Harmony、start/close/exception | 双端 BepInEx 5.4.23.5 references 已冻结；U3DS 引导链 PASS；BUE/LMN 插件 lifecycle 仍为运行义务 |
| 双端 reference intersection | 冻结 references 风险表完成；Candidate compile/IL reachability/插件运行仍待实施 |
| LMN named/reliable/sender/payload/thread/cleanup | 完成，含逐文件 hash与限制 |
| callback/queue/generation | 完成；发现 SCR-RT05-001 blocker |
| 三类 settings storage/atomic/migration/corruption/overlay | 完成；区分 fixed fact 与 adapter requirement |
| Admission/Settings/Lifecycle/Connection separation | 完成，四域表 |
| negotiation/SessionReady/degrade/SafeMode | 完成；明确 LMN handshake不等价 |
| stable error family | 完成；无需新增细粒度公开错误码 |
| GPT report/diagrams/tables/seams/runtime obligations | 完成 |
| no production/no silent contract change | 满足；只有 proposed SCR |

最终判定（迁移后更新）：**研究内容与 Gemini 消费复核已通过，U3DS references 已进入 `BUE-SS-20260824-02`；RT-05 仍暂不可 resolved**。唯一架构阻断为 `SCR-RT05-001` 尚未完成两个最小代码 spike、独立审计与裁定；RT-02/RT-03 仍需登记 successor 迁移。本文不证明 BUE/LMN 在任何环境运行通过。
