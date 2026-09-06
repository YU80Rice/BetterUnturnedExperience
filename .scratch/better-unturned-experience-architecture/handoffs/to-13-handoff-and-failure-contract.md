# GPT → Gemini：GPT-13 前后端交接、失败与超时契约

> 作者: GPT  
> 日期: 2026-08-24  
> 状态: GPT-13 已获人工全量接受；实现与运行未验证

## 请阅读

1. `../Frontend-Backend-Handoff-Spec.md`
2. `../Module-Lifecycle-Isolation-Spec.md` 第 7—9 节
3. `../Settings-Model-Authority-Spec.md`
4. `../Network-Capability-Versioning-Spec.md`
5. `../Item-Placement-Algorithm-Spec.md`

## Gemini 可直接实现的消费规则

- 原生 `sendDragItem` 后隐藏拖拽/预览层，进入 `AwaitingProjection`；2 秒只结束视觉等待，禁止据此宣布拒绝、重试或回滚。
- 只有同 generation、同库存会话、源变化与目标指纹均相符时，才把原生投影视为高置信关联；歧义时只刷新原生 UI。
- 超时后的迟到投影照常接受，但不恢复旧预览、不弹迟到提示、不影响新拖拽。
- 服务器设置提交后控件 Pending；3 秒以同 RequestId 重试一次，总计 8 秒后请求完整快照，无第三次重试与离线队列。
- 插件缺失/握手超时默认静默；只有设置页或实际使用处显示行级说明。
- 按 GPT-13 矩阵区分未协商、Incompatible、Disabled、过渡态、Isolated、服务器关闭与 SafeMode。
- 通知按 `FeatureId + ErrorCode + DiagnosticId` 每会话去重；网络波动和库存视觉超时不弹窗。

## 前端所有权仍不变

Gemini 维护统一设置外壳、Glazier/HUD 表现、动画、本地化文案与 UI 资源的语义清理。GPT 维护状态机、稳定 DTO、revision/generation、设置事务、权威性和错误码。若实现发现必须改变共享 DTO 或状态语义，请先回到 GPT 共同复核，不要在前端私建平行契约。

请 Gemini 回写 `Frontend-Architecture-Spec.md` 第 3.5 节：删除“失败/丢包/超时后恢复原位置图标”这种基于超时推断领域结果的措辞，改为“结束增强等待后，源/目标图标及透明度完全跟随最新原生库存投影”。在回写前，该旧句视为被 GPT-13 取代的历史候选，不是实现依据。

## 证据边界

这是一份 Wayfinder 决策交接，不代表 C# 已实现、DLL 已构建，也不代表单人、SteamP2PFriends 或 U3DS 已通过运行验收。

