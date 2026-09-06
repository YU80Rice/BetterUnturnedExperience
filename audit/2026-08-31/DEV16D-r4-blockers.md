# DEV-16D R4 双轴审查阻断记录

日期：2026-08-31
审查基准：`578db84...42cdc9c`
状态：修复中（不得交付）

## Standards / Spec 阻断

1. `AttachGrid` 在 `DetachGrid()` 异常并触发隔离后没有重新检查隔离态，仍可能写入新的 `onPlacedItem` delegate。
2. 同一容器代际的 `SleekItems`/父链重建未被识别；`BuildSurfaceContext` 返回 `null` 时旧 surface、delegate 和预览没有对称清理。
3. Hook/完整 native hierarchy 兼容失败只记录 GateDiagnostics，功能状态仍可能被面板投影为 `Running/Available`。
4. `InvokePollGuarded` 的清理返回值未形成单一稳定的 FeatureId/ErrorCode/DiagnosticId 传播链。

## 修复边界

- 仅修改 DEV-16D 原生 surface 身份追踪、隔离后 attach 门禁、Hook/层级失败状态投影和相应回归测试。
- 保留单 DLL、Headless 隔离、原生回退及现有 ABI；不修改 U3-SDK/原生游戏源码。
- 修复后重新执行红测→绿测→全量构建/测试/门禁→双轴审查，直到 CLEAN。
