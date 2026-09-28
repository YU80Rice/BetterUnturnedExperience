# V7-R6 自动旋转四向与图标朝向

- **Ticket**: V7-R6
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T6 的事实输入。对照自动旋转与预览图标朝向产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R6-upright-rotation.md`）。

必须回答（只查证不改码，结论带 file:line）：

1. `PlacementCandidateEvaluator` 如何从 `CurrentRotation` 得到 `rotatedRotation`；是否永远 `+1 mod 4`；感应带命中时会不会选到 `rot=2/3`。
2. 拾取进包时原版默认 `jar.rot` 是什么（U3-SDK `ItemJar` / `tryAddItem` / 地面拾取）；正方形与非正方形差异。
3. 预览图标写入：`PreviewIcon.Rotation` → `IVisualElement.RotationAngle` → `itemIcon.rot` / `image.RotationAngle = rot*90`；`isAngled` 的含义。`rot=1` 与 `rot=2` 在贴图上各是什么视觉。
4. 绿框 footprint 与图标宽高是否同一 `rot`；已知锚点/倒置缺陷（注释、实机报告、红测名）。
5. 手动 R 如何改 `dragFromRot`；自动旋转与手动 R 谁覆盖谁。
6. ADR 0003 / CONTEXT「自动旋转」词条与代码是否仍一致；哪些红测钉死「横放保持横 / 竖放到竖」因而会在收成 0/1 两向后红。

不要在本票改 evaluator。正向的产品定义留给 T6。

## Answer

报告：`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R6-upright-rotation.md`

1. `rotatedRotation = (CurrentRotation + 1) & 3`（`PlacementCandidateEvaluator.cs:10,32`）。永远 +1 mod 4。感应带采用同一值，从 `rot=1` 会出 `rot=2`。正方形不转（`:31,105`）。
2. 原版拾取：`Items.tryFindSpace` 先 `rot=0` 再 `rot=1`，从不产 2/3（U3-SDK `Items.cs:577-651`）。`ItemJar(Item)` 不显式写 rot（byte 默认 0）。正方形/非正方形同一套。
3. `PreviewIcon.Rotation` → `IVisualElement.RotationAngle` → `itemIcon.rot` / `image.RotationAngle = rot*90`。`isAngled` = 允许贴图旋转。`rot=1` = 90° 横正向；`rot=2` = 180° 竖倒置。
4. 绿框与图标同一 `preview.Candidate.Rotation`。已知：倒置被红测当成竖成功；rotgrab 已修；BUE 无原生对角补偿；双图标（原生跟 jar.rot、BUE 跟候选）。
5. 手动 R 改 `dragJar.rot++ %= 4`，**不改** `dragFromRot`。BUE 预览读 jar.rot；提交跟预览候选，覆盖 jar.rot。
6. ADR 0003 与代码仍一致。CONTEXT 0/1 是第七阶段目标，标明现网未裁。收成 0/1 后必红：`Placement.Tests` `Rotation == 2`。`symrot-red` / `edge-rot-red` / `corner-lift-red` 用 `2||0` 钉竖 footprint；`symrot-wide-red` 钉中部保持横 `rot=1`。
