# GPT-Wayfinder-Joint-Consistency-Review：前后端 Wayfinder 联合一致性复审

**作者: GPT**  
**日期: 2026-08-24**  
**输入报告:** `Backend-Consistency-Review.md`、`handoffs/Frontend-Wayfinder-Consistency-Review.md`  
**最终联合复审判定: PASS（JCR-01～09 全部 resolved，独立终审阻断项 0）**

## 1. 复审方法与证据边界

两份报告不是互相背书的充分证据。本次把报告中的每个关键结论重新对照当前磁盘文件：`map.md`、GPT/Gemini canonical specs、GPT-01～17、Gemini-01～03、GPT-14 Definition Pipeline 和现有原型。

本报告只裁定文档一致性。当前仍不存在生产工程、候选 DLL、SP/P2P/U3DS 运行证据或 Stable ABI。

## 2. 已确认一致的部分

- 产品范围、前后端所有权和“更好的物品交互”首发参考实现一致。
- 库存提交沿用原版 `sendDragItem → ReceiveDragItem`，前端零乐观库存写入。
- GPT-12 Local-Fit Priority 与 GPT-13 `AwaitingProjection` 2 秒视觉预算一致。
- 静态 Settings facet + 运行时 `FeatureSettingsSnapshot` 的目标模型一致。
- 模块局部隔离与核心 SafeMode 一致；JCR-09 已补齐 U3DS UI 结构隔离要求，但真实 U3DS 加载与运行仍待生产阶段验证。
- GPT 后端报告已通过独立终审，且没有把构建、原型或运行证据混为一谈。

## 3. 首次联合复审历史阻断项

下表保留首次联合复审发现的完整问题谱系，不表示它们当前仍全部 open。当前状态见表后矩阵；第二次联合复审当前唯一 open 项为 JCR-07。

| 编号 | 严重度 | 发现 | 当前事实 | 必须修订方 |
| --- | --- | --- | --- | --- |
| JCR-01 | 阻断 | Gemini 报告把不存在的 `spec.md` 列为 Canonical，并只审到 GPT-15。 | canonical 目录无 `spec.md`；当前已有 GPT-16 及本次 GPT-17，且 GPT-14 后端时序刚完成六轮收口。 | Gemini 报告 |
| JCR-02 | 阻断 | Gemini 删除测试宣称静态门禁“证实 U3DS 正常加载并启动”。 | GPT-14 只冻结未来门禁；无 DLL、无真实 U3DS 日志。 | Gemini 报告 |
| JCR-03 | 阻断 | Gemini 把 readonly struct、对象池设计和算法结构判为生产每帧 0 GC PASS。 | GPT-12 明确要求未来 Release C# 分配测试；Node/HTML 8/8 只证明原型行为。 | Gemini 报告与前端规格 |
| JCR-04 | 阻断 | Gemini 报告把 Gemini-01～03 标为 Ready、Fog 全清；磁盘票据仍为 `Status: open`。 | open 票据仍包含未同步的旧 interface 和待实现/验证内容。 | Gemini 报告、Gemini-01～03 |
| JCR-05 | 阻断 | Gemini 前端规格 §1/§2 和 Gemini-02 仍把运行时 `IFeatureSettings` 当作描述来源。 | GPT-14 已废弃运行时 `Describe()`；唯一模型是静态 Settings facet + 动态 snapshot。 | Gemini 前端规格、Gemini-02 |
| JCR-06 | 阻断 | Gemini §4.2/§5 把 `ISettingsUiService` 和携带 `ISleekView`/`PlayerDashboardInventoryUI` 的 `IClientUiFeatureExtension` 描述为第三方运行时公共 SPI。 | V1 贡献以源码合入单 DLL；ClientUi 类型 internal、生成显式注册表，无外部 feature DLL 动态加载。任何对外 UI ABI 都需独立共享契约决策，当前未获批准。 | Gemini 前端规格、Gemini-03 |
| JCR-07 | 阻断 | “保持抓取偏移”与 evaluator 将输入网格坐标视作物品几何中心并存，但没有坐标 adapter。 | 若直接把原始 pointer grid 传给 evaluator，悬浮图标与 footprint 会错位。前端必须计算 `intendedCenter = pointer + (itemCenter - grabOffset)` 后再调用 evaluator；共享 DTO 名称/文档需避免把原始 pointer 与候选中心混淆。 | GPT 共享契约 + Gemini 前端规格 |
| JCR-08 | 阻断 | Gemini 报告称 GPT-09 为“7 态”、GPT-11 为“4 阶段”，并把原生具体事件与单人路径写成当前插件已证实保证。 | `FeatureState` 当前 9 个值；GPT-11 canonical 术语为三阶段应用握手，内部有 Hello/Snapshot/Ack/Ready 消息顺序；单人只冻结同一纯裁定器，不等于已实现。`onInventoryAdded/onInventoryRemoved` 已有 GPT-07 固定源码静态证据，但本插件 adapter、当前目标版本 IL 对齐及真实运行仍待验证。 | Gemini 报告 |
| JCR-09 | 阻断 | Gemini 前端规格称仅把 UI 代码放在 `!Application.isBatchMode` 门禁后即可“确保 U3DS 绝不加载或解析 UI 类型”。 | 运行门禁只是必要条件。还必须满足 `CoreShared/ClientUi` 源码闭包、签名/基类/attribute/static initializer/type-token 隔离、生成显式注册表、最终 DLL 可达 IL 审计与真实 U3DS 验证。 | Gemini 前端规格、Gemini-03 |

### 3.1 当前状态矩阵

| JCR | 当前状态 | 说明 |
| --- | --- | --- |
| 01～06 | resolved | Gemini 已实际修订，GPT 已逐项回查。 |
| 07 | resolved | 旋转方向正确；grab offset 闭区间与 evaluator center 半开有效域已分离，并通过独立终审。 |
| 08～09 | resolved | 术语/证据层级与 U3DS 结构隔离均已修订；运行证据仍按后续门禁采集。 |

## 4. JCR-07 坐标 seam 的联合裁定提案

为同时满足“抓住物品任意位置时图标不跳”和“候选按物品几何中心投影”，前端应拥有一个纯坐标 adapter：

```text
pointerGrid
+ (currentFootprintCenter - grabOffsetInFootprint)
= intendedItemCenterGrid
→ IPlacementCandidateEvaluator
```

- `grabOffsetInFootprint` 在拖拽开始时冻结，并随手动旋转按相同局部坐标规则变换。
- evaluator 只接收 `IntendedItemCenterGridX/Y`，不读取屏幕 pointer、UI Scale 或像素坐标。
- 前端悬浮图标继续按原始 pointer + grab offset 渲染；占据框按 evaluator 返回的 Candidate 渲染。
- 自动旋转只改变候选 footprint/rotation，不偷偷重写玩家抓取点；旋转后的视觉抓取点变换细节由 Gemini 规格明确。

该段保留为 JCR-07 历史提案记录。当前最终裁定为：Gemini 已复核旋转时 grab offset 的具体变换；Draft 继续保留共享字段名 `CursorGridX/Y`，但其语义永久冻结为 intended item center。是否在首次 Stable ABI 前机械重命名为 `IntendedCenterGridX/Y` 不阻塞 Wayfinder，也不改变语义。

## 5. 必须保持的证据措辞

- “设计要求”“静态门禁”“待验证”不能写成运行 PASS。
- Node 8/8 是 GPT-12 原型回归，不是生产 TDD、C# 零分配或游戏视觉验收。
- U3DS 类型隔离规则是构建/静态/运行验收计划，不是当前已加载证据。
- Gemini-01～03 可写“设计输入已解阻”，但在票据状态实际改变前不能写成 resolved/ready canonical fact。

## 6. 阶段裁定

首次联合复审的 JCR-01～09 均已由 Gemini 实际修订并经 GPT 回查。JCR-07 的 grab offset 统一为闭区间 `[0,W]×[0,H]`；evaluator 的 intended item center 容器有效域独立统一为半开区间 `[0,containerWidth)×[0,containerHeight)`，两者不得混用。旋转规则为 forward/native `rot+1=(H-gy,gx)`，backward/native `rot-1=(gy,W-gx)`，旋转后重算 intended item center。

冻结源码 `PlayerDashboardInventoryUI.updateDraggedItem()` 的 `dragJar.rot++` 与 `updatePivot()` 支持上述方向；Gemini 前端规格、Gemini-01 和前端复核报告均已同步正确公式、四角与中心映射。

边界区间冲突已在 GPT 共享契约与算法规格中修复并通过独立终审。完整 Wayfinder 决策包达到文档与静态设计一致性 PASS，阻断项 0。生产构建、生产 C# 零分配、SP、SteamP2PFriends、U3DS 和发布授权仍未验证；进入 `/to-spec` 仍需人工开发者明确授权。

## 7. 第二次联合复审对账

| JCR | GPT 实际复验结果 |
| --- | --- |
| 01 | `spec.md` 虚假引用已删除；GPT-16/17 纳入矩阵。 |
| 02 | U3DS 已回退为未来构建/运行门禁，无运行 PASS 宣称。 |
| 03 | Node/HTML 8/8 与对象池均限定为原型/设计，生产 C# 零分配待验。 |
| 04 | Gemini-01～03 均有 Answer、证据边界并真实改为 resolved。 |
| 05 | 前端 canonical 已统一静态 Settings facet + 动态 snapshot，无运行时 Describe 残留。 |
| 06 | V1 UI 扩展改为源码贡献、internal ClientUi 类型和生成注册表，无外部运行时 ABI。 |
| 07 | **resolved**：旋转公式、grab offset 闭区间、evaluator center 半开域及 intended center 重算顺序均通过终审。 |
| 08 | 9 态、三阶段应用握手和库存事件证据层级已修正。 |
| 09 | batchmode 被降为必要门禁，并补齐源码闭包、type-token、注册表、IL 审计与真实 U3DS 验证。 |

