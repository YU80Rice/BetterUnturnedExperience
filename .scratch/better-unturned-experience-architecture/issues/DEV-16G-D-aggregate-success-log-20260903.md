# DEV-16G 工单 D：日志"一句成功 + 错误才播报"（聚合成功行）

Type: task
Status: closed（2026-09-03 实机验收通过 + 用户授权关闭）
Parent: DEV-16G 日志规范化
Blocked by: 无（工单 A `114977d` / B `3bc651d` / C `2e60dfe` 已提交）

## 背景（用户 2026-09-03 实机反馈，`UMM-诊断包_20260903_115902`）

> 打开箱子、车辆后备箱，日志内还是有刷新。已实现可用的功能，不需要再插入过多探针检验界面是否打开、什么时候打开。只需要在插件加载成功后打印一句"xxx加载成功，界面已注入"，只有某些节点出问题时再打印；后续游戏运行中，成功不播报、错误才播报。

诊断包事实：116 行 BUE 日志中 ~85 行是 `BUE-INVENTORY surface-context-dispatched`（每次开容器 → generation+1 → 页 2/3/4/6 各刷一条）；另有 surface-ready/not-ready/discarded 转换行；**无 Error/Warning**（功能正常）。

## 最终决策（grill Q1–Q13 全拍板）

| # | 决策 |
|---|---|
| D1 | 聚合成一条成功行，替代分散的 38 条子系统 Info |
| D2 | `surface-context-dispatched` → Debug |
| D3 | `surface-ready` / `surface-discarded` → Debug |
| D4 | `surface-not-ready`：仅 `native-hierarchy-incomplete` 等真异常 → Error；`empty-grid`/`scroll-viewport-not-laid-out` 等良性 → Debug |
| D5 | Harmony 注入点全静默：`constructor-postfix`/`surface-opened`/`host-ui-tick`/`first-tick`/`create-button-*`/`add-child-success` → Debug |
| D6 | 原 38 条 LOAD_ONE_SHOT 全降 Debug（wiring enabled/composition ready/hooks-installed/patch-installed/constructed/initialize-complete 等） |
| D7 | 聚合行落点：`RuntimeReady` 后打 `BueRuntimeLog.Load`（Info）；Headless 分支打"加载成功（无界面）"变体 |
| D8 | 聚合行文案：**"Better Unturned Experience 加载成功，界面已注入"** |
| D9 | 错误行：保留英文结构化 + **统一中文前缀"BUE 错误："** |
| D10 | 错误行格式：结构化字段保留 + 人话前缀 |
| D11 | 新红测 `--logging-aggregate-red`：聚合行只打一条、含"加载成功"、子系统 Info 已消失（全 Debug） |
| D12 | 既有四测契约不变 |
| D13 | BepInEx `[Logging] Levels` 默认过滤 Debug；`Levels=Debug` 恢复全量 |

## 红测锚点

- `--logging-aggregate-red`（新增）：断言聚合成功行在 RuntimeReady 后只打一条、含"加载成功，界面已注入"；且子系统加载事件（hooks-installed/surface-context-dispatched 等）为 Debug 级。
- 既有：`--logging-gate-red` / `--logging-failure-red` / `--logging-verbose-red` / `--logging-bue-runtime-red` / `--logging-timeout-red` 契约不变。

## 验收

- [x] 红测先红后绿（含新 `--logging-aggregate-red`）
- [x] Release 构建 0/0；七项目全 PASS；UI token 零命中；`git diff --check` 通过
- [x] 双轴独立审查 CLEAN
- [x] 实机验收通过（用户 2026-09-03 人工确认，`UMM-诊断包_20260903_130139`）：部署 DLL 哈希 `D13F9A12...` 逐字匹配工单 D 产物；BUE 正常加载；无 Error/Warning；正常退出（exit 0）。日志包仅捕获启动阶段（聚合行以人工验收为准，同 DEV-16F 客机日志先例）。

## 2026-09-03 关闭记录

- [x] 提交 `6c7066a`（实现）+ `0ee5d67`（交付报告）
- [x] 实机验收通过，用户授权冻结并关闭 DEV-16G 工单
