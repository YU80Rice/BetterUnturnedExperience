# GPT-18 独立后端架构复核报告 R1

## 1. 复核范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/18-open-runtime-feature-framework.md`
- 规格：`.scratch/better-unturned-experience-architecture/spec-open-runtime-feature-framework.md`
- 对照基线：DEV-01～DEV-09、GPT-06、GPT-08、GPT-09、GPT-11、GPT-14、GPT-Feature-Definition-Pipeline-Spec、GPT-Contribution-Build-Release-Gates-Spec、GPT-Backend-Architecture-Spec
- 复核角色：GPT 后端开发与项目总维护者
- 复核类型：独立架构一致性审计；未修改生产代码，未执行运行验收

## 2. 最终裁定

**判定：REVISE（方向接受，规格暂不接受为实施基线）**

GPT-18 正确地把 BUE 定义为运行在 BepInEx 之上的大型前置框架插件，并正确保留了 LMN 作为可选网络传输 Adapter。公开 Contracts/SDK、独立功能 DLL、统一设置、功能作用域生命周期、显式注册、Headless 隔离和框架证据/功能证据分离均与项目目标一致。

但在以下问题闭合前，GPT-18 不能进入外部功能 DLL 的生产实现，也不能把“只安装 `BetterUnturnedExperience.dll` + 其他功能 DLL”视为已验收部署事实。

## 3. 已接受部分

| 审查项 | 判定 | 说明 |
| --- | --- | --- |
| BepInEx 与 BUE 分层 | ACCEPT | BepInEx 负责底层发现/装载，BUE 负责更高层运行时，未把 BUE 描述成 BepInEx 替代品。 |
| LMN 定位 | ACCEPT | LMN 是可选 Transport Adapter，不是 BUE 的身份、权限或库存权威。 |
| 显式注册而非目录扫描 | ACCEPT | 禁止 `Assembly.GetTypes()`、类名猜测、直接任意 DLL 装载和全局 `PatchAll()`，符合深模块与安全 Seam 原则。 |
| 统一 Settings Facet/Snapshot | ACCEPT | 第三方功能消费同一设置事实源，不自行创建平行设置系统。 |
| 功能局部隔离 | ACCEPT WITH LIMIT | 对已成功加载并完成注册的功能成立；不能覆盖 BepInEx/CLR 在注册前的程序集加载失败。 |
| 框架证据与功能证据分离 | ACCEPT | BUE 的框架兼容证据不得自动转化为第三方玩法或发布 PASS。 |
| 原生库存权威 | ACCEPT | Better Item Interaction 仍必须沿用 `sendDragItem → ReceiveDragItem`，不得创建平行库存权威。 |

## 4. 阻断项

### B-01：用户部署模型与当前程序集拓扑不一致

**事实**：当前 DEV-01～DEV-09 源码输出 `Contracts`、`Core`、`ClientUi`、`Transport`、`Plugin` 等多个程序集；GPT-18 却把玩家部署模型写成只安装 `BetterUnturnedExperience.dll` 和功能 DLL。

**风险**：若 `BetterUnturnedExperience.dll` 仍引用未随包安装的 `BetterUnturnedExperience.Core.dll` 或 `BetterUnturnedExperience.Contracts.dll`，BepInEx/CLR 可能在进入 BUE 注册 Seam 前就失败。此时 BUE 无法执行自己的故障隔离。

**必须修订**：在 Shared Contract Change Request 中冻结物理发布模型。推荐保持源码级模块化，但将 BUE 官方运行时、公开 Contracts 和官方功能编译为一个用户可安装的 BUE Host 程序集；第三方功能编译为独立 DLL，运行时只依赖该公开 BUE Host ABI。开发期可以保留多项目和内部程序集，不能把开发期拓扑冒充用户部署拓扑。

**验收证据**：干净 plugins 目录仅放 BUE Host 与一个 no-op 外部功能 DLL；无额外 Contracts/Core 依赖，BepInEx 加载成功，公开 ABI 解析成功。

### B-02：公开注册 Interface 尚未冻结

**事实**：规格仅描述“注册契约包含 definition artifact、API range、module factory、Settings Facet、依赖/能力和 UI token”，但没有确定注册者、返回值、错误族、调用线程、所有权和冻结后的行为。

**风险**：第三方作者会各自猜测注册方式，重新形成多个事实源；注册 Interface 过宽还可能把 BepInEx、Unity、Glazier、LMN 或任意 FeatureId 查询泄漏到共享契约。

**必须修订**：提交并批准 Shared Contract Change Request，至少冻结：注册入口的唯一调用者、不可变定义产物、运行时可执行工厂与静态事实的分离、BUE API/Contract 兼容范围、注册结果/稳定错误族、BUE dependency identity、注册线程与生命周期 generation、晚注册行为、客户端 UI 注册 token 语义。`module factory` 不能被序列化进 Definition Artifact，也不能成为第二身份事实源。

### B-03：注册时序与 BepInEx 生命周期未闭合

**事实**：GPT-18 要求“BUE host ready 后注册”，但没有定义 BepInEx `Awake/Start` 顺序、注册窗口、Runtime Catalog 冻结点和特性插件如何在宿主尚未 ready 时排队。

**风险**：插件加载顺序变化会造成非确定性；晚注册可能改写已运行的 Catalog，或者第三方入口在 BUE 尚未初始化时访问 null runtime。

**必须修订**：冻结深模块时序：`HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady`。只有 `RegistrationOpen` 接受注册；`CatalogFrozen` 后拒绝并要求重启。注册调用必须在有界、可审计的主线程/初始化队列上完成；Feature `Start` 不得在 BepInEx 入口 `Awake` 中直接执行，而由 BUE 在 Admission 后统一调用。

### B-04：ClientUi 卫星与 U3DS 部署边界不够严格

**事实**：规格允许可选 ClientUi satellite，但只写了 process gate；此前 RT-03 已明确 `Application.isBatchMode` 不是结构性类型隔离证明。

**风险**：BepInEx/CLR 可能在 U3DS 上先加载或解析带 Glazier/Sleek/Unity token 的卫星 DLL，导致 BUE 尚未接管就发生 TypeLoad/装载故障。

**必须修订**：明确客户端 UI 卫星是独立部署资产，U3DS deployment profile 默认不包含它；核心 DLL 不得引用客户端 UI 程序集；BUE 只能消费无 UI 的 registration token。若坚持同一物理 DLL 必须另行完成 IL 可达性、装载和 U3DS 实际运行证据，不能用运行时 `!batchmode` 单独替代。

### B-05：官方功能物理归属自相矛盾

**事实**：规格同时写了 `BetterUnturnedExperience.dll` 包含 official feature modules，又列出了 independently versioned Official feature DLLs。

**风险**：玩家无法判断“更好的物品交互”是 BUE 内置功能、独立官方 DLL 还是两者之一；重复注册会造成 FeatureId 冲突和证据归属不清。

**必须修订**：V1 明确选择一种模型。后端推荐：官方首发功能随 BUE Host 发布并拥有独立 FeatureId/definition，但不要求额外官方 DLL；第三方功能才采用独立 DLL。若未来官方功能拆成 DLL，必须走与第三方相同的注册和 CandidateBuild 规则，不能保留隐藏特权路径。

### B-06：CandidateBuild 绑定对象必须从单 DLL 扩展为 Load Set

**事实**：GPT-18 仍多处使用“每个功能 DLL 自己的 hash”描述，但框架运行结果取决于 BUE Host、功能 DLL 集合、ClientUi satellite、definition artifact 和 reference set 的组合。

**风险**：仅绑定某一个功能 DLL 或 BUE DLL 会允许拼接不同版本的运行证据，破坏 CandidateBuild 的防伪边界。

**必须修订**：定义不可变 `LoadSetIdentity`（名称可在 SCR 中确定），至少包含 BUE Host SHA-256、每个功能 DLL 的路径/身份/SHA-256、可选 UI satellite hash、DefinitionSet/Artifact digest、工具链/reference-set identity。所有 SP、P2P Host/Client、U3DS 证据必须绑定同一 LoadSetIdentity。

### B-07：BUE 故障隔离的适用范围必须收窄

**事实**：BUE 可以隔离成功进入其注册/生命周期 Seam 的功能，但无法捕获 BepInEx Chainloader、CLR 程序集绑定、静态初始化或类型解析阶段的失败。

**风险**：文档若把“功能失败局部隔离”写成涵盖所有 DLL 故障，会产生超出实现能力的安全承诺。

**必须修订**：把隔离承诺明确限定为“BepInEx 已成功加载、BUE 已接收注册并进入生命周期 Seam 后”。增加 SDK/CI preflight：依赖图、目标框架、引用程序集、UI token、BUE API range 和静态初始化风险在安装前拒绝；装载前失败记录为 `PreflightRejected`/Chainloader failure，不冒充 Runtime Isolated。

### B-08：SDK 的运行时依赖表述不精确

**事实**：规格写“SDK 类型 compile-time forwarded 到稳定 BUE API”，但没有说明第三方最终引用的 assembly identity。

**必须修订**：SDK 只提供编译期 reference、模板、分析器和测试；第三方运行时只依赖 BUE Host 的稳定 public ABI，不依赖 SDK DLL。若保留独立 Contracts reference assembly，必须明确它是 compile-time-only facade，不能成为玩家必须手动安装的隐式运行时依赖。

## 5. 后端建议的修订后产品分层

```text
BepInEx
  └── BetterUnturnedExperience.dll  (BUE Host prerequisite + official features)
        ├── public BUE ABI / Contracts namespace
        ├── Definition/Admission/Catalog runtime
        ├── Settings/Lifecycle/Capability/Transport runtime
        └── unified management projection

Third-party Feature DLL
  ├── independent BepInEx entry
  ├── BepInEx dependency on BUE Host
  ├── generated immutable definition artifact
  ├── explicit BUE registration call
  └── optional client-only satellite (not deployed to U3DS)
```

这保留了深模块原则：BepInEx 是底层 Adapter，BUE Host 是高层深模块，第三方功能只学习一个窄注册 Interface；BUE 内部仍可由多个源码项目组成，但用户部署和证据绑定必须面对明确的物理 Load Set。

## 6. 独立复核结论

- **产品定位**：ACCEPT。
- **LMN/BUE 关系**：ACCEPT。
- **当前 GPT-18 是否可直接关闭**：NO。
- **是否需要修改**：YES，至少修复 B-01～B-08，并完成 Shared Contract Change Request。
- **是否允许立即实现第三方 DLL**：NO。
- **下一步**：先修订 GPT-18 与中英文镜像，再创建注册/Catalog prototype 工单；prototype 通过后才进入外部功能生产接入。

## 7. 证据边界

本报告是静态架构复核，不证明 BUE Host、第三方功能 DLL、ClientUi satellite 在 SP、SteamP2PFriends 或 U3DS 的真实运行通过；也不改变 DEV-01～DEV-09 既有验收状态。

