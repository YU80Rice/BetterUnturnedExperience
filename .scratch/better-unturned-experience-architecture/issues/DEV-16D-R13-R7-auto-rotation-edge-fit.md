# DEV-16D-R13-R7: 自动旋转空位边缘贴边（Auto-Rotation Edge-Fit）

Type: task
Status: claimed
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
