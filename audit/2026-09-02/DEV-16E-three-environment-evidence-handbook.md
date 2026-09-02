# DEV-16E 三环境真实运行证据采集手册

> CaseId：`DEV-16E-20260902` · CandidateBuild：`DEV-16E-CLEAN-20260902`
> 工单：`.scratch/better-unturned-experience-architecture/issues/05-dev-16e-runtime-evidence-qualification.md`（claimed）
> 状态：单人 ✅ 已确认；**待你实机采集 SteamP2P Host/Client 与 U3DS Headless**。

## 1. 候选身份（三环境必须绑定同一份）

| 项 | 值 |
|---|---|
| 候选 DLL | `audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll` |
| SHA-256 | `6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C` |
| BuildIdentity | `600A6926F0AA47ADA0902AA5D64F69E2BBFE2F10FDA817F99C2FEC039FC9B26A` |
| SourceSnapshotId | `61737df` |
| CaseId | `DEV-16E-20260902`（三环境共用，不得更换） |
| CandidateBuild | `DEV-16E-CLEAN-20260902` |

> 部署目标：`E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll`（先删旧 BUE DLL：r5-band、r4-edgefix、r3-fix、r2、r1、1925）。
> 部署后核对：`Get-FileHash -Algorithm SHA256 -LiteralPath "E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll"` → 期望 `6ABB7E0D...`。

## 2. 单人（✅ 已完成，2026-09-02 确认）

用户确认"功能没什么异常"；R6 诊断包 `UMM-诊断包_20260902_133050` 已留存（部署 DLL hash `6ABB7E0D...` 匹配）。单人证据案例已归档待用。

## 3. SteamP2PFriends Host / Client（本地联机，待采集）

**目的**：证明同一候选在同一 DLL 哈希下，Host 与 Client 双端拖入 + 投影收敛均正常，且时间窗重叠。

### 准备（双开或双机均可，建议双开同机更可控）

1. 两台实例都部署同一 DLL（同一 SHA-256），都记录 `CaseId=DEV-16E-20260902`。
2. **开启服务器窗口的 UTC 时钟**：在游戏内或系统托盘记录开始/结束的 UTC 时间戳（PowerShell 可执行 `Get-Date -AsUTC` 截图）。
3. 本地联机方式：主菜单 → 多人 → 创建服务器（Host），另一实例通过 LAN/好友加入（Client）。

### 每端验证步骤（各约 5 分钟）

| 步骤 | Host | Client |
|---|---|---|
| 1. 进图后按 G 打开背包，确认管理面板可开、设置开关（增强预览/自动旋转）可切换 | ☐ | ☐ |
| 2. 从背包拿起物品 → 拖入普通容器 → 绿框跟随 → 松手 → 物品落位、方向与预览一致 | ☐ | ☐ |
| 3. 从容器拿起 → 拖回背包 → 同上 | ☐ | ☐ |
| 4. 拿起横放武士刀 → 确认拿起即有绿框/图标（rotgrab 修复点）→ 拖到左壁转竖（边缘感应带） | ☐ | ☐ |
| 5. 关闭增强开关 → 拖拽恢复原生 → 重新开启 → 恢复增强 | ☐ | ☐ |
| 6. 观察对方操作是否影响己方（网络投影收敛：物品最终位置双方一致） | ☐ | ☐ |

### 记录要求（每端一份）

- 环境指纹：操作系统、Unturned 版本（3.26.3.10）、BepInEx 版本（5.4.23.5）、SteamP2PFriends 传输标识（局域网/Steam 好友）。
- 部署来源：DLL 路径 + SHA-256。
- **UTC 时间窗**：Host 与 Client 的开始/结束时间必须重叠（严格正交重叠，不是首尾相触）。
- 原始日志：`BepInEx/LogOutput.log`（两端各存一份，命名 `p2p-host-<UTC开始>.log` / `p2p-client-<UTC开始>.log`）。
- 诊断包：UMM 导出（两端各一份）。
- 截图/录像：绿框跟随、松手落位、方向一致各 1-2 张；录像引用路径。
- 每个文件的 SHA-256 记录。

## 4. U3DS Headless（多人服务端，待采集）

**目的**：证明同一主 DLL 在 U3DS（BatchMode/Headless）下能启动/运行/关闭，且**不创建 UI、不安装客户端 Hook、不解析客户端表现层**。

### 准备

1. U3DS 服务器（BatchMode 无头模式）部署同一 DLL（同一 SHA-256）。
2. 用同一 `CaseId=DEV-16E-20260902` 记录。

### 验证步骤

| 步骤 | 结果 |
|---|---|
| 1. 服务器启动成功，BUE 日志出现 bootstrap/headless 分流记录（无 ClientUi 实例化） | ☐ |
| 2. 运行数分钟无异常、无 UI 类型解析错误、无客户端 Hook 安装 | ☐ |
| 3. 玩家通过普通客户端连接服务器，正常游玩、库存操作由服务端权威处理 | ☐ |
| 4. 正常关闭服务器（clean shutdown），无崩溃 | ☐ |
| 5. 日志中确认：无 `Glazier`/`Sleek`/`PlayerUI` 客户端类型访问；U3DS 侧无增强预览图元创建 | ☐ |

### 记录要求

- 环境指纹：U3DS 运行方式（专用服务器 / 启动参数）、Unturned 版本、BepInEx 版本（U3DS 通常 5.4.22.0）、平台。
- 部署来源：DLL 路径 + SHA-256（与客户端**同一文件**，不得重新构建）。
- 命令/步骤：服务器启动命令、进入/退出命令。
- UTC 时间窗：启动/运行/关闭时间戳。
- 原始日志：U3DS 的 `LogOutput.log`（或控制台输出文件）。
- 截图/录像：服务器控制台、客户端连接截图。
- 每个文件的 SHA-256 记录。

## 5. 证据包提交格式（采集后交给我）

请在每个环境采集完成后，按以下结构整理（或直接给原始日志路径，我来组装证据包）：

```
evidence/DEV-16E-20260902/
├── sp/            （单人，已完成）
├── p2p-host/      （Host 日志+诊断包+截图）
├── p2p-client/    （Client 日志+诊断包+截图）
└── u3ds/          （U3DS 日志+截图）
```

每个子目录内附 `manifest.json` 或 `README.md`，至少包含：环境指纹、版本、部署来源（DLL hash）、命令/步骤、UTC 时间窗、原始日志路径、截图/录像引用、文件 SHA-256。

收到后我将：
1. 用 `RuntimeEvidencePackageValidator` 校验包完整性（Candidate/DLL/CaseId/路径/摘要一致）。
2. 用 `QualificationEvidenceGate` 导入并裁决：同候选同哈希下 SP + P2P（同 CaseId 重叠时间窗）+ U3DS Headless 全部 Fulfilled → `TechnicallyQualified`；否则 `QualificationIncomplete`（Fail-Closed）。
3. 输出资格裁决 + 交 Gemini 前端消费复核 + 人工开发者批准。

## 6. 边界（工单 L22 硬约束）

三环境证据只证明"同候选同哈希下可运行且增强拖入/投影收敛正常"，**不自动授予**发布授权、Stable 或 ReleaseReady；正式发布仍由人工开发者批准具体 BuildIdentity/LoadSetIdentity/DLL 哈希。
