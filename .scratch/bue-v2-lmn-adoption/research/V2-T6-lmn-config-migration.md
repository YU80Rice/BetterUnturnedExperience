# V2-T6: LMN 配置迁移映射查证

- 调查对象：BUE 接管独立 LMN 时"LMN 配置键 → BUE 设置模型"的迁移映射、保留有效值、可回滚、可诊断（CONTEXT.md「LMN 配置迁移」L81-83，原文：*BUE 接管独立 LMN 时自动读取并迁移兼容配置，保留原有有效值；迁移应可回滚并记录结果，不直接破坏旧配置文件*）。
- 调查性质：只读查证，无任何代码改动，未关闭 ticket。
- 权威主源（均已直接核验，非二手）：
  - LMN 源码仓库 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`（V5，`Core\LaunchMultiplayerNetPlugin.cs`、`Routing\ModTransport.cs`、`Sessions\ConnectionSessionManager.cs`、`Sessions\ConnectionSession.cs`、`Routing\NamespacedTransport.cs`、`LaunchMultiplayerNet.csproj`、`README.md`/`RELEASE.md`）。
  - BUE 源码 `src\BetterUnturnedExperience.Contracts\ContractTypes.cs`、`src\BetterUnturnedExperience.Core\Settings\SettingsRuntime.cs`、`src\BetterUnturnedExperience.Plugin\LoadedPluginCatalogAdapter.cs`、`src\BetterUnturnedExperience.Plugin\BueNativeManagementPanel.cs`、`src\BetterUnturnedExperience.Transport\LmnTransportAdapter.cs`。
  - 本 effort 既有已 resolved 调研：`research/V2-T5-lmn-takeover-mechanism.md`（LMN 加载/停用事实）、`research/V2-T8-v1-ecosystem-inventory.md`。

---

## 结论

> **LMN 没有任何配置系统，也没有任何运行时持久化状态。** 独立 LMN 的整份"配置"就是不存在——它不读取 BepInEx Config，不维护自定义 config 文件，不持久化会话/频道状态；其全部运行条件都由它自身 `Awake()` 内的硬编码逻辑决定。因此 T6 的候选映射表是**空表**：**没有需要迁移的 LMN 配置键**。CONTEXT「LMN 配置迁移」L81-83 的"自动读取并迁移兼容配置"在 LMN V5 上不产生实际条目，属于**空迁移（no-op）**；真正需要迁移/读取的不是文件，而是本 tick 已 inline 到 C# 的各协议常量（频道魔数、GUID、处理逻辑），那些随 T3/T4 的 `BueNetworkApi` / V1 兼容路径实施，不属于"配置键"范畴（见「局限」）。

这从根本上改变了 T6 的验收语义：**没有旧配置文件可破坏**，所以「不直接破坏旧配置文件」（L83 avoid）自动满足；「迁移可回滚并记录结果」（L82）降级为「记录一次空迁移结果/证明无配置可迁」的轻量动作。待格审项集中在「是否接受空迁移语义、是否仍要在面板展示一次状态」。以下分述。

### 1. LMN 的配置事实（问题 1）

**配置文件位置/格式/键集合：不存在。**

- `LaunchMultiplayerNet\Core\LaunchMultiplayerNetPlugin.cs` 的 `Awake()`（L50-74）**完全没有** `Config` / `ConfigEntry` / `BepInEx.Configuration` 引用。它只做 `DontDestroyOnLoad`、`ModTransport.Initialize()`、创建 Harmony、应用两个 Receive 补丁、自检。继承自 `BaseUnityPlugin` 的 `.Config`（BepInEx 提供）从未被读取或写入。
- 全仓库 grep `Config|ConfigEntry|BepInEx.Configuration|.cfg|File.Write|File.Read|.json|.ini` 在 `.cs` 上 **0 命中**。`Routing\ModTransport.cs` 的 `using System.IO;`（L4）只用于 `MemoryStream`/`BinaryReader`/`BinaryWriter`（L344-346、L763-766、L818-827 等）——是网络载荷字节序列化，不是文件持久化。
- `LaunchMultiplayerNet.csproj`（全 102 行）**无任何 `<Content>` / `<EmbeddedResource>` 项**，`OutputType=Library` 单 DLL（L8）。
- `README.md` L36-42「发布包只包含：`BepInEx/plugins/LaunchMultiplayerNet.dll`」；`RELEASE.md` L16「发布包只包含： `BepInEx/plugins/LaunchMultiplayerNet.dll`」。
- 全仓库（含 assets/publish/docs/测试）glob `**/*.{cfg,json,ini,toml,yaml,yml}` 仅命中 build 产物（`*.deps.json` / `*.runtimeconfig.json` / sourcelink），非运行期配置。
- 源码的全部 `File.*`/`Path.*` 命中仅在测试项目 `PatchValidationTests.cs`（L45-49 加载 DLL 用），生产代码无磁盘写。

**键集合：空。** 因为无配置模型，故无 name/type/default/scope/section 可列举。LMN 的"可调参数"全部是 C# 常量/字段，且只读（不可由玩家配置）：`ModTransport` 的暂存队列上限 `MaxPendingPerSide=64`/过期 `PendingExpireSeconds=120f`/告警窗口 `NormalWarningWindowSeconds=10d`（L86-87、L58）、`NamespacedTransport` 的 `MaxPluginGuidBytes=96`/`MaxNamedPayloadBytes=60*1024`（L16-17）、魔数 `MOD`/`LMN2`（`ModRouter.cs`）、GUID `com.yu80rice.launchmultiplayernet`（plugin L44）。这些是协议/实现常数，不是用户配置，BUE 接管后由 T3/T4 的在 BUE 内重表达，不进入设置 facet。

**作用域（per-server/per-client）：不适用。** LMN 无 per-scope 配置；它对 client/server 的处理由运行时 `Provider.isServer/isClient` 现场判定（`ModTransport.SendToServer` L430、`SendToClient` L464、`RuntimeEnvironment.GetDiagnosticSnapshot` L19），不是配置驱动的作用域。BUE 的 `ClientPreference`/`ServerAuthority` 两作用域在此无源可映射。

### 2. LMN 是否持久化会话/频道状态（问题 2：可忽略）

**否。所有状态均进程内存，随会话销毁，无可迁移、也无需忽略的持久文件。**

- `Sessions\ConnectionSessionManager.cs`（全 218 行）：会话存于 `static` 字典 `_sessions`/`_steamIdToSession`（L19-22），由 `Provider.onEnemyDisconnected`/`onServerDisconnected` 事件清理（L179-216），`Shutdown()` 时 `ClearAllSessions()`（L56-73）。**无任何 File 写。**
- `Sessions\ConnectionSession.cs`：`_supportedChannels`/`_customStates` 为实例字典（L17-18），`SetState/TryGetState<T>` 为通用临时状态容器（L64-86），`Dispose()` 只释放 `IDisposable` 并清空（L88-106）。**不落盘。**
- `Routing\NamespacedTransport.cs`：handler 表为 `static` 字典（L19-22），`Initialize/Shutdown` 只增删内存表（L24-40、L391-395）。
- `Routing\ModTransport.cs`：`ServerHandlers`/`ClientHandlers`/pending/告警表全是 `static` 内存（L66-88），`Shutdown()` 清空（L112-131）。
- 结论与 T5 报告 L94 一致：「LMN 侧零持久状态改动，静态表只在内存且随会话销毁。」这些状态**不是配置**，也不需要在配置迁移里"迁移或忽略"——它们天然不存在于磁盘。

### 3. BUE 侧设置模型对照（问题 3）

BUE 把"作用域 + 有效值 + 快照"表达为 **Settings Facet**：一个 `FeatureId` + 一组 `SettingDescriptor` 的 `SettingsRuntime`（`SettingsRuntime.cs` L230-526），内部为每个作用域各持一份 `ScopeState{Revision, Values, Replay}`（L517-523），由 `ISettingsPersistence`（L31-36）按 `(feature, scope)` 原子持久化。

| BUE 概念 | 表达位置 / 行号 | 与 LMN 的对应 |
|---|---|---|
| 作用域 | `SettingRevisionScope { ClientPreference, ServerAuthority }`（ContractTypes.cs L135）；`SettingsRuntime` 两个 `ScopeState client/server`（L238-239） | LMN 无 per-scope 配置（见结论 1），无从映射 |
| 有效值 | `SettingValue{Kind,Boolean,Integer,Float,Text}`（L138-151）；`SettingEntryView.EffectiveValue`（L163）；`BuildEntry` 依 policy/preference 投影（SettingsRuntime L423-431） | 无源 |
| 快照 / 原子 revision | `FeatureSettingsSnapshot{Feature,SchemaVersion,RevisionScope,Revision,SyncState,Source,Entries}`（L166）；`ScopeState.Revision` 单调递增（`Apply` L400-405） | 无源 |
| 持久化 | `FileSettingsPersistence`：`.bue-settings` 文档，临时文件写 + `File.Replace` 原子替换，SHA-256 digest，schema 版本，损坏文件隔离改名 `.corrupt.*.bak`（SettingsRuntime L92-228） | 若未来有配置键，应迁入此类文档 |
| 权威 | `SettingAuthority { ClientLocal, ServerAuthoritative, ServerPolicyWithClientPreference }`（L137） | LMN 无权威配置 |

**迁移映射要落到哪个 facet/scope 结构**：若 LMN 真有配置键，BUE 应为其内置网络模块声明一个 `FeatureId`（network module）的 Settings Facet，把可玩家调整的键注册为 `SettingDescriptor`，落到 `ClientPreference` 或 `ServerAuthority` 作用域，走 `SettingsRuntime.Submit` + `FileSettingsPersistence.TryCommit`。**但正文结论是：这类键当前不存在**，故该 facet 在 T6 上只需为一个空/仅含开关的 Network 模块设定，不产生迁移条目。

### 4. 迁移映射候选（问题 4）——空表，附非配置项的处置

LMN V5 没有玩家可配置键，因此候选映射表为空。唯一在接管语境里"从 LMN 迁到 BUE"的东西是**协议常数/身份**，但它们不是配置，处置如下：

| LMN 项（源码身份/常量） | 类型 | 是否"配置键" | BUE 处置建议 | 说明 |
|---|---|---|---|---|
| 全部 BepInEx ConfigEntry | — | 无（不存在） | 不迁移 | 无任何 Config/ConfigEntry 引用 |
| `com.yu80rice.launchmultiplayernet`（plugin L44） | GUID 字符串 | 否，插件身份 | 由 T5 检测面复用；不迁入设置 | `Chainloader.PluginInfos.ContainsKey(GUID)`（T5） |
| `MOD` / `LMN2` 魔数（ModRouter.cs L13-24） | 协议常数 | 否，协议 | 由 T3/T4 在 BUE 帧格式内固化 | V1 兼容路径（T4）、V2 命名频道（T3） |
| `MaxPendingPerSide=64`/`PendingExpireSeconds=120f`（ModTransport L86-87） | 实现常数 | 否（不可玩家配） | 由 BUE 网络模块按需内固化 | 可选暴露为网络模块设置，属 T7 决策 |
| `MaxPluginGuidBytes=96`/`MaxNamedPayloadBytes`（NamespacedTransport L16-17） | 协议限制 | 否 | 由 T3 固化 | 帧格式约束 |
| 各 `ConnectionSession._customStates` | 运行态 | 否（内存、随会话亡） | 忽略 | 见结论 2 |
| 消费方插件（LIT/LIR/LHT）各自的 BepInEx Config | 其他插件配置 | 不属于本 tick | 不迁移，保留原文件 | 属于各自纳入（T8 范畴），非 LMN 配置 |

**类型兼容性**：无 BepInEx ConfigEntry 基础类型可对照；若未来 BUE 网络模块暴露设置，`SettingKind{Toggle,Integer,Float,Text,KeyBinding,Choice}` 已覆盖 BepInEx 基础布尔/数字/字符串（且 `LoadedPluginCatalogAdapter.CanEdit` L94-102 已演示 bool/整型族/浮点族/string 与 BepInEx `ConfigEntry` 的对应关系可复用）。

### 5. 回滚语义（问题 5）

**BUE 现有设置模型的"快照/回滚"能力——需区分两类：**

- **BUE 自有设置（Settings Facet）**：`FileSettingsPersistence.TryCommit` 是**原子提交**（临时文件写 + 读回校验 `SameValues`/revision + `File.Replace`，L138-163）；失败返回 `SettingPersistenceFailed` 且**不改动现有文档**（L402：commit 失败则 reject、state 不变）。`SettingsRuntime` 通过 `Replay` 表做请求幂等（L284-295），revision 冲突即拒绝（L293）。即 BUE 设置写入天然是"失败不留痕、成功才替换"，**这本身就是回滚-safe**。但它**没有跨请求的多步事务/undo 栈**——`Submit` 追加式提交，一旦成功 revision 递增，没有"撤销上一次 commit"的 API。若迁移需要"整批成功否则整体回退"，须由迁移协调器做两阶段：先全部迁移键**载入内存校验**（`ValueValid` L455-477 逐键验），再一次性原子 `TryCommit` 到目标 scope；`Apply` 的 mutation 数组天然支持一批键单次提交（L384-406），正好可用。
- **旧独立 LMN 文件**：本 tick 无文件可回滚（空迁移）。若未来有（例如某个 LMN 版本引入配置），回滚策略应遵循 T5 L94-95 的恢复语义：**不删不改旧文件**，迁移只"读取→写入 BUE 文档"，用户移除 BUE 后 LMN 自带配置仍原样可用（前提是 BUE 不覆盖旧文件）。这与 CONTEXT「独立 LMN 文件处理」L85-87（不删除、由用户自行清理）一致。
- **迁移失败恢复路径（若未来有键）**：a) 读旧值时逐键 `ValueValid`；b) 任一键无效 → 整体不提交，回 `SettingMigrationFailed=1309`（ContractTypes.cs L181 已预留），旧文件不动；c) 旧文件损坏 → 复用 `FileSettingsPersistence.Load` 的 `.corrupt.*.bak` 隔离而非破坏（L122-123 模式）。本 tick 不实施，作为待格审后的 DEV 设计输入。

---

## 证据（文件 + 行号）

### LMN —— 无配置来源
- `Core\LaunchMultiplayerNetPlugin.cs`：L41-45 入口身份（GUID/Version）；L50-74 `Awake()` 全动作清单，**零 Config 引用**；L76-86 `Update/OnDestroy`。
- 全仓库 `.cs` grep `Config|ConfigEntry|BepInEx.Configuration|.cfg|File.Write|File.Read|.json` → **0 命中**（生产代码）。
- `Routing\ModTransport.cs`：L4 `using System.IO` 仅网络序列化；L58 `NormalWarningWindowSeconds`、L86-87 `MaxPendingPerSide/PendingExpireSeconds`（只读实现常数）；L66-88 静态 handler/pending 表；L108-131 `Shutdown` 清内存。
- `Sessions\ConnectionSessionManager.cs`：L19-22 静态会话字典；L56-73 `Shutdown→ClearAllSessions`；L179-216 断开事件清理。无文件写。
- `Sessions\ConnectionSession.cs`：L17-18 `_supportedChannels/_customStates` 内存字典；L64-86 `SetState/TryGetState<T>`；L88-106 `Dispose` 内存回收。
- `Routing\NamespacedTransport.cs`：L16-17 `MaxPluginGuidBytes/MaxNamedPayloadBytes`；L19-22 静态 handler 表；L24-40/391-395 `Initialize/Shutdown` 内存增删。
- `LaunchMultiplayerNet.csproj`（全 102 行）：无 Content/EmbeddedResource；L8 `OutputType=Library`；L35-67 仅库引用。
- `README.md` L36-42 / `RELEASE.md` L16：发布包只含 `BepInEx/plugins/LaunchMultiplayerNet.dll`。
- glob `**/*.{cfg,json,ini,toml,yaml,yml}` → 仅 build 产物（deps/runtimeconfig/sourcelink）。
- `LaunchMultiplayerNet.Tests\PatchValidationTests.cs` L45-49：唯一 `File./Path.` 命中在测试（加载 DLL），非持久化。

### BUE —— 设置模型
- `src\BetterUnturnedExperience.Contracts\ContractTypes.cs`：L135 `SettingRevisionScope`；L136-137 `SettingKind/SettingAuthority`；L138-151 `SettingValue`；L156-167 描述符/策略/条目/快照；L174-184 `FrameworkErrorCode`（L181 `SettingMigrationFailed=1309` 预留）；L96-101 `IScopedFeatureSettings`。
- `src\BetterUnturnedExperience.Core\Settings\SettingsRuntime.cs`：L31-36 `ISettingsPersistence`；L92-228 `FileSettingsPersistence`（L94 Magic、L132-174 `TryCommit` 原子 + 读回校验、L160 `File.Replace`、L122 损坏隔离）；L230-526 `SettingsRuntime`（L238-239 client/server scope、L277-297 `Submit` 幂等+revision、L353-369 `LoadScope` 兼容载入、L382-406 `Apply` 多 mutation 原子提交、L402 `SettingPersistenceFailed` reject 留原）。
- `src\BetterUnturnedExperience.Plugin\LoadedPluginCatalogAdapter.cs`：L94-102 `CanEdit`（bool/整型族/浮点族/string 对照）、L20-60 `CaptureLoadedPlugins`（读 `instance.Config` 的 `ConfigEntry`）、L62-92 `TrySet`（写回 + 读回校验 + `config.Save()`）——这是 BUE 对"普通 BepInEx ConfigEntry"的既有兼容适配面（CONTEXT「BUE 兼容配置编辑」）。**LMN 不使用 Config，故此适配面在 LMN 上不会列出任何条目。**

### 交叉引用
- `research/V2-T5-lmn-takeover-mechanism.md` L94：「LMN 侧零持久状态改动…静态表只在内存且随会话销毁」；L66；（接管用 `PluginInfos.ContainsKey(GUID)`）。
- `research/V2-T8-v1-ecosystem-inventory.md`：LMN 生态消费方配置属各自纳入，非本 tick。
- `CONTEXT.md`：L81-83「LMN 配置迁移」；L85-87「独立 LMN 文件处理」。

---

## 待格审项（open decisions）

1. **是否接受"空迁移"语义**：LMN V5 无配置键，T6 结论是"无可迁移"。需格审确认 T6 是否就此接受为 no-op（仅记录/证明），还是要求在 BUE 侧预建一个网络模块 Settings Facet（哪怕空/仅开关）作为迁移落点骨架。倾向：接受空迁移，网络模块开关见 T7，不为无源建 facet。
2. **回滚 UX**：CONTEXT L82「迁移应可回滚并记录结果」。空迁移下"结果记录"应落到哪——结构化日志（`BueRuntimeLog` diagnosticId 惯例）+ 面板一行状态（与 T5「已由 BUE 接管」同款）？是否要某处持久化"已做过空迁移"标记以防每次启动重复跑？若否（幂等空跑），面板只显示接管即可。
3. **面板呈现**：CONTEXT「网络接管用户提示」L101-103 已要求面板显示"已由 BUE 接管"和"配置迁移状态"。空迁移时配置迁移状态显示为"无独立配置可迁移（LMN 无配置文件）"还是隐藏该行？倾向：为开发者/玩家透明，显示一行说明，避免玩家误以为有配置丢失。
4. **未来键的预留**：是否需要在 BUE 网络模块 facet 里固定一段"迁移适配器"接口（读外部 config → `SettingValue`），以备 LMN 或消费方将来引入配置；还是 YAGNI，等真出现再设计？倾向：YAGNI，正文以 DEV 设计输入记录而非承诺实现。
5. **协议常数是否算配置**：`MOD/LMN2` 魔数、GUID、缓存上限等是否必须在 BUE 网络模块内以可配置项暴露？倾向：否（协议身份/实现细节，T3/T4 内固化），需格审划清以免误把协议常数当用户设置。

---

## 局限

- **结论基于 LMN V5 源码状况**；若 LMN 后续版本引入 BepInEx Config 或自建配置文件，本"空迁移"结论失效，需按第 8 节"未来键的预留"重新评估。当前存档（`Archive\2-未闭环验证项目`）与 V5 一致地为无配置。
- **不含消费方插件（LIT/LIR/LHT）的配置**：它们各自的 BepInEx Config 属于各自官方纳入/接管（T8），不在 "LMN 配置迁移" 边界内。若玩家只迁移 LMN 而旧消费方仍装，其配置不动，应由各自纳入流程处理。
- **未检查发布 ZIP 内是否有构建期注入的配置文件**：确认基于 `LaunchMultiplayerNet.csproj` 与仓库 glob；发布包清单（README/RELEASE）已明确单 DLL，未逐字节解包旧 ZIP（源码为权威，见 README L36）。
- 纯只读查证，未改动任何源码；未关闭 ticket V2-T6。