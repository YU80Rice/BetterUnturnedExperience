# 交付报告 — DEV-16D-R13-R6-DIAG-R2 判别插桩（盲区修正轮）

> CaseId：`DEV-16D-R13-R6-DIAG-R2-20260901` · CandidateBuild：`DEV-16D-R13-R6-DIAG-R2-20260901`
> 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-r2-20260901.dll`
> SHA-256：`BE4470710A294D4479EA791A2C35EA4A4838C4C8BBBAABD35A7940B9301C3B46`（235520 bytes）
> 性质：**判别插桩 R2**，非 DEV-16E 资格候选。

## 1. 为什么有 R2（R1 实机结果驱动）

用户用 R1 判别 DLL（sha256 `577D...`）实机复测（会话 21:29，Client.log UTC 13:28-13:29 确认），新日志暴露：

| 观测 | 含义 |
|---|---|
| 判别 DLL 已部署（L15 sha256=`577D...`） | ✅ 部署正确 |
| **`[DEBUG-SURF]`/`[DEBUG-DRG]` 判别行零出现** | 盲区：R1 的 `postfix-alive` 计数放在 `adapter==null\|isolated` 门之后 → adapter 被隔离时计数不触发，无法区分"H4 补丁未触发"与"adapter 已死" |
| 管理面板 `host-ui-tick source=VanillaUiUpdate` 持续触发 | Harmony 对 PlayerUI.Update 的 patch 是活的 |
| `event=host-destroyed state=preserved patches-kept=true`（L42） | BepInEx 宿主清扫；`OnDestroy` 非退出分支执行 `pluginUpdateDriver.Clear()`（插件 Update 驱动断） |
| `inventory-events-subscribed` 仅一次（L58） | drag Poll 曾越过 LifecycleCanRun 门执行 |
| 零 `drag-started`、零 `surface-context-dispatched` | 功能驱动仍完全静默 |

**推导**：R1 的插桩有 4 个盲区——① postfix 计数在 null 门后（无法区分 H4 vs adapter-dead）；② `Tick()` 的 `!enabled||isolated` 门未插桩；③ `Tick()`/`InvokePollGuarded` catch 把异常写进 `LastPollDiagnostics` 但从不 LogInfo（异常被吞 → H5 不可见）；④ `staticLog` 缺失导致 static postfix 在 adapter null 时无日志通道。

## 2. R2 修正（关闭盲区）

| 盲区 | R2 修正 |
|---|---|
| ① postfix 计数在门后 | `++postfixTick` 移到 postfix 第一语句（任何 Harmony 调用必计数）；null/isolated 分支输出 `reason=adapter-null` / `reason=adapter-isolated`；新增 `DescribeAdapterGate(hasActiveAdapter, isolated)` / `DescribeAdapterGate(enabled, isolated)` seam |
| ② Tick 的 enabled/isolated 门 | `Tick()` 第一门输出 `[DEBUG-DRG] event=tick-silent reason=DescribeAdapterGate(...)` |
| ③ 异常被吞 | `DashboardUpdatePostfix` catch、drag `Tick` catch → `[DEBUG-DRG] event=tick-exception errorType=.. message=..`；`InvokePollGuarded` catch → `[DEBUG-SURF] event=poll-exception errorType=.. message=..`（随后仍 `IsolateAndDetach`，不改行为） |
| ④ static 日志通道 | 每 adapter 新增 `internal Log` 属性 + `staticLog` 静态字段，构造器赋值（插件 Logger）；static postfix/静态方法可用 |

## 3. TDD 红→绿

- 红测 `--dev16d-r13-silence-red` 扩展：引用两个新 seam `InventoryDragPreviewAdapter.DescribeAdapterGate(enabled,isolated)` 与 `InventorySurfaceLifecycleAdapter.DescribeAdapterGate(hasActiveAdapter,isolated)`。
- **红**：实现前编译失败（CS0117，退出码 1）。
- **绿**：实现后编译通过，运行时断言通过（退出码 0）。

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器 | 全 PASS |
| R13 定向测试（含 silence-red） | 10/10 全 PASS |
| UI/native token 扫描 | ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名，output-review-loop 第 3 步） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① 两个 `DescribeAdapterGate(bool,bool)` 同名不同语义；② drag postfix 硬编码 reason 未复用自身 seam；③ `LogSilenceOncePerTwoSeconds`/`staticLog`/计数块跨 adapter 复制；④ `DescribeTickGate` can-run/enhanced-active 分支不可达；⑤ `DescribeNoActiveSession` 五布尔参数；⑥ 两处 `event=tick-exception` 共用 `BUE-DIAG-DRG-004`；⑦ `Log` 访问器与 `staticLog` 冗余（Middle Man）；⑧ 测试注释写 CS1061 实为 CS0117 |
| **Spec** | **CLEAN** | 无 | `%120` 节流延迟首个 postfix-alive 至第 120 次调用（约 2s@60fps）——2 秒内窗口仍可能看似 H4；drag `DescribeAdapterGate` 硬编码字符串与 helper 不一致（外观性） |

> 关键验证（Spec 轴确认）：计数在每次 Harmony 调用第一语句执行 → 零 `postfix-alive` 且管理面板 postfix 存活 ⇒ **H4 坐实**；`reason=adapter-null/isolated` ⇒ adapter 死；`tick-exception`/`poll-exception` ⇒ H5 异常被吞被显形。`staticLog` 在 `Activate()` patch 前赋值，静态状态越过 host-destroyed 清扫存活。

## 6. 判读要点（R2 实机复测）

复测后查看 `E:\Steam\steamapps\common\Unturned\BepInEx\LogOutput.log`，按顺序判别：

| 观察到 | 结论 |
|---|---|
| `[DEBUG-SURF] event=postfix-alive ... reason=adapter-null` 出现 | PlayerUI.Update postfix **在触发**，但 lifecycle `ActiveAdapter` 为 null（被隔离/未激活）→ 查隔离来源 |
| `[DEBUG-SURF] event=postfix-alive ... reason=adapter-isolated` 出现 | postfix 触发、adapter 被隔离 |
| `[DEBUG-SURF] event=postfix-alive`（无 reason）出现 | postfix 触发且 adapter 活着 → 应随之有 poll-silent/dispatch |
| **SURF+DRG 都无 postfix-alive，但管理面板 host-ui-tick 仍在** | **H4 坐实**：BUE 的两条 Harmony patch 未触发（目标方法/签名与游戏版本不匹配） |
| `[DEBUG-DRG] event=tick-exception errorType=..` 或 `[DEBUG-SURF] event=poll-exception errorType=..` | **H5 坐实**：首次真实交互抛异常（异常类型/消息直接可见）→ 按异常定位修复 |
| `[DEBUG-DRG] event=tick-silent reason=adapter-gate reason=disabled/isolated` | drag adapter 自身门挡住 |
| 出现 `surface-context-dispatched` | surface 正常 → 转坐标/渲染层（DEV-16E 视觉证据） |
| 出现 `drag-started` | 拖拽驱动正常 → 问题在 preview 渲染 |

> 判读提示（Spec 轴）：`%120` 节流下，**至少按住背包悬停约 3 秒以上**再操作，确保首个 postfix-alive（约第 120 帧）与 poll-silent（2 秒窗口）都有机会输出；SURF 的 PlayerUI.Update 计数器是 H4 主信号。

## 7. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-01\artifacts\BetterUnturnedExperience-DIAG-R13SILENCE-r2-20260901.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，重命名为 `BetterUnturnedExperience.dll`（先删除/移走旧 BUE DLL，包括 R1 的 `...-DIAG-R13SILENCE-20260901.dll` 与 `...-1925.dll`）。
3. 核对哈希（期望 `BE4470710A294D4479EA791A2C35EA4A4838C4C8BBBAABD35A7940B9301C3B46`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 单人进图 → 按 G 打开背包 → **按住物品悬停 ≥3 秒** → 拖拽 → 按 R 一次 → 正常退出。
5. 导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回。

## 8. 边界声明

- 判别插桩 R2，非 DEV-16E 资格候选；归档与交付指令在双轴 CLEAN 之后发出（real-machine-test-loop.md 第 41-42 行）。
- R1 判别 DLL（`577D...`）与 R13 正式件（`45D509...`）身份不变，未受影响。
- R2 的 8 项可延后 smell（Standards）+ 2 项（Spec）已列名，DEV-16E 资格轮应消解（尤其跨 adapter 复用与 `%120` 节流）。
