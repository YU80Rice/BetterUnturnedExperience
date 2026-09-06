# DEV-V2-15 R2'（零上下文 fresh 验证轮）——Spec 轴报告

> 归档说明：本报告转录自 2026-09-06 implement 会话内全新 spawn 的 Spec-Reviewer 实例输出（Fresh-instance 规则下的第一轮 fresh 验证；派发提示词不含 R1 结论）。初始判 NOT CLEAN（2 项）→ 实现方同轮补证（编译列表级断言修复 + TidyCompleted 工单分解边界 rebuttal）→ 该实例复判 CLEAN。两轮输出均逐字转录，仅加本说明头。

---

## 初始判词（NOT CLEAN）

NOT CLEAN

### GAP

- **阻塞：验收要求的 `InventorySolver 纯算法直测` 未覆盖。** 工单明确要求"`InventorySolver 纯算法直测`"（issues/DEV-V2-15-lit-singleplayer-path.md:24），规格也要求"`InventorySolver 直测`"（spec.md:231）。测试仅调用 `defaultStrategy.BuildPlan(input)`（tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:3127-3150），未直接调用 `InventorySolver.TryPack`；实际 solver 调用只存在于 adapter（src/BetterUnturnedExperience.Lit/DefaultGridV1Strategy.cs:19-25）。策略层测试不能替代工单字面要求的 solver 直测。

### DEVIATION

- **阻塞：模块在宿主注册结果确定前已启动并安装补丁，偏离"经宿主注册面进入"的生命周期边界。** 规格要求各功能模块"经宿主注册面进入"（spec.md:139），工单同样要求 `IFeatureRegistration` + `IFeatureModule` 经宿主注册面进入（issues/DEV-V2-15-lit-singleplayer-path.md:15）。但 `Register()` 在调用 `BueRuntimeHost.Register(...)` 前先执行 `module.EnsureStarted()`（src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs:24-31）；`EnsureStarted()` 会直接安装 Harmony 补丁（src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs:102-110）。因此注册被拒绝时功能仍可能已启动、补丁仍可能存活，且不受宿主注册结果控制。

（处置：GAP=新增编译列表级+产物级双层断言中的 solver 直测节（Program.cs:3174-3234 直接 TryPack）；DEVIATION=Register 改 Born-inert（先注册，Accepted 才 EnsureStarted+设 WiredModule）。同时补证反驳了 R2' 会话中出现的另一候选发现（TidyCompleted 未发布，判非本票 GAP）——该反驳证据与下方复判一并留链。）

## 同轮补证后复判（CLEAN）

CLEAN — 发现 2 已通过编译列表级与产物级双层断言修复；发现 1 按 DEV-V2-15 的明确规格引用集、DEV-V2-19/21/22 的依赖与移交边界不构成本票 GAP/DEVIATION。

依据：
- **整理策略与直测**：工单要求 `ITidyStrategy`、`default-grid-v1` adapter 及 `InventorySolver 纯算法直测`（issues/DEV-V2-15-lit-singleplayer-path.md:16,24）。测试现已在 tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:3174-3234 直接调用 `InventorySolver.TryPack`，覆盖确定性、边界/无重叠、超尺寸、FFD、空页；策略层仍由 src/BetterUnturnedExperience.Lit/DefaultGridV1Strategy.cs:19-25 单独包裹 solver。
- **宿主注册顺序**：规格要求通过宿主注册面进入（spec.md:139）。当前 src/BetterUnturnedExperience.Plugin/InventoryTidyFeatureRegistration.cs:24-42 先注册、仅在 `Accepted` 后设置 `WiredModule` 并 `EnsureStarted()`；拒绝路径不装 Harmony、不启动模块。惰性 `ModuleFactory.Create()` 也位于宿主已接受后的启动路径（同文件:106-115）。
- **其余 Scope**：生产聚合仍明确包含 LIT 源码并排除旧插件/夹具（src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj:60-76）；测试继续覆盖 enabled=false 原生回退、设置身份、生命周期、面板身份及夹具排除（tests/.../Program.cs:3236-3330）。联机熔断统计、`TidyCompleted` 发布属于 DEV-V2-21/DEV-V2-19 范围，未计入本票欠账。
