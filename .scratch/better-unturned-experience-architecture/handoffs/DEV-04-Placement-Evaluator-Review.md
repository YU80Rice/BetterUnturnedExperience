> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-DEV-04-Placement-Evaluator-Review：DEV-04 候选评估器前端消费复核报告

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核对象**: DEV-04 交付物（`PlacementCandidateEvaluator`, `BetterUnturnedExperience.Placement.Tests`, `Implementation-DEV-04-0010.md`）  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **判定结论**: **ACCEPT（全量通过，无异议，无契约缺口，正式签署验收 DEV-04）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端对齐与算法核查说明 |
| :--- | :---: | :--- |
| **1. Local-Fit Priority 阶梯** | **`ACCEPT`** | 严格实现四级搜索：`current-local` $\to$ `automatic-90-local` $\to$ `current-expanded` $\to$ `automatic-90-expanded`；彻底消除空旷平移时的翻转蠕动，障碍狭缝就地旋转。 |
| **2. JCR-07 Forward 旋转变换** | **`ACCEPT`** | 顺时针旋转公式计算为 `(rotation + 1) & 3`，奇偶当前旋转下的宽高翻转完全正确，四向旋转闭环。 |
| **3. 零分配热路径 (0 GC)** | **`ACCEPT`** | 10,000 次热路径连续评估在单元测试中实测内存分配量**精确为 0 字节**，完全满足 60 FPS 连续拖拽的性能指标。 |
| **4. 边缘 Clamp 与失败状态** | **`ACCEPT`** | 光标超出容器半开域 $[0, W) \times [0, H)$ 返回 `Hidden` (`OutsideGrid`)；全满或超大物品返回 `LocallyInvalid` (`Occupied` / `OutsideGrid`)。 |
| **5. 零类型泄漏 (Headless 强隔离)** | **`ACCEPT`** | `PlacementCandidateEvaluator.cs` 仅引用 `System` 与 `Contracts`，对 `Unity`、`Glazier`、`Sleek`、`LMN` 及原生类型的引用数**精确为 0**。 |

---

## 二、 算法核心与前端呈现 Seam 核对

### 1. 坐标投影（`Project`）与 Seam 契合度
* **数学换算**：
  ```csharp
  var rawX = (int)Math.Floor(cursorX - width / 2f + 0.5f);
  var rawY = (int)Math.Floor(cursorY - height / 2f + 0.5f);
  ```
  输入 `cursorX/Y` 承载 Presenter 计算出的 `intendedItemCenterGrid`。
  当物品尺寸为奇数或偶数时，该四舍五入公式精准对齐格位几何中心，Clamp 范围严格限制在 $[0, \text{gridWidth} - \text{width}] \times [0, \text{gridHeight} - \text{height}]$。

### 2. 前端 Presenter（DEV-05）消费就绪
* 在后续 `DEV-05` 中，`InventoryDragPresenter` 每帧 Tick：
  1. 捕获鼠标并计算 `intendedItemCenterGrid`。
  2. 构造 `PlacementCandidateInput` 传入 `evaluator.Evaluate(input)`。
  3. 直接消费返回的 `ItemPlacementPreview`：
     - 若 `State == Candidate`：驱动 `SleekInventoryFootprintLayer` 绘制绿色占据框（坐标 `Candidate.X/Y`，尺寸 `Width/Height`）。
     - 若 `State == LocallyInvalid`：绘制红色不可用框（坐标 `Candidate.X/Y`）。
     - 若 `State == Hidden`：隐藏占据图层。
  4. 松开鼠标时，若 `State == Candidate`，直接取 `Candidate` 的 `(Page, X, Y, Rotation)` 提交原生 `sendDragItem`。
* **契约与数据流 100% 顺畅闭环！**

---

## 三、 结论与后续推进

1. **无契约缺口**：纯 C# 算法与契约接口完全吻合，无需发起任何 Shared Contract Change Request。
2. **正式通过验收**：Gemini 正式签署对 `DEV-04` 的全量验收与复核通过。
3. **后续推进**：
   * `DEV-01`（骨架与契约）、`DEV-02`（Definition Linker）、`DEV-03`（SettingsRuntime）与 `DEV-04`（Evaluator 纯算法）已全量闭环。
   * **前端已完全准备就绪，同意开启 `DEV-05`（Gemini ClientUi / Glazier 表现层集中实现）！**

---

*报告完。作者: Gemini*


