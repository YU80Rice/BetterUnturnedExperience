# DEV-V2-15 R3（Fresh-instance 验证轮，主会话裁定传达后）——Standards 轴报告

> 归档说明：2026-09-06 主会话裁定 R2（继承轮）作废后派发的全新 spawn standards-reviewer 实例输出（R2' 为实现会话内自行补跑的第一轮 fresh 验证，R3 为用户指定追加复核轮；两轮互为独立 fresh 实例）。逐字归档，仅加本说明头。

---

本实例为全新上下文，结论独立推导。

R1-BLOCKING① 已真实落地且语义正确。`MainThreadDispatcher.EnsureOpen`（`src/BetterUnturnedExperience.Lit/Tidy/MainThreadDispatcher.cs:262-268`）在代际边界把进程静态 `_shuttingDown` 复位；`InventoryTidyModule.EnsureStarted`（`...\InventoryTidyModule.cs:105-117`）调用它，`Stop`（77-96）三阶段：静默 → `Shutdown` drain → `UnpatchSelf` + `ResetStateForShutdown` + 解绑 `LogSink/ErrorLogSink`。方向/模式点击在写页字典前有 `ActiveModule==null` 守卫（`...\Ui\InventoryTidyUiPatch.cs:533-561`）。生产代际=新模块实例；同实例 Stop→Start 的 `ShuttingDown` 粘滞与 Network `isolated` 同构，不构成跨功能泄漏。

R1-BLOCKING② 满足 CONTEXT「实现来源署名」。`docs\third-party\LaunchInventoryTidy-attribution.md` 含作者 YU80Rice、Archive 快照、候选 SHA、MIT/Copyright (c) 2026 YU80Rice、致谢。7 个原样迁移文件带 MIT 版权头。旧 `LICENSE` permission notice 全文未写入副本，与已具名延期一致，不升级阻断。

R1 SMELL 抽查属实：Lit 面类型均为 `internal`；`InstallPatches` 失败 `UnpatchSelf`+清 `ActiveModule`（169-190）；`LogWarning` 走 `ErrorLogSink` 前缀 `"WARN "`（`LitRuntime.cs:43-50`）；`ArmNewModule` 去重 Register/Factory。

T4：`ITidyStrategy`+`default-grid-v1` 包 `TryPack`；Solver/LayoutCandidate 相对 Archive 仅命名空间/可见性/版权头，零算法 diff；`ManualTidyService` 策略参数化+去 harness，默认 adapter 行为等价。`enabled` 唯一 ClientLocal；关→撤补丁+`NativeFallback`，开→`FaultGate.Reset`。Harmony ID=`FeatureId`。csproj 显式 13 文件、零夹具/零 `TIDY_TEST_HARNESS`；测试编译列表+产物反射双层（`Program.cs:3333-3388`）。

[DEFERRABLE] `HandleTidyClick` 在 `ActiveModule` 守卫前 `EnsurePageDefault`（`InventoryTidyUiPatch.cs:591-609`）：残留按钮只写入缺省，不回写玩家态；并入已具名延期 1。
[INFO] `LocalTidyExecutor.cs` 移植 ACK 恢复链无文件头，项目级 attribution 已覆盖。
[INFO] `ArmNewModule`（`InventoryTidyFeatureRegistration.cs:45-51`）实际只构造+绑日志，装配在 `EnsureStarted`。

无新阻断 smell（神秘命名/重复/依恋/数据泥团均不构成硬违规）。

VERDICT: CLEAN
