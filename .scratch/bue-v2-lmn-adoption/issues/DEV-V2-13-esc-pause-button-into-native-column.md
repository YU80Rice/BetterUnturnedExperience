# DEV-V2-13：ESC 暂停菜单 BUE 入口并入原生按钮列

Type: task
Status: resolved（2026-09-05，agent；用户实机测试「BUE 面板和原版功能都无异常」+ 授权关闭（原话「人工核验通过，我同意关闭工单」）。日志复核零异议：指纹 sha256=35670269…aef6 精确匹配、pause-column-shift anchored=9、退出 restored=9/failed=0、BUE 零 Error 行（仅 2 行 SteamP2PFriends 第三方自身错误，非 BUE）、镜像 channels=2 健康。实施链=红0+红1/2/3→绿、R1→R5 五轮双轴双 CLEAN，提交 ac08916）
Parent: spec-V2-phase1-lmn-adoption；前置同根票 DEV-V2-09（主菜单列已修，模式沿用）
Source: 用户 2026-09-05 需求 + ESC 界面截图（暂停菜单「BUE 插件管理」凸块）

## Scope

用户需求：ESC 暂停菜单（PlayerPauseUI）的「BUE 插件管理」不再单独凸出在按钮列右侧，而是并入原生列表——插在「返回」按钮下一行（第二槽），下方原版按钮整体下移一格，上下间距与原版一致（节距 60px / 视觉 10px）。原话：「把游戏原版的选项往下挪一格，这样就可以把 BUE 管理面板放在原生列表里」「就是放在『返回』这个按钮下面一行，上下保持原版按钮之间的间距」。

## 根因与机制（triage 定位，2026-09-05，实机反编译复核）

- 现状：`TryAddPauseButton` 硬编码 `PositionOffset_X = 205f`、`Y = -290f`（列顶右侧），形成独立凸块。
- 原版布局（PlayerPauseUI，全部 PositionScale(0.5,0.5)、X=-100、200×50、节距 60）：returnButton Y=-290 → inviteFriendsButton（条件 !isServer && Steam overlay，存在则 -230）→ optionsButton → displayButton → graphicsButton → controlsButton → audioButton → suicideButton（suicideDisabledLabel 同槽、IsVisible 切换）→ exitButton → quitButton。列起点 `int num = -290`，逐项 `num += 60`。
- **全部为 static 私有字段 → 反射可达**，挪动机制 = 按元素实例锚定原 Y 后统一 +60（一个节距），不动 X（除 spy 模式，见下）。
- 锚定幂等设计：字典按元素实例（引用相等）记原值，Apply 每 tick 设 Y = anchor + 60（绝不累积漂移）；UI 重建（新字段实例）自动重锚；Restore 先写值后删锚，失败保留锚可重试；Destroy 时 Restore 归还原布局（仅存活元素）。
- spy 模式（`PlayerPauseUI.onSpyReady`）把整列 X 移到 -435（原版怪癖：之后不回位，直到 UI 重建）——**不挂 postfix**，注入路径每 tick 镜像原版列首元素的当前 X：自愈、覆盖 spy 早于首次注入的时序、且比事件补丁少一个 Harmony 补丁点（R1 评审后定的机制）。
- **fail-closed（R1-F5）**：清单中除 `inviteFriendsButton`（原版条件创建，缺席正常）外全部为必需字段；必需字段任一缺失 → 跳过暂停入口注入（原版列保持原生布局），一次性 `pause-shift-fields-missing` 诊断。

## Agent Brief

**Category:** enhancement
**Summary:** ESC 暂停菜单 BUE 按钮从右侧凸块移入原生列第二槽（「返回」下一行），下方原版项下移一格，间距与原版一致。

**Current behavior:** pause 按钮 (X=205, Y=-290) 独立凸出于列右；原版列不受影响。
**Desired behavior:** pause 按钮 (X=-100, Y=-230=return+60) 入列；inviteFriends（若存在）/options/display/graphics/controls/audio/suicide+suicideDisabledLabel/exit/quit 十个元素各自 Y=原值+60；spy 模式整列（含 BUE 按钮）X=-435。

**Key interfaces:**
- `BueMenuEntryLayout` 增暂停菜单节：`PauseColumnButtonX=-100`、`PauseColumnButtonWidth=200`、`PauseColumnButtonHeight=50`、`PauseColumnSlotPitch=60`、`PauseReturnSlotY=-290`、`PauseBueSlotY=-230`（=return+pitch）、`PauseSpyColumnButtonX=-435`、`PauseShiftFieldNames` 十字段清单（不含 returnButton、container）、`PauseOptionalShiftFieldNames`（=inviteFriendsButton，其余必需）。
- 新增 `BuePauseColumnShift`：纯 C# 锚定移位注册表（`Apply(key, currentY, setY)` 返回是否新锚；`Restore(key, setY)` 先写值后删锚、失败保锚可重试；`SnapshotAnchors()` 供还原走查；`RemoveAnchor(key)` 弃死实例锚），**引用相等键**，无 Glazier 依赖，headless 可测。R2 后生产路径无 `Clear()`——清空由 Restore/RemoveAnchor 逐锚消费完成，避免误清可重试锚（R4-Spec 核对项）。
- `BueNativeManagementPanel`：`ResolvePlayerPauseShiftFields()`（Public|NonPublic 双解析——exitButton/quitButton 是 public static）、必需字段缺失 fail-closed 门、TryAddPauseButton 重排（清理→建钮→每 tick 移位后置）、X 每 tick 镜像、Cleanup 时 Restore。

**Acceptance criteria:**
- [x] 红测先行：红0 编译红（CS0117×26+CS0246×2）+ **红1** 十字段对真实程序集解析（exitButton NonPublic 漏解析实锤）+ **红2** Equals 相撞键独立锚定 + **红3** Restore 失败保锚可重试，三红留证后转绿。
- [x] 实机截图：BUE 按钮在「返回」下一行，上下间隙 == 原版节距；右侧凸块消失。（用户截图验收通过：`audit/2026-09-05/DEV-V2-13/retest-esc-user-acceptance.png`）
- [x] 自杀禁用标签随按钮同移（在移位清单内）。
- [x] 现有七套测试全绿；MenuDashboardUI / MenuWorkshopUI 注入不受影响（R3/R4 复审确认零改动）。
- [x] 双轴评审 CLEAN 后出独立增量（R1→R5 五轮，R3/R4 Standards CLEAN、R4/R5 Spec CLEAN；链路见 `audit/2026-09-05/DEV-V2-13/DEV-V2-13-closing-report.md`）。

## Out of scope:
- 按钮视觉样式对齐（SleekButtonIcon 图标样式）——沿用现有素按钮 200×50，只管位置与间距。
- 管理面板内容、主菜单列（09 已闭环）、MenuWorkshopUI 入口。
- 修复原版 spy 模式「X 不回位」的既有怪癖（跟随即不扩大）。

## 实施结单（2026-09-05，agent /implement 同回合完成 triage）

- **红绿链**：红0 编译红（CS0117×26+CS0246×2）→ 红1 exitButton/quitButton NonPublic 漏解析实锤（R1-Spec 抓到的真 bug）→ 红2 Equals 相撞键独立锚定 → 红3 Restore 失败保锚可重试，各红留证后转绿；sln Release 重建 0/0 + 七套全 PASS。
- **双轴五轮**：R1 双 FINDINGS（红证不足/重建帧重叠/Restore 丢锚/spy 时序/fail-open + exit-quit 实锤）→ R2 指出 F3/F5 未真闭合 → 就绪门+快照还原+Clear 退出生产 → R3 Standards CLEAN/Spec 写阶段部分移位 → 事务性两阶段（先全读后全写、失败按锚回滚+收回按钮）→ R4 Standards CLEAN/Spec 票面 Clear() 过时声明 → 票面同步 → R5 Spec 复核 CLEAN。**双 CLEAN，环路闭合**；链路详见 `audit/2026-09-05/DEV-V2-13/DEV-V2-13-closing-report.md`（含五项具名延期）。
- **变更**：`BueMenuEntryLayout` +暂停节与字段清单；新增 `BuePauseColumnShift` 引用相等锚注册表；`BueNativeManagementPanel` 暂停入口重写（解析门/就绪门/事务移位/快照还原/每 tick X 镜像）；测试 +3 断言函数。
- **候选身份**：`BetterUnturnedExperience.dll` SHA-256 `3567026930757ffbc5d40b2da12b148f470dba806b5ca08abf14ac6e4a2caef6`（275456 字节，两轮重建逐字节一致）。不继承 07/12 发布批准。

## 人工实机复测清单（停等中）

1. 部署：把 `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` 复制到实机 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll`（多端都换）。
2. 指纹核对：启动后日志 `event=assembly-identity sha256=35670269…` 与候选一致。
3. ESC 截图验收：进任意单机/联机存档按 ESC——「BUE 插件管理」应位于「返回」正下方第二槽，与上下按钮间距和原版一致（60px 节距/10px 视觉间隙），右侧凸块消失；下方「游戏选项→返回桌面」逐项下移一格、间距不变。
4. 回归确认：ESC 各原版按钮（游戏选项/自杀/返回菜单/返回桌面等）功能正常；主菜单（09 修复）与面板开关不受影响。
5. 对照通过 → 本票置 resolved；不通过 → 回炉。

## 复测记录（2026-09-05）

用户部署候选 DLL（35670269…aef6）后实机测试：ESC 界面截图（归档 `audit/2026-09-05/DEV-V2-13/retest-esc-user-acceptance.png`）显示「BUE 插件管理」位于「返回」正下方第二槽、右侧凸块消失、下方原版项逐项下移且间距与原版一致；用户原话「BUE面板和原版功能都无异常，人工核验通过，我同意关闭工单」。

日志复核（UMM 诊断包 20260905_204812，归档 `audit/2026-09-05/evidence/DEV-V2-13-20260905/retest-r1/`）零异议：
- 指纹 `event=assembly-identity sha256=3567026930757FFB…` 与候选精确一致；
- `pause-column-ready ready=true` 一次翻转、`pause-column-shift anchored=9`（十清单减可选 invite，单机正确）、退出 `pause-column-restore restored=9 dropped=0 failed=0` 全量干净还原；
- `pause-shift-failed`/`pause-restore-failed`/`pause-element-read-failed`/`pause-shift-fields-missing` 全零；
- P5：BUE 零 Error 行；LogOutput 仅 2 行 `[Error :SteamP2PFriends]`（第三方实验插件自身 UI 探测与 ResourceObs 观测，非 BUE，同 12 复测先例）；
- 回归：三入口（MenuDashboardUI/MenuWorkshopUI/PlayerPauseUI）创建成功，`v1-table-mirror result=mirrored channels=2 deferred=true` 镜像链健康，退出 `hand-back-to-lmn` 清理线正常。

验收清单 5/5 达成，票 resolved。
