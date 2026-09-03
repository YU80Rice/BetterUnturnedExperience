# DEV-V2-04：独立 LMN 接管实现

Type: task
Status: resolved（2026-09-03 本会话交付；用户拍板边界 = 只交决策核）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-03-buenetworkapi-runtime
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「接管机制」）

## Scope（用户拍板 2026-09-03：本票只交 PVC 决策核）

实现 BUE 对独立 LMN 接管**决策核**（纯 C# Host 内部，可经 7 项目 Plugin.Tests 红测锁定；不触碰 BepInEx/引擎类型）：

- 检测 seam：`Func<bool>` 探针注入（生产绑定 `BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.yu80rice.launchmultiplayernet")`；测试注入 stub）。检测时机 = 调用方（BUE 网络模块初始化）触发的 `Refresh()`。只处理"已加载"口径。
- 停用语义：`ShouldShortCircuit(frame)` 仅在接管激活 **且** 命中 MOD/LMN2 帧时短路（返回 true → Priority.First Prefix `return false`）；非 LMN 帧恒放行。可逆性由激活状态与检测探针完全决定。
- 帧识别：`LmnFrameClassifier.IsLmnFrame` 判 V1 legacy "MOD"（0x4D 0x4F 0x44）+ V2 named "LMN2"（0x4C 0x4D 0x4E 0x32）（LMN `ModRouter.cs:13-24`）。
- 红线：不引入 `[BepInIncompatibility(LMN_GUID)]`（连坐 LIT/LIR/LHT 等硬依赖插件）。
- 残响：接受 LMN Awake 仍执行、静态表仍初始化但路由被短路（无业务副作用）；本决策核不承诺运行时卸载（BepInEx 5 不支持）。

## 移交范围（真实接线归 DEV-V2-06）

- Harmony `Priority.First` Prefix 实际 patch `NetMessages.ReceiveMessageFromClient/Server`、Chainloader 绑定、面板「已由 BUE 接管」+「让我改回独立 LMN」可逆钮 → **DEV-V2-06（网络模块接线）**（用户拍板）。
- 三环境实机验证接管语义 → DEV-V2-07。

## 验收条件

- [x] 红测先行：`--bue-takeover-red`（`AssertBueTakeover`）断言检测逻辑（注入 stub）与短路语义（MOD/LMN2 短路、非 LMN 放行、未激活放行、空/短帧不误判）——先红后绿。
- [x] 接管检测在无 LMN 时零误报（探针返回 false → 不激活 → 恒放行）。
- [x] 构建 0/0；七项目测试 PASS；token 扫描通过（Core 零命中）。（r2 构建 19:51:03 0/0；7 运行器 exit=0；Core=13/ClientUi=11 文件零命中；`--bue-takeover-red` ✅。已log：`audit/2026-09-03/DEV-V2-04/`）

## 不做

- 不做 V1 兼容层（DEV-V2-05）；不删除/修改 LMN DLL；不实现运行时卸载（BepInEx 5 不支持）。
- 不做真实 Harmony Prefix / Chainloader 绑定 / 面板可逆钮（DEV-V2-06）；不做三环境实机（DEV-V2-07）。

## DEV-V2-03 移交范围补充（Spec R3 建议，2026-09-03）

- **可靠透传（wire reliability bit）**：DEV-V2-03 的 `Send*` 接受 `reliable` 布尔但未写入帧（`SendFrame` 无可靠位）；DEV-V2-03 遗留，归 **DEV-V2-06**（真实传输接线时映射 `ENetReliability` 并补帧可靠位）。
- **按帧来源解析分发会话**：DEV-V2-03 的 `DispatchData` 以"首个会话"为接收上下文（单会话 loopback 正确）；归 **DEV-V2-06**（帧携带发送者身份后按来源解析到正确会话）。
