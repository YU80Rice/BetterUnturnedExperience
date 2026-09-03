# 独立 LMN 共存接管机制

Type: wayfinder:research
Status: open
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T1-itransportconnection-shape（先查证 LMN/原生传输内部）

## Question

当用户同时安装 BUE 与独立 LMN 时，BUE 如何检测独立 LMN 实例并停用其运行（不删除 DLL、不误伤不相关插件）？

## 查证要点

1. 独立 LMN 的入口身份（BepInEx GUID、程序集名、Harmony patch 目标）——如何静态识别。
2. 检测机制：程序集扫描禁区（BUE 不扫描 plugins 目录）之外，如何发现"已加载的 LMN 实例"——依赖 BepInEx 运行时事实？事件订阅？
3. 停用机制：阻止独立 LMN 重复运行的可行路径（如在其入口短路、patch 拦截、运行时标记），对照"不删除用户文件、不误伤其他插件"约束。
4. 诊断/恢复：接管状态如何可诊断、可恢复（面板显示"已由 BUE 接管"）。
5. 证据：LMN 源码/反编译事实 + 文件路径 + 行号；必要时用 browser-skill 查公开仓库（Forge 的模块接管/冲突处理实现可作对照）。

## 答案

（resolved 时记录机制选型 + 证据）
