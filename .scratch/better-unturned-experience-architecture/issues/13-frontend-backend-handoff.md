# 定义前后端交接状态与失败反馈

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-08, GPT-09, GPT-10, GPT-11, GPT-12

## Question

Gemini 前端应接收哪些稳定视图模型、预览结果、状态变化和错误信息？UI 如何区分模块未安装、版本不兼容、启动失败、运行时隔离和服务器禁用？

## Answer

完整决策见 `../Frontend-Backend-Handoff-Spec.md`；给 Gemini 的消费摘要见 `../handoffs/to-13-handoff-and-failure-contract.md`。

冻结以下政策：

1. 物品提交后前端隐藏拖拽层并进入 `AwaitingProjection`；2 秒仅结束视觉等待，不代表拒绝、重试或回滚。
2. 原生投影关联要求同一拖拽 generation、玩家/容器会话、预期源变化及目标物品指纹相符；不确定时只接受原生刷新，不播放插件成功动画。
3. 超时后的迟到投影仍按原生状态接受，但不得恢复旧预览、弹迟到通知或移动回原位；旧 generation 不得污染新拖拽。
4. 服务器权威设置提交期间控件为 Pending；3 秒以同一 RequestId 重试一次，总计 8 秒后请求完整快照，不进行第三次重试或离线排队，并保留最后确认快照。
5. 插件缺失或握手超时默认静默降级；只有打开设置或实际使用相关功能时显示行级说明。版本问题显示功能级徽标，不使用通用网络弹窗。
6. 模块状态映射区分未协商、Incompatible、Disabled、过渡态、Isolated、服务器关闭与 Core SafeMode；功能 UI 清理与设置外壳呈现职责明确分离。
7. 同一 `FeatureId + ErrorCode + DiagnosticId` 每会话主动提示最多一次；网络波动和库存视觉超时静默。主动通知仅用于 SafeMode、明确操作被拒绝或可行动的版本问题，并只使用本地化 key。

人工开发者于 2026-08-24 全部接受上述七项政策。GPT-13 独立审计通过前仍只属于 Wayfinder 决策，不能作为生产实现或三环境运行证据。

Gemini 前端规格第 3.5 节原有“失败/丢包/超时后恢复原位置图标”仅是 GPT-13 前的候选描述，自本票起被更精确规则取代：超时不得推断失败，源图标是否存在及透明度只能跟随最新原生库存投影。GPT 不直接改写 Gemini 所有权文档；该修订通过交接文件要求 Gemini 回写。



