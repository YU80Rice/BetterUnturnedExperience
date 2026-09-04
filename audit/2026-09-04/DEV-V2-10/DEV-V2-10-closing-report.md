# 结单报告 — DEV-V2-10：V1 镜像时机修复 + 面板网络条目缺失（real-machine-test-loop 修复轮）

> 票：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-10-v1-mirror-timing-and-panel-entries.md`（Status: resolved）
> 触发：DEV-V2-07 配置 B 四端实机复核（`../DEV-V2-07/configB-verification-r1.md`）F-A + F-B。
> 日期：2026-09-04。审查循环：R1 双 FINDINGS → R2 修复+重审 双 FINDINGS → R3 修复+重审 **双 CLEAN**。

## 1. 根因结论（与票面假设的差异）

**F-B（面板条目缺失）根因不是票面猜的「刷新时机早于注册」，而是注册被拒**：实机日志两场
`Player-prev.log.session1` L196 / `Player.log.session2` L196 均为
`BUE Network Module featureId=io.github.yu80rice.bue.network accepted=False reason=InvalidDefinitionArtifact diagnosticId=BUE-REG-004`。
注册件 `FeatureDefinitionArtifact.ArtifactPayloadDigest` 四个 ulong 是手抄伪值（≈SplitMix64 常数），与 payload
"BUE-NET-V1" 的真实 SHA-256（小端 4×uint64）不符 → `IsValidDefinition` 拒绝 → 目录无网络条目 → 完成后刷新
（`TryRefreshAfterCompletion` → `RefreshManagementPanel`）自然投影不出条目。刷新机制本身无病。
另：`ToManagementEntry` 的「BUE V1 兼容层」映射（L99）从未有注册件到达——v1compat 从未被注册，属死映射。

**F-A（镜像时机）**：`MirrorLegacyHandlersSafe` 在 LMN 类型未解析时主动抛 `ArgumentException` →
`result=failed` → 生产 sink（按 `result=failed` 子串路由 `ErrorFriendly`）每场一条「BUE 错误：」行，且镜像永久失效。
真实时序比票面所述更深一层：LMN 程序集加载后其表仍为空（旧插件在自己的 Awake 才向 LMN 注册 ch250），
「类型已解析但表为空」若被当作完成，晚注册频道同样进不来——R2 审查抓到并一并修复。

## 2. 修复清单（生产侧）

| # | 修复 | 位置 |
|---|---|---|
| F-B1 | payload 摘要改为**计算**而非手抄（`ComputePayloadDigest`：SHA-256 → 小端 4×uint64，与运行时 `ToDigest` 同约定） | `NetworkModuleFeatureRegistration.cs` |
| F-B2 | V1 兼容层立为官方注册件 `io.github.yu80rice.bue.network.v1compat`（规格 spec-V2-phase1 L74），`Register()` 经同一公共 host bridge 注册双 facet，被拒 facet 走 Runtime 通道告警不阻塞 | 同上 |
| F-A1 | LMN 未就绪（类型 null / 表空）→ `result=deferred`（每 episode 一次）+ `RetryPendingMirror()` 节流重试（300 tick ≈5s，静默）；表空不算完成 | `NetworkModuleAdapter.cs` |
| F-A2 | `result=failed` 语义收紧：仅 LMN 已解析但表形状异常（缺字段/非字典/handler 形状错）才 failed 且停止重试；deferred/mirrored 行不含 `result=failed` 子串，永入 Debug 通道 | 同上 |
| F-A3 | 插件 `Update()` 驱动 `WiredAdapter?.RetryPendingMirror()`（Client/Headless 通用） | `BetterUnturnedExperiencePlugin.cs` |
| H3（R3） | 模块关闭（network.enabled=false）时 `ActivateCore` 零镜像工作（`networkEnabled &&` 门），与 RefreshSwitches 零反射规则对齐 | `NetworkModuleAdapter.cs` |
| H4（R3） | 删除 `patchesDesired`（开局禁用时永不置位 → 重开永不重臂的缺陷）；活跃路径无条件走幂等 `ApplyNetworkPatches`（手册 B6 重臂恢复） | 同上 |
| H5（R3） | v1compat payload 字节拼写修正（67'C'→66'B'，"CUE-"→"BUE-"），并新增 payload 文本钉死断言防「摘要自洽但拼错」 | `NetworkModuleFeatureRegistration.cs` |

测试侧（`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`）新增四段 + 桩：`AssertBueV1MirrorTiming`
（`--bue-v1-mirror-timing-red`，真时序三阶段 + 生产路由零 ERROR 断言）、`AssertBueNetworkRegistrationDefinitions`
（`--bue-network-definitions-red`，生产定义过真运行时 + payload 文本钉死）、`AssertBueNetworkPanelEntries`
（`--bue-network-panel-red`，生产时序：注册→Initialize 前置刷新无条目→CompleteRuntime→完成后刷新两条目出现）、
`AssertBueNetworkKillSwitchLifecycle`（`--bue-network-killswitch-red`，关闭零镜像 + 重开重臂）。
`CreateOfficialRegistrations()` 为新抽取缝（生产 Register 与测试同源）。

## 3. 红测先行证据链（全部留档本目录）

| 观察轮 | 锚点 | 红证据 |
|---|---|---|
| 红0 | 三缝缺失 | `build-red0-compile-seams-missing.log`（CS0117/CS1061） |
| 红1 | F-A ERROR 行 + F-B facet 缺失 | `red1-flag-*.log` ×3（空重试桩 + 纯抽取） |
| 红2 | F-B 根因 | `red2-flag-bue-network-definitions-red.log`（**got InvalidDefinitionArtifact BUE-REG-004**，与实机逐字一致） |
| 红3 | R2 加固锚点 | `red3-flag-bue-v1-mirror-timing-red.log`（空表误判完成）+ `red3-flag-bue-network-panel-red.log`（完成刷新后条目缺失，条目级） |
| 红4 | R3 加固锚点 | `red4-flag-bue-network-definitions-red.log`（CUE 拼写）+ `red4-flag-bue-network-killswitch-red.log`（关闭仍镜像） |
| 红5 | R3 H4 | `red5-flag-bue-network-killswitch-red.log`（重开不重臂） |
| 绿 | 全部锚点 | `green4-flag-*.log` ×4 + `green4-full-mainflow.log`（PASS） |

## 4. 审查循环（output-review-loop.md）

- **R1**：双轴独立子代理。Standards [H]1（镜像重试提前终止+时序锚点不真）、[H]2（面板锚点不在条目级）；Spec [GAP-1..4]+[NOTE]。
- **R2**：修复 H1/H2/GAP 后重审（新冻结件 `dev-v2-10-review-freeze-r2.diff`）。Standards [H]3/H4/H5+[S]1；Spec [GAP-1/2]（H5 同根）。
- **R3**：修复 H3/H4/H5/S1 后重审（`dev-v2-10-review-freeze-r3.diff`）。**Standards：CLEAN**；**Spec：CLEAN**。
- R3 后同步性微修（零行为，纯注释）：两处随 H4 过时的注释更新、R2/R3 轮次标注更正——门禁复跑全绿后纳入本报告所附最终树。
- **具名延期（不阻塞）**：主菜单/游戏内两入口「分别打开后直接断言网络条目可见」的端到端断言——现有证据链：
  `AssertManagementPanelOpenHooks` 已证 MenuDashboardUI/PlayerPauseUI/MenuWorkshopUI 三入口 hook 同属管理面板 owner，
  `BueManagementPanelRuntime.Initialize` 同挂两入口，条目模型层已由 `AssertBueNetworkPanelEntries` 断言；
  入口级直接可达性由实机 P2/B4-B6/P4b 补采作为最终验证。

## 5. 门禁（最终树复跑）

- Release `-t:Rebuild`：exit=0，**0 警告 / 0 错误**（`build-gate-release-rebuild.log`）。
- 七运行器全 exit=0：Contracts / Settings / Placement / ClientUi / Network / Plugin / Release（`gate-runner-*.log`）。
- NoUiTokens：Core 18 文件 PASS、ClientUi 11 文件 PASS（Core/ClientUi 本轮零 diff）。
- `git diff --check` 干净。

## 6. kit 身份工具同步（DEV-V2-07 交付物的必要跟踪）

`kit/QualificationGateRunner/Program.cs` 的 `OfficialDefinitions` 表同步三条官方定义：BII（不变）、
network（P 值更正为真 SHA-256）、**v1compat（新增，"BUE-NET-V1C"）**。runner 已重建
（`build-kit-runner-r2-dev-v2-10.log`）。独立复算 DefinitionSetDigest =
`5E16717666CF098FA2247A7DB41275EBE9B25B610CB29BBB54F5F9C68BA22F01`（与 runner identity 模式输出交叉验证见 §7）。
此为身份工具跟踪官方定义集的必要动作，不属「不改候选身份流程」的禁止面（配方/流程/工具均未变，变的只有官方定义全集本身）。

## 7. 候选身份（提交后授予，补记于文末）

