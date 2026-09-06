# DEV-V2-22：LIR 纳入——更好的换弹体验（ReloadContextGuard + 宿主时钟 + 事件消费）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-18（BUE 帧绑定）、DEV-V2-19（HostTick + TidyCompleted 契约件）
Spec: `../spec.md`（「LIR：补丁、守卫与常量」「LIT ↔ LIR：TidyCompleted 功能事件」两节）

## What to build

裸 BUE 的原位换弹：双击换弹键触发原位压弹，行为与旧独立插件一致；用更好的物品交互拖入物品绝不误触发换弹；背包整理完成后无条件自动压弹（不加新开关）；面板中文名「更好的换弹体验」，关闭 = 原生回退且只撤销自己的补丁。

## Scope

- 身份：FeatureId `io.github.yu80rice.bue.in-place-reload`（频道身份同串，旧频道退役）；显示名「更好的换弹体验」。
- 三枚补丁保留为模块内部实现（UseableGun 接弹匣 Prefix/Postfix、forceAddItem Prefix）；结构：LIR 生命周期 → ReloadContextGuard → 三个 Harmony adapter → 换弹行为；Harmony ID 收编 FeatureId；`Stop` 只撤销自身 Harmony ID。
- `TidyCompleted` 消费：TidyCompletedConsumer → ReloadAction（本期 adapter = 整理后自动压弹）；必须验事件结果成功 + 范围符合，保持自身幂等与异常隔离，不见事件就盲执行被禁止；本期不加开关（enabled 总开关已存在）。
- 周期驱动：订阅 HostTick（DEV-V2-19），自负责双击检测、换弹键轮询、换弹状态推进、超时判断；不自建 Unity Update 泵。
- 常量集中 `ReloadRuntimePolicy`（双击窗口 0.3s、队列上限 64、诊断间隔 5s），不散落补丁；主线程 dispatcher 队列保留为内部实现。
- 网络：客机压弹请求 → 服务器；服务器回包按会话定向；注册失败半注册回滚；注册延迟到首帧游戏线程（语义保持，落在新生命周期内）。
- 设置：只持久化 `enabled`（ClientLocal，关 → 原生回退）。
- 扩展三分法：换弹策略走 `IReloadAction` adapter；独立新能力立新 FeatureId 模块；跨功能协作只走公开契约（不恢复反射寻类型 / 跨功能 Harmony postfix）。

## 验收条件

- [ ] 红测先行：**叠加点钉死——BII 拖入 → forceAddItem Prefix 触发 → 上下文 = false → LIR 不执行换弹逻辑** / `Stop` 只撤自身 / 事件消费验成功+范围+幂等 / HostTick 驱动的双击与状态推进——先红后绿
- [ ] 整理 → 自动压弹联调：与 DEV-V2-21 的事件发布端到端（假 transport 下全链绿）
- [ ] 冲突审查复核留档：BUE / BII 现有补丁目标与 LIR 三补丁零交集
- [ ] 面板条目：FeatureId 身份 + 中文名；构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
