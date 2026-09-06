# DEV-V2-14 收尾报告：平台——入站订阅升契约与 Network 注入（契约 Major 升级启动）

日期：2026-09-06。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-14-inbound-subscribe-network-injection.md`（V2 第二阶段先行票，无阻塞）。规约：`docs/agents/output-review-loop.md` 红测先行 + 双轴独立审查 CLEAN 才交付。

## 交付内容

1. **契约①**：`IBueNetworkApi.Subscribe(FeatureId, ChannelDirection, Action<IConnectionSession, byte[]>)` + `enum ChannelDirection : byte { FromClients = 0, FromServer = 1 }` 入 Contracts 冻结面（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`）。
2. **契约②**：`IFeatureBootstrap.Network`（`BueNetwork.IBueNetworkApi` 类型，不泄漏 Host/LMN/Unity 类型）。
3. **组合根**：`src/BetterUnturnedExperience.Core/Registration/FeatureBootstrap.cs`（新文件）——纯组合记录，唯一冻结不变量 = Network fail-fast 非空、同一实例贯穿模块生命周期。
4. **运行时**：`BueNetworkRuntime` 按方向双订阅表（`fromClientsHandlers`/`fromServerHandlers`，订阅记录逐句柄）；方向=会话握手起源（发起方入站 FromServer、应答方入站 FromClients）；派发门=接收侧频道表（handler 表与频道注册解耦）；handler 锁外执行；单 handler 异常隔离；空 handler/非法方向 fail-fast；`SetModuleActive` 停用语义（频道/订阅表不动、会话清零、接收解挂、StartSession 显式 null）。
5. **契约 Major 升级**：注册门槛 `SupportedContractMajor` 1→2；全部生产 `MinimumBueContract` (1,0)→(2,0)（OfficialFeatureRegistration ×2、NetworkModuleFeatureRegistration ×2、NoOpFixture ×1）；Contracts.Tests 桩同步对齐。
6. **冻结面变更登记**：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 新增「契约版本演化」节——当前契约 2.0，条目①②完整登记（后续票追加③④⑤）。

## 红绿链（red-first 证据留盘 `red-compile-build.log` / `stub-stage-build.log` / `fix-round-build.log`）

- **红0（编译红）**：测试先行（两锚点 + 契约形状断言引用未存在的契约面）→ CS0234/CS0246/CS1061/CS0103（缺 `ChannelDirection`/`FeatureBootstrap`/`SetModuleActive`/三参 Subscribe）。
- **红1（运行时红）**：契约面+抛桩落地编译绿后，两锚点运行时红——`--bue-v2-subscribe-red` 与 `--bue-v2-network-injection-red` 均 `System.NotSupportedException: DEV-V2-14: directional subscribe not implemented yet`（逐字 transcript）：
  ```
  DEV-14 official registration parity tests: FAIL
  System.NotSupportedException: DEV-V2-14: directional subscribe not implemented yet
     在 BetterUnturnedExperience.Core.Network.BueNetworkRuntime.Subscribe(FeatureId channel, ChannelDirection direction, Action`2 handler)
     在 BetterUnturnedExperience.Plugin.Tests.Program.AssertBueV2DirectionalSubscribe()
     在 BetterUnturnedExperience.Plugin.Tests.Program.Main()
  ```
  （injection 锚点同型堆栈。）
- **绿**：真实现落地——两锚点 exit 0；锚点折入默认套件（`AssertBueV2DirectionalSubscribe()`/`AssertBueV2NetworkInjection()`）；全套 7/7 PASS、0 警告 0 错误。
- **旧调用点对齐**：DEV-V2-03 的 `AssertBueNetworkRuntime` 补接收侧频道注册（派发门=接收侧频道表的新冻结语义使旧「仅发送方注册」失效）；三处 2 参 Subscribe 调用点对齐方向（b/hub=FromClients，peers=FromServer）；无 (1,0)/2 参残留（全仓 grep 复核）。

## 锚点覆盖面（红测面 → 验收条件对照）

- 订阅方向语义：发起/应答两方向各只派发对应 handler，反向订阅零派发。
- 独立幂等句柄：同一委托双订阅各派发一次、逐句柄 Dispose、重复 Dispose 安全、Dispose 后零派发。
- fail-fast 参数：null handler → `ArgumentNullException`；`(ChannelDirection)42` → `ArgumentOutOfRangeException`。
- 未注册频道合法：先订阅后注册两阶段（未注册零派发、注册后派发）。
- 停用语义：订阅/注册/注销合法、`Sessions` 空快照、三种发送显式 `NoSession`、`StartSession` 显式 null、入站零、重启用后已挂订阅免重挂（客户端再握手后派发恢复）。
- Network 注入：fail-fast 非空、同一实例贯穿停用/重启、未就绪显式结果、停止后句柄安全重复释放 + 频道注册经同一 API 可释放。
- 契约断言工程（Contracts.Tests）：`ChannelDirection` byte 枚举冻结值、Subscribe 三参签名、`IFeatureBootstrap.Network` 属性类型、门槛 (2,0) 收/(3,0) 拒。

## 双轴独立审查链（standards-reviewer / Spec-Reviewer 专属智能体）

| 轮 | Standards | Spec | 处置 |
| --- | --- | --- | --- |
| R1 | NOT CLEAN——**BLOCKING 1**：`HandleHello`/`HandleAck` 重入锁不重检 `moduleActive`，停用窗口可建会话/发 Ack/触发 Connected（违反停用「无会话、无生命周期」）；SMELL 4 项（空 List 残留、FeatureBootstrap 9 参透传、抛异常 handler 留表、锁探针同线程不可证伪） | NOT CLEAN——GAP 3 项：①停止后自动失效未实现（宿主生命周期 seam 不存在）；②Network 未接入真实启动路径；③红测证据未归档 | 锁内重检×3；外线线程锁探针替换同线程重入探针；停止语义注释对齐冻结析取原文 + UnregisterChannel 断言；证据归档 |
| R2 | ~~CLEAN~~ | ~~CLEAN~~ | **整轮作废**（主会话裁定：R2 以 SendMessage 续用 R1 审查实例，违反 output-review-loop Fresh-instance 规则 `425c2aa`——「继承上下文的轮次不计入 CLEAN 链」） |
| R2'（全新实例） | **CLEAN**（4×DEFERRABLE 与既有具名延期一致；停用窗口闭合、外线锁探针可证伪、全量扫描无新违规） | BLOCKED——3×BLOCKER（魔数 BUE2→BUE1 未改 / `Sessions` 未收窄 established / Ack 仍按「第一个未建立会话」）+1 DEFERRABLE+1 INFO | 三项 BLOCKER 经对照工单拆分裁定为**越界发现**：魔数+清扫=DEV-V2-18 Scope 第 19 行、Sessions 收窄=DEV-V2-16 Scope（登记条目③）、Ack 匹配=DEV-V2-17 Scope——均为下游票票面义务；「结单称 established-only」引证经全仓 grep 证伪。按修复轮流程执行审查合同修复（scope brief 校正），代码零变更（增量保持 48937d2）。报告归档 `R2'-standards.md`/`R2'-spec.md`（含裁定附注） |
| R2''（全新实例，仅 Spec 轴） | —（Standards R2' CLEAN 对同一 diff 继续有效，无代码变更） | **CLEAN**（第一层：三项越界裁定逐行核实成立；第二层：本票 Scope 内重审无 BLOCKER，仅余两具名延期） | 环路闭合（fresh-instance 链：R1 → 修复 → R2' → 裁定 → R2''） |

## 具名延期（不阻塞，均登记）

1. **FeatureBootstrap 非冻结成员透传**（Standards R1-SMELL c）：9 参构造中 events/ownedEvents/logger/dependencies/lifetime 等透传，宿主运行时不虚设表面；生产 Start 路径属 DEV-V2-21/22。
2. **抛异常 handler 留在订阅表**（R1-SMELL e）：冻结语义未定义逐出；功能级故障隔离属宿主/功能事件票。
3. **空频道键 List 残留**（R1-SMELL a）：键数以频道数为上界，无增长路径。
4. **功能 Stop 自动失效订阅/频道**（Spec R1-GAP①）：冻结原文为析取「失效**或**可安全重复释放」，本票取后者并有红测；宿主侧自动失效绑 IFeatureModule 启动路径（当前无任何票拥有该路径）。
5. **停用×派发在飞窗口**（R1 复核项）：DispatchData 锁外执行段与并发停用之间的在飞帧可能仍派发一次（入站帧在停用前已被接收匹配）——与「锁外回调」同构的尽力语义，冻结语义未要求撤回在飞派发。
6. **红测不可构造的竞态窗**（Standards R1-BLOCKING 的红测缝）：OnReceive 检查与 HandleHello 重入锁之间的窗口无外部 seam 可确定性到达；修复以三处锁内重检落地，红态由独立审查者的代码路径论证充当证据（output-review-loop「seam gap 命名记录」条款）。

## 身份（双 CLEAN 后授予）

- 产物：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- SHA-256：`8d044c7a6889a9cab939130889313af6980a6971b470e4c1a4d85a442c7a2f7b`（278016 字节）
- 确定性：同源两轮 `-t:Rebuild` 逐字节一致（`identity-rebuild1.txt`/`identity-rebuild2.txt`/`identity-sha256.txt` 留盘）
- 绿测证据：`green-BetterUnturnedExperience.*.log`×7（全 PASS，2026-09-06）
- 边界：不继承 12/13 发布批准；候选采纳由用户决定。
