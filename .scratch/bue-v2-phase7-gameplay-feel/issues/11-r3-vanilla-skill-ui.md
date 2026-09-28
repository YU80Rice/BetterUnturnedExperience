# V7-R3 原版技能页模板与可挂点

- **Ticket**: V7-R3
- **Type**: research（AFK，子代理执行）
- **Status**: resolved
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

T4 的事实输入。对照原版技能 UI 与现网 LIR 分区产出现状报告（`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R3-vanilla-skill-ui.md`）。

必须回答（只查证不改码，结论带 file:line；原版源优先 U3-SDK / 游戏程序集，其次本仓现网适配器）：

1. 原版 `PlayerDashboardSkillsUI` / `SleekSkill`（或等价）一行包含哪些可见零件（图标、名称、等级点、花费、描述、加点按钮）。
2. 现网 `ReloadSkillDashboardAdapter` / `ReloadSkillDashboardSurface` 注入点（`updateSelection`、`skillsScrollBox`）、几何常量、为何是自造行而不是原版控件。
3. 能否在不写入 `Skill[][]` 的前提下复用原版技能行控件（构造参数、是否强制绑 `Skill` 资产）。若不能，最小复刻需要哪些 Glazier 控件。
4. 现网表面 B（设置页降级）何时出现；与表面 A 互斥条件。
5. 0/1/2 级现网文案单源（`ReloadSkillPolicy`）与面板对照表原文。

不要在本票设计新 UI。不要假设可以安全写入原版技能数组。

## Answer

一手对照已落：`.scratch/bue-v2-phase7-gameplay-feel/research/2026-09-21-V7-R3-vanilla-skill-ui.md`（U3-SDK `ea7b497` + 本仓 LIR `Skill/` / `Skill/Ui/`）。未改 `src/`。

1. **原版一行** = `SleekSkill(speciality, index, Skill)`：全幅 `CreateButton`（tooltip + 可点=经验够且未满）+ 每级一根 Unlocked/Locked `CreateImage` + 左上名称级 `CreateLabel` + 20×20 职业小图标 + 左下描述（与 tooltip 同键）+ 条件「当前/下级加成」+ 右下花费/`Full`。无独立加点按钮。页几何 **Y 步进 90、行高 80**（`SleekSkill.cs:13-144`；`PlayerDashboardSkillsUI.cs:51-68, 104-114`）。`PlayerDashboardSkills.dat` **未找到**。
2. **现网** postfix `updateSelection(byte)` → 战斗页且在册且镜像已确认才注入；反射 `skillsScrollBox`/`skills`；几何 `RowHeight=36`、`ButtonHeight=40`、`SectionTopGap=8`、原版步进字面 90。自造 `CreateBox`+`CreateLabel`/`CreateButton`，因为 `SleekSkill` 强制绑原版槽位三元组。
3. **不能**在不写 `Skill[][]` 的前提下复用 `SleekSkill`：`skills` 只读、`_skills` 私有且 InitializePlayer 定长；构造仍读 `skills.cost(speciality,index)`，点击按 Y/90 `sendUpgrade`。最小视觉复刻原语 = `CreateButton` + `CreateImage` + `CreateLabel`（± `SleekWrapper`），不是 `SleekSkill`。
4. **表面 B** 仅注册期 `ProbeSurfaceA()==false`（`updateSelection(byte)` 命中≠1）时挂 settings facet；与表面 A 类型互斥、探测缓存、进程内不来回切。U3DS headless 砍画面、不是表面 B 条件。
5. **文案单源** `ReloadSkillPolicy`：`换弹技能`；`基础` / `快速换弹` / `自动压弹`；行 `等级 n · …`；满级加 ` · 已满级`；按钮 `升级到 t 级 · 花费 125|150 经验`。表面 B 维持档在 `ReloadSkillSettingsSurface`：`维持当前等级（不请求）`。分区行不展示 8s 冷却/等待数字。
