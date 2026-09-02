# 交付报告 — DEV-16D-R13-R7-ROTGRAB 当前空间 grabOffset 修复候选

> CaseId：`DEV-16D-R13-R7-ROTGRAB-20260902` · CandidateBuild：`DEV-16D-R13-R7-ROTGRAB-20260902`
> 归档 DLL：`audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`
> SHA-256：`6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`（237056 bytes）
> 源码快照：`61737df`（rotgrab 修复提交，位于 `5152867` 之上）
> 性质：**横放物品重抓后无预放置渲染修复候选**（spec §2 grabOffset 坐标系契约修正），仍保留 `[DEBUG-]` 判别日志；非 DEV-16E 资格候选。

## 1. 工单背景（R6 实机反馈驱动）

R7-BAND（r5-band `CC8BC4...`）实机复测（`UMM-诊断包_20260902_124432`）确认边缘感应带/引力翻转与开阔区不蠕动**均生效**（00:08-00:09），但暴露新异常：

**已横放物品（非方形，如武士刀 1×3）再次抓取时，强化预放置渲染（绿色/红色候选框 + 浮动图标）完全消失**；必须按 R 转回竖向后渲染立即恢复（00:12-00:17）。背包（5×7）、后备箱（6×3）、木质储物箱 100% 稳定复现。

## 2. 根因（已证实）

**grabOffset 坐标系契约不匹配**（日志 gen 3 vs gen 5 对照）：

| 项 | gen 3（横武士刀 rot=1） | gen 5（竖武士刀 rot=0） |
|---|---|---|
| grabOffset（日志） | 1.48, 0.2 | 0.6, 1.36 |
| 状态 | `state=Hidden reason=None` | `Candidate` |
| 结论 | 无预放置渲染 | 正常渲染 |

- spec §2（`Item-Placement-Algorithm-Spec.md` L16-23）定义 `grabOffsetInFootprint` 为**当前旋转 footprint 空间**；adapter 读原生 `dragPivot`，而原生 `updatePivot()`（U3-SDK L2381-2402）已按当前 `dragJar.rot` 变换过——**adapter 交付的 grabOffset 本就是当前空间**。
- 但 `TryCreateCandidateInput` / `TryGetIconScreenPosition` / `TryGetNativeIconPlacement` 把该值当 **base（rot0）坐标系**输入 `TryRotateGrabOffset(baseWidth, baseHeight, ...)`：横武士刀 `baseGrabX=1.48 > baseWidth=1`（L322 边界校验）→ 返回 false → presenter 持续 `HidePreview()` → 无渲染。
- 竖武士刀 rot=0 时 base==current 碰巧一致，故从未暴露。

## 3. R7-ROTGRAB 修复内容（当前空间契约）

| 变更 | 位置 | 说明 |
|---|---|---|
| `TryCreateCandidateInput` | `InventoryPreviewWiring.cs` | footprint width/height 由 `(rotation & 1)` 直接推导；grabOffset **按给定值使用**（当前空间）；按当前 footprint 闭区间校验（`grabX > width` 拒绝，相等允许，符合 spec §2） |
| `TryGetIconScreenPosition` | 同文件 | 从**当前 footprint 尺寸**出发，按 `delta = (targetRotation - currentRotation) & 3` 相对旋转；`target==current`（常见实时拖拽）为 no-op |
| `TryGetNativeIconPlacement` | 同文件 | 同上相对旋转修复（浮动图标锚点） |
| 红测 | `Program.cs` | 新增 `--dev16d-r13-rotgrab-red`：横武士刀 rot=1 grab (1.48,0.2) 必须到达 candidate seam（修复前红 exit 1 / 修复后绿 exit 0，stash 循环验证） |
| ClientUi 三测试 | `Dev15BTests.cs` | 期望值更新为 spec §2 当前空间数学（原编码 base 帧假设）：`UsesRotatedFootprintCenterForCurrentRotation`→(3.25,4.75)、`AppliesForwardGrabOffsetRotation`→(3.25,3.75)、`AnchorsIconUsingRotatedGrabOffset`→(32.5,37.5) |

## 4. 红测 → 绿（TDD）

| 红测 | 修复前 | 修复后 |
|---|---|---|
| `--dev16d-r13-rotgrab-red`（横武士刀 rot=1 当前空间 grab 到达 candidate seam） | 🔴 红（`1.48 > baseWidth=1` → false） | ✅ 绿 |
| 既有 R13 自动旋转四测试（symrot-red / symrot-wide-red / edge-rot-red / corner-lift-red） | ✅ 绿 | ✅ 绿（spec §11 无回归） |

## 5. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器 | 全 PASS |
| R13 定向测试（含 rotgrab 新增） | 16/16 全 PASS |
| UI/native token 扫描 | ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 6. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① 偶数/奇数 footprint 推导在 3 处重复（可提取零分配 out-param helper）；② `TryRotateGrabOffset` 的 `base*` 参数名已过时（现接收当前 footprint）；③ 3 处重复 R13 注释散文（可合并为共享说明） |
| **Spec** | **CLEAN** | 无 | 方法名 `AppliesForwardGrabOffsetRotation` 已过时（新期望验证的是"当前空间 grab 不重新 forward 旋转"） |

> Spec 轴逐点验证：spec §2 L22"算法不得接收或重算 grab offset"现在逐字实现 ✓；`delta & 3` 字节数学正确（target0−current1=−1&3=3）✓；闭区间边界 ✓；零分配保留（§7）✓；四个 R13 自动旋转测试直接调用 evaluator、不经 adapter，无 §11 回归 ✓；rot-0 既有测试路径不变 ✓。

## 7. 判读要点（R7-ROTGRAB 实机复测）

**预期行为变化（本轮核心）**：已横放的武士刀（或任何非方形物品）再次抓取时——
- **立即出现绿色/红色预放置候选框 + 浮动图标**（与竖着拿起完全一致）；
- 边缘感应带引力、开阔区不蠕动等 R7-BAND 既有行为**保持不变**。

| 观察到 | 结论 |
|---|---|
| 横放武士刀拿起即有绿色候选框/浮动图标 | ✅ rotgrab 修复生效 |
| 按 R 转竖后渲染仍正常 | ✅ 竖放路径无回归 |
| 横/竖拿起渲染表现一致 | ✅ 当前空间契约正确 |
| 背包/后备箱/储物箱均一致 | ✅ 跨容器稳定 |

> 判读提示：复测时按 G 打开背包 → 先横放一把武士刀 → 再抓起它，确认**抓起瞬间即有强化渲染**；然后按 R 转竖确认仍正常；最后在 3 个容器各试一次。

## 8. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-02\BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，重命名为 `BetterUnturnedExperience.dll`（**先删除旧 BUE DLL**：R5 `...-r5-band-...`、R4 `...-r4-edgefix-...`、R3/R2/R1 与 `...-1925.dll`）。
3. 核对哈希（期望 `6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 单人进图 → 按 G 打开背包 → 按上面判读矩阵复测 → 正常退出。
5. 导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回。

## 9. 边界声明

- 本 DLL 是 **rotgrab 修复候选 + 判别插桩**；修复需实机确认"横放物品拿起即有强化渲染、竖放路径无回归、三容器一致"；`[DEBUG-]` 日志按计划在 DEV-16E 资格轮清理。
- 源码已提交 `61737df`；上一轮决议冻结 `135be62`/`576cbed` 不受影响（spec §11 边缘感应带行为无回归）。
- 归档与交付指令在双轴 CLEAN 之后发出（real-machine-test-loop.md 第 41-42 行）。
- 可延后项（Standards 3 项 + Spec 1 项）已列名，DEV-16E 资格轮消解（提取 footprint 推导 helper、重命名 `base*` 参数、合并注释、重命名过时测试方法）。
