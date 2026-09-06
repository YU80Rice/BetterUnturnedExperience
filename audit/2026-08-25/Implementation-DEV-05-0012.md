# GPT DEV-05 实施报告

## 1. 任务与边界

- 任务：ClientUi / Glazier composition seam 与 Presenter 最小实现。
- 基线：`BUE-V1-RT01-20260824`。
- SourceSet：`BUE-SS-20260824-02`。
- 范围：显式 ClientUi 注册表、`ClientUiAvailable && !IsBatchMode && !Headless` 门禁、组件局部隔离与清理、拖动代际防护、设置快照只读消费。
- 未实现：真实 Glazier/Sleek Hook、Harmony、原生库存提交、LMN、DEV-06/DEV-07、三环境运行与发布授权。

## 2. TDD 过程

1. **Red**：先加入 ClientUi 项目和测试，因实现文件不存在得到 `CS2001`。
2. **Green**：加入 `ClientUiTypes`、`InventoryDragPresenter`、`SettingsSnapshotPresenter` 后通过。
3. **独立审计 Round 1**：FAIL，发现初始化与生命周期异常未保证 `OnUiDestroyed` 清理。
4. **修复与复测**：异常路径 best-effort cleanup，并补齐不可用/Headless/异常清理测试。
5. **独立审计 Round 2**：FAIL，发现 Destroy 异常时 cleanup 可能重复调用。
6. **修复与复测**：Destroy 先移除活动槽位，再执行一次清理；补充 `ThrowOnDestroyed` 单次调用断言。
7. **独立审计 Round 3**：PASS，无阻断项。

## 3. 源码溯源

| 需求 | 落实位置 |
|---|---|
| 三条件 UI 装配门禁 | `src/BetterUnturnedExperience.ClientUi/ClientUiTypes.cs:ClientUiEnvironment.CanCompose` |
| 构建期显式注册表 | `ClientUiRegistration`、`GeneratedClientUiRegistry`；无类型扫描 |
| 组件局部隔离 | `ClientUiCompositionRoot.Initialize/OpenInventory/CloseInventory` |
| 清理不重复 | `ClientUiCompositionRoot.Destroy` 先移除槽位后单次 `OnUiDestroyed` |
| 拖动代际守卫 | `InventoryDragPresenter.BeginDrag/EndDrag/Evaluate` |
| 快照只读过滤 | `SettingsSnapshotPresenter.GetVisibleEntries` |
| TDD seam | `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs` |

## 4. 构建与测试证据

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`0 errors / 0 warnings`。

| 测试 | 结果 |
|---|---|
| DEV-02 definition linker | PASS |
| DEV-03 settings runtime | PASS |
| DEV-04 placement evaluator | PASS |
| DEV-05 ClientUi | PASS |
| Contracts/Core UI token scan | PASS |
| ClientUi reflection/native token scan | PASS |

## 5. 产物哈希

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `A9387A4DAFEFAA0A5ED809C379599B9F0F71B1490FE73F7A741A762CFC925FC2` |
| `tests/BetterUnturnedExperience.ClientUi.Tests/bin/Release/BetterUnturnedExperience.ClientUi.Tests.exe` | `095670EF7C6C064DD463808F8A4436BCDF464345428C1B686C1B730970E9FB0A` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `068A6DD7D14C0FA004E36C38F4728ADC76A2701B40F11FE3CACE14F297E3F23F` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `6F502668701A34FB12E707E29086BBF9A2B2F3F391C88A6E616F168788359B08` |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |

## 6. 独立审计

第三轮独立子智能体审计：`PASS`，阻断项 0。审计确认构建、全套测试、门禁、异常清理、代际拒绝、快照消费和依赖方向均通过。

## 7. 证据边界

本报告不证明真实 Glazier/Sleek Hook、Unturned 原生 UI/库存链、单人、SteamP2PFriends Host/Client、U3DS 或发布授权通过。DEV-05 仍需 Gemini 前端消费复核后才能关闭。
