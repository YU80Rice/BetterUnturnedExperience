# DEV-V2-06：配置迁移（空迁移）与网络模块设置

Type: task
Status: resolved（2026-09-04 交付，双轴审查 R1 CLEAN，提交；真机面板/Harmony 安装/三环境验证按拍板归 DEV-V2-07）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-04-lmn-takeover
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「配置迁移」+「成熟度定级」）

## Scope

实现网络模块 Settings Facet + 配置迁移（空迁移）记录 + **真实接管接线（DEV-V2-04 移交）**：

- 空迁移：LMN V5 无配置系统（T6 查证），迁移映射为空表。记录一次"空迁移"结果：结构化日志（`BueRuntimeLog` diagnosticId 惯例）+ 面板一行状态「无独立配置可迁移（LMN 无配置文件）」。不持久化"已做过空迁移"标记（幂等空跑）。
- 网络模块 Settings Facet：网络模块作为官方功能（核心、默认启用、玩家可关）注册一个 Settings Facet，仅含"启用/禁用"开关；不承载任何 LMN 配置迁移条目。`SettingMigrationFailed=1309` 已预留，本票不触发（空迁移无失败路径）。
- **接管接线（DEV-V2-04 移交，用户拍板）**：把 `LmnFrameClassifier` + `LmnTakeoverCoordinator` 决策核接真实传输——网络模块 init 时 `Refresh()`（探针 = `Chainloader.PluginInfos.ContainsKey(LMN_GUID)`）+ 对 `NetMessages.ReceiveMessageFromClient/Server` 注册 `Priority.First` Prefix 短路（命中 MOD/LMN2 帧 `return false`）+ 面板「已由 BUE 接管」状态条目与「让我改回独立 LMN」可逆钮（`BueNativeManagementPanel` seam）。
- **可靠透传 + 按帧来源解析分发（DEV-V2-03/04 移交）**：真实传输接线时映射 `ENetReliability` 补帧可靠位；帧携带发送者身份后按来源解析到正确会话（不再"首会话"）。

## 验收条件

- [x] 红测先行：`--bue-config-migration-red` 断言空迁移记录（日志行 + 面板状态）——先红后绿。（red-config-migration-r2 真红 → 锚点 exit=0）
- [x] 网络模块开关通过 `SettingsRuntime` + `FileSettingsPersistence` 原子提交持久化。（红测第 1 段双 facet 往返）
- [x] 面板显示「无独立配置可迁移」行（与「已由 BUE 接管」卡联动）。（RenderNetworkTakeoverDetails 代码级 + 状态行 headless 断言；真机渲染待补归 07）
- [x] 接管接线（DEV-V2-04 移交）：`Priority.First` Prefix 在 LMN 已加载时短路 MOD/LMN2 帧、LMN 缺席零误报 + 面板「已由 BUE 接管」+「让我改回独立 LMN」可逆钮（`BueNativeManagementPanel` seam）。（决策核九段断言 + 生产安装路径；真机观测待补归 07）
- [x] 可靠位 + 按来源解析分发（DEV-V2-03/04 移交）在真实传输接线下单机环回验证。（红测第 8 段三 runtime hub 拓扑）
- [x] 构建 0/0；七项目测试 PASS。（gates-summary-r1）

## 不做

- 不迁移任何实际配置键（LMN 无配置）；不预留未来迁移适配器接口（YAGNI，T6 Q4）。
