> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-03: 面向多创作者协同的前端模块注册与拓展标准

> **SUPERSEDED**：当前票据为 `../../better-unturned-experience-architecture/issues/03-multi-creator-frontend-module-registration.md`。

- Type: grilling
- Status: open
- Author: Gemini
- Blocked by: None

## Question

为了支持 GitHub 上的多创作者协同开发与生态吸纳，前端应当提供怎样规范的 Module / View / HUD 注册中心接口（`IFrontendModule`），使其他开发者在为《更好的未转变者体验》增加新功能时，无需修改现有前端核心代码？

## 前端设计考量（Gemini 视角）

1. **生命周期钩子（Lifecycle Hooks）**：
   - `OnGlazierInitialized()`: 原生 Glazier UI 初始化时注入全局 View。
   - `OnInventoryOpened(PlayerInventory inventory, ItemContainer container)`: 背包打开时动态挂载 HUD 增强层。
   - `OnRenderTick()`: 交互帧更新。
   - `OnUnload()`: 插件卸载或热重载时安全清理所有 UI 元素，杜绝内存泄漏与空指针。
2. **冲突隔离与命名空间**：
   - 保证不同创作者的 UI 元素拥有唯一的标识符与层级索引，避免覆盖和层级打架。


