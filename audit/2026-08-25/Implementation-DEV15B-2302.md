# DEV-15B 坐标与预览接线实施报告

## 一、需求执行概述

完成 Better Item Interaction 的纯 C# Coordinate + Preview Wiring：屏幕坐标转换、UI Scale/滚动/viewport 裁剪、JCR-07 Forward 抓取偏移、Evaluator 代际接线，以及绿色/红色/隐藏预览渲染命令。

本票没有实现真实 Unity/Glazier/Sleek Hook；该部分交由 Gemini 前端表现层消费接入，不将静态 Seam 证据冒充实机功能。

## 二、源码溯源矩阵

| 规格/工单要求 | 落实位置 |
|---|---|
| UI Scale、滚动、屏幕到连续网格坐标 | `src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs:81-128` |
| `intendedItemCenterGrid` 公式 | `InventoryGridCoordinateAdapter.TryCreateCandidateInput` |
| JCR-07 `(gx,gy) → (H-gy,gx)` | `TryRotateGrabOffset` |
| Evaluator + DragGeneration 接线 | `InventoryPreviewPresenter.Update`、`InventoryDragPresenter.Evaluate` |
| 合法绿色框与图标 | `InventoryPreviewPresenter.Update` Candidate 分支 |
| 非法红框、隐藏图标 | `InventoryPreviewPresenter.Update` LocallyInvalid 分支 |
| Hidden/Pending/陈旧回退 | `InventoryPreviewPresenter.Update` 与 `InventoryDragPresenter` |
| 纯值渲染边界 | `IInventoryPreviewSink`, `PreviewFrame`, `PreviewIcon` |
| TDD Red → Green | `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs` |

## 三、代码变更清单

- 新增 `src/BetterUnturnedExperience.ClientUi/InventoryPreviewWiring.cs`；
- 新增 `tests/BetterUnturnedExperience.ClientUi.Tests/Dev15BTests.cs`；
- 修改 `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj` 纳入实现；
- 修改 `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs` 纳入 DEV-15B 测试；
- 新增 DEV-15B 工单与 GPT→Gemini 交接包。

## 四、TDD 与编译验证

### Red → Green

首轮红测试因坐标/预览类型不存在而失败；随后按最小实现补齐坐标 adapter、预览 sink 和 presenter。独立审计 R1 又发现 JCR-07 旋转偏移与图标锚点缺口；新增非对称抓取偏移红测试并修复，最终 R2 PASS。

### 构建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：0 errors / 0 warnings。

### 测试

7/7 Release 测试 PASS：Contracts、Settings、Placement、ClientUi、Network、Plugin、Release。

ClientUi 测试覆盖：UI Scale/滚动、viewport 外隐藏、当前旋转 footprint、Forward 非对称抓取偏移、绿色/红色/Hidden/Pending、陈旧代际、图标锚点、10,000 次热路径 0 bytes。

### 静态门禁

Contracts/Core/ClientUi/Transport UI/native token scan PASS。既有 Release `Qualification.cs` 的 `BepInEx` 文本命中属于既有扫描债务，不是 DEV-15B 实现泄漏；未修改该无关模块。

## 五、独立审计记录

- R1：FAIL，阻断项为 Forward 抓取偏移和图标锚点；报告 `GPT-DEV-15B-Independent-Audit-R1.md`；已修复。
- R2：PASS，阻断项 0；报告 `GPT-DEV-15B-Independent-Audit-R2.md`。

## 六、产物哈希

- `InventoryPreviewWiring.cs`: `88073877388963F50F97E8010EEF7EE59A7675D8E200BBB0DDC3784B66589878`
- `Dev15BTests.cs`: `3F9A69C389D6283E5B8225E4C654A0B7103E898288BDDF799A42ED0D3F512073`
- `BetterUnturnedExperience.ClientUi.dll`: `A354046A9FB3C23F9C6920DD0D727AA53E52D398232B496AC44A559E626013C8`
- `BetterUnturnedExperience.dll`: `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`

## 七、偏离与剩余门禁

无需求偏离。真实 Glazier/Sleek 挂载、Unity/Harmony Hook、原生投影 Relay、单人/P2P/U3DS 运行和发布资格仍未证明，分别属于 Gemini 表现层接入及 DEV-15C～E/VO 门禁。

## 八、最终裁定

DEV-15B 纯 C# Seam 可交 Gemini 前端消费复核；在 Gemini `ACCEPT` 前保持 `ready-for-human`，不得标记 `resolved`，不得宣称 Better Item Interaction 已完成。
