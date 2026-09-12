# DEV-V4-08 红测先行与测试缝证据

按时间序记录（红在实现之前观察）。全部证据落盘于本目录。

## 1. 编译红（Plugin.Tests，adapter 采集缝与结构化结果缝不存在）

- `red-plugin-build.log`（落盘）：`DevV4ExternalConfigParityTests.cs` →
  - `error CS1061: "LoadedPluginCatalogAdapter"未包含"CaptureConfigEntries"`（每插件采集缝尚不存在，×10 处调用）
  - `error CS1061: "bool"未包含"Accepted"/"Reason"`（IPluginConfigEditor.TrySet 仍返回 bool，结构化结果缝未开）

## 2. 运行时红（ClientUi.Tests，旧缝编译通过、文案缝未实现）

- `red-clientui-run.txt`（落盘）：旧接口签名下的拒绝假件（bool TrySet→false）驱动 SaveDraft：

  ```
  DEV-...-DEV-V4-08 ClientUi tests: FAIL
  System.InvalidOperationException
  PluginNotFound → 插件已卸载（Q70）：文案=「未保存：插件已卸载。」逐字
  ```

  —— 现网只有通用句「未保存：外部配置写入失败。」，三类冻结文案（插件已卸载 /
  配置文件无法写入 / 值不合法）如预期缺失。

## 3. 测试自身缺陷修正（同 02/07 先例，非实现回归）

- ClientUi 测试文件初版漏 `using BetterUnturnedExperience.Contracts;`（CS0246）与
  两处 `NewModel` 参数序笔误（CS1503）——红观察前的作者笔误，修正后重跑红。
- Plugin.Tests 红测组用 `new ConfigDescription(null, ...)` 构造被 BepInEx 拒
  （ArgumentNullException：description 不能为 null）→ 改 `string.Empty`。
- **BepInEx 5.4.23 事实修正**：`ConfigFile.IsReadOnly` 是恒 false 的桩（getter IL=
  `ldc.i4.0; ret`，无后备字段、无 setter），adapter 层无法构造 ReadOnly 场景 →
  删除该子案；ReadOnly 的文案映射由 ClientUi.Tests 模型组（RejectionPluginEditor
  结构化拒绝）钉住，adapter 层真实「无法写入」路径由文件锁（FileShare.None →
  Save 抛 IOException → PersistenceFailed）覆盖。已在测试注释与本审计注记。

## 4. 红测红利（旧缝缺陷被新组抓出）

- `UpmDetectedStillEditable` 组的「Cycle 下一档生效于草稿」在实现中段暴露 02 票
  遗留缺陷：`DraftCyclePluginConfig` 取当前档文本用 `PluginConfigValue.Text`——
  该字段仅 String 类有值，Integer/Float/Boolean 行恒为空串 → 档位永远回落第一档。
  修复=模型层档位匹配重做（见 §6 R2）。

## 5. GREEN

- 两个测试工程直跑全 PASS（`green-fullsuite-ClientUi-run.txt` / `green-fullsuite-Plugin-run.txt`），
  全套 7 运行器 PASS（`green-fullsuite-*-run.txt` ×7）。
- 全解决方案 Rebuild 0 警告 0 错误（`full-build.log`）。

## 6. 双轴审查驱动的修复轮（每轮全新双实例）

- **R1**（S=CLEAN 3 deferrable / Spec=1 blocking）：`WritableCandidate` 按抽象
  Kind 解析（byte 可受 300、uint 可受 -1）违反 Q66「候选可完整写回才画 Cycle」→
  改按 `entry.SettingType` 真实 CLR 类型校验（byte/uint 经 Convert.ChangeType
  越界即拒、float/double 拒 NaN/Infinity、decimal TryParse），红测组
  `CycleCandidatesRespectRealClrRange` 钉住。Standards 3 deferrable 同轮处理：
  徽章 NoChanges 语义对齐（null/NoChanges 不动徽章）、评审向注释修剪、
  TrySet catch 复用 RejectedAfterRestore。
- **R2**（S=CLEAN / Spec=2 blocking）：
  1. bool 档位转移错误——当前值格式化 "True" 与标签候选 "true" Ordinal 不匹配
     → 左键回落首档而非换档。修复=模型 `NextPluginCycleIndex`（bool 行按解析值
     匹配档位；非 bool 行按不变文化文本），`WrappedIndex` 收敛回绕数学，
     `NextCycleIndex`（BUE Choice 用，02 语义不变）委托同核。
  2. ulong 超 long.MaxValue 被降级 Unsupported（ADR-0002 #9「数字」不该折半）
     → `PluginConfigValue` 增 IsUnsigned/Unsigned64 载体 + ULongValue 工厂；
     adapter ToValue ulong→Convert.ToUInt64、ToSerialized 按 IsUnsigned；
     模型 TryParse long 优先、ulong 回落；ConfigValueDiffers 跨载体数值等价
     （Integer64Equals）；WithinBounds/CurrentLevelText/原生 FormatPluginValue
     按 IsUnsigned。红测组 `UnsignedFullRangeStaysEditable`（adapter：超 long
     采集/写回逐位保留+有符号载体写回同条目）+ `BoolAndUnsignedCycleTransitions`
     （模型：bool 转移、ulong 草稿往返、跨载体同值不脏）。BUE 设置路径隔离确认：
     原生 BUE 行走 `TryConvertSetting`（long-only），IsUnsigned 值不进 BUE mutation。
- **R3**（S=CLEAN / Spec=CLEAN）：双轴零 blocking。Standards 具名保留 1 项
  deferrable：`CurrentLevelText`/`ToSerialized`/`FormatPluginValue` 的 Integer
  分支同构（跨程序集、职责分属草稿匹配/序列化/原生显示，不为本票新立缝）。
- 每轮修复后全量重建（`full-build-r2.log`/`full-build-r3.log` 0 警告 0 错误）
  + 全套 7 运行器 PASS（`green-fullsuite-*-run-r2/r3.txt`）。

## 测试缝说明

- adapter 侧全部走真实 BepInEx `ConfigFile`/`ConfigEntryBase`（Bind 构造、真实
  序列化往返、真实 TOML 落盘、真实文件锁），经 `CaptureConfigEntries`（生产
  CaptureLoadedPlugins 的每插件内联缝，生产路径照常调用）——不打桩、不反射。
  Unsupported CLR 类型用**枚举**（BepInEx 有转换器、BUE 基础类型不含）真实 Bind 构造。
- 模型侧全部经 ManagementPanelModel 外显缝（SaveDraft 报告文案 / GetPluginConfigRows
  控件形状 / Draft 命令），拒绝假件只服务结构化分类缝；不断言 Glazier 控件树。
- 面板 RequiresRestart 顶部徽章为原生渲染面（Q67 归 08），语义门在模型
  DraftSaveReport.RequiresRestart（01 已测：仅本次成功写入才置位），原生面无模型
  缝可断言（与 05/06/07 原生面口径一致）。
