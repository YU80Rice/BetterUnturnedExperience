# GPT 前端成果接管审计报告

## 一、审计范围

- 固定点：`fccd69e1e49a392e50aad78007052e1e4cc83c41`（DEV-15A 前基线）。
- 审计差异：`git diff fccd69e...HEAD`。
- 覆盖成果：Gemini DEV-15A～DEV-15D ClientUi、DEV-16A 单 DLL Composition Root 及对应测试。
- 规格依据：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`、`spec-DEV-16-runtime-clientui-management-panel.md`。
- 性质：只读接管审计；未修改生产代码。

## 二、Standards

**判定：PASS（无硬性规范阻断）**

- 显式源码清单聚合，没有发现 `Assembly.GetTypes()`、全局 `PatchAll()` 或任意 DLL 扫描。
- Contracts/Core 与 ClientUi 的分层边界保持清晰；未发现新增线程、锁内外部回调或平行库存权威。
- 代码存在非阻断维护建议：部分项目 XML 压缩为单行；测试输出仍沿用 DEV-14 文案；`slotsPageBoundary/areaPage` 属于魔数式适配参数，应在真实 Adapter 中改为版本化探测结果。

## 三、Spec

**判定：FAIL（DEV-15/DEV-16 真实运行接线尚未完成；不否定已完成的纯 C# Seam）**

### 阻断项

1. **真实 ClientUi/库存接线缺失**
   - 依据：DEV-16 §7、§8 要求受控 Harmony/访问桥接入 `PlayerDashboardInventoryUI`、`SleekItems`、`PlayerInventory` 和原生回调。
   - 事实：`BetterItemInteractionUiComponent` 仅通过纯 C# 接口接收 `IClientUiInventorySurface`；仓库中没有真实 Glazier/Sleek/Harmony/Unturned Adapter 或 Hook 安装路径。
   - 影响：玩家仍不会看到真实绿色/红色投影、浮动物品图标，也不会进入真实拖拽链。

2. **BUE 管理面板完全缺失**
   - 依据：DEV-16 §4、用户故事 3～16、28～32。
   - 事实：`BueClientUiRoot` 为空实现；没有主菜单/暂停菜单入口、插件发现列表、收藏排序、ConfigEntry 安全编辑或 UI 重建恢复。
   - 影响：BUE 设置项和统一插件管理界面尚未可见。

3. **投影中继未进入释放调用链**
   - 依据：DEV-15 §3、§4、§6.2。
   - 事实：`InventoryProjectionRelay` 和 `AwaitingProjectionController` 仅作为纯 C# 类型存在；`OnDragReleased` 完成 Adapter 调用后立即 `runtime.EndDrag()`，没有 `Bind/Pump/AwaitingProjection` 接线。
   - 影响：无法证明原生投影收敛、迟到快照丢弃或 2 秒视觉等待语义。

4. **原生能力探测被硬编码为可用**
   - 依据：DEV-16 §3、§8 要求目标类型/方法探测通过后才能安装 ClientUi/Hook。
   - 事实：`BetterUnturnedExperiencePlugin.Awake` 传入 `nativeUiAvailable=true`；`BueClientUiCompositionRoot` 内部 `clientUiAvailable=true`。
   - 影响：在真实 API 不存在或签名变化时，骨架可能报告 `Ready`，无法形成 Fail-Closed 的真实兼容门禁。

5. **真实物品图标尚未绑定原生纹理路径**
   - 依据：DEV-16 §7 与 DEV-15 §6.3。
   - 事实：`InventoryPreviewVisualSink.ShowIcon` 只保存 `BoundAsset` 值，不调用 `ItemJar/ItemAsset → ItemTool.getIcon/SleekItemIcon.Refresh`。
   - 影响：纯值资产身份测试通过不等于游戏内真实图标渲染通过。

### 非阻断观察

- `NativeInventoryInteractionAdapter.IsOrdinaryGrid` 依赖固定页边界 `2/8`；在 DEV-16C 的版本化原生探测前不能视为通用特殊页放行证明。
- DEV-16A 的单 DLL Composition Root、Headless/Unavailable 零工厂门禁、初始化幂等和销毁清理已由独立审计证明，但它只覆盖装配骨架，不覆盖真实玩法。

## 四、接管结论

Gemini 的前端纯 C# 设计与测试成果可以接管并作为 DEV-16B～D 的基础；但当前项目不能宣称真实 UI、库存拖拽、设置面板或三环境玩法已完成。

### 推荐后续顺序

1. DEV-16B：先实现 BUE 管理面板、设置消费和原生能力探测接口。
2. DEV-16C：接入真实库存 UI 生命周期、容器上下文和受控 Harmony Hook。
3. DEV-16D：接入真实预览图元、原生图标、释放提交和投影中继。
4. DEV-16E：以新 DLL 哈希重新采集单人、P2P Host/Client、U3DS 证据。

## 五、审计摘要

| 轴 | 结论 | 阻断数 |
| --- | --- | ---: |
| Standards | PASS | 0 |
| Spec | FAIL（后续接线未完成） | 5 |
