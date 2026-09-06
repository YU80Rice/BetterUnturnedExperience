# BepInEx 5.4.23.5 前置解析机制与 Forge-like 承诺可行性

- 日期：2026-09-06
- 类型：research（只读；对照主源，不二手转述）
- 问题票：`.scratch/bue-v2-phase2-official-adoption/issues/01-bepinex-resolution-mechanism.md`
- 主源 DLL：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\BepInEx.dll`
  - `FileVersion` / `ProductVersion` = `5.4.23.5` / `5.4.23.5+57f1fb859bd4d0264cd2a59074d0e96c6a492a33`（Windows `FileVersionInfo`）
- 反编译：`ilspycmd` 10.1.1.8388（ICSharpCode.Decompiler 10.1.1.8388），类型 `BepInEx.Bootstrap.Chainloader` / `TypeLoader` / `BepInEx.Utility` / `BepInEx.BepInDependency` / `BepInEx.BepInPlugin` / `BepInEx.PluginInfo` / `BepInEx.BaseUnityPlugin`
- 行号约定：下文 `Chainloader Lxxx` = 对本机 5.4.23.5 DLL 反编译的行号。与既往研究引用的 L45 / L306 / L330-343 / L363-391 / L395-404 **逐条复核一致**（本会话重新反编译，未沿用旧文本）。
- 网络限制：`github.com` / `raw.githubusercontent.com` 本会话 ECONNRESET / 证书失败 / 超时；`docs.bepinex.dev` 教程页超时或 404。官方网页未能作为可引用段落主源。权威以本机 DLL + MSDN/Learn + 本仓库实机 `LogOutput.log` 为准。

---

## 1. 前置依赖解析按什么？

**结论：按插件 GUID，不按文件名。** 解析链是「Cecil 读 `[BepInPlugin].GUID` → 按 GUID 去重 → 按 `[BepInDependency].DependencyGUID` 建图 → 拓扑排序 → 硬依赖 GUID 必须已成功实例化」。

### 1.1 身份与依赖属性只认 GUID 字符串

`BepInPlugin` 构造函数三参数是 `(string GUID, string Name, string Version)`，GUID 写入 `BepInPlugin.GUID`（反编译 `BepInEx.BepInPlugin`）。

`BepInDependency` 只存储：

- `DependencyGUID`（构造函数第一参数，string）
- `Flags`（`HardDependency = 1` / `SoftDependency = 2`）
- `MinimumVersion`

（反编译 `BepInEx.BepInDependency`。）没有任何文件名 / `AssemblyName` 字段。

Cecil 侧：`BepInDependency.FromCecilType` 读自定义属性构造参数 `[0]` 为 GUID 字符串。`Chainloader.ToPluginInfo`（L179-229）用 `BepInPlugin.FromCecilType` + `BepInDependency.FromCecilType` 填 `PluginInfo`，不读文件名。非法 GUID（空或不符合 `^[a-zA-Z0-9\._\-]+$`，L106 / L202-205）直接跳过该类型。

### 1.2 `PluginInfos` 是 GUID → PluginInfo 字典，不是 SortedDictionary

```
Chainloader L45:
public static Dictionary<string, PluginInfo> PluginInfos { get; } = new Dictionary<string, PluginInfo>();
```

写入点在实例化成功之后：`PluginInfos[item6] = value2;`（L401），其中 `item6` 来自拓扑序列，即 GUID。查找点：`BaseUnityPlugin` 构造函数 `Chainloader.PluginInfos.TryGetValue(metadata.GUID, ...)`（反编译 `BepInEx.BaseUnityPlugin`）。

**易混点**：`PluginInfos` 本身是普通 `Dictionary`（枚举序未定义）。GUID 字典序出现在下一步的 `dependencyDict`，不是 `PluginInfos`。

### 1.3 发现阶段：扫 `*.dll`，但匹配仍用 GUID

`TypeLoader.FindPluginTypes`（反编译 `BepInEx.Bootstrap.TypeLoader` L61）：

```
Directory.GetFiles(Path.GetFullPath(directory), "*.dll", SearchOption.AllDirectories)
```

对每个路径 `AssemblyDefinition.ReadAssembly`（Cecil，读完 `Dispose`），用 `ToPluginInfo` 抽元数据。`Chainloader.Start` L295-301 把 **文件完整路径** 写入 `PluginInfo.Location`，供稍后 `LoadFile` 使用。文件名只是「从哪读这个 DLL」，不是依赖键。

MSDN：`Directory.GetFiles`「The order of the returned file names is not guaranteed」（https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.getfiles ）。因此发现阶段连文件名序都不是契约。

### 1.4 按 GUID 分组去重，再按 GUID 建依赖图

`Start` L306-327：

```
SortedDictionary<string, IEnumerable<string>> dependencyDict
    = new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);
Dictionary<string, PluginInfo> pluginsByGUID = new Dictionary<string, PluginInfo>();
foreach (IGrouping<string, PluginInfo> item3 in from info in list group info by info.Metadata.GUID)
{
    // OrderByDescending Version，保留最新；其余 LogWarning Skipping because a newer version exists
    dependencyDict[item4.Metadata.GUID] = item4.Dependencies.Select(d => d.DependencyGUID);
    pluginsByGUID[item4.Metadata.GUID] = item4;
}
```

同 GUID 多文件：只留版本最高的一份；同版本时 LINQ `OrderByDescending` 稳定排序，保留 GroupBy 中先出现的那份（即 `GetFiles` 的未定义序），其余仍打「newer version exists」日志（文案在同版本时不精确，但行为是跳过、不双开）。

不兼容：L330-343 按 `BepInIncompatibility.IncompatibilityGUID` 查 `pluginsByGUID.ContainsKey`，命中则从两字典移除并 `LogError`「Could not load [X] because it is incompatible with: …」。

### 1.5 拓扑排序 + 硬依赖 GUID 检查

L351-352：`Utility.TopologicalSort(dependencyDict.Keys, x => dependencyDict[x] or empty)`。

`Utility.TopologicalSort`（反编译 `BepInEx.Utility` L90-129）：DFS，先访问 `dependencySelector` 返回的节点（即依赖 GUID），再把自身追加到 `sorted_list`。环报 `Cyclic Dependency`。

然后 L355-392 按该序列走：

- 硬依赖 GUID 不在 `dictionary3`（已成功走到「记录版本」的插件），或已记录版本 `< MinimumVersion` → 记入 `list4`
- 硬依赖 GUID 在 `hashSet`（先前因缺依赖/加载异常被标记失败）→ `flag=true`，跳过
- `list4` 非空 → **`LogError`「Could not load [X] because it has missing dependencies: {GUID}」**（L388），`hashSet.Add`，不实例化
- 软依赖缺失：不进 `list4`，不阻止加载

`IsHardDependency`：`(dep.Flags & HardDependency) != 0`（L443-446）。默认构造 `BepInDependency(guid)` 的 Flags 就是 `HardDependency`。

### 1.6 实例化：`Assembly.LoadFile(Location)` + `AddComponent`

L394-404：日志 `Loading [{value2}]` → 按 `Location` 缓存 `Assembly.LoadFile` → `PluginInfos[guid]=info` → `ManagerObject.AddComponent(type)`（同步触发 Unity `Awake`）。异常则 `PluginInfos.Remove` + `hashSet.Add`，后续硬依赖方走「dependency that was not loaded」（L381）。

**本问钉死**：依赖检测与「挂起」（missing hard dependency）的键是 GUID。把 DLL 改名、只要还在 `plugins/` 下被 `*.dll` 扫到且 `[BepInPlugin]` GUID 不变，Chainloader 不会因为文件名找不到前置。

---

## 2. 加载顺序：发现 vs Awake

**结论：Awake / `Loading [...]` 顺序 = GUID 拓扑序（独立插件之间是 `SortedDictionary` 的 GUID 大小写不敏感字典序），不是文件名序。** 「BepInEx 按文件名序加载、BUE 的 B < LMN 的 L」把发现阶段的文件枚举与实例化顺序混为一谈，且与本仓库实机日志矛盾。

### 2.1 两阶段必须拆开

| 阶段 | 做什么 | 顺序由什么决定 | 是否 Load 进 CLR | 日志 |
|---|---|---|---|---|
| 发现 | `GetFiles(*.dll)` + Cecil 读特性 | 文件系统枚举，**序不保证**（MSDN GetFiles） | 否（Cecil `ReadAssembly` 后 Dispose） | 无 `Loading [..]` |
| 实例化 / Awake | `LoadFile` + `AddComponent` | `dependencyDict.Keys`（GUID `SortedDictionary` InvariantCultureIgnoreCase）上的拓扑序 | 是 | `Loading [{PluginInfo}]` 在 LoadFile **之前**（Chainloader L396-402） |

DEV-V2-11 把主机日志 L14 `Loading [LaunchMultiplayerNet]` 写成「发现阶段预载」（`audit/2026-09-04/DEV-V2-11/configB-retest-verification-r1.md` L30-31）——与 Chainloader L396 不符：`Loading` 就是实例化行，紧接着 `Awake`。

### 2.2 拓扑 + GUID 字典序如何排出实机顺序

`TopologicalSort` 的外层 `foreach (TNode node in nodes)` 的 `nodes` 是 `SortedDictionary.Keys`，即 GUID 已按 `InvariantCultureIgnoreCase` 排好。DFS 从字典序最前的 GUID 起步，但会先把依赖 GUID 放进结果。

本仓库已部署插件的 GUID（源码）：

| 插件 | GUID | 源 |
|---|---|---|
| LMN | `com.yu80rice.launchmultiplayernet` | `LaunchMultiplayerNet/Core/LaunchMultiplayerNetPlugin.cs` L44 |
| LIT | `com.yu80rice.launchinventorytidy` | `Archive/2-未闭环验证项目/LaunchInventoryTidy/LaunchInventoryTidyPlugin.cs` L15 |
| LIR | `com.yu80rice.launchinplacereload` | `Archive/2-未闭环验证项目/LaunchInPlaceReload/LaunchInPlaceReloadPlugin.cs` L22 |
| SPF | `com.yu80rice.steamp2pfriends` | `SteamP2PFriends/SteamP2PFriendsPlugin.cs` L24（实验包日志为 0.2.4.8） |
| BUE | `io.github.yu80rice.betterunturnedexperience` | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs` L12 |
| LHT | `io.github.yu80rice.launchhordetracker` | `Archive/2-未闭环验证项目/LaunchHordeTracker/LaunchHordeTrackerPlugin.cs` L11 |
| Fixture | `io.github.yu80rice.bue.lmn-fixture` | `audit/2026-09-04/DEV-V2-07/kit/LmnEcosystemFixture/LmnEcosystemFixturePlugin.cs` L25 |

硬依赖：LIT/LIR/LHT → LMN；LIR 另有 LIT 软依赖。BUE 无 `[BepInDependency]`。

字典序下 `com.yu80rice.launchinplacereload` 最先被 Visit → 先压入硬依赖 LMN、软依赖 LIT → 结果前段为 **LMN → LIT → LIR**，然后独立的 SPF，然后 `io.github.*` 的 BUE，然后依赖 LMN 的 LHT。

实机（`audit/2026-09-05/evidence/DEV-V2-08-20260905/env/u3ds-client/LogOutput.log` L13-41 与 L164 / L194）：

```
6 plugins to load
Loading [LaunchMultiplayerNet 5.0.0.0]          // L14
Loading [LaunchInventoryTidy ...]               // L21
Loading [LaunchInPlaceReload ...]               // L37
Loading [SteamP2PFriends 0.2.4.8]               // L41
Loading [Better Unturned Experience 0.0.0]      // L164
Loading [LaunchHordeTracker 0.0.0]              // L194
```

与 GUID 拓扑完全同构。若按**文件名**序，应为 `BetterUnturnedExperience` → `LaunchHordeTracker` → `LaunchInPlaceReload` → `LaunchInventoryTidy` → `LaunchMultiplayerNet` → `SteamP2PFriends`——与日志相反。

同结构在 DEV-V2-11 / V2-12 / V2-13 多份 `LogOutput.log` 复现：凡同时有 LMN 与 BUE，**永远 LMN 的 `Loading` 在 BUE 之前**（`com.*` < `io.github.*`），与文件名 B < L 预测的「BUE 最先」相反。

### 2.3 历史「B < L」主张的对账

- `audit/2026-09-05/DEV-V2-08/DEV-V2-08-ecosystem-handbook.md` L32：「BUE（无依赖声明）按文件名 B < L 最先」——**与同票实机日志矛盾**。正确说法：BUE 无依赖，落在 GUID 字典序里 `io.github...` 一段，晚于所有 `com.yu80rice.*`。
- DEV-V2-07 复核曾用「文件名序」解释镜像时机，后又被实机「LMN 先于 BUE」推翻（`configB-verification-r1.md` L53）——推翻方向对，机制应改记为 GUID 拓扑，而不是「其实发现阶段已经全部 Load」。
- 发现阶段 Cecil 扫描确实会碰到每个 `*.dll`（含 BUE 与 LMN），但那不是 CLR `Assembly.Load*`，也不能当成 Awake 序。

**本问：机制已钉死（源码 + 多份实机 Loading 序）。** 若还要做「两独立插件、GUID 序与文件名序刻意相反」的最小对照实验，属于重复证实，见文末待验清单。

---

## 3. 跨插件类型引用：CLR 按程序集名，不按文件名

**结论：IL 绑定键是程序集身份（简单名 / 版本 / 区域性 / 公钥标记），不是 DLL 文件名。文件改名、`AssemblyName` 不变时，绑定按规范应当成功。BepInEx 用 `Assembly.LoadFile(绝对路径)` 加载，路径来自发现到的真实文件，因此改名不阻止 BUE 自己被装入。**

### 3.1 CLR / MSDN 权威

https://learn.microsoft.com/en-us/dotnet/standard/assembly/names ：

> The runtime does not consider the file name when determining an assembly's identity. The assembly identity, which consists of the assembly name, version, culture, and strong name, must be clear to the runtime.

同页：私有程序集「can have an assembly name that is different from the file name」（GAC 强名称除外，BUE 无强名称：`PublicKeyToken=null`）。

实测程序集简单名（`AssemblyName.GetAssemblyName`）：

- BUE 产物 `BetterUnturnedExperience, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`（`artifacts/DEV-13-clean-final-20260825/BetterUnturnedExperience.dll`；csproj `AssemblyName`=`BetterUnturnedExperience`，`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj` L5）
- LMN `LaunchMultiplayerNet, Version=5.0.0.0, ...`（`Libs/LaunchMultiplayerNet.dll`）

第三方 IL 里的 `.assembly extern BetterUnturnedExperience` 绑的是这个简单名，不是 `BetterUNExperience.dll`。

### 3.2 `Assembly.LoadFile` 上下文

Chainloader L399：`Assembly.LoadFile(pluginInfo.Location)`。`Location` 是发现到的绝对路径，与当前文件名一致。

MSDN `Assembly.LoadFile`（https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfile ）：

- 按**路径**装入，用于「相同身份、不同路径」的程序集。
- **不进入 LoadFrom 上下文**，**不按加载路径解析依赖**（与 `LoadFrom` 不同）。
- 因此：BUE 文件叫什么，只要路径被 `LoadFile`，CLR 里的身份仍是元数据里的 `BetterUnturnedExperience`。

依赖方随后 `LoadFile(自己的路径)` 时，其对 BUE 的引用走默认 Load 探测，**不会**用依赖方目录里的文件名去「找 BetterUNExperience.dll」。它要的是已加载或可探测的 **`BetterUnturnedExperience` 身份**。

BepInEx 5 的可靠点在于：**硬依赖拓扑保证 BUE 的 `LoadFile` 发生在依赖方 `LoadFile`/`AddComponent` 之前**（§1.5 + §2）。LIT 实机在 LMN `Loading` 之后立刻 `typeof(LaunchMultiplayerNetPlugin)` / `ModTransport`（`LaunchInventoryTidyPlugin` + `LmnDependencyGuard`；DEV-V2-08 日志 L21-26），证明「先 `LoadFile` 前置、再 `LoadFile` 消费方并引用其类型」在 **Unity 2022.3.62 Mono + BepInEx 5.4.23.5** 这条链上是通的。

### 3.3 与文件名有关、但不是 Chainloader 依赖解析的路径

`Utility.TryResolveDllAssembly`（L132-154）会拼 `assemblyName.Name + ".dll"` 再 `File.Exists`。这是 **Cecil 解析失败时的文件探测**（`TypeLoader` 静态构造里 `Resolver.ResolveFailure`，L31-49），用于读插件元数据时解析 `BaseUnityPlugin` 等引用，**不是**运行时 Chainloader 对 `[BepInDependency]` 的匹配。

消费方发现只需要自身 DLL 里的 `BepInPlugin` / `BepInDependency` 特性和「是否 `BaseUnityPlugin` 子类」。`BaseUnityPlugin` 在 `BepInEx.dll`，不在 BUE 文件名上。因此 **改 BUE 文件名不会让消费方在发现阶段被跳过**。

### 3.4 本问钉死与仍待验

钉死：绑定键 = `AssemblyName` 元数据；`LoadFile` 用路径，改文件名不改身份；Chainloader 依赖检测不读文件名。

待验（Unity Mono 细节，见文末）：LoadFile 的「neither」上下文在桌面 CLR 上有「已加载仍可能按身份再探一份」的经典问题。本环境有 LMN↔LIT 成功先例，**改名**是否引入第二次探测失败，应用「只改文件名、不改 AssemblyName」的实机对照确认（预期：成功）。

---

## 4. 文件名的真实敏感面；用户「改名即挂起」能否在 5.4.23.5 复现？

**结论：在 5.4.23.5 下，仅把 `BetterUnturnedExperience.dll` 改名为 `BetterUNExperience.dll`（仍放在 `BepInEx/plugins/`，不改 GUID、不改 AssemblyName、不留第二份同 GUID）——Chainloader 不应报 missing dependency，也不应把硬依赖方挂起。用户报告按字面无法从本版源码复现。**

### 4.1 候选逐条

| # | 候选 | 判定 | 依据 |
|---|---|---|---|
| ① | 开发者 csproj `HintPath` | **只影响编译**，不影响运行 | `NoOpFixture.csproj` L22 `ProjectReference` + `Private=False`；LIT `HintPath=..\Libs\LaunchMultiplayerNet.dll`（`LaunchInventoryTidy.csproj` L67-68）。HintPath 是 MSBuild 找引用文件的路径，不会写进运行时依赖图。 |
| ② | BepInEx GUID 解析 | **不受文件名影响** | §1 全链。NoOpFixture：`[BepInDependency("io.github.yu80rice.betterunturnedexperience", HardDependency)]`（`NoOpFeaturePlugin.cs` L8），与 BUE `[BepInPlugin]` L12 同一 GUID。 |
| ③ | 同 AssemblyName / 同 GUID 双文件 | **有真实风险，但是去重跳过，不是「找不到前置」** | 同 GUID：§1.4 只留一份，另一份 `Skipping ... newer version exists`。同身份不同路径：`LoadFile` 按设计允许装两份（MSDN LoadFile remarks）；但 Chainloader 对第二份同 GUID **根本不 `LoadFile`**，所以正常双装路径是「跳过第二份」，不是双实例。 |
| ④ | 其它会坏的面 | 见下 | |

真正对文件名敏感、但**不是**「前置 GUID 检测」的面：

- **编译期**：HintPath 仍指向旧文件名时，开发者本机编译失败（与玩家运行时无关）。
- **Cecil 探测** `Name + ".dll"`（§3.3）：只在解析引用程序集文件时。消费方发现不依赖 BUE 文件名。
- **人为约定 / 安装器 / 文档**若写死 `BetterUnturnedExperience.dll` 路径。
- **改的是 `AssemblyName` 而不是文件名**：消费方 IL 仍引用 `BetterUnturnedExperience` → 运行时 `FileNotFoundException` / `AddComponent` 失败 → 该插件进 `hashSet` → 其硬依赖方才会被挂起。这是程序集名问题，用户口头却常说成「改文件名」。
- **改完文件名但旧文件还在**：两份同 GUID，一份被跳过（日志 Warning，不是 missing dependency）。

### 4.2 「检测不到前置被挂起」在 5.4.23.5 的真实触发条件

挂起文案只有 GUID 缺失或前置加载失败（Chainloader L381 / L388）。文件名不在字符串里。要复现该文案，必须让 `io.github.yu80rice.betterunturnedexperience` 没有成功进入 `pluginsByGUID` 并完成 L401，例如：

- BUE 不在 `plugins/` 树下（或扩展名不是 `.dll`）
- Cecil 读失败（坏镜像）
- `[BepInPlugin]` GUID 被改 / 非法
- 进程过滤器排除
- BUE `AddComponent` 抛错（此时依赖方看到的是 L381「dependency that was not loaded」）

单纯改文件名不在此列。

### 4.3 用户报告里可能混入的三件真实事实

1. **编译期 HintPath / `Reference Include` 文件名**  
   开发时引用 `BetterUnturnedExperience.dll`；有人把「工程里要这个文件名才能编过」推广成「BepInEx 运行时也按这个文件名找前置」。运行时找的是 GUID（依赖）和 AssemblyName（IL）。

2. **「BepInEx 按文件名序加载」民俗**（手册 L32、DEV-V2-07 §7.3、DEV-V2-11 L31）  
   源码与实机均为 GUID 拓扑。若有人靠改名来「插队」或「躲过检测」，在 5.4.23.5 上既改不了 Awake 序，也躲不过 GUID 匹配。民俗本身是真的社区/内部误传，但机制是错的。

3. **双装 / 改了 AssemblyName / 没删旧 DLL**  
   同 GUID 双文件 → 跳过一份（Warning）。若同时改了 csproj `AssemblyName` 再分发，消费方 IL 绑定失败，看起来像「前置在但类型找不到」，Chainloader 会 `Error loading [消费方]`，其再下游才会 missing dependency。LMN README 已警告「升级前删除旧 LMN、改名 LMN 和重复 LMN DLL」（`LaunchMultiplayerNet/README.md` L44）——说明生态里「改名+双文件」是真实运维事故，容易被说成「改名后前置检测失败」。

---

## 5. Forge-like 承诺可行性与防双装

### 5.1 承诺

**可以向第三方承诺：**

> 运行时用 `[BepInDependency("io.github.yu80rice.betterunturnedexperience", HardDependency)]` 引用 BUE，**不依赖 BUE DLL 的文件名**。只要该 GUID 的插件在 `BepInEx/plugins/` 树内被发现且实例化成功，硬依赖方就会在 BUE `Awake` 之后加载。

这与 Minecraft Forge 用 modid（而非 jar 文件名）解析前置是同一层契约。LMN 生态已经按此模式生产运行：三插件均 `[BepInDependency(LaunchMultiplayerNetPlugin.Guid, HardDependency)]`，编译期 `HintPath` 指向 `LaunchMultiplayerNet.dll`，运行期只认 GUID `com.yu80rice.launchmultiplayernet`（LIT/LIR/LHT 源码；LMN README L50-52；实机 DEV-V2-08 日志）。

本仓库样本：`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs` L7-8。

**承诺边界（必须写进 SDK，否则超售）：**

1. **不要改 `AssemblyName`（保持 `BetterUnturnedExperience`）**。改文件名可以；改简单名会让已编译消费方的 IL 绑定失败（§3）。
2. **GUID 稳定**为 `io.github.yu80rice.betterunturnedexperience`。
3. **消费方不要把 BUE DLL 打进自己的发布目录**（或 `Private=True` 复制）。那是双装，不是改名问题。
4. 编译期 HintPath 指向哪份磁盘文件都可以，与运行时文件名无关。

### 5.2 防双装：同 GUID 两份 BUE 时 5.4.23.5 的行为

不是「报错退出」，也不是「两个实例都 Awake」。

行为（Chainloader L308-327 + L394-404）：

1. 两份都被 Cecil 发现（两个 `Location`）。
2. `group by GUID` 后按 `Version` 降序。
3. 第一份进入 `pluginsByGUID` / `dependencyDict`；其余 `LogWarning("Skipping [X] because a newer version exists (Y)")`，**不** `LoadFile`、**不** `AddComponent`。
4. 随后只对留下的那份 `LoadFile` 一次。`PluginInfos` 该 GUID 一条。
5. 版本相同：稳定排序下「发现列表中先出现者」留下；Warning 文案仍说 newer，属文案不精确。
6. 不会因为双装而把硬依赖方判 missing——GUID 仍在。

因此「防双装」在 BepInEx 层已有 **跳过第二份、保留一份** 的事实，但：

- **不保证留下的是玩家以为的那份**（版本相同则序未定义）；
- **不会**在面板里标「检测到双装」——只有 BepInEx 日志 Warning；
- 若两份 **GUID 不同**（错误分叉）则会当成两个插件，可能 `LoadFile` 两次 → 同 `AssemblyName` 双上下文，这才是 MSDN LoadFile 所说的危险面。

BUE 若要做产品级防双装，应在自身 `Awake` 查 `PluginInfos` / 扫描同身份第二路径，或在文档强制「只放一份、删旧改名副本」（LMN README 已有同类句）。BepInEx 默认行为足够防止「同 GUID 双 Awake」，不足以当作用户可见的双装诊断。

---

## 辅助主源摘要

- **NoOpFixture**：GUID 依赖 + 编译期 ProjectReference，`Private=False`（不把 BUE 复制到输出）。运行时入口 `BueRuntimeHost.Register`（`NoOpFeaturePlugin.cs`）。
- **LMN 第三方**：`[BepInDependency(LaunchMultiplayerNetPlugin.Guid, Hard)]` + HintPath `LaunchMultiplayerNet.dll`；README 明确 GUID 硬依赖；并警告删除重复/改名副本。
- **BepInEx 官方文档**：`https://docs.bepinex.dev/articles/index.html` 教程索引含 plugin_tutorial 1–4；本会话未能稳定抓取 GUID 专节。机制以 DLL 为准。API 页 `docs.bepinex.dev/api/BepInEx.BepInDependency.html` 本会话 404。

---

## ① 五问逐条结论

1. **前置解析按 GUID**（`PluginInfos`/`pluginsByGUID`/`BepInDependency.DependencyGUID` + 拓扑 + 硬依赖检查）。文件名只出现在 `Location`→`LoadFile`。既往行号 L45/L306/L330-343/L363-391/L395-404 复核通过。
2. **Awake 序 = GUID 拓扑（独立节点走 GUID 字典序）**，不是文件名序。发现阶段 `GetFiles` 序不保证且不做 CLR Load。`Loading [...]` 就是实例化。实机六插件序与 GUID 拓扑同构，与文件名序相反。「B < L」不成立。
3. **运行时类型绑定按 AssemblyName 元数据**，不是文件名。`LoadFile(路径)` 装的是改名后的文件，身份仍是 `BetterUnturnedExperience`。MSDN 明确身份不含文件名。
4. **仅改部署文件名，5.4.23.5 不会出现「检测不到前置被挂起」。** 敏感面是 HintPath（编译）、AssemblyName（IL）、同 GUID 双文件（跳过一份）。用户报告更像把这三件事实混进「文件名检测」。
5. **Forge-like 承诺可行**（GUID 硬依赖，文件名非契约），前提是冻结 GUID 与 `AssemblyName`、禁止消费方附带第二份 BUE。双装：同 GUID → 跳过旧/后到者、单实例，不报 fatal、不双 Awake。

## ② 仍是推断、需实机/实验验证的点（具名）

1. **改名对照实验（高优先，Q4 的最后一公里）**  
   `plugins/` 只留一份 BUE，文件名改为 `BetterUNExperience.dll`，GUID/`AssemblyName` 不变；旁挂 NoOpFixture（HardDependency）。预期：BUE 与 Fixture 均 `Loading` 成功，无 missing dependencies。本报告为源码证明，**尚未做这次改名启动**。

2. **Unity Mono LoadFile 二次探测（Q3 边界）**  
   在改名场景下抓 Fusion/Mono 绑定日志，确认消费方引用 `BetterUnturnedExperience` 时命中已 `LoadFile` 的那一份，而不是去应用基目录找 `BetterUnturnedExperience.dll`（文件已不存在）。LMN/LIT 同名同文件先例不能 100% 代替「文件名 ≠ 简单名」对照。

3. **同版本双文件谁留下**  
   `GetFiles` 序未定义。两份相同 Version、相同 GUID、不同路径时哪份被 Skip，需在目标 NTFS 上实测；产品文档应写「不要双装」，不要依赖「先发现者」。

4. **不同 GUID、相同 AssemblyName 的双 LoadFile**  
   恶意/错误分叉。MSDN 说 LoadFile 可装两份同身份。BUE 防双装若只查 GUID 会漏这种。未在 5.4.23.5+Unity Mono 上实打。

5. **Preloader `AssemblyResolve` 是否补了 LoadFile 探测**  
   本机 `Libs/` 无 `BepInEx.Preloader.dll`；网上源码本会话拉不到。Chainloader 本身无 `AssemblyResolve`。若 Preloader 按 `Name+".dll"` 探 plugins 目录，则**改名**可能影响「未被 LoadFile、仅靠 Resolve 补载」的路径；硬依赖拓扑下 BUE 应已 LoadFile，该路径预期不触发。仍需有 Preloader DLL 时复核。

6. **独立插件 GUID 序 vs 文件名序的最小实验**  
   机制与现网日志已足够；若要给手册写「可演示」用例，用两个无依赖、文件名序与 GUID 序相反的空插件各打一条 Awake 日志即可。
