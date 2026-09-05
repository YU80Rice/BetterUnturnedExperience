# DEV-V2-13 收尾报告：ESC 暂停菜单 BUE 入口并入原生按钮列

日期：2026-09-05。工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-13-esc-pause-button-into-native-column.md`（建票+triage+实施同日同回合完成，Agent Brief 为实施契约）。

## 根因与机制（triage，实机反编译复核）

原版 `PlayerPauseUI` 左列：全部按钮 X=-100、200×50、PositionScale(0.5,0.5)、列起点 Y=-290、节距 60（return→[invite 条件]→options→display→graphics→controls→audio→suicide+禁用标签→exit→quit，全部 static 字段反射可达）。BUE 旧注入在 X=205/Y=-290 形成右侧凸块。机制：BUE 按钮入列第二槽（-230=return+60），Return 以下十元素按实例锚定 +60；spy 模式（整列 X→-435，原版怪癖不回位）由每 tick 镜像列首存活元素 X 跟随；BUE 拆除/重建时快照还原原布局。

## 红绿链

- **红0（编译红）**：测试先行 → CS0117×26 + CS0246×2。`red0-compile.log`。
- **红1（行为红）**：十字段对真实 Assembly-CSharp 解析断言——`exitButton`（public static）被 NonPublic 漏解析（R1-Spec 实锤 bug）。`red1-exitquit-resolve.log` → BindingFlags 改 Public|NonPublic → 绿。
- **红2（行为红）**：Equals 相撞键错误共享锚（J3 隐患）→ 引用相等比较器。`red2-collidingkeys.log`。
- **红3（行为红）**：Restore 先删锚后写值，失败不可重试 → 先写值后删锚。`red123-behavioral.log`。
- **绿**：sln Release 重建 0 警告 0 错误（`green-sln-rebuild.log`）+ 七套测试全 PASS（`green-BetterUnturnedExperience.*.log`×7、`green-after-red123.log`）。

## 双轴评审链（R2 起按用户拍板固定专用审查员子代理：standards-reviewer / Spec-Reviewer）

| 轮 | Standards | Spec | 处置 |
| --- | --- | --- | --- |
| R1 | FINDINGS F1-F5 + J1/J2 | FINDINGS（exit/quit 反射 bug 实锤、重建帧重叠、spy 时序、截图未做） | 红1-红3 落地；BindingFlags/重排/镜像/可选清单修复 |
| R2（Explore 混派，含一轮被取消） | FINDINGS：F3 未真闭合（helper 静默失败致 failed 计数失真→Clear 吞可重试锚）、F5 运行期缺口 | 同向 + 部分移位无回滚 | 就绪门（必需元素全集）+ 快照还原 + Clear() 退出生产路径 |
| R3 | **CLEAN**（J2/PauseSpyColumnButtonX/Data Clump 具名延期） | FINDINGS：写阶段中途失败部分移位不回滚 | 事务性两阶段：先全读后全写，写/镜像失败按锚回滚+收回按钮 |
| R4 | **CLEAN**（延期维持具名） | FINDINGS：票面仍声明已移除的 `Clear()` | 票面契约同步（代码零改动） |
| R5（Spec 复核） | — | **CLEAN** | 环路闭合 |

具名延期（不阻塞）：
1. **实机截图验收**（工单验收条件 2）：BUE 按钮位于「返回」下一行、上下间隙==原版节距、右侧凸块消失——须真实游戏环境，停等人工。
2. **面板级 seam gap**：Glazier 绑定路径 headless 不可构造（事务回滚/spy 镜像/重建窗口的行为验证依赖评审+实机），headless 测试钉住契约常量、清单、锚定语义（红1-红3 覆盖）。
3. **J2**：Main/Pause 同值几何常量（工单明确要求独立契约，不合并）。
4. **`PauseSpyColumnButtonX`** 仅作测试锚钉原版反编译值；生产镜像读实时列 X。
5. **Data Clump**：`PauseShiftFieldNames`/`pauseShiftFields`/`elements[]` 平行下标表（N=10），结构体化收益不抵改动，具名不动。

## 变更清单

- `BueMenuEntryLayout.cs`：+暂停菜单节（X=-100、200×50、节距 60、Return -290、BUE -230、spy -435、`PauseShiftFieldNames` 十字段、`PauseOptionalShiftFieldNames`=inviteFriendsButton）。
- `BuePauseColumnShift.cs`（新增）：引用相等锚注册表——Apply（返回是否新锚）、Restore（先写后删、失败保锚）、SnapshotAnchors、RemoveAnchor；无 Glazier 依赖。
- `BueNativeManagementPanel.cs`：`ResolvePlayerPauseShiftFields`（Public|NonPublic）+ 构造期必需字段 fail-closed 门（一次性 `pause-shift-fields-missing`）+ 运行期就绪门（`pause-column-ready`，必需元素任一不可用→整列不动并收回已注入按钮）+ `TryAddPauseButton` 重排（null 门→就绪门→rebind 清理→CreatePauseButton→事务性 Apply）+ `ApplyPauseColumnShift` 两阶段（读失败即退、写/镜像失败按锚回滚）+ `RestorePauseColumn` 快照走查（存活还原/死实例弃锚/失败保重试锚）。
- `tests/.../Program.cs`：+`AssertPauseMenuEntryLayoutMatchesNativeColumn`（常量+清单钉死）、`AssertPauseColumnShiftAnchorsWithoutDrift`（锚定/幂等/独立锚/重试/引用键/快照/弃锚）、`AssertPauseShiftFieldsResolveAgainstVanillaAssembly`（十字段解析）。
- csproj：+`BuePauseColumnShift.cs`。

## 实机复测（2026-09-05，闭环）

用户部署候选 DLL（35670269…aef6）后实机测试通过（截图 `retest-esc-user-acceptance.png`：BUE 按钮位于「返回」正下方第二槽、凸块消失、间距与原版一致；用户原话「BUE面板和原版功能都无异常，人工核验通过，我同意关闭工单」）。日志复核（证据 `../evidence/DEV-V2-13-20260905/retest-r1/`）：指纹精确匹配、`pause-column-shift anchored=9`、退出 `restored=9/failed=0`、四类失败事件全零、BUE 零 Error 行（仅 SteamP2PFriends 第三方 2 行，非 BUE）、镜像链 channels=2 健康。具名延期 1（实机截图验收）就此闭合——具名延期余 2-5（见上节，均为有意不动的设计取舍/可延期 smell）。工单 resolved。

## 身份（双 CLEAN 后授予）

- 产物：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- SHA-256：`3567026930757ffbc5d40b2da12b148f470dba806b5ca08abf14ac6e4a2caef6`（275456 字节）
- 确定性：同源两轮 `-t:Rebuild` 逐字节一致（`identity-rebuild1.log`/`identity-rebuild2.log`/`identity-sha256.txt` 留盘）
- 边界：不继承 07/12 发布批准；实机验收通过后由用户决定采纳。
