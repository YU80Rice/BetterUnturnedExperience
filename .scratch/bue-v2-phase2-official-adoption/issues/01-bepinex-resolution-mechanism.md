# T1：BepInEx 5 解析机制实证与 Forge-like 承诺可行性

Type: research
Status: resolved（2026-09-06，第三次派出成功;前两次为基础设施失败非任务失败）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 无

## Question

实证 BepInEx 5.4.23.5 的解析机制，回答「BUE 能否向第三方承诺：引用 BUE 前置不受 BUE DLL 文件名影响」：

1. 前置依赖解析按 GUID 还是文件名（反编译 `Libs/BepInEx.dll` 的 Chainloader：依赖检测 + 拓扑排序）；
2. 加载顺序的真实决定因素（GUID 字典序 vs 文件名序——与「BepInEx 按文件名序加载」历史实证严格对账：类型发现阶段 vs Awake 执行顺序）;
3. 跨插件类型引用的运行时绑定按程序集名还是文件名（CLR 依据 + `Assembly.LoadFile` 上下文分析）;
4. 改名 `BetterUnturnedExperience.dll` 的真实敏感面；用户报告的「改名即检测不到前置被挂起」能否复现，若不能则辨析其混入的三件真实事实；
5. 防双装事实：第三方误拷 BUE DLL 致同 GUID 双实例时 BepInEx 5.4.23.5 的行为；
6. 结论：Forge-like 承诺与防双装检测的可行性判定 + 仍需实机/实验验证的点具名。

## Answer

报告：`../research/2026-09-06-bepinex-resolution-mechanism.md`（对照 5.4.23.5 本机 DLL 反编译,行号复核 L45/L306/L330-343/L363-391/L395-404）。

1. **前置按 GUID,不按文件名**:`PluginInfos` 是 GUID 字典;`BepInDependency.DependencyGUID` + 拓扑 + 硬依赖检查;缺前置文案只含 GUID。
2. **Awake/`Loading` 序 = GUID 拓扑序(独立节点按 GUID 字典序),不是文件名序**;`GetFiles` 只用于发现且序不保证。实机六插件序与 GUID 拓扑同构,与「B < L」相反——**「BepInEx 按文件名序加载」系讹传**(镜像时机 bug 的真根因是 LMN 类型名,DEV-V2-11 已勘误,本条二次勘误其机制叙事)。
3. **IL 绑定键是 AssemblyName,不是文件名**;`LoadFile(Location)` 用真实路径,身份仍是 `BetterUnturnedExperience`。
4. **仅改部署文件名,5.4.23.5 不会「检测不到前置被挂起」**;用户报告更像混入 HintPath(编译期)/AssemblyName(IL)/同 GUID 双文件(跳过一份)。
5. **Forge-like 承诺可行**:冻结 GUID `io.github.yu80rice.betterunturnedexperience` 与 AssemblyName,禁止消费方附带第二份 BUE;同 GUID 双装 = 留一份、Warning 跳过另一份,不双 Awake。
6. **待验具名**:改名实机对照、Mono LoadFile 二次探测、同版本谁留下、不同 GUID 同 AssemblyName、Preloader AssemblyResolve——移交 T7 契约票的验收面设计。
