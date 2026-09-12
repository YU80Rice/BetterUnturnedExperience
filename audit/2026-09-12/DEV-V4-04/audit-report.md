# DEV-V4-04 审计报告 — 官方 legacy enabled 迁移 adapter

日期：2026-09-12　票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-04-legacy-enabled-migration.md`
状态：**已闭环（2026-09-12）：全套 0 错 0 警 7/7 绿；双轴四轮全 CLEAN（Standards 轮1–4、Spec 轮4）；本票按纪律不授候选/RELEASES/CaseId。**

## 1. 交付面（与 spec 逐条对齐）

- **意图事实=持久权威**：Core `FeatureLifecycleIntentStore`（宿主文档 `io.github.yu80rice.bue`，每功能一条 Toggle 记录）。`SetFeatureEnabled` 停用成功（Running→停 / 已停 noop / Isolated 保持 noop）→ 写记录；启用成功 → 清记录。未组合库的宿主测试保持 03 语义无痕。
- **迁移引擎**（Core `LegacyEnabledMigration` + Plugin `BueLegacyEnabledMigrationAdapter`）：显式六别名表（LIT/LIR/LHT/Network/v1compat 旧 `*.enabled` + BII `Enabled`，全部读各自 feature 的旧文档，含 BII——不做字段名扫描）；意图在库=新权威（不读旧值，重交停用目标=本启动内 honor）；旧值 false→交 03 机解释（Running→停 / Isolated→空操作成功保持隔离）；**机成功且意图确认在库才退役旧键**；true/不存在→静默不动生命周期。
- **退役**：四个注册类（LIT/LIR/LHT/Network+V1Compat）不再实现 IFeatureSettingsRegistration（facet 摘除→目录无 descriptor→注册表无 runtime→面板无行）；BII 手工快照/批编辑/拖拽复活腿退役（AutoRotate 保留为普通设置）；NoOp `noop.probe-toggle` 不登记。
- **adapter 开关源**：NetworkModuleAdapter 的 network/v1compat 开关从 facet runtime 改为意图库（旧 off 用户升级行为保真；机缝/迁移双写路径后 RefreshSwitches 重读）。
- **面板「让我改回独立 LMN」**：退役 `network.enabled` 编辑缝，改 TryToggleFeature（Q21 独立行动命令）+RefreshSwitches。
- **升级时序**：完成链 StartCatalog 后 `RunProduction()`（全环境；故障自隔离不打穿链）。

## 2. 红测先行（证据 red-run.txt）

运行时红×2（ClientUi BII 退役 4 断言；Plugin facet 退役 2 断言）+ 编译红（新 Core 类型 CS0246/CS0103）。判据落点：
- Settings.Tests `RunV4LegacyIntentAndMigration`：意图库幂等/跨实例/留痕；引擎 false→解释、true/absent 静默、解释器失败不退役、意图缺席不退役、新权威以新为准、未登记不迁。
- Plugin.Tests：`legacy facet 退役`（descriptor/runtime 双零）、`legacy 迁移六项`（LIT/LIR/network/BII false 全链真实 e2e：状态投影 Stopped/UserDisabled 或 Isolated 空操作、意图落盘、旧键退役）、`幂等与未声明`（LHT/v1compat false 收口、StateRevision 不动+模块实例不变=不重复代际、生态 `myservice.enabled` 原样、别名表恰六项）、`失败保留旧值（真实文档路径）`（意图写失败→旧键在+无半份权威→自愈）、`退役失败保留旧值（真实文档路径）`（退役写失败→旧键在→honor 再清）。
- ClientUi.Tests `DevV4BiiRetireTests`（快照单行/编辑拒绝/旧快照兼容读不消费 Enabled/停用不复活）。

## 3. 静态闸门（static-gates.txt）

模型文件 UI/native token=0；本票新增 diff 行原生 token=0；生产面板/组合/注册类退役键残留=0（BueNativeManagementPanel 1 处命中=退役注释本身）；git diff --check=0；整解决方案 Rebuild 0 error/0 warning；全套 7 工程 0 警 0 错 7/7 PASS。

## 4. 双轴审查链（fresh instances，每轮两新实例）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| 1 | CLEAN（deferrable：退役工厂留测试用/机钩吞返回值/方法名/BII 读源张力/TryRetire 吞异常） | NOT-CLEAN：①BII 读源接恒真内存态非旧磁盘事实 ②缺 BII 真实 e2e | BII 改 DocAlias 同构（读自身文档 Enabled 键）+e2e 补齐；组合层读源撤销 |
| 2 | CLEAN | NOT-CLEAN：③六项 false e2e 缺 LHT/v1compat ④「成功前旧值仍在」未落真实文档路径 | 幂等组补两笔 false；新增真实持久故障组（EnsureCreatedWith 注入 FailNextReplace=生产拓扑一库两消费者） |
| 3 | CLEAN | NOT-CLEAN：⑤退役写路径失败未在真实文档路径覆盖 | adapter 别名持久注入缝 + 新增「退役失败保留旧值」组 |
| 4 | **CLEAN**（provider 恢复后补审：禁空 catch/留痕/fail-closed/测试卫生/可选参数非投机泛化/双载单一事实源，均核过） | **CLEAN**（逐条验收核对通过） | 双轴闭环 |

## 5. 过程阻断记录（已解除）

第四轮 Standards 轴复审曾三次派发失败（Provider authentication failed ×2 → Insufficient account balance ×1），当时按硬规则未提交未授身份；provider 恢复后用户指示重派，第四轮 Standards CLEAN（见 §4），双轴闭环后提交关单。

## 6. 具名判断与遗留观察

1. DEV-V3-06「真实 LIT 经注入 view」全链组随 facet 退役删除（同缝由 NoOp 支线组+Settings.Tests 覆盖；DEV-V4-06 以 LIT mode/direction 重锚）。
2. LIR/LHT `WiredModule` 加 set（LIT 先例）；LIT/LIR/LHT 模块内部 Enabled 机械保留（无 view 恒真=生命周期全权）。
3. **移交 DEV-V4-05**：network/v1compat 良性隔离冻结（Module.Start 返回不变）→ 机内 `SetFeatureEnabled(true)` 对 Isolated 失败 → 停用意图暂无机内清除路径；05 需为良性隔离功能落启用面（或机外补清缝）。adapter 的再臂=记录清除后 RefreshSwitches（按钮/迁移已接，05 需接其保存路径）。
4. DocAlias 退役忽略 TryCommit 的 false 返回（尽力而为语义，故障由「旧键残留→下次自愈」兜底；Standards 轮 1 列为 deferrable）——「退役失败保留旧值」组证明该路径真实安全。
5. 退役工厂方法（CreateNetworkDescriptors 等）保留=旧文档 schema 形状锚（测试引用）。
6. 升级期一次性 start→stop 瞬态（机无「未启即停」转换，03 冻结转换表内解释停用的唯一路径；同调用栈内完成，无帧经过）。

## 7. 变更清单

见 changed-files.txt / changed-files-stat.txt。核心新文件：
`src/BetterUnturnedExperience.Core/Registration/FeatureLifecycleIntentStore.cs`、`LegacyEnabledMigration.cs`、
`src/BetterUnturnedExperience.Plugin/BueFeatureIntentRuntime.cs`、`BueLegacyEnabledMigrationAdapter.cs`。
