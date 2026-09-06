# DEV-V2-17：平台——自动握手与会话生命周期（Q5）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-16（established 快照语义须先落地）
Spec: `../spec.md`（「平台：会话建立 = 网络模块自动握手」一节）

## What to build

功能模块零握手负担：连接建立后的会话握手（Hello/Ack/Reject）、断线清理、重连换代际全部由网络模块自动完成；公开会话集合里永远只有可以立即收发的已建立会话，`Connected` 只在握手完成后触发。

## Scope

- 握手流程：transport connected → 运行时发 Hello → 服务器 Ack/Reject → Connected → 功能收发。
- 运行时职责：断线清理、重连新会话身份与代际、超时重探退避、重复 Hello 去重、版本不兼容 fail-closed、无 ghost session。
- 会话匹配：Ack 按 peer + 代际 / nonce 匹配（废弃「第一个未建立会话」匹配法）。
- 公开面只含 `Sessions`（established）、`Connected`、`Disconnected`、`GenerationChanged`、发送结果；`Connected` 仅握手完成后触发；Connected/Disconnected 锁外执行。

## 验收条件

- [ ] 红测先行：握手全流程 / 重复 Hello 去重 / Ack 按 peer+代际匹配 / 断线清理与重连换代际 / Reject fail-closed——先红后绿
- [ ] 功能模块视角断言：假 transport 下 Connected 时序正确（握手前不触发），pending 会话不可见
- [ ] 生命周期事件锁外执行（回调中不持状态锁的可重入断言）
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
