# GPT-DEV-12 独立规格审计 R1

- 审计对象：DEV-12「BUE 单 DLL 运行时程序集闭包」
- 基线：BUE-V1-RT01-20260824
- SourceSet：BUE-SS-20260824-02
- 审计角色：GPT 后端/构建与公开 ABI 边界审计
- 审计范围：源码聚合、程序集引用闭包、No-op Fixture 公开 ABI、Headless 隔离、构建/测试证据边界
- 审计时间：2026-08-25

## 一、最终判定

**判定：FAIL（1 项阻断项）**

单 DLL 主程序集的物理引用闭包、No-op Fixture 绑定及现有测试均通过；但 DEV-12 要求的“公开 Contracts/SDK 编译期与运行期身份策略”尚未被实现/固化为可消费的 SDK 规则与回归证据。当前只能证明仓库内 No-op Fixture 改为引用 `BetterUnturnedExperience.dll` 后没有悬空 Contracts/Core 引用，不能证明一个按公开 SDK/独立 Contracts reference assembly 编译的第三方 DLL 在只部署 BUE 主 DLL 时拥有唯一、可解析的契约类型身份。

## 二、阻断项

### B-01：公开 SDK/Contracts 编译期身份策略未闭环

- **问题**：`src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj` 仍会生成独立的 `BetterUnturnedExperience.Contracts.dll`；单 DLL 主产物则把同一源码编译进 `BetterUnturnedExperience.dll`。当前未见 SDK reference facade、明确的“第三方必须引用 BUE 主程序集”的发布规则，或针对独立 SDK 编译输入的 ABI 回归测试。
- **证据**：
  - `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj:11` 将 `ContractTypes.cs` 源码聚合进主 DLL；
  - `src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj:8` 仍产生同命名空间的独立 Contracts assembly；
  - `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:36-48` 仅验证仓库内 Fixture 的类型程序集等于主 DLL，不验证外部 SDK 编译路径。
- **风险**：第三方若按旧 Contracts DLL 编译，其 IL 会携带 `BetterUnturnedExperience.Contracts` AssemblyRef；玩家只安装 BUE 主 DLL 时可能出现 `FileNotFoundException`，或在同时放置旧 Contracts DLL 时形成重复类型身份/绑定歧义。这与 DEV-12 验收条件 3 和 GPT-18/B-08 的 SDK 边界要求不一致。
- **修复建议**：在交付包/SDK 规范中冻结一种且仅一种策略：
  1. 推荐：SDK 提供 compile-time-only reference facade，第三方编译结果必须将公开类型绑定到稳定的 BUE 主程序集身份；SDK facade 不得作为玩家运行时依赖，并增加外部 Fixture 编译/IL 回归测试；或
  2. 若保留独立 Contracts assembly 作为正式 ABI，则必须把它纳入运行时 Load Set，而这将不再满足“仅 BUE 主 DLL”的目标。
  同时在 README/SDK manifest/DEV-12 报告中记录引用命令、程序集身份、版本策略和禁止部署 Contracts DLL 的门禁。

## 三、已通过项目

| 审计项 | 判定 | 证据 |
|---|---|---|
| 主 DLL 聚合 | PASS | `BetterUnturnedExperience.Plugin.csproj` 聚合 `ContractTypes.cs` 及 Core 源文件；主 DLL 中可见 `BetterUnturnedExperience.Contracts.*` 与 `BetterUnturnedExperience.Core.*` 类型实现。 |
| 主 DLL 私有 AssemblyRef 闭包 | PASS | `Assembly.GetReferencedAssemblies()`：`mscorlib 4.0.0.0`、`BepInEx 5.4.23.5`、`System.Core 4.0.0.0`、`System 4.0.0.0`、`UnityEngine.CoreModule 0.0.0.0`；无 `BetterUnturnedExperience.Core`/`BetterUnturnedExperience.Contracts`。 |
| No-op Fixture 运行时 ABI | PASS（仓库内 Fixture） | Fixture AssemblyRef 为 `mscorlib`、`BepInEx 5.4.23.5`、`BetterUnturnedExperience 0.0.0.0`；IL 中 Contracts 类型均以 `[BetterUnturnedExperience]` 解析，未引用 Core。 |
| Headless/Contracts 隔离 | PASS（静态） | Contracts/Core/Release 源码未发现 Unity、Glazier、Sleek、SDG.Unturned、LMN、Harmony、Steamworks、`Assembly.GetTypes`、`PatchAll` 等禁止类型/发现入口。主 DLL 仅在入口层引用 BepInEx/Unity。 |
| ClientUi 卫星边界 | PASS（范围内） | `ClientUi.dll` 独立引用主 BUE assembly；BUE 主 DLL不反向引用 ClientUi，符合可选卫星/Headless 隔离方向。真实卫星运行未由本票证明。 |
| 运行时范围 | PASS（边界声明） | 本审计不把静态 IL、单元测试或仓库产物等同于 clean-install、SP、P2P、U3DS 或 Better Item Interaction 运行通过。 |

## 四、构建与测试复测

### 构建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：**成功，0 errors / 0 warnings**（.NET MSBuild 18.9.0-preview）。

补充：直接调用旧 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe` 无法验证 C# 10（其 CSC 报 CS1617），属于工具链不支持 C# 10 的环境差异；最终构建使用仓库既定 C# 10 兼容的 `dotnet msbuild`。

### 7 个测试项目

| 测试 | 结果 |
|---|---|
| DEV-03 Settings | PASS |
| DEV-04 Placement | PASS |
| DEV-05 ClientUi | PASS |
| DEV-06 Network | PASS |
| DEV-08 Runtime Evidence Package | PASS |
| DEV-10 Registration Runtime | PASS |
| DEV-11 External Fixture | PASS |

## 五、构建产物哈希（本次复测）

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` |
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `A2F128B444A0A851D2C8E2B09DE913D1FFB7958AAE4C982590E57C843F13E852` |

## 六、结论与后续门禁

当前可以确认：**BUE 主 DLL 已完成仓库内源码聚合，并且不再依赖私有 Core/Contracts DLL；No-op Fixture 的新 ABI 绑定在静态/单元测试中成立。**

在 B-01 修复并补充外部 SDK 编译/类型身份回归测试前，不应把 DEV-12 标记为 `resolved`，也不应把当前结果宣称为“任意第三方插件只安装 BUE 主 DLL 即可运行”。修复后必须重新执行 Release 构建、全部测试、AssemblyRef/TypeRef 扫描，再进入 Gemini 复核与真实 clean-install 冒烟。
