# DEV-V2-07：三环境网络层验证与证据包

Type: task
Status: ready-for-agent
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-03-buenetworkapi-runtime, DEV-V2-04-lmn-takeover, DEV-V2-05-v1-compat-layer, DEV-V2-06-config-migration-network-settings
Spec: `../spec-V2-phase1-lmn-adoption.md`（Testing Decisions「三环境验证」）

## Scope

对 V2 第一阶段最终 DLL 完成单人、SteamP2PFriends Host/Client、U3DS Headless 三环境网络层验证，绑定 LoadSetIdentity 证据包：

- 同一 CandidateBuild / DLL SHA-256 / LoadSetIdentity（含网络模块）绑定全部环境证据；不用单一 DLL 哈希拼接证据。
- 单人：本地网络模块启动、频道注册/发送/接收正常。
- SteamP2PFriends Host/Client：V2 命名频道互通；V1 兼容路径（旧 no-op 插件）两端收发；同一 CaseId、时间窗正交重叠。
- U3DS Headless：不实例化任何 UI/ClientUi；网络模块启动正常；本地功能（BII）不受网络故障影响。
- 证据包结构沿用 DEV-15E/16E：candidate/ + cases/{sp,p2p-host,p2p-client,u3ds}/（case.json、evidence.log、diagnostics.zip、screenshots-or-video.txt）。
- 若 U3DS 上独立 LMN 存在，验证接管生效（面板「已由 BUE 接管」）；无 LMN 时验证零误报。

## 验收条件

- [ ] 三环境证据齐全且绑定同一 LoadSetIdentity；人工验收（real-machine-test-loop）。
- [ ] V2 第一阶段完成标准 8 条全满足（CONTEXT「LMN 纳入完成标准」L93-95）。
- [ ] 证据包通过资格裁决（Fulfilled）；发布授权仍需人工批准。

## 不做

- 不实现功能（功能已在 DEV-V2-01~06 完成）；不修改源码。
