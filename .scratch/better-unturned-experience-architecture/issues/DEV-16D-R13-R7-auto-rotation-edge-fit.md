# DEV-16D-R13-R7: 自动旋转空位边缘贴边（Auto-Rotation Edge-Fit）

Type: task
Status: resolved
Blocked by: None（阻塞边已于 2026-09-01 由用户确认为 (b)，见下）

## 父工单 / 背景

- 冻结决议：`docs/adr/0003-bue-auto-rotation-edge-fit-decision.md`（2026-09-01 人工开发者决议，commit `5b77190`）
- 关联 spec：`.scratch/better-unturned-experience-architecture/issues/12-item-placement-algorithm.md`（Local-Fit Priority 冻结 spec，Comments 区已追加 2026-09-01 决议记录）
- 实机背景：R3 修复候选（scrollsize fail-retry）已让强化渲染层恢复；R4 实机复测暴露"横武士刀（1×3）拖回竖位不自动转回竖，必须手动 R"。

## 阻塞边（已确认）

**实机容器类型事实**：用户 2026-09-01 确认测试时使用的是 **(b) 宽容器**——背包（5×7）与车辆后备箱（6×3）内的"心理窄缝"（用 2×2 等物品摆出的窄缝，横放总能放下）。因此 edge-rot 是正确的修复方向：宽容器空旷处保持横（D2），空位区域边缘长边贴边（D6/D7）。该事实已验证，不再阻塞。

## What to build

让"更好的物品交互"的自动旋转与玩家直觉对称：当玩家拿起一个已按横放摆放的非方形物品（如武士刀 1×3），把它拖到**空位区域边缘**（被物品/障碍挡出的可用连续空位边界，非容器物理边界）且**当前方向在该处放不下**时，预览应自动旋转使**长边顺着边缘方向贴边**；宽容器空旷处仍保持当前方向（不蠕动）。松手提交沿用预览 rot（原生 `sendDragItem`），手动 R 仍是随时覆盖。

## Acceptance criteria

- [ ] 红测 `--dev16d-r13-edge-rot-red`（方案 A，已确认）：构造空位区域边缘场景（宽容器内障碍挡出的可用连续空位边界，或容器最边一列/最上一行），断言"光标位于空位边缘时，**即使当前方向（横）也能放下**，也自动旋转为长边贴边（竖放）"；该测试在修复前红、修复后绿。
- [ ] 红测 `--dev16d-r13-symrot-red` 保持绿（窄缝转竖不回归）。
- [ ] 红测 `--dev16d-r13-symrot-wide-red` 保持绿：宽容器**空旷中部**横放能放下 → 保持横（D2 防蠕动守卫，不因 edge-rot 引入空旷中部翻转）。
- [ ] `PlacementCandidateEvaluator` 保持无状态、热路径零分配；edge-rot 判定只在光标位于空位区域边缘时覆盖阶梯①的"当前方向能放立即返回"，空旷中部仍走原阶梯（D7 防蠕动边界不变）。
- [ ] Release 构建 0 errors / 0 warnings；七项目测试运行器全 PASS；R13 定向测试（含新增 edge-rot-red）全 PASS；UI/native token 扫描零命中；`git diff --check` 通过。
- [ ] 双轴独立审查（Standards + Spec）CLEAN 后归档：生成判别+修复 DLL，写交付报告（含判读矩阵与实机复测指令），按 `docs/agents/real-machine-test-loop.md` 第 41-42 行在 CLEAN 后交付。

## Comments

### 2026-09-01 拆单记录

- 决议经 `/grill-me` 多轮访谈冻结（Q1/Q3/Q5/Q7/Q9/Q10/Q11/Q12/Q13），设计树 D1-D8 见 ADR-0003。
- 三个红测锚点：`--dev16d-r13-symrot-red`（窄缝转竖，当前绿）、`--dev16d-r13-symrot-wide-red`（宽容器保持横，当前红=复现用户问题）、`--dev16d-r13-edge-rot-red`（空位边缘长边贴边，本工单新增）。
- 阻塞边（实机容器类型事实）待用户确认后从本工单移除或改写为已验证事实。

### 2026-09-01 阶段冻结（TDD 红已确认，准备进入实现）

- 人工开发者确认容器类型为 **(b) 宽容器**（背包 5×7 / 后备箱 6×3 内心理窄缝），阻塞边解除，工单状态 `claimed`。
- 人工开发者确认 edge-rot 触发语义为 **方案 A**（ADR-0003 Revision 段）：光标位于空位区域边缘时，即使当前方向（横）也能放下，也旋转使长边贴边；宽容器空旷中部保持横（D2 防蠕动边界不变）。
- 红测状态（本阶段冻结点，已实际运行确认）：
  - `--dev16d-r13-symrot-red` → **绿**（exit 0）：3×3 窄缝横武士刀拖回竖列自动转竖，不回归。
  - `--dev16d-r13-symrot-wide-red` → **绿**（exit 0）：宽容器空旷中部横放保持横，D2 守卫通过。
  - `--dev16d-r13-edge-rot-red` → **红**（exit 1，断言 "edge-rot: horizontal katana at the left edge auto-rotates to a vertical footprint"）：精确复现用户实机 bug（宽容器最左列边缘，横武士刀拖回竖位不转竖）。
- **下一步（下一阶段）**：在 `PlacementCandidateEvaluator` 实现 edge-rot（空位边缘检测 + 长边贴边方向选择），使 edge-rot-red 转绿；随后按 output-review-loop 全量验证 + 双轴独立审查 CLEAN 后归档交付。

## Answer

已实现并交付（2026-09-01）：
- 实现提交 `e45bc98`：`PlacementCandidateEvaluator` 新增 edge-rot（rotated 前置计算 + step 1 内 edge-rot 分支 + `LongSideHugsEdge`/`RowFullyBlocked`/`ColumnFullyBlocked` 纯 helper），D2 防蠕动边界保持（空旷中部两者都不贴边 → 走原阶梯①）。
- 红测状态：`--dev16d-r13-edge-rot-red`（左/右边界）红→绿；`--dev16d-r13-symrot-red`（窄缝）、`--dev16d-r13-symrot-wide-red`（D2 守卫）保持绿。
- 全量验证：Release 0/0、七项目全 PASS、R13 定向 14/14、UI token 0、diff-check 0。
- 双轴独立审查：Standards CLEAN / Spec CLEAN（可延后项：右边界测试镜像块可折叠；障碍挡出边界路径已实现但本轮红测未直接覆盖——Spec 轴 traced 确认逻辑正确）。
- 交付物：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-r4-edgefix-20260901.dll`（sha256 `3AF8DCABBC2467ACD82A809966EEA6E8D9ABF650F65FC7ED1601219C1B211E1F`），交付报告 `audit/2026-09-01/Delivery-DEV16D-R13-R7-EDGEFIX-20260901.md`，哈希记录 `audit/2026-09-01/r7-edgefix-dll-sha256.txt`。
- 实机复测判读矩阵见交付报告 §6；待用户实机确认后关闭 R7 支线并进入 DEV-16E 资格轮（届时清理 `[DEBUG-]`）。

### 2026-09-02 决议修订（R5 实机反馈 → edge-rot 升级为边缘感应带）

R5 实机（`UMM-诊断包_20260902_085838`，部署 R4 edgefix `3AF8DC...`）复现：单列 `LongSideHugsEdge` 触发过窄、横武士刀经 edge-rot 转横后重抓（`dragJar.rot=1`）使阶梯①在开阔区永久锁死横向（"单向粘滞"）。经 `/grill-with-docs` 拍板（Q-A~Q-D + Q1-Q6，完整决议见 `docs/adr/0003-bue-auto-rotation-edge-fit-decision.md` Revision 2026-09-02）：

1. 拒绝全局 BaseRotation 记忆（几何/边缘引力驱动）；
2. edge-rot 升级为**边缘感应带**：带宽 `band(dim)=clamp(1.0, dim*0.15, 2.0)`，按轴独立（竖向带用 `containerWidth`，横向带用 `containerHeight`）；
3. 角落重叠区保持当前姿态，重叠区外平滑接管；
4. 光标坐标触发 + 物理容纳守卫（`rotatedFitsGrid && Fits`）；
5. 提交持久化保持（松手用预览 `Candidate.Rotation`）；
6. **D2 红线保持**：开阔正中部严格保持当前方向，`--dev16d-r13-symrot-wide-red` 断言**不翻转**；
7. 障碍边界引力保留（前序物品充当"人造侧壁"并排竖放）。

**红测清单（修订后）**：
- `--dev16d-r13-symrot-red`（窄缝转竖）保持绿；
- `--dev16d-r13-symrot-wide-red`（D2 守卫：开阔中部保持横）保持断言不翻转；
- `--dev16d-r13-edge-rot-red`（左/右壁感应带转竖）保留并扩为感应带断言；
- **新增** `--dev16d-r13-corner-lift-red`（左下角横武器上提离开底带进入左带 → 转竖贴左壁）。

**下一步**：实现边缘感应带（光标坐标判定 + 容纳守卫），使 `corner-lift-red` 红→绿、`edge-rot-red` 扩带后保持绿；随后 output-review-loop 全量验证 + 双轴审查 CLEAN 后归档交付。

## Answer（R7-BAND 2026-09-02）

已实现并交付：
- 决议冻结 `135be62`：CONTEXT 词汇（边缘感应带/边缘引力/开阔中部保持方向）+ ADR-0003 Rev 2026-09-02 + spec §11 + 工单重开（claimed）。
- 实现提交 `576cbed`：`PlacementCandidateEvaluator` 新增 `TryEdgeBandCandidate`（光标坐标触发、band=clamp(1.0,dim*0.15,2.0) 按轴独立、角落重叠保持、物理容纳守卫、D2 开阔中部保持横）+ `BandForDimension`/`ProjectAxis` helper；Q6 障碍边界引力保留为 fallback。
- 红测状态：`--dev16d-r13-corner-lift-red`（左下角保持/上提转竖/下拉回横/13×13 左带 X==0）红→绿；`--dev16d-r13-edge-rot-red`（扩为感应带）、`--dev16d-r13-symrot-red`（窄缝）、`--dev16d-r13-symrot-wide-red`（D2 守卫，断言未翻转）保持绿。
- 全量验证：Release 0/0、七项目全 PASS（含 Placement.Tests 零分配）、R13 定向 15/15、UI token 0、diff-check 0。
- 双轴独立审查：Standards CLEAN / Spec CLEAN（可延后项：`rotation` 参数未用、`ProjectAxis` 与 `Project` 重复、测试红声明注释校正——DEV-16E 轮消解）。
- 交付物：`audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r5-band-20260902.dll`（sha256 `CC8BC4831AF9F5CE78BFACEC1797655E83AF748FF18A5790448ADE04B6470296`），交付报告 `audit/2026-09-02/Delivery-DEV16D-R13-R7-BAND-20260902.md`，哈希记录 `audit/2026-09-02/r7-band-dll-sha256.txt`。
- 实机复测判读矩阵见交付报告 §6；待用户实机确认后关闭 R7 支线并进入 DEV-16E 资格轮（届时清理 `[DEBUG-]`）。
