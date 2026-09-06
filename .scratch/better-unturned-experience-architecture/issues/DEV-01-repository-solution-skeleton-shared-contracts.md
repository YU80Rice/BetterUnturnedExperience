# DEV-01：Repository / Solution Skeleton + Shared Contracts

**Owner:** GPT  
**Required reviewer:** Gemini  
**Status:** resolved  
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**Depends on:** RT-06 (`resolved`)

## Scope

建立《更好的未转变者体验》的首个生产 solution、Contracts/Core/Plugin 项目边界、统一 DLL 聚合构建骨架和 UI Type Token 静态门禁。只落地已冻结的共享类型与接口，不实现设置运行时、Definition Linker、库存算法、Glazier UI、网络 codec 或 LMN adapter。

## Acceptance

- [ ] solution 在 .NET Framework 4.7.2 / C# 10 下可构建，0 errors，目标 0 warnings。
- [ ] Contracts、Core、Plugin 项目依赖方向为 `Contracts <- Core <- Plugin`；Contracts 不引用 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native 类型。
- [ ] 共享接口与 DEV-01 所需 DTO/enum 由 `BUE-V1-RT01-20260824` 精确来源实现，未擅自改名或改值。
- [ ] 统一 DLL 构建骨架可生成单一候选插件程序集；本票不宣称运行通过或发布授权。
- [ ] `eng/Verify-NoUiTokens.ps1` 对 Core/Contracts 源码执行严格 token 扫描，并在 CI/本地构建入口可调用。
- [ ] TDD 至少包含：公共接口可消费、契约 enum 值稳定、Contracts 无 UI/native token、依赖方向可验证。
- [ ] 不修改 LMN，不提前实现 DEV-02～DEV-07。
- [ ] 独立子智能体审计 PASS 后才能标记 resolved。

## Verification evidence

- RED/GREEN 编译日志
- solution/build 输出与 DLL SHA-256
- UI Type Token 扫描输出
- 测试输出
- GPT 前缀实施报告与 Gemini 复核 handoff

## Non-goals

运行时生命周期、设置持久化、物品候选算法、原生库存提交、网络握手/Ready-frame fence、Glazier UI、U3DS 运行验收和发布。

## Execution record

- TDD RED：缺少 `ContractTypes.cs` 时 MSBuild `CS2001`。
- TDD GREEN：4 项目 `0 errors / 0 warnings`；合约测试 PASS；Contracts/Core UI/native token scan PASS。
- 报告：`../../audit/2026-08-24/Implementation-DEV-01-2200.md`。
- Gemini handoff：`../handoffs/to-DEV-01-review.md`。
- Gemini 消费复核任务：`../handoffs/to-DEV-01-review.md`。
- 契约对照修正：`FrameworkErrorCode`、设置、能力协商、Core 状态和 DragInteraction DTO 已按 RT-01 补齐；已重建，当前产物哈希以新报告为准。
- 独立审计 Round 1 `FAIL`：缺失 `HandshakeReject`、`SnapshotKind`、`SnapshotChunkEnvelope`；已补齐，新增对账测试并重建，等待 Round 2。
- 独立审计 Round 2：代码/契约通过，发现报告中测试 EXE 哈希过期；已修正证据，等待最终轻量复核。
- 独立审计最终 `PASS`：`../../audit/2026-08-24/DEV-01-Independent-Audit-Final.md`。GPT 端静态构建与契约门禁已通过；等待 Gemini 对 `../handoffs/to-DEV-01-review.md` 的消费复核后关闭。
- Gemini 复核 `ACCEPT`：`../handoffs/DEV-01-Shared-Contracts-Review.md`。DEV-01 正式关闭；该 PASS 仅覆盖静态构建与契约门禁。


