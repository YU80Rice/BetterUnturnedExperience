# BUE 下一阶段(平台化)产品愿景 — phase-3 规划主源

> 来源:用户与 PM 讨论,2026-09-07 由用户提供存档(逐字保留,仅加本头注与目录索引)。
> 定位:V2 第二阶段(三插件官方纳入与平台首公里)实施期间产出的**下一阶段方向定稿草案**。
> 关联:`.scratch/bue-v2-phase2-official-adoption/map.md`(phase-2 地图,7/7)、`docs/agents/auto-rm-test-sop.md`、`audit/RELEASES.md`。

## 用户与 PM 的规划原文(存档)

真正想做的不是"再造一个 Forge",而是:

> 在 BepInEx 之上建立一套 Unturned 插件开发标准,让开发者只面对 BUE 的稳定接口,不再重复实现网络、生命周期、配置、线程、诊断和兼容逻辑。

这比"自研完整加载器"简单很多,也更适合 BUE。

### BUE 应该如何定位

```text
BepInEx
  负责:注入、发现 DLL、实例化宿主插件
        ↓
BUE Host
  负责:启动平台、提供公共能力
        ↓
BUE Platform Contracts
  负责:第三方插件依赖的稳定接口
        ↓
BUE Modules
  负责:具体功能
```

BepInEx 不需要被替代。

BUE 只需要把自己定义成:

> BepInEx 的平台层、规范层和基础设施层。

LMN 就是这个思路的一个成功例子:

```text
BepInEx 原始能力不足
→ LMN 封装网络通道
→ 插件遵守 LMN 规则
→ 插件不再各自实现网络层
```

BUE 要做的,是把这种模式扩展到更多领域。

### 最核心的设计:能力模块,而不是"大而全框架"

BUE 不应该暴露一个巨大接口,而应该提供几个"深模块"。

每个深模块都应当满足:

```text
开发者只学少量接口
BUE 内部隐藏大量复杂实现
所有插件获得同一种行为
```

建议先建设这些能力:

| BUE 能力模块 | 开发者得到什么 |
|---|---|
| `BueNetwork` | 命名频道、握手、能力协商、限流、主线程派发 |
| `BueLifecycle` | Start、Ready、Stop、异常隔离、清理 |
| `BueEvents` | 玩家、服务器、库存、场景、连接等标准事件 |
| `BueSettings` | 配置声明、迁移、权限、服务器权威、revision |
| `BueThreading` | 主线程调用、后台任务、队列上限、取消 |
| `BuePatching` | 统一 Harmony 注册、目标检查、失败隔离 |
| `BueDiagnostics` | 结构化日志、错误码、状态、诊断包 |
| `BueCompatibility` | Unturned/BepInEx/LMN/API 版本能力判断 |
| `BueUi` | 可选的设置页、按钮、通知、客户端 UI 适配 |

第一版不必实现所有内容。网络、生命周期、诊断、配置和线程是最有杠杆的五项。

### 开发者最终应该看到什么

理想情况下,LIT 不应直接接触 BepInEx、Harmony、LMN 的底层细节,而是写成类似:

```csharp
public sealed class InventoryTidyModule : IBueModule
{
    public void Configure(IBueModuleBuilder builder)
    {
        builder
            .UseNetwork("inventory-tidy", NetworkCapability.ServerAuthority)
            .UseSettings<InventoryTidySettings>()
            .Subscribe<InventoryOpened>(OnInventoryOpened)
            .Subscribe<ServerTick>(OnServerTick);
    }

    public void Start(IBueRuntime runtime)
    {
        // 只实现整理功能本身
    }

    public void Stop()
    {
        // BUE 负责统一释放订阅、网络和补丁
    }
}
```

开发者不需要自己处理:

- LMN 是否初始化;
- 网络 handler 是否重复注册;
- 消息是否来自正确连接;
- 是否在主线程;
- 模块停止时如何注销;
- 配置文件如何迁移;
- 客户端和 U3DS 是否有 UI;
- 一个模块失败是否拖垮全部插件;
- Harmony 是否重复打补丁;
- 网络消息是否超长或刷屏。

这就是平台的价值。

### BUE 的关键不是"替开发者写功能",而是"替开发者承担规则"

每个能力模块都应该包含四部分:

```text
1. Interface   开发者调用的稳定接口
2. Contract    参数、顺序、线程、错误、版本规则
3. Adapter     对接 BepInEx、Harmony、Unity、Unturned、LMN
4. Runtime     处理重试、限流、清理、隔离、诊断
```

例如 `BueNetwork`:

```text
开发者:Send(channel, message)

BUE 内部:检查频道→检查连接状态→检查能力协商→限制长度→限流→编码→投递
         →统计结果→处理断线→记录 DiagnosticId
```

开发者看到的是一个方法,BUE 内部承担的是完整网络规则。

### BUE 不应该一开始做什么

为避免再次变复杂,以下内容暂不纳入第一阶段:

- 自己替代 BepInEx 的 DLL 扫描器;
- 自己实现新的 Doorstop/Preloader;
- 自己设计完整 JAR/DLL 模块发现体系;
- 自己实现类加载器;
- 自己承诺热卸载;
- 自己重写 Steam P2P;
- 自己做服务器发现和身份认证;
- 自己复制 Forge 的物品/方块注册表;
- 自己建立全新资源包生态。

这些属于"加载器或游戏内容平台",不是当前真正需要解决的问题。

BUE 第一阶段完全可以接受:

```text
每个 BUE 功能仍然是一个 BepInEx DLL
但它必须依赖 BUE.Contracts / BUE.Runtime
并遵守 BUE 的能力规范
```

这已经足够形成平台。

### 推荐的 BUE 产品形态

1. **BUE Runtime**:部署为 `BepInEx/plugins/BetterUnturnedExperience.dll`——初始化 BUE、提供公共运行时、管理生命周期/事件/网络/设置/诊断/模块状态。
2. **BUE Contracts**:给第三方开发者引用 `BetterUnturnedExperience.Contracts.dll`——只放稳定接口和数据类型,不放 Unity UI 实现,不放内部状态。
3. **BUE SDK**:项目模板、示例模块、版本兼容说明、API 文档、测试夹具、manifest 或插件属性、本地调试工具。
4. **BUE 规范文档**:每个能力一份小规格——BUE-NET-001 网络通信规范、BUE-LIFE-001 生命周期规范、BUE-EVENT-001 事件规范、BUE-SET-001 配置规范、BUE-THREAD-001 线程规范、BUE-PATCH-001 补丁规范、BUE-DIAG-001 诊断规范。开发者只需要查自己用到的能力。

### 现有 LMN 应该如何演进

LMN 可以成为 BUE 的第一个标准能力,而不是另一个平行前置。建议演进关系:

```text
LMN 底层实现 → BUE Network Adapter → IBueNetwork → LIT/LIR/LHT 等模块
```

第三方模块只依赖 `BUE.Contracts`,而不是直接依赖 LMN 内部类(ModTransport/NamespacedTransport/Harmony 网络 patch)。这样未来即使 LMN 内部换实现,模块也不需要改。

需要明确:BUE Network 是应用层通信能力;它不是认证系统、不是服务器发现、不是 Steam P2P 替代、不是完整游戏状态同步框架、不负责替插件决定业务协议。BUE 负责通用通信规则,插件负责自己的业务消息。

### 最合理的建设顺序

- **阶段 1:把 LMN 正式收敛为 `BueNetwork`**。先只解决:命名频道、Hello/Ready、连接 generation、主线程投递、限流、最大消息长度、断线清理、发送结果、结构化诊断。
- **阶段 2:建设 `BueLifecycle`**。统一:模块启动、模块停止、依赖状态、异常隔离、资源注册、订阅清理、Harmony 清理、状态投影。
- **阶段 3:建设 `BueSettings` 和 `BueEvents`**。让插件不再自己发明:配置文件格式、配置迁移、revision、事件订阅、事件注销、事件异常处理。
- **阶段 4:建设 `Bue SDK`**。提供:最小插件模板、网络模块模板、纯客户端模块模板、U3DS 安全模块模板、带配置的模块模板、带 Harmony adapter 的模板。
- **阶段 5:迁移现有插件**。顺序:LHT → 网络广播示例;LIR → 网络请求/响应示例;LIT → 设置、库存事件、服务器权威示例。这些插件本身就是 BUE 的参考实现和验收样例。

### 最终目标表述

不要说"BUE 要成为 Forge/Fabric/NeoForge"。更准确的产品目标是:

> BUE 是 Unturned+BepInEx 的规范化插件平台,为插件提供稳定的网络、生命周期、事件、配置、线程、补丁、兼容性和诊断能力,使开发者专注于功能本身,而不重复实现底层基础设施。

一句话概括:

> BepInEx 负责"把插件启动起来",BUE 负责"让插件以统一、可维护、可兼容的方式运行"。

这条路线不需要替换 BepInEx,也不需要立即实现完整模块加载器;它更现实,也更符合从 LMN 得出的实际经验。

---

## 对账注记(2026-09-07,主工作树会话;供 phase-3 开图时对照)

见当次对话回答要点:阶段 1 清单与 V2 第二阶段交付面(DEV-V2-14~22)高度重叠但存在四项差值——限流入平台(契约边界反悔,需决策)、主线程投递契约化(需补注)、诊断包自动化(超出现有 BueRuntimeLog)、Contracts 拆分(与 T7 决议冲突,规划需按"四条件暂缓"改写);"LMN 底层实现→Adapter"演进图已被 phase-2 的自有运行时(BUE 帧)超越,规划该节需按已建成事实改写。详见当次对话。

## 对账注记 2(2026-09-07,用户最终裁定:两层模型——本注记覆盖前注中与本条冲突的表述)

用户纠正 PM 的"源码模块接入+聚合构建"过度修正,最终冻结口径为**两层模型**:

1. **官方功能(第一层)**:BII/LIT/LIR/LHT 源码模块,构建期聚合进唯一 `BetterUnturnedExperience.dll`——玩家安装列表里只有一个官方文件(不变)。
2. **生态功能(第二层,本注记的核心)**:第三方开发者交付**独立 DLL**,作为普通 BepInEx 插件安装进 plugins 目录,**由 BepInEx 原生自动发现**(BUE 无需自建模块加载器/扫描器);生态 DLL **声明 BUE 为前置**(BepInDependency 指向 BUE GUID),编译期引用 `BetterUnturnedExperience.dll`(`CopyLocal=false`,禁捆绑),运行期经公开桥(`BueRuntimeHost.Register`,SCR-GPT18-001)注册,消费平台服务(BueNetworkApi/IFeatureBootstrap.Network/功能事件/宿主时钟/设置/诊断/隔离)。
3. **BUE 身份 = 前置库 + 运行时平台**:发现由 BepInEx 原生承担(BUE 不自建);BUE 建的是注册桥(已建)+ 平台服务契约(14~19 已建/在建)+ 开发者契约文档(DEV-V2-23)。
4. **词汇对齐**:此口径与 CONTEXT 冻结词汇「生态功能模块」("由第三方作者独立发布、通过 BUE 公开契约接入运行时,但不随 BUE 官方发行版交付")完全一致,非新决策——是对愿景文档两处误述(①"以源码模块方式接入"仅对官方功能成立;②"BUE 自动扫描外部模块 DLL 暂不建设"应表述为"发现归 BepInEx 原生,BUE 建桥与服务")的最终纠正。
5. **对 DEV-V2-23 的影响**:开发者契约文档八节须按此两层模型写——生态路径(前置声明/引用面/CopyLocal/禁捆绑/注册桥/防双装 BUE-PLATFORM-001)为文档主体。

## 对账注记 3(2026-09-07,PM 对齐稿 = phase-3 愿景冻结基线,逐字存档)

经主工作树会话对仓库事实逐条核验(BueRuntimeHost.Register 公开桥在位 `BueRuntimeHost.cs:20`;NoOpFixture 即生态路径活样板——独立 GUID `io.github.yu80rice.bue.noop` + `[BepInDependency("io.github.yu80rice.betterunturnedexperience", HardDependency)]` + 经公开桥注册,phase-1 起在跑),PM 的以下表述冻结为 **phase-3 愿景基线**:

### 最终模型

```text
BepInEx
  └─ 原生发现并加载所有 BepInEx DLL
       ├─ BetterUnturnedExperience.dll
       │    ├─ BII
       │    ├─ LIT
       │    ├─ LIR
       │    └─ LHT
       │
       └─ 第三方生态 DLL
            ├─ 声明 BUE 为 BepInDependency
            ├─ 引用 BetterUnturnedExperience.dll
            ├─ CopyLocal=false
            ├─ 不捆绑 BUE
            └─ 通过 BueRuntimeHost.Register 接入 BUE
```

### 两层结构

**官方功能层**(BII/LIT/LIR/LHT):BUE 官方维护;源码按领域拆分;构建时聚合;玩家侧只有一个 `BetterUnturnedExperience.dll`;运行时是进程内 `IFeatureModule`;不作为四个独立 BepInEx 插件发布。

**生态功能层**(第三方):独立 DLL;独立作者、独立发布;由 BepInEx 原生扫描和实例化;`[BepInDependency("io.github.yu80rice.betterunturnedexperience")]` 声明前置;编译时引用 BUE 主 DLL;运行时经公开注册桥接入;使用 BUE 提供的网络、生命周期、事件、设置、Tick、诊断和隔离能力。

> BUE 不负责重新发现生态 DLL;BepInEx 已经负责发现。BUE 负责成为这些 DLL 所依赖的前置库与运行时平台。(与 Forge/NeoForge/Fabric Loader 的"模组前置与运行时平台"同构。)

### BUE 不需要做什么

- BUE 自建 DLL 扫描器;BUE 自建外部模块加载器;BUE 自己决定生态 DLL 的发现顺序;把第三方生态功能聚合进官方 DLL;要求第三方提交源码才能接入;玩家安装多个官方 BUE 功能 DLL。

(BepInEx 已完成:DLL 发现 → [BepInPlugin] 识别 → [BepInDependency] 排序 → 插件实例化。BUE 不重复实现这些底层职责。)

### BUE 真正要建设的五个稳定 seam

1. **前置契约**:BUE GUID/支持版本范围/程序集引用方式/CopyLocal=false/禁捆绑/可用性检测/不兼容安全退出。
2. **公开注册桥**(`BueRuntimeHost.Register`,SCR-GPT18-001 核心):注册阶段开放判断、FeatureId 校验、合同版本检查、防重复注册、创建 `IFeatureBootstrap`、注入 Network/Events/Settings/Clock 等平台服务、明确的接受或拒绝结果。
3. **平台服务**:BueNetworkApi、IFeatureBootstrap.Network、BUE Events、Host Clock、Settings Runtime、Main-thread dispatch、Diagnostics、Module lifecycle、Failure isolation——生态作者只写业务逻辑。
4. **运行时管理**:Registered → Accepted → Starting → Running → Disabled/Isolated → Stopped;生态模块失败不拖垮官方功能;BUE 核心失败才进 CoreSafeMode;停止时注销事件/网络/补丁;旧连接/Tick/状态不泄漏;统一诊断可见。
5. **开发者契约和防双装**(DEV-V2-23):写清生态 DLL 完整路径(创建独立 BepInEx 插件→声明前置→引用主 DLL→CopyLocal=false→禁捆绑→公开桥注册→获取平台服务→生命周期与隔离规则);生态 DLL 不得偷偷携带另一份 BUE.dll(`BUE-PLATFORM-001` 防双装检查)。

### BUE 的实际产品形态

```text
官方交付:BetterUnturnedExperience.dll = 官方功能 + 平台运行时 + 公共契约
生态交付:ThirdPartyFeature.dll = 独立 BepInEx 插件 + BUE 前置依赖
```

玩家可能最终安装:

```text
BepInEx/plugins/
├── BetterUnturnedExperience.dll
├── SomeInventoryExtension.dll
├── AnotherServerTool.dll
└── CustomUiFeature.dll
```

唯一的官方 BUE 文件仍然只有一个,生态 DLL 可以有很多个。

### 当前规划的正确下一步(不变)

第二阶段 20/21/22/24 无需改变;其中 **23 的文档必须明确区分:官方功能=源码模块构建期聚合;生态功能=独立 DLL 由 BepInEx 原生发现**。

第二阶段完成后,平台化核心 = 继续完善:公开注册桥、稳定 ABI、网络标准、生命周期标准、事件标准、设置标准、Tick/线程标准、Harmony 接入标准、诊断和隔离标准、生态开发者 SDK 与示例。

### 一句话最终定稿

> BUE 不造加载器;BepInEx 负责发现和加载生态 DLL,BUE 负责作为它们的前置库和运行时平台,把网络、生命周期、事件、设置、线程、诊断和隔离统一起来。
