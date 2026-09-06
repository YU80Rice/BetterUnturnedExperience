# T2：三插件源码盘点与迁入形态（2026-09-06）

Wayfinder 票：`.scratch/bue-v2-phase2-official-adoption/issues/02-three-plugin-source-inventory.md`  
盘点对象：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\{LaunchInventoryTidy,LaunchInPlaceReload,LaunchHordeTracker}`  
方法：只读源码 + DEV-V2-08 既有重建事实 + BUE 契约/嵌入先例。**未改三插件源码，未复跑构建实验。**  
调查日期：2026-09-06。

T8 判据口径（本盘点沿用）：V1 数字频道消费方 = 调用 `ModTransport.Register*/Send*(int virtualChannel, …)`；命名频道消费方 = 调用 `RegisterNamed*` / `SendNamed*`。仅 `[BepInDependency]` 不算频道消费方（`.scratch/bue-v2-lmn-adoption/research/V2-T8-v1-ecosystem-inventory.md:12-32`）。

---

## 结论摘要

三插件**全部已是 LMN 命名频道消费方**，源码中**零** V1 `int` 频道 API 调用（与 T8 口径甲 3/3 已迁 V2 一致）。纳入 BUE 的阻塞点不是「数字频道改命名」，而是：

1. 去掉独立 BepInEx 插件身份与 `HardDependency` LMN，改走 `IFeatureModule` + `IBueNetworkApi`；
2. `IBueNetworkApi` 契约面**没有入站订阅**（接收在 Host 内部 `BueNetworkRuntime.Subscribe`，不在 `ContractTypes.cs` 的 `IBueNetworkApi` 上）；
3. 寻址从 `CSteamID` 换成 `IConnectionSession`；
4. LIT 体量/静态状态/持久熔断/会话 challenge 远大于 LIR/LHT。

DEV-V2-08 已实锤：归档验收候选（BepInEx 元数据均为 `0.0.0`）可在 BUE 接管态 + 独立 LMN DLL 下三环境收发。**官方纳入终态（无独立 LMN / 无三插件 DLL）08 票内互斥、未验证**（`audit/2026-09-05/DEV-V2-08/Delivery-DEV-V2-08-ecosystem-kit-20260905.md:63-69`、`DEV-V2-08-closing-report.md:35,46-48`）。

---

## 1. LaunchInventoryTidy（LIT）

### 1.1 项目结构与 LOC

路径：`Archive\2-未闭环验证项目\LaunchInventoryTidy\`。`LaunchInventoryTidy.csproj:80-113` 编译 33 个 `.cs`（含 `Properties/AssemblyInfo.cs` 与 `Patches\` 2 个）。全树 `.cs` **14811 LOC**（`wc -l`，不含 bin/obj）。

| 文件 | LOC | 角色 |
|---|---:|---|
| `ManualTidyNetwork.cs` | 2188 | 命名频道协议 + 收发 |
| `ManualTidyService.cs` | 1743 | 服务端权威整理事务 |
| `AutoTestDriver.cs` | 1396 | `#if TIDY_TEST_HARNESS` 自动测试 |
| `TestFixtureSession.cs` | 1201 | 测试夹具 |
| `TidyFaultCircuit.cs` | 999 | 玩家级熔断 |
| `TidyFaultCircuitPersistence.cs` | 745 | 磁盘持久熔断 |
| `LaunchInventoryTidyPlugin.cs` | 724 | BepInEx 入口 |
| `InventorySolver.cs` | 690 | 排列算法（O-LIT-1 复议对象） |
| `FaultInjectionTestRunner.cs` | 607 | 故障注入 |
| `Patches/PlayerDashboardInventoryUIPatch.cs` | 596 | 背包 UI 按钮 |
| `FixtureValidator.cs` | 496 | 测试校验 |
| 其余 22 个文件 | 合计约 4426 | 会话/账本/命令/调度/探针 |

测试夹具 8 文件合计 **4246 LOC**（`AutoTestDriver`/`CommandTidyAutoTest`/`CommandTidyFaultInjectionTest`/`FaultInjectionTestRunner`/`FixtureValidator`/`TestFixtureSession`/`NetworkTestProbe`/`ShutdownTestProbe`）。它们仍列在 csproj Compile 列表中，但主体用 `#if TIDY_TEST_HARNESS` 包裹（`LaunchInventoryTidy.csproj:34-41` 的 `TestHarness` 配置才定义该符号；Release 不编译这些块）。迁入生产装配时应把测试夹具排除出 `Plugin.csproj`，否则会把探针类型带进玩家 DLL。

非源码：`CHANGELOG.md`/`README.md`/`CONTRIBUTING.md`/`DEPENDENCIES.md`/`P2P_ADMISSION_GATES.md`/`LICENSE`/`run_tests.ps1`/`assets/`/`audit/2026-08-20/`/`publish/`。

### 1.2 `[BepInPlugin]` / `[BepInDependency]` 原文

```15:18:Archive/2-未闭环验证项目/LaunchInventoryTidy/LaunchInventoryTidyPlugin.cs
    [BepInPlugin("com.yu80rice.launchinventorytidy",
        "LaunchInventoryTidy [验收候选 / LMN V5 命名频道]",
        "0.0.0")]
    [BepInDependency(LaunchMultiplayerNetPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
```

- Harmony ID = 同一字符串 `com.yu80rice.launchinventorytidy`（`LaunchInventoryTidyPlugin.cs:21`）。
- LMN GUID 常量：`LaunchMultiplayerNetPlugin.Guid = "com.yu80rice.launchmultiplayernet"`（`LaunchMultiplayerNet/Core/LaunchMultiplayerNetPlugin.cs:44`）。
- 程序集版本 `0.0.0.0`（`Properties/AssemblyInfo.cs:17-18`）。

### 1.3 LMN API 调用面（逐点）

频道常量：`private const string NetworkChannel = "com.yu80rice.launchinventorytidy.net"`（`ManualTidyNetwork.cs:48`）。负载封套 `NamedPayloadVersion = 1`（`:52`）。**源码零** `RegisterServerHandler(int, …)` / `SendToServer(int, …)` / `ModRouter` / `NamespacedTransport` 直接调用；全部走 `ModTransport` 命名频道门面（门面再委托 `NamespacedTransport`，`LaunchMultiplayerNet/Routing/ModTransport.cs:246-267,579-593`）。

| 文件:行 | 调用 | LMN 签名（权威） |
|---|---|---|
| `ManualTidyNetwork.cs:152` | `ModTransport.RegisterNamedServerHandler(NetworkChannel, HandleServerMessage)` | `void RegisterNamedServerHandler(string channelId, Action<CSteamID, BinaryReader> handler)`（`ModTransport.cs:246`） |
| `ManualTidyNetwork.cs:153` | `ModTransport.RegisterNamedClientHandler(NetworkChannel, HandleClientMessage)` | `void RegisterNamedClientHandler(string channelId, Action<BinaryReader> handler)`（`ModTransport.cs:252`） |
| `ManualTidyNetwork.cs:183-184` | `UnregisterNamedServerHandler` / `UnregisterNamedClientHandler`（`CompleteShutdown`） | `bool UnregisterNamed*(string, Action…)`（`ModTransport.cs:258,264`） |
| `ManualTidyNetwork.cs:235-236` | 同上（兼容 `Shutdown()`） | 同上 |
| `ManualTidyNetwork.cs:406` | `ModTransport.BuildNamedMessage(writer => …)` | `byte[] BuildNamedMessage(Action<BinaryWriter> bodyWriter = null)`（`ModTransport.cs:832`） |
| `ManualTidyNetwork.cs:473` | `SendNamedToServer(NetworkChannel, payload, reliable: true)`（整理请求） | `void SendNamedToServer(string, byte[], bool reliable = true)`（`ModTransport.cs:579`） |
| `ManualTidyNetwork.cs:1100` | `SendNamedToClient(target, NetworkChannel, payload, reliable: true)`（`MSG_TIDY_COMMITTED`） | `void SendNamedToClient(CSteamID, string, byte[], bool)`（`ModTransport.cs:585`） |
| `ManualTidyNetwork.cs:1479` | `SendNamedToServer`（`MSG_INVENTORY_APPLIED_ACK`） | 同 `:579` |
| `ManualTidyNetwork.cs:1891` | `SendNamedToClient`（`MSG_TIDY_HOTKEY_RESULT`） | 同 `:585` |
| `ManualTidyNetwork.cs:1925` | `SendNamedToClient`（`MSG_SESSION_CHALLENGE`） | 同 `:585` |

**未使用**：`BroadcastNamedToAllClients`、`RegisterNamedPacket*`、`SendNamedRequestAsync`、V1 `int` API、`ModP2PTransport`。  
`LaunchInventoryTidyPlugin.cs:114-116` 注释写明依赖 LMN `Awake` 已 `ModTransport.Initialize()`，LIT **不**自己调 `Initialize`。  
`LmnDependencyGuard` 要求 LMN `AssemblyFileVersion >= 5.0.0.0`（`LmnDependencyGuard.cs:17-24,89`）。

私有负载消息（均在 `NamedPayloadVersion` 之后）：`MSG_REQUEST_TIDY_V2=2` / `MSG_TIDY_COMMITTED=3` / `MSG_INVENTORY_APPLIED_ACK=4` / `MSG_TIDY_HOTKEY_RESULT=5` / `MSG_SESSION_CHALLENGE=6`（`ManualTidyNetwork.cs:56-91`）。协议版本字节 `PROTOCOL_VERSION_V3=3`（`:95`）。

### 1.4 游戏 / Unity API 面

- **Harmony**：`PlayerDashboardInventoryUI` 构造函数 postfix，反射注入「整理 / 方向 / 模式」按钮（`Patches/PlayerDashboardInventoryUIPatch.cs:21-22,11-19`）。`ItemsTryAddItemPatch.cs:17,30` **故意无** `[HarmonyPatch]`，`PatchAll` 不会发现。
- **库存权威**：`ManualTidyService.TidyAllPlayerPages(PlayerInventory, …)` / `TidyPage(Items, byte page, …)`（`ManualTidyService.cs` grep 命中）；页范围注释限定玩家 page 2–6（`LaunchInventoryTidyPlugin.cs:176`）。
- **Provider 生命周期**：`onServerHosted` / `onEnemyDisconnected` / `onEnemyConnected`（`LaunchInventoryTidyPlugin.cs:132,146,162`）。
- **命令**：`CommandTidyFaults` / `CommandTidyUnfault` / `CommandTidyFaultRecover` 注册在 `OnServerHosted`（同文件 `:129`）。
- **线程**：`ThreadUtil.assertIsGameThread`（`:650`）；`MainThreadId` 在 `Awake` 缓存（`:78,34`）；`MainThreadDispatcher.ProcessAll` 在 `Update`（`:482-486`）。
- **Unity**：`DontDestroyOnLoad` + `hideFlags`（`:82-83`）；convergence `GameObject` 跟踪（`ManualTidyNetwork.cs:145`）。
- **Steamworks**：`CSteamID` 作为 handler 发送者与 `SendNamedToClient` 目标。
- **Glazier**：编译期**不**引用 `SDG.Glazier.Runtime`（csproj 无此 HintPath）；UI 全反射（`PlayerDashboardInventoryUIPatch.cs:18-19,31-46`）。

### 1.5 配置方式

**无** `ConfigEntry` / `Config.Bind` / `SettingsRuntime`（三插件全树 grep 为空）。可变状态：

- 每页排序方向 / 整理模式存在 **Harmony 补丁静态字典**（`PlayerDashboardInventoryUIPatch.cs:54-67`），进程内、不持久。
- 持久熔断 JSON：`BepInEx/config/LaunchInventoryTidy/fault_scopes/persistent_faults_{mode}_{safeMap}_{mapHash}_slot{slot}.json`（`TidyFaultCircuitPersistence.cs:29-37`），Newtonsoft.Json（csproj `:71-74`）。P2P scope 设计为外部 `SteamP2PFriends.BeginScope` 调用（`LaunchInventoryTidyPlugin.cs:124-126,497-498`）——BUE 仓库无 SPF 类型引用，迁入后这条耦合必须改写。

### 1.6 Harmony / 静态可变状态（迁移风险）

高风险静态：`LaunchInventoryTidyPlugin.Instance` / `Log` / `MainThreadId` / `DependencyGuardPassed` / `_currentScope*`（`LaunchInventoryTidyPlugin.cs:23-40,67-72`）；`ManualTidyNetwork._shuttingDown` / `_activeConvergenceObjects`（`:142-146`）；`ClientSessionNonce` 锁+token（`ClientSessionNonce.cs:32-40`）；`ServerSessionRegistry`、`RequestAdmissionStore`、`TidyFaultCircuit`、`PlayerOperationGate`、`TidyTransactionManager` 等玩家表。卸载分三阶段 `BeginQuiesce → MainThreadDispatcher.Shutdown → CompleteShutdown`（`ManualTidyNetwork.cs:164-177,432-434`）。并入单 DLL 后这些表必须绑到 `IFeatureModule.Start/Stop` 的 generation，禁止跨功能泄漏。

---

## 2. LaunchInPlaceReload（LIR）

### 2.1 项目结构与 LOC

9 个 `.cs`，**3105 LOC**。`LaunchInPlaceReload.csproj:76-84`。

| 文件 | LOC | 角色 |
|---|---:|---|
| `AmmoRepackService.cs` | 1619 | 压弹库存事务 |
| `AmmoRepackNetwork.cs` | 874 | 命名频道 + 主线程 dispatcher |
| `LaunchInPlaceReloadPlugin.cs` | 249 | 入口 / 双击检测 |
| `Patches/TidyServicePostfixPatch.cs` | 127 | 软依赖 LIT Harmony postfix |
| `P2PAmmoManager.cs` | 82 | 换弹槽位上下文 |
| `Patches/ForceAddItemPatch.cs` | 62 | `forceAddItem` 原位放回 |
| `Patches/UseableGunReceiveAttachMagazinePatch.cs` | 43 | 记录新弹匣槽 |
| `RepackToast.cs` | 31 | 成功提示 |
| `Properties/AssemblyInfo.cs` | 18 | `0.0.0.0` |

### 2.2 `[BepInPlugin]` / `[BepInDependency]` 原文

```17:19:Archive/2-未闭环验证项目/LaunchInPlaceReload/LaunchInPlaceReloadPlugin.cs
    [BepInPlugin(Guid, "LaunchInPlaceReload [未发布开发构建]", Version)]
    [BepInDependency(LaunchMultiplayerNetPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.yu80rice.launchinventorytidy", BepInDependency.DependencyFlags.SoftDependency)]
```

`Guid = "com.yu80rice.launchinplacereload"`，`Version = "0.0.0"`，`HARMONY_ID` 同 Guid（`:22-25`）。**跨插件软依赖**是三件里唯一的功能间耦合。

### 2.3 LMN API 调用面（逐点）

频道：`public const string NetworkChannel = "com.yu80rice.launchinplacereload.repack"`（`AmmoRepackNetwork.cs:325`）。`ProtocolVersion=1`，`RequestRepackAmmo=1`，`RepackSuccess=2`（`:326-328`）。

| 文件:行 | 调用 | LMN 签名 |
|---|---|---|
| `AmmoRepackNetwork.cs:381` | `RegisterNamedServerHandler(NetworkChannel, ServerRepackHandler)` | `ModTransport.cs:246` |
| `AmmoRepackNetwork.cs:382` | `RegisterNamedClientHandler(NetworkChannel, ClientRepackHandler)` | `ModTransport.cs:252` |
| `AmmoRepackNetwork.cs:415` | `UnregisterNamedServerHandler` | `ModTransport.cs:258` |
| `AmmoRepackNetwork.cs:423` | `UnregisterNamedClientHandler` | `ModTransport.cs:264` |
| `AmmoRepackNetwork.cs:450` | `BuildNamedMessage` | `ModTransport.cs:832` |
| `AmmoRepackNetwork.cs:456` | `SendNamedToServer(NetworkChannel, payload, reliable: true)` | `ModTransport.cs:579` |
| `AmmoRepackNetwork.cs:607` | `BuildNamedMessage` | `ModTransport.cs:832` |
| `AmmoRepackNetwork.cs:614` | `SendNamedToClient(client, NetworkChannel, payload, reliable: true)` | `ModTransport.cs:585` |

**未使用** `BroadcastNamed*`、V1 `int` API、`ModRouter`/`NamespacedTransport` 直接调用。  
注册延迟到首帧 `Update` + `ThreadUtil.assertIsGameThread()`（`LaunchInPlaceReloadPlugin.cs:99-101,195-209`），因 BepInEx `Awake` 不保证游戏线程。委托实例缓存以保证注销引用匹配（`AmmoRepackNetwork.cs:364-367`）。半注册回滚：`catch` 后无条件 `UnregisterHandlers`（`:386-399`）。

### 2.4 游戏 / Unity API 面

- Harmony 编译期补丁：`UseableGun.ReceiveAttachMagazine` Prefix/Postfix（`UseableGunReceiveAttachMagazinePatch.cs:16-40`）；`PlayerInventory.forceAddItem(Item, bool)` Prefix（`ForceAddItemPatch.cs:18-22`）。
- 运行时补丁：`AccessTools.TypeByName("LaunchInventoryTidy.ManualTidyService")` + `TidyAllPlayerPages` postfix → `AmmoRepackService.MergeSameIdMagazines`（`TidyServicePostfixPatch.cs:12-21,29-46`）。LIT 不在则跳过。
- 输入：`ControlsSettings.reload` + `InputEx.GetKeyDown`，双击阈值 0.3s（`LaunchInPlaceReloadPlugin.cs:117-138`）。
- 单机/房主走本地 `ExecuteRepackOnMainThread`；客机走 `SendRepackRequest`（`:146-173`）。
- Steamworks `CSteamID`；`Provider.isServer`；`Player.LocalPlayer`。

### 2.5 配置方式

无 `ConfigEntry` / 无磁盘设置。冷却、队列上限、诊断窗口是代码常量（dispatcher `MaxPendingRequestSenders=64`、`DiagnosticIntervalSeconds=5f`，`AmmoRepackNetwork.cs:58-62`）。

### 2.6 迁移风险

- 静态：`Instance`、`RepackMainThreadDispatcher` 队列、`PendingRequestIds`、`P2PAmmoManager` 槽位、`_requestSequence`（`AmmoRepackNetwork.cs:329-371`）。
- **LIT 类型名硬编码**：纳入后若 `ManualTidyService` 改命名空间，postfix 静默失效；同 DLL 内应改为直接调用或 `IOwnedFeatureEventPublisher`，不再 Harmony 跨插件。
- 原位换弹补丁碰 `UseableGun`/`forceAddItem`，与 BUE「更好的物品交互」库存路径可能重叠——迁入时需点名冲突审查（推断，见 §7）。

---

## 3. LaunchHordeTracker（LHT）

### 3.1 项目结构与 LOC

19 个 `.cs`，**2242 LOC**。`LaunchHordeTracker.csproj:77-95`。

| 文件 | LOC | 角色 |
|---|---:|---|
| `Patches/PlayerLifeUIPatches.cs` | 396 | HUD 标签反射 |
| `HordeStatusNetwork.cs` | 382 | 命名频道广播 |
| `HordeTrackerService.cs` | 294 | 信标订阅 / epoch |
| `LocationResolver.cs` | 139 | 地名 |
| `LaunchHordeTrackerPlugin.cs` | 124 | 入口 |
| `HordeStatusBroadcaster.cs` | 106 | 脏标记广播 |
| `RuntimeEnvironment.cs` | 104 | ClientUi vs U3DS |
| `OwnerResolver.cs` | 100 | 发起者 |
| `HordeHudRenderer.cs` | 99 | 10Hz HUD |
| `CommandHorde.cs` | 97 | `/horde` |
| 其余 9 个 | 合计约 401 | 快照/mailbox/补丁/守卫 |

### 3.2 `[BepInPlugin]` / `[BepInDependency]` 原文

```11:14:Archive/2-未闭环验证项目/LaunchHordeTracker/LaunchHordeTrackerPlugin.cs
    [BepInPlugin("io.github.yu80rice.launchhordetracker",
        "LaunchHordeTracker",
        "0.0.0")]
    [BepInDependency(LaunchMultiplayerNetPlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
```

显示名**无**括号注记（与 08 手册纠正一致：`Delivery-DEV-V2-08-ecosystem-kit-20260905.md:38`）。Harmony ID 同 GUID（`LaunchHordeTrackerPlugin.cs:17`）。ABI 守卫：`typeof(LaunchMultiplayerNetPlugin).Assembly.GetName().Version.Major == 5`，否则抛（`RuntimeDependencyGuard.cs:13-21`）。

### 3.3 LMN API 调用面（逐点）

频道：`internal const string NetworkChannel = "io.github.yu80rice.launchhordetracker.horde-status"`（`HordeStatusNetwork.cs:39`）。`ProtocolVersion=1`，`Update=1`，`Clear=2`（`:41-47`）。

| 文件:行 | 调用 | LMN 签名 |
|---|---|---|
| `LaunchHordeTrackerPlugin.cs:56` | `ModTransport.Initialize()` | `void Initialize()`（`ModTransport.cs:93`）——三件里**唯一**显式 Initialize |
| `HordeStatusNetwork.cs:70` | `RegisterNamedClientHandler(NetworkChannel, _clientHandler)` | `ModTransport.cs:252` |
| `HordeStatusNetwork.cs:97` | `UnregisterNamedClientHandler(NetworkChannel, _clientHandler)` | `ModTransport.cs:264` |
| `HordeStatusNetwork.cs:112` | `BuildNamedMessage`（Update） | `ModTransport.cs:832` |
| `HordeStatusNetwork.cs:140` | `BuildNamedMessage`（Clear） | 同上 |
| `HordeStatusNetwork.cs:323` | `SendNamedToClient(target, NetworkChannel, payload, reliable)` | `ModTransport.cs:585` |

**无** `RegisterNamedServerHandler`、**无** `SendNamedToServer`、**无** `BroadcastNamedToAllClients`。服务器用 `Provider.clients` 循环 + 跳过本地身份（`HordeStatusNetwork.cs:293-323`）。Update `reliable: false`，Clear `reliable: true`（`:124,148`）。房主 HUD 走 `HordeStateTracker` 本地权威，不 loopback（`:106-107,318-321`）。

注释「ModRouter Prefix 拦截」（`HordeStatusNetwork.cs:31`）描述 LMN 内部路径，LHT 源码**不**直接调 `ModRouter`。

### 3.4 游戏 / Unity API 面

- Harmony：`InteractableBeacon.spawnRemaining` / `despawnAlive` Postfix（`BeaconCounterPatches.cs:26-42`）；`PlayerLifeUI` 构造函数仅 `RuntimeEnvironment.CanUseClientUi` 时加载（`LaunchHordeTrackerPlugin.cs:49-53`）。
- `BeaconManager.onBeaconUpdated`（`HordeTrackerService.cs:10-14`）。
- HUD：反射 `Glazier` / `ISleekLabel` / `ISleekElement`（`PlayerLifeUIPatches.cs` 头部注释；csproj 引用 `SDG.Glazier.Runtime` + `UnityEngine.TextRenderingModule`，`:55-61`）。
- `/horde`：`Command` 子类；U3DS 仅 admin（原版 `ChatManager.process` 守门，`CommandHorde.cs:21-22`）。
- `ThreadContext.AssertMainThread` 贯穿 Update/OnDestroy（`LaunchHordeTrackerPlugin.cs:76,99`）。

### 3.5 配置方式

无 `ConfigEntry`。HUD 文案/10Hz/`/horde` 冷却 1.5s 均为常量（`HordeHudRenderer.cs:26`，`CommandHorde.cs:31`）。

### 3.6 迁移风险

静态：`Instance`/`Log`、`HordeTrackerService` epoch/sequence/脏标记（`:29-47`）、`PendingHordeSnapshot` 单槽 mailbox、`HordeStatusNetwork._clientHandler`/`_acceptClientMessages`/`LastBroadcastedActive`（`:50-55`）、`CommandHorde.LastAcceptedAt`。卸载先关接收闸再 drain（`LaunchHordeTrackerPlugin.cs:100-108`）。不可靠广播的 1:1 语义已在 08 P2P/U3DS 实锤，迁到 `IBueNetworkApi.SendToClients(..., reliable:false)` 后必须复验丢包/重复。

---

## 4. 构建链（复用 DEV-V2-08，不重复实验）

### 4.1 csproj 对照

| | LIT | LIR | LHT |
|---|---|---|---|
| 目标框架 | `v4.7.2`（`LaunchInventoryTidy.csproj:12`） | 同（`:12`） | 同（`:12`） |
| LangVersion | 10（`:14`） | 10（`:14`） | 10（`:14`） |
| Deterministic | true（`:15`） | true（`:15`） | true（`:15`） |
| LMN 引用 | HintPath `..\Libs\LaunchMultiplayerNet.dll`（`:67-69`） | HintPath `..\LaunchMultiplayerNet\bin\Release\LaunchMultiplayerNet.dll`（`:67-69`） | **ProjectReference** `..\LaunchMultiplayerNet\LaunchMultiplayerNet.csproj`（`:72-76`） |
| 额外引用 | Newtonsoft.Json（`:71-74`） | `SDG.Glazier.Runtime` + `UnityEngine.TextRenderingModule`（`:59-65`） | 同 Glazier + TextRendering（`:55-61`） |
| 共性 HintPath | `..\Libs\{0Harmony,BepInEx,Assembly-CSharp,UnityEngine,UnityEngine.CoreModule,com.rlabrecque.steamworks.net}.dll` | 同（无 Newtonsoft） | 同（无 Newtonsoft） |

BUE Plugin 已是 net472 / C# 10 / deterministic（`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj:6`），与三插件对齐。BUE 另引 `SDG.NetTransport`、`UnityEngine.InputLegacyModule`；**不**引 LMN DLL（官方纳入后也不应再引）。

### 4.2 DEV-V2-08 已实锤、勿重复的事实

来源：`audit/2026-09-05/DEV-V2-08/Delivery-DEV-V2-08-ecosystem-kit-20260905.md`、`DEV-V2-08-closing-report.md`。

- 归档区相对路径悬空；08 **未改 csproj**，用 junction `Archive\2-未闭环验证项目\LaunchMultiplayerNet` → LMN 仓库 + 新建 `Libs\`（9 引擎/BepInEx + 权威 LMN DLL）（交付报告 `:55-62`）。
- LHT 构建必须 `-p:BuildProjectReferences=false`，避免重编覆盖 LMN 权威件（`:46-48`）。
- 工具链：MSBuild 18.9.1.35102 / .NETFramework 4.7.2 / CSharp 10；Release `-t:Rebuild` 双轮 0 error / 0 warning，哈希逐字节一致（`:41-49`）。
- kit 身份（08 验证绑定，**不是**纳入后的产物）：LIT `7e35d7c7…5417` 151040B；LIR `6653035b…ac90` 52736B；LHT `6b935f5c…f995` 36864B；LMN `06d8a454…3055`；BUE 行 6 `35670269…aef6`（`:21-26`）。
- 三环境功能收发全过；缺口「不再需要独立 LMN DLL」08 不可验证（结单 `:35`、交付 `:63-69`）。

迁入 BUE 后上述 HintPath/ProjectReference **全部作废**（改为 Plugin EmbeddedCore 编译）。08 的 junction/Libs 只对「继续以独立 DLL 重建归档件」有意义。

### 4.3 版本 / CHANGELOG

| 插件 | BepInEx 版本 | 程序集 | CHANGELOG 状态 |
|---|---|---|---|
| LIT | `0.0.0`（`LaunchInventoryTidyPlugin.cs:17`） | `0.0.0.0`（`AssemblyInfo.cs:17-18`） | `[验收候选] - 2026-08-20`：停用数字频道 100，频道 `com.yu80rice.launchinventorytidy.net`；三环境通过后才编号 `1.0.0.0`（`CHANGELOG.md:5-11`） |
| LIR | `0.0.0`（`LaunchInPlaceReloadPlugin.cs:24`） | `0.0.0.0` | `未发布开发构建 - 2026-08-20`：频道 `com.yu80rice.launchinplacereload.repack`；验收后才 `1.0.0`（`CHANGELOG.md:5-31`） |
| LHT | `0.0.0`（`LaunchHordeTrackerPlugin.cs:13`） | `0.0.0.0` | `[未编号验收构建] - 2026-08-20`：停用数字频道 102，频道 `io.github.yu80rice.launchhordetracker.horde-status`；验收后才 `1.0.0`（`CHANGELOG.md:5-21`） |

历史发布线（LIT v3.0.1 / LIR v3.0.0 / LHT v3.0.0）已冻结，08 验证对象是验收候选 0.0.0（交付报告 `:38`）。纳入 BUE 后版本号应并入 BUE 产品版本，**不必**单独授予三插件 1.0.0（产品方向：玩家只部署 `BetterUnturnedExperience.dll`，`CONTEXT.md:49-52,121-123`）。

---

## 5. LMN 命名频道 API → `IBueNetworkApi` 映射

契约面（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs` `BetterUnturnedExperience.Contracts.BueNetwork`，`:219-284`）：

```csharp
ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion);
bool UnregisterChannel(FeatureId channel);
IReadOnlyList<IConnectionSession> Sessions { get; }
NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable);
NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable);
NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable);
```

`IConnectionSession.Send(byte[] payload, bool reliable)`（`:270`）。契约**禁止** Unity/LMN/Unturned/BepInEx 类型（`:215-217`）。

Host 内部另有 `BueNetworkRuntime.Subscribe(FeatureId, Action<IConnectionSession, byte[]>)`（`BueNetworkRuntime.cs:120-122`），注释写明 **not on the frozen contract surface**。

### 5.1 逐 API 对照

| LMN 调用 | IBueNetworkApi 对应 | 缺口 |
|---|---|---|
| `RegisterNamedServerHandler(string, Action<CSteamID, BinaryReader>)` | **无直接成员**。频道身份 ≈ `RegisterChannel(new FeatureId(channelId), …)`；入站 ≈ Host `Subscribe`（服务器侧用 `session.PeerSteamId` 替代 `CSteamID`） | **契约缺口 A**：模块无法只凭 `IBueNetworkApi` 注册 handler；必须把 `Subscribe` 升到契约，或经 `IFeatureBootstrap` 注入运行时 |
| `RegisterNamedClientHandler(string, Action<BinaryReader>)` | 同上 `RegisterChannel` + `Subscribe`（客户端 handler 无 sender） | 同缺口 A；且 LMN 分 Server/Client 两张表，BUE 按会话方向隐式分流（需验证） |
| `UnregisterNamed*(string, handler)` | `UnregisterChannel(FeatureId)` **或** `Subscribe` 返回的 `IDisposable` | LMN 是 compare-and-remove 委托；BUE 频道注销不按委托身份。LIR 半注册回滚语义要对齐 |
| `SendNamedToServer(string, byte[], bool)` | `SendToServer(FeatureId, byte[], bool)` | 返回值：LMN `void` vs `NetworkSendResult`（热路径不抛，`:221`）。LIT/LIR 需处理 `NoSession`/`ChannelNotRegistered` |
| `SendNamedToClient(CSteamID, string, byte[], bool)` | `SendToClient(FeatureId, IConnectionSession, byte[], bool)` | **缺口 B**：无 `CSteamID` 重载；必须 `Sessions` 里按 `PeerSteamId` 查找。LHT 跳过本地 client/server 身份的循环（`HordeStatusNetwork.cs:302-321`）能否由 `SendToClients` 一次完成 = **未验证** |
| `BroadcastNamedToAllClients`（三插件均未调用） | `SendToClients` | LHT 故意不用广播 API |
| `BuildNamedMessage(Action<BinaryWriter>)` | 无（纯组包 helper） | 迁入后用 `MemoryStream`/`BinaryWriter` 自组 `byte[]` 即可，不占契约 |
| `ModTransport.Initialize()`（仅 LHT） | 无 | 由 BUE 网络模块生命周期取代；功能模块禁止自己 Initialize 传输层 |
| V1 `Register*(int)` / `Send*(int)` | `LmnV1CompatRegistry`（**不是** `IBueNetworkApi`） | 三插件**不走**此路（T8 口径） |

### 5.2 逐插件映射难度

**LHT（低）**  
调用集最小：Initialize + RegisterNamedClient + UnregisterNamedClient + BuildNamedMessage + SendNamedToClient。业务是单向服务器→客机快照。映射为 `RegisterChannel(FeatureId("io.github.yu80rice.launchhordetracker.horde-status"))` + `Subscribe` + `SendToClient`/`SendToClients`（Update 不可靠、Clear 可靠）。无请求-响应、无服务端 handler。剩余：本地身份跳过 vs `SendToClients`；`Subscribe` 契约缺口。

**LIR（中）**  
双端 handler + 请求/成功回包 + 主线程队列，形状接近 `SendToServer`/`SendToClient` + `Subscribe`。额外：`CSteamID`→session；`NetworkSendResult`；与 LIT 的 Harmony 软依赖改为同进程事件。无会话 challenge。

**LIT（高）**  
双端 handler + 五种消息 + session challenge（连接时 `SendNamedToClient`）+ 可靠请求-响应 + 卸载三阶段。`RegisterChannel` 一次对应 LMN 两次 RegisterNamed*。`IConnectionSession` 生命周期事件（`Connected`/`Disconnected`，契约 `:267-268`）可替换部分 `Provider.onEnemyConnected/Disconnected`，但 LIT 还用这些事件建 token/清熔断——不能 1:1 删除 Provider 订阅。`BuildNamedMessage` 多处，负载版本封套保留为功能私有（契约不定义业务协议，`CONTEXT.md:65-67`）。

### 5.3 与 T8 判据的关系

T8：三件已迁 LMN **命名频道**，不是 V1 数字频道消费方（T8 `:37-44,100-103`）。因此：

- 官方纳入重写目标是 **BueNetwork 命名频道**，不是 `LmnV1Compat*`；
- 历史频道号 100/101/102 只存在于 LMN `ModChannels.cs` 与 CHANGELOG，源码已不用；
- 「已知生态 3/3 已迁 V2」≠「已迁 BueNetworkApi」。08 验证的是 LMN 命名频道在 BUE **接管态**下工作。

---

## 6. 迁入形态建议

### 6.1 约束（已拍板 / 典籍）

- 官方纳入 = 吃掉消化：能力以 BUE 实现/契约/生命周期重新表达，原项目不再作为独立运行时（`CONTEXT.md:29-32,49-52`）。
- 玩家侧单 DLL：`BetterUnturnedExperience.dll`；仓库内可按领域模块化、构建时聚合（`CONTEXT.md:121-123`）。
- 官方功能与生态功能走同一套公开契约，无框架私有特权（`OfficialFeatureRegistration.cs:6-8`，`NetworkModuleFeatureRegistration.cs:8-10`）。
- 显示名：LIT=背包整理，LIR=更好的换弹体验，LHT=更好的尸潮播报（`map.md:17`）。

### 6.2 推荐落位（方案 A，推荐）

与现有 **EmbeddedCore / EmbeddedClientUi Compile Include** 同构（`BetterUnturnedExperience.Plugin.csproj:11-59`），不要给三功能各自产出 BepInEx DLL。

```
src/BetterUnturnedExperience.InventoryTidy/     # 领域项目，不生成玩家 DLL
src/BetterUnturnedExperience.InPlaceReload/
src/BetterUnturnedExperience.HordeTracker/
src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj
    Compile Include="..\BetterUnturnedExperience.InventoryTidy\**\*.cs"
        Link="EmbeddedOfficial\InventoryTidy\%(RecursiveDir)%(Filename)%(Extension)"
    （LIR / LHT 同理）
    排除 TIDY_TEST_HARNESS / AutoTest* / *TestProbe / Fixture*
src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs
src/BetterUnturnedExperience.Plugin/InPlaceReloadFeatureRegistration.cs
src/BetterUnturnedExperience.Plugin/HordeTrackerFeatureRegistration.cs
```

每个 `*FeatureRegistration` 仿 `OfficialFeatureRegistration.cs:10-52`：`IFeatureRegistration` + `IFeatureModuleFactory` + `IFeatureModule.Start/Stop`，经 `BueRuntimeHost.Register` 进入，无私有通道。

**FeatureId 建议**（频道身份 = FeatureId，契约 Q1，`ContractTypes.cs:233`）：

| 功能 | 建议 FeatureId（沿用已发布命名频道，避免再迁一次线格式身份） |
|---|---|
| 背包整理 | `com.yu80rice.launchinventorytidy.net` |
| 更好的换弹体验 | `com.yu80rice.launchinplacereload.repack` |
| 更好的尸潮播报 | `io.github.yu80rice.launchhordetracker.horde-status` |

产品显示名走面板本地化，不改 FeatureId（`map.md:17`）。是否改成 `io.github.yu80rice.bue.*` 是产品决策，改则与旧独立 DLL **线不互通**（三插件 CHANGELOG 已声明与数字频道旧包不互通）。

**必须删除/改写**：

- `[BepInPlugin]` / `[BepInDependency(LMN)]` / `BaseUnityPlugin` 入口 → `IFeatureModule`；
- `LmnDependencyGuard` / `RuntimeDependencyGuard` / `ModTransport.Initialize`；
- 全部 `ModTransport.*` → `IBueNetworkApi`（+ 入站订阅，见缺口 A）；
- LIR `TidyServicePostfixPatch` 的 `TypeByName("LaunchInventoryTidy…")` → 同进程调用或功能事件；
- LIT `SteamP2PFriends.BeginScope` 约定 → BUE 会话/设置作用域。

**可保留为功能私有**（不进契约）：负载字节布局、熔断/账本/epoch-seq、排列算法、Harmony 对游戏类型的补丁（改由模块 `Start` 安装、`Stop` `UnpatchSelf`，Harmony ID 收到 FeatureId 下）。

csproj 形态：领域项目可为 net472 类库供测试引用，**Release 玩家产物只来自 Plugin 嵌入列表**（与 Contracts/Core 现状一致）。不要 `ProjectReference` 出第二份 DLL 丢进 plugins。

### 6.3 不推荐的形态

| 方案 | 为何否 |
|---|---|
| B 原样复制三套 csproj 为独立 BepInEx 插件，BUE 只做加载器 | 违反「吃掉消化」与单 DLL（`CONTEXT.md:29-32,121`） |
| C 嵌入但保留 `ModTransport` 调用 | 玩家仍要独立 LMN；与 08 缺口/目的地「只装 BUE」相反 |
| D 走 `LmnV1CompatRegistry` 把命名频道塞回数字频道 | T8：三件已离开 V1；V1 层只服务未知旧插件（`CONTEXT.md:77-79`） |

### 6.4 与 V1 兼容层边界

`LmnV1CompatLayer` / `LmnV1CompatRegistry` / `LmnV1FrameCodec` / `LmnV1TableMirror` 服务 **LMN2 之外的 `MOD` 魔数数字频道**（`LmnV1CompatLayer.cs:8-16`）。三官方功能：

- **禁止**向 V1 注册表登记 100/101/102；
- **禁止**新数字频道入口（`CONTEXT.md:69-71,77-79`）；
- 纳入后独立 LIT/LIR/LHT DLL 应停维护；若玩家同时装旧独立 DLL，属重复加载，应用面板/文档说明而非 V1 层去兼容两套命名频道实现；
- 裸 BUE（无独立 LMN）时未知 V1 旧插件无传输后端——这是 map 已承认的边界（`map.md:11`），与三功能纳入正交。

网络模块继续默认启用（`CONTEXT.md:85-87`）；三功能作为可选官方功能，面板可关，但 BUE 必须提供（`CONTEXT.md:33-35,53-55`）。

### 6.5 配置迁入

三件都不是 `ConfigEntry` 插件。纳入后：

- 玩家可调项（整理模式/方向、功能开关）进 `IScopedFeatureSettings` / `SettingsRuntime`（已嵌入 Plugin，`Plugin.csproj:27`），权威仍在功能模块（`CONTEXT.md:37-39`）；
- LIT 持久熔断 JSON 是安全状态而非偏好，保持功能私有磁盘权威，**不要**复制进第二事实源；
- LIR/LHT 常量暂时可保留，直到规格要求面板化。

---

## 7. 仍是推断、需验证的点（具名）

1. **缺口 A 产品决策**：`BueNetworkRuntime.Subscribe` 是否升格进 `IBueNetworkApi`，或只经 `IFeatureBootstrap` 注入。当前冻结契约没有接收 API（`ContractTypes.cs:276-284` vs `BueNetworkRuntime.cs:120-122`）。T3 生产绑定票应闭合。
2. **缺口 B / LHT 广播**：`SendToClients` 是否自动排除本地听主机身份，语义是否等于 `HordeStatusNetwork.SendToRemoteClients`（`:293-323`）。08 验证的是 LMN `SendNamedToClient` 循环，不是 BUE `SendToClients`。
3. **BueNetwork 生产传输**：`IBueNetworkApi` 已实现于 `BueNetworkRuntime`，但三功能作为第一批真实消费者时，Host 是否已把 live 传输绑到该运行时（vs 08 的 LMN 原生派发）。map 目的地写明「BueNetworkApi 有生产传输绑定」（`map.md:11`）——实施前需对照 T3 票，本盘点未跟代码路径逐帧确认。
4. **Server/Client 双表 vs 单一 Subscribe**：LIT/LIR 在同一进程既当 server 又当 listen-host client。BUE 一个 FeatureId 一次 `RegisterChannel` 是否覆盖双方向，listen-host 会不会自收请求包。
5. **LIT P2P 熔断 scope**：`BeginScope("p2p", …)` 的调用方是 SteamP2PFriends（`LaunchInventoryTidyPlugin.cs:124-126`）。BUE 纳入后谁在会话边界调用？无调用则 P2P 持久熔断不加载。
6. **LIR ↔ LIT Harmony postfix** 在同 DLL 下的正确缝（直接调用 vs 功能事件 vs 仍 postfix）。postfix 失败被吞（`TidyServicePostfixPatch.cs:52-56`），回归可能静默。
7. **LIR `forceAddItem` / LIT 库存 UI** 与 BUE 更好的物品交互（`NativeInventoryInteractionAdapter` 等）的 Harmony 冲突。未做补丁目标交集分析。
8. **O-LIT-1** 排列算法质量（08 结单 `:33`，非网络缺陷）——纳入时改不改算法是独立决策。
9. **FeatureId 字符串**：沿用 `com.yu80rice.*` 频道 vs 改 `io.github.yu80rice.bue.*`。沿用可减少线格式再迁；改名则与残余独立 DLL 不互通（可能是期望）。
10. **测试夹具**：LIT `TestHarness` 配置与 `#if TIDY_TEST_HARNESS` 文件是否在 BUE 测试工程重建，或放弃该套实机自动测试。
11. **`ModTransport.Initialize` 双调用**：LHT 显式 Initialize、LIT 假设 LMN Awake 已 Initialize。纳入后只应由网络模块 Initialize 一次。
12. **不可靠通道**：LHT Update `reliable:false` 在 BUE 帧上是否仍 1:1（08 P2P seq=1..10 是 LMN 路径）。

---

## 8. 给 parent 的三条收口

### ① 三插件迁移难度排序与理由

**LHT < LIR < LIT**（网络改写 + 生命周期消化的综合，不是 08 已验证的收发难度）。

- **LHT 最低**：2242 LOC、只注册 client handler、单向广播、无 ConfigEntry、无跨插件依赖；主要工作是 Initialize 所有权上收、`CSteamID` 循环改为 session/`SendToClients`、HUD Harmony 搬进 `IFeatureModule`。
- **LIR 中等**：3105 LOC、请求-响应双端、主线程队列已隔离；额外消化 LIT 软依赖 postfix 与 `UseableGun`/`forceAddItem` 补丁冲突。
- **LIT 最高**：14811 LOC（测试夹具约 4k 还要剥离）、五种消息 + session challenge + 持久熔断磁盘 + 多类静态表 + UI 反射 + Provider 会话钩子 + 卸载三阶段；且是 LIR 功能 A 的上游。

### ② 迁入形态推荐方案

方案 A：三领域项目源码迁入 `src/BetterUnturnedExperience.{InventoryTidy,InPlaceReload,HordeTracker}`，由 `Plugin.csproj` EmbeddedOfficial Compile Include 进单一 `BetterUnturnedExperience.dll`；各做 `IFeatureRegistration`/`IFeatureModule`；网络只走 `IBueNetworkApi`（补齐入站订阅）；**不**使用 `LmnV1Compat*`；删除 BepInEx 插件身份与 LMN 硬依赖。频道 FeatureId 默认沿用现命名频道字符串。

### ③ 仍是推断需验证的点

见 §7 十二条；实施阻塞最高的是 **§7.1 Subscribe 契约缺口**、**§7.3 生产传输绑定是否已接好**、**§7.4 listen-host 双角色**、**§7.5 LIT P2P scope 调用方消失**。
