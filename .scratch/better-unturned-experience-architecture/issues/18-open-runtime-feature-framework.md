# GPT-18：BepInEx 前置框架运行时与独立功能注册

Type: task
Status: needs-triage
Author: GPT
Blocked by: GPT-14, GPT-16, DEV-09, SCR-GPT18-001

## 目标

将 BUE 从“源码合并为单一聚合 DLL 的产品”升级为“运行在 BepInEx 之上的公开前置框架插件”。BepInEx 负责最低层发现和装载；BUE 提供自己的官方功能，并为独立 BepInEx 功能插件提供 Contracts/SDK、注册、生命周期、设置、能力、环境适配、统一 UI 和兼容验证运行时。LMN 是开放网络插件框架的参照，但 BUE 覆盖更大的范围。

## 用户意图

在 BepInEx 已安装的前提下，玩家只需把 `BetterUnturnedExperience.dll` 和其他功能 DLL 放入 plugins 目录；功能在 BUE 统一管理列表中出现并共享设置、生命周期、兼容性和故障隔离能力。开发者仅依据公开规范和仓库开发，不必修改 BUE 源码，也不必重复研究框架基础设施；但仍必须为自己功能的玩法行为和声明环境提供针对性证据。

## 规范产物

- 英文 Agent 规格：[spec-open-runtime-feature-framework.md](../spec-open-runtime-feature-framework.md)
- 中文人工规格：[spec-open-runtime-feature-framework.zh-CN.md](../spec-open-runtime-feature-framework.zh-CN.md)

## 最高测试 Seam

显式功能注册与统一管理运行时：注册验证 → 确定性 Runtime Catalog → Feature Load Gate → feature-scoped bootstrap → Settings Facet/Snapshot 聚合 → 生命周期隔离 → Headless 门禁。

## 重要边界

- 当前 DEV-09 的单一 BepInEx 入口仅代表 BUE 前置框架插件入口；BepInEx 仍是底层加载器，未来外部功能可以拥有独立 BepInEx 入口。
- 禁止 BUE 全目录扫描、`Assembly.GetTypes()`、类名猜测和全局 `PatchAll()` 发现功能。
- 外部功能核心 DLL 与可选客户端 UI 卫星 DLL 分离，U3DS 不解析 UI 类型。
- 不恢复运行时 `Describe()` 或任意 FeatureId 查询；注册数据来自 SDK 生成的定义产物。
- BUE 框架测试不能替代第三方功能自己的 SP/P2P/U3DS 玩法证据。
- 不提供 C# DLL 沙箱；显式安装是信任决定。

## 下一步验收条件

- 人工开发者、GPT 和 Gemini 共同接受该架构变更。
- 完成 Shared Contract Change Request，冻结注册、Catalog 组合、API 版本和 UI 卫星 Seam。
- 建立 no-op 外部功能 fixture，证明独立 DLL 可注册、显示在统一列表、接受设置快照并被局部隔离。
- 通过静态 IL/引用扫描、确定性组合测试、客户端/Headless 加载测试和独立审计。
- 在此票据关闭前，不修改 DEV-01～09 已验收结论，不宣称三环境功能运行通过。

## Comments

### 2026-08-25 GPT 初始规格化

该提案正式记录了用户对“运行在 BepInEx 之上的大型、带有自身官方功能的前置框架”的产品意图。现有“V1 不动态加载外部 DLL”仍保持冻结，等待本票与共享契约变更共同审批。

### 2026-08-25 GPT 后端独立复核 R1

判定：**REVISE**。

接受产品定位、BepInEx/BUE/LMN 分层、显式注册方向、统一设置和局部隔离目标；阻断项为：

1. BUE Host 物理发布程序集与当前 Contracts/Core 多程序集拓扑未对齐；
2. 公开注册 Interface、错误族、调用时序和所有权尚未冻结；
3. `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 注册阶段未进入正式契约；
4. U3DS 必须从部署层排除 ClientUi 卫星，不能只依赖 batchmode；
5. 官方功能是 BUE Host 内置还是独立官方 DLL 存在矛盾；
6. CandidateBuild 必须升级为绑定完整 BUE/feature/satellite LoadSetIdentity；
7. BUE 隔离不覆盖 BepInEx/CLR 注册前装载失败；
8. SDK 必须明确为编译期工具，第三方运行时只依赖 BUE Host public ABI。

完整报告：[18-Independent-Backend-Review-R1.md](../../../audit/2026-08-25/18-Independent-Backend-Review-R1.md)

在上述问题和 `SCR-GPT18-001` 关闭前，不得将本票标记为 `resolved`，不得开始第三方功能 DLL 生产接入。

### 2026-08-25 Gemini 前端独立复核

Gemini 判定：**ACCEPT（接受架构升级方向）**，并提出以下必须纳入 SCCR/实现的前端消费约束：

- ClientUi 卫星缺失或失败时投影 `PresentationDegraded`/`HeadlessOnly`，但 BUE 统一设置中心仍可消费和编辑 Settings Facet。
- BUE 拥有统一设置模态窗 UI 主权；第三方不得注入原始 Glazier/Sleek/Unity 控件；自定义 HUD 通过 ClientUi satellite Seam。
- 官方“更好的物品交互”必须与第三方功能使用同一公开注册、生命周期和隔离 Seam。
- 生产实现前必须完成 SCCR，保持 DEV-01～DEV-08 共享契约稳定。

联合解释：Gemini 的 ACCEPT 代表前端消费无阻断，不代表后端 B-01～B-08 已关闭。GPT-18 当前结论保持 **ACCEPT IN PRINCIPLE / REVISE FOR IMPLEMENTATION**。

