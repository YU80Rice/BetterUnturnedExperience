# DEV-16D RuntimeFix - R9 cleanup propagation

代码曾由 Gemini 负责，现由 GPT 接手。

## 一、问题定位与修复策略

R9 Spec 审查发现两个清理结果传播阻断：

1. `InventoryDragPreviewAdapter.IsolateAndDetach(bool)` 在组件隔离前未锁存 `detachSucceeded`。组件注册的反向清理重入 `IsolateAndDetach(false)` 时可能读到默认 `cleanupSucceeded=true`，吞掉 detach 失败。
2. `GridPlacedItemWrapper` 使用 `void component.IsolatePreviewFailure`，经 `Action` 包装后恒视为成功，无法把 placed-item 清理失败发布为 `CleanupIncomplete`。

最小修复位于 `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs`：先写入 `cleanupSucceeded = detachSucceeded`，再进入组件隔离；新增结果型 `Func<bool>` placed-item isolate/detach seam，同时保留原有 `Action` overload 的兼容包装。未修改单 DLL 装配、Headless 分流、原生库存权威链或现有 ABI。

## 二、源码溯源清单

| 需求/阻断 | 实现与测试 |
| :--- | :--- |
| detach 失败在组件重入清理中可见 | `InventoryDragPreviewAdapter.IsolateAndDetach`、`CompleteIsolationCleanup`；`Program.AssertDev16DR9CleanupPropagationContracts` |
| placed-item 隔离传播 `CleanupIncomplete` | `InventoryDragPreviewAdapter.InvokePlacedItemGuarded(Func<bool>...)`、`TryDetachResult`、`GridPlacedItemWrapper` |
| 回归进入默认测试套件 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` 默认入口调用 R9 断言 |

## 三、TDD 红绿证据

- 红测（编译级）：`audit/2026-08-31/DEV16D-r9-red.log`，修复前缺少 `CompleteIsolationCleanup` 与结果型 overload，MSBuild 退出 `1`。
- 红测（运行级）：`audit/2026-08-31/DEV16D-r9-red-runtime.log`，临时复现旧行为时断言“组件隔离观察到失败 detach”失败，测试退出 `1`。
- 绿测：`audit/2026-08-31/DEV16D-r9-green.log`，`--dev16d-r9-red` 退出 `0`。

## 四、编译、测试与静态门禁

- Release 全解决方案：`audit/2026-08-31/build-dev16d-r9b.log`，MSBuild `0 errors / 0 warnings`，退出 `0`。
- 7 项测试：`audit/2026-08-31/tests-dev16d-r9b.log`，Contracts、Settings、Placement、ClientUi、Network、Release、Plugin 全部 `PASS`，退出 `0`。
- UI/native token 门禁（ClientUi 11、Contracts 2、Core 10 个 C# 文件）：`audit/2026-08-31/static-gates-dev16d-r9b.log`，`STATIC_EXIT=0`。
- `git diff --check 5e5d12c...1549cd6`：退出 `0`。

## 五、独立审查记录

审查增量基线：`5e5d12c`。

| 轮次 | Standards | Spec | 说明 |
| :--- | :--- | :--- | :--- |
| R9 初审 | FAIL | CLEAN | Standards 发现 `tests-dev16d-r9.log` EOF 多余空行；Spec 确认两项代码阻断已闭合。 |
| R9 复审 | CLEAN | CLEAN | 日志空行修复后，`2ed2ddb`、`b99f5c1`、`1549cd6` 增量通过双轴审查。 |

审查报告均为全新实例、只读完成；非阻断建议（真实实例级重入测试、重复诊断 helper 抽取、测试横幅命名）不影响本轮交付，留待后续范围。

## 六、正式产物与身份

- 源 DLL：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- 归档 DLL：`audit/2026-08-31/artifacts/BetterUnturnedExperience.dll`
- 大小：`222208` bytes
- SHA-256：`98362DCF309C5AECD30A4D9386079D116F50023FDD47BFAB210D9653A7A397E4`
- CandidateBuild：`DEV-16D-r9-clean-20260831`
- CaseId：`DEV-16D-R9-20260831`
- SourceSnapshotId：`1549cd637f98e4fe5103be53e842cfd3af105c90`
- BuildIdentity：`D4749337D66B90727078661AC8861DEB5625F17890C116B203EFCF236BC05619`
- DefinitionSetDigest：`7E86365DC24010875A6FAD96833138E9D79D7D2CB10EB67DF5BEF0EA70753D7C`
- ArtifactPayloadDigest：`7DC14ABE233C3CB5BEC9B1EF0AEE855B91EACFDDA7C6E636CEADD2AB1AD704E7`
- ClientReferenceSetId / U3dsReferenceSetId：`Libs-ReferenceSet-CA9AFA1D47CFE1DCA74431CCA755366F8BA51F0401BB79DD7058AE1DED3C6A75`

以上 CandidateBuild/CaseId 是双轴 CLEAN 后授予的 reviewed artifact identity；单人、SteamP2PFriends Host/Client、U3DS Headless 的真实运行资格仍需使用该同哈希产物另行采集，不能由本轮静态证据代替。

## 七、部署与复测

1. 将唯一文件 `audit/2026-08-31/artifacts/BetterUnturnedExperience.dll` 复制到游戏的 `BepInEx/plugins/`，替换旧 BUE DLL；不要同时放入旧版本副本，也不需要 `Contracts.dll`、`Core.dll` 或 NoOpFixture。
2. 用以下命令核对部署文件哈希（将路径替换为实际游戏目录）：

```powershell
certutil -hashfile "<Unturned>\BepInEx\plugins\BetterUnturnedExperience.dll" SHA256
```

应得到：`98362DCF309C5AECD30A4D9386079D116F50023FDD47BFAB210D9653A7A397E4`。
3. 启动单人客户端，启用 Better Item Interaction 后依次测试玩家背包、普通容器和车辆后备箱；保留 UMM 诊断包以及 `BepInEx/LogOutput.log`。
4. 重点观察拖拽清理异常时是否出现 `BUE-DEV15D-CLEANUP-INCOMPLETE`，并确认原生回退仍可用。

## 八、最终结论

双轴独立审查已 `CLEAN`，正式 DLL 已归档，R9 cleanup 结果传播阻断已关闭。**现在可以开始实机复测。**
