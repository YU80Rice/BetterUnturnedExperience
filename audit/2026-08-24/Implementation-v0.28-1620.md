# Requirements Specification 发布报告 - v0.28

## 【需求执行概述】

把已通过联合复审的 Wayfinder 决策包转换为三层统一需求规格，并发布到本地 issue tracker。

## 【源码溯源清单（Traceability Matrix）】

| 用户要求 | 规格落点 |
| --- | --- |
| 前后端共同函数名与逻辑 | `spec.md` Layer 1 |
| Gemini 前端 U3-SDK 调研 | `spec.md` Layer 2 |
| GPT 后端 U3-SDK 调研 | `spec.md` Layer 3 |

## 【代码变更清单】

- 新增 canonical `spec.md`；独立审核后暂改为 `needs-info`，等待测试 seam 明确确认。
- 更新 Wayfinder map，记录已进入 Requirements Specification 阶段。
- 无生产代码或 DLL 变更。

## 【编译验证记录】

- 生产编译：N/A；当前仍无生产工程。
- 本轮验证对象为需求覆盖、接口一致性和证据边界。

## 【子智能体审核记录】

第一轮判定：FAIL。

- 共享接口缺少精确返回类型、Bootstrap 属性类型和消息方向/kind/关联字段；已补齐。
- `/to-spec` 的测试 seam 需要人工明确确认；确认前规格保持 `needs-info`。

## 【偏离与妥协说明】

没有提前创建实现票；具体拆票留给 `/to-tickets`。

## 【测试建议】

后续 tickets 必须保留共享契约先行、前后端调研可并行、共同 interface 变更双端复核的依赖关系。
