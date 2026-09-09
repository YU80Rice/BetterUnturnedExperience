# V3-R2 诊断包自动化现状研究

- 日期：2026-09-09
- 性质：research（AFK 事实查证；不改生产代码、不改契约、不改其他票据；**不带决策倾向**，决策归 V3-T8）
- 问题票：`.scratch/bue-v2-phase3-platform/issues/12-r2-diagnostics-automation.md`
- 阻塞对象：V3-T8（唯一前置）
- 口径：每条结论回指一手源（file:line 或证据包路径）。找不到写「未找到」，不推测。

## 目录

1. 现行自动回收 SOP
2. UMM 诊断包归档清单（盘点结论）
3. `BueRuntimeLog` / `DiagnosticLogSink` 现行面
4. 现行证据链：采集 → 归档 → 命名 → 哈希 → CaseId → RELEASES
5. 哪些已自动化、哪些仍人工
6. 三层候选的收益与代价（事实依据，无裁决）
7. 未找到 / 限制

---

## 1. 现行自动回收 SOP

主源：`docs/agents/auto-rm-test-sop.md`（v1.1）。它是 `docs/agents/real-machine-test-loop.md` 的自动化扩展，与原人工循环并行有效。

### 1.1 已实证范围与明确缺口

SOP 文首（`auto-rm-test-sop.md:8-13`）：

- 已实证：U3DS headless 链全闭环（分离式启动 → 锚行轮询 → 行为期 → computer-use 干净退出 → 证据包）；双端部署 + 指纹核对（客户端 + U3DS）。
- 已知缺口②：客户端 SP 链（computer-use:启动→菜单→进世界→交互）未建，Tier-2。
- 已知缺口③：P2P 双端（需 VM/第二 Steam 账号）未建，Tier-3，暂归人工。

人工循环第一步仍是「用户实机测试，提交 UMM 诊断包」（`docs/agents/real-machine-test-loop.md:9`）。自动化扩展只覆盖 U3DS 链，不替代 SP/P2P 的 UMM 提交。

### 1.2 执行序列里与「包」相关的步骤

`auto-rm-test-sop.md:27-51` 逐步：

| 步 | 动作 | 自动化形态 |
|---|---|---|
| 0 | 读票锚行 + 读 `audit/RELEASES.md` 取候选 sha256 | 会话内人工读文件 |
| 1 | 候选 DLL → `plugins\`；`certutil -hashfile <dll> SHA256` 双端与 RELEASES 比对，不一致即停 | Agent 可脚本化；工具是本机 `certutil`（同文件:24） |
| 2 | `LogLevels = All` | 检查，非采集 |
| 3 | 分离式启动 U3DS（禁止管道重定向 stdin） | `launch-detached.ps1` 样本在 `audit/2026-09-07/auto-dryrun/launch-detached.ps1:1-3` |
| 4 | ≤240s 每 10s 轮询锚行：`takeover-patch result=installed` + `assembly-identity sha256=` 与候选精确匹配 + 本票专属锚行 | 插件锚行在 `BepInEx/LogOutput.log`；引擎行在控制台窗口（无 stdout 重定向） |
| 5 | 行为期 | 按票；U3DS 可维持，客户端交互未建 |
| 6 | **采集**：`cp` `BepInEx/LogOutput.log` → `audit/<date>/<ticket>/auto-evidence/`；引擎行用 computer-use 截图 | **仅拷贝 BepInEx 日志**，不是 UMM 整包 |
| 7 | computer-use 键入 `shutdown`；兜底 `taskkill //F //IM Unturned.exe` | 干净退出已实证；管道喂命令已实证不可行（同文件:54-55） |
| 8 | 写 `auto-evidence-notes.md` 表格逐项核对 | 会话内写 Markdown，无独立程序 |
| 9 | 票面 Comments 登记路径，停等人工验收 | 不 resolved、不关票 |
| 10 | **不动 RELEASES 的批准态** | 验收责任仍为用户（同文件:63-65） |

验收边界原句（`auto-rm-test-sop.md:65`）：「自动化消除的是采集与断言的人力，不是验收责任。」

### 1.3 干跑实证 vs 24/25 实机主链

- 干跑留存：`audit/2026-09-07/auto-dryrun/`（`LogOutput-boot-capture.log`、`LogOutput-clean-exit.log`、`server-stdout.log`、`auto-dryrun-notes.md`、`launch-detached.ps1`）。干跑笔记（`auto-dryrun-notes.md:9-17`）记录 takeover 锚行 10 秒内出现、assembly-identity 指纹匹配、强杀收尾无 clean-exit 行（预期）。
- DEV-V2-24 目录 `audit/2026-09-09/DEV-V2-24/auto-evidence/` **存在且为空**（本会话 `ls`：仅 `.` / `..`）。24/25 关单主证据不在该目录。
- 本仓库 `src/` 下 **未找到** 任何写入 `UMM-诊断包_*` 或打包 `LogOutput.log` 的 C# 代码（全库 `src` 搜索零命中）。SOP 采集面是 Agent 会话 `cp`，不是 BUE 运行时导出。

### 1.4 SOP 对后续环节的硬边界

- 不授予 CaseId、不改 RELEASES、不关票（`auto-rm-test-sop.md:51`）。
- `LogOutput.log` 覆盖式写入，每次启动重置（同文件:60）——启动前要记基线。
- 多日志会话归属用文件 birth time，横幅时间戳不可信（同文件:61）。
- 指纹工具只针对 **DLL**（`certutil`），SOP 文本未规定对拷出的 `LogOutput.log` 自动算哈希。

---

## 2. UMM 诊断包归档清单（盘点结论）

主源：`.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md`（盘点截止 2026-09-09 15:19；§5 为同日傍晚用户委托执行记录）。

### 2.1 包从哪来、里面有什么

- 导出通道不在 BUE 仓库：盘点对象是启动器工作目录 `…\启动器\UnturnedModManager\publish\UMM-v2.2.1-win-x64`（清单:5）。BUE `src/` 无诊断包写入代码。
- 包命名：`UMM-诊断包_YYYYMMDD_HHMMSS`（文件夹）或同名 `.zip`。
- 主机位典型内部文件（清单 §2.1 / 本会话对 `audit/2026-09-09/DEV-V2-25/evidence/v8-acceptance-p2p/host-193108/` 的 `ls`）：`LogOutput.log`、`Client.log`、`Client_Prev.log`、`UMM-诊断摘要.txt`、`Unturned_d3d11.log`、`Unturned_dxgi.log`。
- 客机位常无 d3d/dxgi（清单 §2.3；v8 客机 `client-193114/` 仅四件：LogOutput + Client + Client_Prev + 摘要）。
- 摘要样本（`host-193108/UMM-诊断摘要.txt:1-13`）：标题含导出时刻；分类 Normal；「来源」列出上述绝对路径。摘要「来源」含 `E:\Steam\...` ≈ 本机主机位，含 `C:\Program Files (x86)\Steam\...` ≈ 客机位（清单:30）。

U3DS 服务端日志**不是** UMM 诊断包导出通道（清单:297）：仓库可有 `logoutput-v7-u3ds-server-…`，UMM 目录无对应散落文件。散落 `U3DS_*LogOutput.log` 是另路拷贝。

### 2.2 三档口径与计数（盘点当时）

清单 §0 / §4：

| 档 | 定义 | 当时计数 |
|---|---|---|
| 已归档 | 仓库 `audit/**` 存在同大小且 SHA-256 相同的 `LogOutput.log`（或 `logoutput-*.log`），并有 case.md / verification.md 绑定 | 44 |
| 仅引用 | `audit/` 或 `.scratch/` 的 md/txt 命中包名时间戳，但无同 SHA 全文 | 15 |
| 未归档 | 两路皆无 | 10 |

合计 69 项、约 54.7 MiB。对照只做 `LogOutput.log` 哈希，**未**对 `Client.log` / d3d / zip 内部做哈希（清单:292）。「已归档」不等于整包入库（清单:294）。

### 2.3 事后补收与清理（同日傍晚）

清单 §5（:301-305）：用户委托执行后，已删 60 项（§3.1 的 44 + §3.2 补归档后的 15 + 升格的 204026）；删除前 60/60 SHA-256 零差异。剩余 9 项为 §3.3 保留清单。

DEV-V2-25 票面 Comment（`.scratch/…/issues/DEV-V2-25-….md:47`）记录同一轮：

- 证据 A/B 三包 210433/225112/225127 整包补到 `audit/2026-09-09/DEV-V2-25/evidence/scope4-raw/`（本会话 `ls` 确认三目录均含 LogOutput + Client + 摘要；主机位另有 d3d/dxgi）。
- 同轮把 24 票引用的其余 13 份夜场/诊断轮日志按盘点建议落位。

本会话抽查：

- `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/diag-night/host-195933/` **仅** `LogOutput.log`（1044020 B）——与清单「多数 cases 只收 LogOutput」一致。
- `scope4-raw/host-210433-litfb4/` 为整包 6 文件。
- v8 验收 `host-193108/` 为整包 6 文件；`client-193114/` 为 4 文件。

### 2.4 对自动化的直接事实

- 采集器 = 仓外 UMM，BUE 不参与打包。
- 入库是事后拷贝 + SHA 对照，历史上多次「票面引用时间戳、原文不在库」（15 项仅引用；Scope 4 三包曾被判「全库/git/游戏目录/TEMP 均无」，实际在 UMM 目录——清单 §2 + DEV-V2-25 Comment :41/:47）。
- 命名键是 UMM 导出时刻 `YYYYMMDD_HHMMSS`，不是 CaseId；CaseId 在仓库侧 `case.md` / RELEASES 另绑。
- 6 位 HHMMSS 有撞号风险（清单:293），归档判定优先完整日期 + SHA。

---

## 3. `BueRuntimeLog` / `DiagnosticLogSink` 现行面

本仓库 **未找到** 类型 `BueDiagnostics`（`src/` / `CONTEXT.md` / `docs/` 搜索零命中）。愿景文档把 `BueDiagnostics` 列为「结构化日志、错误码、状态、诊断包」（`.scratch/bue-v2-phase2-official-adoption/research/2026-09-07-bue-platform-vision-phase3.md:70`）；对账注记将「诊断包自动化」标为「超出现有 BueRuntimeLog」（同文件:210）。现行实现止于日志缝，不含打包。

### 3.1 `BueRuntimeLog`：级别策略 + 测试 Recorder

文件：`src/BetterUnturnedExperience.Plugin/BueRuntimeLog.cs`。

| 方法 | 级别 | 行 |
|---|---|---|
| `Runtime(string)` | Debug（正常游玩静默，除非 BepInEx Disk 含 Debug 或测试 Recorder） | :27-36 |
| `Load(string)` | Info（测试 Recorder 前缀 `Info`；生产走 `LogInfo`） | :42-51 |
| `Warn(string)` | Warning，不受运行时静默策略约束 | :57-66 |
| `Error` / `ErrorFriendly` | Error；后者加中文前缀 `BUE 错误：` | :70-87 |
| `AnnounceReady(bool)` | Info，会话恰一次 | :136-151 |
| `IsRuntimeEvent` / `IsCriticalNotReadyReason` | 纯分类器 | :94-129 |

测试缝：`internal static Action<string> Recorder`（:16）。Recorder 非空时**不写 BepInEx**，只把 `"Debug "/"Info "/"Warning "/"Error "` + 行交给测试。绑定：`Bind(ManualLogSource)`（:20-23）。

身份锚行**不走** `BueRuntimeLog`，走插件 `Logger.LogInfo`（Info，每会话一行）：

```417:417:src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs
Logger.LogInfo("[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity path=" + location + " sha256=" + hash);
```

注释（同文件:413-416）：assembly-identity 是三环境验证的证据锚，保持 Info，其它 load one-shot 降 Debug。哈希由本机读 DLL 文件算 SHA-256（:403-411）。

玩家手册要求报问题时附上该行（`docs/BetterUnturnedExperience-Player-Handbook.md:24`）。

### 3.2 `DiagnosticLogSink`：多处静态 Action，不是统一采集器 API

仓库内是若干 **internal static Action<string>**（ClientUi 一处带级别），生产绑到 `BueRuntimeLog`，测试替换为 List 记录器。无公共 `IDiagnosticCollector` / 无写文件 / 无组包。

| 位置 | 形态 | 生产绑定 |
|---|---|---|
| `NetworkModuleAdapter.DiagnosticLogSink` `NetworkModuleAdapter.cs:66` | `Action<string>` | `BindProductionLog()` :581-586：含 `result=failed` → `ErrorFriendly`，其余 `Runtime`；同时把 `LmnV1CompatLayer.DiagnosticLogSink` 绑到 `Runtime` |
| `LmnV1CompatLayer.DiagnosticLogSink` `LmnV1CompatLayer.cs:25` | `Action<string>` | 见上；发射 `diagnosticId=BUE-V1COMPAT-001`（:92-100, :107-110），sink 空则丢弃 |
| `InventoryDragPreviewAdapter.DiagnosticLogSink` `InventoryDragPreviewAdapter.cs:472` | `Action<string>` | Activate 时绑 `ErrorFriendly("[BUE-DRAG] event=diagnostic-failure … diagnosticId=BUE-DRAG-003")`（:117）；`EmitDiagnosticOnce` :474-486，无 sink 时只留 `LastCleanupDiagnostics` |
| `InventorySurfaceLifecycleAdapter.DiagnosticLogSink` `InventorySurfaceLifecycleAdapter.cs:980` | `Action<string>` | Activate 绑 ErrorFriendly（:1075 一带）；`EmitDiagnosticOnce` :982-995；F-B2 `TransientIsolationGate` 经此缝打一次性行（:874-883） |
| `ClientUiCompositionRoot.DiagnosticSink` `ClientUiTypes.cs:103-114` | `Action<string, ClientUiDiagnosticLevel>`（Debug/Error） | Plugin Awake :138-146：已含 `diagnosticId=` 则不追加 `BUE-CLIENTUI-001`；Error→`BueRuntimeLog.Error`，否则 `Runtime` |

`Emit` 空 sink = 静默（网络适配器 :839-845；V1 层 :107-110）。这是**运行时诊断行出口**，不是诊断包写入口。

### 3.3 结构化行惯例（事实，非正式 schema 文件）

常见 token（抽样源码，非完整枚举）：

- `event=` / `diagnosticId=` / `featureId=` / `result=` / `decision=` / `sha256=` / `path=`
- 前缀：`[BUE-UI-TRACE]`、`[BUE-DRAG]`、`[BUE-CLIENTUI]`、`[BUE-V2HOST]`、`[BUE-V2LHT]`、`[BUE-V2LIR]`、`[Tidy]`、`[TidyNet]` 等
- 身份：`event=assembly-identity … sha256=`（Plugin.cs:417）
- 接管：`takeover-patch result=installed` / `result=removed decision=hand-back-to-vanilla`（SOP 与 case.md 锚）
- 防双装：`BUE-PLATFORM-001` Warning（`BuePlatformDoubleInstallCheck.cs:63` 一带）

本仓库 **未找到** 独立的诊断行 JSON schema 或 DiagnosticId 注册表文件。契约侧有 `DiagnosticId` 字段（注册/启动/状态视图）和 `IFeatureLogger`（`ContractTypes.cs:121-126`），见下节。

### 3.4 公开契约上的日志面：接口在、生产未接线

- `IFeatureLogger`（`ContractTypes.cs:121-126`）：`Info/Warning/Error(eventName, diagnosticId, …)`。
- `IFeatureBootstrap.Logger` 由 `FeatureBootstrap` 原样持有（`FeatureBootstrap.cs:21-39`）。注释写明 events/logger/dependencies/lifetime「pass through as provided」（:14-16）。
- 生产启动路径 `BueFeatureStartRuntime.cs:61-70` 构造 `FeatureBootstrap(…, logger: null, dependencies: null, lifetime: null, featureNetwork)`。
- 全库 **未找到** `class … : IFeatureLogger` 实现。测试里同样传 `null`（`Program.cs:3745` 等）。
- 官方模块（LIT/LIR/LHT）诊断行走 `BueRuntimeLog` / 各内部 sink，不走 `IFeatureLogger`。

因此：契约预留了功能级 logger，**现行生产没有采集器 API 实现**。生态 DLL 若调用 `bootstrap.Logger` 会遇到 null（生产构造如此）。

### 3.5 红测锚点（`--*-red`）

宿主：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`。旗标在 `Main` 前段（:25-326 一带）；默认套件亦调用同一批断言（:398-408 一带含 logging / F-B1 / F-B1c）。

与本票直接相关：

| 旗标 | 断言 | 钉什么 |
|---|---|---|
| `--logging-verbose-red` | `AssertLoggingRuntimeVerbosity` :1508+ | Runtime=Debug / Load=Info / Error 带 reason 且不被静默 |
| `--logging-bue-runtime-red` | `AssertLoggingBueRuntimeClassification` | `IsRuntimeEvent` 分类 |
| `--logging-failure-red` | `AssertLoggingFailureEmission` | DiagnosticLogSink 失败行一次性发出 |
| `--logging-gate-red` / `--logging-timeout-red` / `--logging-aggregate-red` | 门/超时/聚合就绪 | 级别与一次性 |
| `--dev16d-diagnostics-red` | 预览诊断含坐标读数 | 结构化诊断内容 |
| `--bue-v2-fb1-red` | 故障闸 threw/isolated 行 + BUE-DRAG-004 | ClientUi DiagnosticSink |
| `--bue-v2-fb1c-red` | BUE-LIT-001 对账行 | 整理后投影修复诊断 |
| `--bue-v2-fd-red` | headless 完成链 / BUE-BOOTSTRAP-003 | 无头泵锚 |
| `--bue-v2-fe-red` | SteamId 真值表 | 不直接打日志包 |
| `--bue-v2-lit-sendhealth-red` | 限频/退避/BUE-LIT-003 | 失败面可见化（条数有界） |

测试手法：替换 `BueRuntimeLog.Recorder` 或各 `DiagnosticLogSink` 为 `List<string>`（例 :1461-1462, :1511-1512）。**红测钉的是行内容与级别，不钉打包、不钉归档路径、不钉 CaseId。**

愿景对账「诊断包自动化超出 BueRuntimeLog」（vision:210）与源码现状一致：自动化对象若是「包」，现行缝不够。

---

## 4. 现行证据链：采集 → 归档 → 命名 → 哈希 → CaseId → RELEASES

样本主源：DEV-V2-24 / DEV-V2-25 结单、`audit/2026-09-08/`、`audit/2026-09-09/`、`audit/RELEASES.md`、采集手册 `audit/2026-09-08/DEV-V2-24/DEV-V2-24-three-env-acceptance-handbook.md`。

### 4.1 两套并行通道

| 通道 | 触发 | 产物 | 样本路径 |
|---|---|---|---|
| A. UMM 诊断包（SP / P2P / 客户端主链） | 用户在启动器导出 | `UMM-诊断包_YYYYMMDD_HHMMSS/` 整包 | 工作目录在仓外；入库例 `audit/2026-09-09/DEV-V2-25/evidence/v8-acceptance-p2p/host-193108/` |
| B. SOP `cp LogOutput.log`（U3DS headless） | Agent 按 auto-rm-test-sop 步 6 | 单文件 → `audit/<date>/<ticket>/auto-evidence/` | 干跑 `audit/2026-09-07/auto-dryrun/LogOutput-*.log`；24 的 `auto-evidence/` 目录空 |

24/25 关单主链走通道 A。通道 B 在 07 干跑实证，未构成 24/25 结单正文。

### 4.2 逐步（以 DEV-V2-24 / v8 为样本）

**① 采集**

- 用户实机操作 + UMM 导出（real-machine-test-loop.md:9；24 票 Comments 多次「UMM 包 NNNNNN」）。
- Agent 可代部署 DLL，不可代用户点 UMM、不可代 VM 客机操作（24 手册:5「待你按本手册实机采集」；SOP 缺口③）。
- U3DS 服务端日志另路：Agent 从 `U3DS\BepInEx\LogOutput.log` 拷（SOP:40）；不经 UMM（清单:297）。

**② 归档**

观察到至少三种落位，无单一强制脚本：

1. 采集 Case 树：`audit/<date>/evidence/<采集CaseId>/cases/<env>/`  
   例：`audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/{sp,p2p-host,p2p-client,u3ds,coexist-b,t7,diag-night}/`
2. 工单证据树：`audit/<date>/<ticket>/evidence/<label>/`  
   例：`audit/2026-09-09/DEV-V2-25/evidence/scope4-raw/`、`…/v8-acceptance-p2p/`
3. SOP 自动树：`audit/<date>/<ticket>/auto-evidence/`（24 为空）

拷贝粒度不统一：diag-night 多数仅 LogOutput；scope4-raw 与 v8 主机为整包。清单 §3 警告：只收 LogOutput 会丢掉 Client.log / d3d。

**③ 命名**

- UMM 侧：`UMM-诊断包_YYYYMMDD_HHMMSS`（导出时钟）。
- 入库侧常见：
  - 历史 case：目录内原名 `LogOutput.log`（`cases/sp/LogOutput.log`，v3 轮）。
  - 多轮并存：`logoutput-v6-20260909-111429.log`、`logoutput-v7-20260909-125324.log`（同目录堆叠，避免覆盖）。
  - 补归档：`host-195933/`、`host-210433-litfb4/`、`host-193108/`（端别 + HHMMSS [+ 探针标签]）。
- 没有仓库内代码把 UMM 包名自动改写成上述目录名。

**④ 哈希（两条身份，不要混）**

| 对象 | 谁算 | 用途 | 锚 |
|---|---|---|---|
| 候选 DLL | 三轮 `-t:Rebuild` + `certutil` / 审计 txt | 发布身份、部署核对 | `audit/2026-09-08/DEV-V2-24/identity-sha256.txt`；`audit/2026-09-09/DEV-V2-25/identity-sha256.txt`；手册:45 |
| 运行中 DLL | 插件启动读文件 SHA-256 打 `assembly-identity` | 日志内绑定「这场跑的是哪份」 | Plugin.cs:403-417 |
| `LogOutput.log` 原文 | 归档时人工/Agent `certutil` 或盘点 `sha256sum` | 证明入库全文 = UMM 原件 | `cases/sp/case.md:18` 写明本文件 SHA-256；p2p-host `case-v6-20260909.md:2`「sha256 见 certutil 留痕」 |

SOP 的 certutil 只对 DLL（auto-rm-test-sop.md:24,29）。日志哈希不是 SOP 强制步，出现在 case.md / 盘点报告。

**⑤ CaseId**

两套并存（identity-sha256.txt:6；手册:23-24；RELEASES 行 9/10）：

- 候选阶段 CaseId：`DEV-V2-24-CANDIDATE-20260909`、`DEV-V2-25-CANDIDATE-20260909`（跟 DLL 走）。
- 采集 CaseId：`DEV-V2-24-20260908`（手册冻结「采集期间不得更换」；证据目录名用它）。24 关单授予采集 CaseId（24 票 Status 行）。
- 授予规则：`docs/agents/output-review-loop.md:19-21` — 双轴 CLEAN 后才授予 SHA-256 / CandidateBuild / CaseId；中间 DLL 无 CaseId。
- SOP **不授予** CaseId（auto-rm-test-sop.md:51）。

**⑥ RELEASES 绑定**

`audit/RELEASES.md` 表列：候选/CaseId、DLL SHA-256、身份锚、快照、门禁、批准、归档/证据、状态。

- 维护规则（文首）：人工批准/验收入档的同一提交里追加行；禁止删改历史行；批准不向下自动继承。
- 行 9（v7）「归档/证据」列指向 `audit/2026-09-08/evidence/DEV-V2-24-20260908/` + `audit/2026-09-09/DEV-V2-24/` + 手册路径；UMM 包号列在「批准」叙述里（111429/112239/…）。
- 行 10（v8 当前发布物）指向 `audit/2026-09-09/DEV-V2-25/` 与 `evidence/v8-acceptance-p2p/case-v8-20260909.md`；包 193108/193114。
- SOP 步 10 明确不动 RELEASES 批准态；关单由用户或用户授权会话收尾（24 票关单 Comment；25 终关单 Comment :49）。

轻量链口径（RELEASES 注记）：LoadSetIdentity = 同一 DLL SHA-256 贯穿全部 `assembly-identity` 行 + 确定性重建，不另产 canonical 摘要。

### 4.3 目录惯例（本会话 `find`/`ls`）

```
audit/2026-09-08/
  DEV-V2-24/          手册、kit、identity、review-rounds
  evidence/DEV-V2-24-20260908/
    candidate/identity.txt
    cases/{sp,p2p-host,p2p-client,u3ds,coexist-b,t7,diag-night}/
  artifacts/
audit/2026-09-09/
  DEV-V2-24/          结单、重建 log、空 auto-evidence/
  DEV-V2-25/
    identity-sha256.txt, candidate-v8-rebuild*.log, artifacts/
    evidence/scope4-raw/{host-210433-litfb4,host-225112-litfb7,client-225127-v3}/
    evidence/v8-acceptance-p2p/{host-193108,client-193114,case-v8-20260909.md}
```

`case.md` 把 UMM 包名、assembly-identity 行号、LogOutput SHA、锚行表绑在一起（`cases/sp/case.md:5-18`）。v8 case 是判据表 + 行号，不重复贴全文（`case-v8-20260909.md`）。

### 4.4 断裂点（已发生，非假设）

- Scope 4：票面引用 210433/225112/225127 行号，库内无原文 → 「曾送达后中途劣化」无法从库内重放（25 票 Comment :41）。后从 UMM 目录补收才闭合（:47）。
- 盘点 15 项「仅引用」、10 项「未归档」：时间戳进了票面，全文未入库。
- 13 包 assembly-identity 矩阵曾误判 litfb5 在跑，因包未统一落盘、靠记忆对时间戳（24 票 Comment「重大勘误」）。
- `auto-evidence/` 空 vs UMM 主链：自动化目录惯例与实际关单证据树未合流。

---

## 5. 哪些已自动化、哪些仍人工

按证据链环节列。所谓「已自动化」= 有可重复的程序/运行时/SOP 步骤，不必每次从零发明；不表示「零人工」。

| 环节 | 已自动化（事实） | 仍人工（事实） |
|---|---|---|
| 结构化日志发射 | 运行时 `BueRuntimeLog` + 多处 `DiagnosticLogSink`；红测钉级别与 token | 无统一 schema 文件；DiagnosticId 无注册表 |
| 身份行写入日志 | 启动时读 DLL 算 sha256 打 `assembly-identity`（Plugin.cs:403-417） | 无 |
| Debug 行进磁盘 | 依赖 `BepInEx.cfg` `[Logging.Disk] LogLevels = All`（SOP:22；手册:46） | 部署时检查/开启 cfg（手册步 3） |
| U3DS 启动与锚行轮询 | SOP 步 3–4 + `launch-detached.ps1`；07 干跑 10s 内 takeover+identity | 读票提取本票锚行清单（SOP 步 0） |
| U3DS 干净退出 | computer-use 键入 shutdown（SOP 步 7，v1.1 已实证） | 焦点被抢需再 activate；失败则 taskkill |
| U3DS 日志拷贝 | SOP 步 6 `cp LogOutput.log` → `auto-evidence/` | 目录创建、notes 表格、票面回填仍是会话内 Markdown |
| 客户端 SP 交互链 | **未建**（SOP:11 缺口②） | 用户进世界/操作 |
| P2P 双端 | **未建**（SOP:12 缺口③） | 用户 + VM/第二账号 |
| UMM 整包导出 | **仓外启动器**；BUE 无导出代码 | 用户点 UMM；Agent 事后从 UMM 目录找包 |
| 会话内自动留档 | **未找到** 运行时在会话结束时写 `audit/` 或写 UMM 包 | 全程事后拷贝 |
| 文件回收到 git | Agent/用户 `cp`；无入库脚本 | 选目录名、选整包 vs 仅 LogOutput、写 case.md |
| 日志 SHA | 盘点用 `sha256sum`；case 用 certutil 留痕 | 非 SOP 强制；漏做即「仅引用」 |
| DLL 指纹 | `certutil`；identity-sha256.txt；部署指纹 txt | 双端比对、写 `deploy-fingerprint.txt`（手册:45） |
| 锚行断言 | SOP 步 8 表格；case.md 锚行表；Agent 可 grep | 表是手写 Markdown；FAIL 走修复流程 |
| CaseId 授予 | 规则文档化（output-review-loop 第 4 步） | 双轴 CLEAN 后人工/结单会话写入 |
| RELEASES 行 | 台账格式固定 | SOP 禁止动；关单会话追加；用户批准 |
| 验收/关票 | 无 | 用户（SOP:65；CONTEXT 发布授权） |

会话内操作（本表「仍人工」的子集）：读票、提取锚行、写 notes/case.md、票面 Comment、选归档树、决定整包还是只收 LogOutput、RELEASES 加行。

07 干跑已证明 U3DS 最小链可自动到「日志文件落在 audit/」；24/25 关单证据却走 UMM 树。两套目录约定并存，自动回收没有接到关单主链。

---

## 6. 三层候选的收益与代价（事实依据，无裁决）

V3-T8 票面候选层：①一键采集 ②会话内自动留档 ③采集器 API。下列只列可回指的收益/代价事实，**不排序、不推荐**。

### 6.1 层① 一键采集

**所指（由现状外推的最小含义，非规格）**：把「把这场的日志收成一份可复核包」收成一次动作。现状对照物：UMM 导出（用户一键、仓外）与 SOP 步 6（Agent `cp` 单文件）。

**收益，有据：**

- 关单主链已经依赖「一份带时间戳的包」而不是散落路径：v8 成对包 193108/193114 整包入库后，case 才能写双端 identity 与限频恰 1 条（`case-v8-20260909.md`）。
- 漏采集的代价已发生：Scope 4 三包未入库时，「challenge 曾送达后中途劣化」无法从库内重放（25 票 :41）；补收后才双轨闭合（:47）。一键若把原文直接落到 `audit/`，可消除「UMM 工作目录 vs git」检索面分裂（清单 §2「当时只搜库内/git/游戏目录/TEMP」）。
- SOP 已把 U3DS 单文件拷贝写成步骤；缺口是客户端/P2P 与整包附件（Client.log/d3d）。清单:294 记录删 UMM 包会丢这些附件。

**代价，有据：**

- 采集器今天在仓外 UMM；BUE `src/` 无打包代码。在 BUE 内做一键 = 新生产面，或继续依赖 UMM。
- `LogOutput.log` 覆盖式写入（SOP:60）——一键必须发生在下次启动前，否则本轮被冲掉。
- P2P 需两端各一包（24 成对时间戳差数秒：165948/165949、125312/125324、193108/193114）。一键若只打本机，客机/VM 仍要各采一次。
- U3DS 服务端不走 UMM（清单:297）。一键若只封装 UMM，服务端仍要 SOP `cp`。
- 一键不自动产生 CaseId / RELEASES 行（SOP:51；output-review-loop:19-21）。收益停在「原文在库」，不延伸到发布授权。

### 6.2 层② 会话内自动留档

**所指**：游戏/插件在会话结束或关键拍点把证据写入约定目录，无需用户点 UMM、无需 Agent 事后从工作目录翻包。

**收益，有据：**

- 现行断裂全是「会话已结束、包在 UMM 或未导出」：15 项仅引用、10 项未归档（清单 §0）；210433 等三包「完整躺在 UMM」却曾判均无（清单 §2）。自动留档把「导出」从用户记忆里拿掉。
- BepInEx 日志每次启动重置（SOP:60）——若不在会话内截存，下一轮启动即丢。自动留档的时间窗 = 本进程生命周期。
- 干净退出锚 `takeover-patch result=removed` 只在正常 shutdown 后出现（SOP:47；干跑强杀则无，`auto-dryrun-notes.md:26`）。会话内在 hand-back 之后拷贝，才能稳定抓住退出链。

**代价，有据：**

- 生产代码今日只写 BepInEx 日志，不写 `audit/`。会话内留档要碰文件系统、路径、权限、头无盘环境（U3DS headless）。本仓库无此实现，代价是新运行时职责。
- 覆盖式日志 + 多会话归属用 birth time（SOP:61）——自动留档必须自己切会话边界，否则会把上一场尾与本场头拼错。
- 无头/专用服务器与客户端附件集合不同（v8 主机 6 文件 vs 客机 4 文件；U3DS 无 UMM 摘要）。同一套自动留档要处理端别差异，否则又回到「只收 LogOutput」。
- SOP 验收边界：自动化不替代人工验收（auto-rm-test-sop.md:65）。会话内留档仍不关票、不改 RELEASES。
- 客户端 SP/P2P 交互链未建（SOP:11-12）——自动留档解决「收文件」，不解决「这场该做哪些操作才有锚行」。

### 6.3 层③ 采集器 API（供功能模块写入自己的证据）

**所指**：功能（官方或生态）调用稳定 API，把本功能证据写入诊断面/包。愿景 `BueDiagnostics` 含「结构化日志、错误码、状态、诊断包」（vision:70）；对账称诊断包自动化超出 BueRuntimeLog（vision:210）。

**收益，有据：**

- 现行内部缝已经按功能拆：DRAG / INVENTORY / V1COMPAT / CLIENTUI / LIT-001 / LIT-003 / PLATFORM-001。红测按缝替换 sink（Program.cs 多处 `previousSink = …DiagnosticLogSink`）。API 若只是把这些 internal Action 收成公开面，测试手法已存在。
- 契约已预留 `IFeatureLogger`（ContractTypes.cs:121-126）挂在 `IFeatureBootstrap.Logger`。生态路径活样板 NoOpFixture 经公开桥注册（vision 注记 3）。若 API 落在该接口，生态与官方可走同一注入点。
- F-C 风暴（1199/1057/8333/1650 条）说明功能自己的失败面一旦无界就会淹没包（24/25 票）。限频是功能级诊断政策（`--bue-v2-lit-sendhealth-red`），不是打包层政策。采集器 API 可把「功能写证据」与「打包」分开，避免每个模块直接打 BepInEx。

**代价，有据：**

- 生产 `BueFeatureStartRuntime.cs:67` 传 `logger: null`；全库无 `IFeatureLogger` 实现。公开 API 从「接口已写」到「可调用」要接线 + 实现 + 红测；当前调用会 null。
- 各 `DiagnosticLogSink` 是 internal static，ClientUi 还带级别枚举。收成一个 API 要统一签名（有无 level、空 sink 丢弃 vs 保留 LastDiagnostics）。
- 红测钉的是行，不是包。API 若承诺「写入诊断包」，现有 `--*-red` 都不覆盖该承诺，需新旗标。
- 愿景生态层消费「设置/诊断/隔离」（vision 注记 2:217）。诊断若进公开 Major 契约，受地图「不提前扩张公开 Major」（map.md Out of scope）约束——这是地图已写的范围事实，不是本票裁决。
- API 不自动产生 UMM 包或 `audit/` 目录。LIT/LIR/LHT 今天已经能往 LogOutput 写锚行；缺的是入库，不是「功能不会写日志」。

### 6.4 三层与现状的交叉（仍非裁决）

| 层 | 已有对照物 | 明确未覆盖 |
|---|---|---|
| ①一键采集 | UMM 用户导出；SOP `cp` 单文件（U3DS） | 客户端/P2P 一键；整包附件；自动落到 git 树 |
| ②会话内自动留档 | 无 | 全部 |
| ③采集器 API | internal sink + 未接线的 `IFeatureLogger` + 结构化行 | 实现类、生产注入、写包、生态同权可见性 |

层①最接近已跑通的人工/SOP 动作；层②在源码与 SOP 中均未出现；层③在契约有空接口、在运行时有 internal 缝，两者未接。任何一层都不自动完成 CaseId 授予或 RELEASES 批准（output-review-loop 第 4 步 + SOP 步 10）。

---

## 7. 未找到 / 限制

下列为本会话显式检索后的负结果，避免后续把「没有」读成「没查」。

| 检索项 | 结果 |
|---|---|
| 类型 / 模块 `BueDiagnostics` | `src/`、`CONTEXT.md`、`docs/` 零命中 |
| `src/` 写入 `UMM-诊断包_*` 或打包 `LogOutput.log` | 零命中 |
| `class … : IFeatureLogger` 实现 | 零命中；生产传 `null`（`BueFeatureStartRuntime.cs:67`） |
| 独立 DiagnosticId 注册表 / 诊断行 JSON schema 文件 | 未找到 |
| 把 UMM 包自动改名入库、自动算日志 SHA、自动写 case.md / RELEASES 的仓库脚本（ps1/sh/py） | 未找到（`certutil` 仅出现在 SOP/手册/case 叙述） |
| DEV-V2-24 `audit/2026-09-09/DEV-V2-24/auto-evidence/` 内证据文件 | 目录存在、空 |
| SOP 客户端 SP 链、P2P 双端自动链 | 文档标明未建（auto-rm-test-sop.md:11-12） |
| 会话结束时运行时写 `audit/` | 未找到 |

限制：

- UMM 导出实现在仓外启动器目录，本票未反编译 UMM.exe，不把启动器内部算法当 BUE 事实。
- 盘点清单位于 2026-09-09 15:19；§5 执行记录与 25 票补收在其后。本报告以清单 + 票面 + 本会话 `ls` 三方对照，不重做 69 项哈希。
- `DiagnosticLogSink` / `event=` 行未做全量枚举；§3.3 为抽样。
- 不评「应做哪一层」——归 V3-T8。
