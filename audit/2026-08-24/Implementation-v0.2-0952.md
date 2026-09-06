# Wayfinder 架构规划执行报告 - v0.2

## 需求执行概述

为“更好的未转变者体验”绘制前后端职责与可扩展框架决策地图，确立 GPT 后端/共享契约所有权、Gemini 前端所有权，并以“更好的物品交互”作为首个参考实现。

## 源码溯源清单（Traceability Matrix）

| 用户决策 | 落实位置 |
| --- | --- |
| 开放、多创作者协作的原版体验优化平台 | `CONTEXT.md`；GPT-01 |
| GPT 后端和共享契约、Gemini 前端 | GPT-02；`map.md` Notes |
| 功能独立工程、最终单 DLL | GPT-03、GPT-06 |
| 模块故障隔离及前端失败提醒 | GPT-03、GPT-09、GPT-13 |
| 统一设置外壳与后端设置权威 | GPT-10 |
| 语义化公共 API 兼容 | GPT-03、GPT-08、GPT-11 |
| 自动旋转、不交换、失败保留原位 | GPT-04、GPT-12 |
| 单人、SteamP2PFriends、U3DS 独立验收 | GPT-14；`map.md` Out of scope |
| GPT/Gemini 文档前缀与固定文件例外 | `map.md` Notes；tracker 多作者规则 |

## 变更清单

- 新建并更新 `CONTEXT.md` 领域词汇表。
- 新建 `.scratch/better-unturned-experience-architecture/map.md` 唯一决策地图。
- 新建 15 张 `GPT-` 决策票：7 张已解决，8 张开放。
- 新建 3 份 `GPT-` 第一方证据研究报告：运行时基线、单 DLL 构建、库存权威调用链。
- 更新 `docs/agents/issue-tracker.md`，支持多作者 `<Author>-NN` 票据身份与依赖排序。
- 隔离并建立 GPT-15，用于后续审阅 Gemini 并行输入，未修改 Gemini 文件。

## 验证记录

- 本轮为规划与只读研究，不包含生产代码或可执行项目，因此无编译命令。
- `git diff --check`：通过。
- 决策票总数：15；已解决：7；开放：8。
- 当前无依赖前沿：GPT-08、GPT-09；按编号下一张为 GPT-08。

## 研究结论摘要

1. 客户端与 U3DS 当前 Unity 主版本相同，但 BepInEx 和 `Assembly-CSharp.dll` 基线不同；不能以客户端结果代替 U3DS。
2. 首版单 DLL 建议采用模块独立工程、模块自有共享源码清单、单一聚合工程一次编译；不采用 ILMerge。
3. 物品交互必须保持客户端候选预览与原版 `sendDragItem → ReceiveDragItem` 服务端权威提交边界。
4. 所有静态研究均明确保留双端加载及单人、SteamP2PFriends、U3DS 运行时未验证边界。

## 子智能体审核记录

| 轮次 | 判定 | 结果 |
| --- | --- | --- |
| 1 | FAIL | 发现 Fog 重复、tracker ID 规则、未启动 GPT-06、GPT-14 重复决策、Gemini 双事实源五项阻断 |
| 2 | PASS | 五项均修复；依赖可解析，无明显循环，职责、前缀和证据边界一致 |

## 偏离与妥协说明

- `map.md` 与 `CONTEXT.md` 按技能要求保留固定名称并注明 GPT 作者；其他 GPT 产物使用 `GPT-` 前缀。
- Gemini 已存在目录未被删除或覆盖，仅被标记为待审阅输入，避免越权修改并行作者产物。
- Wayfinder 阶段没有实现生产代码，也没有宣称任何运行环境已经通过。

## 后续建议

1. 下一次调用 Wayfinder 时处理 GPT-08“定义公共框架最小契约面”。
2. 随后处理 GPT-09“定义模块生命周期与故障隔离状态机”。
3. 等 GPT-08、GPT-09、GPT-10、GPT-12 完成后处理 GPT-15，与 Gemini 前端方案正式对齐。
4. 地图清空后再进入 `/to-spec`，不可直接跳到实现。
