# GPT → Gemini：DEV-15 Better Item Interaction 规格复核请求

请依据以下文件独立复核 DEV-15 正式功能规格，不进行生产编码：

- `spec-DEV-15-better-item-interaction.md`
- `issues/DEV-15-better-item-interaction-formal-implementation.md`
- `Item-Placement-Algorithm-Spec.md`
- `RT-04-Inventory-Authority-Container-Research.md`
- `RT-06-Joint-Seam-Implementation-Readiness.md`
- `issues/DEV-14-official-better-item-interaction-registration.md`

## 请重点裁定

1. 玩家可见范围是否准确覆盖：背包、箱子/普通容器、车辆后备箱、地面拖入；是否正确排除拖出、特殊页面和自动整理。
2. `Enabled`/`AutoRotate` 的本地设置语义、原生回退和中途切换行为是否可消费。
3. 绿色/红色预览、浮动物品图标、离网格隐藏、2 秒视觉等待和无错误弹窗是否符合前端实现边界。
4. `Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle` 与关闭、换容器、死亡、重连、旧代际回调失效是否完整。
5. 普通网格增强与 AREA/装备/特殊分支 pass-through 是否没有越权。
6. UI 卫星缺失、功能隔离和 Core SafeMode 是否仍能保留原版体验与设置 Facet。
7. DEV-15A～E 拆分是否足以支持独立 TDD、审计和四环境证据采集。
8. 是否存在需要提交的新 Shared Contract Change Request。

## 证据边界

本规格不是生产实现、构建 PASS、玩法运行 PASS 或三环境发布资格。请输出 `ACCEPT`、`ACCEPT WITH CHANGES` 或 `BLOCKED`，并逐项列出阻断项、契约缺口和前端消费影响。

