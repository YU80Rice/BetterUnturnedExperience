# DEV-V2-08 LIT/LIR/LHT 生态迁移实机验证采集手册

> CaseId：`DEV-V2-08-LIT-20260905` / `DEV-V2-08-LIR-20260905` / `DEV-V2-08-LHT-20260905`（每插件一个，采集期间不得更换）
> 工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-08-lit-lir-lht-migration-verification.md`（claimed）
> 交付报告：`Delivery-DEV-V2-08-ecosystem-kit-20260905.md`（身份/口径/门禁）
> 状态：交付 kit 五件就绪；**待你按本手册实机采集三环境证据**。
> 绑定：BUE = RELEASES 行 6 当前发布物 `35670269…aef6`（本票零修改；绑定口径按 RELEASES 注记 = DLL SHA-256 + 部署指纹，不立 BuildIdentity）。

## 1. kit 身份（三环境必须部署同一份，逐一 certutil 核对）

kit 目录：`audit/2026-09-05/DEV-V2-08/kit/`

| 件 | SHA-256 | 字节 | 角色 |
|---|---|---|---|
| `BetterUnturnedExperience.dll` | `3567026930757ffbc5d40b2da12b148f470dba806b5ca08abf14ac6e4a2caef6` | 275456 | V2 网络层（BUE 接管态） |
| `LaunchMultiplayerNet.dll` | `06d8a45438c09fea65f3800bf01a7efb9302f2421bd76aa386f8828701a63055` | 68096 | LMN v5.0.0.0（三插件硬依赖 shim + LMN2 编解码；**非独立网络提供方**，路由被 BUE 短路） |
| `LaunchInventoryTidy.dll` | `7e35d7c7b90c6e25003f0644f746451d6bb75c193e539e86114b37b0793a5417` | 151040 | LIT 验收候选（0.0.0） |
| `LaunchInPlaceReload.dll` | `6653035be15f5d88649e97442d9f41a55d9b4bc20bd96a37ac1434d25c68ac90` | 52736 | LIR 验收候选（0.0.0） |
| `LaunchHordeTracker.dll` | `6b935f5c47572004ffc8c0c1c448bae8d0a60c840b757faa5d263a75925ef995` | 36864 | LHT 验收候选（0.0.0） |

三插件的命名频道（LMN V2 规范，FeatureId 即频道名）：LIT `com.yu80rice.launchinventorytidy.net`、LIR `com.yu80rice.launchinplacereload.repack`、LHT `io.github.yu80rice.launchhordetracker.horde-status`。

## 2. 部署准备（每次换环境前：完全退出游戏）

1. **客户端** `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`：
   - **删除 `LmnEcosystemFixture.dll`**（DEV-V2-07 测试装备，避免与生态实装混采）；
   - 复制 kit 五件进目录（同名单一文件，**不得重新构建**）；
   - 逐一 `certutil -hashfile <件> SHA256` 与 §1 表核对，五件输出存 `deploy-fingerprint.txt`；
   - `SteamP2PFriends.dll` 留着不动，在指纹里记录即可。
2. **U3DS** `E:\Steam\steamapps\common\U3DS\BepInEx\plugins\`：同样清理 fixture、部署同一组五件、逐一核对并留指纹。
3. **开启 Debug 日志（关键，不开则接管/委托/广播锚不可见）**：客户端 + U3DS 的 `BepInEx\config\BepInEx.cfg` → `[Logging.Disk]` → `LogLevels = Fatal, Error, Warning, Message, Info, Debug`。
4. **加载顺序说明**：BepInEx 按硬依赖图排序——LMN 因三插件的 `[BepInDependency(Hard)]` 保证先于三插件加载；BUE（无依赖声明）按文件名 B < L 最先。无需任何手工干预。

## 3. 环境一：单人（约 15 分钟）

启动单人世界（有作弊权限便于 §4-4 调时间）。

| 步骤 | 期望 | ☐ |
|---|---|---|
| S1 启动进世界 | `LogOutput.log` 有「Better Unturned Experience 加载成功，界面已注入」+ **Debug** `[BUE-V2NET] event=takeover-patch result=installed priority=first targets=NetMessages.ReceiveMessageFromClient,NetMessages.ReceiveMessageFromServer diagnosticId=BUE-V2NET-003`（LMN 在场 → 接管必装）；LIT `[TidyNet] 已注册 LMN V5 命名频道=com.yu80rice.launchinventorytidy.net 双端处理器`；LIR `[Bootstrap] LMN V5 网络层初始化完成 (channel=com.yu80rice.launchinplacereload.repack)` + `[RepackNet] 已注册命名频道=… 服务器端+客户端处理器`；LHT `[Dependency] LaunchMultiplayerNet AssemblyVersion=5.0.0.0; required major=5` + `[HordeNet] 已注册 LMN V5 命名频道=io.github.yu80rice.launchhordetracker.horde-status; handler=client; lhtPayloadProtocol=1` + `[Lifecycle] LaunchHordeTracker loaded` + `[Network] transport=LMN V5 named-channel; channel=…; lhtPayloadProtocol=1` + `[Runtime] startupRole=…; clientUi=…` | ☐ |
| S2 LIT 回环整理 | 设置 → 控制 → 给 **Plugin 0** 槽位绑一个键；背包多格页（page 2-6）放乱若干物品；按绑定键 → 日志依序：`[TidyNet] -> 服务器: RequestTidy(reqId=N, page=…)` → `[TidyNet] 服务器已提交整理（reqId=N, …, mappings=M）` → `[TidyNet] <- 服务器 TidyCommitted(reqId=N, result=Committed, mappings=M)` → `[TidyNet] -> 服务器 HotkeyFlowAck(reqId=N)` → `[TidyNet] ACK 处理完成（reqId=N, restored=…, verified=…, cleared=…）`；背包物品同类聚合 → **截图整理前后** | ☐ |
| S3 LIR 本地压弹 | 主机持枪（弹匣未满 + 有备弹）→ **快速连按两次换弹键（双击 R）** → 屏幕 toast「一键压弹：成功压入 N 发子弹」+ 弹匣被压满；单击 R 仍是原版换弹不变 → **截图 toast**。（主机路径为本地事务，不产生网络帧——网络帧证据在 §4/§5） | ☐ |
| S4 LHT /horde 空态 | 聊天输入 `/horde` → 聊天回复「[尸潮监视] 当前无活跃尸潮」 | ☐ |
| S5 零误报 | 无 `BUE 错误：` 行；无 `[Tidy] uncaught`、无 `[RepackB] uncaught`、无 `[HordeNet] 拒绝`；原版玩法正常 | ☐ |

注 1：LIT 诊断按类别 15 秒限频——同一类别 15 秒内的第二条会带抑制计数或不出现；S2 各锚为不同类别，正常逐条出现。若首按遇 `[Tidy] 客户端尚未收到有效服务端 session challenge；本次整理请求未发送。`，等几秒重按即可（服务端 session challenge 未到，该行即其日志形态）。
注 2：SP 内 LIT 请求走 LMN 本地 loopback、LIR/LHT 主机路径本地权威，**不产生远端帧**；SP 验证的是插件功能链与注册链。BUE 接管链路的正向锚（委托）在 §4/§5 采集。

## 4. 环境二：SteamP2P Host / Client（双端同一 kit）

**准备**：双端部署同一 kit（指纹逐一核对）；主菜单 → 多人 → 创建服务器（Host），另一实例好友/LAN 加入（Client）。开始/结束各记一次 UTC 时间（`Get-Date -AsUTC` 截图），Host 与 Client 时间窗**正交重叠**；三 CaseId 共用本环境采集（每插件证据按 CaseId 归档）。

| 步骤 | 期望 | Host ☐ | Client ☐ |
|---|---|---|---|
| P1 双端启动锚 | 双端日志 = S1 全套（takeover-patch installed + 三插件注册锚） | ☐ | ☐ |
| P2 LIT 跨端整理（CaseId LIT） | **Client** 背包放乱 → 按 Plugin 0 绑键：Client 日志 `-> 服务器: RequestTidy(reqId=N…)` + `<- 服务器 TidyCommitted(reqId=N…)`；Host 日志 `服务器已提交整理（reqId=N…）` + `-> 客机 TidyCommitted(reqId=N…)`；**reqId 双端对齐**；Client 背包聚合 → 双端**截图** | ☐ | ☐ |
| P3 LIR 跨端压弹（CaseId LIR） | **Client** 持枪双击换弹键：Client toast「一键压弹：成功压入 N 发子弹」→ **截图**；Host 日志 `[RepackNet] dispatcher summary: dispatches=…`（≥1；该汇总窗口为 5 秒，若恰无输出，重做一次双击即可） | ☐ | ☐ |
| P4 LHT 信标广播（CaseId LHT） | Host 用管理员权限取得尸潮信标（Horde Beacon）道具，**满月夜**放置激活（激活判据 = 信标区刷怪；若当夜非满月，信标不激活——本环节可分离采集，见 §8）：Host **Debug** 日志 `[HordeNet] 广播 Update: epoch=E seq=S remaining=N/M loc=… by=…`；**Client** HUD 出现尸潮条（地点/发起者/剩余）→ **Client HUD 截图**；Client 聊天 `/horde` → 回复含同一地点与剩余数；尸潮结束：Host `广播 Clear: epoch=E seq=…` + Client HUD 消失（清空证据可选） | ☐ | ☐ |
| P5 BUE 委托锚 | **双端各恰一条** **Debug** `[BUE-V2NET] event=lmn2-delegate result=delegated decision=consume`（一次性锚：Host 收到 Client 帧触发一条、Client 收到 Host 帧触发一条）——**这是「命名频道收发在 BUE 网络模块下」的正向判据** | ☐ | ☐ |
| P6 零误报 | 双端无 `BUE 错误：` 行、无三插件异常/拒绝行；原版玩法正常；互不串扰 | ☐ | ☐ |

**每插件 CaseId 的「经 BUE 接管决策点」判据（会话门 + 组合判定，写入各自 case.md）**：

- **会话门（先决，不满足则该会话三件 CaseId 全部判不通过，不得以行为锚单独放行）**：本会话该端 `takeover-patch result=installed` 在场 **且** `lmn2-delegate result=delegated` ≥1 条。原理：BUE 委托是按帧统一路径（识别 LMN2 魔数 → 反射调 LMN 路由器），类型解析失败是会话级持续态——若委托路径不通，锚恒不出现、所有 LMN2 帧放行由 LMN 原生前缀扛（恰为 DEV-V2-11 修复前的事故形态）；反之锚一旦出现，本会话内已注册频道的 LMN2 帧均在 BUE 决策点被委托消费，「某插件帧绕行走原生」与「锚在场」不可并存。
- **组合判定（会话门通过后逐件）**：该插件①自身频道行为锚齐全（LIT=RequestTidy/TidyCommitted 双端链、LIR=toast+dispatcher、LHT=广播 Update+Client HUD）；②全程无 `unknown-channel-dropped`/`[HordeNet] 拒绝` 等丢弃信号。
- **可选严格加采**：若需逐插件独立委托锚（每插件单独会话、单独采锚），把 plugins 目录只保留该插件 + BUE + LMN 重启会话重复 P2–P5 对应步。

## 5. 环境三：U3DS Headless + 客户端（同一 kit）

**准备**：
- U3DS 默认端口 27015。客户端主菜单 → Play → Connect → 地址 `127.0.0.1`、端口 `27015` 直连（本机 U3DS）；U3DS 需已加载同一地图并存档。
- **管理员授权（P4/U4 的 `/horde` 在专用服务器上仅管理员有回复，非管理员静默）**：启动 U3DS 前把你的 SteamID 写入 `E:\Steam\steamapps\common\U3DS\Config.json` 的 `Owner` 字段（最稳），或启动后在 U3DS 控制台执行 `Admin <你的名字或SteamID>`。

U3DS 启动后用客户端直连，三插件以「远程客户端 ↔ 专用服务器」真实网络帧收发。

| 步骤 | 期望 | U3DS ☐ | Client ☐ |
|---|---|---|---|
| U1 U3DS 启动锚 | U3DS 控制台/`Logs`：BUE「加载成功（无界面）」+ `takeover-patch result=installed` + S1 中三插件注册锚全套（U3DS 无 UI，证据以日志为准） | ☐ | — |
| U2 LIT 跨端整理（CaseId LIT） | 客户端连入后按 Plugin 0 绑键：Client `RequestTidy`/`TidyCommitted`（reqId 对齐 U3DS 日志）+ U3DS `服务器已提交整理` + `-> 客机 TidyCommitted` | ☐ | ☐ |
| U3 LIR 跨端压弹（CaseId LIR） | Client 双击换弹键 → Client toast（**截图**）+ U3DS `dispatcher summary: dispatches=…`（5 秒汇总窗口，恰无输出就重做一次） | ☐ | ☐ |
| U4 LHT 信标（CaseId LHT） | U3DS 侧（管理员）激活信标（满月夜，可分离采集）：U3DS **Debug** `广播 Update: …` + Client HUD 出现（**截图**）+ Client `/horde` 回复一致（Client 须为管理员，见「准备」） | ☐ | ☐ |
| U5 BUE 委托锚 + 收尾 | U3DS 日志恰一条 `lmn2-delegate result=delegated`、Client 日志亦各恰一条；无 `BUE 错误：`；控制台正常退出无崩溃 | ☐ | ☐ |

## 6. 预期日志行速查表

| 行 | 级别 | 何时出现 |
|---|---|---|
| `[BUE-V2NET] event=takeover-patch result=installed … BUE-V2NET-003` | **Debug** | 每端每次启动（LMN 在场 → 接管装） |
| `[BUE-V2NET] event=lmn2-delegate result=delegated decision=consume` | **Debug** | 每端每会话恰一条（首个远端 LMN2 帧成功委托；**缺此行 = 委托未发生，该会话三件 CaseId 判不通过**——P5 会话门） |
| `BUE 错误：[BUE-V2NET] …` | Error | 仅故障（任何出现都记 finding） |
| `[TidyNet] 已注册 LMN V5 命名频道=… 双端处理器` | Info | LIT 每次启动 |
| `[TidyNet] -> 服务器: RequestTidy(reqId=N…)` / `<- 服务器 TidyCommitted(reqId=N…)` | Info | LIT 客户端每次整理 |
| `[TidyNet] 服务器已提交整理（reqId=N…）` / `-> 客机 TidyCommitted(reqId=N…)` / `ACK 处理完成（reqId=N…）` | Info | LIT 服务器端每次整理 |
| `[Bootstrap] LMN V5 网络层初始化完成 (channel=…)` + `[RepackNet] 已注册命名频道=…` | Info | LIR 每次启动（首帧延迟初始化） |
| `[RepackNet] dispatcher summary: dispatches=…` | Info | LIR 有网络活动后周期汇总 |
| `[Dependency] LaunchMultiplayerNet AssemblyVersion=5.0.0.0; required major=5` + `[HordeNet] 已注册 LMN V5 命名频道=…; handler=client; lhtPayloadProtocol=1` + `[Lifecycle] LaunchHordeTracker loaded` + `[Network] transport=LMN V5 named-channel…` + `[Runtime] startupRole=…; clientUi=…` | Info | LHT 每次启动（按 Awake 输出序） |
| `[HordeNet] 广播 Update: epoch=… seq=… remaining=N/M …` / `广播 Clear: …` | **Debug** | LHT 服务器端信标开始/结束 |
| `[HordeNet] 拒绝…` | Warning | 仅故障（出现即记 finding） |

## 7. 证据提交结构（采集完放回仓库）

```
audit/2026-09-05/evidence/DEV-V2-08-20260905/
├── env/
│   ├── sp/          deploy-fingerprint.txt + LogOutput.log + screenshots/（S2/S3）
│   ├── p2p-host/    同上（P2/P3/P4/P5）
│   ├── p2p-client/  同上
│   ├── u3ds/        同上（U1–U5 服务器侧）
│   └── u3ds-client/ 同上
└── cases/
    ├── lit/case.md   # CaseId=DEV-V2-08-LIT-20260905
    ├── lir/case.md   # CaseId=DEV-V2-08-LIR-20260905
    └── lht/case.md   # CaseId=DEV-V2-08-LHT-20260905
```

- `deploy-fingerprint.txt`：该端五件 `certutil -hashfile … SHA256` 输出 + BepInEx.cfg LogLevels 行 + 采集日期。
- `LogOutput.log`：整份原样复制，不裁剪不改名内容。
- `case.md` 模板字段：CaseId、插件名+频道名、各环境 UTC 时间窗（开始/结束，`Get-Date -AsUTC` 截图对应）、锚行摘录（逐条注明出自哪个 env 的日志）、截图路径清单（每张一行：路径 + 内容说明 + SHA-256）、结论（通过 / 缺陷描述）。
- 截图最低清单：S2 整理前后、S3 toast、P2/P3 双端、P4 Client HUD、U3 toast、U4 Client HUD。
- 采集完通知 agent 复核锚行与结论，agent 更新工单与结单材料。

## 8. 边界与失败处理

- 三环境证据只证明**同 kit 同哈希**下三插件命名频道功能在 BUE 接管态下正常 + 「已知生态」维度验证闭环；**不自动授予**三插件正式版本号、官方纳入或任何发布授权——这些由后续票决策。
- **LHT 时序边界**：信标环节依赖满月夜，允许与 LIT/LIR 分离采集（先关 LIT/LIR 的锚，LHT 锚补齐后三件一起闭环关票）。
- **任何一步与期望不符**：停止采集，保留现场（LogOutput.log + 截图 + 复现步骤），报告给 agent。缺陷按票面口径**另立修复票**（红测先行 + 双轴 CLEAN 后出新构建再复测）。**不要现场改动或混采配置**。
- 证据绑定行 6（`35670269…aef6`）；若验证期间 BUE 出新候选（如修复票产物），换绑须整包重采，不得新旧拼接。
