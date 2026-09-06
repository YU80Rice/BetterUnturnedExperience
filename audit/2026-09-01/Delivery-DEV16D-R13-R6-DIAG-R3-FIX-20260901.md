# 交付报告 — DEV-16D-R13-R6-DIAG-R3-FIX 根因修复候选

> CaseId：`DEV-16D-R13-R6-DIAG-R3-FIX-20260901` · CandidateBuild：`DEV-16D-R13-R6-DIAG-R3-FIX-20260901`
> 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-r3-fix-20260901.dll`
> SHA-256：`7C8BEC31BD4B34829486E64EB570BC533A45224DD529D4E786A57B260F1A47D3`（235520 bytes）
> 性质：**判别插桩 + 根因修复候选**（scrollsize fail-retry）。仍保留 `[DEBUG-]` 判别日志以便本轮实机确认根因闭合；非 DEV-16E 资格候选，但首次承载**功能修复**。

## 1. 为什么有 R3（R2 实机结果驱动）

用户用 R2 判别 DLL（sha256 `BE4470...`）实机复测（会话 21:57，`UMM-诊断包_20260901_215746`），R2 关闭"异常被吞"盲区后，日志一举捕获根因：

```
L59: [DEBUG-SURF] event=poll-exception errorType=System.InvalidOperationException
     message=native inventory scroll viewport size is invalid  diagnosticId=BUE-DIAG-SURF-004
```

**根因链（全部回到源码）**：
1. 玩家进图 → dashboard 打开 → lifecycle Poll 开会话（`PlayerDashboardInventoryUI.active=true`）。
2. `BuildSurfaceContext(page)` → `nativeScroll.GetAbsoluteSize()`——**dashboard 刚 `AnimateIntoView()` 的第一帧，原生 `horizontalScrollView` 尚未布局完成，返回 0/NaN**。
3. 旧代码 `if (size<=0 || NaN) throw new InvalidOperationException("native inventory scroll viewport size is invalid")` → 异常冒泡到 `InvokePollGuarded` catch（R2 修正让它 LogInfo）→ **fail-closed 隔离**：`IsolateAndDispatch()` → `IsolateAndDetach()` → `ClearActive` → 两个 adapter 的 `ActiveAdapter=null` + `harmony.UnpatchSelf()`。
4. **隔离后一切静默** → 无 `surface-context-dispatched`、无 `drag-started`、无 `preview-evaluated` —— 正是用户观察到的"拿起物品时没有日志刷新"。

**一句话**：`BuildSurfaceContext` 把"瞬时布局未就绪"（scroll size=0/NaN）当致命异常处理，fail-closed 隔离在第一帧杀死整个功能。R13 静态测试全绿是因 `CreateNativeSurface` 直接喂合法 viewport，从未走真实 `GetAbsoluteSize()`。

## 2. R3 修复（fail-closed → fail-retry）

| 变更 | 位置 | 说明 |
|---|---|---|
| 新增纯 seam | `UnturnedInventorySurfaceContext.IsValidScrollViewportSize(Vector2)` | `x>0 && y>0 && IsFinite(x) && IsFinite(y)`；严格拒绝 0/NaN/±Inf（比旧内联检查更严，与 strict-geometry 测试一致） |
| 抛异常 → not-ready 返回 | `BuildSurfaceContext` scroll size 检查 | 无效尺寸 `log surface-not-ready reason=scroll-viewport-not-laid-out` + `return null` → `Poll()` 既有 `context==null→continue` 分支**下一帧重试**（布局完成即正常 dispatch） |
| 保留 `ResolveViewport` fail-closed 抛点 | L302 | 生产路径由 `BuildSurfaceContext` 前置校验拦截，不会触达；`AssertStrictNativeGeometryRejectsInvalidValues` 依赖其抛点，契约不变（防御纵深） |
| R2 判别日志保留 | 全部 | 本轮实机确认根因闭合后，DEV-16E 轮按计划清理 `[DEBUG-]` |

## 3. TDD 红→绿

- 红测 `--dev16d-r13-scrollsize-red` → `AssertDev16DR13ScrollViewportSizeSeam()` 引用尚不存在的 `IsValidScrollViewportSize` seam。
- **红**：实现前编译失败（CS0117 ×3，退出码 1）。
- **绿**：实现 seam + 修复后编译通过，断言通过（0→reject、NaN→reject、400×300→accept，退出码 0）。

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器 | 全 PASS |
| R13 定向测试（含 silence-red、scrollsize-red） | 11/11 全 PASS |
| UI/native token 扫描 | ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名，output-review-loop 第 3 步） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① `LogSilenceOncePerTwoSeconds`+`lastSilenceLogTick` 跨 adapter 逐字复制；② `DescribeAdapterGate` 两处同名不同签名/理由串；③ seam 重实现 `IsFinitePositive` 未组合复用；④ `DescribeNoActiveSession` 5 布尔参数（Primitive Obsession）；⑤ `%120`/`2000ms` 魔法数 |
| **Spec** | **CLEAN** | 无 | 隔离触发已移除（`BuildSurfaceContext` 无效尺寸 return null）；seam 语义正确；编译红锚定有效；`ResolveViewport` 保留抛点生产路径不可达（前置校验拦截）；无超出 throw→null 的原生行为改变 |

## 6. 判读要点（R3 实机复测）

**预期行为变化（本轮核心）**：按 G 打开背包后，第一帧因布局未就绪走 `surface-not-ready reason=scroll-viewport-not-laid-out`（不再隔离），**下一帧布局完成即 `surface-context-dispatched`** → 拖拽应出现绿/红预放置块与浮动物品图标 → 按 R 应自动旋转。

| 观察到 | 结论 |
|---|---|
| `surface-not-ready reason=scroll-viewport-not-laid-out` 出现 1-2 次，随后 **`surface-context-dispatched`** | ✅ **根因修复生效**：fail-retry 正常 |
| 随后 `drag-started` + `preview-visible state=Candidate` | 功能恢复，进入实机功能验收 |
| 只出现 `scroll-viewport-not-laid-out` 且**永无** `surface-context-dispatched` | 布局持续未就绪（scroll 尺寸恒 0）→ 不同根因（如反射拿错 scroll 实例），发回日志继续判别 |
| 出现 `poll-exception ... message=`（其他消息） | 新异常路径，按消息定位 |
| 全程无 `[DEBUG-]` 行但管理面板仍在 | H4 残余（patch 未触发）——已排除为本次根因 |

> 判读提示：按 G 打开背包后**等待 1-2 秒**（让布局完成、Poll 重试），再拿起物品拖拽，确保 fail-retry 有窗口。

## 7. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-01\artifacts\BetterUnturnedExperience-DIAG-R13SILENCE-r3-fix-20260901.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，重命名为 `BetterUnturnedExperience.dll`（**先删除旧 BUE DLL**：R2 `...-r2-...`、R1 `...-20260901.dll`、`...-1925.dll`）。
3. 核对哈希（期望 `7C8BEC31BD4B34829486E64EB570BC533A45224DD529D4E786A57B260F1A47D3`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 单人进图 → 按 G 打开背包 → **等待 1-2 秒** → 拿起物品拖拽悬停 → 按 R 一次 → 正常退出。
5. 导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回。

## 8. 边界声明

- 本 DLL 是**修复候选 + 判别插桩**：修复（scrollsize fail-retry）需实机确认功能真正出现；`[DEBUG-]` 日志按计划在 DEV-16E 资格轮清理。
- R2 判别 DLL（`BE4470...`）、R1 判别 DLL（`577D...`）、R13 正式件（`45D509...`）身份与证据不变。
- 归档与交付指令在双轴 CLEAN 之后发出（real-machine-test-loop.md 第 41-42 行）。
