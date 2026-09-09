# DEV-V2-25 Scope 4 证据 A/B 原始包补归档（2026-09-09）

来源：UMM 工作目录 `D:\Agent-工作目录\...\UMM-v2.2.1-win-x64\UMM-诊断包_*` 整包拷贝（含 LogOutput/Client.log/诊断摘要/d3d 附件）。
背景：scope4-evidence-check.md §0 原判「未归档入库」系检索遗漏 UMM 目录（当时只搜了库内/git/游戏目录/TEMP）；本目录补收后缺口关闭。盘点与哈希核对见 `.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md` §2。

| 目录 | 原包 | 端别/构建 | LogOutput SHA-256（前 16） | 关键锚 |
|---|---|---|---|---|
| host-210433-litfb4/ | UMM-诊断包_20260908_210433 | 主机 litfb4（CFE5080F…），litfb3 探针残留 | 20c7920c2c745f3d | 证据 A：定向发送未送达 ×1199（:1301 起） |
| host-225112-litfb7/ | UMM-诊断包_20260908_225112 | 主机 litfb7（FAF3C0A6…） | 957ac3b033ad6d4a | 证据 B：×1057 |
| client-225127-v3/ | UMM-诊断包_20260908_225127 | 客机候选 v3（C9B6B6E4…） | 7801f31c11d622c4 | :1111 challenge gen=2 应用（缺口核心行） |

（.log 按 .gitignore 全局规则磁盘归档不入 git——与 cases/ 下全部已归档日志同口径。）
