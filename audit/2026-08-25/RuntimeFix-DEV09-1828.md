# DEV-09 单 DLL BepInEx 冒烟诊断报告

## 一、问题定位

用户症状：BepInEx 日志窗口未出现 BUE 预期启动日志，怀疑单 DLL 构建失败。

结论：**本地编译没有失败；失败的是“仅部署一个 DLL”的运行时依赖闭包。** 当前 `BetterUnturnedExperience.dll` 仍通过 IL 引用 `BetterUnturnedExperience.Core` 与 `BetterUnturnedExperience.Contracts`，而实际游戏 `BepInEx\plugins` 目录只有主 DLL。

## 二、证据

| 证据 | 结果 |
|---|---|
| Release 重建 | 0 errors / 0 warnings |
| 构建产物 | `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` |
| 游戏部署产物 | `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll` |
| 构建/部署 SHA-256 | 均为 `4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2` |
| 实际插件目录 | 只有 `BetterUnturnedExperience.dll`，没有 Core/Contracts DLL |
| IL 证据 | 主 DLL 引用 `[BetterUnturnedExperience.Core]` 与 `[BetterUnturnedExperience.Contracts]` 类型 |
| 最小红灯复现 | 仅主 DLL 时，依赖闭包检查稳定报告缺少 `BetterUnturnedExperience.Core`、`BetterUnturnedExperience.Contracts` |
| BepInEx 日志 | 记录 `Loading [Better Unturned Experience 0.0.0]`、`Chainloader startup complete`，但没有 BUE `BootstrapReady` 或 `BootstrapFailed` |
| UMM 诊断摘要 | 退出码 0，但不能证明插件实例化和 `Awake` 成功 |

## 三、诊断结论

- **不是 C# 编译失败。** DLL 已成功生成且哈希一致。
- **不是已证实的 BepInEx 显式异常。** 捕获日志没有给出 `TypeLoadException`/`FileNotFoundException`；日志可能在类型发现/实例化阶段被截断或 BepInEx 没有输出该细节。
- **可以确认单 DLL 产品要求尚未满足。** 当前产物是“主 DLL + Core DLL + Contracts DLL”的多程序集构建结果，不能按“只复制 BetterUnturnedExperience.dll”部署。
- BepInEx 的 `Loading` 行只能证明它识别到插件元数据，不能证明 `Awake` 已执行；缺少 `BootstrapReady` 是本次运行未通过的关键观测。

## 四、最小确认实验（未执行生产修改）

将同一 Release 构建中的 `BetterUnturnedExperience.Core.dll` 与 `BetterUnturnedExperience.Contracts.dll` 临时放在游戏 `BepInEx\plugins` 旁边，再启动一次：

- 若出现 `featureId=... status=BootstrapReady`，即可确认根因是依赖闭包缺失；
- 若仍无 BUE 日志，再进入 BepInEx 类型解析/Unity 运行时版本假设的第二层诊断。

该实验只能确认运行时装载链，不能把多 DLL 临时部署当作单 DLL 验收通过。

## 五、下一步建议

新增或重开“单 DLL Packaging/Assembly Closure”实施票，明确选择并验证：

1. IL 合并/链接为一个 BUE 主程序集；或
2. 重新组织源码，使 Contracts/Core 在最终插件项目内编译为同一程序集；或
3. 明确产品改为多 DLL 部署（这将偏离当前用户需求，需人工批准）。

在此问题修复前，DEV-09 只能保持 `ready-for-human`，不得标记 `resolved`，也不得宣称真实 BepInEx 加载通过。

## 六、复测更新（18:16:04）

人工以多 DLL 方式复测后，诊断包 `UMM-诊断包_20260825_181604` 的 `LogOutput.log` 出现：

```text
Loading [Better Unturned Experience 0.0.0]
Better Unturned Experience featureId=io.github.yu80rice.betterunturnedexperience status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001
Chainloader startup complete
```

部署的三个 DLL 与 Release 构建产物哈希完全一致：

- `BetterUnturnedExperience.dll`：`4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2`
- `BetterUnturnedExperience.Core.dll`：`630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF`
- `BetterUnturnedExperience.Contracts.dll`：`1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD`

这确认：**多 DLL 运行链已通过；单 DLL 依赖闭包仍未通过。** 后续处理票为 `DEV-12-single-dll-runtime-assembly-closure.md`。
