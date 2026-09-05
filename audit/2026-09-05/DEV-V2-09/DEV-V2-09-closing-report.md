# DEV-V2-09 收尾报告：主菜单注入按钮间距与原版节奏对齐

日期：2026-09-05。工单：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-09-main-menu-button-spacing.md`（triage 同日完成，Agent Brief 为实施契约）。

## 根因（triage 定位，实机反编译复核）

原版 `MenuDashboardUI` 左列全部为 container 直接子级、200×50、节距 60px（Play 170 / Survivors 230 / Configuration 290 / Workshop 350 / 商店 `SleekItemStoreMainMenuButton` 410，条件性存在），相邻视觉间隙 10px。BUE 注入按钮硬编码 `PositionOffset_Y = 460f` = 商店按钮底缘（410+50），零间隙贴合，比原版节奏紧 10px。证据：Libs 快照（2026-08-11）与实机（E:\Steam，2026-09-04）两份 Assembly-CSharp.dll 反编译参数一致。V1 注入器不碰主菜单左列（票面假设排除）。

## 变更

- 新增 `src/BetterUnturnedExperience.Plugin/BueMenuEntryLayout.cs`：纯 C# 布局契约（列缘 X=0、尺寸 200×50、节距 60、商店槽 410、BUE 槽 = 410+60 = 470），已登记 Plugin csproj。
- `BueNativeManagementPanel.TryAddDashboardButton`：布局字面量改为从契约取值；陈旧注释（「user-specified y=460」）更正为节距槽位语义。
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`：新增 `AssertMainMenuEntryLayoutMatchesVanillaRhythm`（六条断言，含 470 值钉死与 410+60 自洽），挂入主流程注入断言序列。

## 红绿链

- **红0（编译红）**：测试先行落盘后构建 → CS0103 `BueMenuEntryLayout` 不存在 ×8。留证 `red0-compile.log`。
- **红1（行为红，R1-F1 修复项）**：把契约节距临时改为 50f（bug 形状 = 底缘贴合出 460）→ 套件 FAIL，断言咬住「BUE main-menu column pitch matches the vanilla 60px rhythm」。留证 `red1-behavioral.log`。复原 60f 后回绿。
- **绿**：`-t:Rebuild -p:Configuration=Release` 全解决方案 0 警告 0 错误（`green-sln-rebuild.log`）；七套测试全 PASS（`green-BetterUnturnedExperience.*.log` ×7）。

## 双轴评审链

| 轮 | Standards | Spec | 处置 |
| --- | --- | --- | --- |
| R1 | CLEAN | FINDINGS F1–F4 | F1/F3 修复；F2/F4 具名延期（见下） |
| R2 | **CLEAN** | **CLEAN** | F1 闭合核验（红1 为运行时断言失败非构建失败）；F3 闭合核验（七套 PASS）；无 scope creep；延期如实具名。双 CLEAN，环路闭合 |

R1 Spec 各条处置：

- **F1 红测证据不充分（只证编译红）** → 已修：补红1行为红（bug 形状注入契约，断言咬住后复原），`red1-behavioral.log`。
- **F2 实机截图缺失** → 具名延期：须真实游戏环境前后对照，属人工实机步骤（与 10/11/12 同先例，停等人工复测）；验收清单已写入工单。
- **F3 ClientUi 绿证未落盘** → 已修：七套绿证全部落盘本目录。
- **F4 双轴流程未闭环** → 流程进行时固有项，随 R2 双 CLEAN 闭合。

## 具名延期（不阻塞）

1. **F2 实机截图前后对照**：人工在游戏内验证「BUE 插件管理」与商店位间隙 == 原版相邻项间隙（部署新 DLL 后照工单验收清单执行）。
2. **注入路径取值的 headless 不可直测**（triage 已具名的 seam gap）：`TryAddDashboardButton` 为 Glazier 绑定路径，headless 测试只能钉契约值，调用点绑定由双轴评审把关；值漂移由契约测试防复发。

## 身份（双 CLEAN 后授予）

- 产物：`src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
- SHA-256：`3370f5d8106b4121b635f584b83ceffae971456cfa6040ae886db9ffb2e74007`（270848 字节）
- 确定性：同源两轮 `-t:Rebuild` 逐字节一致（`identity-rebuild.log` + `identity-sha256.txt` 留盘）
- 边界：本候选不继承 07/12 已授予的发布批准；实机截图对照通过后由用户决定是否作为正式增量采纳。
