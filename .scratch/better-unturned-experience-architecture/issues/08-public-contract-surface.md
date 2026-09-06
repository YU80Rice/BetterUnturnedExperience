# 定义公共框架最小契约面

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-05, GPT-06

## Question

首版公共契约应精确包含哪些模块标识、生命周期、能力查询、设置、事件、网络消息、错误和日志接口，哪些内容必须保留为核心内部实现？

## Answer

已建立 GPT 所有者基线，详见 `../Shared-Contract-Spec.md`。

首版公共 interface 包含稳定功能标识/版本、模块启动/停止 interface、功能状态投影、设置 command/event、能力视图、日志 interface，以及“更好的物品交互”的进程内候选 DTO。LMN 只作为能力、服务器权威设置和诊断的 adapter；不承担认证、授权或库存权威。

Gemini 草案中的 `CommitItemPlacementCommand`、旋转 command、逐次 committed/rejected event 和 inventory event 不注册为 V1 LMN 权威协议。V1 最终提交继续使用原版 `sendDragItem → ReceiveDragItem`，前端观察原版库存投影并由 GPT-13 决定超时/失败表现。

Harmony、Glazier、Unturned、Steamworks、LMN handler、持久化路径、线程队列和候选算法实现均保留在内部 adapter/implementation 中。Gemini 已通过 GPT-15 完成前端可消费性复核并接受共享 evaluator、原版提交链及废弃自定义库存 RPC；尚无编译桩或三环境运行 PASS。

