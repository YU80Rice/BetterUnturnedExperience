# GPT-DEV-15A 独立审计报告（R1）

## 一、审计范围与基线

- 审计提交：`e01ba2a8e5b85c1908dc6ffe05cfa63acd5361d4`（`Implement DEV-15A native drag adapter seam`）。
- 基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`、`spec-DEV-15-better-item-interaction.md` §3、§4、§6.1，以及 RT-02 B-01、RT-04、RT-06 原生库存权威边界。
- 范围：`NativeInventoryInteractionAdapter.cs`、`BetterUnturnedExperience.ClientUi.Tests/Program.cs`、ClientUi csproj、`issues/DEV-15A-native-drag-adapter.md`。
- 本审计为源码、构建和测试审计，不是实际 Unturned、SP、SteamP2PFriends Host/Client 或 U3DS 运行通过证明。

## 二、最终判定

**PASS（阻断项 0）**。

DEV-15A 的纯测试 Seam 满足当前工单边界：普通网格只在合法候选时经原生动作端口提交，普通网格非法候选停止增强拖拽并保留原物，快捷槽、AREA、非拖拽状态及同格取消不调用增强提交端口；未发现平行库存权威、Contracts/Core 类型泄漏或共享线程状态风险。

## 三、需求符合性矩阵

| 检查项 | 结论 | 证据 |
|---|---|---|
| 普通网格判定 | PASS | `NativeInventoryInteractionAdapter.cs:54-58,82-85`；`page >= slotsPageBoundary && page != areaPage`。 |
| 合法候选提交 | PASS | `:71-79`；仅 `PlacementPreviewState.Candidate` 调用 `StopDrag` 后一次 `SendDragItem`。 |
| 非法/越界候选 | PASS | `:71-75`；普通网格非 Candidate 只调用 `StopDrag`，不调用提交端口。 |
| 快捷槽、AREA、其他特殊页 pass-through | PASS | `:54-58`；不调用 `StopDrag` 或 `SendDragItem`，交还原生分支。 |
| 同格取消 | PASS | `:66-69`；返回 `PassThrough`，不重复提交。 |
| 非拖拽状态 | PASS | `:49-52`；直接 `PassThrough`。 |
| 陈旧代际 fail-closed | PASS（DEV-15A 范围） | `:60-64`；普通网格代际失配只停止拖拽并返回 `Cancelled`。SessionGeneration/投影代际属于 DEV-15C。 |
| 地面来源拖入普通网格 | PASS | 测试 `Program.cs:99-102`；来源页不参与普通网格目标判定，可提交原生端口。 |
| 原生权威边界 | PASS | Adapter 只有 `StopDrag`/`SendDragItem` 端口；未调用 `ReceiveDragItem`、`removeItem`、`addItem`、LMN。 |
| Contracts/Core 隔离 | PASS | Adapter 源文件只引用 `System` 与 `BetterUnturnedExperience.Contracts`；未引入 UI/引擎/网络具体类型。 |
| 线程与共享状态 | PASS（静态） | Adapter 仅持有不可变页边界，无静态可变状态、异步回调、锁或跨线程队列；实际游戏线程接线留待后续票。 |

## 四、测试证据

测试入口 `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs:85-131` 覆盖：

- 合法普通网格提交及来源/目标坐标保真；
- 地面来源 → 普通网格；
- equipment/快捷槽与 AREA pass-through；
- 普通网格非法候选；
- 非拖拽状态；
- 陈旧 DragGeneration；
- 同格取消。

本轮独立重建命令：

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
```

结果：退出码 `0`，构建输出无 errors/warnings（TreatWarningsAsErrors 已开启）。

七套 Release 测试均退出码 `0`：

```text
DEV-05 ClientUi tests: PASS
DEV-10 registration runtime tests: PASS
DEV-06 network tests: PASS
DEV-04 placement evaluator tests: PASS
DEV-14 official registration parity tests: PASS
DEV-08 runtime evidence package tests: PASS
DEV-03 settings runtime tests: PASS
```

## 五、产物身份

- `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll`
  - SHA-256：`A404B653293A50AD6BD555349E9CCDC42B0A3A4A2D0B838AF0D20A894AD71CCB`
- `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
  - SHA-256：`A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`
- `tests/BetterUnturnedExperience.ClientUi.Tests/bin/Release/BetterUnturnedExperience.ClientUi.Tests.exe`
  - SHA-256：`D713821339BB11E1C0A881F5F10131A8D9794E659A06F55BB91FF08D0519C944`

## 六、非阻断建议与边界

1. 测试入口仍输出 `DEV-05 ClientUi tests: PASS`，建议后续改为 DEV-15A 专属标签，避免证据归属歧义；不阻断本轮逻辑审计。
2. 当前是纯测试 Seam，尚未接入真实 `PlayerDashboardInventoryUI.onPlacedItem`/Harmony/Glazier，也未验证真实 `sendDragItem → ReceiveDragItem` 调用链；这些是后续 DEV-15B～15E 门禁，不能由本 PASS 外推。
3. `SessionGeneration`、投影 relay、`AwaitingProjection`、Enabled/AutoRotate 和故障隔离不属于本票，保持未宣称。

## 七、结论

DEV-15A 独立审计 **PASS**，可提交 Gemini 进行前端消费复核；在 Gemini `ACCEPT`、工单勾选及后续真实接线前，不得宣称 Better Item Interaction 功能完成或三环境运行通过。
