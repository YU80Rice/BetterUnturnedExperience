# GPT 前端成果接管代码复核报告

## 审计范围与固定点

- 固定点：`fccd69e1e49a392e50aad78007052e1e4cc83c41`（DEV-15A 前基线）。
- 差异命令：`git diff fccd69e...HEAD`。
- 提交序列：`e01ba2a`、`0a44114`、`c475314`、`2cefd63`、`f80d2d1`、`381a788`、`6b44e98`、`dfc6a34`、`6b37dd7`、`a23daad`、`1e4c526`、`2f71779`。
- 规格来源：`spec-DEV-15-better-item-interaction.md`、`spec-DEV-16-runtime-clientui-management-panel.md`。
- 审计对象：原 Gemini 负责的 DEV-15A～15D ClientUi 成果，以及 GPT 接入的 DEV-16A 单 DLL 组合根。
- 审计性质：只读接管审计；本次未修改生产代码。

## Standards

**判定：FAIL，2 项阻断。**

1. **Headless/原生能力门禁未真实成立**
   - 文件：`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs:22-31`、`ClientUiCompositionRoot.cs:28-39`。
   - 事实：入口将 `headless` 直接等同 `isBatchMode`，并固定传入 `nativeUiAvailable=true`；组合根内部固定 `clientUiAvailable=true`。
   - 依据：`docs/adr/0002-bue-owned-single-dll-management-panel.md:21` 要求 U3DS Headless 在类型创建、UI 构造和客户端 Hook 前门禁隔离。
   - 风险：非 BatchMode Headless 或原生 API 不匹配时可能错误装配 ClientUi。

2. **卫星缺失被错误升级为功能隔离**
   - 文件：`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs:195-201`。
   - 事实：`SetClientUiSatelliteAvailable(false, ...)` 直接调用 `runtime.Isolate()`。
   - 依据：`CONTEXT.md:148-150` 规定功能隔离应由未处理异常触发并保持其它功能与原版运行；DEV-15/DEV-16 规定卫星缺失应投影 `PresentationDegraded/HeadlessOnly`，而不是将核心功能永久置为 Isolated。
   - 风险：可选 UI 卫星缺失时，核心/设置继续可用和原生回退的降级语义不完整。

非阻断 smell（判断性）：

- `BootstrapGuard` 与组合根多个 bool 参数可能构成 Data Clumps/Primitive Obsession。
- `InventoryPreviewWiring.cs` 中有限值/缩放校验存在重复逻辑，可能构成 Duplicated Code。
- `ItemInteractionUiComponent` 同时承担设置、生命周期、视觉、拖拽、原生提交与清理，可能构成 Divergent Change。

## Spec

**判定：FAIL，5 项阻断。**

1. **真实 ClientUi/库存接线缺失**：DEV-16 §7/§8 要求 Harmony/访问桥接入 `PlayerDashboardInventoryUI`、`SleekItems`、`PlayerInventory` 和原生回调；当前只有纯 C# `IInventorySurfaceContext`，无真实 Glazier/Sleek/Harmony/Unturned Adapter。
2. **BUE 管理面板缺失**：DEV-16 §4 及用户故事 3～16、28～32 要求主菜单/暂停菜单入口、插件发现、收藏排序、ConfigEntry 安全编辑和 UI 重建恢复；当前 `BueClientUiRoot` 为空实现。
3. **投影中继未接入释放链**：DEV-15 §3、§4、§6.2 要求释放后 `Bind/Pump/AwaitingProjection` 收敛；当前 `OnDragReleased` 调用 Adapter 后立即 `runtime.EndDrag()`，Relay/Controller 仅存在于纯 C# 测试层。
4. **原生能力探测硬编码可用**：DEV-16 §3/§8 要求目标类型/方法探测通过后才装配；当前 `nativeUiAvailable=true`、`clientUiAvailable=true`。
5. **真实物品图标路径缺失**：DEV-15 §6.3、DEV-16 §7 要求 `ItemJar/ItemAsset → ItemTool.getIcon/SleekItemIcon.Refresh`；当前 `ShowIcon` 仅保存 `BoundAsset` 值，不证明真实纹理渲染。

附加观察：`NativeInventoryInteractionAdapter.IsOrdinaryGrid` 使用固定页边界 `2/8`，在 DEV-16C 版本化原生探测前不能视为未知特殊页面已安全放行。

## GPT 接管结论

Gemini 的纯 C# ClientUi Seam、坐标/候选算法、投影模型、设置生命周期和测试资产可继续复用；但它们尚未构成真实运行时前端。当前项目不得宣称真实绿色/红色投影、设置面板、库存拖拽或三环境玩法通过。

下一实施顺序：

1. DEV-16B：BUE 管理面板、设置消费、真实原生能力探测。
2. DEV-16C：库存 UI 生命周期、容器上下文和受控 Harmony Hook。
3. DEV-16D：真实图元/图标、释放提交与 Projection Relay 接线。
4. DEV-16E：使用新 DLL 哈希重新采集单人、P2P Host/Client、U3DS 证据。

## 汇总

| 复核轴 | 判定 | 阻断数 |
| --- | --- | ---: |
| Standards | FAIL | 2 |
| Spec | FAIL | 5 |
