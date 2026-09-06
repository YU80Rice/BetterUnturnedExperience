# DEV-12：BUE 单 DLL 运行时程序集闭包

Type: task
Status: resolved
Author: GPT
Owner: GPT（总维护者/后端构建与运行时）
Required reviewer: Gemini（前端消费、公开 ABI 与 Headless 边界）
Baseline: BUE-V1-RT01-20260824
SourceSet: BUE-SS-20260824-02
Depends on: DEV-09, DEV-10, DEV-11, SCR-GPT18-001

## 背景

DEV-09 的多程序集部署已在真实客户端 BepInEx 环境启动成功，但“玩家只安装 `BetterUnturnedExperience.dll` 即可使用 BUE”尚未满足。

18:16:04 诊断包显示：

- BepInEx 5.4.23.5 发现并加载 `Better Unturned Experience 0.0.0`；
- BUE 输出 `featureId=io.github.yu80rice.betterunturnedexperience status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001`；
- 当前部署同时包含 `BetterUnturnedExperience.dll`、`BetterUnturnedExperience.Core.dll`、`BetterUnturnedExperience.Contracts.dll`；
- 三个部署 DLL 与 Release 构建产物哈希一致。

因此，多 DLL 运行链已验证；单 DLL 运行闭包仍是独立阻断项。

## 目标

构建一个物理可部署的 `BetterUnturnedExperience.dll`，使 BUE 自身运行时不再要求玩家额外安装 `BetterUnturnedExperience.Core.dll` 或 `BetterUnturnedExperience.Contracts.dll`，同时保持公开外部功能接入 ABI、类型身份、Headless 隔离和现有 DEV-01～DEV-11 行为不回归。

## 实施范围

- 选择并记录单 DLL 方案：源码合并、受控 IL 合并或等价的确定性链接方案；不得使用不可审计的运行时下载或隐式目录扫描。
- 明确公开 Contracts/SDK 的编译期与运行期身份策略，禁止出现重复类型身份、悬空 AssemblyRef 或外部 Fixture 运行时绑定歧义。
- 让 `BetterUnturnedExperience.NoOpFixture.dll` 仍作为独立 BepInEx DLL，通过公开 BUE ABI 注册，不引用私有 Core 程序集。
- 建立构建后 AssemblyRef/TypeRef/IL 可达性门禁：单 DLL 输出必须具备自洽运行时依赖闭包（BepInEx、Unity 与 Unturned 原生运行库为宿主依赖例外）。
- 在真实客户端 clean-install 中只部署 BUE 主 DLL，验证 `BootstrapReady`；随后再验证主 DLL + No-op Fixture 的公开注册链。

## 明确不做

- 不实现 Better Item Interaction 玩法、库存 Hook、Glazier UI 或 LMN 网络功能。
- 不修改 BepInEx、U3DS、Unturned 原版内容或 LMN。
- 不把“把 Core/Contracts DLL 一起复制”作为单 DLL 解决方案。
- 不宣称 U3DS、SteamP2PFriends Host/Client 或三环境功能验收通过；本票只验证程序集闭包与 BepInEx 启动/注册冒烟。

## TDD 验收条件

1. Red：在只含 `BetterUnturnedExperience.dll` 的干净插件目录中，AssemblyRef/部署闭包测试先稳定失败；Green：单 DLL 产物通过同一测试。
2. `BetterUnturnedExperience.dll` 不再对私有 `BetterUnturnedExperience.Core`/`BetterUnturnedExperience.Contracts` 产生未满足的运行时 AssemblyRef。
3. 公开 `IFeatureRegistration`、`BueRuntimeHost`、DTO/枚举的类型身份只有一个可解析事实源；No-op Fixture 不出现类型加载冲突。
4. Contracts/Core/Release 的 UI/native/LMN/BepInEx 隔离规则不被单 DLL 方案破坏；静态 IL 可达性扫描 PASS。
5. Release solution 0 errors / 0 warnings；DEV-02～DEV-11 现有测试全部 PASS，并新增单 DLL 闭包回归测试。
6. 客户端 clean-install 仅部署 BUE 主 DLL 时，BepInEx 日志出现 `BootstrapReady`；日志不得出现 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException`。
7. 客户端部署 BUE 主 DLL + No-op Fixture DLL 时，Fixture 通过公开 Host 注册，且不要求复制私有 Core/Contracts DLL。
8. 记录最终 BUE DLL、No-op Fixture DLL、SDK/编译期资产（如仍需）的路径、版本和 SHA-256；构建可确定性复现。
9. 完成 GPT 独立审计；通过后交 Gemini 复核。真实冒烟通过前工单不得 `resolved`。

## 交付物

- `GPT-DEV-12-Single-Dll-Assembly-Closure-Implementation.md`
- `GPT-DEV-12-Independent-Audit-R*.md`
- `to-DEV-12-single-dll-review.md`
- 单 DLL 依赖闭包/类型身份回归测试日志
- 客户端 clean-install BepInEx 日志与哈希清单

## 当前证据边界

- 多 DLL 客户端启动：已由 UMM 诊断包 `UMM-诊断包_20260825_181604` 证明 BUE `BootstrapReady`。
- 单 DLL 客户端启动：未通过；当前主 DLL 的 IL 仍引用 Core/Contracts，单 DLL 目录检查为红灯。
- U3DS、单人功能、SteamP2PFriends Host/Client、Better Item Interaction：未由本票证明。

## GPT 实施进度

- TDD Red：旧主 DLL 对 Core/Contracts 存在 AssemblyRef，单 DLL 闭包测试失败。
- TDD Green：Contracts/Core 源码已作为链接编译输入嵌入 `BetterUnturnedExperience.Plugin`；主 DLL 不再包含两项私有 AssemblyRef。
- No-op Fixture 与 ClientUi 已切换到主程序集公开 ABI；Release 构建与全套现有测试 PASS。
- SDK 身份已冻结：第三方编译期/运行期均引用 `BetterUnturnedExperience.dll`；独立 Contracts DLL 仅为内部 compile-time/test artifact，不得部署。
- 已新增外部 SDK ABI AssemblyRef 回归断言与规则文档 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`。
- Gemini 前端消费复核：ACCEPT（`DEV-12-Single-DLL-Review.md`）；静态 ABI 与依赖闭包复核通过，同意进入人工 clean-install 单 DLL 冒烟阶段。

## 人工 clean-install 单 DLL 冒烟证据（2026-08-25）

- 部署目录：`E:\Steam\steamapps\common\Unturned\BepInEx\plugins`
- 实际部署：仅 `BetterUnturnedExperience.dll`（`BetterUnturnedExperience.Core.dll` 与 `BetterUnturnedExperience.Contracts.dll` 均不存在）
- 实际部署 DLL SHA-256：`F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C`
- 诊断包：`UMM-诊断包_20260825_190429`
- BepInEx 日志：出现 `status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001`
- 目标异常扫描：`TypeLoadException`、`FileNotFoundException`、`MissingMethodException`、BepInEx Error/Fatal/Exception 均未命中
- UMM 摘要：`Normal`，受管会话退出码 `0`
- 证据边界：仅证明客户端 BepInEx 单 DLL 启动/程序集闭包冒烟；不证明 U3DS、单人、SteamP2PFriends Host/Client、No-op Fixture 联合部署或 Better Item Interaction 功能验收。
- GPT 独立运行证据审计 R3：`audit/2026-08-25/DEV-12-Independent-Audit-R3.md`，判定 `PASS`。
- 关闭报告：`audit/2026-08-25/Implementation-DEV12-CleanInstall-1910.md`。


