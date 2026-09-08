# DEV-V2-20：LHT 纳入——更好的尸潮播报（双件 + 信标守卫 + 组播广播）

Type: task
Status: resolved（2026-09-08，/implement 会话；双轴最终 CLEAN——Standards=R2 CLEAN（反驳接受，4 INFO 具名可延期）、Spec=R2 CLEAN（REBUTTAL-GAP1 接受，零 GAP/DEVIATION/SMELL）；候选 3bb4452e…6569（528384B 三轮 Rebuild 字节一致，CaseId DEV-V2-20-CANDIDATE-20260908）；结单报告 audit/2026-09-08/DEV-V2-20/）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-18（BUE 帧生产消费绑定）、DEV-V2-19（HostTick 契约件）
Spec: `../spec.md`（「LHT：双件结构与传输上收」一节）

## What to build

裸 BUE 三环境尸潮播报：主机/U3DS 权威追踪信标并广播尸潮快照，客户端 HUD 以 10Hz 宿主时钟展示，本地主机不给自己回发；`/horde` 查询仍由原版权限守门；面板中文名「更好的尸潮播报」，关闭 = 完整停摆。LHT 是三件中最简单者，作为 **BueNetworkApi 生产绑定的第一批真实消费者**先走通全链。

## Scope

- 身份：FeatureId `io.github.yu80rice.bue.horde-tracker`（频道身份同串，旧频道退役）；显示名「更好的尸潮播报」。
- 内部双件（一个 FeatureId 内组合）：HordeTrackingModule（信标追踪/epoch/seq/广播，服务器权威）+ HordePresentationAdapter（HUD 注入/10Hz 更新）——HUD 失败只降表现不伤权威追踪。
- 自有补丁：信标两枚 Postfix 保留（**勘误口径：是信标补丁，不是 LIR 补丁**），Harmony ID 收编 FeatureId；上下文守卫原则——补丁可共享原生调用点，不能共享业务上下文，非相关路径立即放行。
- 传输上收：删除 `ModTransport.Initialize` 直调、LMN ABI 守卫、LMN 类型硬依赖；广播 = `SendToClients` 会话驱动组播整体替换 `Provider.clients` 手写循环 + 跳过本地；可靠度不变（Update 不可靠 / Clear 可靠）。
- 保留业务一致性实现：epoch、sequence、单槽 mailbox、脏标记、双可靠度、停止闸门。
- 设置与命令：只持久化 `enabled`（ClientLocal）；关闭 = 完整停摆（服务器停追踪停广播、客户端停 HUD、`/horde` 无 LHT 行为）；`/horde` 冷却 1.5s、HUD 10Hz 为实现常量；admin 权限判断交给原版 ChatManager，不复制权限事实源。
- U3DS：功能状态 Available、表现状态 HeadlessOnly（不阻塞 U3DS 功能验收）。
- 扩展三分法落点：`IHordeTrackingPolicy`（默认实现承载现行为）+ 表现 adapter 变体位 + 独立新功能立新 FeatureId。

## 验收条件

- [ ] 红测先行：信标守卫（context=false 立即放行）/ 组播不含本地身份 / **BUE 帧不可靠 1:1 复验**（08 基线是 LMN 路径，须在 BUE 帧路径重证）/ `enabled=false` 完整停摆 / U3DS Available+HeadlessOnly——先红后绿
- [ ] 平台第一批真实消费者端到端：注册频道 → 订阅 → `SendToClients`，假 transport 下全链绿
- [ ] 面板条目：FeatureId 身份 + 中文名；epoch/seq 行为与 08 基线一致
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
