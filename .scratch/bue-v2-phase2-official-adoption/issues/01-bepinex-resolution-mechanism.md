# T1：BepInEx 5 解析机制实证与 Forge-like 承诺可行性

Type: research
Status: claimed（2026-09-06，后台 research 代理已派出，报告落盘即 resolved）
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

（research 代理填：结论摘要 + 报告链接）
