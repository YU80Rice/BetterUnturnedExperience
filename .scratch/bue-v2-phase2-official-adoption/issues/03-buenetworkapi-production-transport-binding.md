# T3：BueNetworkApi 生产传输绑定设计

Type: grilling
Status: resolved（2026-09-06,五问拍板,主会话 grilling 闭环）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 01（T1 已 resolved,本票处于前沿）

## Question

BueNetworkApi（BUE 帧,线帧魔数 BUE1——见 Answer 决策 6）至今**无生产传输绑定、无真实消费者**（08 R5 勘误实锤）。本票决定绑定设计：

0. **契约面缺口（T2 盘点实锤）**：公开 `IBueNetworkApi` 无入站订阅（`Subscribe` 仅 Host 内部）——第三方消费者如何收帧？入站面升契约（Subscribe/事件回调/其它）与生产绑定一并决策,这是三插件重写的前置。
1. 两方向收发：client→server（`Provider.clientTransport`/`IClientTransport`，T1 research L110/L128）与 server→client（`ITransportConnection.Send`，T1 L104）；发送侧按 target steamId 寻径（FindClientTransport 先例）;
2. BUE2 帧进不进 `NetMessages.ReceiveMessageFromClient/Server` 前缀决策点?与接管决策核（12 的 live/inert 两态契约、`ShouldConsumeInbound`）的关系——LMN 缺席（inert）时 BUE2 帧是否也拦（BUE 独立可用）还是放行（零误报优先）;
3. 可靠位映射（ENetReliability 两值）与 16KB 上限在生产路径的落实;
4. 与 V1 兼容层/LMN2 委托的互斥与共存（同一前缀内三类帧的决策矩阵）。

产出：绑定设计决策（含决策锚与红测面），成为后续实施票与 T4-T6 重写的地基。

## Answer（2026-09-06,五问全决;用户逐条定稿并补充冻结语义）

### 决策 1(Q1)——入站订阅契约形态 = (a) 单方法+方向枚举

```csharp
IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler);
enum ChannelDirection : byte { FromClients, FromServer }
```

- 方向 = **入站帧来源**(FromClients: client→server;FromServer: server→client),不是本地角色。
- 冻结语义:同频道可分别订阅两方向;每次订阅返回独立幂等的 `IDisposable`,Dispose 只注销自己的委托;handler 锁外执行;单 handler 异常不得破坏其他订阅;未注册频道/非法方向有稳定失败语义;**运行时内部必须同步改为有方向的双 handler 表**——否则只是改签名没改 seam。
- 失败语义细化(agent 按"稳定失败"要求落档):空 handler/非法 direction → 参数异常 fail-fast(开发者错误);订阅未注册频道 → 合法(handler 表与频道注册解耦,帧仅在频道注册且流量到达后派发)——与 LMN 双表独立性同构。

### 决策 2(Q2)——功能获取网络入口 = `IFeatureBootstrap.Network { get; }`

- 官方与生态功能同一公开契约;测试注入假 `IBueNetworkAdapter`;生命周期绑 `IFeatureBootstrap`/`IFeatureLifetime`;不泄漏 Host/LMN/Unity。
- 冻结要求:`Network` 永非 null;网络模块未就绪时方法返回**显式结果**(绝不 null/静默异常);功能停止后订阅句柄与频道注册失效或可安全重复释放;**属 Contracts 冻结面变更,升契约版本**。

### 决策 3(Q3)——BUE 帧消费 seam 与 LMN 接管 seam **拆分**

- 旧规则改写:**探针 false → 零 LMN 相关 patch/反射/镜像**(不再说"零 patch"——BUE 帧可能需要 BUE 自己的 patch;patch 安装门 = 网络模块启用,与 LMN 探针解耦)。
- 前缀决策顺序冻结:①识别 BUE 帧 → ②网络模块开 → BUE 消费 → ③识别 MOD/LMN2 → ④LMN live → 放行原生 → ⑤LMN inert → BUE 处理 → ⑥非目标帧交还 vanilla。
- 契约不变性:网络关闭时不消费 BUE 帧且 patch 被移除;BUE 帧与 MOD/LMN2 不误分类;LMN live 每帧恰一次派发;探针 false 零 LMN 反射/镜像;live/inert 每方向独立判断;异常路径 hand-back 不吞原生流量。
- 两个 seam(BUE 帧消费 / LMN 接管)不得共用一个 takeover 布尔。

### 决策 4(Q4)——发送语义 = 会话驱动组播

- `SendToClients` = 向当前已建立 BUE 会话**逐一定向发送**,不是无目标帧交底层广播;无会话的原版玩家不收;本地主机天然不在远端会话集合(LHT 手写跳过由设计消掉,T2 §7.2 关闭);`SendToClient` 维持 `IConnectionSession` 寻址,**不新增 ulong 重载**。
- 冻结接口事实:`Sessions` = 调用时快照(established only);发送结果语义 = 快照空 → `NoSession`;≥1 成功 → `Sent`;有目标全失败 → `LocalTransportUnavailable`;**新增 `PartialFailure` 枚举成员表达部分失败**(按用户建议选定,不靠诊断日志拼)。
- 发送不持状态锁;`SendToClient` 校验 session 归属 runtime/established/连接代际。

### 决策 5(Q5)——会话建立 = 网络模块自动握手

- 流程:transport connected → runtime 发 Hello → server Ack/Reject → Connected → feature 收发;功能模块零握手负担。
- module 实现职责:Hello/Ack/Reject、断线清理、重连新 SessionId/代际、超时重探退避、重复 Hello 去重、版本不兼容 fail-closed、无 ghost session。
- 契约只公开:`Sessions`、`Connected`、`Disconnected`、`GenerationChanged`、发送结果。
- **运行时内部改造**:pending session 仅内部可见;公开 `Sessions` 只含 established;Ack 按 peer+generation/nonce 匹配(废弃"第一个未建立会话");`Connected` 仅握手完成后触发;Connected/Disconnected 锁外执行。

### 契约变更清单(实施期随 /to-spec 落地)

① `IBueNetworkApi.Subscribe(FeatureId, ChannelDirection, handler)` + 新枚举 `ChannelDirection`;② `IFeatureBootstrap.Network`;③ `NetworkSendResult` + `PartialFailure`;④ `Sessions` 语义收窄为 established 快照;⑤ 契约版本升级 + 冻结面变更登记。运行时内部:方向双 handler 表、会话驱动组播、自动握手、pending 隐藏、锁外回调。

### 具名移交

- 网络模块停用时 `Network` 的方法级语义细节(发送→结果枚举已定;订阅行为倾向"可订阅但入站为零+状态可查")→ /to-spec 定稿。
- T2 §7 其余验证点(listen-host 双角色、LIT P2P scope 调用方、LIR↔LIT 缝、不可靠 1:1)→ 归 T4-T6 票。
- 落地顺序(用户定):Q1 → Q2 → Q4 → Q5 → Q3(先给功能模块稳定 leverage,再把会话/传输/接管复杂度收进 runtime,最大化 locality)。

### 决策 6(Q6 补充,2026-09-06)——线帧魔数 BUE2 → BUE1;数字频道政策重申

- 依词汇改革同纪律:BUE 从未有第一代线上协议,"BUE2"暗示不存在的版本史;帧从未上线,改名零兼容代价。
- 魔数常量 `BUE2` → `BUE1`(4 字节帧头布局不变);产品与文档语言一律「BUE 帧」;"BUE1/BUE2"只允许出现在魔数常量与格式换代文档。
- **数字频道政策重申(与魔数无涉,两者不混淆)**:BUE1 魔数属于命名频道(BueNetworkApi)一侧;数字频道(0..255)只留兼容接收/运行,**不建议也不允许**在其上继续注册——CONTEXT「数字频道」既有冻结政策,此处再确认。
- 实施落点:魔数常量、注释、测试与文档的全部 "BUE2" 字样清扫,归入本票决策的实施票(wayfinder 不动源码)。
