# BUE 发布索引(RELEASES)

> **用途**:回答"现在该部署/交付哪个 DLL"。每个候选一行,与它的证据、门禁、批准记录绑定。
> **当前对外部署物** = 本表"状态"列为 **当前发布物** 的行(换行时由结单会话改标记并追加新行)。
> **维护规则**:任何票在人工批准/人工验收入档的同一提交里追加本表行;禁止删除或改写历史行(证据不可变);批准不向下自动继承——每个新候选必须另走门禁+批准。
> **永远不要**把 `src/**/bin/Release/` 的构建输出当发布物——那只是构建产物;候选身份以本表哈希 + 各票审计文件的确定性重建记录为准。

## 当前对外部署物

**DEV-V2-24 候选 v7**(2026-09-09):DLL SHA-256 `A1B339BF7D6945B8E67C32355CB8CFCC0EB95ECB2112B9CE5EB396F09A871359`(544768 字节,同日三轮 `-t:Rebuild` 逐字节一致,`audit/2026-09-09/DEV-V2-24/candidate-v7-rebuild1..3.log`)。裸 BUE 单 DLL 三环境(SP/P2P/U3DS)四官方功能 + 共存 B(BUE+独立 LMN)B1–B6 全锚 + T7 五项(T7-4 实机正向双证 ✓:C4 finding→C4'/C4'' 修正仪器链,手册补注二);用户十轮实机验收通过后关单(结单报告 `audit/2026-09-09/DEV-V2-24/结单报告.md`)。**DEV-V2-15 候选**(历史,`cacfa527…b040`)已被取代,其档案见台账行 8。

## 候选台账(按时间序)

| # | 候选 / CaseId | DLL SHA-256(字节数) | 身份锚 | 快照 | 门禁 | 批准 | 归档 / 证据 | 状态 |
|---|---|---|---|---|---|---|---|---|
| 1 | DEV-V2-07-CLEAN-20260904<br>CaseId `DEV-V2-07-20260904` | `14A98FC8…5EF6` | BuildIdentity `85FAEA17…A53D` | `ba7ecd9` | 未达(被修复轮取代) | — | `audit/2026-09-04/evidence/DEV-V2-07-20260904/` | 已取代(旧验收包绑旧 CaseId 不可复用) |
| 2 | DEV-V2-10-CLEAN-20260904<br>CaseId `DEV-V2-10-20260904` | `C3A35B07…E4224`(268288) | BuildIdentity `75DA7E6D…03FD`;DefinitionSetDigest `38D66989…B854` | `e8c3a52` | 未走完(复测发现 F-C) | — | `audit/2026-09-04/artifacts/DEV-V2-10-20260904/` | 已取代 |
| 3 | DEV-V2-11-CLEAN-20260904<br>CaseId `DEV-V2-07-20260904`(四 case) | `5B4E948E…C5BCD`(268800) | BuildIdentity `01BFF64000C29FC1FEA8B2C13C8A4EC19EFD0A1A55D8743E512B8D6797FC86B5`;DefinitionSetDigest `38D66989…B854` | `6907a1a` | gate exit 0 TechnicallyQualified(包 canonicalDigest `F05F7CC3…`) | **人工发布批准** `8ccbf80` | `audit/2026-09-04/artifacts/DEV-V2-11-20260904/` + `evidence/DEV-V2-11-20260904/` | 批准有效(历史);源码被 12/09/13 后继取代 |
| 4 | DEV-V2-12-CLEAN-20260905<br>CaseId `DEV-V2-12-20260905`(P2P 成对 `-P2P`) | `B4E37FFA…E959`(270336) | BuildIdentity `C9EEF3B84B9F8C8CEBC37EE3046ED08CDF46AE5697D6B9622283A5EC44FFE272`;canonicalDigest `9E731F7B…D15B` | `b670474` | gate exit 0 TechnicallyQualified | **人工发布批准** `eff5c0d` | `audit/2026-09-05/artifacts/DEV-V2-12-20260905/`(DLL+candidate.json)+ `DEV-V2-12-dll-sha256.txt` + `evidence/DEV-V2-12-20260905/` | 批准有效(历史);源码被 09/13 后继取代 |
| 5 | DEV-V2-09 候选(轻量视觉链,未立 BuildIdentity/gate) | `3370f5d8…4007`(270848) | SHA-256 + 两轮重建一致(`audit/2026-09-05/DEV-V2-09/identity-*.log/txt`) | `c042bc0` | 不适用(视觉票) | 用户截图视觉验收 `bbb0be6` | `audit/2026-09-05/DEV-V2-09/`(结单报告+验收截图) | 已取代(被 13 构建包含) |
| 6 | DEV-V2-13 候选(轻量视觉链) | `35670269…aef6`(275456) | SHA-256 + 两轮重建一致(`audit/2026-09-05/DEV-V2-13/identity-*.log/txt`) | `ac08916` | 不适用(视觉票) | 用户实机验收 + 授权关闭 `c858775` | `audit/2026-09-05/DEV-V2-13/`(结单报告+ESC 验收截图) | 已取代(被 14/15 构建包含) |
| 7 | DEV-V2-08 生态验证 kit(三插件验收候选,绑行 6)<br>CaseId `DEV-V2-08-{LIT,LIR,LHT}-20260905` | LIT `7e35d7c7…5417`(151040) / LIR `6653035b…ac90`(52736) / LHT `6b935f5c…f995`(36864) | SHA-256 + 相邻两轮重建一致(`audit/2026-09-05/DEV-V2-08/identity-sha256.txt`);不立 BuildIdentity | 无(验证票,行 6 复用) | 确定性重建 0 警 0 错 ×2 轮 | 用户人工验收三环境(09-05 SP / 09-06 P2P+U3DS,原话留档结单报告 §3) | `audit/2026-09-05/evidence/DEV-V2-08-20260905/` + `DEV-V2-08/`(结单报告) | 验证闭环(生态插件验收候选 0.0.0;版本号授予/官方纳入属后续票) |
| 8 | **DEV-V2-15 候选(LIT 官方纳入·单人全路径,轻量链)** | `cacfa527…b040`(350208) | SHA-256 + 相邻两轮 `-t:Rebuild` 一致(`audit/2026-09-06/DEV-V2-15/identity-*.txt`) | 无(未提交) | 红绿链(编译红 14 错→桩运行时红→绿)+全套 7/7 PASS 0 警告+双轴 R1 双 NOT CLEAN→修复→零上下文 R2' 双 CLEAN→R3 fresh 追加验证双 CLEAN(首版 R2 续用 R1 实例作废,见结单审查链) | **用户单人实机验收通过**(2026-09-06,原话「我验证LIT功能无异常」,UMM 诊断包留档 `acceptance-singleplayer-20260906.md`) | `audit/2026-09-06/DEV-V2-15/`(结单报告+红绿证据+身份+验收记录) | 已取代(被行 9 取代;行 8 期间实际承载 15..23 期中间候选,均未走完三环境验收故不立行) |
| 9 | **DEV-V2-24 候选(V2 第二阶段终验·裸 BUE 单 DLL 三环境,轻量链)**<br>CaseId `DEV-V2-24-CANDIDATE-20260909`(前身 `2d3de91d…762e1`/`7448b0ce…64c9`/`40154219…8a26`/`c9b6b6e4…eb86`/`7d5dd3b5…c223`/`3cbd6268…9e4d`/`22be7a3a…5c56b`/`626bc330…d2715` 八代全部作废,不得用于采集) | `A1B339BF7D6945B8E67C32355CB8CFCC0EB95ECB2112B9CE5EB396F09A871359`(544768) | SHA-256 + 同日三轮 `-t:Rebuild` 逐字节一致(`audit/2026-09-09/DEV-V2-24/candidate-v7-rebuild1..3.log`);身份锚 `audit/2026-09-08/DEV-V2-24/identity-sha256.txt` + 各轮日志 `assembly-identity` 行 | `54f228d`+`02c17a5`(修复链末) | 每修复轮红测先行(编译红→绿)+全套 7 exe 7/7 PASS 0 警告+双轴独立审查(fresh 实例,R2 CLEAN×多轮;F-E 轮 Standards R1 BLOCKING→F1s→R2 CLEAN)+三环境实机验收(SP/P2P/U3DS 四功能全锚)+共存 B 双端 B1-B6+T7 五项(T7-4 经 C4 finding→C4'/C4'' 修正仪器链取得实机正向双证:逐条 001 Warning+面板红行,手册补注二) | **用户实机验收通过**(2026-09-09 全天十轮/组,原话「验证下来…均无异常」「四功能也无异常」「配置B也测完了…顺手把客机双击换弹测了一遍,功能正常」,UMM 包 111429/112239/112244/113740/125312/125324/130038/132554/132622/134231/135549/140253/145719/151902 全留档) | `audit/2026-09-08/evidence/DEV-V2-24-20260908/`(采集 CaseId `DEV-V2-24-20260908`:cases sp/p2p-host/p2p-client/u3ds/coexist-b/t7+kit+指纹)+ `audit/2026-09-09/DEV-V2-24/`(红绿/重建/诊断证据+结单报告)+ `audit/2026-09-08/DEV-V2-24/`(采集手册+identity)+ 玩家手册 `docs/BetterUnturnedExperience-Player-Handbook.md` | **当前发布物**(裸 BUE 单 DLL 三环境+共存 B 全锚+T7 五项含 T7-4 实机正向双证 ✓;标准装载路径折叠/非标准路径 001 兜底双层口径见 SDK §8 补注;F-C 登记 DEV-V2-25 open 不阻) |

## 注记

- **两种批准链**:网络语义票(07/10/11/12)走全链 = 红绿 → 双轴 CLEAN → 四角色资格门禁 → 人工发布批准;视觉/布局票(09/13)与官方纳入/终验票(15/24,行 8/9)走轻量链 = 红绿 → 双轴 CLEAN → 用户实机截图/日志验收 + 授权关闭,身份锚 = DLL SHA-256 + 确定性重建,不立 BuildIdentity、不产 candidate.json。
- **LoadSetIdentity 轻量链口径(行 8/9)**:`LoadSetIdentity` 词条(CONTEXT.md)定义的组件集合——BUE Host、功能、ClientUi satellite、Definition Artifact——在 V2 单 DLL 交付形态下**全部内嵌于 BUE DLL 本体**,部署环境即该 DLL(+宿主游戏环境,非 BUE 可序列化身份);故轻量链候选以「同一 DLL SHA-256 贯穿全部证据锚行(assembly-identity 行)+确定性重建记录」承担 LoadSetIdentity 绑定语义,不另产 canonical 摘要。若未来交付形态重新拆分(如 satellite 外置),须回到全链身份。
- **截断哈希**:表内 `…` 为缩写;完整值以行内"归档/证据"列指向的审计文件为准(07/10/11/12 有 candidate.json 或 sha256 清单,09/13 有 `identity-sha256.txt`)。
- **DEV-V2-08 注意**:08(LIT/LIR/LHT 迁移验证)的证据必须绑定行 6(`35670269…aef6`)或其后的新候选;不得绑旧候选拼接证据。
- V1 时代候选(根目录 `artifacts/DEV-*`,2026-08-25~09-01)随 V1 冻结(`f1ae85c`)归档,不进本台账。
