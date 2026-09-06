# GPT-10 设置模型与权威性决策交付报告

## 【需求执行概述】

在 Wayfinder 阶段关闭“定义设置模型、持久化与权威性”，为公共框架、后端 SettingsRuntime 与 Gemini 统一设置外壳冻结共同语义。

## 【源码溯源清单（Traceability Matrix）】

| 需求 | 落实位置 |
| --- | --- |
| 三种设置权威 | `Settings-Model-Authority-Spec.md` 第 2.1 节；`Shared-Contract-Spec.md` `SettingAuthority` |
| 描述器、默认值、可见性与校验 | 设置规格第 2.2 节；共享契约 `SettingDescriptor` |
| 每功能/作用域 revision 与原子事务 | 设置规格第 3 节；共享契约 command/event/snapshot DTO |
| RequestId 幂等与并发线性化 | 设置规格第 3.3 节 |
| 三环境持久化作用域 | 设置规格第 4 节 |
| 原子写、损坏与迁移 | 设置规格第 5 节；后端规格 SettingsRuntime/持久化章节 |
| 连接快照与断线清理 | 设置规格第 6 节 |
| Gemini 前端消费边界 | 设置规格第 7 节；`to-10-settings-contract.md` |

## 【文档变更清单】

- 新增 `Settings-Model-Authority-Spec.md`。
- 新增 `handoffs/to-10-settings-contract.md`。
- 更新 `Shared-Contract-Spec.md`、`Backend-Architecture-Spec.md`。
- 更新 `CONTEXT.md`、GPT-10 工单和 `map.md`。

## 【编译验证记录】

当前仓库没有生产项目或可执行构建命令。本次仅修改 Wayfinder Markdown 决策文档，未执行编译；不得把文档审计 PASS 表述为代码、部署或运行 PASS。

## 【子智能体审核记录】

### 第 1 轮：FAIL

独立审核发现 4 个阻断：政策型设置缺少三层状态、持久/内存提交点不明确、并发与 RequestId 无线性化边界、共享 DTO 缺少可选约束及快照来源。原始失败报告保存在 `Implementation-v0.8-1116.md`。

### 修复

- 收窄 `ServerPolicyWithClientPreference` 的 V1 语义，补齐政策、偏好、有效值投影。
- 分离 `ClientPreference` 与 `ServerAuthority` revision scope。
- 冻结单写者事务执行器、InFlight 去重、耐久提交点、内存线性化点及崩溃恢复。
- 补齐 `SettingValueOption`、文本规则、动态政策和快照来源 DTO。

### 第 2 轮：PASS

- 审核员：独立子智能体 `audit_gpt10_settings`
- 阻断项：0
- 结论：GPT-10 决策材料自洽，可以在 Wayfinder 地图中关闭。

## 【偏离与妥协说明】

无需求偏离。为保持 V1 可实现性，政策型客户端偏好被明确限定为不改变服务器共享世界状态的体验偏好；改变服务器规则的设置必须使用 `ServerAuthoritative`。

## 【后续验证建议】

- GPT-11 冻结网络集合上限、分片、能力协商和跨 Minor 降级。
- Gemini-02 覆盖全部 SettingKind，并验证政策变化使旧偏好失效时的 UI 表现。
- 实现阶段为内部 `AuthorityScope` 建立强类型，不使用自由字符串作为事务键。
- 代码落地后补做编译、自动化测试及 SP、SteamP2PFriends Host/Client、U3DS 同哈希运行验收。

## 【最终结论】

GPT-10 Wayfinder 决策闭环：PASS。生产实现、编译和三环境运行状态：未开始、未验证。



