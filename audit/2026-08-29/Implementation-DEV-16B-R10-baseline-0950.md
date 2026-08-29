# DEV-16B R10 基线固化审计

- 日期：2026-08-29 09:50（Asia/Shanghai）
- 执行者：DeepSeek Harness Agent（本轮会话）
- 基线：`master` @ `2bf61ce` + 7 个未提交修改文件（R10 方向，mtime 2026-08-28 15:12–15:38）
- 输入：`BUE-Project-Handoff-2026-08-29.md`、`audit/2026-08-28/RuntimeDiagnosis-DEV16B-142408.md`、RT-07 核验报告

## 结论

**静态基线固化 PASS；运行时未证明；发布未授权。**

R10 方向的未提交修改（插件自身 `Update()` 直驱 driver、三路注入局部隔离、Runtime tick 去重/重入/fail-closed、Runtime Catalog 消费、ConfigEntry 兼容编辑）已通过完整 Release 构建、全部 7 个测试运行器、静态门禁与 `git diff --check`。新 DLL 已落 r10 产物目录并记录 SHA-256。DEV-16B 的真实客户端按钮可见性门禁**仍未通过**——唯一缺口是人工 clean-install 部署 + UMM 诊断包证据。

## 1. 构建

- 命令：`dotnet build BetterUnturnedExperience.sln -c Release`
- 结果：**0 错误 / 0 警告**（日志：本目录 `build.log` 摘要见会话记录；构建为增量，确认 R10 源码已在产物基线内）

## 2. 测试（7/7 PASS，日志归档本目录）

| 测试运行器 | 结果 | 覆盖 |
|---|---|---|
| Contracts.Tests | PASS | DEV-10 registration runtime |
| Network.Tests | PASS | DEV-06 network codec/fence |
| Placement.Tests | PASS | DEV-04 placement evaluator |
| Settings.Tests | PASS | DEV-03 settings runtime |
| ClientUi.Tests | PASS | DEV-05/15A/15B/15C/15D/16B ClientUi |
| Release.Tests | PASS | DEV-15E qualification evidence |
| Plugin.Tests | PASS | DEV-14/DEV-16B plugin runtime（含 R10 新增：driver 转发、三路隔离、同帧去重、重入拒绝、fail-closed、panel→seam、Catalog 投影、parent 重绑、单 DLL 闭包、SDK 身份） |

单 DLL AssemblyRef/ABI 闭包检查由 `AssertSingleDllAssemblyClosure` / `AssertExternalSdkAssemblyIdentity` 在 Plugin.Tests 内覆盖并通过。

## 3. 静态门禁

- `eng/Verify-NoUiTokens.ps1`：Contracts（2 文件）/ Core（10 文件）/ ClientUi（11 文件）全部 **PASS**（日志：`noUiTokens-gate.log`）
- `git diff --check`：**CLEAN**（仅 CRLF 转换提示，无空白错误）

## 4. 新 DLL 产物（不继承旧证据）

- 目录：`artifacts/DEV-16B-management-panel-runtime-fix-r10-20260829/`
- 文件：`BetterUnturnedExperience.dll`，**168960 bytes**（r9 为 159744 bytes；+9216 bytes 为 R10 新增 driver/dispatcher/adapter 代码）
- SHA-256：`AF065D83B04D604113CB96DDB57B7A39CD98F1BEC1456DA303ECD6A1F1064CBE`
- 二进制验证：`BuePluginUpdateDriver`、`BueRuntimeTickDispatcher`、`BueButtonInjectionCoordinator`、`PresentationDegraded` 类型名均存在于 DLL 元数据
- **CaseId**：`DEV-16B-R10-20260829`（新 CandidateBuild；旧 r9 证据 `0C158856…` 仅作对照，不得引用为本轮证据）
- 部署边界不变：客户端只部署此单一主 DLL；不部署 Contracts/Core/ClientUi 独立 DLL；U3DS Headless 不得创建 UI。

## 5. 判别矩阵（真机 UMM 诊断包读数方法）

部署 r10 DLL 后，按以下顺序读 `LogOutput.log` 中的 `[BUE-UI-TRACE]` 事件，第一处非零即定位故障边界：

| 顺序 | 事件 | 非零含义 | 仍为零时的结论 |
|---|---|---|---|
| 1 | `plugin-update` | Unity 已调度插件 `Update()`，驱动边界通过 | 假设 #2 成立：宿主不调度插件 Update → 需生命周期探针 |
| 2 | `runtime-pump-tick` | 独立泵 MonoBehaviour 被调度 | 前者非零而此为零属预期（r10 已隔离泵依赖） |
| 3 | `host-ui-tick` | Harmony `MenuUI/PlayerUI.Update` postfix 实际命中 | 假设 #3 成立：Harmony 登记成功但 detour 未生效 |
| 4 | `create-button-begin` | 容器非空、注入开始 | 容器时序边界：检查 `surface-opened` 与容器重建 |
| 5 | `create-button-result` / `add-child-success` | 按钮创建/挂载成功 | 注入实现边界 |
| 6 | 截图可见按钮 | 可见性门禁通过 | 可见性/布局边界 |
| — | `RuntimeCompletionIsolated`（R10b 新增） | 运行时屏障完成路径异常并被隔离（附 errorType） | 出现即表示注册屏障失败被拦截；此时 `RuntimeReady` 不会出现，属预期 fail-closed 而非判别矩阵故障 |

判定规则：`plugin-update` > 0 且最终截图无按钮 → 按表继续向右定位；六项全零 → 假设 #1（部署身份错误）复核 `assembly-identity` SHA-256 是否等于本文第 4 节值。

## 6. 已知风险（2026-08-29 R10b 轮更新）

1. ~~`TryCompleteRuntime()` 无异常屏障~~ → **已在 R10b 修复**（`BueRuntimeCompletionBarrier`：未就绪可重试、异常永久隔离并保留 `LastFailure` 诊断；`CompleteRuntime` 成功后的 Refresh 副作用局部隔离，不再阻塞 RuntimeReady 输出）。详见第 9 节。
2. `BueRuntimeTickDispatcher` 在 `log == null`（纯测试宿主）时 frameProvider 恒为 `-1`，同帧去重语义与生产分叉；生产路径不受影响，已有测试锁定哨兵行为。
3. 面板挂载于 vanilla 页面容器（非独立 `SleekWindow`），光标/遮罩/输入焦点需真机确认（交接文档第 8.4 条）。
4. **待规格裁决项（双轴审查发现，未在本轮修改，已记入工单 Comments）**：枚举（AcceptableValueList）条目只读 vs 规格要求的校验编辑路径；第三方条目显示名/版本/运行状态模型缺口（`FeatureState` 恒 Running）；面板打开时整页遮蔽宿主页面的规格授权；`IsAlive` 反射失败返回 false 的 fail 方向。

## 7. 唯一人工步骤（HITL，2026-08-29 更新为 r10b 产物）

1. 备份并替换 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll` 为 **r10b 产物**（部署前删除旧文件，避免残留）。
2. 启动 Unturned → 进入主菜单（Workshop 页与 Dashboard 页各停留数秒）→ 进入游戏 → 打开暂停菜单。
3. 导出 UMM 诊断包，并将包内 `LogOutput.log`（含 `assembly-identity` 行）与截图交回。
4. 预期比对：`assembly-identity` SHA-256 必须等于 `D8F9AF516AC530EDDA93AE61DDBE168375C2F6B96027751C1CB7F6FCEA4212D5`（r10b），否则为部署身份错误。

## 8. 三态声明

- **静态已证明**：R10b 代码编译、7/7 测试、门禁、单 DLL 闭包、diff 卫生、双轴子代理审查（Standards/Spec 独立并行）通过。
- **运行时未证明**：按钮可见性、面板交互、Harmony 命中、容器时序。
- **发布未授权**：DEV-16B 不得标记 resolved/Stable/三环境通过，直至新证据包与资格门禁完整通过。

## 9. R10b 修复记录（2026-08-29，双轴审查驱动的 TDD 轮）

**范围**：仅修复审查发现的阻断项，其余记入 §6.4 待规格裁决。

| 发现来源 | 问题 | 修复 |
|---|---|---|
| Standards（硬违规） | R10b 初版屏障 catch 裸吞异常，违反「核心安全降级保留最小诊断」 | 屏障保存 `LastFailure`；隔离日志含 `errorType=` |
| Standards（smell） | R10b 初版的 `completionIsolatedLogged` 防重字段与屏障幂等重复 | 首次隔离回调 `onFirstFailure` 内聚进屏障，初版字段同轮删除 |
| Spec-② | 隔离诊断缺结构化字段 | **新增** `RuntimeCompletionIsolated` 事件，含 `decision=Isolate`（规格 §3 四元组：FeatureId/DiagnosticId/Decision/Status；该事件 R10b 轮新增，非基线已有） |
| Spec-③ | `CompleteRuntime` 成功后 Refresh 抛异常 → RuntimeReady 永不输出、退订被跳过 | `TryRefreshAfterCompletion()` 副作用局部隔离（`BUE-CLIENTUI-004`），RuntimeReady 事实输出不再被 UI 刷新失败阻塞 |

TDD 闭环：红（CS0246 类型缺失）→ 绿（`AssertRuntimeCompletionBarrierIsolates` **16 断言**，含 LastFailure 保留、回调仅一次、未就绪可重试、完成幂等）。

**第三轮复审记录（循环审查第二轮）**：Standards 轴 `CLEAN`（前轮硬违规消除确认 + 3 项可推迟 smell：完成路径退订纳入屏障的失败语义偏宽、单参构造轻冗余、BUE-CLIENTUI-004 日志格式与主隔离日志不一致）；Spec 轴 3 项记录/证据偏差 → 已修正（断言计数 13→16、溯源表述更正为「R10b 新增事件」、R10b 轮测试证据以独立 `tests-r10b-*.log` ×7 + 测试 exe SHA-256（`r10b-tests-exe-sha256.txt`，`A4F65BCC…EABB`）留存）。

**第四轮终审记录（循环闭合，2026-08-29）**：双轴独立终审均 `CLEAN`。Spec 侧（5/5 闭合）：16 断言计数与源码实证一致；`git show 1747f98` 取证确认基线无 `RuntimeCompletionIsolated`，溯源表述成立；§5 矩阵行已补；`tests-r10b-*.log` ×7 全 PASS + exe SHA 锚定 + mtime 弱证据；工单证据引用与循环链完整。Standards 侧（4/4 核验）：§9 屏障语义与 CONTEXT.md「核心安全降级保留最小诊断」逐字吻合、未夸大为核级停机；3 项可推迟 smell 按循环规则第 3 条显式记录；矩阵行事件名与代码（Plugin.cs:163/168/189）实证一致；循环链无自相矛盾。**循环终止：双轴无阻断发现，`e3d1986` 及本审计记录为正式输出。**

**新产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r10b-20260829/BetterUnturnedExperience.dll`，169984 bytes，SHA-256 `D8F9AF516AC530EDDA93AE61DDBE168375C2F6B96027751C1CB7F6FCEA4212D5`，**CaseId `DEV-16B-R10B-20260829`**（r10 的 `AF065D83…` 从未被部署，保留作对照）。验证：构建 0/0、7/7 测试 PASS（`tests-r10b-*.log`）、NoUiTokens 三 SourceRoot PASS、`git diff --check` CLEAN。

## 10. R11 门禁修复轮（2026-08-29 真机诊断驱动，循环审查闭合）

### 真机诊断（`UMM-诊断包_20260829_194254`）

部署 r10b（`assembly-identity` SHA-256 实测等于 r10b 产物 ✓）后：`runtime-gate decision=Client` → **`BUE-CLIENTUI-001`（composition unavailable）** → BootstrapReady。面板整链未构造（无 constructed / patch-installed / pump / 注入事件），按钮不存在。故障边界：**门禁层**（早于 R9 的驱动层问题）。

### 根因（证据链闭合）

R10 新增的 `CanBindNativeUi()` 把 `Glazier.Get() != null` 混入门禁。`Glazier.Get()` 返回静态 `instance` 字段（U3-SDK `SDG.Glazier/Glazier.cs`——主菜单构建前恒 null），Awake（Chainloader 阶段）调用时**恒 false** → 面板整链被时机性误杀。对照：R9 基线（`2bf61ce`）该参数硬编码 `true`，面板可构造。测试宿主探针（tagged `[DEBUG-cbnu]`，已清理）实证 8 个成员存在性条件全 True、方法仍 False——与真机行为一致，离线可复现。

### 修复（TDD 红→绿）

- 红：新增 `AssertNativeUiGateReflectsMemberPresence`——纯宿主（Glazier.instance=null）下 `CanBindNativeUi()` 必须为 true；当前代码 FAIL（`native ui gate stays true on vanilla member presence while Glazier is not yet initialized`）。
- 绿：`CanBindNativeUi()` 移除 `Glazier.Get() != null` 条件，语义收敛为**纯成员存在性**；引擎就绪性由注入路径既有防御接管（vanilla container 非空 ⇒ 菜单已建 ⇒ Glazier 就绪，双轴审查特别核验确认）。
- 全套验证：构建 0/0、7/7 测试 PASS（`tests-r11-*.log`）、`git diff --check` CLEAN。

### 循环审查记录

- 第 1 轮（增量双轴并行）：Standards **无硬违规** + 2 项判断性可推迟；Spec **CLEAN**（忠实根因、Headless 隔离由 `ClientUiEnvironment.CanCompose` 独立承担不削弱、预期日志链与判别矩阵一致、测试语义正确）。
- **循环终止：双轴无阻断发现。** 可推迟项显式记录：① 测试未显式断言 `Glazier.Get()==null` 前提（锁定力依赖纯宿主事实）；② 门禁注释 "null guards" 措辞偏宽（实为容器前置检查 + try/catch 隔离）；③ 无「成员缺失→false」负向测试。

### 新产物（正式输出）

`artifacts/DEV-16B-management-panel-runtime-fix-r11-20260829/BetterUnturnedExperience.dll`，169984 bytes，SHA-256 `EA77D360E0C2BCB2FBF8F8DE4696A6B40C49E69934A3D8A40121B33E838D000A`，**CaseId `DEV-16B-R11-20260829`**（r10b 的 `D8F9AF51…` 已被真机否证为门禁误杀，保留作对照）。

### 下一次真机判读变更

预期日志链：`assembly-identity`（=r11 哈希）→ `decision=Client` → **`BUE-CLIENTUI-002`（composition ready）** → `BootstrapReady` → `REG-ACCEPT` → `plugin-update` → `create-button-*`。门禁层已排除；若 `plugin-update` 仍为 0，则回到 R9 的驱动层假设（宿主是否调度插件 Update）。

## 11. R12 诊断探针轮（2026-08-29，驱动层三分判别）

### R11 真机判读（`UMM-诊断包_20260829_201509`）

部署 r11（`assembly-identity` = `EA77D360…` ✓）：**门禁层通过**（`constructed` + `patch-installed ×11` + `BUE-CLIENTUI-002`），但**驱动层全零**——`start-entered`/`plugin-update`/`runtime-pump-tick`/`host-ui-tick`/`constructor-postfix`/`surface-opened`/`create-button-begin` 全部 0。游戏确实进入主菜单+PEI（`Client.log` 会话 20:11-20:14），`MenuUI.Update` 每帧在跑、UI 构造器必然触发——同步代码链 100% 工作，一切 Unity 消息泵与 Harmony detour 依赖路径全部静默。R9 原始谜团以更强证据回归。

### 对照实验（测试宿主 vs 真机）

宿主探针（`AssertSelfPatchProbeHitsOnThisHost`）：`bodyCalls=2 postfixHit=True`——同一套 detour 代码在宿主**完全正常**。⇒ 假设空间收窄至真机特有因素：Harmony 机制在 BepInEx 环境整体失效，或游戏类型 patch 特异失效（注意：诊断包 `1 plugin to load`——**UPM 从未在本机验证**，"UPM 正常工作"仅为文档假设）。

### R12 探针集（tagged `[DEBUG-drv]`，诊断完成后清理）

| 探针 | 事件 | 判别 |
|---|---|---|
| self-patch（patch 自家 `DrvProbeTarget` + 直调/反射调用） | `self-patch-installed` / `self-patch-probe-done bodyCalls= postfixHit=` / `self-patch-hit` | 命中 ⇒ Harmony 机制正常、问题在游戏类型 patch；不命中 ⇒ BepInEx 环境的 Harmony detour 整体失效 |
| 宿主对象状态 | `awake-object-state activeInHierarchy= activeSelf= enabled= scene=` | Awake 时宿主对象活性基线 |
| 生命周期 | `on-enabled` / `on-disabled`（LogInfo） | 宿主对象死亡/禁用判别 |
| 协程泵（仅 Client 分支启动） | `coroutine-tick count=` | 协程跑而 `plugin-update` 不跑 ⇒ 消息派发特定失效；两者皆零 ⇒ 宿主对象问题 |

### 循环审查记录

第 1 轮（增量双轴并行）：Standards 1 硬违规（测试侧探针未带标记，单前缀清理不完备）+ 3 判断性；Spec 4 可推迟级（① 矩阵未补探针行 ② 协程在 Headless 可达 ③ OnDisable 警告级误报 ④ 生命周期探针 seam gap）→ 修复 4 项（标记补全；协程移入 Client 分支；降 LogInfo；本节记录）→ 复审 **CLEAN**（1=闭合标记全覆盖 2=闭合 Client 分支独占 3=闭合 LogInfo 4=记录类已写入本节）。构建 0/0、Plugin/ClientUi 测试 PASS。

**Seam gap 记录**（output-review-loop 第 1 步义务）：生命周期探针（OnEnable/OnDisable/协程）依赖 Unity 引擎回调，纯 C# 宿主不可自校验；其有效性由真机读数与 `awake-object-state` 基线交叉印证。

### 探针产物（正式输出）

`artifacts/DEV-16B-management-panel-runtime-probe-r12-20260829/BetterUnturnedExperience.dll`，CaseId `DEV-16B-R12-20260829`（SHA-256 见 `audit/2026-08-29/r12-dll-sha256.txt`）。**探针轮产物仅用于诊断，不代表修复进度。**

### R12 真机读数顺序

`[DEBUG-drv]` 流（裸 Logger）：`awake-object-state` → `on-enabled` → `coroutine-tick count=1`；`[BUE-UI-TRACE]` 流：`self-patch-installed` → `self-patch-probe-done bodyCalls=2 postfixHit=?` → 判别关键读数。若 `postfixHit=False` ⇒ BepInEx/Harmony 环境级失效（方向：Harmony 版本兼容性）；若 `True` 而 `host-ui-tick`=0 ⇒ 游戏类型 patch 特异失效（方向：类型身份/预编译 detour）；`coroutine-tick` 增长而 `plugin-update`=0 ⇒ 消息派发特定失效。

## 12. R13 探针轮（2026-08-29，R12 读数驱动的归因/对抗/判别）

### R12 真机读数（`UMM-诊断包_20260829_204130`）

部署 r12 探针（`assembly-identity` = `773A4FDF…` ✓）：**三分判别全部命中**——
1. **Harmony detour 机制在真机正常**：`self-patch-hit` + `self-patch-probe-done bodyCalls=2 postfixHit=True`（第 29-31 行）。
2. **宿主对象 Awake 时完全活跃**：`awake-object-state activeInHierarchy=True activeSelf=True enabled=True scene=DontDestroyOnLoad`（第 39 行）。
3. **`on-disabled` 在 "Chainloader startup complete" 之后立即发生**（第 43→44 行），随后 `coroutine-tick count=2` 永不再现、`plugin-update`/`start-entered`/`host-ui-tick`/`constructor-postfix` 全零——**宿主被禁用，一切 Unity 消息泵停摆**；而 Harmony patch 是静态 postfix，不受组件禁用影响，游戏类型 patch 仍零命中 ⇒ 「游戏程序集方法 patch 特异失效」独立成立。

### R13 探针集（在 R12 基础上，tagged `[DEBUG-drv]`）

| 探针 | 事件/读数 | 判别 |
|---|---|---|
| OnDisable 归因 | `on-disabled activeSelf= activeInHierarchy= enabled= name= parent= components=` + `on-disabled-stack trace=`（Environment.StackTrace 单行化） | 区分「组件 enabled=false」vs「GameObject SetActive(false)」，栈直接抓禁用调用方 |
| 自唤醒对抗 | `self-revive componentReEnabled= objectActiveSelf=` | 禁用后立即恢复，观察是否被反复禁用；若成功，面板链可能直接复活 |
| UnitySynchronizationContext 泵 | `context-pump-started` / `context-tick count= enabled= activeInHierarchy=`（3600 tick 上限护栏） | 独立于组件生命周期的主线程通道：持续跳动即证实禁用且拥有恢复通道 |
| 游戏 detour 判别 | `game-detour-check manualInvoked=true hostUiHitsBefore= hostUiHitsAfter=`（恢复后反射 `MenuUI.instance` 手动 Invoke 一次被 patch 的 `Update`） | After>Before ⇒ 游戏方法 detour 有效（不命中归因调用路径）；After=Before ⇒ 游戏方法 patch 无效（类型身份/detour 对游戏程序集失效） |

### 循环审查记录

第 1 轮（增量双轴并行）：Standards **CLEAN**（0 硬违规 + 3 判断性）；Spec 4 项（StackTrace 归因缺失、pump 无退出护栏、测试名夸大、读数分支未入审计）→ 修复 3 项 + 记录 1 项 → 复审双轴 **CLEAN**（1=StackTrace 单行化先于自复活 2=tick-limit 后不再 Post 3=断言语义一致）。构建 0/0、7/7 测试 PASS（`tests-r13-*.log`）、`git diff --check` CLEAN。

**可推迟项（显式记录）**：① OnDisable 内自 re-enable/SetActive 副作用偏激进（探针设计内、try/catch 隔离、已标记，**r13 产物不得驻留真机**）；② `HostUiTickHits` 前缀脱离 Drv* 家族；③ 手动 Invoke MenuUI.Update 每帧幂等但 tickInput 可能重复处理一次输入（单次执行已由 `drvGameDetourChecked` 保证）。**Seam gap**：context pump 与自唤醒依赖 Unity 主线程/引擎语义，纯宿主不可自校验（计数器逻辑已由 `AssertHostUiTickCounterAdvances` 锁定）。

### 探针产物

`artifacts/DEV-16B-management-panel-runtime-probe-r13-20260829/BetterUnturnedExperience.dll`，175616 bytes，SHA-256 `54783075E0B024624D6A7CF9D2790AA33E918B83FE368B7EBB752D529AEAEE4E`，CaseId `DEV-16B-R13-20260829`。**探针轮产物仅用于诊断。** 一手资料调查（BepInEx Chainloader 行为）另见 RT-08。

## 13. R14 探针+修复轮（2026-08-29，HideAndDontSave 统一假设）

### R13 真机读数（`UMM-诊断包_20260829_210341`）——归因命中

- `on-disabled` 归因（第 45 行）：宿主 = **`BepInEx_Manager`**，被**整对象 SetActive(false)**（`activeSelf=False`、组件 `enabled=True`），父链无，组件清单仅 `Transform + BUE(self)`——**Chainloader 组件不在其上**（BepInEx 5.4.23 的 Chainloader 是 static 类，反编译 `bepinex-decompile/BepInEx.Bootstrap/Chainloader.cs` L289-294：`ManagerObject` 创建后设 `HideFlags.HideAndDontSave` + DontDestroyOnLoad；L433 complete 后**无任何清理动作**）。
- `on-disabled-stack`：Unity native 调用，托管栈不可得——禁用者非托管代码可见路径。
- `self-revive` 成功（SetActive(true) 打回）但 `context-tick` 从未执行——回调断链，怀疑对象随后被销毁或禁用源持续存在。
- **统一嫌疑成立性**：`BepInEx_Manager`（HideAndDontSave）被禁 + `BUE.RuntimePump`（同 HideAndDontSave，`BueRuntimePump.cs` 旧代码 L196）从未 tick + self-patch（程序集级）正常 ⇒ **游戏启动序列清理/禁用 HideAndDontSave 对象**（反外挂常用手段；SDK 反编译版本旧无此逻辑）。

### R14 变更（探针 + 修复性实验）

1. **修复性实验**：`BueRuntimePumpBehaviour.Attach` 的 `HideAndDontSave` → `HideInHierarchy`（保留 DontDestroyOnLoad）——若 HideAndDontSave 假设成立，pump 的 `runtime-pump-tick` 应在 r14 恢复。
2. **独立探针宿主**：`DEBUG.ProbeHost`（**无 hideFlags** + DontDestroyOnLoad + `DrvProbeHostBehaviour.Update`）——`drv-host-tick` 增长 ⇒ 普通对象被正常驱动、HideAndDontSave 假设证实；为零 ⇒ 假设证伪、扩大排查。
3. **manager 销毁判别**：`DrvManagerStateCheck`（probe host 定期调）用 Unity 重载 `==` 判 `Destroy`（`manager-destroyed` 事件），并持续 `manager-state`/自唤醒。
4. **context pump 健壮化**：`this==null` 防御、全 try/catch、finally 续链三分支（chain-ended / repost / broken）、3600 tick 上限——消除断链静默与无限刷屏。

### 循环审查记录

第 1 轮（增量双轴并行）：Standards 0 硬违规 + 3 判断性；Spec 判别充分性/Headless 隔离/`==` 语义全部成立 + 1 项偏差（destroyed 分支不计数 → 泵无限续链）→ 修复 4 项（销毁分支置 reason 终止链；probe host 3600 上限；public→internal；context-mismatch 打 broken 不静默）→ 复审双轴 **CLEAN**（4/4 闭合）。构建 0/0、7/7 测试 PASS（`tests-r14-*.log`）、`git diff --check` CLEAN。

**可推迟项**：① HideInHierarchy 使 pump 对 `FindObjectsOfType` 可见性增大（判断级）；② 周期性自唤醒掩盖禁用事实（判别必需，诊断后清理）；③ 归因（谁是禁用/销毁者）无托管栈手段，仅能答存在性——**依赖 RT-08 一手调查**。

### 探针产物

`artifacts/DEV-16B-management-panel-runtime-probe-r14-20260829/BetterUnturnedExperience.dll`，177664 bytes，SHA-256 `A3B144281EE82B6A5ECA35428B3F0308F939FD1DDB12D88A43976CC21447C4E1`，CaseId `DEV-16B-R14-20260829`。**探针轮产物仅用于诊断，不得驻留真机。**

### R14 真机判读分支

| 读数 | 结论 → 方向 |
|---|---|
| `drv-host-tick` 持续增长 + `runtime-pump-tick` 恢复 | **HideAndDontSave 假设证实**：驱动链修复即「pump 脱离 HideAndDontSave」+ 评估插件宿主迁移；面板按钮应出现 |
| `drv-host-tick` 增长 + `runtime-pump-tick` 仍 0 | 独立宿主正常但 pump 仍死 → pump 特异问题（创建时机/父对象） |
| `drv-host-tick` 恒 0 | HideAndDontSave 假设证伪 → 排查场景/引擎级消息派发，依赖 RT-08 |
| `manager-destroyed` | 存在销毁者（非单纯禁用）→ 定位销毁时机与 Client.log 时序互证 |
| `self-patch-probe-done` 后无 `drv-host-tick` 且无 `manager-*` | probe host 创建即死 → 引擎级异常，收窄至 Unity 2022.3.62 + BepInEx 组合 |

## 14. R15 生存策略探针轮（2026-08-29，场景加载后重建）

### R14 真机读数（`UMM-诊断包_20260829_215924`）——HideAndDontSave 假设证伪

- `manager-destroyed`（`context-pump-stopped reason=manager-destroyed ticks=0` + `context-pump-chain-ended`）：`BepInEx_Manager` 在 Chainloader complete 后被 **Destroy**（下一帧回调以 Unity 重载 `==` 判定）。
- **`drv-host-tick` = 0**：无 hideFlags 的独立对象 `DEBUG.ProbeHost`（DontDestroyOnLoad）的 Update 也从未执行 ⇒ **HideAndDontSave 假设证伪**；统一解释收敛为：**游戏清理引导阶段（第一场景加载前）创建的一切对象**（SetActive(false)+Destroy），包括 manager、pump、probe host。
- `self-patch` 仍命中（程序集级 detour 正常）；游戏类型 patch 零命中为独立未解问题。
- 唯一存活的托管入口：`SceneManager.sceneLoaded` 静态订阅（RuntimeReady 未达成、未退订，BUE 组件销毁不影响 static 事件）。

### R15 变更（生存策略验证）

1. `OnSceneLoaded` 增强：`scene-loaded scene= mode= managerAlive=` 归因日志 + 调用 `DrvRebuildProbeHost()`。
2. `DrvRebuildProbeHost`：计数（`DrvSceneRebuildCount`）+ `rebuilt` 单次守卫 + **Unity 调用隔离**到 `DrvCreateProbeHostObject`（纯宿主 Mono 对含 ECall 的方法体在 JIT 边界抛 `SecurityException`，隔离到独立方法后异常在 try 内调用点抛出、可捕获——**新 seam gap**）+ try/catch；重建对象 `DEBUG.ProbeHostR15`（DontDestroyOnLoad，无 hideFlags）。
3. 测试 `AssertSceneLoadedRebuildCounterAdvances`（纯宿主锁定计数语义）。

### 循环审查记录

第 1 轮（增量双轴并行）：Standards **CLEAN**（0 硬违规 + 3 判断性：子方法标记行、首败永久放弃、删除边界注释）；Spec 1 项（R15 seam gap 未按规则第 1 步入审计）→ 本节即补记。**循环闭合：双轴无阻断发现。** 构建 0/0、7/7 测试 PASS（`tests-r15-*.log`）、`git diff --check` CLEAN。

**Seam gap 记录（output-review-loop 第 1 步义务）**：
1. `DrvCreateProbeHostObject` 的 Unity ECall 在纯宿主 JIT 边界抛 `SecurityException`——计数语义已由测试锁定，GameObject 创建/DontDestroyOnLoad/AddComponent/Update 驱动的 Unity 行为**不可宿主自校验**。
2. 场景回调链（`scene-loaded` → 重建 → `drv-host-tick`）整体依赖 Unity 场景系统，有效性只能由真机读数判定。

**R15 读数分支**：
| 读数 | 结论 → 方向 |
|---|---|
| `scene-loaded` 出现 + `probe-host-rebuilt` + `drv-host-tick` 增长 | 生存策略成立：驱动链迁移到「场景加载后重建宿主」即为 R16 修复形态 |
| `scene-loaded` 出现 + `probe-host-rebuilt` + `drv-host-tick` = 0 | 场景后对象仍不被驱动 → 引擎级派发失效（RT-08 扩查 + UPM 对照实验） |
| `scene-loaded` 未出现 | static 事件链路也失效 → 托管世界与游戏主循环全面脱钩，环境级问题 |

### 探针产物

`artifacts/DEV-16B-management-panel-runtime-probe-r15-20260829/BetterUnturnedExperience.dll`，CaseId `DEV-16B-R15-20260829`（SHA-256 见 `audit/2026-08-29/r15-dll-sha256.txt`）。**探针轮产物仅用于诊断，不得驻留真机。**

## 15. R16 正式修复轮（2026-08-29，UPM 对照实验定案）

### 决定性对照实验（`UMM-诊断包_20260829_223101`，BUE + UPM 双插件并存）

- **UPM 完全正常**：Harmony 构造器 postfix 命中（创意工坊 + 暂停菜单按钮注入成功）、Update 驱动、组件存活——**环境（官方 BepInEx 5.4.23.5 × Unturned 3.26.3.9 × UMM `-NoBattlEye`）健康，HideAndDontSave 清理假设证伪**。
- **BUE 组件死亡真因**：R13 引入的 `self-revive`（OnDisable 中 `SetActive(true)`）与 Unity 禁用流程竞争，导致组件被引擎销毁（`manager-destroyed` 判定 + UPM 对照）——**诊断对抗行为本身放大并造成了组件死亡**。
- 附带确认：宿主 `SetActive(false)` 为瞬态（UPM 存活证明对象随后恢复）；UPM changelog 记载 3.26.3.8+ UI 重建销毁 uGUI 底层对象——属注入层参考，BUE 已有容器有效性检查（`IsAlive`）。

### R16 变更

1. **移除全部 `[DEBUG-drv]` 诊断仪器**（Phase 6 清理，grep 归零）：self-revive、context/coroutine pump、probe host、self-patch 对、生命周期探针、`HostUiTickHits`、三个探针测试。
2. **保留有效修复**：`CanBindNativeUi()` 门禁纯成员存在性语义（R11）、pump `HideInHierarchy`（R14，安全改进）、`BueRuntimeCompletionBarrier`（R10b）。
3. 注释改为稳定设计理由（去诊断轮次编号）。

### 循环审查记录

第 1 轮（增量双轴并行）：Standards **CLEAN**（清理彻底无误删、R9 基线对照无生产逻辑丢失、1 判断性注释改进）；Spec **CLEAN**（存活条件与 UPM 等价、保留修复对应工单验收项、日志链与判别矩阵一致）→ 循环闭合。构建 0/0、7/7 测试 PASS（`tests-r16-*.log`）、`git diff --check` CLEAN。

### 正式产物

`artifacts/DEV-16B-management-panel-runtime-fix-r16-20260829/BetterUnturnedExperience.dll`，SHA-256 `B8E4DB14D69530FD37DD5D46416252E699E97E9220E0385741C9EC70ACFABBF8`，**CaseId `DEV-16B-R16-20260829`**。

### R16 真机预期

与 UPM 同等存活条件：`BUE-CLIENTUI-002` → `start-entered` → `plugin-update`（驱动链恢复）→ `runtime-pump-tick` → `constructor-postfix`/`host-ui-tick`（Harmony 命中）→ `create-button-*` → **按钮可见**。若某环节仍缺失，按判别矩阵定位（此时无 self-revive 干扰，读数可信）。DEV-16B 保持 `ready-for-human`：真实按钮可见性门禁待本次部署验证。

## 16. R17 极简 bisection 探针轮（2026-08-29，用户反证驱动的方向修正）

### 用户反证（关键纠偏）

R16 真机（`UMM-诊断包_20260829_225222`）驱动层依旧全停（37 行后无事件）。用户指出：**UPM 不需要修改任何 cfg 即正常工作**——推翻「HideManagerGameObject 未设置」假设与「清场导致停摆」推理链。**R15 同场铁证重申**：同一方法 `MenuWorkshopUI.constructor`，UPM 的 postfix 命中、BUE 的 postfix 未命中——**差异在 BUE 自身**。

### R17 极简对照版

`BetterUnturnedExperiencePlugin` 重写为 UPM 形状的最小插件：仅 `Harmony patch MenuWorkshopUI.constructor → ctor-postfix-hit 日志` + `Update → plugin-update 日志` + `Start/OnDisable` 日志；BUE 其余全部旁路（类型保留编译、不被调用）。其余源文件不变，其余测试不变（7/7 PASS，构建 0/0）。

### R17 真机判读分支

| 读数 | 结论 → 方向 |
|---|---|
| `ctor-postfix-hit` + `plugin-update` 出现 | 通道可用：BUE 完整版 Awake 的某语句破坏帧管线 → 按语句清单 bisection 定位 |
| `ctor-postfix-hit` 出现但 `plugin-update`=0 | Harmony 命中但组件 Update 仍不被调度 → 组件级消息问题（与 UPM 对比仅剩 GUID/类差异） |
| 两者皆零 | BUE 程序集/身份级差异 → 检查 GUID 冲突、程序集结构、加载顺序 |

### 探针产物

`artifacts/DEV-16B-management-panel-runtime-probe-r17-minimal-20260829/BetterUnturnedExperience.dll`，SHA-256 `26010A213FEA5FBC15642198AF08AA4136356F9F99EFE83DF970859CD4DF0C6B`。**探针轮产物仅用于诊断，不得驻留真机；全量 [DEBUG-min] 标记。**

## 17. R18 命中图 + R19 保活修复轮（2026-08-29，真因定案）

### R17 读数（`UMM-诊断包_20260829_230326`）——UPM 形状即可用

极简版（UPM 形状）：`awake-entered` → `patch-installed` → `on-disabled` → **`ctor-postfix-hit` 命中**——环境、detour、游戏类型 patch 全部可用；`start-entered`/`plugin-update` 因宿主禁用停止（组件消息语义）。**用户反证成立**：UPM 无需 cfg 修改即存活，"清场导致停摆"链不成立。

### R18 命中图（`UMM-诊断包_20260829_231242`）——11 patch 全命中

极简宿主 + 全部 11 个 vanilla patch，每 postfix 独立日志：**全部命中**，`MenuUI.Update` 每帧命中（数百条）、constructor ×3 命中。**Harmony 通道从来可用**；完整版静默的真因锁定为**组件 OnDestroy 链**。

### 真因定案（R15 + R18 联合证据）

`BepInEx_Manager` 被游戏 SetActive(false) → **BUE 组件 `OnDestroy` → `nativeManagementPanel.Destroy()` → `harmony.UnpatchSelf()` 自废全部 patch** → MenuUI.Update 每帧照跑但 BUE postfix 已被解除 → 一切静默。**UPM 不 unpatch**：组件死但 patch 永存，按钮死前已注入（static UI 状态）→ 功能正常。R13 的 self-revive（OnDisable 中 SetActive(true)）与引擎禁用流程竞争，叠加加速组件死亡。

### R19 修复（TDD 红→绿）

1. `BueNativeManagementPanel.Destroy(bool unpatchHarmony = true)`——`false` 跳过 UnpatchSelf（`OnTickFailure` 自毁路径传 false，patch 保留供观察/恢复）。
2. `BetterUnturnedExperiencePlugin`：`applicationQuitting` 静态标志 + `Application.quitting` 订阅（Awake 首行，先于一切可失败代码）+ `OnDestroy` 分支——**非退出（宿主被游戏销毁）= 保活**（清 driver/退订场景回调/保留 patch 与 panel 静态状态，日志 `host-destroyed state=preserved patches-kept=true`）；**应用退出 = 完整清理含 unpatch**。
3. 红测试 `AssertPanelSurvivesComponentTeardown`（CS1739 红 → 绿）：`Destroy(unpatchHarmony: false)` 后 workshop-open 与 MenuUI.Update 两 patch 仍属 BUE owner。

### 循环审查记录

第 1 轮（增量双轴并行）：Standards 0 硬违规 + 3 判断性；Spec 2 记录类 → 修复（注释措辞准确化、quitting 订阅提前、测试扩双 patch 断言、plugin 层 OnDestroy 分支路由**纯宿主不可测记 seam gap**）→ 复审双轴 **CLEAN**（4/4 闭合）。构建 0/0、7/7 测试 PASS（`tests-r19-*.log`）、`git diff --check` CLEAN。

**Seam gap 记录**：plugin 层 `OnDestroy` 的 preserve/quit 分支路由依赖 Unity 组件生命周期（`Application.quitting` 时序、宿主销毁语义），纯宿主不可自校验；panel 层语义已由 `AssertPanelSurvivesComponentTeardown` 双 patch 断言锁定。

### 正式产物

`artifacts/DEV-16B-management-panel-runtime-fix-r19-20260829/BetterUnturnedExperience.dll`，SHA-256 `3FC1C55467AE8DED713B1A3C8D19B5D479C03C2EAF8CD573D921C8A885573FEA`，**CaseId `DEV-16B-R19-20260829`**。

### R19 真机预期

R18 已证 `MenuUI.Update` postfix 每帧命中 + `host-destroyed state=preserved` 后 patch 保留 ⇒ **驱动链在宿主被清后由 Harmony 通道独立维持**：`create-button-*` 应随菜单 UI 构造出现（`OnUiRebuilt`/`OnSurfaceOpened` 均为命中路径），**"BUE 插件管理"按钮应可见**。若按钮出现可点击 → DEV-16B 可见性门禁通过，进入证据归档。
