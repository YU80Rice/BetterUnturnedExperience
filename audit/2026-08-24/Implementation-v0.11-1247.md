# GPT-13 Wayfinder 决策交付与独立审计报告

## 【需求执行概述】

人工开发者接受 GPT-13 七项政策后，完成公共框架与“更好的物品交互”的前后端交接、失败、超时、降级和通知规则闭环。

## 【源码溯源清单（Traceability Matrix）】

| 接受项 | 落实位置 |
| --- | --- |
| 2 秒 AwaitingProjection 视觉等待 | `.scratch/better-unturned-experience-architecture/Frontend-Backend-Handoff-Spec.md` §3.1 |
| 原生投影四条件关联 | 同上 §3.2 |
| 迟到投影不回滚、不污染新 generation | 同上 §3.3 |
| 设置 3 秒重试一次、总计 8 秒请求快照 | 同上 §4.2；`Shared-Contract-Spec.md` 的 `0x0104` |
| 网络缺失/超时默认静默 | 同上 §5；`Network-Capability-Versioning-Spec.md` |
| 模块/UI 状态矩阵 | 同上 §6—7 |
| 通知按 FeatureId/ErrorCode/DiagnosticId 去重 | 同上 §8 |

## 【文档变更清单】

- 新增 `Frontend-Backend-Handoff-Spec.md`。
- 新增 `handoffs/to-13-handoff-and-failure-contract.md`。
- 关闭 `issues/13-frontend-backend-handoff.md` 并更新 `map.md`。
- 扩充 `Shared-Contract-Spec.md`：新增 `RequestModuleConfigSnapshotCommand`、`CoreRuntimeState`、`CoreRuntimeStatusView/Event` 与 `0x0104`。
- 同步 GPT-09/10/11、后端规格中的 GPT-13 后续引用。
- 未越权修改 Gemini 所有权文档；在交接中明确要求 Gemini 回写其第 3.5 节旧候选措辞。

## 【验证记录】

- 文档存在性与非空检查：PASS。
- 关键符号/旧冲突措辞检索：PASS。
- 本次为 Wayfinder 文档决策，无生产工程与构建命令；未进行 DLL 编译，不得视为生产构建或运行验证。

## 【子智能体审核记录】

| 轮次 | 判定 | 阻断项与处理 |
| --- | --- | --- |
| 1 | FAIL | 未定义稳定视图、缺少快照请求线路、通知冲突、Gemini 旧候选冲突；逐项修订 |
| 2 | FAIL | Core SafeMode 无可消费稳定 DTO；新增进程内核心状态投影 |
| 3 | FAIL | Shared Contract 未登记 `CoreRuntimeState`；补齐完整枚举 |
| 4 | PASS | 阻断项 0；未发现新增契约、竞态、权威或证据边界问题 |

## 【偏离与妥协说明】

无需求偏离。为保证 8 秒快照恢复可实现，新增一条最小、有界、只读的 `0x0104` 请求线路；它不修改设置或 revision，并具备固定限流。

## 【后续建议】

1. Gemini 按交接文件回写前端规格第 3.5 节。
2. 推进 GPT-14：开放贡献目录、清单 schema、CI、审批与三环境发布门禁。
3. Wayfinder 完成前不进入生产代码、DLL 打包或正式版本声明。

## 【最终结论】

GPT-13：PASS（独立审核通过）。当前结论仅为 Wayfinder 决策闭环；SP、SteamP2PFriends Host/Client、U3DS 均未进入运行验收。



