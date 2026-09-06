# GPT-09 模块生命周期与故障隔离决策报告 - v0.7

## 需求执行概述

依据人工开发者授权和 Gemini 四项前端输入，冻结模块生命周期、依赖级联、故障隔离、资源清理、核心 SafeMode、前端状态投影和 U3DS UI 防穿透规则。

## 核心决策

1. 功能状态：`Discovered / Incompatible / Disabled / Starting / Running / Isolating / Isolated / Stopping / Stopped`。
2. 任一未处理异常首次越过模块边界即隔离；V1 不自动重启 Isolated 模块。
3. Stop 与 `IFeatureLifetime.TryTrack` 登记簿双保险清理 UI、输入、事件、协程、LMN handler 和 Harmony lease。
4. 清理必须关闭 dispatch gate、淘汰 generation、清空队列并达到 in-flight 静默点；无法静默进入 Core SafeMode。
5. Stop/Dispose/撤补丁失败走 `Stopping → Isolating → Isolated/CleanupIncomplete`，不得伪装为 Stopped。
6. required 边参与 DAG、拓扑启动和逆拓扑级联；optional 边不阻塞，按使用点动态查询。
7. 依赖缺失、循环、版本不符使用独立错误码。
8. SafeMode 当前进程不可恢复，只承诺尽可能停止自定义功能，不保证原版完全不受影响。
9. U3DS 不扫描、反射创建或注册 UI extension；`isBatchMode` 只是 adapter 门禁，不替代类型隔离。
10. `FeatureStatusChangedEvent` 当前只保证进程内投影，`0x0201` 在 GPT-11 前保持 reserved。

## 变更清单

- 新建 `Module-Lifecycle-Isolation-Spec.md`。
- 更新 `Shared-Contract-Spec.md`：依赖声明、生命周期状态、StopReason、StateRevision、Lifetime interface 和错误码。
- 更新 `Backend-Architecture-Spec.md`，引用 GPT-09 唯一详细决策源。
- 更新 `CONTEXT.md`，增加“功能隔离”和“核心安全降级”。
- GPT-09 置为 resolved，并更新唯一地图。
- 新建 GPT-16“第三方参考功能模块与最小验收面”，受 GPT-10/GPT-11 阻塞。

## 编译与验证

- 本轮为 Wayfinder 决策，没有生产源码或项目清单，无可执行编译命令。
- `git diff --check`：通过。
- 独立审核两轮：FAIL → PASS。

## 第一轮阻断与修复

| 阻断 | 修复 |
| --- | --- |
| Stopping 清理失败无合法终态 | 增加 Stopping→Isolating→Isolated/CleanupIncomplete |
| 缺少并发静默点 | 增加 dispatch gate、in-flight、generation、迟到 TryTrack 处理 |
| 依赖图语义不完整 | required DAG、optional 非阻塞、独立循环/版本错误码 |
| 0x0201 authority/revision 冲突 | 改为 reserved，GPT-11 前仅进程内投影 |
| SafeMode 文案绝对保证 | 改为尽可能继续、建议重启、清理可能失败 |

## 最终审核

- 判定：PASS。
- 阻断项：无。
- GPT-09 可正式关闭。

## 下一阶段

- 主前沿：GPT-10“设置模型、持久化与权威性”。
- 可并行：GPT-11 能力协商、GPT-12 候选算法原型、Gemini-03 前端模块 SPI。
- 仍处于 Wayfinder；禁止进入生产实现或宣称三环境验收。

