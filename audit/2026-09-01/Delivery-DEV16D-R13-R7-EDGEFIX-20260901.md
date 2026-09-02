# 交付报告 — DEV-16D-R13-R7-EDGEFIX 自动旋转边缘贴边修复候选

> CaseId：`DEV-16D-R13-R7-EDGEFIX-20260901` · CandidateBuild：`DEV-16D-R13-R7-EDGEFIX-20260901`
> 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-r4-edgefix-20260901.dll`
> SHA-256：`3AF8DCABBC2467ACD82A809966EEA6E8D9ABF650F65FC7ED1601219C1B211E1F`（236032 bytes）
> 源码快照：`e45bc98`（edge-rot 实现提交，位于 `7c4a987` 冻结之上）
> 性质：**自动旋转边缘贴边修复候选**（ADR-0003 方案 A），仍保留 `[DEBUG-]` 判别日志以便实机确认；非 DEV-16E 资格候选。

## 1. 工单背景（DEV-16D-R13-R7）

R4 实机复测暴露：**武士刀（1×3）"竖→横"自动旋转成功后，重新拿起横放的武士刀拖回"竖着的位置"时，预览不再自动转回竖，必须手动按 R**。R4 日志确认真实容器是宽容器（背包 5×7 / 后备箱 6×3）。

**根因**（上一轮冻结已确认）：宽容器中横放（3×1）几乎任何位置局部都能放下，Local-Fit Priority 阶梯①（"局部投影能放当前方向时立即返回，绝不检查旋转"）永远命中，横武士刀被**永久困在横放**——仅在"横放也放不下"的窄缝才触发阶梯②，而宽容器里这种窄缝几乎不存在。

**决议**（`docs/adr/0003-bue-auto-rotation-edge-fit-decision.md`，commit `5b77190` + Revision 方案 A）：当光标位于**空位区域边缘**（容器最边一列/最上一行，或被障碍挡出的空位边界）时，即使当前方向（横）也能放下，也允许自动旋转使**长边顺着边缘方向贴边**；宽容器**空旷中部**仍保持当前方向（D2 防蠕动边界不变）。

## 2. R7 修复内容（edge-rot）

| 变更 | 位置 | 说明 |
|---|---|---|
| rotated 候选前置计算 | `PlacementCandidateEvaluator.Evaluate` | 阶梯①（当前方向能放立即返回）前先算 rotated 投影；step 1 分支先检查 edge-rot 条件 |
| edge-rot 分支 | step 1 内 | 当 **rotated 能放 且 rotated 长边贴边 且 当前方向长边不贴边** 时，返回 rotated 候选；否则保持当前方向 |
| 新增纯 helper | `LongSideHugsEdge` / `RowFullyBlocked` / `ColumnFullyBlocked` | 横长边贴顶/底边界或上方/下方整行被占；竖长边贴左/右边界或左/右侧整列被占；纯静态、零分配 |
| D2 防蠕动保持 | 空旷中部 | 两者都不贴边（或都贴边）时返回当前方向；step 2 / Search 阶梯不变 |

**判定规则**（方案 A 精确语义）：
- **横长边**（width ≥ height）：`y==0`（贴上）或 `y+height>=Height`（贴下），或上方/下方整行全被占（障碍挡出的横边界）；
- **竖长边**（height > width）：`x==0`（贴左）或 `x+width>=Width`（贴右），或左/右侧整列全被占（障碍挡出的竖边界）；
- **edge-rot 触发**：仅当 rotated 贴边且当前方向不贴边时才翻转——空旷中部两者都不贴边 → 走原阶梯①（保持横），**不引入独立翻转源**。

## 3. 红测 → 绿（TDD）

| 红测 | 冻结时状态 | 实现后状态 |
|---|---|---|
| `--dev16d-r13-edge-rot-red`（左边界 0.4,1.5 + 右边界 5.6,1.5，6×3 空容器横武士刀拖到边缘） | 🔴 红（返回横 3×1） | ✅ 绿（转竖 1×3，长边贴边） |
| `--dev16d-r13-symrot-red`（3×3 窄缝转竖） | ✅ 绿 | ✅ 绿（不回归） |
| `--dev16d-r13-symrot-wide-red`（D2 守卫：宽容器空旷中部保持横） | ✅ 绿 | ✅ 绿（edge-rot 不引入空旷中部翻转） |

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器 | 全 PASS |
| R13 定向测试（含 edge-rot/symrot 3 项） | 14/14 全 PASS |
| UI/native token 扫描 | ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项 |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① 生产 evaluator 文件此前未纳入 git 跟踪（本次 `e45bc98` 已将其加入，`create mode 100644`）；② edge-rot 右边界测试块与左边界镜像（~9 行重复，可折叠为 `AssertEdgeRot(cursorX, edgeName)`，清晰度权衡可接受） |
| **Spec** | **CLEAN** | 无 | 障碍挡出的行列边界路径（`RowFullyBlocked`/`ColumnFullyBlocked`）已实现但本轮红测未直接覆盖（容器边界路径已覆盖；Spec 轴 traced 确认逻辑正确）；evaluator 未跟踪文件需在归档前提交（已由 `e45bc98` 完成） |

> Spec 轴关键验证（逐点 trace 确认）：左边界 `(0,0)` 竖放 `x==0` 贴边 → edge-rot 触发；右边界 `(5,0)` `x+width>=6` 贴边 → 触发；空旷中部 `(2,0)` 竖放不贴边 → D2 保持横；窄缝场景当前方向放不下 → step 2 转竖（不回归）。四个行为验证点全部正确。

## 6. 判读要点（R4/edgefix 实机复测）

**预期行为变化（本轮核心）**：在背包/后备箱里，把横放的武士刀拖到**容器最左列/最右列**（或障碍挡出的空位边界），预览应自动转回竖（长边贴边）；拖到空旷中部则保持横。

| 观察到 | 结论 |
|---|---|
| 横武士刀拖到最左列/最右列 → 预览转竖（绿框竖放、长边贴边） | ✅ **edge-rot 生效** |
| 拖到空旷中部 → 保持横（不转竖） | ✅ D2 防蠕动正确 |
| 松手放下后物品方向 = 预览方向（竖） | ✅ 提交跟随（sendDragItem 用预览 rot） |
| 3×3 窄缝（右上角 2×2 挡出竖列）→ 横拖入自动转竖 | ✅ 窄缝转竖不回归（symrot-red） |
| 全无转竖、无 surface 日志 | 回归 R6/R7 之前的根因（检查 `surface-not-ready`/`poll-exception`） |

> 判读提示：复测时按 G 打开背包 → 拿起横放的武士刀 → 分别拖到**最左列、最右列、空旷中部、一个 2×2 挡出的竖列**四个位置，观察预览方向是否如上述矩阵；松手确认方向一致；最后按 R 一次确认手动旋转仍正常。

## 7. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-01\artifacts\BetterUnturnedExperience-DIAG-R13SILENCE-r4-edgefix-20260901.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，重命名为 `BetterUnturnedExperience.dll`（**先删除旧 BUE DLL**：R3 `...-r3-fix-...`、R2 `...-r2-...`、R1 `...-20260901.dll`、`...-1925.dll`）。
3. 核对哈希（期望 `3AF8DCABBC2467ACD82A809966EEA6E8D9ABF650F65FC7ED1601219C1B211E1F`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 单人进图 → 按 G 打开背包 → 拿起横放的武士刀 → 按上面判读矩阵四个位置拖拽观察 → 松手确认方向一致 → 按 R 确认手动旋转 → 正常退出。
5. 导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回。

## 8. 边界声明

- 本 DLL 是 **edge-rot 修复候选 + 判别插桩**；修复需实机确认"拖到边缘自动转竖、空旷中部保持横、松手方向一致"；`[DEBUG-]` 日志按计划在 DEV-16E 资格轮清理。
- 源码已提交 `e45bc98`（evaluator 首次纳入 git，满足审查"归档 DLL 对应可审查源码"要求）；工单 DEV-16D-R13-R7 状态在实现完成后更新为 resolved。
- 归档与交付指令在双轴 CLEAN 之后发出（real-machine-test-loop.md 第 41-42 行）。
