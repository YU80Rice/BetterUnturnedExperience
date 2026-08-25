# GPT → Gemini：DEV-15B 坐标与预览接线交接包

## 交接状态

- 工单：`DEV-15B-coordinate-preview-wiring.md`
- 当前阶段：GPT 纯 C# Seam 已实现，等待 GPT 独立审计最终 PASS 与 Gemini 前端消费复核；尚未进入 DEV-15C。
- 规格：`spec-DEV-15-better-item-interaction.md`
- 算法：`GPT-Item-Placement-Algorithm-Spec.md`
- 依赖：DEV-15A（Gemini ACCEPT，已 resolved）

## GPT 已交付的纯 C# Seam

实现文件：`src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs`

1. `InventoryGridViewport`：以左上原点和半开 viewport 裁剪域判断指针是否可参与预览。
2. `InventoryPreviewInput`：承载屏幕指针、网格 origin、cell pixel size、UI Scale、滚动像素、物品尺寸、当前旋转、抓取偏移和只读 occupancy。
3. `InventoryGridCoordinateAdapter`：
   - 将屏幕坐标转换为连续网格坐标；
   - 使用当前 footprint center 与抓取偏移计算 `intendedItemCenterGrid`；
   - 按 JCR-07 Forward 公式重复执行 `(gx, gy) → (H - gy, gx)`；
   - 使用最终旋转后的 offset 计算图标屏幕锚点。
4. `InventoryPreviewPresenter`：
   - 先经过 `InventoryDragPresenter` 的 DragGeneration 守卫；
   - `Candidate` → 绿色占据框 + 浮动物品图标；
   - `LocallyInvalid` → 红色占据框 + 隐藏图标；
   - `Hidden`、`PendingAuthoritativeProjection`、陈旧 evaluator 输出 → 清理全部图元；
   - Preview Rotation/Width/Height/Reason 直接消费 evaluator，不在表现层重算候选。
5. `IInventoryPreviewSink`：纯值渲染命令接口，当前不引用 Unity、Glazier、Sleek、Unturned 或 Harmony。

## Gemini 需要实现的真实前端表现层

Gemini 应在独立的 ClientUi/卫星实现中消费上述 seam，不修改 Contracts/Core 算法：

- 将绿色/红色 `PreviewFrame` 挂载到目标 `SleekItems.itemsPanel` 对应网格；
- 将 `PreviewIcon` 挂载到顶层 `PlayerDashboardInventoryUI.container`，避免 viewport 裁剪；
- 复用/池化图元，避免拖拽更新逐帧分配；
- 将原生 UI Scale、滚动、裁剪和屏幕坐标填充到 `InventoryPreviewInput`；
- 用原生拖拽上下文填充 `DragGeneration`、Source、TargetContainer、footprint 与 grab offset；
- 在关闭容器、换容器、取消拖拽、SafeMode、功能隔离时调用 `EndDrag` 并清理 sink；
- 不向 Contracts/Core 引入任何 UI 或原生类型；不使用程序集扫描、动态 DLL 发现或全局 `PatchAll()`。

## 已验证事实

- Release solution：待最终独立审计复核后确认；当前本地 Rebuild 通过，0 errors / 0 warnings。
- 7 个测试：全部 PASS；ClientUi 测试包含 DEV-15B 坐标、旋转、预览、陈旧代际与 0 GC 场景。
- ClientUi/Contracts/Core UI/native token scan：PASS。
- 当前仅有静态/单元证据；没有真实 Glazier、Unity Hook、单人、SteamP2PFriends 或 U3DS 运行证据。

## Gemini 复核问题

1. `PreviewFrame` 的颜色/挂载分流是否与前端原生容器结构一致？
2. `PreviewIcon` 的屏幕锚点是否正确保持非中心抓取与 Forward 旋转后的抓取点？
3. viewport 裁剪、UI Scale 和滚动的填充是否不会重复缩放或重复偏移？
4. Hidden、Pending、陈旧代际及 UI 隔离是否清理全部自定义图元并恢复原生可用路径？
5. 是否存在必须提交 SCCR 的前端消费缺口？

## 明确不应据此宣称

本交接包不代表真实 Glazier UI 已完成，不代表 Better Item Interaction 已完成，不代表任何单人/P2P/U3DS 运行或发布资格通过。真实表现层完成后仍需 Gemini 复核、GPT 独立审计及 DEV-15E 同哈希资格证据。
