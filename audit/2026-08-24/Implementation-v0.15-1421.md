# 后端与共享契约一致性复核执行报告 - v0.15

## 【需求执行概述】

修复第一轮独立审核发现的 Runtime Admission 与 Lifecycle 状态双写问题，并重新执行一致性审计门禁。

## 【源码溯源清单（Traceability Matrix）】

| 阻断项 | 修复位置 |
| --- | --- |
| Admission 与 Lifecycle 双写 `Incompatible/Disabled` | `Contribution-Build-Release-Gates-Spec.md` §12 |
| 当前 FeatureState 事实所有权不清 | `Feature-Definition-Pipeline-Spec.md` §2 |
| 被拒绝功能状态投影缺少单写者 | `Module-Lifecycle-Isolation-Spec.md` §2 |
| 后端启动时序未表达完整批次 | `Backend-Architecture-Spec.md` §3.1、§9 |
| 正式复核报告缺少修复轮次 | `Backend-Consistency-Review.md` |

## 【代码变更清单】

- 引入不可变 `AdmissionEvaluationBatch` 作为 Admission 到 ModuleRuntime 的唯一交接结果。
- Runtime Admission 只返回 decision 与 admitted handle，不写生命周期状态。
- ModuleRuntime/Lifecycle 独占 `FeatureState`、`StateRevision` 和状态事件写入。
- 所有功能先建立 `Discovered`；拒绝 decision 转为 `Incompatible/Disabled`，成功项才进入 `Starting`。

本轮无生产代码、项目文件或 DLL 变更。

## 【编译验证记录】

- 生产编译：N/A；当前仓库没有生产工程或 build command。
- 文档静态复扫：Admission/Lifecycle 单写者措辞已在四份 canonical 规格一致出现。
- Markdown 相对链接检查：0 个失效链接。
- GPT-12 原型回归沿用本轮同一文档修改链上的 8/8 PASS；仅为原型证据。

## 【子智能体审核记录】

| 轮次 | 判定 | 结果 |
| --- | --- | --- |
| 1 | FAIL | Admission 与 Lifecycle 双写状态；已修复 |
| 2 | FAIL | `RejectDisabled` 无 admitted handle，无法支持 `Disabled → Starting`；本报告保留为第二轮失败记录 |

## 【偏离与妥协说明】

无偏离。采用单写者状态 seam，没有把 Admission 合并进 Lifecycle，也没有新增可序列化 Permit 或第二套状态机。

## 【测试建议】

`/to-spec` 阶段应为 `AdmissionEvaluationBatch` 添加表驱动测试：全允许、局部 Incompatible、局部 Disabled、全局 core escalation、批次重复/乱序、同 FeatureId 冲突以及状态 revision 单调性。

