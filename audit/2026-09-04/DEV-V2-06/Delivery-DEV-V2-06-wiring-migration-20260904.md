# 交付报告 — DEV-V2-06：接管接线 + 空迁移 + 网络模块设置

> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-06-config-migration-network-settings.md`
> 阶段：V2 第一阶段实施第 6 票（/implement + /tdd 红测 + 双轴独立审查）
> 性质：把 DEV-V2-04 决策核与 DEV-V2-05 V1 兼容层接成真实接管（生产路径），网络模块成为官方设置 facet
> 对照决策：spec T4/T5/T6/T7（`../spec-V2-phase1-lmn-adoption.md` L71-103）；三接缝决策以 handoff-DEV-V2-06（2026-09-03）定稿为准

## 1. 交付内容

**三接缝决策落地（handoff §三接缝决策，全部按定稿实现）：**
1. **V1 注册入口 = 反射镜像**：接管激活时读独立 LMN `ModTransport` 的静态 `ServerHandlers`/`ClientHandlers` 表（`LmnV1TableMirror.MirrorLegacyHandlers`，Core 纯 C#；handler 首参类型从委托签名自解析、`Activator.CreateInstance(type, ulong)` 构造，Core 不点名引擎类型）；镜像失败（类型 null/字段缺失/异常）→ 诊断 `BUE-V2NET-002` + V1 帧走兼容层未知频道丢弃路径，绝不 crash。
2. **LMN2 帧 = Prefix 决策后反射委托 LMN 自身 router**：`TryHandleFromClient/FromServer` 返回 true → Prefix `return false`（消费）；返回 false → Prefix `return true`（交还 vanilla + LMN Prefix）；异常 → 诊断 + 交还（LMN 自愈）。「唯一 Hook」= BUE 是唯一 patch 决策点，LMN Prefix 正常路径永不执行。
3. **双 facet 开关 + 三元条件 + 可逆钮**：`io.github.yu80rice.bue.network`（`network.enabled` Toggle 默认开）与 `io.github.yu80rice.bue.network.v1compat`（`v1compat.enabled` Toggle 默认开，spec T4 L74 锚定归位——票面未列，交付报告说明）；激活 = `networkEnabled && takeoverActive && LMN 帧`；可逆钮 = 面板把 `network.enabled` 置 false → `UnpatchSelf` → 独立 LMN Prefix 恢复，重新打开自动重臂。

**Core（`src/BetterUnturnedExperience.Core/Network/`，纯 C# 零禁词，双 csproj 嵌入）：**

| 能力 | 实现 |
|---|---|
| 零拷贝分类 | `LmnFrameClassifier.IsLmnFrame/IsLegacyV1Frame/IsNamespacedV2Frame(byte[], int, int)` offset 重载（热路径无子数组分配；现有单参 API 不变） |
| 传输 seam 扩宽 | `INetworkTransport.Send(byte[] frame, bool reliable, ulong targetSteamId)`（0=untargeted）；`LocalLoopbackTransport`/`LmnTransportAdapter` 同步（loopback 忽略两轴，signature parity） |
| 真传输 seam | `HostNetworkTransportAdapter`（`LmnTransportAdapter` 同构 + reliable + target；生产绑引擎反射、测试绑环回委托） |
| 帧格式 v2 | `BueNetworkRuntime`：`[BUE2 4x][kind 1][chanLen 1][channel n][sender 8][payload]`；`SendFrame(kind, channel, payload, reliable, target)` 盖本地 steamId；`SendToClient` 传 `target = session.PeerSteamId`；`DispatchData` 按 `session.PeerSteamId == sender` 解析，无匹配丢弃（**不再"首会话"**）；控制帧 payload 不变（兼容 03 握手逻辑） |
| V1 表镜像 | `LmnV1TableMirror.MirrorLegacyHandlers(Type, LmnV1CompatRegistry)` 返回镜像条数；字段名仅 `ServerHandlers`/`ClientHandlers`；FQN 与 TypeByName 归 Plugin（禁词只查 Core+ClientUi） |

**Plugin（`src/BetterUnturnedExperience.Plugin/`）：**

| 能力 | 实现 |
|---|---|
| 决策核接线 | `NetworkModuleAdapter`：ctor 五参（settingsRootPath + 三 Func + refreshPanel）；`ActivateCore()`（探针 refresh + 双开关读取 + 激活时镜像 + 空迁移记录 + 状态；**不做 Harmony**）；`ApplyNetworkPatches()`（生产才调；探针 false/网络关 → 零 patch 零反射零镜像）；`RefreshSwitches()`（面板编辑后重读开关 + 卸载/重臂）；`IsolateAndDetach()`（UnpatchSelf + 清态 + 清 `ActiveAdapter`）；`ShouldConsumeInbound(fromClient, senderSteamId, packet, offset, size, connection)` 决策核 |
| Priority.First Prefix | 两个静态 Prefix 拦 `NetMessages.ReceiveMessageFromClient/Server`（该类 internal → `AccessTools.TypeByName` 反射解析，与独立 LMN 同法）；方向守卫镜像 LMN（ClientPatch L58/ServerPatch L57）；成员存在门 + 手动 `harmony.Patch` + `{ priority = Priority.First }`（仓库首例）+ `UnpatchSelf` 可逆；sender steamId 经缓存的 `TryGetSteamId`/`m_SteamID` 反射提取（免 Steamworks 编译期引用），仅 V1 帧路径需要 |
| 官方注册 | `NetworkModuleFeatureRegistration.Register()`：公共桥注册（官方功能与外部功能平权）；`ClientUi = null` 合法（headless 能力，注册运行时接受）；构造即武装（**实况：注册运行时只存工厂、从不调用 `Module.Start`**——已 grep 全仓核实，故接线在 Register 内完成；`Module.Start/Stop` 保留为防御性 hook，若未来运行时开始调 Start，ActivateCore/ApplyNetworkPatches 均幂等） |
| 面板编辑器 | `SettingsRuntimeBueEditor`（Apply → `SettingsRuntime.Submit` 原子提交，Interlocked request id，ClientPreference scope；接受后回调 `RefreshSwitches`）+ `RoutingBueSettingsEditor`（按 FeatureId 路由，未命中 → BII 编辑器不动）；composition root 构造期组合（票面「panel editor 换成 Routing 组合」） |
| 面板接管卡 | `BueNativeManagementPanel.RenderNetworkTakeoverDetails`：网络 entry 详情 = 「接管状态：已由 BUE 接管」+「配置迁移：无独立配置可迁移（LMN 无配置文件）」+ 接管激活时「让我改回独立 LMN」按钮（仅走设置契约关 `network.enabled`，不删文件）；条目特判模式同 BII（`ClientUiCompositionRoot.NetworkManagementEntry`，快照走 adapter 的 SettingsRuntime） |
| 诊断 | `BUE-V2NET-001` 空迁移记录（不持久化，幂等空跑）/ `BUE-V2NET-002` 接管管道故障（镜像/委托/patch 失败）/ `BUE-V2NET-003` 生命周期（安装/卸载/隔离）；`BindProductionLog`：`result=failed` → `BueRuntimeLog.ErrorFriendly`，其余 → `BueRuntimeLog.Runtime`（DEV-16G 运行时静默策略：正常游玩不可见） |
| Bootstrap | 全局区注册网络模块（L56-59 区，双分支生效——Headless 也有网络模块）；client 分支 composition root 换 Routing 编辑器；`OnDestroy` 退出分支 `IsolateAndDetach` 交还网络 |

**交付边界**：Contracts 零变更（freeze diff 无 Contracts 文件）；帧格式不进 Contracts（T3 Q6/L69）；不迁移任何实际配置键、无迁移适配器接口（T6 Q4 YAGNI）；`SettingMigrationFailed=1309` 未触发（空迁移无失败路径）。

## 2. TDD 循环

| 阶段 | 证据 / 结果 |
|---|---|
| 红 r1 | `red-config-migration-r1.log`：2×CS0104——**测试代码缺陷**（新测试用了裸 `Action<>`，与 `SDG.Unturned.Action` 二义；旧测试全避裸写故未暴露）。测试侧修复（8 处限定 `System.Action`），非红测抓到的实现缺陷，如实登记 |
| 红 r2（真红） | `red-config-migration-r2.log` exit=1：**16×CS0103 + 3×CS0234 + 5×CS0246，全部为缺失 DEV-V2-06 类型**（NetworkModuleAdapter/HostNetworkTransportAdapter/SettingsRuntimeBueEditor/RoutingBueSettingsEditor）；日志同时显示 Plugin.dll/NoOpFixture 编译成功——实现缺席的红先于实现 |
| 流程发现（转绿间） | green-r1：新 Core 类型 `IReadOnlyList` 漏 using（测试工程编译错，补 `System.Collections.Generic`）；green-r2：`SDG.Unturned.NetMessages` 类为 **internal**，`typeof` 不可达（CS0122）——改 `AccessTools.TypeByName("SDG.Unturned.NetMessages")` 缓存解析（与独立 LMN 拦截同法），加类型缺失门 |
| 绿 | `green-config-migration-r3.log` Plugin.Tests 重建 exit=0、0 error 0 warning → `green-config-migration-anchor-r1.log`：`--bue-config-migration-red` **exit=0**（九段断言全过；日志 0 字节 = 该锚点成功路径静默，与 gates-summary 双证）→ `plugin-tests-full-r1.log`：全套 exit=0 `DEV-14/DEV-16B plugin runtime tests: PASS` |
| 03 测试适配 | `AssertBueNetworkRuntime` **零改动即绿**（handoff L71 预判「大概率绿」成立）：loopback 双运行时场景下 sender 字段与按源解析天然自洽。**合法测试更新登记**：`Network.Tests/Program.cs` 3 处 `Send` 签名适配点（loopback Send 调用、适配器构造委托 1→3 参、适配器 Send 调用），handoff L35 明示「同步更新」范围 |

## 3. 验证矩阵（`gates-summary-r1.log`）

| 项 | 结果 |
|---|---|
| Release 全解决方案重建（`build-sln-release-r1.log`） | exit 0；0 error / 0 warning 行（TreatWarningsAsErrors=true）→ **0/0** |
| `--bue-config-migration-red` | exit 0（`green-config-migration-anchor-r1.log` + gates-summary 双证） |
| 七项目测试运行器 | Contracts/Settings/Placement/Network/Release/ClientUi/Plugin 全 **exit=0** |
| UI/native token 扫描 | Core **18 文件** PASS（05 基线 16 + 2 新 Core 文件）、ClientUi **11 文件** PASS，零命中 |
| `git diff --check` | 退出码 0（autocrlf 信息性 LF 提示；工作副本 LF、仓库 blob LF，同 05 口径） |

## 4. 双轴独立审查（R1）

- **Standards 轴：R1 CLEAN**（独立上下文子代理）。10/10 硬约束逐条通过：禁词语义级规避成立（独立大小写不敏感全量 grep，本轮增量零命中）、Core.csproj 引用面未动、无 BepInIncompatibility、线格式路径零抛异常 + `DispatchSafely` 逐帧隔离链完整、C#10/net47.2 兼容（元组/`is bool` 模式合法、0 警告佐证）、静态可变状态仅诊断 seam + 仓库先例、测试惯例（锚点+全套双注册/每断言带消息/假体 Reset/finally 恢复 sink）、LF、Harmony 先例一致（成员存在门比先例更严）、门禁数字独立复核自洽（红 r2 计数逐条吻合，并独立复跑锚点 exit=0）。**可延后 smell（具名登记，不阻断）**：(a) `LmnTakeoverCoordinator.cs:11`（DEV-V2-04 已闭环文件，非本轮增量）注释含小写 "harmony" 语义命中——下次触及该文件时一并改写。INFO 记录：sink 赋值宜入 try；`DispatchData` 订阅者循环无逐 handler 隔离（本轮未触及，提名）；`UnpatchSelfSafe` 先置位再卸载（Harmony 去重自愈）；token 计数含 obj 生成文件；锚点静默成功；BUE2 帧升级无线格式版本协商（host 内部格式，双端同 assembly）；`IsLmnFrame` offset+size 理论溢出不可达。
- **Spec 轴：R1 CLEAN**（独立上下文子代理）。验收记分卡 **6/6 PASS**：红测先行（r2 真红 + 锚点绿）；原子持久化（Submit → temp+回读+Replace）；面板「无独立配置可迁移」行（代码级 + 状态级，headless 断言口径符合 handoff 拍板）；接管接线（Priority.First/零误报/可逆钮链路逐环核实，真实安装按拍板顺延 07）；可靠位 + 按源分发（三 runtime hub 拓扑全过）；构建 0/0 + 七运行器。三接缝决策全部落实；组件契约签名与 handoff 逐字一致；九段红测 **9/9** 对照无缺失；不做清单无越界；已知坑全部遵守（双 csproj 精确对齐、探针 false 零动作、`IConnectionSession.Send` 保持 NoSession）。**Findings 处置**：D1（freeze diff 第 800 行截断 ~7 行 context）→ 已核实工作副本完整，登记为证据卫生项；D2（待补项清单）→ 本报告 §5 具名；D3（状态行半角括号 vs 票引文全角）→ **登记为接受偏差**（一行文案，语义一致，不为标点触发重审轮）；INFO-1（类型初始化器对引擎类型 `AccessTools.TypeByName` 的静态反射与 handoff「零反射」字面偏差）→ 说明：零反射硬规则约束的是 **LMN 相关**反射（镜像/委托/patch 严格惰性 + 三元门控，探针 false 零执行）；引擎类型解析（NetMessages/CSteamID）不触碰 LMN 且仅解析 MemberInfo 不触发任何行为，零误报语义成立。INFO-2（token 计数含 obj）/INFO-3（锚点 0 字节）记录。
- **双轴 R1 CLEAN 汇合**：无 BLOCKER；Standards 1 条 + Spec 3 条 findings 全部为可延后 smell 或已当场处置的记录卫生项。本票**交付**。

## 5. 解锁与移交（待补项具名，handoff 拍板不阻塞闭环）

- **真机待补（归 DEV-V2-07 三环境实机验证）**：原生 Glazier 面板渲染与「让我改回独立 LMN」真机点击；真实 Harmony Priority.First 安装观测；真实独立 LMN `ModTransport`/`ModRouter` 反射（真 `CSteamID` 构造）；三环境（单人/U3DS/SteamP2PFriends）网络层验证。
- **具名延后**：`LmnTakeoverCoordinator.cs:11` 小写 "harmony" 注释修正（下次触及该文件时）；状态行括号全半角偏差（接受）；freeze diff 截断（权威内容以工作副本与提交为准）；锚点成功时打印显式 PASS 行（未来票改进）。
- **解锁**：DEV-V2-07（三环境实机验证，人工环节）→ DEV-V2-08（LIT/LIR/LHT 迁移验证）。顺序拍板：06→07→08。
- 提交清单（本票）：13 源码/工程文件（3 新 Core + 2 新 Plugin + 3 改 Plugin + 2 改 Core + 2 改测试 + 1 新测试适配）+ 票 + 本报告；构建/测试 log 与 freeze diff 不入库（随 04/05 先例，报告引用其 audit 路径）。
