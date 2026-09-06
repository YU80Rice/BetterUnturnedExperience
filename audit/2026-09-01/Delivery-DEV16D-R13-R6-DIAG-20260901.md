# 交付报告 — DEV-16D-R13-R6-DIAG 判别插桩

> CaseId：`DEV-16D-R13-R6-DIAG-20260901` · CandidateBuild：`DEV-16D-R13-R6-DIAG-20260901`
> 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-20260901.dll`
> SHA-256：`577D57668796FBE41D31BA70D999900EBB4EAA32824253B455F62F64C58CB997`（233984 bytes）
> 本轮性质：**判别插桩（diagnostic instrumentation）**，非 DEV-16E 资格候选。用于在下一轮实机日志中区分 H1-H4 哪个门挡住了功能。

## 1. 背景（为什么需要这一轮）

用户部署 R13 正式 DLL（`BetterUnturnedExperience-1925.dll`，sha256 `45D509...`）在单人实机测试：背包页、普通容器、车辆后备箱**均无**绿/红预放置块、无自动旋转、无强化吸附。两份 UMM 诊断包（19:55、20:45 会话）确认：

- 插件加载成功（sha256 匹配）、runtime gate Client、全部 Harmony hook 安装；
- feature `accepted=True reason=None`、composition ready；
- `inventory-events-subscribed` 触发过（说明 Player.LocalPlayer 存在、LifecycleCanRun 曾为 true）；
- **但全程零 `surface-context-dispatched`、零 `drag-started`、零 `placement-decision`**。

即：功能被完整接线，但运行时从未驱动。源码静态审计无法在本地复现（游戏不可运行），故按 `real-machine-test-loop.md` 走判别插桩轮，让下一份实机日志精确指出被哪个门挡住。

## 2. 本轮假设 → 插桩映射

| 假设 | 判别插桩点 | 新增日志 |
|---|---|---|
| H1 surface 从未 dispatch | `InventorySurfaceLifecycleAdapter.Poll()`：`!tracker.TryGetActiveGeneration` 静默 return；`!liveSurfaceReady continue` | `[DEBUG-SURF] ... reason=no-active-session (reason=disconnected/dashboard-closed/tracker-inactive/generation-unknown)`；`[DEBUG-SURF] ... reason=hierarchy-not-ready page=.. hierarchy=.. hasSleekItems=..` |
| H2 drag Poll/Tick 被门吞掉 | `InventoryDragPreviewAdapter.Tick()` L537 门；`Poll()` 门 | `[DEBUG-DRG] event=tick-silent/poll-silent reason=lifecycle-gate reason=blocked lifecycleCanRun=.. enhancedDragActive=.. state=..` |
| H3 `Provider.isConnected`/`active` 读值异常 | 并入 H1 的 no-active-session reason 字段（`dashboardActive/isStoring/connected`） | 见 H1 |
| H4 Harmony postfix 未触发 | `PlayerUIUpdatePostfix`、`DashboardUpdatePostfix` 每 120 帧计数 | `[DEBUG-SURF] event=postfix-alive target=PlayerUI.Update count=..`；`[DEBUG-DRG] event=postfix-alive target=updateDraggedItem count=..` |

全部日志 `[DEBUG-*]` 前缀 + 每 2 秒节流（`LogSilenceOncePerTwoSeconds`），不改变任何原生行为、不新增运行时依赖、保持单 DLL 闭包。

## 3. TDD 红→绿

- **红测 seam**：`--dev16d-r13-silence-red` → `AssertDev16DR13SilenceTraceSeams()` 引用两个尚不存在的生产 seam `InventorySurfaceLifecycleAdapter.DescribeNoActiveSession(...)` 与 `InventoryDragPreviewAdapter.DescribeTickGate(...)`。
- **红**：实现前编译失败（`CS0117` ×2，退出码 1）。
- **绿**：实现两 seam + 接入六个插桩点 → 编译通过，运行时断言通过（退出码 0）。

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建（MSBuild 18.9.1, sln /t:Build Release） | 0 errors / 0 warnings，退出码 0 |
| 七个测试运行器（Contracts/Settings/Placement/ClientUi/Network/Plugin/Release） | 全 PASS，退出码 0 |
| R13 定向测试（red/boundary/surface/rotation/stale/page/page-seam/native-delegate/passthrough/**silence**） | 10/10 全 PASS，退出码 0 |
| UI/native token 扫描（ClientUi 11 / Contracts 2 / Core 10） | 全 PASS（新增插桩零 UI token） |
| `git diff --check` | 退出码 0（仅 CRLF 提示） |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名，按 output-review-loop 第 3 步） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① `LogSilenceOncePerTwoSeconds`/`lastSilenceLogTick`/`Log` 访问器/`%120` 计数跨两个 adapter 复制（Duplicated Code，可提取共享 throttle）；② `DescribeNoActiveSession` 五布尔参数（Data Clumps）且重新实现 tracker-gate 语义，reason 可能与 `TryGetActiveGeneration` 漂移；③ `DescribeTickGate` 的 `can-run`/`enhanced-active` 分支在两调用点不可达（Speculative Generality）；④ postfix 经 `adapter.Log` 暴露 internal 访问器（Feature Envy），且该日志位于隔离 try/catch 之外 |
| **Spec** | **CLEAN** | 无 | ① H3 字段只在 `tracker-inactive` 分支完整携带，`disconnected/dashboard-closed/generation-unknown` 分支不渲染字段——reason token 仍足以判别 H1/H2/H3 及其子情形，故非阻断 |

> 依据 `output-review-loop.md` 第 3 步：judgment-call smells 仅在审计中显式列名时可延后；以上均已列名。后续在 DEV-16E 资格轮应消解 ①②③④ 与 H3 字段覆盖。

## 6. 判读要点（实机复测时如何读日志）

复测后查看新 `E:\Steam\steamapps\common\Unturned\BepInEx\LogOutput.log`，按以下顺序判别：

| 观察到 | 结论 |
|---|---|
| `[DEBUG-DRG] event=postfix-alive target=updateDraggedItem count=..` 与 `[DEBUG-SURF] event=postfix-alive target=PlayerUI.Update count=..` **都不出现** | **H4 坐实**：Harmony postfix 未在游戏内触发（管理面板 postfix 已证明 Harmony 本身工作 → 可能是 patch 目标方法签名/版本差异） |
| 只有 `postfix-alive` 出现，随后 `[DEBUG-SURF] event=poll-silent reason=no-active-session reason=disconnected` | **H3a**：`Provider.isConnected` 在单人读为 false |
| `reason=dashboard-closed` 反复出现 | **H3b**：`PlayerDashboardInventoryUI.active` 读为 false（面板其实开着） |
| `reason=tracker-inactive dashboardActive=True isStoring=False` 出现 | **H1a**：watcher 收到 dashboardActive 但会话未开（tracker/watcher 逻辑问题） |
| `[DEBUG-SURF] event=poll-silent reason=hierarchy-not-ready hierarchy=NotCreated/Incompatible page=.. hasSleekItems=..` | **H1b**：surface 反射/父链探针失败（`items[]` 未取到或 `scroll→grid→itemsPanel` 父链无效） |
| `[DEBUG-DRG] event=tick-silent/poll-silent reason=lifecycle-gate reason=blocked lifecycleCanRun=False` | **H2 坐实**：Lifecycle 掉出 Running（state=.. 提供具体值） |
| **出现 `surface-context-dispatched`** | H1/H3 排除，surface 正常 → 问题在坐标/渲染层，转 DEV-16E 视觉证据 |
| **出现 `drag-started`** | H2/H4 排除，拖拽驱动正常 → 问题在 preview 渲染 |

> 注意：节流为每 2 秒最多一条/点，故判别行是稀疏出现而非刷屏；复测时按 G 打开背包后**悬停拖拽数秒**再放下，确保判别行有机会输出。

## 7. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-01\artifacts\BetterUnturnedExperience-DIAG-R13SILENCE-20260901.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，并**重命名**为 `BetterUnturnedExperience.dll`（先删除/移走旧 BUE DLL——包括上次的 `BetterUnturnedExperience-1925.dll`）。
3. 核对哈希（期望 `577D57668796FBE41D31BA70D999900EBB4EAA32824253B455F62F64C58CB997`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 启动游戏 → 单人进图 → 按 G 打开背包 → 悬停拖拽一件物品数秒 → 按 R 一次 → 正常退出。
5. 重新导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回，我按第 6 节判读矩阵定位根因。

## 8. 边界声明

- 本 DLL 是**判别插桩**，非 DEV-16E 资格候选；其存在不改变"静态 CLEAN ≠ 玩法通过"的结论。
- 归档与交付指令在双轴 CLEAN 之后发出，符合 `real-machine-test-loop.md` 第 41-42 行边界。
- 旧正式件 `BetterUnturnedExperience-1925.dll`（sha256 `45D509...`）身份与证据不变，未受影响。
