# V3-T7 BueSettings 与配置权威/迁移范围

- **Ticket**: V3-T7
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-10 七项裁决定音，照推荐方案）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

阶梯三级（与 BueEvents 并级；Events 已由 V3-T3 覆盖，本票专注 Settings）。风险级 **Worth exploring**。按六问结构：

1. **配置权威**：每功能私有权威 + 面板统一编辑入口（CONTEXT「统一功能设置面板」）对生态功能如何表述——生态 DLL 的设置文件落点（`Unturned\BetterUnturnedExperience\*.bue-settings` 先例）、校验/持久化契约；
2. **迁移/revision**：愿景的配置迁移、revision、服务器权威——本阶段做到哪层？服务器权威对 U3DS / P2P 两环境的语义差值？
3. **实施深度**；
4. **留到后续**；
5. **契约影响**：Settings Runtime 进公开契约的版本代价；
6. **同权影响**：官方功能设置与生态设置在面板/持久化/迁移上是否同一条规则。

事实输入：V3-R1。主源：CONTEXT「统一功能设置面板」、愿景阶段 3 节、现有 BUE 设置持久化实现。

## Answer

2026-09-10 用户+PM 七项裁决全部定音，照推荐方案实施。deepening 目标=把「官方私有 SettingsRuntime+面板硬编码路由+生态 Settings 恒 null」深化为按 FeatureId 作用域隔离、功能拥有权威、BUE 提供稳定 view、面板只做 editor adapter 的深设置 module。

### 1. 生态设置权威模型 = 共用 SettingsRuntime 语义

生态与官方同规则：`FeatureId + SettingRevisionScope + schemaVersion → SettingsRuntime`。平台负责：快照读取、类型与范围校验、revision 单调推进、持久化、损坏文件安全默认、原子提交、作用域隔离、停止与代际失效。生态不得：自建 BUE 配置格式、绕过 SettingsRuntime 直接写文件、把面板状态当第二事实源、用静态字段替代持久化权威、共享其他功能设置文件。默认路径 `Unturned\BetterUnturnedExperience\<FeatureId>.bue-settings` 仅为当前 persistence adapter 实现约定——生态契约依赖 `Settings` interface 与 GetSnapshot/TryGet/Submit 语义，**不硬编码绝对路径**。

### 2. 接入缝分层（本票核心接线）

①`SettingsRuntime` 类本体与 `ISettingsPersistence` 保持内部自由不列契约（文件布局/锁/持久化细节可演进，外部不得直接构造或持有 runtime）；②**`IFeatureBootstrap.Settings` 正式接线**（生态模块注册→宿主 bootstrap→注入 IScopedFeatureSettings→读/提交自己的设置）——view 只能访问当前功能作用域，不能访问其他 FeatureId、改 schemaVersion、改 revision、替换 persistence adapter、伪造 ServerAuthority 快照；③**面板路由同权收编**：不再以官方 FeatureId 硬编码清单为唯一来源——冻结注册目录→读取设置 facet/editor registration→按 FeatureId 动态路由→调用该功能自己的 Settings view/editor。面板只负责统一入口/转发编辑/显示提交结果与 revision/显示可用性；功能仍拥有 Schema/校验/权威/持久化/运行时读取/迁移。**面板=编辑 adapter，不是第二设置权威**；未提供设置的功能不伪造设置页；面板不得直接访问功能私有字段。

### 3. 服务器权威 = 本阶段只冻结语义边界

权威端：U3DS Dedicated Server 与 SteamP2PFriends Listen Host **语义相同**（权威端写 ServerAuthority scope，客户端连接期间消费会话覆盖快照）。客户端：保留自己的 ClientPreference；连接期间读服务器权威快照作会话覆盖；断线/换服/ConnectionGeneration 改变即清除。客户端不得：把 ServerAuthority 回写持久化、把服务器快照当本地偏好、断线后继续用旧会话快照、伪造服务器权威提交。**本票不做**：服务器→客户端同步协议、设置网络消息、改 BueNetwork 协议、远程管理、把 P2P 与 U3DS 拆成两套语义。未来跨机同步必须在 Network 领域另立票（协议/权限/revision/失败语义）。

### 4. 迁移与 revision = 功能自理，平台供稳定通道

平台提供 schemaVersion+Load+TryCommit+revision；功能负责：识别旧 Schema/读旧值/映射新 Schema/校验迁移结果/以新 Schema 写回/失败回退安全默认/记录迁移诊断。平台不建：统一迁移 DSL、统一字段映射器、跨功能迁移、自动推断语义、替功能解释旧字段。revision 现行语义照登：每 FeatureId 与每 scope 独立、成功提交单调推进、失败不推进、`ExpectedRevision` 防旧 UI 覆盖新值、服务器会话覆盖不污染客户端持久化 revision。

### 5. 实施深度 = 现状加固五件套

①bootstrap.Settings 接线（红测：GetSnapshot/TryGet/Submit 成功、校验失败、ExpectedRevision 过期、损坏加载安全默认、revision 推进）；②面板路由同权收编（官方/生态设置同样可见可编辑）；③NoOp probe 扩链：Register→取 Settings view→读 ClientPreference→提交合法变更→观察 revision 推进→提交非法变更→观察显式拒绝→停止→视图失效或拒绝后续写入（未提供 ServerAuthority 写入口则只验证已承诺 scope，不伪造）；④SDK 登记（读取/提交语义、双 scope、会话权威覆盖、断线清除、revision、schemaVersion、功能自理迁移、面板非第二事实源、生态与官方共用规则）；⑤官方 dogfooding：至少一个官方功能改经注入 view 读取和提交（面板编辑→Settings view→SettingsRuntime→运行时读新快照全链，不能只证明类型存在或面板能显示）。

### 6. 留到后续

服务器→客户端同步协议、同步权限/签名/冲突解决、统一迁移框架、导入导出、加密、远程管理、配置备份恢复工具、版本自动升级助手、网络健康阈值可配置入口、更复杂 facet 类型系统。**SettingsRuntime=本地/权威设置事实源，BueNetwork=未来可能的同步传输——职责不在本票混合**。

### 7. 契约影响与同权

Minor 加性：①Settings 恒 null→可用；②IScopedFeatureSettings 快照/提交语义；③ClientPreference/ServerAuthority scope；④revision 与 ExpectedRevision；⑤schemaVersion 与功能自理迁移边界；⑥会话权威覆盖与断线清除；⑦面板统一入口但非第二事实源。与 T2..T5 同批共用 **2.1**，晚则顺延 **2.2**。SettingsRuntime/ISettingsPersistence/面板 editor 类本体不自动进公开契约。同权四条：①同一 IScopedFeatureSettings seam；②面板按注册目录路由、生态与官方同样可见可编辑；③NoOp probe 证明生态可读/提交/观察拒绝；④官方 dogfooding 证明官方功能也经注入 view 消费。补充：官方不得绕过 Settings view 私改字段冒充平台消费；生态不得直接访问官方私有 SettingsRuntime；两者共享校验/revision/持久化/失败语义。

### 本票不做

跨机同步协议、统一迁移框架、设置导入导出、设置加密、远程管理 UI、平台设置健康阈值配置化、SettingsRuntime 类本体冻结为 public contract。

## Comments
