# POST-P4 可见修复候选 · 已批准发布

Status: **当前发布物**（2026-09-13 用户原话「三个功能都正常，人工复核无异议，准许发布候选」→ RELEASES 行 13 + `publish/第4阶段-正式交付版本/` DLL 换新）

| 项 | 值 |
|---|---|
| SHA-256 | `AF1F50C990954F315F0823BC9E39698979D5E491E25E2D8EB1682EC35A9D16D0` |
| 字节 | 641536 |
| 源基线 | `a5ed3fb` |
| CaseId | `POST-P4-CANDIDATE-20260913` |
| 确定性 | 3× `-t:Rebuild` 逐字节一致，exit 0，error=0/warning=0 |
| 全套 | 7/7 PASS |
| 契约 | 仍 2.1 零扩面 |

## 实机

- U3DS headless 负面 PASS：identity×1 绑本哈希、5×accepted、gate=Headless、LIT to=Running、posted=0、Error=0、[TidyUI]=0、心跳×2
- P2P 客机包 `UMM-诊断包_20260913_234709` / 主机包 `UMM-诊断包_20260913_234730`：双端 identity 全哈希一致、无旧 v5；host `TidyCommitted` reqId=1；runtime-arm client/server；enable-failed=0
- BUE [Error] 双端各 1 = noop-probe-error 设计内负探针；其余 = SPF P2P-UI / ResourceObs 另案
- 用户目视三处可见面（网络「可继续通信」/收藏双键/分类 chips）无异议

## 可见修复（进本 DLL）

1. POST-P4-01：`bue.network` Isolated →「可继续通信」，关开关；真正故障功能仍走 Q44
2. POST-P4-03：收藏星 / 「需要重启」按（种类, StableId）双键
3. POST-P4-04：Category chips + Item/Blueprint/Creature 列表文本路径进草稿

工程票 02/05/06/07/08 不改玩家画面，随同源。
