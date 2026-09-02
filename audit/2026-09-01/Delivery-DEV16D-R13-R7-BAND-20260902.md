# 交付报告 — DEV-16D-R13-R7-BAND 边缘感应带自动旋转修复候选

> CaseId：`DEV-16D-R13-R7-BAND-20260902` · CandidateBuild：`DEV-16D-R13-R7-BAND-20260902`
> 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-DIAG-R13SILENCE-r5-band-20260902.dll`
> SHA-256：`CC8BC4831AF9F5CE78BFACEC1797655E83AF748FF18A5790448ADE04B6470296`（237056 bytes）
> 源码快照：`576cbed`（实现提交，位于决议冻结 `135be62` 之上）
> 性质：**边缘感应带自动旋转修复候选**（ADR-0003 Rev 2026-09-02 / spec §11），仍保留 `[DEBUG-]` 判别日志；非 DEV-16E 资格候选。

## 1. 工单背景（R5 实机反馈驱动）

R4 edgefix DLL（`3AF8DC...`）实机复测（`UMM-诊断包_20260902_085838`）暴露两个问题：
1. **单列触发过窄**：`LongSideHugsEdge` 仅 `x==0`/`y==0` 单列触发，鼠标向内偏 1 格即失效；
2. **单向粘滞**：横武士刀经 edge-rot 转横、重抓后 `dragJar.rot=1` 使阶梯①在开阔区永久锁死横向（"往上一提立不起来"）。

经前端交互实测与三角洲收纳机制对比，人工开发者经 `/grill-with-docs` 拍板（D-A~D-D + Q1-Q6），冻结决议见 `docs/adr/0003-bue-auto-rotation-edge-fit-decision.md` Revision 2026-09-02 与 `Item-Placement-Algorithm-Spec.md` §11。

## 2. R7-BAND 修复内容（边缘感应带）

| 变更 | 位置 | 说明 |
|---|---|---|
| `TryEdgeBandCandidate` | `PlacementCandidateEvaluator` step 1 内 | 光标坐标触发：竖向带（左/右壁）倾向竖长边贴壁，横向带（上/下壁）倾向横长边贴壁 |
| `BandForDimension` | helper | `band(dim)=clamp(1.0, dim*0.15, 2.0)`，按轴独立（竖向带用 `containerWidth`，横向带用 `containerHeight`） |
| `ProjectAxis` | helper | 单轴投影 + 边缘 clamp |
| 角落裁决 | 前置守卫 | `verticalBand && horizontalBand` → 保持进入姿态（防抖动）；重叠区外平滑接管（"往上一提立起，往下一拉躺平"） |
| 物理容纳守卫 | band 内 | `rotatedFitsGrid` + bounds + `Fits(x,y,rotatedW,rotatedH)`——放不下绝不盲目翻转 |
| D2 红线 | `else return false` | 开阔中部（远离任何带）严格保持当前方向，不引入蠕动 |
| Q6 障碍引力 | 保留 fallback | `LongSideHugsEdge`/`RowFullyBlocked`/`ColumnFullyBlocked`——前序物品充当"人造侧壁"并排竖放 |

## 3. 红测 → 绿（TDD）

| 红测 | 修复前 | 修复后 |
|---|---|---|
| `--dev16d-r13-corner-lift-red`（左下角保持横 → 上提转竖贴左壁 → 下拉回横贴底；13×13 左带 X==0） | 🔴 红（大容器带内投影不贴边时返回横） | ✅ 绿 |
| `--dev16d-r13-edge-rot-red`（扩为感应带：0.9/5.1 带内偏 1 格仍转竖） | ✅ 绿（投影恰落壁） | ✅ 绿 |
| `--dev16d-r13-symrot-red`（窄缝转竖） | ✅ 绿 | ✅ 绿 |
| `--dev16d-r13-symrot-wide-red`（D2 守卫：开阔中部保持横，**断言未翻转**） | ✅ 绿 | ✅ 绿 |

## 4. 验证矩阵（全部通过）

| 项 | 结果 |
|---|---|
| Release 构建 | 0 errors / 0 warnings，退出码 0 |
| 七项目测试运行器（含 Placement.Tests 零分配断言） | 全 PASS |
| R13 定向测试（含 corner-lift 新增） | 15/15 全 PASS |
| UI/native token 扫描 | ClientUi 零命中 |
| `git diff --check` | 退出码 0 |

## 5. 双轴独立审查（并行子代理，互不可见）

| 轴 | 判定 | 阻断项 | 可延后项（已列名） |
|---|---|---|---|
| **Standards** | **CLEAN** | 无 | ① `TryEdgeBandCandidate` 的 `rotation` 参数未使用；② `ProjectAxis` 与 `Project` 单轴逻辑重复；③ 测试注释"红"声明过度——仅 13×13 X==0 断言对旧代码真红，6×3 内偏与 corner lift/pull 旧实现也过（投影恰落壁） |
| **Spec** | **CLEAN** | 无 | `rotation` 参数未用；横向带仅对"rotated 宽"生效（已宽的 katana 在横带返回 false 保持——符合 spec"倾向"/D2 语义） |

> Spec 轴逐点验证 §11 全部 7 项契约：光标坐标触发 ✓、band 公式精确 ✓、贴壁定位 ✓、角落保持 ✓、D2 开阔中部保持 ✓、障碍引力保留 ✓、红测锚定 ✓。物理容纳守卫 ✓。

## 6. 判读要点（R7-BAND 实机复测）

**预期行为变化（本轮核心）**：横武士刀在宽容器（背包/后备箱）中——
- **开阔中部**：严格保持横（D2，与你拍板一致）；
- **左下角横贴底边，鼠标上提**（离开底部带、进入左壁带）→ **顺畅翻转为竖长边贴左壁**（"往上一提立起"）；
- **下拉回纯底带** → 切回横贴底（"往下一拉躺平"）；
- **靠近左/右壁（含带内偏 1 格）** → 转竖贴壁；
- 沿已立起的武器右侧拖动 → 前序武器充当"人造侧壁"，第 2/3 把可并排竖放。

| 观察到 | 结论 |
|---|---|
| 左下角上提 → 转竖贴左壁 | ✅ 边缘感应带生效 |
| 下拉回底带 → 转横贴底 | ✅ 角落裁决 + 平滑接管 |
| 开阔中部横放保持横 | ✅ D2 红线正确（不蠕动） |
| 松手方向 = 预览方向 | ✅ 提交跟随（WYSIWYG） |
| 沿前序武器拖动并排竖放 | ✅ 障碍边界引力 |

> 判读提示：复测时按 G 打开背包 → 拿起横武士刀 → 拖到**左下角** → 上下移动鼠标观察"提立/拉躺"；再拖到**左壁带内偏 1 格**确认转竖；拖到**开阔中部**确认保持横；最后沿一把已竖立的武器右侧拖动确认并排。

## 7. 部署与复测（现在可以开始）

1. **完全退出 Unturned。**
2. 复制 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\audit\2026-09-01\artifacts\BetterUnturnedExperience-DIAG-R13SILENCE-r5-band-20260902.dll`
   到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`，重命名为 `BetterUnturnedExperience.dll`（**先删除旧 BUE DLL**：R4 `...-r4-edgefix-...`、R3 `...-r3-fix-...`、R2/R1 与 `...-1925.dll`）。
3. 核对哈希（期望 `CC8BC4831AF9F5CE78BFACEC1797655E83AF748FF18A5790448ADE04B6470296`）：
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```
4. 单人进图 → 按 G 打开背包 → 按上面判读矩阵复测 → 松手确认方向一致 → 正常退出。
5. 导出 UMM 诊断包（或直接复制 `BepInEx\LogOutput.log`）发回。

## 8. 边界声明

- 本 DLL 是 **边缘感应带修复候选 + 判别插桩**；修复需实机确认"上提立起/下拉躺平/开阔中部保持横/并排竖放"；`[DEBUG-]` 日志按计划在 DEV-16E 资格轮清理。
- 源码已提交 `576cbed`；决议冻结 `135be62`（CONTEXT 词汇 + ADR-0003 Rev + spec §11 + 工单重开）。
- 归档与交付指令在双轴 CLEAN 之后发出（real-machine-test-loop.md 第 41-42 行）。
- 可延后项（Standards/Spec 共 3 项）已列名，DEV-16E 资格轮消解（移除未用参数、合并 ProjectAxis、校正测试红声明注释）。
