# DEV-V2-07 三环境网络层实机验证采集手册

> CaseId：`DEV-V2-07-20260904` · CandidateBuild：`DEV-V2-07-CLEAN-20260904`
> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-07-three-env-network-validation.md`（claimed）
> 状态：候选已构建、身份已授予、证据包骨架与判读 runner 就绪；**待你按本手册实机采集三环境证据**。
> 前置门禁全绿：Release 重建 0 error/0 warning、七测试运行器 exit=0、NoUiTokens（Core+ClientUi）PASS、`git diff --check` 0。

## 1. 候选身份（三环境必须绑定同一份）

| 项 | 值 |
|---|---|
| 候选 DLL | `audit/2026-09-04/artifacts/DEV-V2-07-20260904/BetterUnturnedExperience.dll`（266752 字节） |
| SHA-256 | `14A98FC838B343FAC68DAFE3B1A8224C5A2484E7A211E9E24E1973E0B6EA5EF6` |
| BuildIdentity | `85FAEA1729C51C76831D069E76B45605B1D6FC0D7D92D60D7F644D903AA0A53D` |
| SourceSnapshotId | `ba7ecd9c7a82b6201f61f652c2df9ada6ffdf063`（= DEV-V2-06 提交 `ba7ecd9`，**本票源码零修改**） |
| DefinitionSetDigest | `C545D9AC8766E149C0A1B10474FDD6B23B4C9F48F21A8564FC0AD2AD27DC99A0` |
| ReferenceSet（Client+U3DS） | `Libs-ReferenceSet-951EFCD4E73C37E2D514B6B7D05AE8FDF2141F3C18A9C60D37192BD068775030` |
| ToolchainIdentity | `MSBuild-18.9.0.32302\|.NETFramework-4.7.2\|CSharp-10` |
| CaseId | `DEV-V2-07-20260904`（三环境共用，不得更换） |
| CandidateBuild | `DEV-V2-07-CLEAN-20260904` |

**配套测试装备**（随候选一并部署、一并记录哈希；fixture 是普通 LMN 消费方，不引用 BUE）：

| 件 | SHA-256 | 来源 |
|---|---|---|
| `LmnEcosystemFixture.dll` | `2B82114F12ABD25C93EDD5957FDD510C2E3DF25824BA264EF4AAF419F3BA7096` | `audit/2026-09-04/DEV-V2-07/kit/out/` |
| `LaunchMultiplayerNet.dll`（独立 LMN v5.0.0.0） | `06D8A45438C09FEA65F3800BF01A7EFB9302F2421BD76AA386F8828701A63055` | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet\bin\Release\` |

## 2. 部署准备（每次换配置前：完全退出游戏）

1. **客户端** `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`：
   - 删除旧 `BetterUnturnedExperience.dll.disabled`（历史遗留，避免混淆）；
   - 复制候选 DLL 为 `BetterUnturnedExperience.dll`；
   - 核对：`certutil -hashfile BetterUnturnedExperience.dll SHA256` → 期望 `14A98FC8...5EF6`。
2. **U3DS** `E:\Steam\steamapps\common\U3DS\BepInEx\plugins\`：删除旧 `BetterUnturnedExperience.dll`，复制同一候选文件（**同一文件，不得重新构建**），同样 certutil 核对。
3. **开启 Debug 日志（关键，不做则 BUE-V2NET 运行时行不可见）**：两端 + U3DS 的 `BepInEx\config\BepInEx.cfg` → `[Logging.Disk]` → `LogLevels = Fatal, Error, Warning, Message, Info, Debug`。
4. **配置 B 追加部署**：`LaunchMultiplayerNet.dll` + `LmnEcosystemFixture.dll` 放入同一 plugins 目录，分别 certutil 核对上表哈希。
5. `SteamP2PFriends.dll` 留着不动（无 BepInPlugin 属性，休眠件），在环境指纹里记录即可。

**两种配置**：

| 配置 | plugins 内容 | 用途 |
|---|---|---|
| A（零误报基线） | 仅候选 BUE | 单人 + U3DS 短冒烟：无 LMN 时零 patch、零反射、零干扰 |
| B（接管矩阵） | BUE + LMN + fixture | 单人完整 + P2P 双端 + U3DS 完整 |

## 3. 单人

**配置 A（短冒烟，约 3 分钟）**：

| 步骤 | 期望 | ☐ |
|---|---|---|
| A1 启动进世界 | `LogOutput.log` 有「Better Unturned Experience 加载成功，界面已注入」；**无任何** `takeover-patch` 行（探针 false → 零 patch 零反射） | ☐ |
| A2 查 Debug 行 | 有 `[BUE-V2NET] event=lmn-config-migration result=no-op mapping=empty reason=lmn-has-no-config diagnosticId=BUE-V2NET-001` | ☐ |
| A3 零误报 | 无 `BUE 错误：` 行；原版玩法（移动/交互/背包）无任何异常 | ☐ |

**配置 B（完整，约 10 分钟）**：

| 步骤 | 期望 | ☐ |
|---|---|---|
| B1 启动进世界 | A1 + 新增 `[BUE-V2NET] event=takeover-patch result=installed priority=first targets=NetMessages.ReceiveMessageFromClient,NetMessages.ReceiveMessageFromServer diagnosticId=BUE-V2NET-003` | ☐ |
| B2 fixture 就绪 | `[LMNFIX] ready fixture=0.1.0 ... lmnOperational=True channels=v1-server=ok;v1-client=ok;v2-server=ok;v2-client=ok;`；LMN 自身 `[ModTransport] server handler registered: channel=250` 与 `[NamespacedTransport] ... guid=io.github.yu80rice.bue-fixture.named` 注册行 | ☐ |
| B3 回环收发 | 每 10 秒一组：`[V1FIX] send-broadcast ...` + `[V1FIX] recv-from-server kind=ping ...` + pong 往返；`[V2FIX]` 同构（本地主机回环走 LMN 路由器） | ☐ |
| B4 面板接管卡 | G 开背包 → 管理面板 → 网络 entry：「接管状态：已由 BUE 接管」+「配置迁移：无独立配置可迁移(LMN 无配置文件)」（半角括号，与源码串逐字一致）→ **截图** | ☐ |
| B5 可逆钮 | 点「让我改回独立 LMN」→ 日志 `takeover-patch result=removed decision=hand-back-to-lmn`、面板变「已改回独立 LMN(BUE 网络模块已停用)」；V1/V2 回环仍工作（LMN 独立恢复）→ **截图** | ☐ |
| B6 重臂 | 面板把网络模块开回来 → `takeover-patch result=installed` 再次出现，面板恢复「已由 BUE 接管」 | ☐ |

## 4. SteamP2PFriends Host / Client（配置 B，双端）

**准备**：双端部署同一候选 + 同一 LMN + 同一 fixture（哈希逐一核对）；主菜单 → 多人 → 创建服务器（Host），另一实例好友/LAN 加入（Client）。开始/结束各记一次 UTC 时间（PowerShell `Get-Date -AsUTC` 截图）——**Host 与 Client 时间窗必须正交重叠**（不是首尾相触），双端共用 `CaseId=DEV-V2-07-20260904`。

| 步骤 | 期望 | Host ☐ | Client ☐ |
|---|---|---|---|
| P1 加入后日志 | `takeover-patch result=installed` + `lmn-config-migration result=no-op` + `[LMNFIX] ready` | ☐ | ☐ |
| P2 面板接管卡 | 「已由 BUE 接管」+「无独立配置可迁移」行 | ☐ | ☐ |
| P3 V2 命名频道互通 | Client：`[V2FIX] send-to-server seq=N` → Host：`[V2FIX] recv-from-client sender=<ClientSteamId> seq=N` + `[V2FIX] send-pong target=<ClientSteamId> seq=N` → Client：`[V2FIX] recv-from-server kind=pong seq=N`（**seq 双端对齐**） | ☐ | ☐ |
| P4a V1 兼容 Step A（不做任何面板操作直接观察） | Client：`[V1FIX] send-to-server seq=M` → Host：应有 `[V1FIX] recv-from-client sender=... seq=M`。若 Host 无 recv（Debug 下见 `event=unknown-channel-dropped channel=250 ... diagnosticId=BUE-V1COMPAT-001`）→ **如实记录结果，继续 P4b**（镜像时机判别点；注意 **P4a 失败 ⇒ V1 兼容开箱不成立 ⇒ 资格裁决非 Fulfilled**，须走 real-machine-test-loop 修复轮出新候选后重采，P4b 通过只算修复验证证据） | ☐ | ☐ |
| P4b V1 兼容 Step B（面板刷新触发重镜像） | Host 打开管理面板 → 网络 entry →「让我改回独立 LMN」关 → 再开 → 等一个 10 秒周期 → Host 是否出现 `[V1FIX] recv-from-client seq=M'`。A/B 两步结果都记录 | ☐ | — |
| P5 零误报 | 全程原版玩法正常（移动/物品/同步），无 `BUE 错误：` 行 | ☐ | ☐ |
| P6 双端互不串扰 | 对方操作不影响己方界面；无异常帧错误日志 | ☐ | ☐ |

## 5. U3DS Headless

**配置 A（短冒烟）**：

| 步骤 | 期望 | ☐ |
|---|---|---|
| U-A1 启动 | 服务器控制台/`Logs` 日志出现「Better Unturned Experience 加载成功（无界面）」；**无** `takeover-patch` 行、**无** `BUE-V2NET-002` 故障行 | ☐ |
| U-A2 无 UI 实例化 | 无 `ClientUi`/`PlayerUI`/`Sleek`/`Glazier` 相关报错或 Hook 安装行 | ☐ |
| U-A3 本地功能不受网络影响 | 普通客户端连入后，客户端 BII（增强拖拽/面板）正常——网络模块无 LMN 可接管时零干扰 | ☐ |

**配置 B（完整）**：

| 步骤 | 期望 | ☐ |
|---|---|---|
| U-B1 启动 | U3DS 日志出现 `takeover-patch result=installed`（U3DS 无 UI，接管证据以日志为准）+ `[LMNFIX] ready` | ☐ |
| U-B2 周期广播 | 每 10 秒 `[V1FIX] send-broadcast ...` + `[V2FIX] send-broadcast ...`（U3DS 侧 isServer） | ☐ |
| U-B3 客户端互通 | 客户端（部署候选 BUE；可带 LMN+fixture）连入：U3DS 日志 `recv-from-client`、客户端日志 `recv-from-server kind=pong`，seq 对齐 | ☐ |
| U-B4 BII 不受影响 | 客户端 BII 增强拖拽正常；U3DS 无 UI 报错 | ☐ |
| U-B5 clean shutdown | 控制台正常退出，无崩溃、无未处理异常 | ☐ |

## 6. 预期日志行速查表

| 行 | 级别 | 何时出现 |
|---|---|---|
| `Better Unturned Experience 加载成功（无界面）` | Info | U3DS 每次启动 |
| `...加载成功，界面已注入` | Info | 客户端/单人每次启动 |
| `[BUE-V2NET] event=lmn-config-migration result=no-op ... BUE-V2NET-001` | **Debug** | 每环境每次启动（网络模块注册时，幂等空跑） |
| `[BUE-V2NET] event=takeover-patch result=installed ... BUE-V2NET-003` | **Debug** | 仅当独立 LMN 已加载且网络模块开 |
| `[BUE-V2NET] event=takeover-patch result=removed decision=hand-back-to-lmn` | **Debug** | 可逆钮 / 网络开关关闭时 |
| `BUE 错误：[BUE-V2NET] ... result=failed ... BUE-V2NET-002` | Error | 仅故障时（任何出现都记为 finding） |
| `event=unknown-channel-dropped channel=250 ... BUE-V1COMPAT-001` | **Debug** | 仅 V1 帧到达但镜像缺该频道时（P4a 判别信号） |
| `[LMNFIX]/[V1FIX]/[V2FIX] ...` | Info | fixture 每次注册/每 10 秒收发 |

## 7. 证据提交结构（采集完放回仓库）

```
audit/2026-09-04/evidence/DEV-V2-07-20260904/
├── candidate/candidate.json                    （已就绪，勿改）
├── cases/sp/        case.json + evidence.log（该端 LogOutput.log 改名）+ diagnostics.zip* + screenshots-or-video.txt
├── cases/p2p-host/  同上
├── cases/p2p-client/ 同上
└── cases/u3ds/      同上（服务器 + 若有客户端连接日志一并放）
```

- 四份 `case.json` 模板已就位：把 `TODO` 字段填实（环境指纹/版本/部署来源含 DLL 哈希/命令步骤/UTC 窗口/采集人/诊断摘要）；`role` 已按 case 目录固定（sp=SinglePlayer、p2p-host=SteamP2PHost、p2p-client=SteamP2PClient、u3ds=U3dsHeadless），**采集时不得改动**；`startedUtc/endedUtc` 当前为 `TODO`（占位会被门禁拒绝，属预期 fail-closed），填实用 `DateTimeOffset.UtcNow` 的 `O` 格式（如 `2026-09-04T12:34:56.789+00:00` 写成 `2026-09-04T12:34:56.7890000Z`），Host 与 Client 两窗必须正交重叠。
- `diagnostics.zip`*：UMM 诊断导出若可用则附上；不可用就把该条目从 `extraArtifacts` 删除并在 `diagnosticSummary` 注明。
- `screenshots-or-video.txt`：逐行列出截图/录像文件路径与内容说明（面板接管卡、可逆钮前后、P3/P4 seq 对齐片段、U3DS 控制台）。
- 每个文件留 SHA-256（`certutil -hashfile <文件> SHA256`）记入 `screenshots-or-video.txt` 或报告。
- 采集完通知 agent，由 agent 运行资格门禁：
  `audit/2026-09-04/DEV-V2-07/kit/out/QualificationGateRunner.exe gate --package audit/2026-09-04/evidence/DEV-V2-07-20260904 --candidate audit/2026-09-04/evidence/DEV-V2-07-20260904/candidate/candidate.json`（exit 0 = TechnicallyQualified；2 = 包校验失败；3 = 资格未齐）。

## 8. 边界与失败处理

- 三环境证据只证明**同候选同哈希**下网络模块可运行、接管/零误报/V1 兼容/V2 委托行为正确；**不自动授予**发布授权、Stable 或 ReleaseReady——发布仍需人工批准具体 BuildIdentity/LoadSetIdentity/DLL 哈希。
- **BueNetworkApi（BUE2 帧）边界**：其真实传输驱动尚无生产消费者（官方功能尚未使用 V2 频道发消息）。本票「V2 命名频道互通」证据 = LMN V2 命名通道（LMN2 帧）经 BUE 接管决策点（Priority.First → LMN2 委托路径）双端互通；BueNetworkApi 本体以契约/运行时 seam 全绿 + 本边界声明入裁决。此边界会写入资格裁决报告。
- **任何一步与期望不符**：停止采集，保留现场（LogOutput.log + 截图 + 复现步骤），报告给 agent。按 real-machine-test-loop：agent 引日志行分析 → 修复走红测先行 + 双轴审查 CLEAN 后才出新候选 → 才通知复测。**不要现场改动或混采配置**。
