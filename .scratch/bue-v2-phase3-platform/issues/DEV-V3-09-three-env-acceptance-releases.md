# DEV-V3-09：三环境验收与发布（唯一 2.1 候选+RELEASES 行+publish 换新）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-08（SDK 附录总装）
Spec: `../spec.md`（Further Notes 候选策略+「契约版本」节）

## What to build

Phase-3 全部平台缝以单一对外版本交付：唯一的 `2.1` 候选 DLL 经单机/P2P/U3DS 三环境实机验收全判据通过，SHA-256/CaseId 绑定候选身份，人工批准后加 RELEASES 行，publish 正式交付包同步换新（DLL+SDK 契约文档+交付说明）。这是 Phase-3 唯一产生候选身份与 RELEASES 行的票。

## Scope

- 契约版本合批生效：T2..T8 的 Minor 加性变更单一批次合入 **2.1**（若实施中实际分批则按 Minor 顺延 2.2——分批是允许的实施计划）；宿主门槛 `SupportedContractMajor=2` 不动；2.0 模块继续可注册回归。
- 三环境实机验收（判据按各平台缝冻结语义逐条核对：白名单拒绝/事件归属/生命周期启停与隔离/发送 Throttled/链路健康/HostTick/Settings 双 scope/诊断摘要/Logger 行）。
- 候选身份：`-t:Rebuild` 候选 DLL + assembly-identity SHA-256 绑定 + CaseId + 人工批准 → RELEASES 行 + publish 交付包换新（v8 2.0 基线包退役归档）。
- 全套测试 7 个测试工程 exe 直跑全 PASS、0 警告 0 错误。
- 实机部署/清目录/cfg/指纹/日志回收 agent 代办，用户只做游戏内操作（指引压缩成编号步骤）。

## 验收条件

- [ ] 三环境验收全判据过（每环境记录结构化诊断与摘要证据，绑定候选 SHA-256）
- [ ] 全套测试 7 exe 直跑全 PASS；0 警告 0 错误
- [ ] 唯一 `2.1` 候选身份确立：SHA-256/CaseId 绑定，RELEASES 行加入（注明 Phase-3 平台缝 Minor 批次），人工批准记录在案
- [ ] publish 正式交付包同步换新（DLL+SDK 附录版契约文档+交付说明；SDK 文档随主 DLL 版本走不独立发版）
- [ ] 双轴独立审查（每轮全新实例）CLEAN；2.0 模块兼容回归绿
