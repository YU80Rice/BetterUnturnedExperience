# GPT-DEV-15A 独立审计报告（R3）

## 一、审计范围与证据边界

审计对象：

- `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs`
- `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`
- `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs`
- `.scratch/better-unturned-experience-architecture/issues/DEV-15A-native-drag-adapter.md`

基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`、`spec-DEV-15-better-item-interaction.md` §3、§4、§6.1。

本报告仅覆盖源码、静态边界、Release 编译及单元测试；不将本结果外推为真实 Unturned、单人、SteamP2PFriends Host/Client、U3DS 或三环境运行通过。

## 二、修复复核

R2 阻断项 B-01 已修复：

- `NativeInventoryInteractionAdapter.cs:54-57` 在 `IsDragging` 后、目标页面分类前执行 `DragGeneration` 校验；陈旧普通页与陈旧特殊页统一返回 `Cancelled`。
- 陈旧路径不调用 `StopDrag` 或 `SendDragItem`。
- `Program.cs:114-117` 新增陈旧特殊页测试，断言两个原生动作计数均为 0。
- `Program.cs:128-130` 陈旧普通候选同样断言零原生动作。

## 三、最终判定

**PASS（阻断项 0）**

## 四、需求符合性矩阵

| 审计项 | 判定 | 证据 |
|---|---|---|
| 合法普通网格候选 | PASS | `NativeInventoryInteractionAdapter.cs:59-77`：仅普通网格、当前代际、`Candidate` 状态调用一次 `SendDragItem`；合法路径不调用 `StopDrag`。 |
| 特殊页 pass-through | PASS | `:59-63`：当前代际的快捷槽、AREA 等特殊目标返回 `PassThrough`，不调用两个增强动作端口。 |
| 陈旧普通页/特殊页 fail-closed | PASS | `:54-57`：代际校验在页面分类之前；两类陈旧输入均无原生动作。测试 `Program.cs:114-117,128-130`。 |
| 非法普通候选 | PASS | `:70-74`：非 `Candidate` 只调用 `StopDrag`，不调用 `SendDragItem`。 |
| 未拖拽 | PASS | `:49-52`：不触发任何原生动作。 |
| 同格取消 | PASS | `:65-68`：页、坐标、旋转完全相同则 `PassThrough`，不重复提交。 |
| 地面来源拖入普通网格 | PASS | `Program.cs:98-102`：来源页不改变目标普通网格增强判定，提交一次原生请求。 |
| 原生权威边界 | PASS | Adapter 仅持有 `StopDrag`/`SendDragItem` 端口；未调用 `ReceiveDragItem`、`removeItem`、`addItem`、LMN 或并行库存 RPC。 |
| 类型/UI 隔离 | PASS | Adapter 文件只引用 `System` 与 `BetterUnturnedExperience.Contracts`；禁用词扫描未发现 Unity、Glazier、Sleek、LMN、Unturned、BepInEx、Harmony 或反射/Hook 扫描调用。 |
| 任务范围 | PASS | 未实现坐标换算、预览图元、投影 Relay、AwaitingProjection、Settings/生命周期、真实原生 Hook 或三环境证据；未越界到 DEV-15B～E。 |
| Release 构建 | PASS | `dotnet build BetterUnturnedExperience.sln -c Release --no-restore`：0 errors、0 warnings。 |
| Release 测试 | PASS | ClientUi、Contracts、Network、Placement、Plugin、Release、Settings 七个测试均退出码 0。 |

## 五、残余风险与非阻断建议

1. 当前仍是纯测试 Adapter Seam，不证明真实 `PlayerDashboardInventoryUI.onPlacedItem`/原生回调栈、`sendDragItem → ReceiveDragItem` 或 Unity 游戏线程接线；该验证属于后续 DEV-15B～15E。
2. `IsSamePlacement` 仅比较 `ItemGridPosition` 的页、坐标和旋转。后续若 page/坐标不具备跨容器全局唯一性，应补充 `ContainerReference` 或明确原生 page 身份约束；本票纯值 Seam 暂不阻断。
3. 测试入口已输出 `DEV-05/DEV-15A ClientUi tests: PASS`，仍带历史阶段标签，后续可改为 DEV-15A 专属标签以减少证据归属歧义；不阻断。
4. `native == null` 仍按代码库惯例抛出 `ArgumentNullException`；生产 Hook 需在更高层纳入功能隔离和异常诊断，不属于本票阻断。

## 六、当前产物身份

- `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs`
  - SHA-256：`4615B3596C7BD6E4448A078C3FFA1296A6447103014E1679A1717CED45A3A086`
- `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs`
  - SHA-256：`1FDE93202C2C7580A3634CC90FED2D29173D1AFCFCACBFA74345E6EC4EEF0F3C`
- `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`
  - SHA-256：`659D2F20788EBB069606940203A9A1F22CBEA2C59A6BFD38CE94C34F16693402`
- `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll`
  - SHA-256：`1F3E4594EC7503AA06F6AED6615C85F3C231478EABF973C6ED294FC7E16474BF`
- `tests/BetterUnturnedExperience.ClientUi.Tests/bin/Release/BetterUnturnedExperience.ClientUi.Tests.exe`
  - SHA-256：`F1103B4C7F36F9AD05A270AEF5ABFC99FA19BBDE01B4226D38445850033CF873`

## 七、结论

DEV-15A 当前满足本票的 Adapter 行为、fail-closed、特殊分支、类型隔离、范围边界与自动化构建/测试门禁，独立审计 **PASS**。可交 Gemini 进行前端消费复核；在 Gemini `ACCEPT` 前不得关闭工单，也不得宣称真实玩法或三环境通过。
