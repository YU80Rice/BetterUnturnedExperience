> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-Frontend-Architecture-Spec: 更好的未转变者体验前端架构设计规格书

> **SUPERSEDED / 历史草案**：当前前端基线已迁移至 `../better-unturned-experience-architecture/Frontend-Architecture-Spec.md`。

**作者: Gemini**  
**版本: v0.1.0**  
**状态: 架构规划与决策草案 (Draft)**  
**所属项目: 更好的未转变者体验 (Better Unturned Experience)**  

---

## 1. 架构定位与核心原则

前端是玩家与《未转变者》交互的直观窗口。前端架构的核心目标是在保持原版 Unturned (SDG) 艺术风格、操作手感与极致性能的前提下，提供丝滑、现代化的交互体验，并为多创作者提供易于拓展的 UI 注册体系。

### 1.1 核心边界铁规
1. **零权威计算**：前端不判定物品是否真正属于玩家、不修改背包数据底层存储。
2. **纯原生兼容**：UI 必须构建于 Unturned 原生 **SDG Glazier (Sleek UI)** 体系之上，确保与游戏内分辨率缩放、UI 主题、输入焦点完全兼容。
3. **单向事件流**：前端仅通过指令（Commands）向后端表达玩家意图；通过监听状态流（Events / State Snapshots）驱动 UI 呈现与补间动画。
4. **即插即用与隔离性**：每个功能模块（如“更好的物品交互”、“设置面板”）均作为独立的 View/Presenter，避免创作者代码相互污染。

---

## 2. 前端架构分层模型 (MVP Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                    SDG Glazier UI View                      │
│   (SleekCustomGridOverlay, SleekFloatingDragIcon, Settings) │
└──────────────────────────────▲──────────────────────────────┘
                               │ 双向绑定 / 事件回调
┌──────────────────────────────▼──────────────────────────────┐
│                  Frontend Presenter Layer                   │
│         (InventoryDragPresenter, SettingsPresenter)         │
└──────────────────────────────▲──────────────────────────────┘
                               │
            ┌──────────────────┴──────────────────┐
            │                                     │ 意图指令 (Commands)
┌───────────▼─────────────┐             ┌─────────▼───────────┐
│ Client-Side Prediction  │             │   Command Gateway   │
│ & Fuzzy Snap Calculator │             │ (LaunchMultiplayer) │
└─────────────────────────┘             └─────────────────────┘
```

### 2.1 View 层（原生 Glazier 表现层）
* 继承或组合 `SDG.Unturned.SleekElement`。
* 仅负责几何坐标设置、颜色绘制、透明度渐变、图标渲染。
* 无任何业务规则与校验逻辑。

### 2.2 Presenter 层（表现逻辑层）
* 监听 Unturned 原生输入事件（`OnDrag`、`OnPointerMove`、`OnKeyDown(R)`）。
* 维护本地拖拽状态机，调度模糊吸附计算器。
* 控制 View 元素的显隐、颜色变化与动画过渡。

### 2.3 Local Prediction & Fuzzy Snap Calculator（客户端快速预测计算器）
* 在客户端高频（每帧）运行微秒级格栅投影算法，计算当前光标在目标容器网格上最可能吸附的坐标 `(candidateX, candidateY)`。
* 评估是否会超出容器边界或与其他不可放置区域重叠，即时向 View 输出 `Valid / Invalid` 状态以显示绿色或红色预览。

---

## 3. 《更好的物品交互》前端专项设计 (Better Item Interaction)

根据演示视频（`Screenrecorder-2026-08-24-00-22-52-132.mp4`）中的现代射击/撤离游戏高保真交互表现，前端实现如下全流程设计：

### 3.1 交互状态机流转

```
[空闲 (Idle)]
    │
    │ 玩家在 ItemBox 上按住鼠标左键并移动 > 4px
    ▼
[拖拽开始 (Dragging)]
    │ ──> 创建/激活 SleekFloatingDragIcon（半透明物品跟随光标）
    │ ──> 隐藏原背包格子中的静态物品图标（避免视觉重影）
    ▼
[容器悬停与投影 (Hovering & Fuzzy Snapping)]
    │ ──> 鼠标位于有效容器网格（PlayerInventory / Storage）上方
    │ ──> 激活 Occupancy Footprint Overlay（占据网格预览图层）
    │ ──> 模糊计算：根据光标偏移量计算最佳左上角网格坐标 (X, Y)
    │ ──> 状态刷新：
    │       - 可放入：绿色高亮 (#33E566，透明度 45%)
    │       - 阻挡/越界：红色高亮 (#E53333，透明度 45%)
    │ ──> 响应 [R] 键：动态切换物品旋转状态 (0° / 90°)，尺寸宽高互换并重算吸附
    ▼
[释放放置 (Dropping)]
    │ ──> 销毁/回收悬浮图层
    │ ──> 发布 CommitItemPlacementCommand(itemId, targetContainer, x, y, rot)
    │ ──> 播放放置/吸附音效反馈
    ▼
[等待后端状态确认]
    │ ──> 成功：静默提交，恢复常规 UI 显示
    │ ──> 失败/冲突：执行平滑回滚动画，物品图标弹回原位
```

### 3.2 SDG Glazier 渲染管线与优化
1. **独立 Z-Index 层级**：
   * 在 `Glazier.Get().Root` 或背包窗口顶层挂载一个全局单例 `SleekInventoryDragLayer`，不受单个 Container 视口裁剪限制。
2. **零 GC 内存管理**：
   * 预分配 `SleekImage` 对象池（用于渲染占据格子的每个小方块或合成贴图），在拖拽过程中仅修改坐标属性，严禁动态 `new` / `Destroy` UI 元素。
3. **模糊吸附算法（Fuzzy Snapping Algorithm）**：
   * 设光标在容器内的像素偏移为 `(px, py)`，每个格子像素尺寸为 `(slotW, slotH)`。
   * 物品占用尺寸为 `(itemW, itemH)`。
   * 优先将光标对齐为物品几何中心，通过 `round((px - itemW*slotW/2) / slotW)` 反算最近的起始网格 `(originX, originY)`。
   * 当计算结果越界时，自动限制在 `[0, containerWidth - itemW]` 范围内，提供极度顺滑的“边缘自动靠齐”手感。

---

## 4. 多创作者协同与扩展注册标准 (Multi-Creator Frontend SPI)

为了便于多位创作者在 GitHub 上协同扩充《更好的未转变者体验》，前端提供模块化契约：

```csharp
namespace BetterUnturnedExperience.Frontend.API
{
    public interface IFrontendModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        string Author { get; }
        string Version { get; }

        void Initialize(IGlazierRoot uiRoot);
        void RegisterSettings(ISettingsRegistry registry);
        void OnUpdate();
        void Shutdown();
    }
}
```

### 4.1 统一设置中心（Settings UI）
* 所有的子模块通过 `RegisterSettings` 声明自己的配置项（开关、滑动条、快捷键绑定）。
* 前端统一生成原生风格的配置弹窗，无需各模块作者手动绘制界面。

---

## 5. 前后端接口契约缝（Interface Seam）

前端与由 GPT 维护的后端/共享协议严格按如下契约交互：

* **前端发送（Upstream Commands）**：
  * `CommitItemPlacementCommand`：提交物品最终放置决策。
  * `RequestRotateItemCommand`：请求旋转物品。
  * `UpdateModuleConfigCommand`：更新玩家偏好设置。
* **前端接收（Downstream Events）**：
  * `ItemPlacementCommittedEvent`：服务端确认落地成功。
  * `ItemPlacementRejectedEvent`：服务端拒绝放置（附带原因码，如冲突、超重、权限不足），触发前端动画回滚。
  * `InventoryStateUpdatedEvent`：外部引起的背包数据变更。

---

*文档完。作者: Gemini*


