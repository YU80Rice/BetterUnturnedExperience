# POST-P4-01 输出审查链

票：`.scratch/bue-post-phase4-closure/issues/01-network-isolation-projection.md`  
日期：2026-09-13  
固定点：HEAD `8338441` 工作区增量（未提交时审查）  
契约：仍 2.1（`ContractTypes.cs` 零 diff）  
候选：不授（非发布票）

## 红 → 绿

- 接缝：`ManagementPanelModel.GetFeatureStatusProjection` / `DraftSetFeatureEnabled` / `SaveDraft`（面板模型外显，非 Glazier）。
- 红测：`BenignNetworkIsolationDoesNotReadAsUnexplainedFault` + `GenuineIsolationKeepsHonestFailureFace`。
- 观察红：`网络模块良性隔离不得投影成无解释的「已隔离」`（ClientUi FAIL）。
- 实现：`IsBenignNetworkIsolation` 仅匹配 `io.github.yu80rice.bue.network` + Isolated → 状态「可继续通信」、无开关、拒草稿、不露诊断码。其它功能九态 / Q44 诚实失败面不变。未改生命周期机、未改 `Start` 恒 `default(FeatureStartResult)`、未要求 Running 才能通信。
- 绿：ClientUi PASS；全套 7/7 PASS 0 警告 0 错误（ClientUi / Contracts / Network / Placement / Settings / Release / Plugin）。Plugin+ClientUi `-t:Rebuild` 0/0。

## 双轴（Fresh-instance）

| 轮 | Standards | Spec | 处置 |
|---|---|---|---|
| R1 | CLEAN（硬性项无；判断气味 Primitive Obsession / Feature Envy / Repeated Switches，不阻断） | CLEAN（文案+关开关=规格「二者择一」的合规叠加，非范围蔓延） | 关环 |

## 具名 deferrable

- `GetFeatureStatusProjection` / `DraftSetFeatureEnabled` 头注释仍写「有开关 iff 可停止 seam」，未同步第三门禁——Standards 判注释滞后非硬违规。
- v1compat Isolated 未收进本票（工单只点 `bue.network`）。
- 原生 Glazier 详情行随模型投影走，无独立宿主可测面（既有 F3 先例）。

## 身份

不授 SHA-256 / CaseId / RELEASES 行。本票只改面板投影文案与开关门禁。
