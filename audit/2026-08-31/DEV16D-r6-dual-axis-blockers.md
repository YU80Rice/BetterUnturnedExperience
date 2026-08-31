# DEV-16D R6 双轴审查阻断记录

审查基准：`03188edc81f931400f3df392c879e09574adf907`
当前冻结提交：`c962fd83cce71cf6f969ba44b9bbdfc688f13fac`
审查结论：Standards FAIL；Spec FAIL。

## 共同阻断

1. `ClassifyNativeHierarchy` 将已存在 owner 的缺失子节点判为 `NotCreated`，而 `Poll` 仅对 `Incompatible` 调用隔离。活动库存会保持 Running/Available 并持续重试，未稳定投影 `Isolated/PresentationDegraded`。
2. `InventorySurfaceLifecycleAdapter.IsolateAndDispatch` 通过 `Action` 调用组件隔离，无法消费组件清理返回值；`CleanupIncomplete` 可能在真实 poll 失败路径丢失。

## 修复边界

- 只修改 DEV-16D native hierarchy 状态分类/隔离接线与 cleanup bool 传播及对应红绿测试。
- 保持单 DLL、Headless 隔离、原生回退、既有 ABI；不修改 U3-SDK、原生源码或 LMN。
- 修复后重新执行红测→绿测→Release/全套测试/静态门禁→全新 Standards→全新 Spec。
