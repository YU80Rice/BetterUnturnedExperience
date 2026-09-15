# DEV-V5-02：统一标签分段行带排版

Type: task
Status: resolved（2026-09-15 双轴 CLEAN 关单，见 Answer + audit/2026-09-15/DEV-V5-02/review-loop.md）
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01
Spec: `../spec.md`（「统一排版（V5-T3 → DEV-V5-02）」节）

## What to build

玩家点「整理」（当前栏或 Ctrl+全身）得到按固定用途标签从左上分段、空位收在右下的布局。设置里不再用同类/空间/大件决定算法。旧存档仍能读，只做迁移。

## Scope

- 形成一个深层排版模块：`TryPlanLayout` → 确定性计划或明确失败。规划不改真实背包。当前栏与全身整理只消费该出口。
- 标签顺序与分类规则按规格冻结。弹药箱按给弹匣供弹的蓝图认。分类失败进「其他」，不得丢物。
- 硬性：不重叠、不越界、不丢物、能放下必须放下、失败零提交。
- 旧 mode/direction 首次读取归一，不再决定算法。标题栏仍只留「整理」。
- 内部标识可不进玩家界面。不做选择器、不给第三方加载策略、不扩契约。
- 容器与恢复入口本票不接线，但模块必须可被它们调用（03–05 的前置）。

## 验收条件

- [x] 红测先行：规格所列输入类型（同 ID 医疗堆、细长混放、并排、换行、旋转改善 vs 更差、「其他」、弹药箱 vs SUPPLY）；放得下却重叠失败不得再现；失败零提交；确定性。先红后绿
- [x] 旧三档不再出现在玩家算法档；官方先行消费：真实整理按钮走新计划（模块默认策略=tagged-row-band-v1+PreparePage 标签缝+服务拒绝路径；宿主 e2e 点击不可测=具名接缝缺口，实机随 08）
- [x] 玩家手册相应改写（三模式退役；容器/HUD/换弹属 03/06/07 落地时再改）
- [x] 双轴独立审查 CLEAN（R1-R7，全新实例每轮；终态 Standards R7 CLEAN+Spec R7 CLEAN）
- [x] **候选纪律**：不授候选 / RELEASES / CaseId（diff 实证无 publish/RELEASES/DLL；中间构建哈希仅记录）

## Answer

**2026-09-15 关单。** 统一标签分段行带排版按 V5-T3 冻结语义落地：

- **深模块**：`TaggedRowBandLayout.TryPlanLayout`（纯 C#、零 Unity、单出口）→ 确定性计划或显式失败；四层=流序排序→行带排版（主体高优先锚定、细长件等高让位、并排全局单调准入、换行 x=0、不回填）→局部救济（正向先试旋转后试）→有界精确重排（回溯完备、首解即停）。硬性五条+确定性+失败零提交全部红测钉死（3a-3f/4a-4p）。
- **分类器**：`PlayerUseClassifier.Classify(PlayerUseSignals)` 纯表（明确类型优先、弹匣需真资产、弹药箱按 FillTargetItem 蓝图认、失败入其他）+ `ItemUseSignalsProvider` 引擎侧全 try/catch 与懒建供弹索引。20 档标签逐字冻结（PlayerUseLabel）。
- **官方先行消费**：`TaggedRowBandV1Strategy`（tagged-row-band-v1）为唯一内置策略；PreparePage 填标签；旧三套（DefaultGridV1Strategy/InventorySolver/LayoutCandidate）整体退役删除（编译列表+程序集类型负向钉）。
- **迁移**：mode 从 schema/面板退役（旧档可读、下次保存归一）；direction 保留为稳定收尾（新冻结描述句）；线协议帧零改动；标题栏仍只「整理」；tooltip/玩家手册退役三模式文案。
- **验证**：桩红+4 突变红在案；终态 7 套件+6 门禁全绿；双轴 R1-R7 CLEAN（评审链 audit/2026-09-15/DEV-V5-02/review-loop.md，含具名判断性气味 6 条+接缝缺口 1 条）。
- **候选纪律**：不产候选、不更 RELEASES、不授 CaseId。

**下站**：02 已闭；前沿=DEV-V5-03（容器会话整理，消费本模块）/06/07；04/05 依赖 02 已满足。

## Comments

### 2026-09-15 实施会话：冻结设计（红测前落档）

- **深模块**：新增 `Layout/TaggedRowBandLayout.TryPlanLayout(width,height,stableDescending,items,out plan,out failureReason)`（纯 C#、零 Unity 依赖，输入 items 不被改写；输出=与输入等长的克隆计划，失败=全部 Placed=false 零半成品）。`TaggedRowBandV1Strategy`（StrategyId=`tagged-row-band-v1`）为唯一内置 `ITidyStrategy`；模块默认换绑。`DefaultGridV1Strategy`/`InventorySolver`/`LayoutCandidate` 整体删除（不留三套并行实现；`PackableItem`/`TidyMode` 迁入 `TidyStrategy.cs` 保持线协议与签名兼容）。
- **分类器**：纯核 `PlayerUseClassifier.Classify(PlayerUseSignals)`（类型显式优先；MAGAZINE 需真 `ItemMagazineAsset` 才算弹匣；弹药箱=FillTargetItem 供弹蓝图关系（SUPPLY/BOX 在供弹集内→弹药箱，AMMO 显式类型优先→弹药；SUPPLY 无关系→补给与制作材料；CLOUD/LIBRARY/TIRE/REFILL/FISHER 等无可靠类型→其他）。引擎侧 `ItemUseSignalsProvider` 全 try/catch，宿主无资产=其他，绝不丢物；供弹集=懒建全局索引（含数量失效守卫）。
- **排版冻结规则**（T3-2 可计算裁决）：标签冻结序分桶；桶内组按组总面积降序→长边→面积→宽（几何键固定），升/降序只翻转同几何收尾键。行带：首件为锚定高，细长高件（w≤1 且 h≥3）不配当锚则与后续非细长件交换；同带自左向右连续放，宽不足或高超带→封带换行（新带 x=0），不回填补洞；跨标签并排=同一行带内 x 单调、标签序非降（整体单调边界）；死洞=封带后不可再及的自由格（仅兜底救济路允许填）；带高允许不同。旋转：默认正向，仅当避免漏放才转（主路带内尝试 pref→alt；救济路 bottom-left pref→alt）。救济：主路有未放置→有序 bottom-left 扫描；仍失败→整单 Failure。计划内部先验重叠/越界/集合一致再出参。
- **反例强化（R4/R5-Spec）**：评审先后给出 4×4 上 1×2+2×3+2×3 与 1×3+2×2+2×3 两个「可行但被贪心拒」案例，均落为常驻回归（4n 双变体+4o），并加 4p 钉精确层的不可行面完备（面积恰满但几何不可行 → 显式失败零提交，不虚报可行）。裁决：任何固定序的贪心重排都存在此类缺口，整单重排层因此做成**有界精确可行性求解**（回溯完备，找到任一可行解即停、不比较质量；节点预算 200k，超预算回卷为零并显式失败）——T3「不做全局最优装箱」（不求最优/不比质量）与「能放下必须给出合法布局」（可行性完备）由此同时成立。标签结构保证完整存在于行带层与局部救济层；重排层是具名的结构让位硬底线层。
- **合法件契约（具名裁决，R2-Spec 要求正式写入票面）**：TryPlanLayout 的「全部放置或明确失败」在合法件（尺寸为正）上量化——这是迁入求解器的既有契约（异常物品 Placed=false 但不影响整体）与服务层未放置计数口径（`ManualTidyService` 仅对 size>0 的未放置件拒绝）一致，且 0×0 工坊错制件在生产链上（commit 循环只移动 Placed 件、指纹/Tag 校验要求条目全集）不会丢物、也不会被误提交。零尺寸条目恒以 Placed=false 出现在计划中（计划=每输入一条），消费者按「未放置=原位保留」处理，无须复制服务的尺寸判断（commit 循环本即如此）。改变此语义（零尺寸→整单失败）会让单个错制件拦死整页整理，判为更差；维持现语义并在此正式具名。
- **设置迁移**：`inventorytidy.mode` 从 schema/面板退役（旧存档键可读不报错、下次保存自然消失=归一）；`inventorytidy.direction` 保留但语义降为稳定收尾（新冻结描述句）；点击缝只读 direction；线协议 mode 字节原样传输（值恒旧默认，服务端规划不消费）。标题栏仍只「整理」；tooltip/玩家手册 LIT 行随三模式退役改写。
