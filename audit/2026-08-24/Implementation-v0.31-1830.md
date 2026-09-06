# `/to-tickets` 研究任务分发报告 - v0.31

## 【需求执行概述】

依据已通过审计的双语需求规格，将共享契约、Gemini 前端 U3-SDK 调研、GPT 后端 U3-SDK 调研和联合收敛拆为六张可领取的本地任务票，并冻结负责人、复核人、阻塞边和生产授权边界。

## 【源码溯源清单（Traceability Matrix）】

| 规格层/需求 | 任务票 |
| --- | --- |
| 共享函数、消息、状态、坐标、算法和证据词汇 | RT-01 |
| Gemini 物品拖动、坐标、Glazier、投影与 hook 调研 | RT-02 |
| Gemini 设置 UI、状态投影、生命周期清理与 Headless 隔离 | RT-03 |
| GPT 原生库存权威、验证、修改、容器会话和线程 | RT-04 |
| GPT BepInEx、LMN、设置、生命周期与双端引用交集 | RT-05 |
| 双端冲突解决、契约 change request 与实施就绪 | RT-06 |

## 【变更清单】

- 新增 `RT-01`～`RT-06` 六张 Markdown 任务票。
- 更新架构 `map.md`，登记任务分发、Owner、阻塞关系和当前 frontier。
- 使用 `RT-` 标识避免与已完成的 GPT/Gemini Wayfinder 决策票混淆。

## 【验证记录】

- 生产编译：N/A；本轮仅发布研究与契约任务，不包含生产代码。
- 结构检查：六票均包含 Owner、Required reviewer、Blocked by、`ready-for-agent`、Acceptance、Verification 和 Comments。
- 阻塞图：`RT-01 → {RT-02, RT-03, RT-04, RT-05} → RT-06`，无环；RT-01 是唯一 frontier。
- 链接检查：任务地图中的六个相对链接和既有 Markdown 链接均有效。

## 【子智能体独立审核记录】

### 第 1 轮：FAIL

1. RT-01 缺少 Required behavior、单向依赖、单一定义产物和五个 V1 禁用库存插件消息。
2. RT-02/03 缺少部分输入焦点、客户端 reference 差异、Harmony fallback 条件、补丁兼容/排序边界及调用链固定列。

已逐项补齐。

### 第 2 轮：PASS

- 阻断项：无。
- 六票完整覆盖三层规格且未授权生产编码。
- Owner/reviewer 符合 GPT 后端与共享契约、Gemini 前端的职责分配。
- DAG、frontier、人工生产授权门禁和证据边界正确。

## 【偏离与妥协说明】

无需求偏离。任务文件采用 `RT-01`～`RT-06`，而不是复用数字 `01`～`06`，是为了避免覆盖同一 effort 下已存在的 Wayfinder 决策票。

## 【后续执行建议】

1. GPT 先领取并完成 RT-01。
2. RT-01 通过 Gemini 与独立审计后，同时释放 RT-02～RT-05。
3. RT-06 完成且人工再次授权前，不发布或领取生产实现票。

## 【最终结论】

任务分发已完成，独立审计 PASS；当前只允许 GPT 开始 RT-01。
