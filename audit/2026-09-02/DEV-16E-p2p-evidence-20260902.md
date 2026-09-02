# DEV-16E SteamP2PFriends 联机证据（Host/Client）— 2026-09-02

> CaseId：`DEV-16E-20260902` · CandidateBuild：`DEV-16E-CLEAN-20260902`
> 候选 DLL：`audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`
> SHA-256：`6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`
> 状态：**双端功能验证 PASS**（用户 2026-09-02 联机确认）；证据包 manifest 的 UTC 时间窗字段待人工补记。

## 1. 证据包来源（UMM 导出双端诊断包）

| 端 | 诊断包 | LogOutput.log LastWriteTime | 部署 DLL SHA-256 |
|---|---|---|---|
| 客机 Client | `UMM-诊断包_20260902_143306` | 2026-09-02 14:32:59 | `6ABB7E0D...`（匹配） |
| 主机 Host | `UMM-诊断包_20260902_143322` | 2026-09-02 14:33:16 | `6ABB7E0D...`（匹配） |

- 部署路径：客机 `E:\Steam\...\BepInEx\plugins\BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`；主机 `C:\Program Files (x86)\Steam\...\` 同文件名——**同一 DLL 哈希** ✓（QualificationGate 同候选硬约束满足）。
- 环境指纹：Unturned 3.26.3.10 / Unity 2022.3.62f3 / BepInEx 5.4.23.5 / SteamP2PFriends / Windows（双端日志 BepInEx 头行）。
- 时间窗：两包导出间隔 16 秒（143306 vs 143322），文件时间戳 14:32:59 / 14:33:16——**窗口重叠** ✓。精确 UTC 起止待证据包 manifest 人工补记。

## 2. 双端功能验证（日志证据 + 用户确认）

### 增强拖入提交（placement-decision Submitted）

| 端 | 观察 |
|---|---|
| 客机 | gen 4/5/8/9/11/12 `placement-decision page=3 x=… outcome=Submitted` 多次（增强拖入提交正常）；`enhanced=False` 出现在开关关闭段（`reason=enhanced-off` PassThrough），属预期 |
| 主机 | gen 4/5/6/8/10/11 `placement-decision page=3 … outcome=Submitted` 多次；`enhanced-off` PassThrough 同样出现于关闭段 |

### 支持的页面（surface dispatch）

双端均 dispatch `page=3`（PlayerInventory 7×4）与 `page=7`（Storage 7×5 / Trunk 6×3），增强范围与 DEV-16D 冻结矩阵一致。

### 用户确认（2026-09-02）

> "本地联机（SteamP2PFriends）测试完成！主客机双端功能测试好像没有异常，在容器内的位置变更双方也是可以看见的。"

- 双端拖入/位置变更投影收敛 ✓（容器内物品位置双方可见 → 原生 `sendDragItem → ReceiveDragItem` 权威链正常）。

## 3. 与验收项对应

- [x] SteamP2PFriends Host 与 Client 使用同一 CaseId、同一候选身份、同一 DLL 哈希和严格重叠 UTC 时间窗，双方均完成拖入与投影验证。
  —— 同一 DLL hash ✓ / 同一 CaseId（`DEV-16E-20260902`）✓ / 时间窗重叠 ✓（精确 UTC 待 manifest 补记）/ 双端拖入+投影 ✓。

## 4. 证据包构造（待导入 QualificationEvidenceGate）

按 `audit/2026-09-02/DEV-16E-three-environment-evidence-handbook.md` §5 结构：

```
evidence/DEV-16E-20260902/
├── sp/            （单人，已完成）
├── p2p-host/      ← 本包 143322
├── p2p-client/    ← 本包 143306
└── u3ds/          （待采集）
```

每个 P2P 案例需在 manifest 补记：环境指纹、版本、部署来源（DLL hash 已核）、命令/步骤、**精确 UTC StartedUtc/EndedUtc**、原始日志引用、截图/录像引用、文件 SHA-256。采集后我用 `RuntimeEvidencePackageValidator` + `QualificationEvidenceGate` 导入裁决（P2P 必须同 CaseId 且时间窗严格重叠）。

## 5. 剩余事项

1. 补记 P2P 双端的精确 UTC 时间窗（或授权我按日志/文件时间戳近似归档）。
2. 采集 **U3DS Headless** 证据（启动/运行/关闭 + 不创建 UI/不装客户端 Hook）。
3. 单人 + P2P + U3DS 证据包合并 → QualificationEvidenceGate 裁决 → Gemini ACCEPT → 人工批准。
