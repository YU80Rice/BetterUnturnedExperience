> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini → GPT：RT-02 & RT-03 前端全量调研复核 Prompt

> **发送方**: Gemini（前端负责人）  
> **接收方**: GPT（后端负责人、共享契约与权威边界强制 Reviewer）  
> **复核任务**: RT-02（物品拖动与坐标渲染链调研）与 RT-03（统一设置、生命周期与 Headless 隔离调研）  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-01`  

---

### 一、 交付文件清单

1. **RT-02 完整调研报告**：  
   [`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\.scratch\better-unturned-experience-architecture\RT-02-Frontend-Inventory-Coordinate-Research.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/RT-02-Frontend-Inventory-Coordinate-Research.md)
2. **RT-03 完整调研报告**：  
   [`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\.scratch\better-unturned-experience-architecture\RT-03-Frontend-Settings-Lifecycle-Headless-Research.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/RT-03-Frontend-Settings-Lifecycle-Headless-Research.md)
3. **RT-02 & RT-03 同步简报**：  
   - [`handoffs/to-RT-02-sync.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/handoffs/to-RT-02-sync.md)  
   - [`handoffs/to-RT-03-sync.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/handoffs/to-RT-03-sync.md)
4. **已更新验收工单**：  
   - [`issues/RT-02-frontend-inventory-ui-coordinate-research.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/RT-02-frontend-inventory-ui-coordinate-research.md)  
   - [`issues/RT-03-frontend-settings-lifecycle-headless-research.md`](file:///D:/Agent-工作目录/DevelopMyUNMultiplayerModAndModloader/更好的UN体验/.scratch/better-unturned-experience-architecture/issues/RT-03-frontend-settings-lifecycle-headless-research.md)

---

### 二、 前端核心裁定与事实汇总

#### 1. RT-02（物品交互与坐标 Seam）
* **JCR-07 坐标 Seam 源码闭环**：
  * 基于 `PlayerDashboardInventoryUI.cs:2425-2428` 严格验证原生 Forward 顺时针旋转公式：
    $$\text{newGrabX} = H - \text{oldGrabY}, \quad \text{newGrabY} = \text{oldGrabX}$$
  * Presenter 确立换算：$\text{intendedItemCenterGrid} = \text{pointerGrid} + (\text{currentFootprintCenter} - \text{grabOffsetInFootprint})$ 并传入 Evaluator。
* **Glazier 图元分流挂载**：
  * 悬浮图标挂载于全屏顶级容器 `PlayerDashboardInventoryUI.container`（避开视口裁剪，最高 Z-Order）。
  * 绿/红占据框挂载于目标网格 `SleekItems.itemsPanel`（随背包滚动条自然平移，0 GC 对象池）。
* **音频与事件**：释放放置依赖原生音频链路，增强层 0 重复播放；原生库存投影事件 `onInventoryAdded/Removed` 驱动最终刷新。

#### 2. RT-03（设置中心、生命周期与 Headless 隔离）
* **双入口注入与焦点 Seam**：
  * `PlayerPauseUI`（游戏内暂停菜单）与 `MenuConfigurationUI`（主菜单选项）双重注入原生 `SleekButtonIcon` + 全局 `F8` 热键。
  * 采用 `ShouldGameIgnoreInput` 机制隔离按键绑定输入焦点，输入文本时静默快捷键。
* **构建期 Facet + 运行时 Snapshot 消费**：
  * 静态 Settings Facet 构建控件骨架；不可变 `FeatureSettingsSnapshot` 驱动实时值与权限。150ms / PointerUp 防抖提交原子事务。
  * `ServerPolicyWithClientPreference` 保留玩家本地偏好，并在服务端锁定生效时展示金色徽标，不静默覆盖本地持久化。
* **9 态生命周期与 Core SafeMode 投影**：
  * `Isolated` 立即注销并卸载该模块所有 UI 扩展；`Core SafeMode` 弹出警报条并注销增强层，Unturned 原生游戏与菜单 100% 可用。
* **Headless / U3DS 四重强隔离**：
  * 梳理出全部 Client-Only Type Tokens。
  * 确立了 **Core/Contracts 零 UI 引用** + **`ClientUi` internal 封装** + **`!Application.isBatchMode` 前置门禁** + **构建期显式生成注册表** + **CI IL 可达性静态扫描** 的全套隔离方案，保证 U3DS 绝不发生类型解析崩溃。

#### 3. 共享契约充分性确认
* 整个调研严格基于 `BUE-V1-RT01-20260824` 契约基线，前端**未产生任何 Shared Contract Change Request**，契约完备充足。

---

### 三、 请 GPT 逐项复核并裁定

请 GPT 依据 `spec.md` 及 `GPT-RT-01` 共享契约基线进行独立权威复核：

1. **权威边界核对**：确认前端方案未发生任何库存乐观写入越权或服务端状态篡改。
2. **生命周期与设置契约核对**：确认前端对 9 态生命周期、Core SafeMode 投影、防抖提交与服务端政策锁定的消费符合后端规范。
3. **Headless 强隔离核对**：确认 U3DS 无图形环境下的类型隔离方案严密可靠，满足 `GPT-05` 与 `GPT-14` 门禁。
4. **推进裁定**：复核通过后，推进并协调下一阶段任务。



