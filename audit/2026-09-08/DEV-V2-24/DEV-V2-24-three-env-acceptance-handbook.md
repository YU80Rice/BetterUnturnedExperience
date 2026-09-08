# DEV-V2-24 三环境终验 + T7 五项实机采集手册

> CaseId：`DEV-V2-24-20260908`（三环境 + T7 + 共存共用，采集期间不得更换）
> 工单：`.scratch/bue-v2-phase2-official-adoption/issues/DEV-V2-24-three-env-acceptance-releases.md`（claimed）
> 状态：候选已确认、采集 kit 六件就绪、证据包骨架与六份模板就位；**待你按本手册实机采集**。
> 前置门禁全绿：DEV-V2-20/21/22/23 四票双轴 CLEAN + 全套 7/7 PASS 0 警告；本票主体为验证与证据（沿 DEV-V2-07 先例），另含 D0-b 面板显示名修复一轮（§0，红测先行 + 双轴审查，新候选见 §1）。

## 0. 采集前拍板点（D0）——已拍板：D0-b 修复轮（2026-09-08，用户拍板）

**BII 面板条目名判别**：规格 story 3 冻结「面板显示四个官方功能的中文名（更好的物品交互 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报）」。源码修复前：面板 BII 条目显示名为**英文** `"Better Item Interaction"`（`src/BetterUnturnedExperience.Plugin/ClientUiCompositionRoot.cs:126`）。

**D0-b 已执行**（修复轮，红测先行 + 双轴审查）：面板 BII 条目显示名改为「更好的物品交互」（一处常量）；红测 = Plugin.Tests 新增断言（面板目录以官方中文名「更好的物品交互」投射 BII 条目，挂 `AssertLitSingleplayerPath` 面板块）——观测红（FAIL 于新断言）→ 改串 → 绿 + 全套 7/7 PASS 0 警告；新候选身份见 §1（前身 `7d5dd3b5…c223` 作废）。REG-ACCEPT 启动日志的 `Better Item Interaction featureId=…` 标签非面板显示名，不在本判别范围、维持原样（手册 S1 锚不受影响）。采集按本手册正常进行，S2 面板步骤的判别点已消解（四件中文名齐）。

## 1. 候选身份与 kit 清单（三环境必须部署同一份，逐一 certutil 核对）

**候选**（= DEV-V2-23 候选 + D0-b 面板显示名修复 + **F-A 挑战重臂 + F-B2 隔离去抖**（实机缺陷修复轮，见票面 Comments 与 review-rounds.md）；三轮 `-t:Rebuild` 字节一致，见 `identity-rebuild1/2/3.log` + `identity-sha256.txt` v3）：

| 项 | 值 |
|---|---|
| 候选 DLL | `audit/2026-09-08/artifacts/DEV-V2-24-20260908/BetterUnturnedExperience.dll`（537088 字节） |
| SHA-256 | `7448b0ce14ecced001e4e8b4dfce09416dc66985ab063a904acd681ec57464c9` |
| 来源快照 | `e102935` + D0-b（`e70b7c4`）+ F-A（`3db75c0`）+ F-B2（`f89194f`/`03b661f`/`fe3e3cc`）+ F-B1 含 F1（本修复轮）；增量 round5-increment.diff（含 F1 终稿） |
| 候选阶段 CaseId | `DEV-V2-24-CANDIDATE-20260908`（前身 `7d5dd3b5…c223` 与 `3cbd6268…9e4d` 均作废，不得用于采集） |
| 采集 CaseId | `DEV-V2-24-20260908`（本手册与全部证据统一使用） |

**kit 一站式目录**：`audit/2026-09-08/DEV-V2-24/kit/out/`（部署从这里拿，**不得重新构建**）：

| 件 | SHA-256 | 字节 | 角色 |
|---|---|---|---|
| `BetterUnturnedExperience.dll` | `7448b0ce…64c9`（完整值见上表） | 537088 | 候选 BUE（裸 BUE 单 DLL 主验配置） |
| `LaunchMultiplayerNet.dll` | `06d8a45438c09fea65f3800bf01a7efb9302f2421bd76aa386f8828701a63055` | 68096 | 独立 LMN v5.0.0.0（仅配置 B 共存用） |
| `LmnEcosystemFixture.dll` | `2b82114f12abd25c93edd5957fdd510c2e3df25824ba264ef4aaf419f3ba7096` | 9216 | V1 旧插件替身（普通 LMN 消费方，仅配置 B；V1 数字频道 ch250 + V2 命名频道） |
| `BetterUnturnedExperience.NoOpFixture.dll` | `9ee9944ded11ec97e3462a435b2be822c9a45a16fec0b475497c0456d79bed68` | 6144 | 生态样板插件（仅 T7-1 改名对照用；GUID `io.github.yu80rice.bue.noop`，经公开桥注册） |
| `BueSameAsmProbe-ABefore.dll` | `30b93000d2e54f23e0bf23684db7f507b4bbdde0cd900f84f1de90ed9bd7c54a` | 4096 | T7-4 探针 A 变体（AssemblyName=BetterUnturnedExperience，GUID `io.github.yu80rice.aaprobe.sameasm`，**排序在 BUE 前**） |
| `BueSameAsmProbe-ZAfter.dll` | `dbc64f6937b4a386d1bacfd9b07533d346907daa936ed04dc3264df45677c2ca` | 4096 | T7-2/3 探针 Z 变体（同程序集名，GUID `io.github.yu80rice.zprobe.sameasm`，**排序在 BUE 后**） |

- 两个探针 = T7 实验的**证据仪器**（非生产代码；源码 `kit/BueSameAsmProbe/SameAsmProbe.cs`，构建 `build-probes.cmd`，log `build-probe-a/z.log`）。它们与 BUE 同程序集名同版本（0.0.0.0）、不同插件 GUID、不同文件名——正是「不同 GUID + 同程序集名」的活体副本。
- `SteamP2PFriends.dll` 留着不动（无 BepInPlugin 属性的休眠件），在部署指纹里记录即可。

## 2. 部署准备与三种配置（每次换配置前：完全退出游戏）

**部署位置**：客户端 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`；U3DS `E:\Steam\steamapps\common\U3DS\BepInEx\plugins\`。

1. 换配置前先**清空**本实验涉及的 DLL（对照下表），再从 `kit/out` 复制所需件；
2. 逐件 `certutil -hashfile <件> SHA256` 与 §1 表核对，输出存入该会话的 `deploy-fingerprint.txt`；
3. **开启 Debug 日志（关键）**：两端 + U3DS 的 `BepInEx\config\BepInEx.cfg` → `[Logging.Disk]` → `LogLevels = Fatal, Error, Warning, Message, Info, Debug`——BUE-V2NET/装饰性行多为 Debug 级，不开则 LogOutput.log 不可见；
4. U3DS 管理员授权（`/horde` 仅管理员有回复）：启动 U3DS 前把 SteamID 写入 `E:\Steam\steamapps\common\U3DS\Config.json` 的 `Owner` 字段，或控制台 `Admin <名字或SteamID>`；客户端侧 `/horde` 亦需该客户端为管理员。

| 配置 | plugins 内容 | 用途 | 会话 |
|---|---|---|---|
| A（裸 BUE 主验） | 仅 `BetterUnturnedExperience.dll` | 三环境四功能验收 + 防双装真机基线（零 001 行） | §3/§4/§5 |
| B（共存承诺） | `BetterUnturnedExperience.dll` + `LaunchMultiplayerNet.dll` + `LmnEcosystemFixture.dll` | 「BUE + 独立 LMN」下 V1 旧插件继续收发 | §6 |
| C（T7 实验） | 按 §7 各项单独部署（改名/探针组合） | T7 五项 + 防双装真机基线 | §7 |

- 配置 A 的启动预期：`event=takeover-patch result=installed …`（**这是 BUE 自己的帧消费补丁，装不装只看网络模块开关，与独立 LMN 是否在场无关**，DEV-V2-18 拆分语义）；配置 A **零** `v1-table-mirror` 行（镜像只在 LMN 接管缝激活时运行）。
- 已承认边界（随证据记录，不是缺陷）：裸 BUE（无独立 LMN）环境下，依赖 LMN 数字频道的 V1 旧插件不收发。

## 3. 环境一：单人（配置 A，约 15 分钟）

启动单人世界。全部锚行来自源码现行串；每步在 `cases/sp/case.md` 填行号。

| 步骤 | 期望 | ☐ |
|---|---|---|
| S1 启动 | `Better Unturned Experience 加载成功，界面已注入`；`[BUE-UI-TRACE] plugin=io.github.yu80rice.betterunturnedexperience diagnosticId=BUE-MANAGEMENT-TRACE-002 event=assembly-identity path=…\BetterUnturnedExperience.dll sha256=7448B0CE…64C9`（**身份绑定行，照抄原文**） | ☐ |
| S1 注册 | 五行注册锚全 `accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`：`Better Item Interaction featureId=io.github.yu80rice.bue.better-item-interaction` / `BUE Network Module featureId=io.github.yu80rice.bue.network` / `BUE Inventory Tidy featureId=io.github.yu80rice.bue.inventory-tidy` / `BUE In-Place Reload featureId=io.github.yu80rice.bue.in-place-reload` / `BUE Horde Tracker featureId=io.github.yu80rice.bue.horde-tracker` | ☐ |
| S1 网络 | `[BUE-V2NET] event=takeover-patch result=installed priority=first targets=NetMessages.ReceiveMessageFromClient,NetMessages.ReceiveMessageFromServer diagnosticId=BUE-V2NET-003` + `[BUE-V2NET] event=bue-runtime-arm result=armed role=server localSteamId=… diagnosticId=BUE-V2NET-003`（进世界后出现；role 以实际为准记录）+ `event=lmn-config-migration result=no-op mapping=empty reason=lmn-has-no-config diagnosticId=BUE-V2NET-001` | ☐ |
| S1 防双装基线 | 全程 **零** `BUE-PLATFORM-001` 行（无冲突部署基线，SDK 文档 §8 附注） | ☐ |
| S1 三功能注册行 | `[Tidy] 模块已启动（宿主 bootstrap：功能代际=N）` + `[Tidy] 整理按钮补丁已安装（Harmony ID=io.github.yu80rice.bue.inventory-tidy）`；`[Lir] 模块已启动（宿主 bootstrap：功能代际=N，网络注册延迟至首帧游戏线程）` + `[Lir] 原位换弹补丁已安装（Harmony ID=io.github.yu80rice.bue.in-place-reload）`；`[HordeTracker] 已订阅 BeaconManager.onBeaconUpdated + Provider.onServerHosted` + `[HordeTracker] 已注册 /horde 命令（Commander.register）` | ☐ |
| S2 面板 | G 开背包 → 管理面板：条目含 **更好的物品交互 / BUE 网络模块 / BUE V1 兼容层 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报**（D0-b 已收口：四件官方功能中文名齐，BII 条目=更好的物品交互）→ **截图**；每个功能开关单独关闭→行为消失→再开启（原生回退，抽验 LIT 或 LIR 一件即可） | ☐ |
| S3 LIT 整理 | 背包多格页（page 2-6）放乱物品 → 点整理按钮（或 Ctrl+点击整理全身）：日志 `[Tidy] 本地整理已提交（page=N, mode=…, mappings=M）。` → 背包同类聚合 → **整理前后截图** | ☐ |
| S4 LIR 压弹 | 持枪（弹匣未满+有备弹）**双击换弹键** → toast「一键压弹：成功压入 N 发子弹」+ 弹匣压满；单击 R 仍原版 → **截图** | ☐ |
| S5 LIR×LIT 链 | 整理完成后自动压弹：日志 `[MergeA] 整理后自动压弹完成（target=…, merged=N, txn=…）`（无备弹时为跳过变体行，如实记录） | ☐ |
| S6 LHT 空态 | 聊天 `/horde` → 回复含「[尸潮监视] 当前无活跃尸潮」 | ☐ |
| S7 零误报 | 无 `BUE 错误：` 行；无 `uncaught`/`拒绝`/`result=failed` 行；原版玩法正常 | ☐ |

## 4. 环境二：SteamP2P Host / Client（配置 A，双端）

**准备**：双端部署同一候选（指纹逐一核对）；主菜单 → 多人 → 创建服务器（Host），另一实例好友/LAN 加入（Client）。开始/结束各记一次 UTC 时间（PowerShell `Get-Date -AsUTC` 截图）——**Host 与 Client 时间窗必须正交重叠**（不是首尾相触），双端共用 `CaseId=DEV-V2-24-20260908`。

| 步骤 | 期望 | Host ☐ | Client ☐ |
|---|---|---|---|
| P1 启动锚 | 双端 = S1 全套（加载行 / assembly-identity / REG-ACCEPT ×5 / takeover-patch / bue-runtime-arm，Host role=server、Client role=client） | ☐ | ☐ |
| P2 LIT 跨端整理 | **Client** 背包放乱 → 整理按钮：Client `[Tidy] -> 服务器: RequestTidy(reqId=N, page=…, mode=…, …)` → Client `[TidyNet] <- 服务器 TidyCommitted(reqId=N, result=…)` → Client `[TidyNet] -> 服务器 HotkeyFlowAck(reqId=N)`；Host `[TidyNet] 快捷键快照已验证：uploaded=…, accepted=…, reqId=N。` + `[TidyNet] -> 客机 TidyCommitted(reqId=N, result=…, mappings=M)。`——**reqId 双端对齐**；Client 背包聚合 → 双端**截图** | ☐ | ☐ |
| P3 LIR 跨端压弹 | **Client** 持枪双击换弹键：Client toast「一键压弹：成功压入 N 发子弹」→ **截图**；Host `[RepackNet] dispatcher summary: dispatches=…`（≥1；5 秒汇总窗，恰无输出就重做一次双击） | ☐ | ☐ |
| P4 LHT 信标广播 | Host（管理员）放置并激活尸潮信标（**满月夜**，激活判据=信标区刷怪；非满月可分离采集，见 §10）：Host **Debug** `[HordeTracker] 尸潮爆发 @ nav=… epoch=…` + `[HordeNet] 广播 Update: epoch=E seq=S remaining=…`；**Client** `[HordeNet] 收到 Update: epoch=E seq=S …` + HUD 出现尸潮条 → **Client HUD 截图**；Client `/horde` → 回复含同一地点与「剩余 N / 总数 M」；尸潮结束：Host `[HordeNet] 广播 Clear: epoch=E seq=…` + Client `[HordeNet] 收到 Clear: …` + HUD 消失 | ☐ | ☐ |
| P5 主机本地路径 | Host 自己双击压弹走本地事务（无网络帧、toast 正常）；Host 自己整理请求不入站回环（无「自己发给自己」迹象） | ☐ | — |
| P6 零误报 + 互不串扰 | 双端无 `BUE 错误：` 行；无重复派发（LIT 同 reqId 同侧恰一次、LIR 每次双击 toast 恰一次、LHT 收到行 (epoch,seq) 重复键为零）；原版玩法正常 | ☐ | ☐ |
| P7 原版玩家零感知 | （可选，如手头有未装 BUE 的第三实例）原版客户端连入 Host：不被任何功能帧打扰、正常游玩 | ☐ | ☐ |

## 5. 环境三：U3DS Headless + 客户端（配置 A）

**准备**：U3DS 默认端口 27015；客户端主菜单 → Play → Connect → `127.0.0.1:27015` 直连本机 U3DS（需已加载同一地图并存档）。U3DS 与客户端均为配置 A（仅候选 BUE）。`/horde` 授权见 §2 第 4 条。

| 步骤 | 期望 | U3DS ☐ | Client ☐ |
|---|---|---|---|
| U1 启动锚 | U3DS 日志：`Better Unturned Experience 加载成功（无界面）` + `[BUE-UI-TRACE] … diagnosticId=BUE-BOOTSTRAP-002 event=runtime-gate decision=Headless batchMode=True headless=True` + `Better Unturned Experience featureId=… status=BootstrapReady decision=Headless` + S1 注册锚全套（assembly-identity / REG-ACCEPT ×5 / takeover-patch / 三功能注册行）；**全程零 `BUE-PLATFORM-001` 行**；无 ClientUi/Glazier/Sleek 类报错 | ☐ | — |
| U2 LIT 服务器权威 | 客户端连入后整理：Client RequestTidy/TidyCommitted/HotkeyFlowAck（reqId 对齐）+ U3DS `[TidyNet] 快捷键快照已验证…` + `[TidyNet] -> 客机 TidyCommitted(reqId=N…)` | ☐ | ☐ |
| U3 LIR 跨端压弹 | Client 双击换弹键 → Client toast（**截图**）+ U3DS `dispatcher summary: dispatches=…` | ☐ | ☐ |
| U4 LHT 广播 | U3DS 侧（管理员）激活信标（满月夜，可分离采集）：U3DS `尸潮爆发 @ …` + `广播 Update: …`；Client `收到 Update: …` + HUD（**截图**）+ `/horde` 回复一致 | ☐ | ☐ |
| U5 收尾 | 双端零误报；客户端 `bue-runtime-arm role=client`；U3DS 控制台正常退出无崩溃 | ☐ | ☐ |

**U3DS 功能判据口径（spec 冻结）**：LHT 在 U3DS 功能状态 Available（追踪与广播照常）、表现状态 HeadlessOnly（不装 HUD，不阻塞 U3DS 验收）；其余三件以服务器侧日志锚 + 客户端体验为准。

## 6. 共存承诺：BUE + 独立 LMN（配置 B，单人环境约 10 分钟）

**部署**：客户端 plugins 目录 = `BetterUnturnedExperience.dll` + `LaunchMultiplayerNet.dll` + `LmnEcosystemFixture.dll`（三件逐一核对哈希；U3DS 侧无需采本配置）。fixture 是普通 LMN 消费方（零 BUE 引用），即「未知 V1 旧插件」替身：V1 数字频道 ch250 + V2 命名频道双路。

| 步骤 | 期望 | ☐ |
|---|---|---|
| B1 启动 | S1 基线全套仍在；新增 `[LMNFIX] ready fixture=0.1.0 … lmnOperational=True channels=v1-server=ok;v1-client=ok;v2-server=ok;v2-client=ok;` | ☐ |
| B2 V1 镜像 | `[BUE-V2NET] event=v1-table-mirror result=mirrored channels=…`（若先 defer 后成功则同条带 `deferred=true`） | ☐ |
| B3 V1 回环 | 每 10 秒一组：`[V1FIX] send-broadcast …` + `[V1FIX] recv-from-server kind=ping …` + pong 往返（seq 递增无重复）——**旧插件在「BUE + 独立 LMN」下继续收发 = 共存承诺成立** | ☐ |
| B4 V2 回环 + 放行锚 | `[V2FIX]` 回环同构；一次性 `[BUE-V2NET] event=lmn2-frame-release result=released decision=lmn-native-dispatch diagnosticId=BUE-V2NET-003`（LMN live → BUE 决策核放行，LMN 原生路径恰一次派发） | ☐ |
| B5 V1 放行锚 | 一次性 `[BUE-V2NET] event=v1-frame-release result=released decision=lmn-native-dispatch diagnosticId=BUE-V2NET-003` | ☐ |
| B6 决策核干净 | 全程零 `event=lmn2-delegate result=delegated` 行（inert 兜底路径专用，live 会话出现 = 决策核回归）、零 `unknown-channel-dropped`、零 `BUE 错误：` 行 | ☐ |

## 7. T7 实机五项 + 防双装真机基线（配置 C，**独立会话**，不与 A/B 混采）

> 对应 SDK 文档 §8。第 4 项 = **红测 + 实机双证**（红测锚 `--bue-v2-platform-red` 已 CLEAN，本节是实机半）。第 1/2/3/5 项为事实观察：**两种结果都如实入证据**；第 4 项正向场景若未出现 001 行则属 finding——停止采集保留现场报 agent。

### C1（T7-1）改名实机对照

1. plugins = `BetterUnturnedExperience.r24.dll`（候选 BUE **改名**，哈希不变）+ `BetterUnturnedExperience.NoOpFixture.dll`；
2. 启动单人世界（进世界即可）；
3. 期望：BepInEx 无「缺少依赖/Dependency」告警；BUE 加载行 + `event=assembly-identity path=…\BetterUnturnedExperience.r24.dll sha256=7448B0CE…64C9`（**path=改名路径**、sha256=候选原值）；NoOpFixture `BUE no-op fixture featureId=io.github.yu80rice.bue.noop accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`（前置按 GUID 解析 + IL 按程序集名绑定，均与文件名无关）；
4. 采完恢复原文件名。**记录**：☐

### C2+C3+C5（探针 Z 变体会话：LoadFile 二次探测 / 同版本谁保留 / Preloader 观察）

1. plugins = 候选 BUE（原名）+ `BueSameAsmProbe-ZAfter.dll`；
2. 启动单人世界，观察并记录：
   - **T7-2 Mono LoadFile 二次探测**：BepInEx 对 Z 探针的加载结局——探针 `[PROBE] awake probeGuid=io.github.yu80rice.zprobe.sameasm assemblyName=BetterUnturnedExperience version=0.0.0.0 location=<探针路径>` 行出现（Mono 装载了第二副本）？还是探针加载失败/类型缺失（Mono 按身份返回已加载的 BUE 程序集）？**记录事实**；
   - **T7-3 同版本程序集最终谁保留**：以 `[PROBE] awake … location=…` 行与 BUE selfPath（001 行或 assembly-identity 行 path）对照，记录类型绑定最终由哪个副本承担；
   - **T7-5 Preloader `AssemblyResolve`**：LogOutput Preloader 段在探针在场时有无新增解析错误/异常；只记录，不承诺；
3. 同时预期：Z 探针程序集在 BUE Awake **之后**才进入 AppDomain → 001 行**不出现**（SDK §6 冻结边界：自检只看「已进入 AppDomain」的程序集；若出现则记录为过报事实）。**记录**：☐

### C4（T7-4）不同 GUID + 同程序集名（红测+实机双证的正向半）——探针 A 变体会话

1. plugins = 候选 BUE（原名）+ `BueSameAsmProbe-ABefore.dll`（A 变体 GUID 排序在 BUE 前 → 副本程序集先于 BUE Awake 进入 AppDomain）；
2. 启动单人世界；
3. 期望（001 双证）：
   - **日志**：Warning 行 `BUE double-install detected diagnosticId=BUE-PLATFORM-001 assembly=BetterUnturnedExperience conflictLocation=…\BueSameAsmProbe-ABefore.dll selfPath=…\BetterUnturnedExperience.dll suggestion=移除非官方副本`（照抄原文入 case）；
   - **面板**：G → 管理面板 → 底部红色状态行「错误：检测到 BetterUnturnedExperience 冲突副本（BUE-PLATFORM-001）：请移除非官方副本后重启游戏，详见 BUE 日志。」→ **截图**；
4. 摘除探针重启 → 001 不再出现（用户处置路径验证）；全程确认 BUE **没有**删除/移动/改名任何文件（探针 DLL 仍在原处）；
5. 若 001 未出现：**停止采集**，保留 LogOutput + BepInEx 加载序行，报 agent（= 诊断漏报 finding，走 real-machine-test-loop）。**记录**：☐

## 8. 预期日志行速查表

| 行 | 级别 | 何时出现 |
|---|---|---|
| `Better Unturned Experience 加载成功（无界面）` | Info | U3DS 每次启动 |
| `Better Unturned Experience 加载成功，界面已注入` | Info | 客户端/单人每次启动 |
| `… event=assembly-identity path=… sha256=7448B0CE…64C9`（BUE-MANAGEMENT-TRACE-002） | Info | 每端每次启动（**身份绑定行**，逐环境照抄） |
| `Better Item Interaction / BUE Network Module / BUE Inventory Tidy / BUE In-Place Reload / BUE Horde Tracker featureId=… accepted=True reason=None diagnosticId=BUE-REG-ACCEPT` | Info | 每端每次启动（五行） |
| `[Tidy] 整理按钮补丁已安装（Harmony ID=…）` / `[Lir] 原位换弹补丁已安装（Harmony ID=…）` | Info | 每端每次启动（enabled 时） |
| `[Tidy] 本地整理已提交（page=N, mode=…, mappings=M）。` | Info | LIT 本地/主机整理提交 |
| `[Tidy] -> 服务器: RequestTidy(reqId=N…)` / `[TidyNet] <- 服务器 TidyCommitted(reqId=N…)` / `[TidyNet] -> 服务器 HotkeyFlowAck(reqId=N)` | Info | LIT 客户端每次联机整理 |
| `[TidyNet] 快捷键快照已验证：…` / `[TidyNet] -> 客机 TidyCommitted(reqId=N…)` | Info | LIT 服务器端每次联机整理 |
| `[MergeA] 整理后自动压弹完成（target=…, merged=N, txn=…）` | Info | LIT 整理成功后（LIR 消费 TidyCompleted） |
| `[RepackNet] 已注册 BUE 命名频道=io.github.yu80rice.bue.in-place-reload 双方向处理器（首帧游戏线程）` | Info | LIR 首帧游戏线程注册 |
| `[RepackNet] dispatcher summary: dispatches=…` | Info | LIR 服务器有网络压弹活动后周期汇总 |
| `[HordeNet] 已注册 BUE 命名频道=io.github.yu80rice.bue.horde-tracker（FeatureId 即频道身份，旧频道退役）` | Info | LHT 每次启动 |
| `[HordeTracker] 尸潮爆发 @ nav=… epoch=…` / `尸潮结束 @ …` | Info | 信标激活/平息（服务器权威侧） |
| `[HordeNet] 广播 Update: epoch=… seq=…` / `广播 Clear: …` | **Debug** | 服务器端广播（Update 不可靠通道周期发、Clear 可靠） |
| `[HordeNet] 收到 Update: …` / `收到 Clear: …` | **Debug** | 客户端每收到一帧（(epoch,seq) 重复键为零） |
| `[BUE-V2NET] event=takeover-patch result=installed … BUE-V2NET-003` | **Debug** | 每端启动（网络模块开；与 LMN 是否在场无关） |
| `[BUE-V2NET] event=bue-runtime-arm result=armed role=… localSteamId=… BUE-V2NET-003` | **Debug** | 进世界/连入后 |
| `[BUE-V2NET] event=lmn-config-migration result=no-op … BUE-V2NET-001` | **Debug** | 每端每次启动 |
| `[BUE-V2NET] event=v1-table-mirror result=mirrored channels=…` | **Debug** | 仅配置 B（LMN 接管缝激活时镜像旧频道表） |
| `[BUE-V2NET] event=v1-frame-release / lmn2-frame-release result=released decision=lmn-native-dispatch` | **Debug** | 仅配置 B，各一次性（首个对应类帧放行） |
| `BUE double-install detected diagnosticId=BUE-PLATFORM-001 …` | **Warning** | 仅同程序集名冲突副本在场（C4 正向；配置 A 基线必须为零） |
| `BUE 错误：…` / `result=failed` / `unknown-channel-dropped` / `handler-fault-isolated` / `network-module-isolated` / `[HordeNet] 频道注册被拒绝` | Error/Warning | 仅故障（任何出现都记 finding；`BUE-PLATFORM-002` 为自检自身隔离留痕） |

## 9. 证据提交结构（采集完放回仓库）

```
audit/2026-09-08/evidence/DEV-V2-24-20260908/
├── candidate/identity.txt                （已就位勿改：候选两轮重建哈希 + NoOpFixture 哈希）
├── cases/sp/                             case.md + deploy-fingerprint.txt + LogOutput.log + 截图
├── cases/p2p-host/                       同上
├── cases/p2p-client/                     同上
├── cases/u3ds/                           同上（U3DS 服务器日志；客户端侧截图可归此或注明）
├── coexistence/                          case-coexistence.md + deploy-fingerprint.txt + LogOutput.log
└── t7/                                   case-t7.md + 各会话 LogOutput.log（改名/Z/A 三段，改名标注）+ 面板截图
```

- 每份 `case.md` 的 TODO 字段全部填实（collector / 版本 / UTC 窗口 `Get-Date -AsUTC` O 格式 / 部署指纹 / 锚行+行号 / 截图清单每行附 SHA-256）；结构不改。
- `deploy-fingerprint.txt` = 该端逐件 certutil 输出 + BepInEx.cfg LogLevels 行 + 采集日期 + `SteamP2PFriends.dll` 在场记录。
- **身份绑定（LoadSetIdentity）**：每份 case 的 assembly-identity 行 sha256 必须与候选 `7448b0ce…64c9` 一致；三环境 + T7 + 共存全部绑定同一候选——结单时由 agent 汇总为 RELEASES 候选行的绑定表。
- 截图最低清单：sp 面板四条目（D0）、LIT 整理前后、LIR toast；p2p-client toast + LHT HUD；t7 面板红色状态行（C4）。
- UMM 诊断导出若可用则附上；不可用即在 case.md 注明。
- 采集完通知 agent：复核锚行与结论 → 双轴独立审查（standards-reviewer / Spec-Reviewer，全新实例）→ CLEAN 后授予 CaseId、落 RELEASES 候选行、真机手册（玩家安装/升级）落盘 → 关票。

## 10. 边界与失败处理

- 证据只证明**同候选同哈希**下四功能三环境可用 + T7 五项事实 + V1 共存承诺；**不自动授予**发布授权——RELEASES 换标与「当前发布物」标记随结单人工验收入账（不继承既往批准）。
- **D0-b 已收口**（2026-09-08 用户拍板修复轮）：面板 BII 条目 =「更好的物品交互」，S2 直接验收四件官方中文名；修复轮候选身份见 §1。
- **LHT 时序边界**：信标环节依赖满月夜，允许与 LIT/LIR 分离采集（分次会话分别归档，绑同一 CaseId 与同一候选）。
- **T7-2/3/5 是观察项**：结果与预期同异都如实入证据；**T7-4 是判据项**：正向场景缺 001 行 = finding。
- **任何期望步骤与实际不符**：停止采集，保留现场（LogOutput.log + 截图 + 复现步骤），报告 agent。修复走 real-machine-test-loop：红测先行 + 双轴 CLEAN 后出新候选 → **换绑后全量重采，不得新旧候选拼接证据**。**不要现场改动或混采配置**。
- 候选 DLL 永远从 `kit/out` 取，**不得重新构建**；`src/**/bin/Release` 不是发布物。




