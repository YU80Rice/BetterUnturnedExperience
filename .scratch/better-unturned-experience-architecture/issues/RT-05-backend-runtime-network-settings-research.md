# RT-05：调研后端生命周期、LMN、设置与双端引用交集

**Owner:** GPT（后端与共享契约负责人）  
**Required reviewer:** Gemini（前端状态与设置消费复核）  
**Blocked by:** RT-01：冻结共享契约与对账基线  
**Status:** resolved

## What to build

形成公共框架在客户端与 U3DS 上启动、协商、保存设置、隔离故障和关闭的完整后端证据链，使统一 DLL 能在三种环境中按能力降级而不加载不兼容类型。

## Acceptance criteria

- [x] 追踪客户端和 U3DS 的 BepInEx 初始化、Harmony 应用、功能启动、关闭及异常路径。
- [x] 比较双方 `Assembly-CSharp`、BepInEx 和可达 API 身份，产出安全类型交集与泄漏风险表。
- [x] 根据固定 LMN V5 源码验证 named-channel 注册、可靠发送、sender context、payload limit、handler thread 和 cleanup。
- [x] 划定 LMN callback、game-thread queue 和 feature callback 的安全排队及 generation 边界。
- [x] 追踪 ClientPreference、ServerAuthority、ServerPolicyWithClientPreference 的存储根、原子写入、迁移、损坏恢复和会话覆盖。
- [x] 分离 Runtime Admission 静态事实、SettingsRuntime 动态事实、Lifecycle 状态写入和 ConnectionGeneration。
- [x] 验证 capability negotiation、SessionReady、插件缺失/版本不兼容降级和 SafeMode/单模块隔离路径。
- [x] 把原生错误归并为稳定 framework error family，原始实现细节只进入内部诊断。
- [x] 输出 GPT 前缀研究报告、线程/排队图、引用交集表、设置存储表、LMN 能力表、内部 adapter seam 和运行证据义务。
- [x] 不实现生产运行时，不修改共享契约；发现不足时提交 change request。

## Verification

- [x] 每个原生或 LMN 事实带固定源码身份和证据分类。
- [x] Gemini 确认 SessionReady、设置快照、状态投影和降级结果可由前端安全消费。

## Comments

- 2026-08-24：人工开发者授权 GPT 使用 `/research` 领取 RT-05；继续使用最新批准的共享契约与统一 SourceSetId，仅开展后端运行时、LMN、设置及双端引用交集研究，禁止生产编码。
- 2026-08-24：GPT 完成研究报告、两项 Shared Contract Change Request 与 Gemini 交接；独立只读审计 `PASS`。等待 Gemini、SourceSet successor 人工批准及 SCR 裁定，因此未标记 `resolved`。

## Answer

- 研究报告：`../RT-05-Backend-Runtime-Network-Settings-Research.md`
- Gemini 同步：`../handoffs/to-RT-05-review.md`
- 变更请求：`../change-requests/SCR-RT05-001-receive-time-connection-context.md`
- SourceSet 勘误：`../change-requests/SCR-RT05-002-sourceset-manifest-and-u3ds-references.md`
- 研究内容独立审计：`PASS`
- 2026-08-24：`BUE-SS-20260824-02` 已批准；`SCR-RT05-002` accepted；RT-02～RT-05 已统一迁移，Gemini 迁移复核 `PASS`。
- 2026-08-24：`SCR-RT05-001` A/B 两个抛弃式 spike 均在第 2 轮独立审计 `PASS`。GPT 裁定 A（BUE frame fence）为 V1 必需基线，B 仅保留为未来 LMN 加固；RT-05 关闭。


