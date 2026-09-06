# 定义设置模型、持久化与权威性

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-08, GPT-09

## Question

设置定义、默认值、校验、可见性、客户端本地值、服务器权威值、持久化格式和连接时快照同步应如何建模，前端统一设置外壳通过什么契约消费这些信息？

## Answer

人工开发者于 2026-08-24 接受全部七项政策。完整决策见 `../Settings-Model-Authority-Spec.md`。

- 设置权威冻结为 `ClientLocal / ServerAuthoritative / ServerPolicyWithClientPreference`。
- 描述器冻结稳定身份、本地化、类型、默认值、权威、Schema、排序、可见性、启用条件与适用校验；Min/Max/Step 仅用于数值，Choice 使用允许值集合。
- revision 以 `FeatureId + SettingRevisionScope` 为粒度；同一功能、同一作用域的一次多字段提交全部成功或全部失败，changed/rejected 都返回完整快照。
- RequestId 在单连接 generation 内幂等；相同 id 不同 payload 明确拒绝。
- 客户端本地、单人世界、P2P Host 世界和 U3DS 实例/世界分作用域持久化；服务器快照只形成当前连接覆盖，不污染本地偏好。
- 写盘使用临时文件、flush、可重读校验和同卷原子替换；损坏与迁移失败保留证据、逐版本迁移并安全回退。
- 前端只消费描述器和不可变快照，可预校验但以后端投影为最终事实；非 Running 状态禁止普通设置提交。
- GPT-11 仍需冻结快照集合上限、分片、能力协商和跨 Minor 编码，因此网络设置契约仍是 Draft。

该结论是 Wayfinder 规划决策；当前没有生产实现、可执行构建或三环境运行证据。

