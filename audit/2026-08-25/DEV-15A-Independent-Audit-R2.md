# GPT-DEV-15A 独立审计报告（R2）

## 一、审计范围与证据边界

审计对象：

- `src/BetterUnturnedExperience.ClientUi/NativeInventoryInteractionAdapter.cs`
- `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`
- `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs`
- `.scratch/better-unturned-experience-architecture/issues/DEV-15A-native-drag-adapter.md`

基线：`BUE-V1-RT01-20260824`、`BUE-SS-20260824-02`、`spec-DEV-15-better-item-interaction.md` §3、§4、§6.1。

本报告仅覆盖源码、静态隔离、编译及单元测试；不把本结果外推为真实 Unturned、SP、SteamP2PFriends Host/Client、U3DS 或三环境通过。

## 二、最终判定

**FAIL（B-01：陈旧代际在特殊页分支上的 fail-closed 保护缺失）**

## 三、通过项

| 审计项 | 判定 | 证据 |
|---|---|---|
| 合法普通网格提交 | PASS | `NativeInventoryInteractionAdapter.cs:54-78`：当前代际、`Candidate` 状态只调用一次 `SendDragItem`；合法路径不再先调用 `StopDrag`。 |
| 特殊页 pass-through | PASS（非陈旧输入） | `:54-58`：快捷槽、AREA 等非普通目标不调用两个增强动作端口并返回 `PassThrough`。 |
| 普通网格非法候选 | PASS | `:70-74`：调用 `StopDrag`，不调用 `SendDragItem`。 |
| 未拖拽 | PASS | `:49-52`：不调用原生动作。 |
| 陈旧普通候选 | PASS | `:60-63`：当前测试期望 `Cancelled` 且 `StopCount == 0`，不提交、不调用原生动作。 |
| 同格取消 | PASS（当前坐标语义） | `:65-68`：不重复提交，返回 `PassThrough`。 |
| 类型隔离 | PASS | Adapter 仅引用 `System` 与 `BetterUnturnedExperience.Contracts`，无 Unity/Glazier/Sleek/LMN/Unturned/BepInEx/Harmony 类型。 |
| 范围控制 | PASS | 未实现坐标转换、预览、投影 Relay、设置/生命周期、真实原生 Hook 或库存 RPC。 |
| 编译与测试 | PASS | `dotnet build BetterUnturnedExperience.sln -c Release --no-restore`：0 errors、0 warnings；7 个 Release 测试退出码均为 0。 |

## 四、阻断项 B-01

当前 `HandleRelease` 先在 `:55-58` 按目标页分类，再在 `:60-63` 校验 `Preview.DragGeneration`。因此，当输入满足以下条件时：

- `IsDragging == true`；
- `Preview.DragGeneration != input.DragGeneration`；
- `Preview.Candidate.Page` 属于快捷槽、AREA 或其他特殊页；

方法会直接返回 `PassThrough`，不会进入代际失配分支。若调用方依据 `PassThrough` 继续原生释放，旧代际拖拽可触发装备、丢弃或其他特殊原生行为，违反 DEV-15A 验收条件“代际不匹配时 fail-closed，不触发任何原生动作”及规格 §4 的旧代际不得污染当前交互约束。

### 修复要求

将代际检查置于普通/特殊页分类之前，并冻结其调用语义。推荐：

1. `!IsDragging` 仍返回 `PassThrough`，因为没有增强拖拽；
2. 一旦 `IsDragging` 且代际失配，返回 `Cancelled`，不调用 `StopDrag`/`SendDragItem`；
3. 只有当前代际输入才按普通网格增强或特殊页 pass-through；
4. 增加“陈旧特殊页”测试，断言结果为 `Cancelled` 且两个原生动作计数均为 0。

## 五、非阻断建议

1. `IsSamePlacement` 仅比较 page、x、y、rotation；若不同容器可复用相同 page/坐标，应在后续 seam 明确 page 全局唯一，或携带 `ContainerReference`，避免跨容器拖入被误判为同格取消。
2. 测试入口仍输出 `DEV-05 ClientUi tests: PASS`，建议改为 `DEV-15A ClientUi tests: PASS`，避免证据归属混淆。
3. `native == null` 当前抛出 `ArgumentNullException`；现有代码库依赖注入入口普遍采用该约定，暂不阻断，但生产 Hook 需将其纳入功能隔离边界。

## 六、当前产物哈希

- `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll`
  - `41737582AE2EB25D1BA641BFA1990CA301F97EBC9A7E010CADA4910FB00EEC53`
- `tests/BetterUnturnedExperience.ClientUi.Tests/bin/Release/BetterUnturnedExperience.ClientUi.Tests.exe`
  - `19B589300B5CD2E5579E6A85358673CF6D9C2A71BD14E056C046532F4FAD9CEB`

## 七、结论

当前实现已解决合法候选误调用 `StopDrag` 的问题，编译与七项测试通过；但陈旧特殊页仍可 `PassThrough`，故独立审计暂为 **FAIL**。修复 B-01 后须重新编译、运行全套测试并进行下一轮独立审计。
