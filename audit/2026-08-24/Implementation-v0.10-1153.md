# GPT-12 物品候选算法原型阶段报告

> **SUPERSEDED / 历史阶段证据**：本文记录 GPT-12 原型演进过程，其中“双方向按距离竞争”已被人工开发者与 Gemini 联合否决，不再是现行算法。最终决策以 `Implementation-v0.10-1221.md` 与 `.scratch/better-unturned-experience-architecture/Item-Placement-Algorithm-Spec.md` 的 Local-Fit Priority 为准。

## 【需求执行概述】

为“定义模糊落点与自动旋转算法规格”建立一次性可交互 HTML 原型，供人工与 Gemini 实际体验后决定是否冻结算法。

## 【需求映射】

| 前端输入 | 原型落实 |
| --- | --- |
| 无状态纯计算 | `PlacementLogic.evaluate` 不访问 DOM；页面壳只负责输入和渲染 |
| 几何中心与边缘靠齐 | 候选中心到浮点光标排序；合法位置天然限制于边界 |
| 完整预览状态输出 | Candidate / LocallyInvalid / Hidden 及候选、尺寸、旋转、Reason |
| 自动旋转优先级 | 当前方向与 90° 方向共同按距离竞争；更近者获胜，完全同距保持当前方向 |
| 确定性排序 | 中心平方欧氏距离 → Y → X |
| 尺寸矩阵 | 1×1、1×4、1×5、2×3、3×3 引导场景 |

## 【变更文件】

- 新增 `.scratch/better-unturned-experience-architecture/prototypes/12-item-placement-logic-prototype.html`。

## 【验证记录】

- Node `vm.Script` 内嵌脚本语法检查：PASS。
- 浏览器自动化：本地 `file://` 被浏览器安全策略阻止，未执行视觉自动化；未绕过安全限制。
- 当前无生产 C# 工程，因此未执行 DLL 编译。

## 【独立审核】

### 第 1 轮：FAIL

1. 引导场景先渲染后设置光标。
2. 1×1 场景没有实际触发等距 Y/X 裁定。
3. `Occupied` Reason 永远不可达。

### 修复

- 所有场景在统一渲染前设置完整状态。
- 光标 `(4,3)` 制造四候选真实等距，预期选择 `(3,2)`。
- 至少一个已尝试方向尺寸可容纳但无合法格时返回 `Occupied`；所有尝试方向均尺寸不容纳时返回 `OutsideGrid`。
- 明确 HTML 不能证明 C# 零 GC，也不模拟真实 page 或 DragGeneration。

### 第 2 轮：PASS

- 审核员：`audit_gpt12_prototype`
- 阻断项：0

## 【证据边界】

本报告只证明原型脚本语法和逻辑审计通过。它不证明玩家手感已接受、不证明生产 C# 零分配，也不证明 Unturned、SP、SteamP2PFriends 或 U3DS 运行通过。

## 【当时阶段结论】

GPT-12 保持 `claimed`。必须由人工开发者或 Gemini 双击体验原型并反馈后，才能冻结算法、更新正式规格和关闭票据。

## 【人工体验后的自动旋转修订】

人工开发者指出旧规则会因远处仍存在当前方向候选，而忽略鼠标附近可容纳的旋转候选。算法已改为当前方向与 90° 方向共同参与距离排序：更近者获胜，完全同距才保持当前方向。

- 新场景：中央 3×3 障碍，2×3 物品，光标 `(3.5,1.1)`。
- 旋转 3×2 候选 `(2,0)` 距离平方 `0.01`。
- 当前 2×3 最近候选距离平方 `6.41`。
- 修订版独立审计：PASS，阻断项 0。
- 当前仍等待 Gemini 复核，不关闭 GPT-12。

