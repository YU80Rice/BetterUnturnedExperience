# 🛠️ 缺陷修复执行报告 - DEV-16B R8（2026-08-28）

### 一、问题定位与修复策略

- **根因**：诊断包 `UMM-诊断包_20260828_104216` 证明 R7 主 DLL 已加载且 Harmony 目标登记成功，但没有 `Start`、`Update`、`host-ui-tick`、`surface-opened`、`create-button` 或 `add-child-success`，所以按钮注入链没有被真实宿主驱动。
- **修复策略**：新增独立 `BueRuntimePumpBehaviour` 作为 Unity 主线程泵；由 `BueRuntimePump` 转发到管理面板 Tick 和 RuntimeReady 屏障；加入异常回滚、重复初始化幂等、泵异常后的 fail-closed 原生回退日志与启动门禁日志。

### 二、核心代码变更

- `src/BetterUnturnedExperience.Plugin/BueRuntimePump.cs`
  - 新增可测试的 `BueRuntimePump`、幂等 `BueRuntimePumpSlot`。
  - `BueRuntimePumpBehaviour.Attach` 使用局部 `GameObject`，异常时显式销毁。
- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
  - 客户端初始化后挂载独立泵；销毁时先清回调、再销毁对象。
  - 增加 `runtime-gate`、`runtime-pump-created`、`runtime-pump-tick`、`runtime-pump-isolated` 结构化诊断。
- `src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs`
  - 增加 `RuntimePump` Tick 来源标识。
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
  - 增加泵转发、清理与 Slot 幂等回归断言。

### 三、编译与自测状态

- **编译状态**：`dotnet build BetterUnturnedExperience.sln -c Release --no-restore` → `0 errors / 0 warnings`。
- **测试结果**：7 个 Release 测试程序全部 PASS；Contracts/Core UI-native token scan 全部 PASS。
- **主 DLL SHA-256**：`9FFCFE53497023872D2EB132BAA037FD891629C77585A694F3BC8A111E21E4AA`。
- **部署产物**：`artifacts/DEV-16B-management-panel-runtime-fix-r8-20260828/BetterUnturnedExperience.dll`。

### 四、子智能体审核记录

| 审核项 | 判定 | 说明 |
| :--- | :--- | :--- |
| 运行泵异常回滚 | PASS | `Attach` 失败时清理局部对象；插件异常路径调用统一清理。 |
| 重复初始化幂等 | PASS | `BueRuntimePumpSlot.GetOrCreate` 与有效 Behaviour 快返。 |
| Headless 分流 | PASS（静态） | 运行泵只在 `BootstrapDecision.Client` 分支创建。 |
| 单 DLL ABI | PASS（静态） | 主 DLL 不引用私有 Core/Contracts 运行时程序集。 |
| 真实客户端可见性 | **待人工验证** | R8 尚无真实游戏日志/截图，不能宣称 DEV-16B 已关闭。 |

### 五、最终结论

- 代码修复与静态审计完成，可移交人工部署验证。
- DEV-16B 继续保持 `ready-for-human`，不得因本地构建/单元测试通过而标记 `resolved`。
- 人工测试应部署上述 R8 DLL，并导出包含同一 SHA 的完整 UMM 诊断包；重点观察 `runtime-pump-created`、`runtime-pump-tick`、`create-button-begin`、`add-child-success` 及按钮点击/面板打开证据。
