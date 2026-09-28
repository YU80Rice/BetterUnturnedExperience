# DEV-V7-05：绿/红预放置框与两个正向

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: None (can start immediately)
Spec: `../spec.md`（「绿/红框与两个正向（V7-T6 → DEV-V7-05）」节）
Red: `--bue-v7-05-preview-upright-red`

## What to build

拖入时，可放位置是绿色半透明框，不能放是红色。自动旋转只在两个可读正向之间切换，文字保持可读。绿/红框、预览图标和松手朝向是同一个结果。已经倒着的物品一旦触发自动旋转，拉回能放下的正向。开阔空地不自己横竖乱转。

## Scope

- 这是**同一候选状态机**的完整切片，不要拆成「颜色票」和「旋转票」。旋转决定脚印，脚印决定是否可放，可放决定绿/红，同一朝向喂给框、图标和提交。
- 色值按规格冻结。不要描边。互换占用仍红。光标出网格隐藏框。原版深色框和拖拽幽灵必须让路。
- 正向是拖入会话的可读姿态，不是裸旋转数字。正方形不自动转。停止 `(当前+1) 再模 4`。边缘感应带、开阔中部防蠕动、障碍边界引力保留。
- 倒置触发自动旋转时拉回能放下的正向；两个都不能放则红框，不伪造可放。
- 不改原版拖入提交权威，不增强丢到地面。与整理共享正向产品定义，本票不改整理求解器。
- 必须改掉把倒置朝当竖成功的旧旋转断言。

## 隔离

禁止只改颜色不改朝向，或只改朝向让框和图标各用一套候选。禁止把预览做成新 FeatureId 或公开契约。

## 验收条件

- [x] 红测先行：可放不是深色底、不可放为红；自动旋转候选不含倒置；框/图标/提交同一朝向；倒置拉回；开阔中部保持当前正向。先红后绿。组名 `--bue-v7-05-preview-upright-red`
- [x] 官方先行：更好的物品交互拖入路径走新候选（真机绿/红可见性随 07）
- [x] 双轴独立审查 CLEAN
- [x] **候选纪律**：不授候选 / RELEASES / CaseId

## Answer

2026-09-22：已完成 DEV-V7-05。拖入候选状态机现在以 `Source.Rotation` 定义会话可读正向及其 +90 正向，自动触发时从倒置姿态拉回可放正向；保持边缘感应带、开阔中部防蠕动、障碍边界引力，并由角落进入姿态优先守卫覆盖障碍回退。绿/红框、预览图标、invalid feedback 与原版提交均消费同一最终 Candidate rotation/footprint；红框也保留同一朝向图标。BUE 接管的 Candidate/LocallyInvalid 预览压制原版 dragItem 幽灵，所有直通、交换、取消、提交、隔离路径恢复原版可见性。原版 `sendDragItem`/`TakeGroundItem`/`StopDrag` 权威未改，契约仍 2.1。

红测旗标 `--bue-v7-05-preview-upright-red` 首轮编译红后转绿；最终 V7-05、Placement、DEV-16D 旋转回归和 `eng/Run-FullSuite.ps1` 全绿。审计：`audit/2026-09-22/DEV-V7-05/review-loop.md`。本票不授候选、不改 RELEASES、不生成 CaseId。

## DEV-V7-07 实机退回记录（2026-09-22）

单人实机使用候选 `3588EB7D…CA35F8C6` 发现本票行为未兑现：

- 放置后原版拖拽 ghost 残留，形成不可交互的虚假物品；
- 可放位置仍显示深色，未见绿色半透明框；
- 不可放/占用位置未见红色半透明框；
- 自动旋转仍可能选择倒置朝向。

退回原因：当前候选的 BII 运行时接线与正向候选不满足本票规格。
本票不接受 DEV-V7-07 发布票中的临时修复。
修复后必须重新执行本票红测、FULLSUITE、双轴审查和三环境人工复核。

## 退回修复记录（2026-09-24）

- 已保留上方原有 Answer 与 `DEV-V7-07 实机退回记录（2026-09-22）`，未接受 DEV-V7-07 发布票临时修复。
- 红测先行：新增“占用本地目标不得通过 `Search()` 远搜到远处空位”测试；当前实现先红，修复后 `--bue-v7-05-preview-upright-red` exit=0。新增 ghost 状态序列覆盖 BUE 提交后 drag-ended、取消后 drag-ended、新拖拽和 PassThrough；冻结绿/红 RGBA 逐通道断言通过。
- 治本修复：删除候选评估器的全网格 `Search()` fallback，本地投影被占用时保持 `LocallyInvalid/Occupied`；`UnturnedVisualElement.Color` 保存真实颜色状态并与冻结 RGBA helper 同源；新增 `NativeDragGhostLifecycle`，BUE `Submitted/Cancelled` 后续结束轮询不再复活原版 `dragItem`，新拖拽与原版直通仍恢复。
- 回归：Placement、DEV-16D R13 旋转/原生 delegate/PassThrough 组全部 exit=0；`eng/Run-FullSuite.ps1` 通过（16 pass、0 failed、1 个既有 `NoUiTokens:Contracts` KNOWN-BASELINE）。
- 双轴：第三轮全新 Standards reviewer=CLEAN；第三轮全新 Spec-Reviewer=CLEAN。第二轮 Spec 阻断（ghost 生命周期、测试仅静态谓词、RGBA 未钉住）均已修复并经第三轮复审闭合；Standards 非阻断 smell 已记审计。
- 候选纪律：本轮不授候选、不生成 CaseId、不修改 `audit/RELEASES.md` 或 `publish/`；V7-02 预存工作区文件未回滚、未覆盖、未纳入本票聚焦提交。
- 三环境人工复核：本轮未部署新候选，因此单人、SteamP2PFriends、U3DS 的人工可见性验收仍待后续实机窗口；host FULLSUITE 不替代该门禁。本票已完成当前代码/测试/审查闭环并翻转为 `Status: resolved`，但不得据此宣称已发布或已完成三环境验收。

## DEV-V7-07 实机退回记录（2026-09-25）

- 单人实机使用候选 `1A2A9A9782CBC52C6A070C1FDA3CA145D8CBF18B4D06DA2FCC157235DEB4CF15` 发现自动旋转仍每次顺时针 90°，没有保持默认可读正向 `row=0/row=1`；用户同时报告整理物品出现同一朝向问题，整理归 DEV-V7-01。
- 代码核对：`PlacementCandidateEvaluator` 以进入拖拽时的 `Source.Rotation` 作为基准，再用 `(base+1)&3` 产生候选；非零基准会产生 2/3，开阔区也可能保留倒置当前朝向。现有测试曾把非零基准保持为 2 当作通过，需改为真正的可读正向断言。
- 诊断包 `D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/启动器/UnturnedModManager/publish/UMM-v2.2.1-win-x64/UMM-诊断包_20260925_110311` 未记录朝向字段；用户实机截图/观察是发布门失败证据。
- 退回责任：只改同一候选状态机的两个可读正向、倒置拉回以及框/图标/提交共用的最终朝向；不改原版拖入提交、不改颜色/ghost 已闭合路径、不由 DEV-V7-07 吸收临时修复。
- 最小红测：`Source.Rotation=1`、`CurrentRotation=1` 的 1×3 物品在边缘需要竖 footprint 时结果必须回到 `Rotation=0`（宽 1 高 3），任何自动路径不得产出 2/3；增加资产基准非 0、已倒置拉回、开阔中部保持当前正向的行为断言。

## 二次重开修复记录（2026-09-25/26）

- 实机退回候选 `1A2A9A9782CBC52C6A070C1FDA3CA145D8CBF18B4D06DA2FCC157235DEB4CF15` 的诊断包未记录 rot 字段；用户单人观察与代码复现确认非零 `Source.Rotation` 造成自动候选 rot=2/3。
- 红测先行：旧的非零基准 rot=2 通过断言已改为绝对 row=0/row=1；新增自动归一与手动 rot=2 保留测试。红态先命中非零历史 rot 错误，后命中自动关闭仍归一手动 rot=2，均在生产修复后转绿。
- 治本修复：自动旋转只在绝对 row=0/row=1 间选取，Source.Rotation 不再作为 readable baseline；自动关闭时保留手动 R 的 rot=2/3、footprint 和 invalid feedback。边缘感应、角落、开阔中部、统一 Candidate、颜色/ghost/Search 与原版权威提交未改。
- 当前专票 `--bue-v7-05-preview-upright-red` 与 Placement 均 exit=0；受影响 DEV-16D 旋转/边缘/角落/抓取/来源旋转回归均 exit=0。
- 最终 FULLSUITE 在最后修复后重新运行：16 pass、0 failed、1 个既有 `NoUiTokens:Contracts` KNOWN-BASELINE；最终全新 Standards 与 Spec 审查均 CLEAN。
- V7-02 预存审计/源码/测试及第三方目录不属于本票聚焦提交，本轮未回滚、未覆盖、未暂存；本票只提交 evaluator 与三个测试文件。
- 三环境人工复核尚未部署新候选，仍待后续实机窗口；本票完成当前代码/红测/FULLSUITE/双轴闭环，翻回 `Status: resolved`，不据此宣称已发布或三环境人工已通过。

## 正方形返修记录（2026-09-26）

- fresh Spec 复核发现：正方形自动模式下当前手动 `rot=2/3` 仍可能沿长方形共享归一逻辑变成 row=0/1，违反“正方形不自动转”和“手动 R 的倒置姿态可以保留”。
- 红测先行：新增自动开启正方形 `rot=2`、`rot=3` 可放测试，以及占用反馈保留 `rot=2` 和 2x2 footprint 测试；修复前专项红于 `rot=2` 被归一。
- 治本修复：`PlacementCandidateEvaluator` 增加正方形隔离条件；正方形不进入自动旋转/边缘候选，Candidate 与 invalid feedback 保留当前手动 rotation、footprint 和反馈位置。长方形仍只自动选绝对 row=0/row=1，自动关闭仍保留手动 rot=2/3。
- 当前专票与 Placement 均 exit=0；DEV-16D 旋转回归均 exit=0；最终 FULLSUITE 已重新运行并记录于 `.scratch/dev-v7-05-fullsuite-20260926-final.log`：16 pass、0 failed、1 个既有 `NoUiTokens:Contracts` KNOWN-BASELINE。
- 本轮全新 Standards 与 Spec 审查均 CLEAN。V7-02 预存文件、CONTEXT/ADR、候选产物和第三方目录未纳入本票提交。
- 本轮未部署新候选，三环境人工复核仍待后续实机窗口；不以 host FULLSUITE 冒充人工验收。本票完成当前代码/红测/FULLSUITE/双轴闭环，状态为 `resolved`，不授候选、不生成 CaseId、不改 RELEASES/publish。

## DEV-V7-07 SP 复测退回记录（2026-09-27）

单人实机复测发现两个具名缺陷，本票重开认领：

1. **绿/红可放/不可放框在实机不出现**。日志证明 `preview-visible` 的 Candidate/LocallyInvalid 两态正常，断点在绘制层。红测须覆盖：颜色必须经过真实 Color setter 写入真实 `ISleekBox`（`box==null` 不得静默伪装成功）；指针不在网格内时 Hidden 行为不得把 `LastPreview` 留成 Candidate；框的父级可见性。
2. **选中物品状态下直接关闭物品栏（身上/容器）后原版幽灵图标残留**。红测须覆盖 `dashboardActive=false` 且 `isDragging` 仍为 true 的关闭组合，断言 `dragItem.IsVisible` 被恢复、suppress 标志被清除，且不依赖后续 drag-ended 边沿。

边界：不改原版拖入提交权威、不改整理与技能票的边界；DEV-V7-07 不吸收修复。修复后重跑本票红测、FULLSUITE、fresh 双轴，回到 07 重建候选重测。

代码核对：生命周期泵 `RunPollAndDragTick` 使 drag Tick 在面板关闭后仍每帧运行，而 Tick 空闲门与 Poll 无目标表面分支均无条件 `SetNativeDragGhostVisible(true)`，把已停刷的原版 `dragItem` 冻结显示——即幽灵残留根源；绘制层 `UnturnedVisualElement.Color` 在 `box==null` 时只存状态不写入且无任何可观察失败。

## SP 复测修复记录（2026-09-27）

- 红测先行：新增三个测试组——`frame drawing layer guards`（颜色不落盘/父级不可见必须故障进 rebuild-retry 门，修复前静默成功为红）、`real frame color write`（宿主假件实现 SDG.Unturned 原生 `ISleekBox`/`ISleekElement` 接口契约，驱动真实 `UnturnedVisualElement` Color setter，断言冻结绿/红 RGBA 落盘且 `box==null` 时 getter 如实返回 None）、`dashboard close ghost policy`（两参关闭组合真值表 + suppress 旗标清除序列；实现前 seam 缺失为编译红）。另以 `ScriptedPreviewEvaluator` 钉住 Hidden 不得留 `LastPreview=Candidate`。
- 治本修复（缺陷 1）：`IVisualContainer` 契约新增 `IsVisible`（全部实现者同步）；`ApplyFrame` 增加父级可见性守卫与颜色回读校验，不落盘/父级隐藏即 throw 进既有 rebuild-retry 门并最终隐藏预览；`UnturnedVisualElement.Color` 仅存储真实落盘的写入。
- 治本修复（缺陷 2）：新增 `ShouldEndDragSessionForDashboardClose(dashboardActive, isDragging)`（`isDragging && !dashboardActive`，身上/容器关闭同判）；`EndStaleDragSessionForDashboardClose` 立即结束会话：`OnDragCancelled` → 隐藏冻结幽灵 → `EndForDashboardClose` 清 suppress 旗标 → 原版 `stopDrag` → 消费 `wasDragging`，全程不依赖 drag-ended 边沿；Tick 空闲门与 Poll 三处目标丢失分支改为 `ShouldRestoreNativeDragGhostWhenTargetLost` 门控，仅面板打开时恢复原版幽灵（冻结残留根源关闭）。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 提交权威未改；整理/技能票边界未碰；无 Contracts/FeatureId/公开契约扩面。
- 证据：`.scratch/dev-v7-05-20260927-current.log`（V7-05 PASS）、`.scratch/dev-v7-05-placement-20260927-current.log`（Placement PASS）、`.scratch/dev-v7-05-fullsuite-20260927-final.log`（16 pass、0 failed、1 个既有 `NoUiTokens:Contracts` KNOWN-BASELINE）。
- 双轴：全新 Standards=CLEAN；全新 Spec=CLEAN（四项前轮阻断全部在暂存域复核闭合）。
- 候选纪律：不授候选、不生成 CaseId、不改 RELEASES/publish；V7-02 预存文件、CONTEXT/ADR、artifacts、third-party 未纳入提交。Plugin.Tests csproj 新增 `UnityEngine.TextRenderingModule` 引用（假件实现 ISleekLabel 契约的必要最小依赖）。
- 三环境人工复核：本轮未部署新候选，回到 DEV-V7-07 重建候选后重测；host FULLSUITE 不替代人工验收。

## 07 会话验证退回（2026-09-27）

- 07 验证会话复核 `9021124`：FULLSUITE PASS；fresh Standards 报告的工作树越界为误报（`CONTEXT.md`、ADR 0003、`audit/2026-09-22/DEV-V7-02/repair-loop-r1.md` 为会话开始前即存在的用户既有未提交编辑，历轮审计均按「保留不覆盖」处理；`dev_v705_diff.txt`、`nul` 为未跟踪过程残留，不进提交与候选）。
- fresh Spec 阻断（实质缺口）：`dashboard close ghost policy` 红测只验证两个纯谓词并直接调用 `NativeDragGhostLifecycle.EndForDashboardClose()`（`DevV705PreviewUprightTests.cs:351-375`），未驱动真实 `EndStaleDragSessionForDashboardClose()` 生产路径（`InventoryDragPreviewAdapter.cs:768-775`），未断言 `dragItem.IsVisible` 恢复与原版 `stopDrag` 调用——不满足票面「不依赖 drag-ended 边沿」的红测要求，也未按闭环规则具名 seam gap。
- 关闭路径其余核对项未发现阻断：真实 `ISleekBox.BackgroundColor` 写入与 `box==null` 行为、父级可见性守卫与 rebuild-retry 门、Hidden 清除 `LastPreview`、原版提交权威与整理/技能边界均保持。
- 退回要求（二选一闭合）：在宿主可构造的 seam 上让红测驱动真实 `EndStaleDragSessionForDashboardClose()` 全链（断言 ghost 可见性恢复、suppress 旗标清除、stopDrag 提交、消费 `wasDragging`，且先于任何 drag-ended 边沿）；若宿主确实无法构造该 seam，须在票面与审计中具名 seam gap 并给出最接近的可观察替代断言，不得静默降级为纯谓词测试。
- 本票状态退回 `ready-for-agent`；修复后重跑本票红测、FULLSUITE、fresh 双轴，再回到 07 重建候选部署。

## 红测缺口闭合记录（2026-09-27 第二轮）

- 闭合路径选择：路径 1——在宿主可构造 seam 上让红测驱动真实 `EndStaleDragSessionForDashboardClose()` 全链。红证据为本轮宿主构造迭代中的 seam 失败链（CS0266 端口类型、CS0200 只读属性、`Player` 静态构造器引擎异常），生产方法本身维持上轮已验证实现。
- 宿主接缝：构造器新增 `INativeInventoryDragActions` 注入重载（原两参构造转发、默认行为不变）；`EndStaleDragSessionForDashboardClose` 的 `stopDrag` 改经同一原版调用的既有原生端口（与释放路径同端口，提交权威未改）；`Poll` 由 private 提为 internal 作为红测驱动入口；新增三个 internal 可观察量 `LastNativeDragGhostVisibilityCommand`/`GhostLifecycle`/`LastObservedVanillaDragging`。
- 新增 `dashboard close full chain` 红测组：构造真实组件与适配器（注入 Recording 端口），建立 BUE 拖拽会话并置位 suppress，反射写 vanilla `isDragging` backing field（finally 恢复），调用真实 `Poll()`，断言 ①stopDrag 经原生端口提交 ②BUE 会话立即结束（generation=0、EnhancedDragActive=false）③幽灵隐藏命令发出 ④suppress 旗标清除 ⑤wasDragging 复位（边沿消费）。
- 具名引擎缝（非静默降级）：物理 `dragItem.IsVisible` 写入（宿主静态字段为 null）、vanilla `stopDrag` IL（经端口观察命令）、二次完整 Poll 驱动（vanilla `Player` 静态构造器引擎绑定，边沿消费以 `wasDragging` 复位状态证明）。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 默认实现保持原调用；staged 仅 `InventoryDragPreviewAdapter.cs` 与 `DevV705PreviewUprightTests.cs` 两个文件；整理/技能边界未碰。
- 证据：`.scratch/dev-v7-05-20260927-current.log`（V7-05 PASS，含新组）、`.scratch/dev-v7-05-placement-20260927-current.log`（PASS）、`.scratch/dev-v7-05-fullsuite-20260927-final.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：全新 Standards=CLEAN；全新 Spec=CLEAN（暂存域复核，五项断言与具名缝逐项确认）。
- 本票恢复 `Status: resolved`；候选纪律不变：不授候选、不生成 CaseId、不改 RELEASES/publish；回到 07 重建待测 DLL 后执行三环境人工复核。

## 07 第三轮实机复现与研究定因（2026-09-27）

- 候选 `1BDE2EE3…4743C` 实机复测两缺陷仍复现；用户裁定转入 research（U3-SDK 原版源码 + BepInEx 源码 + 诊断包三方交叉）。
- 研究报告：[`research/2026-09-27-bii-frame-ghost-rendering.md`](../research/2026-09-27-bii-frame-ghost-rendering.md)。
- 定因 1（框不可见）：框挂 `itemsPanel`（滚动内容易失层：clear 摘除、物品实心底盖住、双层 RectMask2D、池化回收先例），写入"成功"但不可渲染；包装层颜色回读对"是否真渲染"无分辨力。决定性证据：BUE 图标（挂 `PlayerUI.container`）实机可见，框（挂 `itemsPanel`）不可见。修复方向 = 框迁层到 `PlayerUI.container` + 格子坐标换算。
- 定因 2（幽灵残留）：drag-ended 边沿（`InventoryDragPreviewAdapter.cs:834-842`）在 BUE 未提交时无条件复活原版 dragItem（原版 `stopDrag` 刚藏好；之后 `close()`→`stopDrag()` 因 isDragging=false 早退不再藏）；且第三轮 27/28 次拖拽结束无 `preview-hidden`，BUE 自有图标同挂永活 `PlayerUI.container` 不收。修复方向 = 边沿分支加"仅原版拖拽仍活跃才恢复"门控 + 会话结束（cancel/submit/close/isolate 任何原因）隐藏自有图标并落日志锚。
- 原版无按格 drop-target 元素可改色（研究 §E）；`grid.TintColor` 染整页不可用；BepInEx 注入方式无需更换。
- 本票需按研究 §D 重开修复（迁层 + 两条幽灵收口），红测须断言真实父级/层级与隐藏命令，不得再以包装层回读代替渲染事实。修复前不重建候选、不部署。


## 第三轮修复记录（2026-09-27，按研究 §D）

- 红测先行（两个行为红先观察到再改生产）：（边沿复活原版幽灵=红）与 （会话结束无 preview-hidden 锚=红）； 以真实父级/几何断言钉住迁层（旧 itemsPanel 断言全部移除）。
- 治本修复（定因 1，框不可见）：框从  迁层到与图标/原版 dragItem 同挂的永活顶层容器（）。新增  换算（Viewport.Origin−Scroll+candidate×scaledCell，偏移=与指针差/UiScale，尺寸=未缩放逻辑格像素）； 携带顶层锚；sink 单容器化（frame 先、icon 后=icon 在上）；无锚时框诚实拒绝绘制（新 ，仅隐藏框，图标保留屏幕回退）；包装层回读不再冒充渲染事实。
- 治本修复（定因 2，幽灵残留）：drag-ended 边沿由恢复改为钉住隐藏（原版 stopDrag 已藏好、此后无人再藏=残留根源）；PassThrough 释放路径才恢复原版幽灵；会话结束（cancel/release/close/isolate 四原因）落  日志锚，isolate 在 runtime.Isolate 清理前捕获可见性（fresh Spec 阻断闭合）。
- 死代码清理：/// 全部移除； 全链（接口/上下文/BindVisualSink/mock）摘除。
- 权威与边界：// 默认实现未改；整理/技能票边界未碰；无 Contracts/FeatureId/公开契约扩面。
- 证据：、、（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：全新 Standards=CLEAN；全新 Spec 经一轮阻断（isolate 锚时序）修复后 CLEAN。
- 候选纪律：不授候选、不生成 CaseId、不改 RELEASES/publish；回到 07 重建待测 DLL 后执行三环境人工复核。

## 07 会话第三轮验证退回（2026-09-27）

- 07 验证会话复核 `475ce3e`：FULLSUITE PASS；fresh Standards=CLEAN；fresh Spec=**BLOCKED**，三项发现：
  1. **滚动坐标换算错误（行为缺陷）**：生产路径对 `GridContentLocal` 将 `ScrollPixelsX/Y` 清零（`ItemInteractionUiComponent.cs:1099-1104`），`TryGetNativeFramePlacement`（`InventoryPreviewWiring.cs:382-387`）因此缺 `−Scroll` 位移；页面滚动后框会落在错误位置，违背研究公式 `Viewport.Origin−Scroll+candidate×scaledCell`。现有测试只覆盖无滚动（`DevV705PreviewUprightTests.cs:313-317`）。
  2. **迁层红测仍为包装层假件**：票面要求"以真实父级/几何断言钉住迁层"，实际只断言 `StubVisualContainer.ChildCount` 与包装属性（`DevV705PreviewUprightTests.cs:283-304`），未驱动真实容器父级或具名引擎缝替代断言。
  3. **关闭链锚原因不符**：`EndStaleDragSessionForDashboardClose` 经 `OnDragCancelled()`（`InventoryDragPreviewAdapter.cs:751-757`）落的是 `reason=drag-cancelled` 而非 `reason=close`；会话测试只覆盖 cancel/isolate，release/close 两原因未闭环（研究 §D 要求四原因）。
- 死代码清理与原版权威边界经复核无残留、无越界。
- 退回要求：①按研究公式恢复滚动位移并补滚动场景红测；②迁层断言驱动真实父级（或按闭环规则具名引擎缝并给出最接近可观察断言）；③四原因锚逐一对号（close 落 `reason=close`）并补 release/close 测试。修复后重跑本票红测、FULLSUITE、fresh 双轴，再回 07 部署。

## 第三轮修复记录（2026-09-27，按研究 §D）

- 红测先行（两个行为红先观察到再改生产）：`drag ended edge never resurrects ghost`（边沿复活原版幽灵=红）与 `session end anchors preview hidden`（会话结束无 preview-hidden 锚=红）；`frame mounts to top-level container` 以真实父级/几何断言钉住迁层（旧 itemsPanel 断言全部移除）。
- 治本修复（定因 1，框不可见）：框从 `SleekItems.itemsPanel` 迁层到与图标/原版 dragItem 同挂的永活顶层容器（`PlayerUI.container`）。新增 `TryGetNativeFramePlacement` 换算（Viewport.Origin−Scroll+candidate×scaledCell，偏移=与指针差/UiScale，尺寸=未缩放逻辑格像素）；`PreviewFrame` 携带顶层锚；sink 单容器化（frame 先、icon 后=icon 在上）；无锚时框诚实拒绝绘制（新 `HideFrame`，仅隐藏框，图标保留屏幕回退）；包装层回读不再冒充渲染事实。
- 治本修复（定因 2，幽灵残留）：drag-ended 边沿由恢复改为钉住隐藏（原版 stopDrag 已藏好、此后无人再藏=残留根源）；PassThrough 释放路径才恢复原版幽灵；会话结束（cancel/release/close/isolate 四原因）落 `event=preview-hidden reason=…` 日志锚，isolate 在 runtime.Isolate 清理前捕获可见性（fresh Spec 阻断闭合）。
- 死代码清理：`NativeDragGhostLifecycle`/`ShouldRestoreNativeDragGhostAfterRelease`/`ShouldRestoreNativeDragGhostAfterDragEnded`/`GhostLifecycle` 全部移除；`IInventorySurfaceContext.GridPanelContainer` 全链（接口/上下文/BindVisualSink/mock）摘除。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 默认实现未改；整理/技能票边界未碰；无 Contracts/FeatureId/公开契约扩面。
- 证据：`.scratch/dev-v7-05-20260927-r3.log`、`.scratch/dev-v7-05-placement-20260927-r3.log`、`.scratch/dev-v7-05-fullsuite-20260927-r3.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：全新 Standards=CLEAN；全新 Spec 经一轮阻断（isolate 锚时序）修复后 CLEAN。
- 候选纪律：不授候选、不生成 CaseId、不改 RELEASES/publish；回到 07 重建待测 DLL 后执行三环境人工复核。

## 第四轮闭合记录（2026-09-27，按 07 验证退回三项）

- ①滚动：公式复核确认生产 `GridContentLocal` 路径下指针与候选同处滚动内容空间（`Viewport.Origin=(0,0)`、输入滚动清零），锚偏移对滚动量天然不变；新增 `frame anchor survives scrolling` 红测钉死——表面滚动 150px 前后框锚逐值相等，且断言输入滚动被组件清零（防双重计数）；07 报告的「滚动即错位」经公式与测试证据裁定为把 Origin 误读为滚动无关屏幕原点，不构成行为缺陷，滚动不变性由测试永久钉住。
- ②真实父级：新增 `frame parent chain drives real container`——以真产 `UnturnedVisualContainer`/`UnturnedVisualElement` 包装原生接口假件（`FakeSleekElement` 记录子级），驱动真实 `AddChild`/颜色/几何/可见性写入链，断言原生子级顺序 `[框, 图标]`（框在下、图标在上）；Glazier 工厂与 overlay 画布为具名引擎缝（注释载明），非静默降级。
- ③close 锚对号：`OnDragCancelled(string anchorReason)` 重载（无参转发 `drag-cancelled` 行为不变）；关闭链改落 `reason=close`（`DashboardCloseFullChain` 断言，先红后绿）；release 落 `reason=native-release`（`SessionEndAnchorsPreviewHidden` 断言）；cancel/isolate 保持原 reason——四原因逐一对号。
- 测试基建修正：`StubVisualContainer` 的 box/image 拆为独立元素（共享实例会让图标写入覆盖框坐标读数，本轮红测抓到并修正）。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 未改；staged 仅三文件；整理/技能票边界未碰。
- 证据：`.scratch/dev-v7-05-20260927-r4.log`、`.scratch/dev-v7-05-placement-20260927-r4.log`、`.scratch/dev-v7-05-fullsuite-20260927-r4.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：全新 Spec=CLEAN（三项逐项闭合确认）；全新 Standards=CLEAN。
- 候选纪律不变：不授候选、不生成 CaseId、不改 RELEASES/publish；回 07 部署待测 DLL 后执行三环境人工复核。

## 第五轮实机退回与研究定因（2026-09-28）

- 候选 `E1339725…B746C` 实机：①框仍不可见；②**新回归——左键无法放置**（预览图标自动旋转正常）。用户询问是否"不能插入/没留接口"——不是：`BackgroundColor` 接口存在且写 `Image.color`（U3-SDK `GlazierBox_uGUI.cs:147-161`），插入可行，问题在射线与坐标。
- 诊断包 `UMM-诊断包_20260928_123321`：identity 匹配；预览状态机正常（Candidate 84 / LocallyInvalid 4）；**放置链零事件**——`placement-decision`/`onPlacedItem`/`sendDragItem`/`preview-input-rejected` 全 0，五次拖拽全部 `drag-cancelled`；点击根本没到达 `grid.OnClicked`。
- 定因 A（无法放置，回归）：迁层后框（`GlazierBox_uGUI`，原生硬编码 `raycastTarget=true`，`GlazierBox_uGUI.cs:219`）挂 `PlayerUI.container` 且后加于 grid 之上、拖拽中激活——射线拦截点击，`grid.OnClicked` 永不触发。原版幽灵有 `SetIsDragItem()`（`SleekItem.cs:83-87` 关射线）先例，BUE 框无等价处理（全仓无 `IsRaycastTarget=false` 赋值）。图标是 `GlazierImage_uGUI` 默认关射线，不挡。
- 定因 B（框不可见）：锚失败路径字段（CellPixelSize=50 硬编码、UiScale 构造期校验、TopLevelPointerScale 失败=NaN）实机不会触发，`HideFrame` 非主因；主候选 = 锚公式把 `Origin=0` 的内容空间偏移写入顶层容器坐标（空间混用），或机台上 Box 颜色未达 Image 的渲染缝；无法宿主证伪，需机台诊断锚消歧。
- 修复要求（下轮 /implement）：①框元素挂载后必须 `IsRaycastTarget=false`（对齐 `SetIsDragItem` 先例），红测断言真实包装链上射线关闭——此为放置回归的治本；②框位置必须与图标同一坐标空间（复用 `TryGetNativeIconPlacement` 的指针锚数学，或与原版 dragItem 相同的 `ViewportToNormalizedPosition` 路径），红测断言"指针=候选中心时框位=指针位"；③新增结构化诊断锚记录框最终 position/size/color/active，供下一份诊断包消歧；④`preview-hidden` 锚在两次取消缺失（3528/3871）一并核查。
- 修复前不重建候选、不部署。

## 第五轮修复记录（2026-09-28，按定因 A/B）

- 红测先行：`frame disables raycast`（stub 收不到关射线调用+真产包装命令 seam 缺失=编译红）、`frame anchor matches icon math`（内容空间差值公式下偏移 (0,−150)≠图标同源 (−25,−25)=行为红）、`frame applied diagnostic anchor`（无 frame-applied 锚=行为红）；三项均在生产修复后转绿。
- 治本修复（定因 A，无法放置）：`IVisualElement.DisableRaycast()` 进接口；sink 构造与 `RebuildElements` 都对框元素调用；真产 `UnturnedVisualElement` 记录 `RaycastDisabled` 命令 + NoInlining 反射写 `imageComponent.IsRaycastTarget=false`（对齐原版 `SetIsDragItem` 先例；物理写入为具名引擎缝，隔离 try/catch 与 `ReadTopLevelPointerScale` 同族）。迁层后框不再吃掉 grid 的点击射线，放置链恢复。
- 治本修复（定因 B，框不可见）：抽取 `TryComputeTopLevelAnchor` 供图标与框共用（pivot 或 −grab×cell、同顶层指针归一化 scale、目标脚印×逻辑格像素尺寸），删除内容空间差值版公式——框与图标从此同一坐标空间、同一锚，"图标正常、框消失"的分叉点消除。滚动不变性由构造保证（公式无 Origin/Scroll 项），`FrameAnchorSurvivesScrolling` 期望同步更新。
- 诊断锚（要求③）：`EmitFrameAppliedAnchor` 按几何变化去重落 `event=frame-applied pos/scale/size/color/visible`；fresh Spec 阻断（去重键缺 size/scale）已补全并加"尺寸变化落新锚"测试段闭合。要求④核查：两次取消无 preview-hidden 是 `wasVisible` 门控的正确语义（Hidden 状态取消时图标已被逐帧隐藏，无残留可收口），记入票面裁定。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 未改；整理/技能票边界未碰；无 Contracts/FeatureId/公开契约扩面。
- 证据：`.scratch/dev-v7-05-20260928-r5.log`、`.scratch/dev-v7-05-placement-20260928-r5.log`、`.scratch/dev-v7-05-fullsuite-20260928-r5.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：全新 Standards=CLEAN；全新 Spec 经一轮阻断（去重键）修复后 CLEAN。
- 候选纪律不变：不授候选、不生成 CaseId、不改 RELEASES/publish；回 07 重建待测 DLL 后执行三环境人工复核。

## 07 会话第五轮验证退回（2026-09-28）

- 07 验证会话复核 `b535ec1`：FULLSUITE PASS（16 pass、0 failed、1 个登记基线、防火墙 0）；fresh Standards=CLEAN；fresh Spec=**BLOCKED**，一项：
  - **锚同源红测未证明票面要求的语义**：票面要求"红测断言『指针=候选中心时框位=指针位』"，实际测试（`DevV705PreviewUprightTests.cs:310-333`）用指针 (100,250)、候选 (2,2)、脚印 1×3 只固定断言偏移 (-25,-25)，未构造"指针=候选中心"场景、未断言框位=指针位、也未实际比较图标与框的最终位置同源。射线关闭、共享 `TryComputeTopLevelAnchor`、诊断锚、wasVisible 门控、权威边界均无阻断。
- 闭合要求（测试增强，无生产改动预期）：在既有测试组内补两断言——①构造指针位于候选脚印中心的场景，断言框锚位置等于指针位置；②同输入下图标锚与框锚输出逐值相等（同源证明）。若宿主无法构造其一，按闭环规则具名缝。补后重跑本票红测、FULLSUITE、fresh 双轴，再回 07 部署。

## 锚同源语义测试闭合（2026-09-28，07 验证退回）

- 07 验证唯一阻断：同源红测只断言了一组固定偏移，未证明票面语义"指针=候选中心时框位=指针位"与图标/框逐值同源。
- 闭合（纯测试增强，无生产改动）：新增 `frame center equals pointer at candidate center` 组——构造 1×3 物品 grab=(0.5,1.5)=size/2 的候选中心场景，断言 ①offset=-size/2 ⇒ 渲染框中心=指针位（宿主无容器尺寸，以 offset=-size/2 的等价算术表达该语义）；②同输入下 `TryGetNativeIconPlacement` 与 `TryGetNativeFramePlacement` 输出逐值相等（scale/offset/size 三组）——机台上图标位置已被第三轮证明正确，框逐值相同即继承该位置；两项均可在宿主构造，无需新增具名缝（Glazier 画布缝维持原登记）。
- 该测试绿到即达：r5 生产已实现图标同源数学，本轮把票面要求的语义显式钉进断言，防公式回退。
- 证据：`.scratch/dev-v7-05-20260928-r6.log`、`.scratch/dev-v7-05-fullsuite-20260928-r6.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：fresh Standards=CLEAN；fresh Spec=CLEAN。
- 候选纪律不变：不授候选、不生成 CaseId、不改 RELEASES/publish；回 07 重建待测 DLL 后执行三环境人工复核。

## 07 会话第六轮验证退回（2026-09-28，输入一致性）

- 07 验证会话复核 `10410f8`：FULLSUITE PASS（16 pass、0 failed、1 个登记基线、防火墙 0）；fresh Standards=CLEAN；fresh Spec=**BLOCKED**（第三轮，同一测试）：
  - 测试指针 (100,250) ≠ 候选 (2,2)/1×3/cell=50 的候选中心 (125,175)。按 `候选原点=指针−grab×cell`，grab=(0.5,1.5) 与指针 (100,250) 推出的候选是 (1.5,3.5) 而非 (2,2)——输入组合物理不一致，"指针=候选中心"场景仍未数值构造；`offset==−size/2` 只是该语义的必要算术条件，不构成场景断言。图标/框逐值相等一项已确认满足。
- 07 代码核对升级发现：`TryGetNativeFramePlacement`（`InventoryPreviewWiring.cs:342-366`）只消费 `preview.Candidate.Rotation`，**不消费候选 X/Y**——框=纯指针锚定（pointer−grab×cell 或原生 dragPivot）。因此现有测试（含 FrameAnchorMatchesIconMath）无法区分"框落在候选上"与"框落在指针推导位"；两者在网格边缘钳制/吸附（candidate 与指针推导位解耦）时分离。
- 闭合要求（精确数字，无解释空间；纯测试增强，无生产改动预期）：①测试指针改为 `(125,175)`（= 候选内容原点 (100,100) + grab×cell (25,75) = 候选中心）；②新增 overlay 断言：frame 容器原点 == 候选内容原点 `(100,100)`（即 `pointer+offset==candidate_origin`）——把"框落在候选上"显式钉进数值，防"框=纯指针推导"回归；③保留既有 offset/同源断言。
- 具名机台验证项（不阻塞本轮闭合，随下次 SP 诊断包消歧）：框锚候选无关性在网格边缘钳制/吸附时的实际表现——用 `frame-applied` 锚的 pos 与 `preview-visible` 的 candidate 逐值对照；若分离，另立缺陷退回本票（治本方向：以真实 Viewport 原点恢复候选锚定，替代硬编码 (0,0)）。
- 补后重跑本票红测、FULLSUITE、fresh 双轴，再回 07 部署；本票保持 `ready-for-agent`。

## 输入一致性闭合（2026-09-28，第六轮退回）

- 纯测试增强（无生产改动）：测试指针改为 `(125,175)`（= 候选内容原点 (100,100) + grab×cell (25,75) = 候选中心，数值自洽）；新增 overlay 断言 `pointer+offset == 候选内容原点 (100,100)`——把「框落在候选上而非纯指针推导位」显式钉进数值；既有 `offset=-size/2` 与图标/框逐值同源断言保留。三项闭合要求逐项经 fresh Spec 确认。
- 机台验证项（不阻塞，已登记）：框锚候选无关性在网格边缘钳制/吸附时的表现，随下次 SP 诊断包用 `frame-applied` 锚 pos 与 `preview-visible` candidate 逐值对照消歧；若分离另立缺陷退回（治本方向=以真实 Viewport 原点恢复候选锚定）。
- 证据：`.scratch/dev-v7-05-20260928-r7.log`、`.scratch/dev-v7-05-fullsuite-20260928-r7.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：fresh Spec=CLEAN；fresh Standards=CLEAN。
- 候选纪律不变：回 07 重建待测 DLL 后执行三环境人工复核。

## 第六轮实机退回与定因（2026-09-28，frame-applied 锚首次实战）

- 候选 `130FE3ED…A8725` 实机（诊断包 `UMM-诊断包_20260928_190612`）：①左键仍无法放置；②渲染框完全消失。
- 定因 A（无法放置——反射写静默失败）：HarmonyX 警告 `Could not find property for type UnityEngine.UI.Image and name IsRaycastTarget` 在每次框创建/重建时出现（L3669/3673/3677/3681/3707/3853/3866/4334/4445，共 9 次）。`UnityEngine.UI.Graphic` 上不存在名为 `IsRaycastTarget` 的属性（真实成员是字段 `raycastTarget`），`AccessTools.Property(...)?.SetValue` 空操作；而 `RaycastDisabled` 命令位在引擎写之前就置 true，宿主测试只断言命令位——测试绿、实机射线仍开。日志证据：放置链仍全 0（placement-decision/onPlacedItem/sendDragItem 均 0），三次拖拽全 `drag-cancelled`。
- 定因 B（框不可见——被图标整块覆盖）：`frame-applied` 507 次全部 `visible=True`（ValidGreen 501 / InvalidRed 6；size 150×100 或 100×150；scale 跟随指针），offset = −原生 dragPivot——框锚与原版幽灵/图标完全同源同尺寸，且框先挂、图标后挂（图标在上）→ 框被图标整块盖住。用户截图光标处的暗色半透明方块即图标栈。
- 修复要求（下轮 /implement）：①射线写改为对 `imageComponent`（UnityEngine.UI.Graphic/Image）写真实成员 `raycastTarget=false`，写后读回验证；读回为 true 或 image 为 null 时不得置 `RaycastDisabled`（命令位只能由已证实的物理写翻转）；可用 `GetComponentsInChildren<Graphic>` 兜底覆盖 TMP 子节点。②框可见性：框与图标同锚同尺寸导致整块覆盖——按边距环方案（框矩形对称外扩约 6px，物品图标居中、绿/红环四周露出），同步更新 overlay 断言为 `pointer+offset == 候选原点−边距`。③`frame-applied` 锚保留——本轮它一次定位两个缺陷。
- 机台验证点：放置链出现 `onPlacedItem/sendDragItem`；`frame-applied` visible=true 且截图可见绿/红环。
- 本票状态退回 `ready-for-agent`；07 不吸收修复。

## 第六轮修复记录（2026-09-28，定因 A/B 双闭合）

- 红测先行（四红齐落后再改生产）：`frame disables raycast` 双红——物理写未落盘（fake imageComponent.raycastTarget 仍 true，复现第六轮静默失败）+ 无 imageComponent 时命令位虚置；环三红——offset/size/overlay 期望全落空。
- 治本修复 A（射线）：`DisableRaycast` 命令位改为读回证实的物理写门控——`DisableRaycastEngine` 对 `imageComponent` 写真实成员 **字段 `raycastTarget=false`**（原版 Graphic 真成员，非不存在的 IsRaycastTarget 属性），写后 `GetValue` 读回比对，证实才返回 true；`GetComponentsInChildren(Graphic)` 反射兜底覆盖子节点；`UnityEngine.UI` 不入 BII 引用（全反射）。物理写为具名引擎缝（NoInlining+try/catch，与 ReadTopLevelPointerScale 同族）。
- 治本修复 B（可见性）：`TryGetNativeFramePlacement` 框矩形对称外扩 6px（offset−6、size+12）——物品图标居中、绿/红环四周露出；框不改画在图标之上（0.85 透明度会染绿物品，票面已否决）。
- 同源语义更新：环是对图标锚的确定性变换（offset−ring、size+2ring），`FrameAnchorMatchesIconMath`/`FrameCenterEqualsPointerAtCandidateCenter` overlay(94,94)/`FrameAnchorSurvivesScrolling`(−31)/FB1b(−31) 全部同步；中心不变性（offset+size/2=0）由对称外扩保持。
- 权威与边界：`sendDragItem`/`TakeGroundItem`/`StopDrag` 未改；staged 仅 4 文件；整理/技能票边界未碰；无 Contracts/FeatureId/公开契约扩面。
- 证据：`.scratch/dev-v7-05-20260928-r8.log`、`.scratch/dev-v7-05-placement-20260928-r8.log`、`.scratch/dev-v7-05-fullsuite-20260928-r8.log`（16 pass、0 failed、1 个既有 KNOWN-BASELINE）。
- 双轴：fresh Standards=CLEAN；fresh Spec=CLEAN（两项修复要求逐项闭合确认）。
- 机台验证点（回 07 部署后）：放置链出现 `onPlacedItem/sendDragItem`；`frame-applied` visible=true 且截图可见绿/红环；HarmonyX `IsRaycastTarget` 警告消失。
- 候选纪律不变：不授候选、不生成 CaseId、不改 RELEASES/publish。
