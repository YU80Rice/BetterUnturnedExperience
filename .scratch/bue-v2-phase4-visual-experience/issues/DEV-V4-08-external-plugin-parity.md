# DEV-V4-08：外部配置同等升级

Type: task
Status: resolved（2026-09-12 红测先行（Plugin 编译红 CS1061 CaptureConfigEntries/bool.Accepted → ClientUi 运行时红三类文案缺失）→GREEN；双轴审查三轮闭环：R1 Spec 1 blocking（WritableCandidate 按抽象 Kind，byte/uint 越界误授 Cycle）→按真实 CLR 类型校验+红测组；R2 Spec 2 blocking（bool 档位 Ordinal 匹配回落首档；ulong 超 long 域降级 Unsupported）→NextPluginCycleIndex 按解析值匹配+PluginConfigValue 无符号载体全链贯通；R3 双轴 CLEAN（1 具名 deferrable：Integer 分支跨程序集同构）；红测红利=抓出 02 票遗留（非字符串行 Cycle 恒回落首档）；构建 0/0+全套 7 绿；BepInEx 5.4.23 IsReadOnly 恒 false 桩已注记；本票按纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-08/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-01, DEV-V4-02
Spec: `../spec.md`（「外部配置同等升级（V4-T7 → DEV-V4-08）」节）

## What to build

玩家在外部 BepInEx 插件详情里能看到配置描述，并能对声明了档位的项用循环切换改值；改动进同一套草稿，保存失败时看到短中文原因。检测到 UPM 仍可改其受支持配置，底栏说明 BUE 不改它的加载状态。外部插件没有功能级长描述，也没有进程级启停。

## Scope

- 不画插件级长描述。配置行描述来自 ConfigDescription，空不画，截断 120。模型须采集 DisplayName 与 Description。
- 可编辑基础类型：bool、数字、字符串。Cycle 仅当值类型受支持、约束为 Unturned.Cycle 或 AcceptableValueList、候选非空且可写回。未识别离散约束不变成选择器；底层仍属基础类型则普通控件进草稿。
- RequiresRestart：行级=固有属性；顶部徽章仅针对本次保存成功写入的项。
- 检测到 `com.trae.pluginmanager`：仍列出并可编受支持 cfg；底栏「不修改其状态」=加载与运行状态。
- 失败三类：插件已卸载 / 配置文件无法写入 / 值不合法。结构化 adapter 结果，不匹配异常文本、不画堆栈。
- 不做：热卸载、进程级启停、ItemList/BlueprintList/CreatureList、Category、路径调试信息、自定义元数据契约。

## 验收条件

- [ ] 红测先行：描述采集与空不画；Cycle 可写回才画 Cycle；不可写回或非基础类型只读；失败三类短中文；UPM GUID 仍可编 cfg；写入走 01 草稿而非立刻 Save
- [ ] 外部条目无功能级启停开关（与 05 一致）；全套测试 0 警告 0 错误
- [ ] 不做选择器/路径的缺席红测或明确断言未识别标签静默忽略
- [ ] 双轴独立审查 CLEAN
- [ ] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments
