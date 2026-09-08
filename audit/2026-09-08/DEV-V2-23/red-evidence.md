# DEV-V2-23 红绿证据链

锚点：`--bue-v2-platform-red`（tests/BetterUnturnedExperience.Plugin.Tests/Program.cs，AssertBueV2PlatformSelfCheck，票面冻结六例收集式：无冲突/同程序集名冲突/不同程序集名/空路径/重复条目/诊断 id 与关键字段）。面板可见性为票面另一条验收（诊断在日志与面板可见），由独立断言 `AssertPlatformPanelNotice` 随套件常跑（F1 R1 裁定：不与六例锚混装，见 review-rounds.md 与票面 Comments）。

## 红链（先红）

1. **编译红（独立锚点）**：`compile-red.log` — CS0246（`BueLoadedAssemblyView` 类型不存在，tests/Program.cs:7870，1 错误；seam 视图类型与检查类 `BuePlatformDoubleInstallCheck` 同批缺失，七组/六例红测先于实现落盘，实现在观测编译红之后写入）。
2. 未走桩级/运行时红阶段（LHT 同形，如实留痕）：编译红已满足「先红」门；实现一次写入后锚点直绿，无「测试先于实现失败于真 bug」的行为红。

## 绿链（后绿）

- `impl-build1.log`：实现接入后 Rebuild 0 错。
- `impl-green-run.log`：锚点收集 ALL GREEN（首绿，当时为七组含面板组）。
- F1 后：`fix1-build.log`（0 错 0 警）+ `fix1-green-run.log`（六组 ALL GREEN）+ `fix1-plugin-suite-run.log`（Plugin 全量 PASS，含 AssertPlatformPanelNotice）。
- 终轮：`sln-rebuild-fullsuite.log`（sln Rebuild 0 错 0 警告）+ `fullsuite-run.log`（7/7 PASS：Contracts/Network/Plugin/Placement/Settings/ClientUi/Release）。

## 验收面覆盖对照

- 无冲突：注入干净清单 + 真实测试 AppDomain（映射含自身条目 IsSelf，Run 干净零发射）。
- 同程序集名冲突：异路径副本检出（含大小写变体保守口径）。
- 不同程序集名：Contracts/Core/异名不误报。
- 空路径：null/空 Location 以 `(路径不可用)` 占位仍报告；自身路径缺失不吞真实副本。
- 重复条目：同副本多次列举去重为一项、恰一条日志。
- 诊断 id 与关键字段：BUE-PLATFORM-001 + assembly/conflictLocation/selfPath/suggestion=移除非官方副本；Warning 级、每副本一条；null 清单 fail-fast。
- 面板可见（独立断言）：默认空/写入携带 id/Refresh 保留/条目集合不变/null 归位空。
- 无文件删除路径：决策核零文件 IO（仅 AppDomain 视图 + 字符串）；本票增量无任何 File/Directory API（全仓既有 File.Delete 仅设置 tmp 原子写清理，非本票面）。
