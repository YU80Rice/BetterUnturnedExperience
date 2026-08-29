# RT-08 · BepInEx 5.4.23.5 Chainloader 源码核查与 "Awake 后无 Update" 社区报告调查

- 调查日期：2026-08-29（会话内）
- 调查角色：一手资料调查员（RT-08）
- 范围：仅两个问题 —— ① Chainloader 在 "startup complete" 之后对 `BepInEx_Manager` 做了什么；② "插件 Awake 正常但 Update/Start/协程不执行" 的已知报告与解法
- 环境：Unturned 3.26.3.9（Unity 2022.3.62f3，Mono，winhttp doorstop）+ 官方 BepInEx 5.4.23.5，UMM 以 `Unturned.exe -NoBattlEye` 模组模式启动

## 证据来源与方法说明

- 本会话直连 `github.com` / `raw.githubusercontent.com` 均失败（curl 连接重置/超时，git clone 两次失败，已达重试上限，不再重试）。因此源码证据取自**本地两份独立副本**并交叉互证：
  1. `bepinex-decompile/BepInEx.Bootstrap/Chainloader.cs` —— 对**实际安装的 BepInEx 5.4.23.5 DLL** 的反编译（448 行，最贴近真机运行代码）；
  2. `.scratch_tmp_sources/bepinex-src/BepInEx/Bootstrap/Chainloader.cs` —— 上游源码风格副本（510 行，含注释），与本机反编译逐结构一致，判定为上游 5.4.23.x 对应源码。
  3. tag 存在性经 `git ls-remote` 证实：`v5.4.23` / `v5.4.23.1` … `v5.4.23.5` 均存在于 `github.com/BepInEx/BepInEx`（本机网络对 git 协议曾瞬时可达）。
- 下文行号标注格式：`[src L<行号>]` = 上游源码副本行号；`[dec L<行号>]` = 安装版反编译行号。两者内容一致处同时给出。

## 问题 1 Findings：Chainloader 在 "startup complete" 之后对 BepInEx_Manager 做了什么？

### 1.1 结论（逐条可溯源）

1. **5.4.23.5 的 Chainloader 是静态类，不是 MonoBehaviour**：`public static class Chainloader` [dec L18]。它本身不挂在任何 GameObject 上，因此经典 5.4.21- 时代 "Chainloader 组件随 Manager 对象存在" 的结构已不存在（与真机诊断"组件清单仅 Transform + BUE(self)，Chainloader 组件不在其上"吻合）。
2. **`BepInEx_Manager` 由 Chainloader 自己创建**：`ManagerObject = new GameObject("BepInEx_Manager");` [src L302 / dec L289]。
3. **创建后仅两个生命周期相关操作，全部发生在 "startup complete" 之前**：
   - 可选隐藏：当 `BepInEx.cfg [Chainloader] HideManagerGameObject=true` 时 `ManagerObject.hideFlags = HideFlags.HideAndDontSave;` [src L303-304 / dec L290-293]；
   - `UnityEngine.Object.DontDestroyOnLoad(ManagerObject);` [src L306 / dec L294]。
4. **所有插件组件统一挂在 `BepInEx_Manager` 上**：`pluginInfo.Instance = (BaseUnityPlugin)ManagerObject.AddComponent(ass.GetType(pluginInfo.TypeName));` [src L439 / dec L402]。即：`BepInEx_Manager` 一旦被 `SetActive(false)` 或 `Destroy`，**全部插件组件随之失去帧驱动或被销毁**；插件 Awake 在 AddComponent 时刻（仍处于 Chainloader.Start() 调用栈内）同步执行——这正是"Awake 全链正常"的原因。
5. **"Chainloader startup complete" 之后无任何代码**：`Logger.LogMessage("Chainloader startup complete"); _loaded = true;` 后方法直接结束 [src L468-469 / dec L433-434]。整个 Chainloader.cs 全文**不存在** `SetActive`、`Destroy`、`DestroyImmediate`、`enabled=` 等调用（全文 grep 证实，唯一相关词只有 L304 的 hideFlags 与 L306 的 DontDestroyOnLoad）。
6. **BepInEx 自身代码中没有清理者**：对 `bepinex-decompile` 全目录 grep `SetActive(|Destroy(|DestroyImmediate(` 无命中（BepInEx 核心 5 个命名空间范围内）。
7. **旁证——BepInEx 自身也有依赖帧驱动的对象**：`ThreadingHelper.Initialize()` 创建 `BepInEx_ThreadingHelper` GameObject（同受 HideManagerGameObject 配置控制 hideFlags）并 AddComponent，其 `Update()` 负责主线程委托调度 [src ThreadingHelper.cs L34-41, L53-59]。若真机上"一切 MonoBehaviour Update 不执行"，`BepInEx_ThreadingHelper.Update` 也必然停摆，`ThreadingHelper.StartAsyncInvoke`/Dispatcher 类功能将全部失效——可作为真机复验的对照探针。

### 1.2 调用链与时序（谁在什么时候调用了 Chainloader.Start）

- Preloader 将 `Chainloader.Initialize` + `Chainloader.Start` 的调用 IL **注入进"入口方法体开头"**：`PatchEntrypoint(ref AssemblyDefinition assembly)` [src Preloader.cs L144-220]，注入内容为 `Chainloader.Initialize(null, false, preloaderLogEvents)` 与 `Chainloader.Start()` [src Preloader.cs L205-219]。
- 默认注入目标（`BepInEx.cfg [Preloader.Entrypoint]`）：`Assembly=UnityEngine.CoreModule.dll`、`Type=Application`、`Method=.cctor` [src Preloader.cs L279-292]。即 **`Chainloader.Start()` 在 `UnityEngine.Application` 静态构造函数首次被触发时执行**——这是 Mono doorstop 场景下最早的托管时机之一，早于任何场景加载完成。
- 时序推断（与真机日志吻合）：doorstop 启动 → Preloader 运行（Harmony 补丁、打补丁后的 UnityEngine.dll 回写）→ Unity 引擎走到 `Application..cctor` → `Chainloader.Start()`：创建 `BepInEx_Manager`（DontDestroyOnLoad）→ 逐个 `AddComponent` 插件（Awake 依次同步触发）→ 打印 "Chainloader startup complete" → **Start() 返回，BepInEx 侧对该对象再无任何操作**。
- 另有 `BepInEx.Bootstrap.Linker.StartBepInEx()` [src Linker.cs L7-11] 直接连续调用 Initialize+Start，供非注入式入口（doorstop dotnet entry 等）使用，生命周期语义相同。

### 1.3 `HideManagerGameObject` 配置的官方语义（上游注释原文）

[src Chainloader.cs L32-33 / dec L32]：
> "If enabled, hides BepInEx Manager GameObject from Unity. **This can fix loading issues in some games that attempt to prevent BepInEx from being loaded.** Use this only if you know what this option means, as it can affect functionality of some older plugins."

上游在配置注释中明确承认：**存在会"试图阻止 BepInEx 被加载"的游戏**（即对未知/管理 GameObject 有主动处理行为的游戏）。这为"游戏侧主动禁用/销毁 `BepInEx_Manager`"的嫌疑提供了官方文档级的旁证，但 Chainloader 本身绝不是清理者。

### 1.4 问题 1 小结

针对核心疑问"**Chainloader 在 startup complete 之后是否 SetActive(false)/Destroy 了 BepInEx_Manager**"：**否。** 一手源码（安装版反编译 + 上游源码双证）显示 Start() 在打印 "startup complete" 后立即返回，文件内无任何对象生命周期操作；创建时仅做 DontDestroyOnLoad（与可选 HideAndDontSave）。因此真机观察到的 `SetActive(false)`→`Destroy`（Unity 重载 `==` 判定）**来自 BepInEx 之外的行为者**（游戏本体/官方模组模式集成层/其他插件），或来自 Unity 引擎层面的对象处置（详见"解释力评估"节）。

## 问题 2 Findings："插件 Awake 正常但 Update/Start 不被调用"的已知报告与解法

检索通道：GitHub API（api.github.com 可达；github.com 网页与 raw 域被网络阻断）+ web_search。以下全部为 GitHub 一手原文摘录（原始 JSON 已存档于 `RT-08-src/`）。

### 2.1 案例 A：BepInEx Discussion #827 —— "BaseUnityPlugin destroyed before first Frame Update!"

- 链接：<https://github.com/BepInEx/BepInEx/discussions/827>（2024-04-08，状态 open，含已选答案）
- 环境：游戏 Angel Legion（Steam），Unity 2021.3.15f1，BepInEx 5.4.22，Windows 11
- 症状（发帖者 bugerry87 原文要点）：游戏一次更新后，"all BepInEx BaseUnityPlugin Scripts get destroyed before the first frame update. In consequence, no Unity Pipeline Functions can be used such as Start, Update, OnGUI, etc. **Expect, all Harmony Patches are successfully injected and working well.**"——与真机症状同构：Harmony 程序集级 detour 存活、MonoBehaviour 帧回调全灭。发帖者明确怀疑"3rd Party component that detects BepInEx Scripts and deletes them / custom code by the game that either accidently or willingly destroys my mod scripts"。
- **解法（已选答案，BepInEx collaborator toebeann）**：在 `BepInEx.cfg` 中设置
  ```toml
  [Chainloader]
  HideManagerGameObject = true
  ```
  发帖者回复："**Works!**"
- 含义：该案例确认存在**游戏侧检测/删除 BepInEx 管理对象**的行为，且 `HideManagerGameObject=true` 是官方社区给出的对症解法（原理见问题 1 §1.3 上游注释与 §1.1 第 3 条——hideFlags 置为 HideAndDontSave 使对象从 Unity 常规枚举中隐藏）。

### 2.2 案例 B：BepInEx Issue #420 —— "BaseUnityPlugin got destroyed after awake, can't use update / onGUI"

- 链接：<https://github.com/BepInEx/BepInEx/issues/420>（2022-05，状态 closed）
- 环境：游戏 Craftopia，Unity 2022.1.0f1，BepInEx 5.4.11（+MultiFolderLoader）
- 症状：`Start()`、`Update()`、`OnGUI()` 均未被调用，排查发现插件类在 Awake 后被销毁（`OnDestroy` 有日志）。
- **维护者 ghorsington 的两步排查指引（方法论教训）**：
  1. 先改用 BepInEx 自带 `Logger`（`BaseUnityPlugin.Logger`）而非 `UnityEngine.Debug.Log` —— 排除"其实 Update 在跑、只是该游戏内 Unity 日志不可见"的假阴性；
  2. 再试 `BepInEx.cfg` 的 `HideManagerGameObject = true`。
- **维护者对机制的定性原文**（一手，贡献者 ghorsington）：
  > "The option marks the BepInEx plugin manager object as hidden from the Unity scene, **which prevents the game from deleting it**. This is useful for some games that do manual extremely aggressive scene cleanup at the early stages of the game. **Some games also use this approach as a rudimentary anti-cheat/anti-modding measure.** It's impossible to know for sure without analysing the game code."
- 结果：两位用户（Eradev、nmanhei）确认设置该选项后 Update/Start/OnGUI 恢复调用（"I can see the calls being made" / "works fine, thank you"）。
- 含义：这是**维护者背书的定性**——"游戏在早期阶段做极端激进的场景清理，部分游戏把它用作朴素的反作弊/反模组手段，删除 BepInEx 管理对象"是 BepInEx 社区已知且有先例的问题类别。

### 2.3 机制 C：PR #222 —— 游戏用 `Resources.UnloadUnusedAssets` 强力清场（Oddworld: Soulstorm）

- 链接：<https://github.com/BepInEx/BepInEx/pull/222>（状态 merged）
- 描述原文要点："To prevent forceful destruction of the manager GOs by the game (see Oddworld: Soulstorm) via `Resources.UnloadUnusedAssets` … **Oddworld: Soulstorm (for example) uses aggressive resource management approach that forcefully unloads any objects, including those that were set to DontDestroyOnLoad()** … Plugins used to be destroyed and unloaded after fir[st frame]."
- 解法：BepInEx 的 GameObjects 采用 `HideFlags.HideAndDontSave`（其中 DontUnloadUnusedAsset 语义可避开 `Resources.UnloadUnusedAssets` 的清理）。
- 含义：这是第三种已确认的"插件宿主对象被游戏间接清掉"的引擎级机制——**`Resources.UnloadUnusedAssets` 会销毁普通 `DontDestroyOnLoad` 对象**；未带 HideAndDontSave 的插件自建对象同样会被清。

### 2.4 Unity 2022.3.62f3 专属：PR #1235（已合并，即 v5.4.23.5 的首个 commit）

- 链接：<https://github.com/BepInEx/BepInEx/pull/1235>（标题原文："v5 - Fix for unity 2022.3.62f3 not having get_graphicsDeviceID"，merge commit `8e9afa93df`）
- 背景：Unity 2022.3.62f3 的 `SystemInfo` 缺失 `graphicsDeviceID` 属性（评论者 xinitrcn1："unity 2022 just has no graphicsDeviceID, i checked the DLLs (unityjit-win32) it's not there"），导致 BepInEx 5.4.23.4 及更早版本在该 Unity 版本上启动报 MissingMethodException。
- compare API 证实（v5.4.23.4...v5.4.23.5 区间共 4 个 commit）：
  1. `8e9afa93df` Fix for unity 2022.3.62f3 not having get_graphicsDeviceID (#1235)
  2. `3e8aebdb9b` Upgrade Doorstop to version 4.5.0 (#1259)
  3. `37f0a9aa5a` v5: Fix log writer errors for Unity 6 (#1264)
  4. `57f1fb859b` Bump version number
- 含义：**真机所用 5.4.23.5 恰是官方为其 Unity 版本发布的最新 v5 补丁版**；且 v5.4.23.2→v5.4.23.5 全部变更（sdk 工程迁移、Doorstop 4.4.0/4.4.1/4.5.0、#1235、Unity 6 日志修复、版本号）中**没有任何 Chainloader 生命周期行为改动**——反向坐实问题 1 的结论。

### 2.5 阴性结果（如实记录）

- **GitHub 全站**：`Unturned BepInEx in:title` 检索 0 命中（search/issues API，total_count=0）——GitHub 上没有 Unturned+BepInEx 标题级的一手 issue/discussion。
- **Unturned 官方一手文档**（本会话可直连部分）：`docs.smartlydressedgames.com`（含 u3-sdk FAQ）、`blog.smartlydressedgames.com`（含 3.25.8.0 Update Notes 与站内搜索 `?s=BepInEx`）、`eprison.de` 的 3.25.2.0 公告镜像——**均无 "BepInEx" 字样**；`support.smartlydressedgames.com` 与 `steamcommunity.com` 直连被网络阻断（各尝试 1 次后停止，符合重试预算）。因此**未能在公开一手渠道找到"Unturned 上 BepInEx 插件 Awake 后 Update 不执行"的直接报告**，也未找到 Unturned 官方关于模组模式如何对待 BepInEx_Manager 的公开说明（此项标注待查，需要能访问 Steam 社区/SDG 支持站的通道）。
- **pardeike/Harmony 仓库**：`Awake+Update` 检索仅 1 个无关命中（[Issue #376](https://github.com/pardeike/Harmony/issues/376)，MissingMethodException）。Harmony 侧无同类报告——符合预期：Harmony detour 是 IL/JIT 层改写，与 Unity MonoBehaviour 帧驱动完全无关；这也解释了真机上"程序集级 detour 正常"与"帧驱动死绝"可以并存。
- **社区背景（非本会话一手验证，仅作定向提示）**：Unturned 服务端生态的传统插件框架 RocketMod 采用静态事件/命令模型而非 MonoBehaviour 帧驱动，Unturned 插件开发者普遍缺少"宿主 GameObject 帧驱动"的使用经验——在此生态中出现"游戏侧对未知 GameObject 做清理"的兼容性问题更不易被沉淀为公开报告。

### 2.6 问题 2 小结

"Awake 正常但 Start/Update/协程全灭、Harmony detour 正常"这一症状在 BepInEx 社区**有至少两个已归档的同构案例**（#827、#420），官方社区解法一致且经发帖者/维护者确认有效：**`HideManagerGameObject = true`**（隐藏管理对象，使游戏侧清理/反模组逻辑发现不了它）。已确认的游戏侧机制有三类：① 显式检测/删除 BepInEx 管理对象（#827、#420 推断）；② 早期激进场景清理/朴素反作弊（#420 维护者定性）；③ `Resources.UnloadUnusedAssets` 强力卸载（PR #222，含"普通 DontDestroyOnLoad 对象也会被清"的重要推论）。Unturned 侧无公开一手报告（阴性结果已如实记录）。

## 对真机时序的解释力评估

真机时序：插件 Awake 全链正常（Harmony detour 自检命中）→ "Chainloader startup complete" → `BepInEx_Manager` 被 SetActive(false) 后 Destroy（Unity 重载 `==` 判定）→ 此后一切 MonoBehaviour 帧驱动（组件 Update/Start、协程恢复、独立新建 DontDestroyOnLoad 对象的 Update）不执行，但同步代码与程序集级 Harmony detour 正常。

| 时序片段 | 解释力 | 依据 |
| --- | --- | --- |
| Awake 全链正常 | ★★★★★ | 问题 1 源码：插件 Awake 在 `ManagerObject.AddComponent(...)` 时刻同步触发 [src L439]，发生在 Start() 调用栈内，对象此时尚为 active；之后才可能被外部禁用/销毁 |
| Harmony detour 自检命中 | ★★★★★ | Harmony 是 IL/JIT 层 detour，不依附任何 GameObject；与社区案例中"帧驱动全灭但 Harmony 正常"完全一致（#827 原文） |
| "startup complete" 后对象被 SetActive(false)→Destroy | BepInEx 侧 100% 排除；行为者为外部 | 问题 1 源码：complete 后无任何操作；社区先例（#827/#420）证实"游戏侧删除管理对象"是已归档问题类别。"先 SetActive(false) 再 Destroy"的两步分离更像**显式代码**所为（枚举-禁用-销毁流程），而非 `Resources.UnloadUnusedAssets`（后者直接 Destroy，无禁用步骤） |
| 组件 Update/Start、协程恢复停摆 | ★★★★ | 插件组件全部挂在 `BepInEx_Manager` 上 [src L439]：宿主被禁用→组件 OnDisable、Update/Start 停、协程挂起；宿主被销毁→组件随之销毁。一条假设即覆盖全部插件侧症状 |
| 独立新建 DontDestroyOnLoad 对象的 Update 不执行 | ★★（部分解释，存在两个待判定分叉） | 见下文分叉分析 |
| 同步代码正常 | ★★★★★ | 同步调用不经过 Unity 帧循环；与上述任何假说兼容 |

**关键分叉（决定最终定位）**：真机此前审计显示 `BepInEx_Manager` 与自建 pump 均带 `HideAndDontSave`。这产生两个分叉：

- **H1（游戏侧统一清理，含隐藏对象）**：Unturned/官方模组集成层的清理逻辑能枚举 `HideAndDontSave` 与 inactive 对象（`FindObjectsOfTypeAll` / `FindObjectsByType(FindObjectsInactive.Include)` 原生支持），对未知宿主做"禁用→销毁"。可同时解释 manager 与 pump 的死亡；与 #420 维护者"朴素反作弊/反模组手段"定性一致但更彻底。**解释力 ★★★★**。
- **H2（帧循环层干预）**：若存在"从未被禁用/销毁、依然 active 的对象其 Update 也不跑"，则超出任何对象清理假说的解释范围，指向 PlayerLoop 被改写（`PlayerLoop.SetPlayerLoop` 摘除 ScriptRunBehaviour 相关子系统）、应用级暂停（runInBackground=false + 失焦）或托管 Update 调度被 detour 干扰。**无社区一手直接证据，★★（待真机判别）**。判别方法见修复建议 §5 探针 ④。
- **H3（诊断方法学假阴性）**：#420 的教训——部分游戏内 `UnityEngine.Debug.Log` 不可见，"Update 没跑"可能只是"日志没出来"。真机判定依据为对象重载 `==` 比对与回调（非纯日志），该假说权重低（★），但复验时仍建议统一改用 `BepInEx Logger`/文件直写。

**综合判断**：真机时序与 BepInEx 社区已知问题类别（游戏侧清理管理对象）**高度吻合**，#827 与真机症状几乎同构；但"HideAndDontSave 仍被禁"与"独立对象 Update 不跑"两个细节表明 Unturned 环境的清理/干预比已知案例更彻底，或叠加了 H2 层面的干预。BepInEx 自身（含 5.4.23.5 全部已知变更）不构成任何解释成分。

## 修复方向建议（按优先级）

1. **核实并设置 `HideManagerGameObject = true`**（[Chainloader] 节）：#827（collaborator 解法，发帖者确认）与 #420（维护者指引，两用户确认）的双案例对症解法。注意两点：① 上游注释警告可能影响依赖 `Find` 寻找 Manager 的老插件；② 若真机 manager 在未开启该配置时就已带 HideAndDontSave（此前审计如此记录），先核实本机 `BepInEx.cfg` 实际值，再判断该开关在 Unturned 上是否仍有增益——若 Unturned 清理逻辑枚举隐藏对象，则此开关治不了本例，但它仍是零成本的第一步验证。
2. **放弃"可被枚举的 GameObject 承载帧驱动"**，改变挂载方式（任务提示项之三，优先推荐）：
   - **程序集级 Harmony detour 挂游戏自身每帧执行的方法**（真机已证明 detour 存活），在 detour 内驱动泵逻辑——与 manager/pump 存亡完全解耦；
   - 或 **PlayerLoop 注入**（自定义 `IPlayerLoopItem` 插入 Update 阶段）：不依赖 MonoBehaviour；若 H2 成立（有第三方改写 PlayerLoop），需在注入后校验 tick 是否恢复，并考虑"重新 SetPlayerLoop 修复循环"的对抗方案；
   - **场景加载后重建宿主**（任务提示项之一）作为兜底：在 `SceneManager.sceneLoaded` 或首帧后重建宿主对象并重挂组件；由于 Unturned 清理可能持续发生，必须做成**守卫式重建**（每帧/detour 内检测宿主存活 → 死亡即重建并计数上报），避免静默失效。
3. **升级 BepInEx：无增益，不建议**：compare 证实 5.4.23.5 是 v5 线终点（含 2022.3.62f3 专属修复 #1235；此后仅 Doorstop/日志修复），且 v5.4.23.x 全系列无 Chainloader 行为变更；BepInEx 6 为另一代架构（本环境 Mono+doorstop 5.x 结构不兼容直接迁移），不在短期动作内。
4. **官方渠道待办**（本会话网络受阻未完成）：经可访问 Steam 社区/SDG 支持站的人工或代理通道，查证 Unturned 官方对模组模式（`-NoBattlEye`）中 BepInEx_Manager 的处理说明——若官方确有清理逻辑，应能找到公告或支持文章（本报告 §2.5 已列阴性与未达渠道）。
5. **真机判别探针清单**（区分 H1/H2/H3，一次真机会话可全部完成）：
   - ① `BepInEx_ThreadingHelper` 的 `Update` 是否 tick（BepInEx 自家帧驱动组件，问题 1 §1.1 第 7 条）；
   - ② 读出本机 `BepInEx.cfg` `[Chainloader] HideManagerGameObject` 当前值；
   - ③ "死后"用 `FindObjectsOfTypeAll`（含 inactive）枚举 manager/pump：记录 `activeSelf`、`enabled`、`hideFlags`、销毁前后 `==` 比对；
   - ④ 观察游戏画面/粒子/UI 动画是否仍在动：画面正常而托管 Update 停 → H2 的 PlayerLoop 定向改写；画面也停 → 应用暂停/失焦（H2 变体）；
   - ⑤ 在已知存活的 Harmony detour 内做每帧计数（`Stopwatch`+文件直写），验证玩家循环主频是否正常——正常则证明"循环在跑、只是托管 Update 子系统/对象被摘"。

## 附录：检索方法、通道与存档

- 可用通道：web_search（harness）、api.github.com（REST/搜索/compare）、git ls-remote；被阻断通道（各尝试后按预算停止）：raw.githubusercontent.com（连接重置）、github.com:443（超时）、git clone、r.jina.ai、steamcommunity.com、support.smartlydressedgames.com（curl 层 SSL/连接失败）。
- 一手源码：安装版 5.4.23.5 反编译（`bepinex-decompile/BepInEx.Bootstrap/Chainloader.cs`）+ 上游源码副本（`.scratch_tmp_sources/bepinex-src/BepInEx/Bootstrap/Chainloader.cs`）交叉互证；tag 谱系经 `git ls-remote` 证实。
- 原始证据 JSON/HTML 存档：`RT-08-src/`（discussion-827.api.json、discussion-827-comments.json、i420*.json、pr1235*.json、pr222.json、compare-*.json、sdg-docs-*.html、sdg-blog-*.html、eprison-3252.html 等）。
- 局限：Unturned 闭源，游戏侧清理代码无一手可引；Unturned 官方公告与 Steam 社区一手内容未能获取（通道受限），相关结论已标注"待查"。
