# DEV-16D-R13 RuntimeFix — native delegate lifecycle closure

代码曾由 Gemini 负责，现由 GPT 接手。

## 一、问题定位与修复策略

R13-5 Spec 审查的唯一阻断是测试没有进入生产 native delegate seam：页面回调只记录页码并丢弃组件 surface，没有实际执行 `InventoryDragPreviewAdapter.DetachGrid(page)`，因此无法证明 `SleekItems.onPlacedItem` 的 exact original 恢复、页面隔离、可逆重绑、幂等 detach 或拖拽来源清理。

R13-6 最小修复：

- 把 `AttachNativeGrid(SleekItems,page)` 作为生产与测试共用的 native seam；每个绑定保存 exact original delegate 与独立 wrapper delegate。
- `DetachGrid(page)` 仅在当前 delegate 仍是该 wrapper 时恢复 exact original，并按页移除绑定；已解绑页面重复 detach 返回成功且不触发 native callback。
- 增加有序 `DetachGridAndDiscardSurface(page)`，固定先 detach native delegate，再清理组件 surface；生产 `BetterUnturnedExperiencePlugin` 页面重建回调改用此路径，保留 adapter 缺失时的组件回退。

## 二、源码溯源清单

| 需求/阻断 | 落实位置 |
| :--- | :--- |
| 双页面 native wrapper 与 exact original 保存 | `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs:131-141,162-228` |
| 页面局部 detach、幂等和生产顺序 | `src/BetterUnturnedExperience.Plugin/InventoryDragPreviewAdapter.cs:230-260` |
| 生产页面重建回调 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs:101-107` |
| native delegate 生命周期红测 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:498-597,614-639` |
| 工单/阻断/责任变更记录 | `.scratch/better-unturned-experience-architecture/issues/DEV-16D-R13-native-conformance-remediation.md` |

## 三、TDD 红→绿证据

- 红测：先新增 `--dev16d-r13-native-delegate-red` 回归入口并编译；修复前因缺少 `AttachNativeGrid` 与 `DetachGridAndDiscardSurface` 生产 seam 触发 `CS1061`，退出码 `1`（`PAGE_NATIVE_DELEGATE_RED_EXIT=1`）。
- 绿测：修复后执行同一入口，退出码 `0`；测试覆盖 Backpack/Storage 双 wrapper、非当前页重建、Storage wrapper 保留、exact original 恢复、DragOrigin/generation 清零、原生回退、重复 detach 和重绑恢复。证据：`audit/2026-09-01/tests-dev16d-r13-r6-targeted.log`。

## 四、编译、测试和静态门禁

- Release 命令：

  ```powershell
  & 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe' `
    'BetterUnturnedExperience.sln' /t:Build /p:Configuration=Release /v:minimal
  ```

- Release 结果：`0 errors / 0 warnings`，退出码 `0`。证据：`audit/2026-09-01/build-dev16d-r13-r6.log`。
- 七个测试运行器全部 PASS、退出码 `0`：证据 `audit/2026-09-01/tests-dev16d-r13-r6.log`。
- 九项 R13 定向测试全部 PASS、退出码 `0`：证据 `audit/2026-09-01/tests-dev16d-r13-r6-targeted.log`。
- `Verify-NoUiTokens.ps1`：ClientUi 11、Contracts 2、Core 10 个 C# 文件全部 PASS。
- `git diff --check`：退出码 `0`。证据：`audit/2026-09-01/static-gates-dev16d-r13-r6.log`。
- 单 DLL/ABI 与 Headless 约束仍由既有 Plugin/Release 测试覆盖；本轮未新增运行时依赖、未修改 U3-SDK/BepInEx/原生库存权威。

## 五、Standards + Spec 双轴审查

冻结审查材料：`audit/2026-09-01/DEV16D-R13-r6-review-freeze.diff`，SHA-256：`5F453649A3B971FCC85F0868B1A04B3E78CF24AC10F6DA370B1CAF6ABE8AA51C`。

| 轮次 | Standards | Spec | 结果 |
| :--- | :--- | :--- | :--- |
| R13-1 | FAIL | FAIL | 双页面路由、来源 footprint、Pass-Through、stale preview 四项阻断 |
| R13-2 | CLEAN | 待复审 | 路由/来源页门禁/旧预览清理修复 |
| R13-3 | CLEAN | FAIL | 可变旋转、页面级重建、stale preview 和回退顺序阻断 |
| R13-4 | CLEAN | FAIL | 页面重建测试仍未进入生产 dispatch seam |
| R13-5 | CLEAN | 待复审 | 暴露 page dispatch seam，补跨页生命周期红测 |
| R13-6 | CLEAN | CLEAN | native delegate 生命周期 seam 与回归闭环完成 |

R13-6 Spec 确认生产路径为 `DetachGrid(page) → DiscardInventorySurface(page)`，并验证 exact original delegate 恢复、存活页 wrapper 隔离、可逆重绑定、重复 detach 幂等、拖拽来源清理与后续 Pass-Through。Standards 无阻断、无非阻断建议。审查期间工作区冻结且无源码漂移；两个审查子智能体均已关闭。

## 六、正式候选产物与身份

- 源 DLL：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- 归档 DLL：`audit/2026-09-01/artifacts/BetterUnturnedExperience-1925.dll`
- 大小：`230912` bytes
- SHA-256：`45D509A84FF5DF46955B65E4E0FAF7974B2771F0780258A4C5CD560B393AC72E`
- CandidateBuild：`DEV-16D-R13-R6-CLEAN-20260901`
- CaseId：`DEV-16D-R13-R6-20260901`
- SourceSnapshotId：`59d08b86a2d92b732777dc5702879de89eca9503`
- BuildIdentity：`DA405697247544B0549451A3D2A1888089531F6F1602F3A895ED35CBC31AFAC3`
- DefinitionSetDigest：`A6351887957F7D463521BD4355899DDBE62409E9B2DE15A2FCF52207C8B492F1`
- ArtifactPayloadDigest：`45D509A84FF5DF46955B65E4E0FAF7974B2771F0780258A4C5CD560B393AC72E`
- ToolchainIdentity：`MSBuild-18.9.1+a81b43525|.NETFramework-4.7.2|CSharp-10`
- ClientReferenceSetId / U3dsReferenceSetId：`Libs-ReferenceSet-CA9AFA1D47CFE1DCA74431CCA755366F8BA51F0401BB79DD7058AE1DED3C6A75`
- LoadSetIdentity digest：`8F48B57CFD9E4D10E2DA9D1562D81D889F89562FDBF59D973188F082E2903CCA`（玩家侧无独立 ClientUi satellite，ClientUi/Core/Contracts 按单 DLL 聚合）。

以上身份只绑定本轮双轴 CLEAN 的新代码候选，不继承 R12/R13-5 旧 DLL、hash 或运行证据。静态 CLEAN 不等于真实玩法资格；DEV-16E 仍需使用本候选另行采集单人、SteamP2PFriends Host/Client 与 U3DS Headless 证据。

## 七、部署与实机复测

现在可以开始实机复测。

1. 完全退出 Unturned。
2. 将 [BetterUnturnedExperience-1925.dll](./artifacts/BetterUnturnedExperience-1925.dll) 复制到游戏目录并重命名为 `BepInEx/plugins/BetterUnturnedExperience.dll`，替换旧 DLL。
3. 玩家侧只部署这一个 DLL；不要把 `BetterUnturnedExperience.ClientUi.dll`、`BetterUnturnedExperience.Contracts.dll`、`BetterUnturnedExperience.Core.dll` 或旧 BUE DLL 一并放入 plugins。
4. 用以下命令核对部署文件：

   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "<Unturned>\BepInEx\plugins\BetterUnturnedExperience.dll"
   ```

   期望值：`45D509A84FF5DF46955B65E4E0FAF7974B2771F0780258A4C5CD560B393AC72E`。
5. 在 BUE 管理面板启用“更好的物品交互/增强预览”和自动旋转，依次测试 Backpack、普通 Storage、车辆 Trunk；同时确认关闭开关后回到原生拖拽。
6. 记录 CandidateBuild `DEV-16D-R13-R6-CLEAN-20260901`、CaseId `DEV-16D-R13-R6-20260901`，导出新的 UMM 诊断包，并保留 `BepInEx/LogOutput.log`。旧 R12/R13-5 运行日志不得复用。

## 八、最终结论

DEV-16D-R13 已完成红测→绿测→Release→全套测试/门禁→Standards/Spec 双轴 CLEAN 闭环；工单已标记 `resolved`。正式 DLL 已归档，**现在可以开始实机复测**。
