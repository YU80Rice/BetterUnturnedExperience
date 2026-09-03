# 交付报告 — DEV-V2-05：LMN V1 数字频道兼容层

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-05-v1-compat-layer.md`
> 阶段：V2 第一阶段实施第 5 票（/implement + /tdd 红测 + 双轴独立审查）
> 性质：Host 内部兼容层（纯 C# Core；用户拍板边界 = **本票不做 06 接线**）
> 对照决策：spec T4「V1 兼容路径」`../spec-V2-phase1-lmn-adoption.md` L71-79；T8 判据 `../research/V2-T8-v1-ecosystem-inventory.md` L14-31（LMN 源码不在本仓库，T8 行号为权威转述）

## 1. 交付内容

票内拍板（2026-09-03 human 定稿）：验收入口不依赖真实旧 DLL（host 侧 no-op fixture）；05 先于 06；接线 seam（LmnTakeoverCoordinator 实例化/面板开关）归 06。

`src/BetterUnturnedExperience.Core/Network/` 三个纯 C# 类（经 Core.csproj + Plugin.csproj 嵌入列表双注册，单 DLL 装配）：

| 能力 | 实现 |
|---|---|
| V1 帧编解码 | `LmnV1FrameCodec.IsV1Frame/TryParse/TryBuild` — `["MOD" 3x][channel:1byte][payload]`（LMN `ModRouter.cs:13-16,40-51`；0..255 对齐 `ValidateLegacyChannel` `ModTransport.cs:695-704`）；线格式路径零抛异常（bool 报告失败）；payload 为拷贝（调用方不可变入站帧） |
| V1 兼容注册表 | `LmnV1CompatRegistry` — 模拟 V1 注册语义：server=`Action<ulong, BinaryReader>`、client=`Action<BinaryReader>`（LMN `ModTransport.cs:137,159,186,216`）；64-bit steam id 以 ulong 承载保 Core 纯 C#（引擎侧转换归 06）；重复注册后写覆盖（V1 表语义）；注销幂等；注册期 0..255 校验抛 ArgumentOutOfRange |
| 官方开关 | `LmnV1CompatLayer.Enabled`（默认启用；独立于网络模块开关；面板绑定归 06）；关闭 → 入站帧交还不消费 + 出站拒绝（vanilla 兜底） |
| 路由消费语义 | enabled + V1 帧恒消费（路由到 handler，或无 handler 丢弃+诊断）——不向 vanilla 泄漏 MOD 帧；LMN2/非 LMN 帧永不消费（V2 路由与 vanilla 零接触） |
| 故障隔离 | `DispatchSafely` 逐帧 catch → 诊断 `diagnosticId=BUE-V1COMPAT-001` → 层保持健康；V1 故障不扩散到 V2/本地功能（spec T4 L77） |
| 诊断 seam | `internal static Action<string> DiagnosticLogSink`（仓库既有 idiom，对照 `InventoryDragPreviewAdapter.cs:472-477`）；06 接线绑 BueRuntimeLog；测试捕获 `List<string>` |

交付边界：不触碰 host/bootstrap 类型；不提供新 V1 注册入口（registry 为 Host 内部兼容兜底，非面向新消费方的 V1 API 面，票「不做」条目）；**Contracts 零变更**（`git diff -- src/BetterUnturnedExperience.Contracts` 为空，仅 bin/obj untracked）。

## 2. TDD 循环

| 阶段 | 证据 / 结果 |
|---|---|
| 红 | `red-v1-compat-r1.log`：Plugin.Tests 重建 exit=1，`error CS0234`（`LmnV1CompatLayer` 等三类型不存在）——红先于实现 |
| 绿 | `green-v1-compat-r2.log` 重建 exit=0 → `green-v1-compat-anchor-r3.log`：`--bue-v1-compat-red` exit=0（带正证据 exit 行；Spec R1 F2：原 r2 捕获为 0 字节——运行器成功时静默）；`plugin-tests-full-r1.log`：no-arg 全套 exit=0（`DEV-14/DEV-16B plugin runtime tests: PASS`） |
| 流程发现（转绿间） | **Plugin.Tests 只经 Plugin 程序集看到 Core 类型**：三新文件须同时进 `Plugin.csproj` 的 `EmbeddedCore` 嵌入列表（04 先例 L18-19），仅加 Core.csproj 不够（red-v1-compat-r2-embed-list-missing.log 复现 CS0234；原误名 green-v1-compat-r1.log，Spec R1 F1 更名归位）→ r2 修复。非红测断言抓到的缺陷，是构建链路知识缺口，如实登记 |
| 断言覆盖 | 编解码双向字节精确（含 channel 0/255 边界、null payload、非法频道拒绝）；注册表注册/分发/注销幂等/后写覆盖/0..255 校验；开关默认启用、off→放行+出站拒绝、on→消费；LMN2/非 LMN 永不消费；handler 抛异常→帧丢弃+诊断 BUE-V1COMPAT-001+层保持健康；未知频道→丢弃+诊断；客户端面 RouteFromServer；fixture（`LmnV1NoOpPluginFixture`，频道 103）不改代码收发：出站=字面 MOD 帧、入站=sender+payload 原样 |

## 3. 验证矩阵（`gates-summary-r1.log`，21:22 复跑固化）

| 项 | 结果 |
|---|---|
| Release 全解决方案重建（`build-sln-release-r1.log`） | exit 0；日志零 warning/error 行（各项目 TreatWarningsAsErrors=true）→ **0/0** |
| `--bue-v1-compat-red` | exit 0（正证据：`green-v1-compat-anchor-r3.log`） |
| 七项目测试运行器 | Contracts/Settings/Placement/Network/Release/ClientUi/Plugin 全 **exit=0** |
| UI/native token 扫描 | Core **16 文件** PASS、ClientUi **11 文件** PASS（13+3 新增吻合）零命中 |
| `git diff --check` | 退出码 0（CRLF 提示为 autocrlf 信息性警告；工作副本 LF、仓库 blob LF，符合「提交前转 LF」惯例） |

## 4. 双轴独立审查（R1）

- **Standards 轴：R1 CLEAN**（独立上下文子代理）。8/8 硬约束逐条通过：禁词 8-token 大小写敏感 grep 全 `Network\` 目录零命中、Core.csproj 仅 System/System.Core/Contracts、无 BepInIncompatibility 红线、线格式路径零抛异常+逐帧隔离（`DispatchSafely` L87-103）、C#10/net47.2 兼容（显式访问器/sealed/readonly，无 init/record）、静态可变状态仅 `DiagnosticLogSink` seam（`enabled` 为实例字段；`Emit` 快照读+判空+护 try/catch）、注册期抛出边界正确、测试惯例（锚点+全套双注册、每断言带消息、LMN 行号引用风格）。**可延后 smell（具名登记，不阻断）**：(a) `LmnV1FrameCodec.IsLegacyChannel` 谓词 vs 邻文件内联风格（taste 级，与基线一致性成立）；(b) `DiagnosticLogSink` 非 volatile——`Emit` 快照读在当前单线程使用下安全，06 生产接线若跨线程写 sink 需 revisit（同 04 对 `active` 的移交注记）；(c) `Registry` 公开 getter 扩大表面——预期 compat seam，可接受。⚠️ 分工标注：该审查者上下文无 shell，`git diff` hunks 与 token 脚本未机械复跑——由主会话门禁运行补足（§3）。
- **Spec 轴：R1 CLEAN**（独立上下文子代理）。验收记分卡 **4/4 PASS**：红测先行（red-r1 CS0234 exit=1 → green r2 干净重建 + 全套 PASS）；编解码双向字节精确（MOD 3x / channel 0..255 / payload；LMN2/非 LMN/截断/null 全拒绝）；注册表 Host 内部 int 频道键、Contracts 零源码变更；`LmnV1NoOpPluginFixture` 证明旧插件不改代码兼容（int 频道注册 + MOD 线格式收发经兼容层；真机旧 DLL 验证按拍板为可选项）。附加核实：开关默认启用 / off 交还 + 出站拒绝、LMN2/非 LMN 永不消费、逐帧故障隔离 + 诊断、未知频道丢弃 + 诊断；04 地基两文件（LmnFrameClassifier/LmnTakeoverCoordinator）diff 为空未触碰。**Findings 处置**：F1（green-v1-compat-r1.log 误名失败构建）→ 已更名 `red-v1-compat-r2-embed-list-missing.log` 归位 TDD 链；F2（anchor r2 捕获 0 字节）→ 已补 `green-v1-compat-anchor-r3.log` 正证据 exit=0；F3 INFO（sender ulong vs judge A CSteamID）→ **CONFORMANT**（纯 C# seam 强制，spec L114；引擎绑定归 06）；F4 INFO（不做清单）→ 已履行（无新 V1 注册入口、无 Contracts 变更、无接线越界）。
- **双轴 R1 CLEAN 汇合**：无 BLOCKER；Standards 3 条 + Spec 2 条 findings 全部为可延后 smell 或已当场处置的记录卫生项。本票 V1 兼容层**交付**。

## 5. 解锁与移交

- **DEV-V2-06 接线面**：`LmnTakeoverCoordinator.Refresh()` 实例化时机（网络模块初始化）；面板「V1 兼容」开关 ↔ `LmnV1CompatLayer.Enabled` 绑定；`DiagnosticLogSink` → `BueRuntimeLog` 绑定（diagnosticId=BUE-V1COMPAT-001）；ulong ↔ CSteamID 引擎侧转换；LMN V1 注册入口重定向到兼容注册表。
- **真机 no-op 验证 = 可选项**（拍板允许，不阻塞闭环）：标注「真机验证待补」；若日后获得真实旧 V1 DLL（LIT/LIR/LHT 旧版本），作为真机补充证据计入证据链，三环境实机验证归 DEV-V2-07。
- 提交清单（本票）：6 源码文件（Program.cs + 三新 Core 文件 + 双 csproj）+ 票 + 本报告；运行器/构建 log 不入库（随 04 先例，报告引用其 audit 路径）。
