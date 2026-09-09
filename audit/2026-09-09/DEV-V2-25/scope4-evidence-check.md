# DEV-V2-25 Scope 4 — 证据 B 轮 LHT/LIR 同期发送结果核对（通道选择性结论）

- 日期：2026-09-09
- 票面条款：Scope 4「证据 B 轮 LHT/LIR 同期发送结果核对（扩大或收窄『通道选择性』结论）」
- 性质：研究项（不改生产代码），结论回填票面 Comment 与本文件

## 0. 原始证据可得性（缺口登记 → 2026-09-09 傍晚已补收关闭）

- 证据 A/B 所引诊断包 **20260908_210433 / 20260908_225112 / 20260908_225127**（litfb7 轮，09-08 夜）系机器侧 UMM 诊断导出。**原判「未归档入库」时声称全盘搜索无果——该判断有误**：当时检索范围为库内文件名/git 全历史/游戏目录/TEMP，漏检了 UMM 工作目录（`D:\Agent-工作目录\...\启动器\UnturnedModManager\publish\UMM-v2.2.1-win-x64\UMM-诊断包_*`，诊断导出的实际落盘点）。2026-09-09 傍晚 UMM 目录盘点（`.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md` §2）发现三包完整在位，当场复核量化（×1199/×1057/225127:1111）与票面一致，**已整包补归档至 `evidence/scope4-raw/`（host-210433-litfb4 / host-225112-litfb7 / client-225127-v3，含 SHA 核对，见该目录 README）——本缺口关闭**，证据 B「challenge 曾送达后链路中途劣化」读法现可从库内原始数据重放。
- 等效核对（本文件 §1-§3）继续有效：库内归档的 DEV-V2-24 v6/v7 P2P 主机+客机完整日志与原始包结论互相印证。

## 1. 证据源（库内归档）

| 轮次 | 文件 | 说明 |
|---|---|---|
| v6 主机 | `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-host/logoutput-v6-20260909-112239.log` | 26117 行，F-C ×8333 |
| v6 客机 | `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-client/logoutput-v6-20260909-112244.log` | challenge gen=2/3/4 应用 |
| v7 主机 | `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-host/logoutput-v7-20260909-125324.log` | 7807 行，F-C ×1650 |
| v7 客机 | `audit/2026-09-08/evidence/DEV-V2-24-20260908/cases/p2p-client/logoutput-v7-20260909-125312.log` | challenge gen=2 应用 |

## 2. v7 主机时间线（1650 条风暴的全窗口解剖）

| 行号 | 事件 |
|---|---|
| 2491 | `[TidyFault] peer scope 已开启（peer=76561199721762479, generation=2）` |
| 2492–4199 | `定向发送未送达（generation=2, result=LocalTransportUnavailable）` ×1650（Error 级直出，逐帧一条） |
| 4200 | `[Info:SteamP2PFriends] [[Host]] [P2P-Connection] event=HOST_AUTHENTICATE_RECEIVED t=193.208s …`（**引擎级 P2P 认证完成拍**，紧贴风暴结束） |
| 6173/6179/6184 | `[TidyNet] -> 客机 TidyCommitted(reqId=1/2/3, result=Committed, mappings=0)`（**同会话 gen=2** 可靠定向发送成功） |
| 6580–6987 | `[HordeNet] 广播 Update: epoch=2/3/4 … result=Sent` ×18（LHT 同会话广播全成功；epoch=1 seq=1/2 行 1640/1647 为会话建立前 `result=NoSession`，属会话前置 fail-fast，非传输拒绝） |
| 7387 | `[TidyNet] 会话代际已清（… generation=2）`（退出清理） |

- v7 客机（125312）行 533：`已接收并应用服务端会话 challenge（generation=2）`——gen=2 会话最终握手成功且全程未更替。
- 判读：**风暴=「BUE 会话采纳先行、引擎级 P2P 认证未完成」的传输就绪窗**。窗内出向通道整体拒绝；认证完成拍后，同一会话、同一频道的 LIT 响应/LHT 广播全部恢复。

## 3. v6 主机逐代际风暴与同期成功发送

| 代际 | 风暴行号区间 | 条数 | 同期/紧随的成功发送 |
|---|---|---|---|
| gen=2 | 2364–3019（600）+ 3167–4707（1420） | 2020 | 行 5576/5584/5586 `-> 客机 TidyCommitted(reqId=1/2/3)`（**恰在 gen=2 风暴与 gen=3 风暴之间的间隙**）；v6 客机行 376 challenge gen=2 应用 |
| gen=3 | 6353–13231 | 2808 | v6 客机行 999 challenge gen=3 应用 |
| gen=4 | 14990–24135 | 3505 | v6 客机行 1627 challenge gen=4 应用 |

- v6 三个代际的 challenge **最终均送达并被客机应用**（三次网络整理全链成功 reqId=1/2/3）——即风暴不是死代际永久拒绝，而是每个代际诞生窗的就绪等待，重臂最终成功。
- 本轮 LHT 广播/LIR 压弹在 v6 主机未触发（0 条广播行；压弹命中冷却窗口跳过），故 v6 轮不构成 LHT/LIR 同期样本——同期样本由 v7 轮 LHT ×18 Sent 承担。

## 4. 结论（对票面「通道选择性」的收窄）

1. **失败呈时间窗选择性，而非按消息种类（LIT/LIR/LHT）或频道的永久选择性**：v7 轮同一会话、同一频道上，窗内 challenge 被拒 ×1650，窗外（引擎认证完成拍后）TidyCommitted 与 LHT 广播全部 Sent。
2. 机上四轮量化（1199/1057/8333/1650 条）形态一致：每段风暴对应一个会话代际的就绪/劣化窗，窗终止即恢复，与「每帧一条 WARN 的逐拍自旋」机制吻合。
3. 证据 B（225112/225127）「challenge 曾送达后链路中途劣化」与该机制兼容（劣化窗可在会话中途出现），但原始包未归档无法复核——**此项作为具名缺口保留**，不影响本票修复面（退避+限频+结构性诊断对两种窗形态同样成立）。
4. 修复面有效性依据：就绪窗典型时长为秒级~分钟级，退避表 {0,1s,2s,4s,8s(封顶)} 在 7 分钟模拟窗内将尝试从 8333 压到 55、WARN 从 8333 压到 2（harness 组断言），且首重试立即位保持 F-A「一帧抖动下一拍恢复」契约。

## 5. 复核命令留档

```
grep -n "定向发送未送达" <v6-host-log> | awk -F: '{print $1}'   # 风暴区间聚类
grep -n "HOST_AUTHENTICATE_RECEIVED" <v7-host-log>              # 引擎认证完成拍
grep -n "广播 Update" <v7-host-log> | head -20                  # LHT result=Sent
grep -n "已接收并应用服务端会话 challenge" <v6-client-log>       # 三代际 challenge 应用
```
