# DEV-16D-R13 审查阻断记录 R3 / R13-6

## 基准与来源

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-16D-R13-native-conformance-remediation.md`
- R13-5 冻结提交：`6e90157ce6712c30ce1018e3a09ff437624971ce`
- Standards 轴：`CLEAN`
- Spec 轴：`FAIL`
- 审查子智能体：`spec_r13_5_review2`（已在结论返回后关闭）
- 责任变更：相关前端实现原由 Gemini 负责，现由 GPT 接手。

## 阻断

R13-5 的页面生命周期回归只验证了 `Action<byte>` 页面回调记录和
`DiscardInventorySurface(page)`，没有执行生产
`InventoryDragPreviewAdapter.DetachGrid(page)`。测试使用的合成状态中
`SleekItems` native delegate 全为 `null`，因此无法证明：

1. 原生 `SleekItems.onPlacedItem` 的 exact original delegate 保存/恢复；
2. Backpack detach 不影响存活的 Storage/Trunk wrapper；
3. 页面重建后的 wrapper 可逆重绑；
4. 重复 detach 的幂等性；
5. `DragOriginContainer` 在该真实回调路径上被清零。

## 已执行的 R13-6 红测与最小修复

- 红测命令：
  `MSBuild.exe tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj /t:Build /p:Configuration=Release /v:minimal`
- 红测结果：编译失败，`CS1061` 缺少 `AttachNativeGrid` 与
  `DetachGridAndDiscardSurface`，退出码 `1`（`PAGE_NATIVE_DELEGATE_RED_EXIT=1`）。
- 最小修复：
  - `InventoryDragPreviewAdapter.AttachNativeGrid(SleekItems,page)` 作为生产/测试共用 native seam；
  - `AttachedGridBinding` 保存 wrapper delegate 实例，detach 仅在当前仍为该 wrapper 时恢复 exact original；
  - `DetachGridAndDiscardSurface(page)` 固化 `DetachGrid(page)` 先于
    `component.DiscardInventorySurface(page)`；
  - `BetterUnturnedExperiencePlugin` 的页面回调改用该有序操作，并保留
    adapter 缺失时的组件清理回退。

## 当前门禁状态

- native delegate 定向绿测：已通过，退出码 `0`。
- Release 全量验证：待执行。
- 新双轴审查：待执行。
- 新 DLL / SHA-256 / CandidateBuild / CaseId：尚未生成，禁止交付实机测试。
