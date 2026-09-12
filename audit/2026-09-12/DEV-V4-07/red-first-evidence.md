# DEV-V4-07 红测先行与评审链证据

按时间序记录（红在实现之前观察）。三处证据落盘于本目录，其余为会话内直跑输出（逐字引录）。

## 1. 编译红（两工程同缝，测试驱动出新投影缝）

- `red-clientui-build.log`（落盘）：`DevV4OfficialCopyTests.cs` →
  - `error CS0103: 当前上下文中不存在名称"PanelChromeCopy"`（chrome 对照表类型尚不存在）
  - `error CS1061: "ManagementPanelModel"未包含"GetFeatureDescription"`（×9 处调用）
- `red-plugin-build.log`（落盘）：`Program.cs(12005)` →
  - `error CS1061: "ManagementPanelModel"未包含"GetFeatureDescription"`（锚③组引用同缝）

## 2. 运行时红（chrome 缝落地后、文案缝实现前）

- ClientUi.Tests 直跑（会话内输出，未落盘）：
  ```
  DEV-05/.../DEV-V4-07 ClientUi tests: FAIL
  System.InvalidOperationException
  AutoRotate 显示名=「自动旋转」（Q62 冻结）
  ```
  —— chrome 三组已转绿，BII 显示名如预期红（真实编辑器仍无描述符，回退 SettingId）。
- `red-plugin-run.txt`(落盘) = Plugin.Tests 首轮运行时红：
  ```
  System.InvalidOperationException: 消费锚：整理模式描述=Q61 冻结原文逐字（DEV-V4-07 交接落地，06 时为空不画）
  ```
  —— DEV-V4-06 留下的交接断言（描述键为空）按计划翻转后红，LIT 描述句如预期缺失。

## 3. 测试自身缺陷修正（同 02 先例，非实现回归）

- 锚③组初版断言「保存后右键拨回默认档不再脏」逻辑错误：保存后基准已变为乙，
  再右键回甲相对新基准必脏（Q41 的「拨回不脏」只在同一草稿会话内成立）。
  修正断言序列：+1 脏 →（会话内）-1 不脏 → +1 待保存 → 保存成功 → 空操作保存。
  运行时红逐字（会话内输出）：`InvalidOperationException: 锚③：右键上一档（乙→甲）拨回默认档不再脏`。
- 注：Plugin.Tests 直跑为「首败即停」口径，NoOp/LIT 各组的红无法逐组独立观察；
  组级红以锚③组编译红（CS1061 即在该组）与 06 交接断言翻转红为准，其余组随批转绿。

## 4. 评审驱动的测试缝收敛（Round 2/3，实现零改动、只动测试）

- Round 2 Spec finding：`KnownFeatureIds.Count` 断言读内部键集 → 删除该内表面与断言，
  七句逐字全经 `GetFeatureDescription`（Round 3 落实时 chrome 部分已如此）。
- Round 3 Spec finding：LIT/NoOp/enabled 组直读 `CreateSettingsDescriptors` /
  `GetDescriptors` / `SettingDescriptors`，绕过面板外显缝 → 删除三个直读组，
  文案断言全部收敛到 `GetSettingRows` 行投影缝（新增「行投影 Q61 描述逐字与退役
  enabled 对拍」组=真实宿主 LIT+NoOp 路由；BII 行级对拍归 ClientUi.Tests；
  锚③补 probe-toggle 默认开；06 消费锚组的描述断言移交 07 组）。
- Round 4：修复 Round 3 两条 deferrable（测试方法名去 Descriptors 字样 →
  `BiiRetiredEnabledStaysOutOfPanelRows`；`IBueSettingsEditor.GetDescriptors`
  接口注释与 BII 现状对齐）。评审终轮双轴 CLEAN、零递延。

## 5. 转绿

- 每轮评审修复后均全量 `-t:Rebuild`（0 警告 0 错误：`full-build-r2/r3/r4.log`）并直跑
  全套 7 运行器（`green-fullsuite-*-run-r2/r3/r4.txt` 全 PASS；最终轮=r4）。
- 静态门：Core 30 文件 NoUiTokens PASS（`static-gates-nouitokens-core.txt`）；
  模型三文件（ManagementPanel/PanelChromeCopy/BetterItemInteractionLifecycle）token=0；
  `git diff --check` exit 0。

