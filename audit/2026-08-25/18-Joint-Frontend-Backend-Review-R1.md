# GPT-18 前后端联合复核报告 R1

## 裁定

**ACCEPT IN PRINCIPLE / REVISE FOR IMPLEMENTATION**

Gemini 已确认前端可以消费 GPT-18 的总体方向；GPT 后端复核确认产品定位正确，但 B-01～B-08 仍要求 Shared Contract Change Request 和物理部署/装载验证。两份报告并不矛盾：一个是消费层 ACCEPT，一个是实施门禁 REVISE。

## Gemini 建议的合并结果

| 建议 | 联合裁定 |
| --- | --- |
| ClientUi 缺失时 `PresentationDegraded`/`HeadlessOnly`，Settings Facet 仍可用 | 纳入 SCR-GPT18-001；状态投影和设置可用性必须分别建模。 |
| BUE 设置模态窗拥有 UI 主权 | 接受；第三方只能提供静态 Facet，自定义 HUD 走 ClientUi satellite registration。 |
| 官方 Better Item Interaction 与第三方平权 | 接受；官方可物理内置，但不得使用隐藏注册/生命周期路径。 |
| 生产前完成 SCCR | 接受；这是后端 B-02 的直接关闭条件。 |

## 尚未关闭的后端门禁

- BUE Host 物理程序集与当前多项目开发拓扑的部署闭合。
- 注册 Interface、错误族、依赖身份、调用线程、所有权和生命周期阶段冻结。
- `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 时序及晚注册行为。
- ClientUi satellite 的 U3DS 部署排除与装载前类型安全。
- 官方功能内置/外置的物理归属和证据规则。
- 绑定 BUE Host、全部功能 DLL、UI satellite、Definition Artifact 和 reference-set 的 `LoadSetIdentity`。
- BepInEx/CLR 注册前失败与 BUE Runtime Isolated 的证据边界。
- SDK compile-time-only 依赖模型。

## 最终推进裁定

1. GPT-18 保持 `needs-triage`，不能标记 `resolved`。
2. 先执行 `SCR-GPT18-001`，再制作 no-op 外部功能 fixture。
3. no-op fixture 仅在 SCCR 冻结后进入实现；不能反向定义共享契约。
4. DEV-01～DEV-09 既有验收状态不被追溯修改，三环境运行证据仍未由本联合复核产生。
