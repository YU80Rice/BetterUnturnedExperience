# DEV-15：Better Item Interaction 正式功能实现

Type: task  
Status: ready-for-agent  
Owner: GPT（后端运行时、共享契约与功能装配）  
Required reviewer: Gemini（前端 UI 与消费复核）  
Baseline: BUE-V1-RT01-20260824  
SourceSet: BUE-SS-20260824-02  
Specification: `../spec-DEV-15-better-item-interaction.md`  
Official FeatureId: `io.github.yu80rice.bue.better-item-interaction`

## 目标

将 DEV-14 官方注册 tracer bullet 扩展为真实可用的 Better Item Interaction，覆盖玩家背包、普通容器和车辆后备箱的增强拖入、预览、原生提交、投影收敛与多环境资格证据。

## 已冻结的玩家行为

- 默认启用，可关闭并再次开启；
- 支持地面→背包/容器、背包↔普通容器、同网格移动与旋转；
- 不增强拖出到地面；
- 特殊槽位和特殊页面原生放行；
- 无效候选保留原物；
- 不显示服务器拒绝或技术错误；
- 故障时恢复原生拖拽。

## 子工单

- [ ] DEV-15A Native Drag Adapter
- [ ] DEV-15B Coordinate + Preview Wiring
- [ ] DEV-15C Projection Relay + AwaitingProjection
- [ ] DEV-15D Settings + Lifecycle + Isolation
- [ ] DEV-15E SP/P2P/U3DS Qualification Evidence

## 统一验收门禁

- [ ] TDD Red → Green → Refactor 证据齐全；
- [ ] Release 构建 0 errors，尽量 0 warnings；
- [ ] 独立 GPT 审计 PASS；
- [ ] Gemini 前端消费复核 ACCEPT；
- [ ] 新候选 DLL SHA-256 绑定新 CaseId；
- [ ] 单人、SteamP2PFriends Host/Client、U3DS 分别完成适用性验证；
- [ ] 任一环境未完成时不得宣称功能完成或发布通过。

## 明确不做

- 不新增库存 RPC；
- 不修改 LMN、BepInEx、U3DS 或 Unturned 原版；
- 不做目录扫描、动态 DLL 发现或全局反射探测；
- 不做自动交换、自动重排、批量整理或增强丢弃。

## 规划状态

本票只完成规格化和子工单规划，尚未授权生产编码。进入实现前需由人工开发者明确授权 `/implement DEV-15A`。
