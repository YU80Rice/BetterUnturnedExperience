# DEV-05：ClientUi / Glazier 装配与 Presenter seam

Status: resolved
Owner: GPT（后端/共享边界维护）
Consumer reviewer: Gemini（前端）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02

## 目标

建立 ClientUi 的可测试装配边界，使前端 Presenter 只消费冻结 Contracts/Core seam；使用显式注册表和运行时三条件门禁装配 UI 组件，并为物品拖动与设置快照提供代际安全的消费入口。

## 范围

- `ClientUiAvailable && !IsBatchMode && !Headless` 装配门禁。
- 构建期显式注册记录；禁止程序集扫描、类名猜测和 `Assembly.GetTypes()`。
- 单组件异常隔离与 UI 资源清理回调。
- `InventoryDragPresenter` 消费 `IPlacementCandidateEvaluator`，拒绝过时代际输入。
- `SettingsSnapshotPresenter` 只读消费 `FeatureSettingsSnapshot`，不持有设置事实源。
- ClientUi 项目不得反向修改 Contracts/Core，也不实现 DEV-06/DEV-07。

## 不在本票

- 具体 Glazier/Sleek Hook、Harmony patch 和 U3-SDK 运行验证。
- 原生库存提交、LMN 网络适配器、三环境发布验收。

## 验收条件

- [x] TDD Red→Green 测试覆盖：三条件 gate、显式注册、组件异常隔离、销毁清理、拖动 generation、设置快照只读消费。
- [x] Release solution 0 errors；目标 0 warnings。
- [x] Contracts/Core UI token scan PASS；ClientUi 不反向引用原生 adapter。
- [x] 不使用 `Assembly.GetTypes()`、全局 `PatchAll()` 或隐式入口扫描。
- [x] 生成 GPT 前缀实施报告、构建日志、测试结果、DLL 哈希和 Gemini handoff。
- [x] 独立子智能体审计 PASS；Gemini 架构与 TDD 终审复核全量 ACCEPT。

## 证据边界

本票静态代码、单元测试和装配 seam 通过不等于 Glazier 原生 Hook、Unturned 玩家体验或 SP/P2P/U3DS 运行通过；这些义务转入后续环境验证与 DEV-07。
