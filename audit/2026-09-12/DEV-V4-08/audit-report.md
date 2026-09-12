# DEV-V4-08 审计·外部配置同等升级

- 票：`.scratch/bue-v2-phase4-visual-experience/issues/DEV-V4-08-external-plugin-parity.md`
- 规格：`spec.md`「外部配置同等升级（V4-T7 → DEV-V4-08）」节；裁决 `issues/07-t7-external-plugin-parity.md`（Q65–Q70）；ADR-0002 第 2、9 条
- 日期：2026-09-12；会话：Phase-4 实施票 08（依赖 01/02 已 resolved，按冻结依赖图领取）
- 构建：MSBuild（VS 18 Insiders）Release `-t:Rebuild` → **0 警告 / 0 错误**（终轮 `full-build-r3.log`；r2 同）
- 测试：**全套 7 运行器全 PASS**（逐 runner `green-fullsuite-*-run-r3.txt`）；静态门：Core 30 文件 NoUiTokens PASS、模型四文件（ManagementPanel/PanelChromeCopy/BetterItemInteractionLifecycle/LoadedPluginCatalogAdapter）UI/native token=0、`git diff --check` exit 0（`static-gates.txt`）
- **候选纪律（本票 08）**：不产正式候选 DLL、不更新 RELEASES、不授 CaseId。契约仍 2.1（Contracts 零文件入 diff）。

## 交付摘要

外部 BepInEx 插件目录与官方同权的「看得懂、改得动、存得上」三缝，全部落既有 adapter/模型/原生缝，零契约成员变化：

- **描述采集**（`LoadedPluginCatalogAdapter`）：配置行描述来自 `ConfigDescription.Description`（采集保原文，120 截断归投影层 Q37），空=空串由渲染层跳行（Q65 空不画）；`HasDiscreteConstraint→整行 Unsupported` 的旧形状摘除。
- **Cycle 形状两层（Q66）**：类型层不变（bool/数字/字符串，其它 CLR 类型如枚举仍 Unsupported 只读）；形状层=Unturned.Cycle 标签（冒号后 `|` 分割档位、裸标签回落 AcceptableValueList——UPM 快照同序）或 AcceptableValueList，候选非空且按**真实 CLR 类型**逐个可写回才画 Cycle（byte 拒 300、uint 拒 -1、float/double 拒 NaN/Infinity）；候选不可写回/未识别离散约束→普通控件进草稿，不再整行只读。
- **结构化失败（Q70）**：`IPluginConfigEditor.TrySet` 返回 `PluginConfigEditResult`（复用既有 rejection 枚举），adapter 自分类（GUID/条目失效→PluginNotFound/EntryNotFound、IsReadOnly/文件锁→ReadOnly/PersistenceFailed、往返不一致（BepInEx 对非法值静默忽略的唯一暴露缝）/类型不符→InvalidValue/UnsupportedType），面板 `ExternalSaveFailureText` 渲染三类冻结短中文（插件已卸载/配置文件无法写入/值不合法），不匹配异常文本、不画堆栈；失败保留草稿、失败不打重启徽章。
- **UPM（Q68）**：`com.trae.pluginmanager` 仍列出、受支持 cfg 按同一草稿/Cycle/校验/保存编辑（adapter/模型双层红测钉住「识别标签≠剥夺能力」）；底栏文案逐字未动。
- **RequiresRestart 徽章（Q67）**：行级（需要重启）固有属性照常；原生详情页顶部新增「需要重启」徽章（01 审计 F4 具名移交面）——仅本次 attempted save 成功写入 RequiresRestart 项才点亮，NoChanges/取消不重裁，行级标记独立承担持久指向。
- **草稿纪律**：写入走 01 草稿（编辑零写盘、保存才逐条 TrySet、失败留草稿不假成功），与 BUE 设置同一套保存顺序与确认语义。
- **Spec 轴两轮驱动的保真修复**：Cycle 档位匹配按真实 CLR 类型（R1）；bool 档位按解析值匹配+ulong 全域数值载体（R2，ADR-0002 #9 数字不折半）——详见 `red-first-evidence.md` §6。

## 红测先行与测试缝

红→绿链全程见 `red-first-evidence.md`。要点：

- 编译红（Plugin.Tests：CS1061 CaptureConfigEntries/bool.Accepted）+ 运行时红（ClientUi.Tests：旧缝下三类冻结文案缺失）均在实现前观察落盘。
- 红测红利：新组抓出 02 票遗留缺陷（外部非字符串行 Cycle 恒回落首档）。
- adapter 侧全部走真实 BepInEx `ConfigFile`/`ConfigEntryBase`（Bind 构造、真实序列化往返、真实 TOML 落盘、真实文件锁），经 `CaptureConfigEntries`（生产路径内联缝）；Unsupported 类型用枚举真实 Bind 构造，无反射无打桩。模型侧全经 ManagementPanelModel 外显缝（SaveDraft 报告文案/行投影控件形状/Draft 命令），不断言控件树。
- 命名 deferrable 缝记录：`ConfigFile.IsReadOnly` 在 BepInEx 5.4.23 是恒 false 桩（getter IL `ldc.i4.0;ret`、无后备字段），adapter 层 ReadOnly 场景不可构造——其文案映射由模型组（结构化拒绝假件）钉住，adapter 层真实「无法写入」路径由文件锁组覆盖。

## 双轴独立审查链（每轮全新实例，无 SendMessage 续用）

### Round 1
- Standards（新实例）：**CLEAN**，3 项 deferrable（徽章 NoChanges 注释与行为不一致；多处评审向注释；CurrentLevelText/ToSerialized 同构 + catch 重复）。
- Spec（新实例）：**FINDINGS** — 1 blocking：WritableCandidate 按抽象 Kind 解析（byte 可受 300、uint 可受 -1 → 违反 Q66 可写回才画 Cycle）。

### Round 2（修复：WritableCandidate 按 SettingType 真实 CLR 类型 + 红测组；Standards 3 deferrable 同轮处理）
- Standards（新实例）：**CLEAN**（保留跨程序集同构 1 项具名 deferrable）。
- Spec（新实例）：**FINDINGS** — 2 blocking（新）：bool 档位转移错误（"True" vs "true" Ordinal 不匹配→左键回落首档）；ulong 超 long.MaxValue 被降级 Unsupported（违反 ADR-0002 #9 数字基础类型）。

### Round 3（修复：NextPluginCycleIndex bool 按解析值匹配 + WrappedIndex 收敛；PluginConfigValue 无符号载体 IsUnsigned/Unsigned64 全链贯通 adapter/模型/原生，BUE 路径隔离确认；新增红测组 UnsignedFullRangeStaysEditable / BoolAndUnsignedCycleTransitions）
- Standards（新实例）：**CLEAN**，1 项具名 deferrable（Integer 分支同构：CurrentLevelText/ToSerialized/FormatPluginValue——跨程序集、职责分属草稿匹配/序列化/原生显示，不为本票新立缝）。
- Spec（新实例）：**CLEAN**，零 gap、零 deviation、零蔓延；R1/R2 修复确认，ulong 扩域无新增保真风险（BUE 行走原生 TryConvertSetting long-only，IsUnsigned 值不进 BUE mutation）。

**结论：三轮评审链于 Round 3 双轴 CLEAN（1 项具名 deferrable 显式保留）。** 中间构建无身份；本票按 08 纪律不赋候选身份、不授 CaseId。

## Deferrable smells

1. `CurrentLevelText`（ManagementPanel.cs）/`ToSerialized`（LoadedPluginCatalogAdapter.cs）/`FormatPluginValue`（BueNativeManagementPanel.cs）的 Integer 分支同构（Kind→Invariant 文本，IsUnsigned 选载体）——跨程序集（ClientUi/Plugin）、职责不同（草稿档位匹配/序列化写盘/原生显示），收敛需新立跨程序集缝，不属本票（R1 命名、R3 复核未恶化）。
