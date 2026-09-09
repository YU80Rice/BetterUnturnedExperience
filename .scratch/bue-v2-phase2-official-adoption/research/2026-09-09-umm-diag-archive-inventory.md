# UMM 诊断包归档状态清单 + 清理建议

- 日期：2026-09-09
- 性质：只读盘点（未删除、未移动、未修改 UMM 目录任何文件）
- 盘点对象：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.1-win-x64`
- 对照对象：BUE 仓库 `audit/` 与 `.scratch/` 的 `.md/.txt/.log`
- 口径：关键日志 `LogOutput.log`（及散落 `U3DS_*LogOutput.log`）以**文件大小 + SHA-256** 对仓库入库全文；文档时间戳命中但无全文入库记「仅引用」

## 0. 盘点计数

| 类别 | 数量 | 磁盘（文件合计） |
|---|---|---|
| `UMM-诊断包_*` 文件夹 | 62 | 见 §3 |
| `UMM-诊断包_*.zip` | 1 | 98091 B（0.1 MiB） |
| 散落 `U3DS_*LogOutput.log` | 6 | 84796 B（0.1 MiB） |
| **诊断相关合计** | **69** | **57391927 B（54.7 MiB）** |
| 目录内 UMM.exe + pdb（非诊断，勿动） | 2 | ~73 MiB |
| 整个 UMM 工作目录 `du -sh` | — | 129 MiB |

三档（69 项全计入）：

| 档 | 计数 | 构成 |
|---|---|---|
| **已归档** | 44 | 38 个诊断包文件夹 + 6 个散落 U3DS 日志（SHA-256 与仓库 evidence 全文一致，且有 case/verification 绑定） |
| **仅引用** | 15 | 15 个诊断包文件夹（票面/结单/验收文档引用时间戳或行号，原始 `LogOutput.log` 未入库） |
| **未归档** | 10 | 9 个诊断包文件夹 + 1 个 zip（仓库无时间戳命中） |

特别包 `*210433*` / `*225112*` / `*225127*`：**三个都还在 UMM 目录**，档位均为「仅引用」。DEV-V2-25 Scope 4 具名缺口**可以靠补归档这三包收掉**（详见 §2）。

配对约定：摘要「来源」含 `E:\Steam\...` ≈ 本机主机位；含 `C:\Program Files (x86)\Steam\...` ≈ 客机位。端别另用日志 `[[Host]]` / `[[Client]]` / `peer scope` / `CLIENT_CONNECT` 交叉确认。

---

## §1 总览表

列说明：`LogOutput` 列为字节 + SHA-256 前 16 位；完整哈希见各行「证据」。清理建议取值：`可删` / `先归档再删` / `保留`。

### 1.1 2026-09-04（DEV-V2-07 / 10 / 11）

| 包名 | 轮次 / 端别 | 档 | 仓库锚点 | 清理 | 证据 |
|---|---|---|---|---|---|
| `UMM-诊断包_20260904_103302` | DEV-V2-07 configA 晨间 / 客机侧（摘要 638 B，无 d3d） | 已归档 | `audit/2026-09-04/evidence/DEV-V2-07-20260904/configA/morning-session-103302/LogOutput.log` + `audit/2026-09-04/DEV-V2-07/configA-verification-r1.md` | 可删 | LogOutput 355395 B，sha256=`316bdab9769adaeb…ac32e502` 与仓库逐字节一致 |
| `UMM-诊断包_20260904_103321` | 同上晨间 / 主机（摘要 785 B + d3d/dxgi） | 已归档 | `…/configA/morning-session-103321/LogOutput.log` + 同上 verification | 可删 | 1489068 B，`582fb4e9083c85bc…468b46b9` |
| `UMM-诊断包_20260904_155941` | configA / VM 客机 | 已归档 | `…/configA/local-vm-client-155941/LogOutput.log` | 可删 | 114486 B，`e0e032183ea41035…3d336ce5` |
| `UMM-诊断包_20260904_160703` | configA / 本机主机 | 已归档 | `…/configA/local-host-160703/LogOutput.log` | 可删 | 523262 B，`5ea06a9b485c7468…45e3e5b8` |
| `UMM-诊断包_20260904_161432` | configA / U3DS 客机 | 已归档 | `…/configA/u3ds-client-161432/LogOutput.log` | 可删 | 172306 B，`5f0e64919fa24378…af2ea710` |
| `UMM-诊断包_20260904_165453` | configB / VM 客机 | 已归档 | `…/configB/local-vm-client-165453/LogOutput.log` + `configB-verification-r1.md` | 可删 | 124392 B，`c8be368044603ff9…931aab78` |
| `UMM-诊断包_20260904_165513` | configB / 本机主机 | 已归档 | `…/configB/local-host-165513/LogOutput.log` | 可删 | 468943 B，`8a57f077efcb7d12…d7ca6bd7` |
| `UMM-诊断包_20260904_170257` | configB / U3DS 客机 | 已归档 | `…/configB/u3ds-client-170257/LogOutput.log` | 可删 | 619428 B，`2f08b32e76a02d6f…d4cf73de` |
| `UMM-诊断包_20260904_215250` | DEV-V2-10 configB 复测 / VM 客机 | 已归档 | `audit/2026-09-04/evidence/DEV-V2-10-20260904/configB-retest/vm-client-215250/LogOutput.log` | 可删 | 158912 B，`bb540522c97e5912…19a63b9ff` |
| `UMM-诊断包_20260904_215303` | DEV-V2-10 / 主机 | 已归档 | `…/host-215303/LogOutput.log` | 可删 | 497095 B，`d2ab9b9ef7484712…065bb49a8` |
| `UMM-诊断包_20260904_215822` | DEV-V2-10 / U3DS 客机 | 已归档 | `…/u3ds-client-215822/LogOutput.log` | 可删 | 210333 B，`7bcbcad9d5219b37…87a80f60` |
| `UMM-诊断包_20260904_235508` | DEV-V2-11 configB 复测 / VM 客机 | 已归档 | `audit/2026-09-04/evidence/DEV-V2-11-20260904/configB-retest/vm-client-235508/LogOutput.log` | 可删 | 404413 B，`ee6de26d290d07bd…a9553c9a` |
| `UMM-诊断包_20260904_235544` | DEV-V2-11 / 主机 | 已归档 | `…/host-235544/LogOutput.log` | 可删 | 1572999 B，`7f8c6b7a977d4f54…206c314f` |

配对：103302/103321；155941/160703；165453/165513；215250/215303；235508/235544。U3DS 客机单端：161432、170257、215822。对应服务端散落日志见 §1.5。

### 1.2 2026-09-05～09-07

| 包名 | 轮次 / 端别 | 档 | 仓库锚点 | 清理 | 证据 |
|---|---|---|---|---|---|
| `UMM-诊断包_20260905_000024` | DEV-V2-11 / U3DS 客机（跨日 00:00） | 已归档 | `…/DEV-V2-11-20260904/configB-retest/u3ds-client-000024/LogOutput.log` | 可删 | 515316 B，`a0bda99d351643ac…69671dfe` |
| `UMM-诊断包_20260905_004017` | 09-05 00:40 本机（E:\Steam；4 插件；BUE sha `5B4E948E…C84C5BCD`） | 未归档 | 无时间戳命中 | 保留 | LogOutput 441220 B，`b589dbe71d7da673…a8539978`；票面未绑 |
| `UMM-诊断包_20260905_143829` | DEV-V2-12 复测 / VM 客机 | 已归档 | `audit/2026-09-05/evidence/DEV-V2-12-20260905/retest-r1/vm-client-143829/LogOutput.log` | 可删 | 422351 B，`547d94bd8cc82ce4…792a094c` |
| `UMM-诊断包_20260905_143901` | DEV-V2-12 / 主机 | 已归档 | `…/host-143901/LogOutput.log` | 可删 | 1741948 B，`e30f93e1d59b0e1d…f22887516` |
| `UMM-诊断包_20260905_144421` | DEV-V2-12 / U3DS 客机 | 已归档 | `…/u3ds-client-144421/LogOutput.log` | 可删 | 990299 B，`ded764b64583abb5…fe1fac0582e43e5d` |
| `UMM-诊断包_20260905_204812` | DEV-V2-13 ESC 暂停钮复测 / 本机 | 已归档 | `audit/2026-09-05/evidence/DEV-V2-13-20260905/retest-r1/LogOutput.log` + `.scratch/bue-v2-lmn-adoption/issues/DEV-V2-13-*.md` | 可删 | 232930 B，`1192c9770ba3d72e…eae468c5` |
| `UMM-诊断包_20260905_234739` | DEV-V2-08 SP 环境（包于 09-06 晨导出，sha 对 09-05 evidence） | 已归档 | `audit/2026-09-05/evidence/DEV-V2-08-20260905/env/sp/LogOutput.log`（fingerprint 点名 234739） | 可删 | 714617 B，`bf9f698951055c39…01aa8ce0` |
| `UMM-诊断包_20260906_085207` | DEV-V2-08 P2P 主机（时间戳字符串未入文档，sha 命中） | 已归档 | `…/env/p2p-host/LogOutput.log` | 可删 | 1788634 B，`cbcccd7bd483088b…eb2fbdc9` |
| `UMM-诊断包_20260906_085211` | DEV-V2-08 P2P 客机 | 已归档 | `…/env/p2p-client/LogOutput.log` | 可删 | 937270 B，`54882c08f1243360…32c39bbf` |
| `UMM-诊断包_20260906_090550` | DEV-V2-08 U3DS 客机 | 已归档 | `…/env/u3ds-client/LogOutput.log` | 可删 | 447607 B，`56329e4b23b9b905…acd43502` |
| `UMM-诊断包_20260906_183015` | DEV-V2-15 单人验收 / 本机 SP（BUE sha `CACFA527…9B03B040`） | 仅引用 | `audit/2026-09-06/DEV-V2-15/acceptance-singleplayer-20260906.md`（显式指向本 UMM 路径 + `:136` 身份行）；结单同引 | 先归档再删 | 463139 B，`7f5d7187ac3135d3…b66b60f7`。建议落位：`audit/2026-09-06/DEV-V2-15/evidence/sp-183015/LogOutput.log` |
| `UMM-诊断包_20260907_233302.zip` | 09-07 夜 zip（内含 LogOutput 1139547 B 等 4 文件） | 未归档 | 无 | 保留 | zip 98091 B；未与仓库任何 log 对上 |
| `UMM-诊断包_20260907_233517` | 09-07 夜 / 本机主机（E:\Steam；**无 BUE 身份行**，仅 SPF；两路 HOST_AUTHENTICATE） | 未归档 | 无 | 保留 | LogOutput 4892927 B，`092230f252c0bda4…4dc6ff8d`；最大单包 |
| `UMM-诊断包_20260907_233519` | 09-07 夜 / 客机（C:\Program Files；无 BUE） | 未归档 | 无 | 保留 | 674258 B，`47d66772d4a5932b…e2cd2cddd`；与 233517 成对 |

配对：143829/143901；085207/085211；233517/233519。234739 与 085207/085211/090550 同属 DEV-V2-08 环境包，导出时刻跨到 09-06 晨。

### 1.3 2026-09-08（DEV-V2-24 候选采集 / litfb 夜场）

| 包名 | 轮次 / 端别 | 档 | 仓库锚点 | 清理 | 证据 |
|---|---|---|---|---|---|
| `UMM-诊断包_20260908_163823` | DEV-V2-24 v3 SP / 本机 | 已归档 | `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/sp/LogOutput.log` + `cases/sp/case.md` | 可删 | 622640 B，`c4a9eaa5378dd9d3…f98fde7c` |
| `UMM-诊断包_20260908_165948` | DEV-V2-24 v3 P2P 客机 | 已归档 | `…/cases/p2p-client/LogOutput.log` + `case.md` | 可删 | 421357 B，`097305120d4c79f8…23754f24` |
| `UMM-诊断包_20260908_165949` | DEV-V2-24 v3 P2P 主机 | 已归档 | `…/cases/p2p-host/LogOutput.log` + `case.md` | 可删 | 1890353 B，`2ef14426105e9d54…709ab510` |
| `UMM-诊断包_20260908_180432` | 18:04 成对 / **客机**（C:\Program Files；BUE sha `3CBD6268…0D399E4D`；CLIENT_CONNECT） | 未归档 | 无时间戳命中 | 保留 | 307552 B，`eaf90cd557bb0918…7c9137ea` |
| `UMM-诊断包_20260908_180438` | 18:04 成对 / **主机**（E:\Steam；同 sha `3CBD6268…`；peer scope + HOST_AUTHENTICATE） | 未归档 | 无 | 保留 | 1611041 B，`ef29f8cf6d6730fd…346c7ca3` |
| `UMM-诊断包_20260908_190250` | 19:02 成对 / **客机**（sha `3CBD6268…`） | 未归档 | 无 | 保留 | 308832 B，`7ee7cece3ebdb16b…4387144c` |
| `UMM-诊断包_20260908_190251` | 19:02 成对 / **主机**（sha `3513C7D2…0155883F32`，与客机不同构建） | 未归档 | 无 | 保留 | 1250777 B，`537873f458aaf293…83caacd0` |
| `UMM-诊断包_20260908_195933` | 候选 v3 夜场 / **主机**（sha `C9B6B6E4…E526EB86`） | 仅引用 | `.scratch/.../issues/DEV-V2-24-*.md` Comment「195933/195934 两端实为候选 v3」 | 先归档再删 | 1044020 B，`a90c1d787ead6390…2be821e2`。建议：`audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/diag-night/host-195933/LogOutput.log` |
| `UMM-诊断包_20260908_195934` | 候选 v3 夜场 / **客机**（同 sha `C9B6B6E4…`） | 仅引用 | 同上票面 | 先归档再删 | 263785 B，`26d71b218d6221df…983a5a75`。建议：`…/diag-night/client-195934/LogOutput.log` |
| `UMM-诊断包_20260908_204026` | 20:40 成对 / **客机**（sha `C9B6B6E4…`） | 未归档 | HHMMSS 无命中（204042 有、204026 无） | 保留 | 332893 B，`93fbbef463e1f63a…d53b0308`；与 204042 时间成对，建议随 204042 一并归档 |
| `UMM-诊断包_20260908_204042` | litfb3 主机（`DEBUG-litfb3`；sha `5B6E5968…A1F6E374`） | 仅引用 | DEV-V2-24 票面「204042 主机为 litfb3」 | 先归档再删 | 1406869 B，`04e6d0457d5609ce…8ec4c09c`。建议：`…/diag-night/host-204042-litfb3/LogOutput.log` |
| `UMM-诊断包_20260908_210425` | litfb 夜场 / **客机**（sha `C9B6B6E4…`=候选 v3） | 仅引用 | 票面「210425 客机仍是候选 v3」 | 先归档再删 | 298738 B，`b424aac1b87b544b…83af4785`。建议：`…/diag-night/client-210425/LogOutput.log` |
| `UMM-诊断包_20260908_210433` | **证据 A / litfb4 主机**（详见 §2） | 仅引用 | DEV-V2-25 票 Scope 4 + `audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md` | **先归档再删（最高优先）** | 1352504 B，`20c7920c2c745f3d…1240b10a`；身份 `CFE5080F…E8CFB1F`；WARN×1199 |
| `UMM-诊断包_20260908_225112` | **证据 B / litfb7 主机**（详见 §2） | 仅引用 | 同上 | **先归档再删（最高优先）** | 1612043 B，`957ac3b033ad6d4a…1cc7cb36`；身份 `FAF3C0A6…ADB63E7`；WARN×1057 |
| `UMM-诊断包_20260908_225127` | **证据 B / litfb7 客机**（详见 §2） | 仅引用 | 同上 | **先归档再删（最高优先）** | 432900 B，`7801f31c11d622c4…00f3a1d09`；身份 `C9B6B6E4…E526EB86`；行 1111 challenge gen=2 应用 |

配对：165948/165949；180432/180438；190250/190251；195933/195934；204026/204042；210425/210433；225112/225127。

### 1.4 2026-09-09（DEV-V2-24 v4-F1 / v5 / v6 / v7 / T7）

| 包名 | 轮次 / 端别 | 档 | 仓库锚点 | 清理 | 证据 |
|---|---|---|---|---|---|
| `UMM-诊断包_20260909_000640` | F-B1b 发现 / SP（v4-F1 sha `7448B0CE…C57464C9`；首拖 NRE） | 仅引用 | DEV-V2-24 票 + `audit/2026-09-08/DEV-V2-24/diag/litfb8-identity.txt` | 先归档再删 | 308544 B，`a23fa461a095fbde…ff518615`。建议：`…/cases/sp/logoutput-v4f1-20260909-000640.log` |
| `UMM-诊断包_20260909_001648` | litfb8 SP 10 秒轮（sha `BE3EB099…F8E0647F`；stack= 定位） | 仅引用 | 同上 identity + 票面 | 先归档再删 | 272333 B，`b244a43db60e9759…d228c2d6`。建议：`…/cases/sp/logoutput-litfb8-20260909-001648.log` |
| `UMM-诊断包_20260909_081724` | v5 SP 存活 / 本机（sha `2D3DE91D…B20762E1`） | 未归档 | 无「081724」字符串；票面点的是 081731 | 保留 | 2210932 B，`e06fa8d542b2e6c8…2c4dc076`；与 081731 同会话近双份（LogOutput 差 ~2 KB） |
| `UMM-诊断包_20260909_081731` | v5 SP 存活确认（同 sha `2D3DE91D…`） | 仅引用 | DEV-V2-24 票「诊断包 20260909_081731」+ `cases/sp/case-v6-20260909.md` 提及 | 先归档再删 | 2212851 B，`6ac79327f1431cd3…f5281896`。建议：`…/cases/sp/logoutput-v5-20260909-081731.log` |
| `UMM-诊断包_20260909_083303` | v5 P2P 存活 / **主机**（sha `2D3DE91D…`；拖 25 代际） | 仅引用 | 票面「主机 083303/客机 083313」 | 先归档再删 | 2785932 B，`f785b9420963b2a1…2f69b0dd7`。建议：`…/cases/p2p-host/logoutput-v5-20260909-083303.log` |
| `UMM-诊断包_20260909_083313` | v5 P2P 存活 / **客机**（同 sha） | 仅引用 | 同上 | 先归档再删 | 455987 B，`580656a7b0ee8bc1…839a3955`。建议：`…/cases/p2p-client/logoutput-v5-20260909-083313.log` |
| `UMM-诊断包_20260909_111429` | v6 SP | 已归档 | `…/cases/sp/logoutput-v6-20260909-111429.log` + `case-v6-20260909.md` | 可删 | 297793 B，`5926c8140b25c619…4f422b07` |
| `UMM-诊断包_20260909_112239` | v6 P2P **主机**（F-C ×8333） | 已归档 | `…/cases/p2p-host/logoutput-v6-20260909-112239.log` | 可删 | 4640081 B，`b6a9a76cb84e7a2b…b9215914` |
| `UMM-诊断包_20260909_112244` | v6 P2P **客机** | 已归档 | `…/cases/p2p-client/logoutput-v6-20260909-112244.log` | 可删 | 608260 B，`f669b65f9cc7d954…b8ec143d` |
| `UMM-诊断包_20260909_113740` | F-E 发现 / U3DS 直连客户端（sha `626BC330…6BCD2715`；serverSteamId=0） | 仅引用 | `audit/2026-09-09/DEV-V2-24/结单报告.md` + `audit/RELEASES.md` | 先归档再删 | 304290 B，`4b495f5091375dbb…74d779b4a`。建议：`…/cases/u3ds/logoutput-fe-client-20260909-113740.log` |
| `UMM-诊断包_20260909_120001` | litfb9 探针 / U3DS 客户端（sha `A895B32D…515C6A7E`；`server-steamid-zero`） | 仅引用 | 结单「litfb9 探针轮 12:00 / 120001」 | 先归档再删 | 243802 B，`06c8e2cb0bd83f05…f8450d2f5`。建议：`…/cases/u3ds/logoutput-litfb9-20260909-120001.log` |
| `UMM-诊断包_20260909_125312` | v7 P2P **客机** | 已归档 | `…/cases/p2p-client/logoutput-v7-20260909-125312.log` + `case-v7-20260909.md` | 可删 | 640853 B，`cc825c10b158bbde…9d41c235` |
| `UMM-诊断包_20260909_125324` | v7 P2P **主机**（同 sha 亦拷入 `cases/sp/logoutput-v7-…125324.log`） | 已归档 | `…/cases/p2p-host/logoutput-v7-20260909-125324.log` + `cases/sp/case-v7-20260909.md` | 可删 | 1857607 B，`2070850e7ffec80d…8c9cdfdd`（sp 与 p2p-host 两份入库哈希相同） |
| `UMM-诊断包_20260909_130038` | v7 U3DS **客户端** | 已归档 | `…/cases/u3ds/logoutput-v7-u3ds-client-20260909-130038.log` + `case-v7-20260909-client.md` | 可删 | 377042 B，`b03a00677b61ac76…dbcc7fbd` |
| `UMM-诊断包_20260909_132554` | v7 coexist-b **客机** | 已归档 | `…/cases/coexist-b/logoutput-v7-coexist-b-client-20260909-132554.log` + `case-v7-20260909.md` | 可删 | 269097 B，`24efbbc3c11b3cc3…197be65e` |
| `UMM-诊断包_20260909_132622` | v7 coexist-b **主机** | 已归档 | `…/cases/coexist-b/logoutput-v7-coexist-b-host-20260909-132622.log` | 可删 | 1229808 B，`547055aa01963a96…6beecda9` |
| `UMM-诊断包_20260909_134231` | v7 T7 C1 / 本机 | 已归档 | `…/cases/t7/logoutput-v7-t7-c1-20260909-134231.log` + `case-c1-20260909.md` | 可删 | 306136 B，`88a6382fd5284d7a…95063fe8d` |
| `UMM-诊断包_20260909_135549` | v7 T7 C2/C3/C5 | 已归档 | `…/cases/t7/logoutput-v7-t7-c2c3c5-20260909-135549.log` | 可删 | 300289 B，`fa7997cfe362b5a3…bff47a63` |
| `UMM-诊断包_20260909_140253` | v7 T7 C4 | 已归档 | `…/cases/t7/logoutput-v7-t7-c4-20260909-140253.log` | 可删 | 308292 B，`62b2d7705350c0ae…c6964f7c` |
| `UMM-诊断包_20260909_145719` | v7 T7 C4-LF | 已归档 | `…/cases/t7/logoutput-v7-t7-c4lf-20260909-145719.log` | 可删 | 389939 B，`81c2d05bd1c9687e…33262d10` |
| `UMM-诊断包_20260909_151902` | v7 T7 C4-LF2 | 已归档 | `…/cases/t7/logoutput-v7-t7-c4lf2-20260909-151902.log` | 可删 | 359936 B，`fcc6293e64285a1b…bb613da47` |

配对：081724≈081731（同机近双份，票面认 081731）；083303/083313；112239/112244；125312/125324；132554/132622。113740 与 120001 为 U3DS 客户端连续探针轮（非 host/client 对）。

### 1.5 散落 U3DS 日志（非诊断包文件夹）

| 文件 | 轮次 | 档 | 仓库锚点 | 清理 | 证据 |
|---|---|---|---|---|---|
| `U3DS_20260904_1615_LogOutput.log` | DEV-V2-07 configA 服务端 | 已归档 | `…/DEV-V2-07-20260904/configA/u3ds-server-1615_LogOutput.log` | 可删 | 1132 B，`77502100533ee3ee…26400312` |
| `U3DS_2026年9月4日_17点03分_LogOutput.log` | configB 服务端 | 已归档 | `…/configB/u3ds-server-1703_LogOutput.log` | 可删 | 14944 B，`81666e30c95c2f0a…b44d40c` |
| `U3DS_2026年9月4日_22点00分_LogOutput.log` | DEV-V2-10 服务端 | 已归档 | `…/DEV-V2-10-20260904/configB-retest/u3ds-server-2200_LogOutput.log` | 可删 | 21199 B，`646f05d5785d0ea5…265a6706` |
| `U3DS_2026年9月5日_00点00分_LogOutput.log` | DEV-V2-11 服务端 | 已归档 | `…/DEV-V2-11-20260904/configB-retest/u3ds-server-0000_LogOutput.log` | 可删 | 14793 B，`865cab1959498c7f…b22ad28` |
| `U3DS_2026年9月5日_14点44分_LogOutput.log` | DEV-V2-12 服务端 | 已归档 | `…/DEV-V2-12-20260905/retest-r1/u3ds-server-1444_LogOutput.log` | 可删 | 15508 B，`6b4c8045f146182f…61877e1b` |
| `U3DS_2026年9月6日_09点06分_LogOutput.log` | DEV-V2-08 U3DS 服务端 | 已归档 | `…/DEV-V2-08-20260905/env/u3ds/LogOutput.log` | 可删 | 17220 B，`c20b4af64772ff4f…b082eb7` |

---

## §2 三个特别包详查（210433 / 225112 / 225127）

`audit/2026-09-09/DEV-V2-25/scope4-evidence-check.md` §0 登记：证据 A/B 所引诊断包 **20260908_210433 / 20260908_225112 / 20260908_225127**「未归档入库=具名缺口」，并写「全库文件名搜索、git 全历史、游戏目录、TEMP 均无」。本次盘点结论：**三包仍完整躺在 UMM 工作目录**，可直接补收。

对照口径：仓库内无同名/同 SHA 的 `LogOutput.log`（已对 `audit/**/LogOutput*.log` 与 `logoutput*.log` 做 SHA-256 全集比对，三哈希均未命中）。文档侧大量引用时间戳与行号 → 档位=**仅引用**。

### 2.1 `UMM-诊断包_20260908_210433`（证据 A，主机）

- 路径：`…\UMM-v2.2.1-win-x64\UMM-诊断包_20260908_210433`
- 磁盘：`du` 1.4 MiB
- 摘要：`UMM 本地运行诊断 · 2026-09-08 21:04:33`；分类 Normal；来源全部 `E:\Steam\steamapps\common\Unturned\...`（本机主机位）
- 内部文件：

| 文件 | 字节 |
|---|---|
| `LogOutput.log` | 1352504 |
| `Client.log` | 12470 |
| `Client_Prev.log` | 13856 |
| `UMM-诊断摘要.txt` | 785 |
| `Unturned_d3d11.log` | 16835 |
| `Unturned_dxgi.log` | 4209 |

- `LogOutput.log` SHA-256：`20c7920c2c745f3dfc3a434f89cdb2c4b7aeef468dbabefe3202b46c1240b10a`
- 身份行（`:136`）：`event=assembly-identity path=E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll sha256=CFE5080FD7B46742EB239194632A57612C830597A83D60050F17EC095E8CFB1F`
- 探针：`[DEBUG-litfb3] open-dispatch`（构建带 litfb3 探针残留）；票面勘误认定本包实跑 **litfb4**（`CFE5080F…`），与 `audit/2026-09-08/DEV-V2-24/diag/litfb7-identity.txt` 前置勘误一致。
- 端别：SteamP2P `[[Host]]`；`:1301` `[TidyFault] peer scope 已开启（peer=76561199721762479, generation=2）`
- 证据 A 量化复核：`定向发送未送达（generation=2, result=LocalTransportUnavailable）` **1199 条**（与 DEV-V2-25 票面「:1301 起 ×1199」逐字吻合）
- 尾部：模块停止 1/3→3/3 + `hand-back-to-vanilla`（正常退出）
- 成对客机：`20260908_210425`（候选 v3，`C9B6B6E4…`，仅引用）

### 2.2 `UMM-诊断包_20260908_225112`（证据 B，主机）

- 路径：`…\UMM-诊断包_20260908_225112`
- 磁盘：`du` 1.7 MiB
- 摘要：`2026-09-08 22:51:12`；来源 `E:\Steam\...`
- 内部文件：

| 文件 | 字节 |
|---|---|
| `LogOutput.log` | 1612043 |
| `Client.log` | 16343 |
| `Client_Prev.log` | 12470 |
| `UMM-诊断摘要.txt` | 785 |
| `Unturned_d3d11.log` | 16835 |
| `Unturned_dxgi.log` | 4209 |

- `LogOutput.log` SHA-256：`957ac3b033ad6d4a3868ef97caad330d8ed976e05bf92779ac19167d1cc7cb36`
- 身份行（`:136`）：`path=E:\Steam\...\BetterUnturnedExperience.dll sha256=FAF3C0A6FB3515CFD10A0AFB589AA090C13C2682CEF07CB448D842444ADB63E7`
- 与 `audit/2026-09-08/DEV-V2-24/diag/litfb7-identity.txt` 逐字一致（**litfb7**）
- 探针：`DEBUG-litfb6` / `DEBUG-litfb5` / `DEBUG-litfb7 drag-update`（litfb7 决定性插桩）
- 端别：主机；`:2891` peer scope generation=2
- 证据 B 量化复核：`定向发送未送达` **1057 条**（与票面「host 225112 刷 1057 条」吻合）
- 尾部同 210433，正常退出链

### 2.3 `UMM-诊断包_20260908_225127`（证据 B，客机）

- 路径：`…\UMM-诊断包_20260908_225127`
- 磁盘：`du` 457 K
- 摘要：`2026-09-08 22:51:27`；来源 `C:\Program Files (x86)\Steam\...`（客机位）；无 d3d/dxgi
- 内部文件：

| 文件 | 字节 |
|---|---|
| `LogOutput.log` | 432900 |
| `Client.log` | 14355 |
| `Client_Prev.log` | 13599 |
| `UMM-诊断摘要.txt` | 638 |

- `LogOutput.log` SHA-256：`7801f31c11d622c49a21a204d4859978d2223c5b8ed70289c31f5b200f3a1d09`
- 身份行（`:136`）：`path=C:\Program Files (x86)\Steam\...\BetterUnturnedExperience.dll sha256=C9B6B6E40684DF04B6141BEF1F220A84D49DD96DFA7FAE228FF51696E526EB86`（客机仍是候选 v3，与主机 litfb7 不对齐——票面已记录）
- **缺口核心行仍在**：`:1111` `[TidyNet] 已接收并应用服务端会话 challenge（generation=2）。` —— 即 scope4「client 225127:1111 应用 gen=2」原文可重放
- `定向发送未送达`：0 条（客机侧不刷主机出向 WARN，符合预期）
- `:1723` 会话代际已清 generation=2
- 与 225112 时间差 15 秒，host/client 成对

### 2.4 与 DEV-V2-25 Scope 4 缺口的关系

- 缺口原文：「证据 B 原始包不可复核，其『challenge 曾送达（client 225127:1111 应用 gen=2）后链路中途劣化』的读法无法从库内原始数据重放」。
- **补归档这三包即可收掉该具名缺口**：210433 提供证据 A ×1199；225112×1057 + 225127:1111 提供证据 B 原文链。本次已在 UMM 侧当场复核条数与行号，与票面一致。
- 建议入库落位（只建议，本盘点不执行拷贝）：

```
audit/2026-09-09/DEV-V2-25/evidence/scope4-raw/
  host-210433-litfb4/LogOutput.log          (1352504, 20c7920c…)
  host-225112-litfb7/LogOutput.log          (1612043, 957ac3b0…)
  client-225127-v3/LogOutput.log            (432900,  7801f31c…)
```

备选（若希望挂在 24 的 cases 树下）：`audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/diag-night/`。收口动作还需改 `scope4-evidence-check.md` §0 把「均无」改成上述路径，并在 DEV-V2-25 票面 Comment 留一条「缺口已补收」。

- 三包合计 **3524836 B（3.4 MiB）**。补归档完成前：**保留，禁止删**。

---

## §3 清理建议汇总

> 本报告不执行任何删除。下列「可删」仅在操作者确认仓库 SHA 仍在、且不需要 UMM 侧 Client.log / d3d 附件时成立。仓库多数 cases 只收了 `LogOutput.log`，包内 `Client.log` / `Client_Prev.log` / d3d/dxgi **从未入库**——若将来要追 Unity 侧崩溃，删包即丢这些附件。默认建议：已归档包等下一次 UMM 目录大扫时再删。

### 3.1 内容已完整在库、可直接删（44 项，约 29.4 MiB）

38 个诊断包文件夹（§1.1–1.4 标「可删」）+ 6 个散落 U3DS 日志。关键 `LogOutput.log` SHA 已与 `audit/2026-09-04|05|08/evidence/` 对齐。

最大块：v6 主机 112239（4.5 MiB）、v7 主机 125324（1.9 MiB）、09-04/05 各 config 包。

不包含：Client.log / d3d 附件（库内无对应全文）。若坚持「可删」口径=仅 LogOutput 已入库，则这 44 项可删；若口径升级为「整包附件也要在库」，则降为「保留」或先补拷附件。

### 3.2 建议先归档再删（15 项，约 13.5 MiB）

按优先级：

1. **P0 — DEV-V2-25 Scope 4 三包**（3.4 MiB）：210433 / 225112 / 225127 → `audit/2026-09-09/DEV-V2-25/evidence/scope4-raw/`（见 §2.4）
2. **P1 — DEV-V2-24 夜场诊断矩阵**（票面「13 包 assembly-identity」仍缺原文）：195933+195934、204042、210425（建议连带未引用的成对客机 204026 一起收）。落位：`audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/diag-night/`
3. **P1 — F-B1b / litfb8 SP**：000640、001648 → `…/cases/sp/logoutput-v4f1-000640.log` 与 `logoutput-litfb8-001648.log`
4. **P1 — v5 存活确认原文**（结单引用、cases 未收全文）：081731、083303、083313。081724 为 081731 近双份，可只收 081731
5. **P1 — F-E / litfb9 U3DS**：113740、120001 → `…/cases/u3ds/`
6. **P2 — DEV-V2-15 单人验收原文**：183015 → `audit/2026-09-06/DEV-V2-15/evidence/sp-183015/`（验收文档目前只指向 UMM 绝对路径）

### 3.3 拿不准、建议保留（10 项，约 11.9 MiB）

| 项 | 原因 |
|---|---|
| `20260905_004017` | 无票面引用；09-05 00:40 四插件会话，可能是 V2-11 与 V2-12 之间的非正式轮 |
| `20260907_233302.zip` | 压缩包，内容未入库、时间戳未引用；解压后 LogOutput 1.1 MiB 未知轮次 |
| `20260907_233517` + `233519` | 09-07 夜 host/client 对，**无 BUE 插件**（仅 SPF）；最大单包 4.8 MiB；可能是非 BUE 对照/联机噪声 |
| `20260908_180432` + `180438` | 18:04 host/client，BUE sha `3CBD6268…`，票面未点名 |
| `20260908_190250` + `190251` | 19:02 对；主机 sha `3513C7D2…` 与客机 `3CBD6268…` 不对齐，或为构建切换轮 |
| `20260908_204026` | 与已引用的 204042 成对客机，票面漏写时间戳；**建议升格进 §3.2 P1 与 204042 同收**，在升格前先留 |
| `20260909_081724` | 与已引用 081731 同 sha、同机、LogOutput 仅差 ~2 KB 的近双份；留一份防 081731 损坏即可 |

### 3.4 可释放空间估算

| 动作 | 释放量 |
|---|---|
| 只删 §3.1 已归档 44 项 | **~29.4 MiB** |
| 先归档 §3.2 再删 15 项 | 再 **~13.5 MiB**（仓库会多占同等数量；净释放≈0，UMM 工作目录腾出 13.5 MiB） |
| 保留 §3.3 | 0（11.9 MiB 继续占 UMM 目录） |
| 诊断相关理论上限（69 项全清） | **54.7 MiB** |
| 勿动 | `UnturnedModManager.exe` 73 MiB + pdb 0.2 MiB |

整目录 129 MiB 中诊断约占 55 MiB、启动器本体约占 73 MiB。清理诊断包**不会**把目录压到远小于 ~75 MiB。

---

## §4 方法与限制

- **只读**：对 UMM 目录仅 `ls` / `find` / `du` / `sha256sum` / `head` / `tail` / `rg` / `unzip -l`。零删除、零移动、零改写。
- **对照口径**：
  1. 已归档 = 仓库 `audit/**` 存在同大小且 SHA-256 相同的 `LogOutput.log`（或 `logoutput-*.log` / `U3DS_*LogOutput.log`），并有 case.md / verification.md 绑定时间戳或目录名。
  2. 仅引用 = `audit/` 或 `.scratch/` 的 `.md/.txt` 命中包名时间戳（完整 `YYYYMMDD_HHMMSS` 或 HHMMSS），但无同 SHA 全文。
  3. 未归档 = 上述两路皆无。
- **SHA 比对范围**：UMM 全部 62 个 `LogOutput.log` + 6 个散落 U3DS；仓库 `audit/` 下全部 `LogOutput*.log` / `logoutput*.log`。未对 `Client.log`、截图 png、zip 内部做哈希对照。
- **HHMMSS 误判风险**：6 位时间戳可能撞其他日志（例：`000024` 亦出现在 `Client_Prev.log` / `auto-dryrun/server-stdout.log`）。本清单对 09-04～09-07 包优先用**完整日期+SHA**，避免单靠 HHMMSS。`132622` 亦命中 `audit/2026-08-30/Client.log` 字符串，归档判定仍以 SHA 对准 coexist-b host 日志为准。
- **「已归档」不等于「整包入库」**：典型 cases 只收 `LogOutput.log`（及少量 fingerprint/截图）。包内 Client.log、d3d11/dxgi、摘要 txt 多数未拷。删 UMM 包会丢掉这些附件。
- **端别推断**：摘要来源盘符 + 日志 `[[Host]]`/`[[Client]]` + `peer scope`/`CLIENT_CONNECT`。SteamP2P 在客机启动时也会打 `[[Host]] [WorldBroadcast] activation pending`，故不以该行单独定端。
- **zip 未深挖**：`20260907_233302.zip` 只 `unzip -l`，未解压、未对内部 LogOutput 做 SHA（避免在 UMM 目录写文件）。
- **U3DS v7 服务端**：仓库有 `logoutput-v7-u3ds-server-20260909-1300.log`（47367 B），UMM 目录无对应散落文件——服务端日志本来就不是 UMM 诊断包导出通道。
- **盘点截止**：2026-09-09 15:19 目录快照（当时最新包 `151902`）。之后新导出的包不在本清单。


## §5 清理执行记录（2026-09-09 傍晚，用户委托执行）

- 已删 **60 项**（54 个诊断包文件夹 + 6 个散落 U3DS 日志 = §3.1 的 44 项 + §3.2 补归档后的 15 项 + 升格的 204026）：**删除前逐项 SHA-256 比对 UMM 原件与库内归档副本，60/60 零差异后才执行**（含 §2 三包、§1 各表全部锚点）。
- 剩余诊断项恰为 §3.3 保留清单 9 项：004017、233302.zip、233517、233519、180432、180438、190250、190251、081724（保留与否待用户后续拍板）。
- UMM.exe + pdb 未动；目录 129 MiB → 85 MiB（释放约 44 MiB）。
