# 后端与共享契约规划交付报告 - v0.4

## 需求执行概述

完整阅读 Gemini 前端设计与回传，对齐“更好的未转变者体验”前后端 seam，交付 GPT 共享契约与后端架构 Draft，并更新 GPT 决策票及双目录地图索引。

## 源码溯源清单（Traceability Matrix）

| 用户要求 | 落实位置 |
| --- | --- |
| ItemGridPosition、ItemPlacementIntent、ContainerReference | `Shared-Contract-Spec.md` 第 3 节 |
| Commands / Events 与序列化 | 同文件第 4、5 节 |
| 版本兼容与错误码 | 同文件第 5～7 节 |
| 服务端权威网格判定 | `Backend-Architecture-Spec.md` 第 3.3、3.4 节 |
| 防刷、并发与主线程 | 同文件第 4、5 节 |
| Harmony 安全策略 | 同文件第 3.4、10 节 |
| 设置持久化 | 同文件第 3.2、8 节 |
| 后端决策票 | 规范目录 GPT-08～GPT-15 |
| 地图索引 | 唯一地图及 Gemini 历史工作区 map |

## 变更清单

- 新建 `Shared-Contract-Spec.md`。
- 新建 `Backend-Architecture-Spec.md`。
- GPT-08 置为 resolved，并链接共享契约规格。
- GPT-15 根据 Gemini 回传置为 resolved，并记录接受、拒绝、延后及票据依赖。
- 更新唯一规范地图与 Gemini 历史工作区地图索引。
- 未创建生产源码、DLL 或发布资产。

## 核心架构裁定

1. 最终库存提交继续使用原版 `sendDragItem → ReceiveDragItem`；服务器重新校验，客户端候选无授权性。
2. Gemini 原提案的自定义库存 Command、逐次 committed/rejected 网络 Event 不进入 V1。
3. LMN 只作为能力、服务器权威设置和诊断 adapter，不承担认证、授权、事务或库存权威。
4. 候选算法通过无状态 `IPlacementCandidateEvaluator` seam 供前端消费。
5. 拖拽状态固定为 `Idle → Dragging → Hovering → Dropping → AwaitingProjection`，使用 `DragGeneration` 淘汰过期结果。
6. 设置消息包含关联 RequestId、changed/rejected 结果、revision 与字段级编码；观察者广播使用 RequestId=0。
7. 能力协商消息仅保留编号，等待 GPT-11 后启用。

## 编译与验证记录

- 当前仍无生产工程或编译清单，本轮为规格和决策工作，没有可执行编译命令。
- `git diff --check`：通过。
- 两份规格均明确标记为 Draft、实现与运行未验证。
- Gemini 已通过回传接受共享 evaluator、原版提交链和废弃自定义库存 RPC。

## 子智能体审核记录

| 轮次 | 判定 | 阻断与修复 |
| --- | --- | --- |
| 1 | FAIL | 发现 Gemini 回传遗漏、公开类型不完整、拖拽状态未闭合、设置线路不完整、完成度表述过高；已补齐 |
| 2 | FAIL | 发现过期状态、能力消息过早启用、广播 RequestId 歧义、尾字段规则冲突；已修复 |
| 3 | PASS | 类型、状态机、线路、唯一事实源和证据边界全部通过 |

## 偏离与妥协说明

- 用户任务预设 LMN 自定义库存 Command/Event；依据原版源码权威链和双方复核，V1 明确拒绝该路径，改为原版提交与库存投影观察。
- 规格按用户指定路径写入 Gemini 历史工作区，但唯一事实源仍为规范目录地图；旧目录不重新成为第二事实源。
- “闭环一致”仅指 GPT/Gemini interface 基线一致，不代表 GPT-09～13 全部完成或三环境运行通过。

## 后续建议

1. GPT-09：冻结模块状态转换和隔离阈值。
2. GPT-10：冻结设置不适用字段、schema 与持久化格式。
3. GPT-11：定义并启用能力协商 DTO。
4. GPT-12：原型验证候选算法及自动旋转手感。
5. GPT-13：冻结无逐次 ACK 条件下的视觉超时策略。

