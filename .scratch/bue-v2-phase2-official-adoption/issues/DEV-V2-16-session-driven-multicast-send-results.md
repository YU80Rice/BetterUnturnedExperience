# DEV-V2-16：平台——会话驱动组播与发送结果语义（Q4）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-14（契约 Major 升级与登记流程须先建立）
Spec: `../spec.md`（「平台：发送语义 = 会话驱动组播」一节）

## What to build

功能模块的组播变成可推理的动作：`SendToClients` 只向当前已建立 BUE 会话逐一定向发送——未装 BUE 的原版玩家零感知，本地主机身份天然不在远端会话集合（功能模块无需手写跳过本地）；每次发送返回显式结果，部分失败不再需要解析日志。

## Scope

- `SendToClients` 语义重写：established 会话逐一定向，不是无目标帧交底层广播。
- 发送结果冻结：快照空 → `NoSession`；≥1 成功 → `Sent`；有目标全失败 → `LocalTransportUnavailable`；**新增 `PartialFailure` 枚举成员**表达部分失败。
- `Sessions` 语义收窄为 established 快照（登记条目③）；pending 会话仅内部可见。
- `SendToClient` 维持按会话寻址（**不新增** SteamId 重载），校验会话归属本运行时、established、当前连接代际。
- 发送不持状态锁。

## 验收条件

- [ ] 红测先行：发送结果五值（含 `PartialFailure`）/ established-only 快照 / 外来会话与过期代际被拒 / 无会话 → `NoSession`——先红后绿
- [ ] 与 DEV-V2-14 停用语义联动回归（停用时发送仍 `NoSession`，无新错误码）
- [ ] 冻结面变更登记条目③追加
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
