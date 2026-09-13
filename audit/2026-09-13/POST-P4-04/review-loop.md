# POST-P4-04 关单报告 — UPM 分类导航与列表配置的文本路径

票：`.scratch/bue-post-phase4-closure/issues/04-upm-category-list-text.md`（enhancement）
实现提交：`633a8be`（main）· 类型=票 04 非发布票，**不授候选、RELEASES 不动、契约仍 2.1（Contracts 零改动）**

## 交付面（9 文件，+607/−9）

- `LoadedPluginCatalogAdapter.cs`：`Unturned.Category:<名>` 解析（trim；空名/无标签回落配置节，空分类归「通用」在投影侧做）；ItemList/BlueprintList/CreatureList 裸标签+冒号后缀两式 → 逐字格式提示（Item/Blueprint/Creature 三条冻结文案 const 锚定），固定 Item>Blueprint>Creature 优先序；**列表标签压过 Cycle 标签与 AcceptableValueList**（不投影档位→行=文本框）；未识别列表样式标签字符串底层仍普通可编（Q69 纪律延伸）。
- `ManagementPanel.cs`（纯模型，零原生 token）：`PluginConfigEntryView`/`PanelConfigRowView` 新增 `Category`/`ControlHint`（默认空串，既有生产者行为不变）；`GetPluginConfigCategories`（去重、首现序、空/空白归「通用」、未注册如实空集）；`GetPluginConfigRows(stableId, category)` 过滤重载（空白=全量=既有语义；草稿生效值/脏标记照常）；`TryEditPluginConfig`/`SaveDraft`/`ProjectConfigRow` 重建点透传，杜绝元数据漂移。
- `BueNativeManagementPanel.cs`：多分类才画 chips（`categories.Count > 1`），单分类平铺不画导航；筛选态随插件切换复位+失效分类回落首条；chip 点击只重绘详情不动草稿会话（`OpenDetail` 同键保草稿语义，Spec 轴核实）；列表行在文本框上方画格式提示（空不画不占位，与描述行同纪律）。
- 测试：ClientUi 模型组 6 案 + Plugin.Tests 真实 ConfigFile 采集组 4 案；csproj 白名单与 Program 注册齐。

## 红→绿链

1. 红（编译级，新缝不存在）：`red-clientui-compile.log.txt` 17 错（CS1061/CS1501/CS1729）；`red-plugin-compile.log.txt` 11 错（CS1061）。
2. 绿：`green-sln-rebuild.log.txt` 全套 Rebuild **0 错误 0 警告**；7 个测试 exe `green-fullsuite-*-run.txt` 全 exit=0，含 `POST-P4-04 ClientUi category/list tests: PASS` 与 `POST-P4-04 Plugin.Tests UPM tag parsing tests: PASS`。R1 增量冻结件=`review-freeze-r1.txt`。
3. 回归保持：DEV-V4-02/08 的 Cycle/描述/草稿组全绿（`UnrecognizedTagsAreSilentlyIgnored` 等既有断言零改动通过）；`eng/Verify-TestFixturesTracked.ps1` PASS 2 项。

## 双轴（每轮全新实例，Fresh-instance 规则）

- **R1 Standards（standards-reviewer 新实例）**：**CLEAN**。Blocking 0。Deferrable-smell 2 条 → **具名递延**：
  1. `tests/BetterUnturnedExperience.ClientUi.Tests/Program.cs:31` FAIL 横幅未追加 POST-P4-04（PASS 横幅已含；纯文案不对称）。
  2. `DevTicket04UpmCategoryListTests.cs` helper `configRow` 小驼峰，同文件其余 helper 为 PascalCase（纯命名）。
  递延理由：均不承载行为、修一动即触发 R2 全轮重审；按 POST-P4-02/03 先例具名递延不阻塞。
- **R1 Spec（Spec-Reviewer 新实例）**：**CLEAN**。Gap 0、Deviation 0；逐条对照票面验收核实（含 chips 仅 >1、草稿会话不被 chip 破坏、冒号式既有测试语义、范围外四项零触碰）。

## 边界实证

- GitHub PR #1 保持不合并：`git merge-base --is-ancestor d784d7f main` = 非祖先；main 无 pr-1 merge 提交。`pr-1` 分支本体零改动（仅读取参考解析函数与测试意图）。
- PR 上维护者留言「已在 main 重做，请关 PR」=票面注明**非本票强制**，留维护者自领。

## 缝缺口与门禁面记录（每缺口具名，不静默）

- 原生面板 chips/提示行渲染为宿主绑定面（Glazier），纯宿主不可构造——按既有 Testing Decisions「只经模型投影缝断言外显行为」：导航判定=`GetPluginConfigCategories` 计数、行内容=`GetPluginConfigRows(stableId, category)`；本票模型缝全覆盖，渲染层为薄投影无独立逻辑。
- `eng/Verify-NoUiTokens.ps1` 未跑作本票门禁：其 DEV-15B 时代 PASS 基线已漂移——现 `src/BetterUnturnedExperience.ClientUi/ItemInteractionUiComponent.cs` 与 `Contracts/ContractTypes.cs` 票前即含 Glazier 引用（非本票改动，本票新增纯模型代码不含任何原生 token，已 grep 核实）。该门禁是否重定标属仓库级另案，本票不擅改脚本或扫描范围。
