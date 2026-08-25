# GPT-DEV-15B 独立审计 R2

## 结论

**PASS（DEV-15B 纯 C# 坐标与预览 Seam）**。

本审计为只读复核，未修改生产源码。结论仅覆盖本票声明的纯 C# 接线，不代表真实 Unity/Glazier、单人、P2P、U3DS 或发布资格通过。

## 审计对象

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-15B-coordinate-preview-wiring.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-DEV-15-better-item-interaction.md`
- 实现：`src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs`
- 测试：`tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs`
- 实现 SHA-256：`88073877388963F50F97E8010EEF7EE59A7675D8E200BBB0DDC3784B66589878`
- 测试 SHA-256：`3F9A69C389D6283E5B8225E4C654A0B7103E898288BDDF799A42ED0D3F512073`
- ClientUi DLL SHA-256：`A354046A9FB3C23F9C6920DD0D727AA53E52D398232B496AC44A559E626013C8`

## 逐项核验

| 项目 | 结果 | 证据 |
|---|---|---|
| JCR-07 Forward | PASS | `TryRotateGrabOffset` 使用 `nextX = height - grabY`、`nextY = grabX`；R1/R2 单元测试覆盖奇数旋转与实际坐标结果。 |
| 坐标换算 | PASS | 先应用 UI Scale 与滚动，再以 `pointerGrid + (footprintCenter - rotatedGrabOffset)` 生成 Evaluator 输入；非法有限值和非正尺寸 fail-closed。 |
| 图标锚点 | PASS | `TryGetIconScreenPosition` 使用目标旋转后的 footprint/grab offset；测试验证 `(17.5,47.5)` 锚点与最终旋转。 |
| generation/Pending | PASS | `InventoryDragPresenter` 先拒绝陈旧代际；Presenter 再拒绝返回代际不匹配、Hidden、PendingAuthoritativeProjection，Pending 不投影红色拒绝。 |
| 预览分流 | PASS | Candidate 只显示绿色框+图标；LocallyInvalid 只显示红框并隐藏图标；转换失败/Hidden 隐藏全部图元。 |
| 越界 | PASS | viewport 使用左闭右开裁剪域；指针越界不调用 Evaluator 并清理 sink。Evaluator 的网格边界继续由 DEV-04 负责。 |
| 热路径 | PASS（测试范围内） | Release 测试 10,000 次更新测得 0 bytes；实现未使用 LINQ、临时集合、闭包或逐帧字符串日志。 |
| 静态隔离 | PASS（DEV-15B 作用域） | Contracts/Core/ClientUi/Transport 扫描通过；DEV-15B 实现不引用 Unity/Glazier/Sleek/LMN/原生/BepInEx/Harmony。 |

## 构建与测试

命令：`dotnet build BetterUnturnedExperience.sln --configuration Release --no-restore`

- 结果：PASS
- Errors：0
- Warnings：0

7 组测试全部通过：Contracts、Settings、Placement、Network、ClientUi、Plugin、Release。ClientUi 输出：`DEV-05/DEV-15A/DEV-15B ClientUi tests: PASS`。

## 门禁说明

对 `src/BetterUnturnedExperience.Release` 运行通用 `Verify-NoUiTokens.ps1` 时，发现既有 `Qualification.cs` 的文本 `BepInEx` 命中；这不是 DEV-15B 实现引用，也不影响本票 ClientUi/Core/Contracts 隔离结果。该通用扫描项应作为既有 Release 门禁债务单独处理，不能被本票 PASS 解读为全仓零 Token。

## 未覆盖事项

真实 Glazier/Sleek 挂载、Unity/Harmony Hook、原生库存投影 Relay、实机三环境拖拽仍属于后续 DEV-15C～E/VO 门禁。

## 最终裁定

DEV-15B 的纯 C# Coordinate + Preview Wiring 满足工单验收条件，建议交 Gemini 进行前端消费复核；在 Gemini ACCEPT 前不得关闭工单。
