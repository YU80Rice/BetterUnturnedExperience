# T7：开发者契约与防双装设计

Type: grilling
Status: resolved（2026-09-06,五问拍板,主会话 grilling 闭环;本票闭环=地图 7/7 走完）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 01（T1 已 resolved,本票处于前沿）

## Question

把「Forge-like 身份承诺」落成可验收的平台契约（用户拍板 Q9=是）：

1. 契约内容：第三方引用 BUE 前置**不受 BUE DLL 文件名影响**——按 T1 实证结论定承诺边界与措辞（含编译期 HintPath 的开发者指引）;
2. 防双装检测：第三方误拷 BUE DLL 进自己发布目录 → 同 GUID 双实例时，BUE 侧给结构化诊断（诊断 id、决策点、可恢复行为）而非静默异常;机制挂在哪里（Chainloader 时机?宿主自检?）;
3. 契约的验收面：写进开发者文档的哪一层、红测锚怎么钉。

产出：契约 + 防双装设计决策，可交 `/to-spec`。

## Answer（2026-09-06,五问全决;用户逐条定稿）

### 决策 1(Q1)——契约措辞 = 三段式(承诺/不承诺/编译期指引)

**承诺**:①插件 GUID `io.github.yu80rice.betterunturnedexperience` 冻结;②程序集名 `BetterUnturnedExperience` 冻结(**未来若要改 AssemblyName,不承诺兼容,必须作为破坏性公告和迁移事件处理**——不做"既冻结又不承诺"的歧义表述);③公开契约(`BueNetworkApi`/`IFeatureBootstrap`/功能事件/宿主时钟)按契约版本演化,冻结面破坏性变更必升版本并登记。
**文件名精确措辞**:在受支持的 BepInEx 加载方式下,DLL 文件名不是 BUE 的稳定契约身份;第三方绑定依赖插件 GUID 与程序集身份。文件路径、加载目录和 BepInEx 发现规则仍属部署前提。
**不承诺**:非 BepInEx 加载方式;未验证的 Preloader `AssemblyResolve` 等内部行为;任意修改 AssemblyName 后仍兼容;任意重命名/复制/阴影加载后的行为;把单次 Mono/.NET 加载实验当永久 ABI 保证。
**编译期指引**:引用官方 `BetterUnturnedExperience.dll`;`CopyLocal=false`;**不把 BUE DLL 捆进第三方发布包**;契约版本与目标 BUE 版本对齐——核心 = 第三方只引用公开 interface,不复制 implementation。

### 决策 2(Q2)——SDK 引用面 = 直接引用主 DLL + `CopyLocal=false`

当前拆独立 SDK 程序集收益不足以抵消版本/分发/绑定复杂度。立项条件(出现才做):第三方需脱离完整 BUE DLL 编译 / 多仓库需稳定纯契约包 / runtime 与 SDK 发布节奏须独立 / 需公开桥接 adapter 而不暴露主程序集。此前独立 SDK 只是提前多一个 adapter 和发布事实源。

### 决策 3(Q3)——防双装 = 运行时自检诊断(**不宣传为完整防重复加载系统**)

```text
Awake → 注入程序集列表 → 检查同 AssemblyName → 结构化诊断
diagnosticId = BUE-PLATFORM-001
```

诊断字段至少含:检测到的 AssemblyName、程序集 Location、当前 BUE 路径、冲突副本路径、"移除非官方副本"建议。
**边界**:自检只处理已进入 AppDomain 的程序集;不替代 BepInEx 的 GUID 去重,不保证捕获未加载/加载失败/隔离上下文中的副本。分工:同 GUID 双装 = BepInEx 原生 Warning + 文档 FAQ;同 AssemblyName 不同 GUID = BUE 自检补强;**不自动删除用户文件**。locality:诊断集中平台 module,文件处置留给用户。

### 决策 4(Q4)——验收面

- **红测**(程序集列表注入 seam,不触文件系统):无冲突 / 同 AssemblyName 冲突 / 不同 AssemblyName / 空路径 / 重复条目 / 诊断 id 与关键字段。
- **实机清单**(T1 五项,入实施票,不阻塞本票):改名实机对照;Mono `LoadFile` 二次探测;同版本程序集最终谁保留;不同 GUID + 同 AssemblyName;Preloader `AssemblyResolve`。其中"不同 GUID + 同 AssemblyName"须**红测 + 实机双证**——红测不能替代实机结论。

### 决策 5(Q5)——文档落点 = 本票冻结大纲,正文随 /to-spec 实施票

`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 扩写,不新建 docs/developers(避免双事实源)。大纲:1 适用范围 / 2 承诺 / 3 不承诺 / 4 编译期引用指引 / 5 GUID·AssemblyName·DLL 文件名 FAQ / 6 双装诊断 BUE-PLATFORM-001 / 7 契约版本演化 / 8 实机验证清单。

### 用户期待登记(契约两锚推论)

> 未来版本更新可变的是**文件名**;程序集名不动——动了前置引用就断。

可交 `/to-spec`。
