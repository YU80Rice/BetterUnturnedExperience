# 独立 LMN 共存接管机制

Type: wayfinder:research
Status: resolved（2026-09-03 查证 + grilling 拍板，机制冻结）
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T1-itransportconnection-shape（已 resolved；先查证 LMN/原生传输内部）

## Question

当用户同时安装 BUE 与独立 LMN 时，BUE 如何检测独立 LMN 实例并停用其运行（不删除 DLL、不误伤不相关插件）？

## 查证要点

1. 独立 LMN 的入口身份（BepInEx GUID、程序集名、Harmony patch 目标）——如何静态识别。
2. 检测机制：程序集扫描禁区（BUE 不扫描 plugins 目录）之外，如何发现"已加载的 LMN 实例"——依赖 BepInEx 运行时事实？事件订阅？
3. 停用机制：阻止独立 LMN 重复运行的可行路径（如在其入口短路、patch 拦截、运行时标记），对照"不删除用户文件、不误伤其他插件"约束。
4. 诊断/恢复：接管状态如何可诊断、可恢复（面板显示"已由 BUE 接管"）。
5. 证据：LMN 源码/反编译事实 + 文件路径 + 行号；必要时用 browser-skill 查公开仓库（Forge 的模块接管/冲突处理实现可作对照）。

## 已预埋事实（T1 查证，research 直接引用不必重查）

- LMN 是 BepInEx 插件（`LaunchMultiplayerNet.dll`），其 Harmony Prefix 拦截目标 = `NetMessages.ReceiveMessageFromClient(ITransportConnection, byte[], int, int)`（U3-SDK `NetMessages.cs:123`）；帧识别靠魔数 + `ModRouter.BuildModPacket`（LMN `Routing\ModTransport.cs:714-726`）。
- LMN 程序集引用集：`mscorlib, BepInEx, 0Harmony, com.rlabrecque.steamworks.net, SDG.NetTransport, Assembly-CSharp, UnityEngine.CoreModule, System`（T1 反编译确认）。
- LMN V2 命名频道 = pluginGuid 路由；V1 = int virtualChannel——两种帧都在 `ITransportConnection.Send` 之上（T1 报告）。
- 停用需满足：不删除 DLL、不误伤不相关插件、可诊断可恢复（CONTEXT.md「网络能力接管」L69-71 +「独立 LMN 文件处理」L81-83）。

## 答案

### 查证结论（research 报告 `research/V2-T5-lmn-takeover-mechanism.md`）

- **检测**：`BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.yu80rice.launchmultiplayernet")` 只读枚举已加载插件实例——零目录扫描、零 `Assembly.GetTypes()`（BepInEx.dll 5.4.23.5 反编译核验：L45 声明、L401 写入、L402 实例）。
- **停用（唯一可行 = 优先权抢占）**：BUE 网络模块对 `NetMessages.ReceiveMessageFromClient/Server`（U3-SDK L123/L167）注册 `Priority.First` Prefix，语义与 LMN 同构（命中 MOD/LMN2 帧 `return false` 短路），LMN Prefix 永不被调、处理器永不触发——唯一网络 Hook/频道注册/消息路由。
- **否决路径**：a) patch LMN Awake 不可靠（GUID 字典序 BUE 在 LMN 后）；c) LMN 无外部标志钩子；d) BepInEx 5 不支持运行时卸载。
- **红线**：静态 `[BepInIncompatibility(LMN_GUID)]` 不可用——LIT/LIR/LHT `[BepInDependency(HardDependency)]` 会连带 skip 未迁插件，误伤用户插件。

### grilling 拍板（2026-09-03 人工）

- **Q1 接管覆盖面 = A**：BUE 覆盖 V1 legacy + V2 named 全部帧（T3/T4 合流，避免 double-handling）。
- **Q2 残响语义 = A**：接受 LMN Awake 仍执行、静态表仍初始化、但路由被 BUE 短路；仅占内存无业务副作用，BUE 卸载即 LMN 恢复原样。
- **Q3 连坐红线 = A**：禁止 `BepInIncompatibility`（T8 修正后"全迁"依据不成立；且违反不误伤原则）。
- **Q4 面板 UX = B**：「已由 BUE 接管」状态 + 「让我改回独立 LMN」按钮（可逆性展示，与 T7 网络模块可关对齐）。
- **Q5 检测口径 = A**：只处理"已加载"实例（DLL 存在但被跳过 = 没在跑 = 不接管，避免误报、不扫目录）。
- **Q6 排序依赖 = A**：不需要 BUE 先于 LMN——Harmony 优先级抢占与加载顺序无关，检测时机 = BUE 网络模块初始化时。

### 用户方向指令（2026-09-03，影响全 V2）

> "后续 LMN、LIT、LIR、LHT 等功能**不再作为单独的插件存在，也不再单独维护**，而是**并入 BUE 内部作为官方功能**。"

- 含义：这四个项目走"官方纳入"（吃掉消化）路径，最终玩家只部署 `BetterUnturnedExperience.dll`；V1 兼容层服务对象收敛为"未知第三方"，已发布已知生态全部并入 BUE。
- 影响：T4 退出阈值中"已知生态插件迁移"维度随官方纳入自然消解；LIT/LIR/LHT 的 V2 迁移验证并入 BUE 实施阶段（作为官方功能实机验证）。
- 解锁：T6（LMN 配置迁移映射）。
