# T3：BueNetworkApi 生产传输绑定设计

Type: grilling
Status: open
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 01

## Question

BueNetworkApi（BUE2 帧）至今**无生产传输绑定、无真实消费者**（08 R5 勘误实锤）。本票决定绑定设计：

1. 两方向收发：client→server（`Provider.clientTransport`/`IClientTransport`，T1 research L110/L128）与 server→client（`ITransportConnection.Send`，T1 L104）；发送侧按 target steamId 寻径（FindClientTransport 先例）;
2. BUE2 帧进不进 `NetMessages.ReceiveMessageFromClient/Server` 前缀决策点?与接管决策核（12 的 live/inert 两态契约、`ShouldConsumeInbound`）的关系——LMN 缺席（inert）时 BUE2 帧是否也拦（BUE 独立可用）还是放行（零误报优先）;
3. 可靠位映射（ENetReliability 两值）与 16KB 上限在生产路径的落实;
4. 与 V1 兼容层/LMN2 委托的互斥与共存（同一前缀内三类帧的决策矩阵）。

产出：绑定设计决策（含决策锚与红测面），成为后续实施票与 T4-T6 重写的地基。
