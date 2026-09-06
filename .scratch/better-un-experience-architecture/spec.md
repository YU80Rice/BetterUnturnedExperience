# 更好的未转变者体验（Better Unturned Experience）架构规格书

> **SUPERSEDED / 历史草案**：该文件包含 GPT-15 前的旧接口假设，不得作为实施规格。当前事实源为 `../better-unturned-experience-architecture/map.md`、`Shared-Contract-Spec.md` 与 `Backend-Architecture-Spec.md`。

## 概述

本项目旨在构建一个公开、开放、支持 GitHub 多创作者协同的《未转变者》（Unturned）原版体验深度优化模组。通过模块化与清晰的前后端解耦设计，确保新特性可以即插即用，并保持长期向后兼容与低运行时开销。

## 首发特性：更好的物品交互（Better Item Interaction）

优化原版背包与容器的物品交互逻辑：
- 玩家拖拽物品时，无需像素级对齐网格左上角坐标；
- 系统根据鼠标所在位置实时计算最近的有效吸附位置，并在网格上渲染绿色（可用）或红色（不可用）的占据预览框（Occupancy Footprint）；
- 鼠标释放时完成模糊吸附与放置，降低玩家在复杂战局中的物品管理心智负担。

## 分工架构与所有权

1. **前端（Frontend）**：由 **Gemini** 负责。
   - 原生 SDG Glazier UI 呈现（HUD、设置面板、物品交互视觉反馈）。
   - 模糊光标追踪与占据预览绿图（Occupancy Grid Preview）绘制。
   - 前端 Presenter 与本地意图事件发布。
   - 文档规范：前缀 `Gemini-`，文件内明确标注 `作者: Gemini`。
2. **后端（Backend）**：由 **GPT** 负责。
   - 服务端权威网格吸附与冲突校验（防止越界与非法重叠）。
   - BepInEx Harmony 补丁拦截与原生 PlayerInventory 状态同步。
   - `LaunchMultiplayerNet` 命名信道通信管道与数据持久化。
   - 文档规范：前缀 `GPT-`，文件内明确标注 `作者: GPT`。
3. **共享契约（Shared Contracts）**：由 **GPT** 统一维护。
   - 包含双端共享的 DTO、Command/Event 定义、错误码与协议版本控制。

## 子领域规格文档索引

- [前端架构设计规格书](./Frontend-Architecture-Spec.md) (作者: Gemini)
- *后端架构设计规格书（由 GPT 自行编撰）*
- *共享协议与事件契约书（由 GPT 自行编撰）*

