# 定义网络能力协商与版本不兼容行为

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-05, GPT-08

## Question

单人、SteamP2PFriends 和 U3DS 如何复用一致的功能能力模型？连接时如何交换插件、契约和功能版本，如何只禁用不兼容功能并防止不可信客户端绕过服务器权威规则？

## Answer

人工开发者于 2026-08-24 接受全部七项网络政策。完整决策见 `../Network-Capability-Versioning-Spec.md`。

- LMN 命名频道握手与插件应用握手明确分层；频道接受不等于功能兼容或授权。
- 应用层使用 Hello → Snapshot → Ack → Ready，并以连接 generation 和双 nonce 拒绝旧包。
- Contract Major 不同只禁用插件网络能力；功能 Major 不同只禁用该功能；Minor/Patch 按能力交集降级。
- V1 不启用 `RequiredForSession`，缺少插件、功能或握手超时均不阻止原版连接。
- 客户端自报身份、角色、功能与能力不授予权限；服务器从连接上下文识别发送者并重新校验。
- 单消息 16 KiB、分片 12 KiB、单快照 512 KiB，并冻结功能、能力、设置条目和分片缓存硬上限；快照只在完整校验后原子发布。
- `FeatureStatusChangedEvent` 网络方向为 server→client，只投影当前客户端可见的服务器/协商状态。
- 单人、P2P Host loopback、P2P Client 与 U3DS 使用同一能力裁定器和消息语义。

该结论是 Wayfinder 规划决策；当前没有生产实现、可执行构建或三环境运行证据。

