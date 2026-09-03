# DEV-V2-04：独立 LMN 接管实现

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-03-buenetworkapi-runtime
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「接管机制」）

## Scope

实现 BUE 对独立 LMN 的共存接管（T5 决策）：

- 检测：`BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.yu80rice.launchmultiplayernet")` 只读枚举已加载实例；检测时机 = BUE 网络模块初始化时（不依赖 Awake 顺序）；只处理"已加载"口径。
- 停用：BUE 网络模块对 `NetMessages.ReceiveMessageFromClient/Server` 注册 `Priority.First` Prefix，命中 MOD/LMN2 帧 `return false` 短路（LMN Prefix 永不被调）；未命中 `return true` 放行。
- 红线：不引入 `[BepInIncompatibility(LMN_GUID)]`（连坐 LIT/LIR/LHT 等硬依赖插件）。
- 残响：接受 LMN Awake 仍执行、静态表仍初始化但路由被短路（无业务副作用）。
- 面板：管理面板显示「已由 BUE 接管」状态条目 + 「让我改回独立 LMN」按钮（可逆性展示，与网络模块可关对齐）。

## 验收条件

- [ ] 红测先行：`--bue-takeover-red` 断言检测逻辑（注入 PluginInfos 快照）与 Prefix 短路语义——先红后绿。
- [ ] 接管检测在无 LMN 时零误报（PluginInfos 无该 GUID 即不接管）。
- [ ] 面板状态条目 + 停药按钮（`BueNativeManagementPanel` 现有 seam 测试）。
- [ ] 构建 0/0；七项目测试 PASS；token 扫描通过。

## 不做

- 不做 V1 兼容层（DEV-V2-05）；不删除/修改 LMN DLL；不实现运行时卸载（BepInEx 5 不支持）。
