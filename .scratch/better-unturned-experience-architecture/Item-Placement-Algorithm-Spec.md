# GPT-Item-Placement-Algorithm-Spec：模糊落点与自动旋转算法

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-12 联合体验决策基线；实现与游戏运行未验证**  
**输入确认:** 人工开发者与 Gemini 联合否决无差别方向竞争，接受 Local-Fit Priority

## 1. 目标

为“更好的物品交互”冻结确定性候选算法：光标附近能直接放下当前方向时绝不旋转；当前方向局部受阻而旋转方向局部可放时就地旋转；两种局部候选都失败后才扩大搜索。

算法只生成客户端预览和原版提交参数，不具有库存权威性。最终仍调用原版 `sendDragItem(page,x,y,rot)`，由 `ReceiveDragItem` 重新校验。

## 2. 输入不变量

- `CursorGridX/Y` 是历史字段名，语义为前端坐标 adapter 计算后的 `intendedItemCenterGrid`，不是原始 pointer grid。其**容器中心坐标**有效域为半开区间 `[0,containerWidth) × [0,containerHeight)`；超出即 `Hidden/OutsideGrid`。这与前端 `grabOffsetInFootprint` 的闭区间 `[0,W] × [0,H]` 不同：前者描述预期物品中心是否仍在容器连续坐标域内，后者描述 pointer 在物品 footprint 内的连续边界位置。
- `ItemWidth/Height` 为资产未旋转时的正整数格数。
- `CurrentRotation` 规范化为 `0..3`；偶数使用 `width×height`，奇数使用 `height×width`。
- `Occupancy.Width/Height` 是当前容器网格尺寸。
- Occupancy 必须是稳定只读快照，并排除当前正在拖拽物品自己的来源 footprint；否则原位置会被错误视为障碍。
- `DragGeneration`、容器 session generation 或快照失效由调用方在调用前后复核；算法不缓存它们。
- 抓取偏移、UI Scale、屏幕像素与悬浮图标锚点由前端拥有。算法不得接收或重算 grab offset；调用方用 `pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)` 生成本输入中心。
- 前端坐标 adapter 使用左上原点、X 右、Y 下的连续坐标。原生 `(CurrentRotation + 1) & 3` 的 grab offset forward 变换为 `(H - gy, gx)`；`(gy, W - gx)` 是 backward/rot-1，禁止用于 forward 按键旋转。

## 3. 几何投影

对方向 footprint `(w,h)`：

```text
rawX = floor(CursorGridX - w/2 + 0.5)
rawY = floor(CursorGridY - h/2 + 0.5)
x = clamp(rawX, 0, containerWidth  - w)
y = clamp(rawY, 0, containerHeight - h)
```

这是“把光标视为物品几何中心”的最近整数左上角投影；`.5` 使用向正方向取整。局部投影舍入只决定唯一的直落格，不使用 Y/X 同分规则；Y/X 只用于局部失败后的扩大搜索。若 footprint 大于容器，对应方向不可容纳，不执行 clamp 的负上界。

## 4. Local-Fit Priority 阶梯

### 第一级：局部当前方向

计算当前方向的投影格位。若 footprint 完整在边界内且所有格为空，立即返回 `Candidate/current-local`。不得继续检查旋转方向。

### 第二级：局部旋转方向

仅当第一级失败、允许自动旋转且物品不是正方形时，计算 90° 方向的投影格位。若合法，立即返回 `Candidate/automatic-90-local`。

### 第三级：扩大搜索当前方向

前两级均失败后，遍历当前方向全部合法左上角。按候选中心到光标的平方欧氏距离升序，距离相同按 Y、再 X。若存在候选，返回 `Candidate/current-expanded`。

### 第四级：扩大搜索旋转方向

仅当前方向扩大搜索也无解时，遍历旋转方向并使用相同排序。若存在候选，返回 `Candidate/automatic-90-expanded`。

该优先级是方向惯性：旋转只解决光标局部障碍或当前方向全局无解，不与空地当前方向进行逐像素距离竞争，因此不会产生横竖“蠕动”。

## 5. 失败输出

- 光标在容器外：`Hidden / OutsideGrid`，Width/Height 为 0，前端隐藏预览。
- 至少一个实际尝试方向的尺寸可放入容器、但所有位置均被占用：`LocallyInvalid / Occupied`。
- 所有实际尝试方向的 footprint 都大于容器：`LocallyInvalid / OutsideGrid`。
- `NoCandidate` 在 V1 evaluator 中保留但不产生；未来只有引入额外搜索约束后才能重新定义。

红色无效预览使用当前方向的 clamped 投影、当前 Width/Height/Rotation；它是解释性本地反馈，不是可提交候选。

## 6. 确定性与复杂度

- 使用平方欧氏距离 `dx²+dy²`，不调用开方；排序结果与欧氏距离相同。
- 浮点距离完全相等时按 Y、X；不得依赖集合迭代顺序。
- 不使用随机数、时间、帧率或 Unity 状态。
- 最坏检查量为两个方向的全部左上角与 footprint 占用检查；实际常见空地在第一级一次命中。

## 7. 生产零分配要求

生产 C# `Evaluate`：

- 为纯同步方法，不持有输入或返回可变集合。
- 不使用 LINQ、迭代器、闭包、装箱、字符串构造或临时候选集合。
- 只用局部值类型变量和嵌套循环，直接维护当前最佳候选。
- `PlacementCandidateInput` 与 `ItemPlacementPreview` 保持 readonly struct；Occupancy 由调用方复用只读快照。
- 必须用 Release 构建的分配测试验证热身后连续调用的线程分配增量为 0；HTML 原型不能作为零 GC 证据。

## 8. 表驱动场景

- `1×1`：光标位于四格交点且半格向正方向得到的局部投影格被占用；扩大搜索对其余等距候选按 Y/X 选择左上。
- `1×4 / 1×5`：右下边缘 clamp；手动旋转后 footprint 互换。
- `2×3` 空地：任意平移均保持 `2×3 current-local`，不得自动翻转。
- `2×3` 中央障碍：局部 2×3 被挡、局部 3×2 可放，返回 `automatic-90-local`。
- `1×4` 仅旋转可容纳：局部失败且当前全局无解后找到 4×1。
- `3×3` 满网格：`LocallyInvalid/Occupied`。
- 物品两方向都大于容器：`LocallyInvalid/OutsideGrid`。
- 自动旋转关闭：绝不检查旋转方向。

## 9. 前端消费

Gemini-01 只按 `State / Candidate / Width / Height / Reason` 渲染，不重新计算方向或格位。`Candidate.Rotation` 是最终预览方向；方向来源标签仅用于原型和诊断，不进入玩家 UI。

切换旋转是确定性阶梯结果，不需要时间滞回阈值。若真实游戏运行仍出现边界抖动，应先核查光标坐标和 occupancy 快照稳定性，再决定是否另开平滑票，不能私自改变本算法优先级。

## 10. 证据边界

交互 HTML 与 Node 行为烟测证明候选逻辑可表达，并经独立静态审计。尚未建立生产 C#、Release 零分配、Unturned UI、SP、SteamP2PFriends 或 U3DS 运行证据。

## 11. Revision 2026-09-02：边缘感应带（edge-rot 升级，实机反馈驱动）

R5 实机复现：单列 `LongSideHugsEdge` 触发过窄、横武士刀经 edge-rot 转横后重抓造成"单向粘滞"。人工开发者经 `/grill-with-docs` 拍板（Q-A~Q-D + Q1-Q6，完整决议见 `docs/adr/0003-bue-auto-rotation-edge-fit-decision.md`）。本 Revision 是 §4 Local-Fit Priority 阶梯的**受控扩展**，不改变阶梯①/③/④与防蠕动否决：

1. **触发基准（§4 阶梯①扩展）**：光标处于**边缘感应带**内时，即使当前方向局部能放下，也允许评估旋转方向并返回 `automatic-90-edge` 候选，前提是满足**物理容纳守卫**（`rotatedFitsGrid && Fits(rotatedX, rotatedY, ...)`）。带外仍走原阶梯①（当前方向能放立即返回）。
2. **感应带定义**：带宽 `band(dim) = clamp(1.0f, dim * 0.15f, 2.0f)`（格），**按轴独立**——竖向感应带（左/右壁）用 `containerWidth` 计算，横向感应带（上/下壁）用 `containerHeight` 计算。光标网格坐标满足 `cursorGridX < band(containerWidth)` 或 `cursorGridX >= containerWidth - band(...)` → 竖向带；`cursorGridY < band(containerHeight)` 或 `cursorGridY >= containerHeight - band(...)` → 横向带。
3. **长边贴边**：竖向带内倾向返回竖长边候选（长边贴左/右壁），横向带内倾向返回横长边候选（长边贴上/下壁）；障碍整行/整列（`RowFullyBlocked`/`ColumnFullyBlocked`）挡出的空位边界与容器外壁等价。
4. **角落裁决**：同时处于竖向带与横向带的重叠区（如左下角），保持当前进入姿态（防抖动）；重叠区外由感应带平滑接管。交互预期：横长武器在左下角贴底边，鼠标上提离开横向带进入竖向带 → 翻转为竖长边贴左壁；下拉回底带 → 切回横放。
5. **D2 红线**：开阔正中部（远离任何感应带）严格保持当前方向，不引入双方向竞争、不强制自愈翻转。
6. **零分配**：感应带判定使用局部值类型与 `clamp` 常量计算，不引入分配；满足 §7 生产零分配要求。
7. **红测锚点**：`--dev16d-r13-symrot-wide-red`（D2 守卫，开阔中部保持横，断言不翻转）、`--dev16d-r13-edge-rot-red`（感应带左/右壁转竖）、`--dev16d-r13-corner-lift-red`（左下角上提转竖贴左壁）。
