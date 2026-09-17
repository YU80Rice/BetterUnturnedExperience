# DEV-V5-08 实机修复轮 F1/F2（候选 v1 bb33c2dd 作废 → v2 d7549806）

## 根因（真机实证链）

- SP 轮1（候选 v1）：升级按钮点击零痕迹；双击 R 每次触发「技能窗判定异常: Object reference」×7（fail-open 兜住→双击仍成交、但 0 级合并窗从未武装）。
- 探针 #1（dc948b54，非候选）：`buttonEnabled=True xp=3689`、升级链抵达 ExecuteUpgrade 抛 NRE；窗口全栈钉到 `CharacterKeyOfPlayer [0x0015]`。过程具名：入口探针行被 LogDiagnostic 全局节流吞并（「此前抑制 6 条」）——诊断通道教训。
- 探针 #2（e5ef12ac，非候选）：逐环判空+LogError（不节流）——PROBE2 行仍缺席、NRE 偏移移至 [0x0033]。
- 静态定案（Cecil 读宿主 IL，脚本 inspect4..10.ps1 留档）：`SDG.Unturned.SteamPlayerID : System.Object` 定义**无判空自定义 ==**（`op_Equality(a,b)` 两侧直接 `callvirt get_steamID`）→ 源码任何 `playerID == null` 编译成对该运算符的调用，**无论实值是否 null 必抛 NRE**（null 侧 callvirt 先炸）。探针 #2 的 `pid == null` 同样中招，故其 LogError 永不可达、偏移随指令序移动——与两份日志逐字吻合。
- 连锁解释：GetLevelFor fail-closed→分区恒画 0 级；技能窗恒抛→不武装；升级恒抛→拒绝且账零改（经验 3689 未动=方向安全）。Lht OwnerResolver 用 `ReferenceEquals(sp.playerID, null)` 生产先例=正确形状早已存在。

## 修复（代码增量=唯一 src 改动）

`LirSkillEngineHooks.CharacterKeyOfPlayer`：逐环判等一律 `is null`（IL 引用比较，不经运算符），保持 fail-closed（任一环 null→返回 null→NormalizeCharKey 归空串）。探针全部撤除（git diff 实证：模块/适配器回零差异；V508-PROBE 残留=0）。

## 红测先行（新组 DevV508IdentityIlGuardTests，Program.cs 注册+默认跑+旗标 --bue-v5-08-identity-il-guard-red）

- 判据=BUE 程序集内任何 BetterUnturnedExperience.* 方法体 IL 不得含 `SteamPlayerID::op_Equality/op_Inequality` 调用（Cecil 逐指令扫描，扫描异常=守卫失效=红）；锚组钉 CharacterKeyOfPlayer/NormalizeCharKey 在册防改名空转。
- 证红：修复前源码（含探针 `pid == null`）→ exit 1，违规清单精确命中 `CharacterKeyOfPlayer @IL_0x0035/@IL_0x00B3 call -> op_Equality`（red-il-guard-run1.txt）。
- 转绿：修复后 exit 0（green-il-guard-run.txt）。
- 突变 M1：`is null`→`== null` 复红（exit 1，mutant-M1-red.txt）→还原复绿（exit 0，mutant-M1-restored-green.txt）——守卫非空转。

## 构建与门禁（fix-* 证据）

- 7 套件 7/7 exit 0、0 FAIL（fix-fullsuite-*.txt）。
- 六门禁：5 PASS + NoUiTokens-Contracts 命中=V2-02 基线逐字相同（diff 实证）。
- 3× `-t:Rebuild` 逐字节一致：`D7549806DA41AE88CC4A866E839B28D371B84972C386FFF7D6B66F75C1326BA0`（725504B，fix-rebuild-1..3.txt 含 runner footer）。
- 契约仍 2.1：修复全 internal，Contracts 零 diff 不变。

## 身份

- v1 `BB33C2DD…4E16B` **作废**（F1/F2 本体），不得用于采集；探针 dc948b54/e5ef12ac 非候选。
- v2 `D7549806…326BA0` = 当前开发态候选，CaseId 不变 `DEV-V5-08-CANDIDATE-20260916`；SP+U3DS 双端在位哈希已复核=d7549806。
- 待办：v2 双轴审查（本文件即送审工件）→ SP 复测（升级链+技能窗+未做腿）→ P2P/U3DS。复测硬门槛：每轮会话日志 assembly-identity 必须=D7549806DA41AE88CC4A866E839B28D371B84972C386FFF7D6B66F75C1326BA0，不匹配=停测清残留重部署，v1/探针证据不得续用。
