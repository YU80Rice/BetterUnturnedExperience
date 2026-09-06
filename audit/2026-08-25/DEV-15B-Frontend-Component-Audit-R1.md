# GPT-DEV-15B 前端表现组件独立审计 R1

## 一、结论

**FAIL（存在 2 项阻断项，暂不能将 Gemini 表现组件视为 DEV-15B 可关闭交付）。**

本次为只读独立审计，未修改源码。此前纯 C# Coordinate + Preview Wiring 审计结论不变；本报告专门覆盖 Gemini 新增的 `ItemInteractionUiComponent.cs` 表现层抽象及其测试。

## 二、验证范围

- 实现：`src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs`
- 测试：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs`
- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15B-coordinate-preview-wiring.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`

构建与测试：

- `dotnet build BetterUnturnedExperience.sln --configuration Release --no-restore`：0 errors / 0 warnings
- Contracts、Settings、Placement、Network、ClientUi、Plugin、Release：7/7 PASS
- ClientUi 测试包含表现组件生命周期、绿色/红色框、图标、Pending 上游 Presenter、热路径分配测试。

## 三、阻断项

### B-01：表现层使用固定 50f 网格像素，未消费实际 CellPixelSize/UI Scale

位置：`ItemInteractionUiComponent.cs` 的 `SleekInventoryPreviewSink.ShowFrame`。

实现将 `Candidate.X/Y` 与 `Width/Height` 直接乘以 `50f`。但 DEV-15B/DEV-15 规格明确要求坐标链消费 UI Scale、滚动和网格 viewport；`PreviewIcon` 已由上游按真实 `CellPixelSize * UiScale` 计算，而占据框却另行使用固定像素尺寸。只要原生网格 cell size、UI Scale 或窗口布局不是 50f，绿色/红色框就会与真实网格错位或尺寸错误。

现有测试在 50f 期望值上通过，只证明了该硬编码，不证明运行时布局正确。

修复建议：将实际网格像素尺寸（或已计算的网格坐标变换上下文）作为构造/绑定时的不可变配置传入 sink，并在 frame 更新时使用该值；不得在热路径创建对象。测试至少覆盖 25f、50f、UI Scale 2.0 和滚动布局。

### B-02：ClientUi 源码静态隔离门禁被表现组件自身破坏

位置：`ItemInteractionUiComponent.cs` 的 XML 注释及 `ISleek*` 类型名。

执行 `eng/Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.ClientUi` 失败，命中：

`ItemInteractionUiComponent.cs:Glazier`

该文件还包含 `Sleek` 标识。DEV-15B 工单要求 ClientUi 源码通过 UI/Native/LMN 类型泄漏门禁；即使这些是抽象接口而非第三方运行时类型，当前仓库机械扫描仍明确失败，不能把门禁结果报告为 PASS。

修复建议：将纯 C# 抽象改为不含禁用 Token 的中性命名（例如 `IVisualElement`、`IVisualContainer`、`InventoryPreviewVisualSink`），删除/改写命中禁用词的注释，并将真实 Glazier/Sleek 适配器放入明确允许 UI 依赖的独立 ClientUi composition 边界；然后重新执行扫描。

## 四、非阻断项

1. `HidesAwaitingProjectionState` 已验证 `InventoryPreviewPresenter` 对 `PendingAuthoritativeProjection` 隐藏预览且不显示红色拒绝；但没有直接以 `BetterItemInteractionUiComponent.OnDragUpdated` 入口覆盖 Pending，建议补一条组件级回归测试。
2. `OnInventoryClosed` / `OnUiDestroyed` 的 sink 卸载与 `EndDrag` 清理路径正确，`Unmount` 具备幂等保护；`BindVisualSink` 重新绑定时也先卸载旧 sink。真实容器切换、SafeMode/隔离调用链仍需后续实机或组合根测试。
3. 当前抽象没有真实 Unity/Glazier 实例化或原生 Hook；这符合本票“不实现真实 Hook”的证据边界，不能据此宣称实机表现通过。

## 五、通过项

| 审查项 | 判定 | 依据 |
|---|---|---|
| 绿/红分流 | PASS | `ShowFrame` 根据 `PreviewFrameKind` 设置颜色；Presenter 对 Candidate/LocallyInvalid 分流。 |
| 浮动物品图标顶层挂载 | PASS（抽象层） | icon 创建于 top-level container，frame 创建于 grid container。 |
| 释放/取消清理 | PASS | `OnDragReleased`、`OnDragCancelled` 隐藏并结束拖拽；关闭/销毁对称卸载。 |
| Pending 不伪造拒绝 | PASS（上游接线） | Presenter 已拒绝 Pending；组件透传 sink，不自行把 Pending 变成红框。 |
| 热路径分配 | PASS（测试范围内） | 10,000 次 sink 更新测得 0 bytes；表现更新未使用 LINQ/闭包/临时集合。 |

## 六、最终裁定

当前不能签署 `ACCEPT`。必须先修复 B-01 的动态网格尺寸/缩放接线和 B-02 的静态隔离门禁失败，随后重新 Release 构建、7 组测试、UI Token 扫描并再次独立审计。修复完成前，不应关闭 DEV-15B，也不应宣称真实 Glazier/Unturned 前端表现已通过。
