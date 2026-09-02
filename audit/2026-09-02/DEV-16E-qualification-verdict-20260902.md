# DEV-16E 三环境证据包与技术资格裁决 — 2026-09-02

> CaseId（环境级）：`DEV-16E-SP-20260902` / `DEV-16E-P2P-20260902` / `DEV-16E-U3DS-20260902`
> PackageId：`pkg-16e-20260902` · CandidateBuild：`DEV-16E-CLEAN-20260902`
> 候选 DLL：`audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`
> SHA-256：`6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`
> BuildIdentity：`600A6926F0AA47ADA0902AA5D64F69E2BBFE2F10FDA817F99C2FEC039FC9B26A`

## 1. 裁决结果（QualificationEvidenceGate.Evaluate，QualificationPolicy.Default）

**Status = `TechnicallyQualified` · TechnicallyQualified = True**

| 角色 | Verdict |
|---|---|
| SinglePlayer | `Fulfilled` |
| SteamP2PHost | `Fulfilled` |
| SteamP2PClient | `Fulfilled` |
| U3dsHeadless | `Fulfilled` |
| U3dsClientUi | `NotApplicable`（政策默认） |

P2P 硬约束通过：Host/Client 同一 CaseId `DEV-16E-P2P-20260902`、时间窗重叠（Host 14:32–14:34Z、Client 14:32:30–14:34:30Z）。

## 2. 证据包结构（`audit/2026-09-02/evidence/DEV-16E-20260902/`）

| 案例 | CaseId | 文件 | SHA-256 | 字节 |
|---|---|---|---|---|
| SP | `DEV-16E-SP-20260902` | `sp/sp.log` | `CAC67B63...1571B` | 1,386,582 |
| P2P Host | `DEV-16E-P2P-20260902` | `p2p-host/p2p-host.log` | `21DD2BA8...8B9690` | 1,897,583 |
| P2P Client | `DEV-16E-P2P-20260902` | `p2p-client/p2p-client.log` | `E6FEC0F3...63BC22` | 3,937,080 |
| U3DS | `DEV-16E-U3DS-20260902` | `u3ds/u3ds-server.log` | `FB6E8277...2DEC15A7` | 1,921 |

## 3. 各环境验证摘要

- **单人**：管理面板/开关/绿红预览/真实图标/拖入提交/投影收敛；用户 2026-09-02 确认"功能无异常"。
- **SteamP2P Host/Client**：双端同一 DLL hash、同一 CaseId、时间窗重叠；placement-decision Submitted 多次、容器内位置变更双方可见（原生 `sendDragItem → ReceiveDragItem` 权威链）。
- **U3DS Headless**：`runtime-gate decision=Headless batchMode=True`、`BootstrapReady decision=Headless`、Better Item Interaction `accepted=True`；无 ClientUi/PlayerUI/Sleek/Hook/预览 dispatch；客户端经 U3DS 服务器连接增强渲染正常。

## 4. 时间窗说明

本裁决使用**估算 UTC 时间窗**（基于诊断包文件时间戳 UTC+8 换算）：
- SP 13:30–13:32Z、P2P 14:32–14:34:30Z、U3DS 14:40–14:46Z。
- 若需精确到用户记录的 UTC 起止，可在人工批准时替换 `StartedUtc/EndedUtc` 并重新裁决（P2P 重叠约束仍满足）。

## 5. 边界

技术资格 = "同候选同哈希在 SP/P2P/U3DS 可运行且增强拖入/投影正常"。**不自动授予**发布授权、Stable 或 ReleaseReady。后续仍需：Gemini 前端消费复核 ACCEPT → 人工开发者批准 BuildIdentity/LoadSetIdentity/DLL 哈希 → 才进入发布门禁。
