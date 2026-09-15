# DEV-V5-02 双轴评审链（统一标签分段行带排版）

日期：2026-09-15。会话：/implement 独立实施票。基线=HEAD `9c2f394`（第五阶段前沿 DEV-V5-02/06/07，用户放行开工）。
纪律：红测先行 + 双轴独立审查每轮全新实例（standards-reviewer / Spec-Reviewer）；不授候选 / 不更 RELEASES / 不授 CaseId。

## 红测先行证据链

| 文件 | 内容 |
|---|---|
| `red-run-stub.txt` | 新组首跑（桩）：classifier 断言红（RED-STUB 返回 Other） |
| `red-run-litsingle-stub.txt` | `--bue-v2-lit-red`（桩）：`strategy: the plan always accounts for every input item` 红 |
| `red-run-default-stub.txt` | 默认套件（桩）：同红，链式 fail-fast |
| `red-run-layout-stub.txt` | classifier 实现后、layout 桩时：`layout: the mixed-label fixture plans fully on a 6x4` 红 |
| `red-mutation-M1-run.txt` | 突变 M1=流序比较器去标签键 → `layout: labels sharing a row band sit side by side in one band` 红 |
| `red-mutation-M2-run.txt` | 突变 M2=分类器兜底改弹匣 → `classifier: MAGAZINE counts as 弹匣 only with a real ItemMagazineAsset` 红 |
| `red-mutation-M3-run.txt` | 突变 M3=PreparePage 摘标签填充 → `official-first: PreparePage fills the frozen labels from the classifier seam` 红 |
| `red-mutation-M4-run.txt` | 突变 M4=点击缝重新消费旧 mode → `migration: even a junk legacy mode value cannot refuse the unified click` 红 |
| `red-run-r5-counterexample.txt` | （R5 反例验证跑，exit 0=现三层已覆盖，反例误判留证） |

M1 前两次尝试（去 CellFree、去不可达分支 `it.Label < curLabel`）不咬红——两者经论证为构造性冗余防御（行带游标单调推进不可能重叠；流序单次扫描不回头），已还原并在源码注释如实标注「不可达，仅作契约防御」。

## 绿证据链（最终态）

`green-v502-group-run.txt`（新组 exit 0，4s 含 4p 穷尽证明）、`green-fullsuite-{ClientUi,Contracts,Network,Placement,Plugin,Release,Settings}-run.txt`（7 套件全 exit 0）、`gate-*-final.txt`（6 门禁全 exit 0）、`green-sln-rebuild.log.txt` 缺省（Rebuild -v:q 静默成功）。最终 DLL SHA-256 前缀 `6bd2bece389c4826`——**中间构建不授身份**（候选纪律），仅记录以证明审查对象=被测二进制。

## 评审轮次

### R1（双轴全新实例）
- Standards：无硬违规；判断题 #1 TidyStrategy.cs 多类共文件（=票面显式选择，保留）；#2 `it.Label < curLabel` 死分支注释应如实标注契约防御 → **已修**。
- Spec：① 零尺寸异常件成功语义质疑；② 范围蔓延（`docs/third-party/unturned-plugin-dev/` 等被 `git add docs` 误扫入冻结包）。
  - ① 裁定=合法件契约延续迁入求解器既有约定+服务层同口径，不改行为、注释与测试措辞强化（见 R2 复评）。
  - ② **属实但根因=冻结命令误扫**：该目录与会话前既有的 DEV-V5-01 票面用户改动均非本票产物（未跟踪状态、9/11-9/14 文件时间戳）。修复=冻结范围改精确文件清单（`changed-files-r2` 起），产物文件保持原位不提交。

### R2（双轴全新实例）
- Standards：硬发现=玩家手册「行带式」泄漏 CONTEXT「行带排版」avoid 名单 → **已修**（改「各段按从左上到右下的顺序摆放」）；记录 TidyStrategy.cs:109 悬空 InventorySolver 引用句 → **已修**（改述统一排版模块）。
- Spec：R1① 零尺寸项裁定「须写入票面规格层」→ 票面 Comments 新增「合法件契约（具名裁决）」段；其余 A–I 通过。R1② 范围裁定闭包。

### R3（双轴全新实例）
- Standards：**CLEAN**（R2 两项修复核验+终扫全过）。
- Spec：误读红证文件（把桩阶段 `red-run-layout-stub.txt` 当现时失败）+ 手册阶段整体清单误归本票——两项均裁定不成立，R4 复核。

### R4（Spec 全新实例）
- 确认 R3 两项误读；留 1 项新发现：Stage 3（单序 BL 重排）对 `1×2+2×3+2×3@4×4` 可行页误拒——**有效**（同标签变体实测救济层可救，跨标签变体真红）。
- 修复：重排层大件优先序 + 反例落常驻回归 4n（双变体）；红→绿闭环（4n 初跑红于跨标签变体，修复后绿）。

### R5（Spec 全新实例）
- 新反例 `1×3+2×2+2×3@4×4`：单序贪心重排（含大件优先）仍可达误拒——**裁定有效**。
- 修复（闭包形态）：Stage 4 重排层改为**有界回溯精确可行性求解器**（完备+首解即停，不比较质量）；4o（占角型可行）+4p（面积恰满但几何不可行→显式失败零提交）双面包驻化。T3 L36「不做全局最优装箱」与 L37「能放下必须给出合法布局」的并存裁定写入票面裁决段。
- Standards R5：**CLEAN**（判断题：轮次注释前缀库内惯例 R<n>-<轴>，本票 `Spec-R4` 顺序孤例 → 已统一为 `R4-Spec`/`R5-Spec`/`R2-Spec`）。

### R6（Standards 全新实例）
- **CLEAN** 含 3 判断题：① 「真实页 ≤6×6 量级」缺引用 → 已改写为不夸大措辞（常规输入毫秒级+4p 实测数据）；② 票面合并引用格式保留（具名）；③ 票面裁决段漏 4p → 已补。

### R7（Spec 全新实例）
- **CLEAN**：R5 发现裁定闭包；自造新攻击例（`4×2+2×3+1×1@4×4`）经 Stage 4 通过；接缝语义/预算具名/终扫（20 档、四裁决、迁移链、候选纪律、契约 internal）全过。

### 终态
- Standards：R7 CLEAN；后续判断题修复均为纯注释/文档增量（修复后 Rebuild+7 套件+6 门禁复绿）。
- Spec：R7 CLEAN。
- 双轴最后行为性变更（精确求解器）由 R7 双轴在其上评审；R7 后的改动为注释措辞+票面文档（Standards 判断题①③闭环），无行为差量。

## 具名判断性气味（不阻断，全部登记）

1. TidyStrategy.cs 多类型共文件（PackableItem/TidyMode/ITidyStrategy/TidyInput/TidyPlan）——票面显式选择（保线协议/签名兼容）。
2. TryPlanLayout 原始参数四元组（网格宽高拆开传）——延续迁移签名形状，不动。
3. 票面 Comments「R4/R5-Spec」合并引用格式——保留（简洁优先，语义无歧义）。
4. 局部救济/精确重排层破坏行带单调性——票面裁决段具名「结构让位于硬底线」；4j 单调断言只钉 band 成功路径。
5. 精确求解器 200k 节点预算=「预算内完备」——票面具名；超预算显式 cannot-fit 零提交，不虚报。
6. 分类器「弹药」段=ItemCaliberAsset 非弹匣（本游戏构建无 EItemType.AMMO）——票面 Comments 具名披露；CLOUD/LIBRARY/TIRE/REFILL/FISHER 等无可靠类型保守入「其他」。

## 接缝缺口（如实登记）

- 点击→执行器→提交的宿主 e2e 不可测（LocalTidyExecutor 需 Player.LocalPlayer）；本票以「模块默认策略身份 + PreparePage 标签缝 + 服务层拒绝路径 + 协议帧不变」为最深可测点，实机全链随 DEV-V5-08 候选验收。

## 冻结包

R1–R7 各轮 `review-freeze-r*.diff.txt` + `changed-files-r*.txt`；终版 `review-freeze-final.diff.txt`（18 文件，+3.4k/−1.2k 行级）。全部本票文件：Layout 四件（label/classifier/provider/layout 模块）+ 策略适配器 + 策略缝迁入 + 模块 schema/点击 + PreparePage 接线 + 执行器诊断 + tooltip/注册注释 + csproj + 红测组 + 既有钉点改写 + 玩家手册 LIT 行 + 票面（claimed→resolved+冻结设计+裁决段）。
