# BUE 发布索引(RELEASES)

> **用途**:回答"现在该部署/交付哪个 DLL"。每个候选一行,与它的证据、门禁、批准记录绑定。
> **当前对外部署物** = 本表"状态"列为 **当前发布物** 的行(换行时由结单会话改标记并追加新行)。
> **维护规则**:任何票在人工批准/人工验收入档的同一提交里追加本表行;禁止删除或改写历史行(证据不可变);批准不向下自动继承——每个新候选必须另走门禁+批准。
> **永远不要**把 `src/**/bin/Release/` 的构建输出当发布物——那只是构建产物;候选身份以本表哈希 + 各票审计文件的确定性重建记录为准。

## 当前对外部署物

**DEV-V2-13 候选**(2026-09-05):DLL SHA-256 `3567026930757ffbc5d40b2da12b148f470dba806b5ca08abf14ac6e4a2caef6`(275456 字节,同源两轮 `-t:Rebuild` 逐字节一致)。含 DEV-V2-01~12 全部修复 + 09(主菜单节距)+ 13(ESC 暂停菜单并入原生列)。用户实机验收通过并授权关闭(`c858775`)。

## 候选台账(按时间序)

| # | 候选 / CaseId | DLL SHA-256(字节数) | 身份锚 | 快照 | 门禁 | 批准 | 归档 / 证据 | 状态 |
|---|---|---|---|---|---|---|---|---|
| 1 | DEV-V2-07-CLEAN-20260904<br>CaseId `DEV-V2-07-20260904` | `14A98FC8…5EF6` | BuildIdentity `85FAEA17…A53D` | `ba7ecd9` | 未达(被修复轮取代) | — | `audit/2026-09-04/evidence/DEV-V2-07-20260904/` | 已取代(旧验收包绑旧 CaseId 不可复用) |
| 2 | DEV-V2-10-CLEAN-20260904<br>CaseId `DEV-V2-10-20260904` | `C3A35B07…E4224`(268288) | BuildIdentity `75DA7E6D…03FD`;DefinitionSetDigest `38D66989…B854` | `e8c3a52` | 未走完(复测发现 F-C) | — | `audit/2026-09-04/artifacts/DEV-V2-10-20260904/` | 已取代 |
| 3 | DEV-V2-11-CLEAN-20260904<br>CaseId `DEV-V2-07-20260904`(四 case) | `5B4E948E…C5BCD`(268800) | BuildIdentity `01BFF64000C29FC1FEA8B2C13C8A4EC19EFD0A1A55D8743E512B8D6797FC86B5`;DefinitionSetDigest `38D66989…B854` | `6907a1a` | gate exit 0 TechnicallyQualified(包 canonicalDigest `F05F7CC3…`) | **人工发布批准** `8ccbf80` | `audit/2026-09-04/artifacts/DEV-V2-11-20260904/` + `evidence/DEV-V2-11-20260904/` | 批准有效(历史);源码被 12/09/13 后继取代 |
| 4 | DEV-V2-12-CLEAN-20260905<br>CaseId `DEV-V2-12-20260905`(P2P 成对 `-P2P`) | `B4E37FFA…E959`(270336) | BuildIdentity `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272`;canonicalDigest `9E731F7B…D15B` | `b670474` | gate exit 0 TechnicallyQualified | **人工发布批准** `eff5c0d` | `audit/2026-09-05/artifacts/DEV-V2-12-20260905/`(DLL+candidate.json)+ `DEV-V2-12-dll-sha256.txt` + `evidence/DEV-V2-12-20260905/` | 批准有效(历史);源码被 09/13 后继取代 |
| 5 | DEV-V2-09 候选(轻量视觉链,未立 BuildIdentity/gate) | `3370f5d8…4007`(270848) | SHA-256 + 两轮重建一致(`audit/2026-09-05/DEV-V2-09/identity-*.log/txt`) | `c042bc0` | 不适用(视觉票) | 用户截图视觉验收 `bbb0be6` | `audit/2026-09-05/DEV-V2-09/`(结单报告+验收截图) | 已取代(被 13 构建包含) |
| 6 | **DEV-V2-13 候选(轻量视觉链)** | `35670269…aef6`(275456) | SHA-256 + 两轮重建一致(`audit/2026-09-05/DEV-V2-13/identity-*.log/txt`) | `ac08916` | 不适用(视觉票) | 用户实机验收 + 授权关闭 `c858775` | `audit/2026-09-05/DEV-V2-13/`(结单报告+ESC 验收截图) | **当前发布物** |
| 7 | DEV-V2-08 生态验证 kit(三插件验收候选,绑行 6)<br>CaseId `DEV-V2-08-{LIT,LIR,LHT}-20260905` | LIT `7e35d7c7…5417`(151040) / LIR `6653035b…ac90`(52736) / LHT `6b935f5c…f995`(36864) | SHA-256 + 相邻两轮重建一致(`audit/2026-09-05/DEV-V2-08/identity-sha256.txt`);不立 BuildIdentity | 无(验证票,行 6 复用) | 确定性重建 0 警 0 错 ×2 轮 | 用户人工验收三环境(09-05 SP / 09-06 P2P+U3DS,原话留档结单报告 §3) | `audit/2026-09-05/evidence/DEV-V2-08-20260905/` + `DEV-V2-08/`(结单报告) | 验证闭环(生态插件验收候选 0.0.0;版本号授予/官方纳入属后续票) |
| 8 | DEV-V2-15 候选(LIT 官方纳入·单人全路径,轻量链) | `cacfa527…b040`(350208) | SHA-256 + 相邻两轮 `-t:Rebuild` 一致(`audit/2026-09-06/DEV-V2-15/identity-*.txt`) | 无(未提交) | 红绿链(编译红 14 错→桩运行时红→绿)+全套 7/7 PASS 0 警告+双轴 R1 双 NOT CLEAN→修复→R2 双 CLEAN | 待用户:单人实机自验(点按钮→本地整理;关闭→原生回退;面板「背包整理」)+发布批准 | `audit/2026-09-06/DEV-V2-15/`(结单报告+红绿证据+身份) | **候选(待实机验收/批准)** |

## 注记

- **两种批准链**:网络语义票(07/10/11/12)走全链 = 红绿 → 双轴 CLEAN → 四角色资格门禁 → 人工发布批准;视觉/布局票(09/13)走轻量链 = 红绿 → 双轴 CLEAN → 用户实机截图/日志验收 + 授权关闭,身份锚 = DLL SHA-256 + 确定性重建,不立 BuildIdentity、不产 candidate.json。
- **截断哈希**:表内 `…` 为缩写;完整值以行内"归档/证据"列指向的审计文件为准(07/10/11/12 有 candidate.json 或 sha256 清单,09/13 有 `identity-sha256.txt`)。
- **DEV-V2-08 注意**:08(LIT/LIR/LHT 迁移验证)的证据必须绑定行 6(`35670269…aef6`)或其后的新候选;不得绑旧候选拼接证据。
- V1 时代候选(根目录 `artifacts/DEV-*`,2026-08-25~09-01)随 V1 冻结(`f1ae85c`)归档,不进本台账。
