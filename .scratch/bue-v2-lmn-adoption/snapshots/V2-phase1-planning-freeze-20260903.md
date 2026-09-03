# V2 第一阶段规划冻结快照 — 2026-09-03

> 性质：V2 第一阶段（LMN 官方纳入 + BueNetworkApi）**规划阶段冻结**——wayfinder 地图 7/7 完成、规格已批准、实施工单已拆分。
> 冻结时点：DEV-V2-01 开工前（2026-09-03）。
> 本快照非发布/Stable 授权；实施产物按 output-review-loop 逐票红测→双轴→提交，发布仍需人工批准。

## 1. 已冻结的决策链

| 阶段 | 产出 | 位置 |
|---|---|---|
| wayfinder 地图 | 7 决策票 + 4 research 全部 resolved | `.scratch/bue-v2-lmn-adoption/map.md` + `issues/` + `research/` |
| 契约批准 | SCR-GPT18-001 人工批准冻结（T2） | `issues/V2-T2-scr-gpt18-001-approval.md` |
| 规格 | V2 第一阶段完整规格，人工批准 | `spec-V2-phase1-lmn-adoption.md` |
| 工单拆分 | 8 张实施票（DEV-V2-01~08）带阻塞边 | `issues/DEV-V2-*.md` |

## 2. 实施工单依赖链

```
DEV-V2-01 SDK 基线 ──┐
                     ├──→ 03 BueNetworkApi 运行时 ──→ 04 LMN 接管 ──┬──→ 05 V1 兼容层
DEV-V2-02 契约类型 ──┘                                   │         └──→ 06 空迁移+网络设置
                                                         └──→ 07 三环境验证 ──→ 08 生态迁移验证
```

- frontier（开工时）：DEV-V2-01（SDK 基线）、DEV-V2-02（契约类型）——均无阻塞，可并行。

## 3. 关键决策摘要（实施时必须遵守）

- BueNetworkApi：频道=FeatureId；版本协商 API 内部自动；透传可靠性+本地失败信号；会话事件订阅；双向链路抽象；公开 API 进 Contracts / 帧格式留 Host 内部；`BueNetwork` 命名空间；`NetworkSendResult` 显式枚举。
- 接管：Chainloader.PluginInfos 检测（零扫描）；Priority.First 前缀抢占（唯一可行）；禁 BepInIncompatibility；接受残响；面板带停药按钮；只处理已加载。
- V1 兼容：旧插件无改动运行；帧识别在接收面；兼容策略可关；故障只隔离 V1；退出阈值叠加"验证闭环"维度。
- 空迁移：LMN 无配置 → no-op；日志+面板行；YAGNI。
- 定级：网络模块=核心；BueNetworkApi=核心基础设施（不参与面板开关）；网络故障只隔离网络功能。
- 方向：LMN/LIT/LIR/LHT 并入 BUE 官方功能，不再独立维护；玩家只部署单 DLL。

## 4. 实施依赖清单（DEV-V2-01 需满足）

- [ ] SDK 基线锁定：`Libs\SDG.NetTransport.dll` 刷新到游戏安装版 + `ITransportConnection` 成员清单固化。
- [ ] LIT/LIR/LHT V2 迁移验证（DEV-V2-08，末位）。
- [ ] 网络模块 Settings Facet（DEV-V2-06）。
- [ ] BueNetworkApi 类型写入 ContractTypes.cs（DEV-V2-02）。

## 5. 边界

- 本快照为规划冻结，不含任何实施产物；DEV-V2-01 起逐票实施。
- 三环境验证（DEV-V2-07）完成前不宣称 V2 第一阶段运行或发布通过。
