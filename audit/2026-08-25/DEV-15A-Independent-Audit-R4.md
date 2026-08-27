# GPT-DEV-15A 独立审计报告（R4）

## 一、固定审计对象

- HEAD：`0a44114380941aa758f8e339746f8071a9110b10`（`Implement DEV-15A native drag adapter seam`）
- `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs`
- `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`
- `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs`
- `.scratch/better-unturned-experience-architecture/issues/DEV-15A-native-drag-adapter.md`
- 基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`、`spec-DEV-15-better-item-interaction.md` §3、§4、§6.1

证据边界：本审计覆盖源码、静态类型隔离、Release 编译和自动化测试；不证明真实 Unturned、SP、SteamP2PFriends Host/Client、U3DS 或三环境运行/发布资格。

## 二、最终判定

**PASS（阻断项 0）**

## 三、重点顺序与行为核验

### 1. 合法候选的原生动作顺序

`NativeInventoryInteractionAdapter.cs:76-78` 明确执行：

```text
SendDragItem(source, target) → StopDrag() → Submitted
```

测试 `Program.cs:93-96` 断言 `SendCount == 1`、`StopCount == 1` 且 `OperationOrder == "send>stop"`。因此合法普通网格候选不会在提交前取消原生拖拽。

### 2. 代际先验

`NativeInventoryInteractionAdapter.cs:54-57` 在 `IsDragging` 判断之后、目标页面分类之前校验 `Preview.DragGeneration`。任何拖拽中的代际失配（普通页、快捷槽、AREA 或其他特殊页）统一返回 `Cancelled`，不调用任何原生动作。

测试 `Program.cs:114-117` 和 `128-130` 分别覆盖陈旧特殊页、陈旧普通页，并断言 `StopCount == 0 && SendCount == 0`。

### 3. 同格合法/非法区分

状态校验位于同格判断之前：

- 合法 `Candidate` + 同页/坐标/旋转相同：`Program.cs:132-135` 断言 `PassThrough` 且零原生动作；
- `LocallyInvalid` + 同位置：`Program.cs:137-141` 断言 `Cancelled`、调用一次 `StopDrag`、不调用提交。

这避免了把非法同格反馈误当成同格取消。

### 4. 特殊分支

当前代际的快捷槽和 AREA 在 `:59-63` 返回 `PassThrough`，测试 `:104-112` 断言不触发 `StopDrag` 或 `SendDragItem`。陈旧特殊页则被更早的代际门禁拒绝，不会落入原生 pass-through。

## 四、需求符合性矩阵

| 审计项 | 判定 | 证据 |
|---|---|---|
| 普通网格合法候选 | PASS | 当前代际、`Candidate` 状态下只调用一次 `SendDragItem`，随后一次 `StopDrag`。 |
| 合法提交顺序 | PASS | `:76-78` 与 `OperationOrder == "send>stop"` 测试。 |
| 普通网格非法/越界候选 | PASS | `:65-69` 只调用 `StopDrag`，不提交。 |
| 陈旧普通/特殊候选 | PASS | 代际校验前置，零原生动作。 |
| 未拖拽 | PASS | `:49-52` 零原生动作、`PassThrough`。 |
| 特殊页 pass-through | PASS | 当前代际特殊页零原生动作；陈旧特殊页不放行。 |
| 同格取消 | PASS | 仅对合法 `Candidate` 同格输入 pass-through；非法同格仍取消。 |
| 地面来源拖入 | PASS | 测试 `:98-102`，目标普通网格仍走增强提交。 |
| 原生权威边界 | PASS | Adapter 仅依赖 `StopDrag`/`SendDragItem` 委托，不调用平行库存 RPC、LMN 或服务端权威逻辑。 |
| 类型/UI 边界 | PASS | Adapter 仅引用 `System` 与 `BetterUnturnedExperience.Contracts`；禁用类型/扫描 API 检查无命中。 |
| DEV-15B～E 越界 | PASS | 未实现坐标转换、预览、投影、设置、生命周期、真实 Hook 或三环境证据。 |
| 编译 | PASS | `dotnet build BetterUnturnedExperience.sln -c Release --no-restore`：0 errors、0 warnings。 |
| 测试 | PASS | 7 个 Release 测试全部退出码 0。 |

## 五、当前产物身份

- `NativeInventoryInteractionAdapter.cs`：`D6D4F33FDEFA2F5C5FE8D8EF3BE578AE75576AF8E282D36B1EF0FD5855BE4AB7`
- `Program.cs`：`752E81CEC826ECA2EF0C56C04681BB4EA8EF7F56F21CBF216D4D891529542EAE`
- `BetterUnturnedExperience.ClientUi.csproj`：`659D2F20788EBB069606940203A9A1F22CBEA2C59A6BFD38CE94C34F16693402`
- `BetterUnturnedExperience.ClientUi.dll`：`473F4ABB1A924B6B78DD9ABAEFABEBF670EB0D73A3CC5CA09139404C1D355B46`
- `BetterUnturnedExperience.ClientUi.Tests.exe`：`54A8AE6F63EE4B5FC2F32F5C9266885122343DDB6B4F91925036529E31D62DCC`

## 六、残余边界

本 PASS 仅代表 DEV-15A 纯值 Adapter Seam。真实 `PlayerDashboardInventoryUI.onPlacedItem` 接线、原生 `sendDragItem → ReceiveDragItem` 运行链、Glazier 预览、投影 Relay、单人/P2P/U3DS 验证仍属于 DEV-15B～E，不能由本报告推断通过。

## 七、结论

HEAD `0a44114380941aa758f8e339746f8071a9110b10` 的 DEV-15A 实现满足最终动作顺序、代际先验、特殊页隔离、同格合法/非法区分、类型边界和构建测试门禁，独立审计 **PASS**。可交 Gemini 进行前端消费复核；Gemini `ACCEPT` 前不得关闭 DEV-15A。
