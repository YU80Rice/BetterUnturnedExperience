# DEV-V2-16 实施结单报告——会话驱动组播与发送结果语义

日期：2026-09-06。工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-16-session-driven-multicast-send-results.md`（Blocked by DEV-V2-14，已 resolved）。规约：`docs/agents/output-review-loop.md`（Fresh-instance 规则版）红测先行 + 双轴独立审查 CLEAN 才交付。规格：`.scratch/bue-v2-phase2-official-adoption/spec.md`「平台：发送语义 = 会话驱动组播（T3 决策 4）」。

## 交付

- **契约③（发送结果 + `PartialFailure`）**：`NetworkSendResult` 新增 `PartialFailure = 204`（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`）；`SendToClients` 重写为会话驱动组播——对当前 established 快照逐一定向（每会话一帧，target=该会话 peer steam id），不再交底层单帧无目标广播；结果聚合冻结（快照空→`NoSession`；全送达→`Sent`；有目标全失败→`LocalTransportUnavailable`；混合→`PartialFailure`）；发送不持状态锁（锁内只做频道检查与快照，transport 调用全程锁外）。
- **契约④（`Sessions` 收窄）**：`IBueNetworkApi.Sessions` 只返回 established 会话快照；pending 会话仅运行时内部可见。
- **`SendToClient` 校验强化**：归属本运行时改为**对象身份**判据（`ReferenceEquals`，替换原 `sessions.ContainsKey(id)`——外来/伪造会话对象持活代际 id 亦拒）；established 校验（pending 会话不可寻址）；当前连接代际（SessionId 即连接代际，字典成员资格即权威——已丢弃/被替换对象同一查表被拒）。不新增 SteamId 重载。
- **`SendToServer` 连带对齐**（票面 Scope 未单列，为④+「发送不持锁」的直接推论）：NoSession 门从「任意会话存在」改为「established 快照非空」（pending 会话不再是可发送状态，否则与④矛盾）；发送保持**单帧** untargeted（单逻辑目标=服务器，不逐会话重复）；transport 调用移出状态锁。
- **载荷预检上提**：`IsEncapsulatable` 在目标循环前一次性校验（null/>16KB payload、频道 id >255 字节→`PayloadTooLarge`），超限载荷保留专用结果，不被聚合吞成逐目标传输失败。语义优先序冻结：频道未注册（`ChannelNotRegistered`）→ 目标集空（`NoSession`）→ 载荷（`PayloadTooLarge`）→ 逐目标发送。
- **每目标异常隔离**：transport 抛异常计一次目标失败（`Q11: hot paths never throw`），不向调用方传播。
- **SDK 登记**：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 契约版本演化节追加条目③④（契约维持 2.0——本票条目属 DEV-V2-14 启动的第二阶段契约 Major 包的逐票追加，无新增破坏性接口变更）。
- 生产 DLL 零行为外溢面：LIT（DEV-V2-15 已迁入）不消费 `Sessions`/组播（全仓 grep 实证），LHT 组播消费属 DEV-V2-22。

## 红绿链（证据留盘 `red-compile-build.log` / `stub-stage-build.log` / `red-runtime-transcript.log` / `red-fix-round-transcript.log` / `green-stage-build.log` / `fix-round-build.log` / `green-*.log`×7）

- **红0（编译红）**：`--bue-v2-send-semantics-red` 测试先行（引用未存在的 `NetworkSendResult.PartialFailure`）→ `error CS0117: "NetworkSendResult"未包含"PartialFailure"的定义`（`red-compile-build.log` 逐字）。
- **红1（运行时红，收集式 transcript）**：仅加枚举成员（stub stage）后，红测以收集模式一次捕获 **10 条红**（`red-runtime-transcript.log` 逐字），覆盖验收四组：established-only 快照（pending 不可见）×1；无会话→`NoSession`（组播/SendToServer pending-only 仍发送）×2；pending 会话 `SendToClient` 被拒 ×1；逐一定向（transport seam 实录 target=[0] 单帧）×1；聚合五值（`PartialFailure`×2、`LocalTransportUnavailable`×1）×3；外来伪造会话（持活 id 对象）被拒 ×1；**发送不持状态锁**（transport 内 Data 帧阻塞期间外线线程 `Sessions` 无法取锁）×1。
- **修复轮红（R1 后）**：R1-Spec BLOCKER 修复前，新断言「SendToClient 报告未注册频道先于 null 上下文检查」在 R1 代码上红（`red-fix-round-transcript.log`：`channel: SendToClient reports an unregistered channel before the null-context check`）；同组另两条频道优先断言（非 null 路径）在 R1 代码已绿，属既有优先序回归保持。
- **绿**：运行时重写后同旗 0 退出；修复轮后全套 7/7 测试 PASS（`green-BetterUnturnedExperience.{ClientUi,Contracts,Network,Placement,Plugin,Release,Settings}.Tests.log`），构建 0 警告 0 错误（`fix-round-build.log` 末「0 个警告」）。
- **红轮已绿项的归类（回应 R1-Spec GAP-1，R2-Spec 独立核验成立）**：「过期代际被拒」「Sent 聚合」「停用联动 NoSession」「超限载荷专用结果」「ChannelNotRegistered 优先」五项在红1轮已绿——前者的旧代码防伪路径即字典成员资格（出表即拒，无红路径可构造；其可变部分=身份防伪已由 stub 伪造场景红1覆盖），后四者为既有冻结语义/上游（DEV-V2-03/14）已红过的回归保持断言，非本票新语义。

## 与既有测试的相容性核查

- 全仓 `.Sessions` 用法穷举核对：所有既有断言均观察 established 时点（握手完成后/停用后），收窄不破任何既有断言。
- `SendToClients` 既有调用点（trio 测试）：聚合后仍 `Sent`、reliability 位逐帧转发、loopback 下双 peer 达——语义外 observable 行为不变；新增 target 实录断言钉死逐一定向。
- `SendToServer` 既有调用点（trio/裁剪 disabled/injection）：established 门下全部保持绿。

## 修复轮（R1 → 修复 → R2）

R1 双轴 NOT CLEAN，发现与修复：

- **[S-BLOCKING] 公开快照谓词锁外写入**：`Established` 置位原在 `MarkEstablished()`（锁外）执行——DEV-V2-14 时代非公开谓词无观察面，本票把它升格为 `Sessions`/发送门后构成「Connected 已触发但快照仍不可见」数据竞争。修复：置位移入 `HandleHello`/`HandleAck` 的 `lock (sync)` 内（`MarkEstablishedLocked()`/`EstablishWithContractLocked()`），锁外仅 `FireConnected()` 回调（repo dispatch convention 不变，DEV-V2-14 的 moduleActive 锁内重检语义保持）。
- **[SPEC-BLOCKER] SendToClient 频道门优先序**：null 上下文检查原在频道门之前，「未注册频道+null session」返回 `NoSession` 违反已登记的「频道未注册仍优先 `ChannelNotRegistered`」。修复：null 检查移入锁内频道门之后（统一三条发送路径的频道优先序），补红测断言（先红后绿）。
- **[S-SMELL] 同轮清理**：SendToClients 注释失实修正（锁取一次；快照后消失会话如实描述为「仍尝试发送，结果由 transport 逐目标裁定」）；`SendTargets` 删除唯一 `untargeted: false` 调用的 speculative 参数；established 过滤收敛至 `EstablishedSnapshot()` 单源（`Sessions` 协变返回 + `SendToServer` 复用，预容量 `sessions.Count`）；`ContractTypes.cs` 契约面 `Sessions`/`SendTo*` 补冻结语义注释与 SDK ③④ 对齐。

## 候选身份

`BetterUnturnedExperience.dll` SHA-256 `3e4259e36b2a3a0d48e4df4b6e82829160c2842bb386d0a1c42e3b39bdd1073f`（351232 字节，修复轮后两轮 `-t:Rebuild` 逐字节一致，`identity-rebuild1.txt`/`identity-rebuild2.txt`）。不继承 14/15 发布批准。

## 具名延期 / 边界声明

1. **自动握手本体**（重复 Hello 去重、Ack 按 peer+代际/nonce 匹配、超时重探退避）属 DEV-V2-17；本票的「当前连接代际」校验以字典成员资格为权威，重连替换语义随 17 落地时自然继承（SessionId 即代际、被替换对象必已出表）。
2. **`IConnectionSession.Send` 仍为 `NoSession` 桩**：按会话对象直发的便捷路径不在本票 Scope（契约面未动），保持既有桩语义。
3. **生产会话创建路径**：沿用 DEV-V2-14 具名延期（宿主启动路径属 DEV-V2-21/22），本票不改。
4. **`PartialFailure` 的版本归类**：枚举扩员为非破坏性变更，随 DEV-V2-14 启动的 2.0 Major 包登记条目③追加，不另升版本号；`SupportedContractMajor=2` 与各功能 `MinimumBueContract=(2,0)` 不变。
5. **同 peer 多 established 会话**（DEV-V2-14 测试拓扑中存在）：组播按会话逐一定向（每会话一帧），不去重 peer——会话才是寻址单元；去重属 DEV-V2-17 重连清理后的不可能状态。
6. **锁外探针覆盖面**（R2-Standards DEFERRABLE）：锁探针仅打 `SendToClients`——三条发送路径结构对称（锁内快照/校验、锁外 `SendFrame`），`SendToServer`/`SendToClient` 的独立探针列为后续加固项，不阻断。
7. **锁探针超时依赖**（R1/R2-Standards DEFERRABLE）：探针用 2s/5s 超时窗口，调度极差环境存在假红可能；与 DEV-V2-14 同模式，确定性握手（真实事件序）不在本票 Scope。
8. **`SendToServer` 物化整表**（R2-Standards DEFERRABLE）：established 存在性判断走预容量快照分配；热路径短路扫描为微优化，行为等价，不阻断。
9. **握手控制帧锁内发送**（R1/R2-Standards 越界注记）：`StartSession`/`HandleHello`/`HandleAck` 的 Hello/Ack/Reject 仍在锁内 `SendFrame`——自动握手语义与锁策略属 DEV-V2-17，本票 Scope 冻结的是功能模块 Data 发送路径。

## 审查链

- **R1**（2026-09-06，双轴 fresh 实例并行派发）：Standards **NOT CLEAN**（BLOCKING 1：Established 锁外写入数据竞争；SMELL 4：注释失实/speculative 参数/三处过滤重复/契约注释缺失；DEFERRABLE 3）+ Spec **NOT CLEAN**（BLOCKER 1：SendToClient 频道门先于 null 检查违反登记优先序；GAP 1：过期代际先红证据归类异议）。报告 `R1-standards.md` / `R1-spec.md`。
- **修复轮**（2026-09-06）：上述全部发现实施修复（见「修复轮」节），新增修复轮红证据 1 条（`red-fix-round-transcript.log`），修复后全套 7/7 PASS 0 警告（`fix-round-build.log` + `green-*.log`×7），候选身份刷新。
- **R2**（2026-09-06，双轴全新实例复审）：Standards **CLEAN**（BLOCKING 0；DEFERRABLE 3 项并入具名延期 6/7/8；越界注记=握手帧锁内发送属 DEV-V2-17）+ Spec **BLOCKED**（唯一 BLOCKER=审查链与判词报告归档未完成，即本节与 `R1/R2-*.md` 落盘——属 output-review-loop 第 4 步收尾记账，R2 返回后由实施者执行；Scope/验收①②③内容全部核验通过，R1 GAP-1 经独立核验成立为回归保持归类）。报告 `R2-standards.md` / `R2-spec.md`。
- **记账收尾**（2026-09-06）：本节与四份判词报告落盘，即 R2-Spec BLOCKER 所指事项完成；生产代码零改动，Standards R2 CLEAN 对同一生产增量（`round2-increment.diff`，候选 `3e4259e3…073f`）继续有效。
- **R3**（2026-09-06，全新实例，仅 Spec 轴）：两层复核——第一层 R2-Spec BLOCKER（记账）逐项核实**解除**（审查链/R1R2 四报告/票面同步全部在盘且一致）；第二层 Scope 内终审**零 GAP / 零越界 / 零错实现**。唯一 BLOCKER=R3 判词自身落盘（自指记账：其判词内容即要求本判词写入结单）。报告 `R3-spec.md`。
- **实施者裁定**（2026-09-06，附于 R3-spec.md 后）：R3 唯一 BLOCKER 为自指记账事项——判词已返回，其落盘是规约第 4 步的机械动作，本节即该动作的执行；若以此循环派轮将无穷递归，非规约意图。按 DEV-V2-14 先例（实施者裁定+全新实例核验），派 R4（fresh，仅 Spec 轴）对裁定与全链做最终核验。
- **R4**（2026-09-06，全新实例，仅 Spec 轴，最终核验轮）：**CLEAN**——三层核验全过：①实施者裁定成立（R3 唯一 BLOCKER 确为自指记账且落盘如实，规约第 4 步以「R3 报告归档+结单链补录」方式满足）；②全链记录真实（五份判词与结单叙述逐项对应，无选择性转录）；③Scope 独立终审零 BLOCKER/零 GAP/零越界/零错实现。报告 `R4-spec.md`。

## 最终判定

fresh-instance 链 **R1 → 修复 → R2（S:CLEAN / Spec:BLOCKED 记账）→ 记账收尾 → R3（Spec:BLOCKED 自指记账，实质两层全过）→ 裁定 → R4（Spec:CLEAN）** 闭环，双轴最终 CLEAN（Standards=R2，Spec=R4）。规约依据 `docs/agents/output-review-loop.md`（Fresh-instance 规则版）；裁定+核验结构沿 DEV-V2-14 先例（R2'→裁定→R2''）。候选身份 `3e4259e3…073f` 自 R2 起生产代码零改动（R3/R4 为文档轮），身份不变。
