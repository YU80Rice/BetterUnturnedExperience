# GPT-11 网络能力协商决策交付报告

## 【需求执行概述】

关闭 Wayfinder 决策“定义网络能力协商与版本不兼容行为”，冻结三环境共用的应用握手、能力交集、降级、分片和状态投影规则。

## 【源码溯源清单（Traceability Matrix）】

| 需求 | 落实位置 |
| --- | --- |
| LMN 与应用双层握手 | `Network-Capability-Versioning-Spec.md` 第 1、3 节 |
| Contract/Feature 版本降级 | 网络规格第 4 节；共享契约 `WireSemanticVersion` |
| V1 不强制插件入服 | 网络规格第 2、4、11 节 |
| 能力不授予权限 | 网络规格第 5 节 |
| 连接 generation、nonce、重连 | 网络规格第 3 节 |
| 有界原子分片 | 网络规格第 6 节；共享契约 `SnapshotChunkEnvelope` |
| 状态投影隐私 | 网络规格第 7、8 节 |
| 消息与频道映射 | 网络规格第 9 节；共享契约第 5 节 |

## 【文档变更清单】

- 新增 `Network-Capability-Versioning-Spec.md`。
- 新增 `handoffs/to-11-network-contract.md`。
- 更新共享契约、后端架构、生命周期规格、领域词汇、GPT-11 工单和地图。

## 【依赖事实核对】

静态读取本地 LMN V5 的 `HandshakeProtocol.cs`、`NamespacedTransport.cs`、测试与命名频道标准，确认依赖具备频道 Hello/Ack 和命名路由。该事实不构成本插件实现、编译或三环境运行证据。

## 【编译验证记录】

当前项目没有生产工程或可执行构建命令。本次仅修改 Wayfinder Markdown 文档；未执行编译，不构成实现或运行 PASS。

## 【子智能体审核记录】

- 第 1 轮 FAIL：状态提前完成、mutation 上限未冻结、线路使用 `System.Version`。
- 第 2 轮 FAIL：Major 拒绝帧未脱离 Contract envelope，单片/分片不可判别。
- 第 3 轮 FAIL：频道映射残留普通 `0x0005`。
- 修复：冻结 `WireSemanticVersion(u16×3)`、mutation 64、snapshot entry 256、固定 36-byte `BUEB` bootstrap reject、始终使用 SnapshotChunkEnvelope、普通 `0x0005` 禁发及频道唯一映射。
- 最终独立审计：PASS，阻断项 0。

失败过程详见 `Implementation-v0.9-1135.md`，未覆盖历史记录。

## 【偏离与妥协说明】

无需求偏离。为保持开放和原版兼容，V1 明确不启用 `RequiredForSession`；插件或握手缺失不会阻止原版连接。

## 【测试建议】

- 为 BUEB bootstrap parser 与普通 envelope parser 分别实施 fuzz/property tests。
- 覆盖旧 generation、nonce 错误、重复 Hello、分片冲突、普通 `0x0005`、错误方向 BUEB 和保留版本组合。
- 使用同一候选 DLL 哈希分别验证 SP、P2P Host/Client 和 U3DS。

## 【最终结论】

GPT-11 Wayfinder 决策闭环：PASS。生产实现、编译、部署和三环境运行：未开始、未验证。



