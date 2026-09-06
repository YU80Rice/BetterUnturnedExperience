# RT-04 研究交付与独立审计报告

## 【需求执行概述】

基于 `BUE-SS-20260824-01` 完成 Unturned 原生库存权威链、容器状态、投影事件与线程边界的只读源码研究；未编写生产后端，未修改共享契约。

## 【源码溯源清单】

- 四环境权威链、RPC 身份与限速 → `RT-04-Inventory-Authority-Container-Research.md` §3～4。
- 原生验证、mutation 顺序与 callback 风险 → 同报告 §5～7。
- storage/container session、授权与失效 → 同报告 §8。
- 线程边界、内部 adapter seam 与拒绝方案 → 同报告 §9～12。
- 运行证据义务与未决项 → 同报告 §13～14。

## 【变更清单】

- 新增 `.scratch/better-unturned-experience-architecture/RT-04-Inventory-Authority-Container-Research.md`。
- 新增 `.scratch/better-unturned-experience-architecture/handoffs/to-RT-04-review.md`。
- 更新 RT-04 票据为 `ready-for-human`，并更新 `map.md` frontier。
- 未修改任何生产源码或 RT-01 共享契约。

## 【验证记录】

- U3-SDK commit：`ea7b4973af5ba10f62baad2bfde36ab2e5b060eb`。
- 报告登记的 14 个 U3-SDK 源文件 SHA-256 均由主 Agent 重新计算并匹配。
- 最终研究报告 SHA-256：`E09215C5171A4A5C9B537C9C6D281E1F9C117A9B3369B8F033FC735DF8B70453`。
- 本任务为只读研究，无生产构建目标；没有用 build 代替 SP、P2P 或 U3DS 运行证据。

## 【子智能体审核记录】

| 轮次 | 判定 | 结果 |
| --- | --- | --- |
| 1 | FAIL | 修正 generated reader 参数读取检查的条件编译边界；补齐 `onStateUpdated/onInventoryStateUpdated` 不安全 callback。 |
| 2 | PASS | 两项阻断闭环；SourceSet、哈希、四环境证据分类、无生产实现与无契约改写均通过。 |

## 【偏离与妥协说明】

无需求偏离。独立 U3DS DLL、SteamP2PFriends Host 角色、线程身份及投影时序仍按证据规则标为 `UNRESOLVED`，没有越界宣称运行 PASS。

## 【剩余门禁】

Gemini 尚需确认原生投影与 `AwaitingProjection` 的消费条件完整且无推断性回滚。因此 RT-04 当前为 `ready-for-human`，不能标记 `resolved`，RT-06 仍受其阻塞。



