> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-Frontend-Architecture-Spec: 更好的未转变者体验前端架构设计规格书

**作者: Gemini**  
**版本: 0.3.0-draft**  
**状态: 经前后端联合一致性复审（JCR-01～JCR-09）修订；待生产实现与三环境运行验证**  
**唯一规范决策地图:** `map.md`  

---

## 1. 架构定位与核心原则

前端专注于玩家直接感知与操作的 UI/HUD 表现层、输入捕获、即时视觉反馈与设置外壳呈现，严格遵循以下原则：

1. **零权威计算与零乐观写入**：
   - 前端不修改、不持有权威 `Items` 数据，绝不绕过原版进行乐观扣减。
   - 最终放置严格调用原版 `Player.LocalPlayer.inventory.sendDragItem(page, x, y, rot)`，由服务端权威 `ReceiveDragItem` 重新校验。
2. **纯原生 SDG Glazier (Sleek UI) 体系**：
   - 深度集成 Unturned 原生 UI 树，确保自动适配游戏内分辨率缩放（UI Scale）、主题风格与输入焦点。
   - 严禁向 Core 或 Contracts 层泄露任何 Glazier/Sleek 具体类型。
3. **U3DS / Headless 结构与运行双重隔离**：
   - 前端所有 UI 代码与控制器必须置于 `!Application.isBatchMode` 运行门禁之后。
   - 满足 `CoreShared` 与 `ClientUi` 源码闭包隔离，严禁在 Core/Contracts 的方法签名、基类、特性、静态构造函数或 type-token 中引用 UI 类型，防止 U3DS 在 JIT/类型装载阶段发生崩溃。
4. **构建期 Facet 驱动与无状态纯计算**：
   - 前端不私自维护复杂的格栅碰撞算法，直接消费 `IPlacementCandidateEvaluator`（由 GPT-12 维护）输出的 `ItemPlacementPreview`。
   - 设置界面由构建期静态 **Settings Facet** 与运行期不可变 **`FeatureSettingsSnapshot`** 双层驱动，彻底废除运行时反射 `Describe()`。

---

## 2. 前端分层与组件架构 (MVP Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                    SDG Glazier UI View                      │
│   (SleekInventoryFootprintLayer, SleekFloatingDragIcon,     │
│    SleekFeatureSettingsModal, SleekSettingsItemRow)         │
└──────────────────────────────▲──────────────────────────────┘
                               │ 双向绑定 / 坐标传递
┌──────────────────────────────▼──────────────────────────────┐
│                  Frontend Presenter Layer                   │
│         (InventoryDragPresenter, SettingsPresenter)         │
└───────────────┬─────────────────────────────┬───────────────┘
                │                             │
                │ 传递 IntendedCenter 纯坐标    │ 提交命令 / 监听投影
┌───────────────▼─────────────┐ ┌─────────────▼───────────────┐
│ IPlacementCandidateEvaluator│ │    SettingsRuntime Snapshot │
│    (GPT-Shared-Contract)    │ │   & Vanilla Inventory Adap. │
└─────────────────────────────┘ └─────────────────────────────┘
```

---

## 3. 《更好的物品交互》前端渲染管线与坐标 Seam

### 3.1 抓取偏移与几何中心坐标 Adapter (JCR-07 Seam)

为了同时满足“玩家抓取物品任意局部时图标不发生瞬移跳跃”与“Evaluator 按物品几何中心执行投影与靠齐”，前端 Presenter 必须执行严格的坐标转换：

1. **坐标系与连续范围**：
   - 局部坐标原点 `(0, 0)` 为物品 Footprint 左上角，X 轴向右，Y 轴向下。
   - `grabOffsetInFootprint` 为 `[0, W] × [0, H]` 连续浮点坐标（严格使用尺寸 $H$ 与 $W$，绝不使用离散 $W-1$ 或 $H-1$）。
2. **抓取初始冻结**：
   - `grabOffsetInFootprint = pointerGrid - sourceItemOriginGrid`（在抓取瞬间锁定）。
3. **每帧预期中心换算**：
   - `intendedItemCenterGrid = pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)`。
4. **原生 Forward 旋转公式（Unturned `rot++` / 按 `[R]` 顺时针旋转）**：
   - 当前尺寸为 $W \times H$，旋转后新尺寸为 $H \times W$。
   - **Forward 变换（Native `rot + 1`）**：
     $$\text{newGrabX} = H - \text{oldGrabY}$$
     $$\text{newGrabY} = \text{oldGrabX}$$
   - *（对比参考：Backward 逆向变换 `rot - 1` 为：$\text{newGrabX} = \text{oldGrabY}, \text{newGrabY} = W - \text{oldGrabX}$）*。
5. **四角与中心映射验证表（Forward `rot + 1`）**：

| 原始局部位置 $(x, y)$ | 语义 | 旋转后新位置 $(\text{newX}, \text{newY})$ | 新 Footprint 语义 |
| :--- | :--- | :--- | :--- |
| $(0, 0)$ | 左上角 | $(H, 0)$ | 右上角 |
| $(W, 0)$ | 右上角 | $(H, W)$ | 右下角 |
| $(W, H)$ | 右下角 | $(0, W)$ | 左下角 |
| $(0, H)$ | 左下角 | $(0, 0)$ | 左上角 |
| $(W/2, H/2)$ | 几何中心 | $(H/2, W/2)$ | 新几何中心 |

6. **分流渲染与计算**：
   - 悬浮图标位置：按 $\text{pointerGrid} - \text{grabOffsetInFootprint}$ 渲染（随光标平滑移动，70% 透明度）。
   - Evaluator 输入：换算后的 $\text{intendedItemCenterGrid}$ 作为算法的连续中心坐标传入。
   - 占据框图层位置：消费 Evaluator 返回的稳定格位 $\text{Candidate}.(X, Y)$。
   - 自动旋转时：候选 footprint 改变，视觉抓取偏移按上述公式同步映射，保证物品视觉中心与光标相对位置严密对齐。

### 3.2 占据网格图层（Occupancy Footprint Overlay）
* **双态分级渲染**：
  * **合法空位（Candidate）**：渲染**绿色半透明填充（#33E566，40% Alpha）+ 细绿边框**，精确覆盖物品长宽所占用的网格单元。
  * **遇到阻挡/空间不足（LocallyInvalid）**：渲染**红色半透明填充（#E53333，40% Alpha）+ 细红边框**，直观告知不可放置。
  * **完全移出容器网格**：自动隐藏占据框，不残留任何色块。
* **零 GC 对象池设计（生产 C# 目标门禁）**：
  * 预创建常驻 `SleekImage` 图元矩阵池，仅动态调整 `PositionOffset_X/Y`、`SizeOffset_X/Y` 与 `TintColor`，严禁每帧动态 GC 分配（最终需经 Release C# 分配测试验证）。
  * 挂载于背包顶级 View 节点，杜绝被单个 `ItemBox` 或滑动视图剪裁（Clip）。

### 3.3 旋转响应与手感（Rotation & [R] Key）
* **即时切换（Instant Switch）**：按下 `[R]` 键瞬间长宽互换（如 2x4 立即变为 4x2），按上述规则同步变换 `grabOffsetInFootprint` 并无延迟重算吸附。

### 3.4 模糊吸附手感与边缘行为（Fuzzy Snapping & Edge Clamping）
* **几何中心反算**：Evaluator 将 `intendedItemCenterGrid` 视为几何中心进行就近网格投影。
* **边缘自动靠齐（Edge Clamping）**：当光标贴近容器边界时，计算坐标自动约束在 `[0, containerWidth - itemWidth]` 与 `[0, containerHeight - itemHeight]` 范围内。

### 3.5 释放放置与视听反馈（Drop Audio-Visual Feedback）
* **原版音效**：释放瞬间触发 Unturned 原生物品放置音效。
* **原生投影跟随（零推断与零残影策略）**：
  * 释放瞬间立即淡出并隐藏悬浮图标与绿/红占据预览图层，进入 `AwaitingProjection` 视觉等待态（2.0 秒超时预算）。
  * 2.0 秒超时仅用于结束增强 UI 的等待指示并回到可交互状态；禁止据此推断服务端拒绝，不触发任何客户端回滚、重试或失败弹窗。
  * 结束增强等待后，源格子与目标格子的物品图标及透明度完全跟随最新原生库存投影刷新（唯一事实源）；超时后迟到的原生投影照常接收并渲染，不弹迟到提示。

---

## 4. 插件统一设置中心（Settings UI）

### 4.1 双入口唤出机制
1. **原生菜单注入**：在游戏按 `Esc` 呼出的原生暂停菜单（Pause Menu）及主界面“选项”中注入原生风格按钮 `[更好的UN体验]`。
2. **全局快捷键**：支持自定义快捷键（默认 `F8`）随时一键唤出/关闭设置模态弹窗。

### 4.2 静态 Facet 驱动生成与防抖提交
* **双事实源绑定（GPT-14 契约）**：
  * **控件元数据与结构**：从构建期链接生成的静态 **Settings Facet**（`SettingDescriptor` 列表）读取控件种类（Toggle/Slider/KeyBinding/Choice）、默认值、边界、排序与分组。
  * **实时值与权限**：从 `SettingsRuntime` 派发的不可变 **`FeatureSettingsSnapshot`** 读取当前有效值、权限（ClientLocal/ServerAuthoritative/Policy）与单调 `Revision`。
* **交互防抖与原子提交**：
  * 高频连续拖动滑块或文本输入时，前端 Presenter 维持本地平滑视觉，仅在**鼠标松开**或**静止 150ms 后**向 `SettingsRuntime` 提交 `UpdateModuleConfigCommand` 原子事务。

---

## 5. 多创作者前端模块贡献与内部注册架构

* **V1 源码贡献模式**：
  * V1 不提供动态加载外部 DLL 的第三方公共 UI 运行时 ABI。
  * 所有创作者 UI 模块均作为 `internal` 类型，通过源码贡献汇入单 DLL，由构建期生成的显式注册表完成装配。
* **客户端 UI 内部扩展接口**：

```csharp
namespace BetterUnturnedExperience.Frontend.Internal
{
    internal interface IClientUiFeatureComponent
    {
        void OnUiInitialized(ISleekElement root);
        void OnInventoryOpened(PlayerDashboardInventoryUI ui);
        void OnInventoryClosed();
        void OnUiDestroyed();
    }
}
```

* **安全沙盒与故障隔离**：
  * 每个 UI 组件的生命周期回调由独立 `try-catch` 保护，异常时自动隔离该组件并从 UI 树中卸载，绝不影响主插件与游戏原生菜单。
  * 整个 ClientUi 模块仅在 `!Application.isBatchMode` 客户端生命周期中被显式注册表调用，U3DS 完全不执行任何 UI 装配逻辑。

---

*文档完。作者: Gemini*

