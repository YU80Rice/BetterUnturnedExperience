# GPT-06 — 独立功能工程合并为单 DLL 的构建方案

- 作者：GPT
- 调查日期：2026-08-24
- 调查方式：第一方工具文档、上游源码、本机既有项目与构建记录交叉验证
- 范围：只读研究；未修改生产代码，未创建构建原型，未执行客户端或 U3DS

## 决策摘要

**推荐：源码级聚合，采用“模块独立工程 + 模块拥有的共享源码清单（`.projitems` 或等价 `.props`）+ 一个发布聚合工程”作为首版正式方案。**

每个功能保留独立目录、独立 `.csproj`、测试和责任边界；模块项目与最终聚合项目导入同一份模块源码清单。发布聚合工程只执行一次 C# 编译，输出唯一的 `BetterUnturnedExperience.dll`，且只包含一个 `BaseUnityPlugin` / `[BepInPlugin]` 入口。公共契约、核心运行时、功能后端和功能前端在源码与命名空间层保持单向依赖，但不先编译为待合并的中间程序集。

**不推荐把 `ProjectReference` 产生的多个 DLL 再用 ILRepack/ILMerge 合并作为首版默认发布链。** 这条路线可保留为未来受控实验，但它增加了程序集元数据、类型可见性、PDB、资源、特性、反射和 Harmony 扫描的第二次变换，而且本仓库没有任何已验证的合并工具基线。

## 一、不可动摇的输入约束

1. GPT-05 已确定工程基线为 `.NET Framework 4.7.2 + C# 10`，客户端和 U3DS 使用不同的 `Assembly-CSharp.dll`，且 BepInEx 分别为 `5.4.23.5` 与 `5.4.22.0`。构建方案必须接受双引用集静态门禁和三环境独立运行门禁。
2. 用户要求各功能独立工程、开放协作、最终发布一个 DLL；某功能失败应被框架隔离，而不是拖垮整个插件。
3. U3DS 是 `-batchmode -nographics`。单 DLL 不能意味着核心启动时强制解析或实例化客户端 UI。
4. 共享契约由 GPT 维护；功能前端只能依赖契约，不能反向引用后端内部实现。

## 二、本机项目的实际构建证据

### 2.1 已有成功模式

本工作区现有 `LaunchMultiplayerNet.csproj`、`LaunchInventoryTidy.csproj` 和 `SteamP2PFriends.csproj` 都是传统 MSBuild C# Library 工程，并共同具备：

- `TargetFrameworkVersion=v4.7.2`
- `LangVersion=10`
- `Deterministic=true`
- 对 BepInEx、Harmony、Unity、Unturned 程序集使用显式 `Reference` + `HintPath`，且游戏侧引用 `Private=False`
- 用多个 `<Compile Include="..." />` 将 Core、Shared、Host、Client、Patches、UI 等目录一次编译进一个程序集
- Release 通常输出 PDB 或关闭符号，且已有项目使用 `TreatWarningsAsErrors=true`

可复查路径：

- `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\LaunchMultiplayerNet.csproj`
- `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\LaunchInventoryTidy\LaunchInventoryTidy.csproj`
- `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\SteamP2PFriends\SteamP2PFriends.csproj`

实际记录的命令包括：

```powershell
dotnet build .\LaunchMultiplayerNet.csproj -c Release --no-restore -nologo
dotnet build .\LaunchInventoryTidy.csproj -c Release -nologo -warnaserror
dotnet build .\LaunchInventoryTidy.csproj -c TestHarness -nologo -warnaserror
```

因此，“单次编译多个源码目录为单 DLL”已有本地工程先例；“先构建多个 DLL 再合并”没有本地已验证先例。

### 2.2 该证据能证明与不能证明的内容

能证明：传统 MSBuild 项目可以在当前工具链上把分目录源码稳定编译为单个 .NET Framework 4.7.2 DLL，并启用确定性编译。

不能证明：新的多模块导入布局、双引用集检查、BepInEx 双端加载、模块隔离、Harmony 注册和 PDB 路径已经可用。它们仍需原型和运行验证。

## 三、BepInEx 与 Harmony 的真实程序集边界

### 3.1 BepInEx 发现机制

BepInEx 5 `Chainloader` 会在插件目录调用 `TypeLoader.FindPluginTypes`，通过 Cecil 识别继承 `BaseUnityPlugin` 且带 `[BepInPlugin]` 的类型，然后按 GUID 和依赖关系排序、加载程序集并把插件类型作为 Unity 组件实例化。它还从程序集引用读取目标 BepInEx 版本。

这意味着：

- 一个物理 DLL 可以包含多个可发现插件类型，但本项目为了统一生命周期、故障隔离和单一产品身份，应只暴露一个 BepInEx 入口。
- 源码聚合不会改变最终程序集的二进制身份；后处理合并则会重写模块、AssemblyRef、类型和特性，必须重新证明 BepInEx 可发现性。
- 不应把 `BepInEx.dll`、`0Harmony.dll`、Unity 或 Unturned 程序集打进产品 DLL；它们是宿主提供依赖，且 BepInEx 会检查插件的 BepInEx AssemblyRef 版本。

第一方源码：

- BepInEx v5-lts `Chainloader.cs`：https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx/Bootstrap/Chainloader.cs
- BepInEx v5-lts `TypeLoader.cs`：https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx/Bootstrap/TypeLoader.cs
- BepInEx 5 插件开发文档：https://docs.bepinex.dev/v5.4.21/articles/dev_guide/plugin_tutorial/index.html

### 3.2 Harmony 类型扫描

Harmony 的 `PatchAll(Assembly)` 明确遍历给定程序集中的类型，找出带 Harmony 特性的类型并处理。源码聚合后，所有补丁类型天然属于最终程序集，扫描语义直接且可预期。

但是，项目的模块隔离要求比 `PatchAll()` 更严格：若全程序集一次扫描，任何第三方模块的坏补丁都可能使启动阶段失败。推荐由核心逐模块执行显式注册（显式补丁类型列表、模块专属 Harmony ID，或经验证可用的分类），每个模块各自 `try/catch`、记录失败并通知 UI；不得把“在一个程序集”误解为“必须一次性 PatchAll 全部类型”。

第一方来源：

- Harmony 官方 API 源码 `Harmony.PatchAll(Assembly)`：https://github.com/pardeike/Harmony/blob/master/Harmony/Public/Harmony.cs
- Harmony 官方注解文档：https://harmony.pardeike.net/articles/annotations.html

## 四、候选方案比较

| 维度 | A. 源码级聚合 / 共享项目 | B. 项目引用后嵌入或合并 | C. ILRepack / ILMerge 后处理 |
| --- | --- | --- | --- |
| 最终 DLL | 编译器原生直接产生一个 DLL | 常先产生多个 DLL，再由自定义装载或后处理收束 | 合并器重写多个输入程序集为一个 DLL |
| 功能工程隔离 | 强：独立 `.csproj` + 独立源码清单/测试 | 强 | 强 |
| 编译期依赖方向 | 聚合工程可统一检查；需防止导入顺序和重复源码 | `ProjectReference` 最自然 | `ProjectReference` 最自然，但合并后边界改变 |
| BepInEx 发现 | 最简单；最终编译产物直接含唯一入口 | 若运行时嵌入并动态加载，BepInEx/解析器路径更复杂 | 必须重新验证入口、AssemblyRef、特性和依赖元数据 |
| Harmony 扫描 | 类型天然处于最终程序集；可按模块显式注册 | 嵌入 DLL 需要装载后分别扫描 | 合并后通常可扫到，但类型重写/重复名/内部化需验证 |
| 内部化 | 用 C# `internal`、命名空间与契约项目约束，语义明确 | 原程序集可见性保留 | `/internalize` 会改写非主程序集类型；反射、序列化、扩展接口风险高 |
| PDB/调试 | 编译器直接生成一套 PDB，源文件映射最清楚 | 多 PDB；嵌入后诊断复杂 | ILRepack 声称可合并 PDB/MDB；仍是额外映射步骤，必须验证堆栈行号 |
| 确定性 | 可直接使用编译器 `/deterministic`；仍须固定引用和路径 | 各项目可确定，但嵌入顺序和资源需固定 | 输入确定不等于合并输出已证明确定；还需固定工具版本、顺序、参数并双构建比哈希 |
| 资源/特性 | 编译期发现冲突；只保留聚合工程 AssemblyInfo | 嵌入资源和解析器需自定义 | 主程序集元数据、`/copyattrs`、重复资源、重复类型都有工具特定规则 |
| 第三方贡献 | 目录、项目、测试、清单边界清晰；合入时受统一编译约束 | 独立性最好，但单 DLL 需额外装载设计 | 源码隔离清晰，发布链复杂且合并冲突晚出现 |
| 客户端/U3DS | 可用同一聚合源码对两套引用快照分别编译检查 | 每个项目都要双矩阵，组合面更大 | 每套中间产物和最终合并物都要双矩阵验证 |
| 首版适合度 | **最高** | 低；不符合“一个普通插件 DLL”的简单部署目标 | 低；新增未验证工具和元数据变换 |

MSBuild 的 `Compile` 项和 `Import` 是原生项目机制；C# 编译器的 `Deterministic` 选项用于在输入相同的情况下产生逐字节相同的程序集。来源：

- MSBuild 常用项目项：https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=vs-2022
- MSBuild `Import` 元素：https://learn.microsoft.com/en-us/visualstudio/msbuild/import-element-msbuild?view=vs-2022
- C# 编译器确定性选项：https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/code-generation#deterministic

## 五、推荐布局与构建契约

建议规格阶段固化以下逻辑布局（名称可在实现票中调整）：

```text
src/
  Contracts/
    BetterUnturnedExperience.Contracts.csproj
    BetterUnturnedExperience.Contracts.projitems
  Core/
    BetterUnturnedExperience.Core.csproj
    BetterUnturnedExperience.Core.projitems
  Features/
    BetterItemInteraction/
      BetterItemInteraction.csproj
      BetterItemInteraction.projitems
      Backend/
      Frontend/
      Tests/
  Plugin/
    BetterUnturnedExperience.Plugin.csproj
    BetterUnturnedExperiencePlugin.cs
```

构建契约：

1. 每个 `*.projitems`（或等价模块 `.props`）由模块自己维护，只声明该模块的生产源码；不得声明 `AssemblyInfo.cs`、BepInEx 插件入口、输出路径或宿主 DLL 复制行为。
2. 模块 `.csproj` 导入自己的源码清单，用于独立编译、测试和贡献者快速反馈；聚合 `.csproj` 导入所有获批准模块的同一清单。
3. 最终聚合工程是唯一拥有 AssemblyName、版本、程序集特性、嵌入资源清单和 `[BepInPlugin]` 入口的工程。
4. 发布产物只包含产品 DLL，以及作为独立诊断制品保存但不强制部署的 PDB/manifest/SHA-256。宿主提供的 BepInEx、Harmony、Unity、Unturned、LMN 等依赖绝不合并。
5. 依赖方向由工程引用和静态检查表达：Contracts 不引用 Unity/UI；Core 只引用 Contracts 和最小宿主表面；Feature Backend 不引用 Frontend；Frontend 可引用 Contracts 和对应功能契约；功能间禁止直接引用。
6. 聚合源码清单必须显式排序或由固定清单生成，禁止发布构建依赖不受控通配符。编译器通常不因 C# 文件顺序改变语义，但固定输入清单更利于审计和可复现 manifest。
7. 模块初始化由核心注册表逐个执行；每个模块独立状态机、日志域、Harmony ID 和故障边界。U3DS 不实例化 UI 模块，且核心启动不得触碰 UI 专属类型。

### 为什么仍称“独立工程”

独立性发生在源码所有权、可单独编译、测试、依赖规则、CODEOWNERS/PR 和模块生命周期上，而不是要求生产时保留独立 Assembly。最终发布工程复用同一源码清单编译，避免“开发工程源码”和“发布工程复制源码”发生漂移。

## 六、对 ILRepack / ILMerge 的专项结论

ILRepack 官方说明它把多个 .NET 程序集合并为单程序集，支持 `/internalize`、类型重命名、PDB/MDB、XML 文档、属性复制和资源选项。其 README 同时把自己描述为已停止的 ILMerge 的替代品。ILMerge 官方仓库显示最新版本为 `3.0.29`，发布于 2019-04-10，并写明只在 Windows 平台运行、不支持 Mono。

第一方来源：

- ILRepack 官方 README：https://github.com/gluck/il-repack/blob/master/README.md
- ILMerge 官方 README：https://github.com/dotnet/ILMerge/blob/master/README.md

因此：

- **反对 ILMerge**：工具陈旧，官方说明不支持 Mono，而目标运行时正是 Unity/Mono；即使“合并发生在 Windows 构建机”也不能消除最终 IL/元数据在 Mono 上的兼容风险。
- **暂不采用 ILRepack**：它比 ILMerge 更合适，但没有本仓库验证证据；`/internalize` 会改变模块对外可见性，可能破坏反射、设置描述发现、DTO 序列化、第三方扩展点或 Harmony 类型定位。
- 如果未来实验 ILRepack，必须把主插件程序集放第一位；只合并本仓库自有模块；默认禁止 `/internalize`，直到形成精确保留清单；禁止合并 BepInEx/Harmony/Unity/Unturned/LMN；固定 NuGet/tool 版本与响应文件；合并前后分别做 API/元数据检查；连续两次洁净构建比较 SHA-256；在客户端和 U3DS 分别验证加载、Harmony 注册、堆栈/PDB 和卸载/故障隔离。

## 七、确定性与供应链要求

`Deterministic=true` 是必要条件，不是充分条件。正式可复现构建还必须固定：

- SDK/MSBuild/Roslyn 版本
- NuGet 锁文件或等价锁定
- 客户端与 U3DS 引用快照及每个 DLL 的 SHA-256
- 源文件清单、生成器版本、资源顺序、版本号和构建配置
- 路径映射（避免 PDB/编译元数据吸收机器绝对路径）
- UTC/时区无关的版本生成规则；禁止把当前时间写入程序集或资源

CI 门禁应执行两次全新目录 Release 构建，比较产品 DLL 哈希；PDB 是否要求逐字节一致需单独决策，但至少必须能映射到仓库提交和源文件。

## 八、静态验收项

实现该方案时至少需要以下静态门禁：

1. 聚合产物只有一个 `[BepInPlugin]` 且继承 `BaseUnityPlugin` 的入口。
2. 产物不存在对模块中间 DLL 的 AssemblyRef；不存在被意外复制或嵌入的 BepInEx、Harmony、Unity、Unturned、LMN。
3. 所有正式模块源码只由模块清单声明一次；聚合构建无重复类型、重复资源、重复 AssemblyInfo。
4. Contracts 程序集/源码表面不引用 UI/Unity 实现类型；U3DS 激活路径不触碰 Frontend 类型。
5. 每个 Harmony 补丁可追溯到功能模块和专属 Harmony ID；禁止无隔离的全局 PatchAll 启动路径。
6. 对客户端和 U3DS 两套冻结引用分别执行编译或 API 兼容检查，记录引用哈希。
7. 两次洁净构建 DLL 哈希一致，输出 manifest 记录提交、工具版本、引用集、功能列表和哈希。

## 九、仍需构建与运行验证的边界

本研究只形成方案，以下均未被证明：

- `.projitems`/`.props` 原型在当前传统 ToolsVersion 15 工程与 `dotnet build` 下能无警告工作。
- 模块独立构建和聚合构建不会因 `internal` 可见性产生差异；若测试需访问内部类型，应使用受控 `InternalsVisibleTo`，不能扩大公共 API。
- 聚合 DLL 可被客户端 BepInEx `5.4.23.5` 和 U3DS BepInEx `5.4.22.0` 同时发现并实例化。
- Harmony 的逐模块注册能在真实 Unturned 版本上完整命中，且一个模块失败不会污染其他模块。
- U3DS 不会在类型装载/JIT 阶段解析客户端 UI 专属依赖。
- PDB 堆栈行号、两次洁净构建哈希、SP/SteamP2PFriends Host+Client/U3DS 均未验证。

只有完成构建原型、双引用集静态检查和三环境同候选 DLL 哈希运行证据后，才能把本方案从“推荐架构”提升为“已验证发布链”。

## 十、给后续规格的明确决议

1. 首版 MUST 使用源码级聚合，不得把 ILRepack/ILMerge 作为正式发布必经步骤。
2. 每个功能 MUST 有独立工程和唯一模块源码清单；聚合工程 MUST 导入同一清单，禁止复制源码形成第二事实源。
3. 最终 DLL MUST 只有一个 BepInEx 入口；功能是框架模块，不是多个隐式 BepInEx 插件。
4. 模块故障隔离 MUST 通过显式模块生命周期和逐模块 Harmony 注册实现，不能依赖程序集天然隔离。
5. 发布构建 MUST 固定工具和两端引用快照、生成 manifest，并以双洁净构建哈希作为确定性门禁。
6. ILRepack MAY 作为未来研究票实验，但在证明 BepInEx、Harmony、反射、PDB、确定性和双端运行兼容前不得进入正式链；ILMerge MUST NOT 采用。

