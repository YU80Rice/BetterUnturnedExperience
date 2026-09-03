# LMN 配置迁移映射

Type: wayfinder:research
Status: resolved（2026-09-03 查证 + grilling 拍板：空迁移 no-op）
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T5-lmn-coexistence-takeover（已 resolved；接管机制定了才能谈迁移时序）

## Question

BUE 接管独立 LMN 时，哪些 LMN 配置键迁移到 BUE 设置？如何保留有效值、可回滚、可诊断？

## 查证要点

1. 独立 LMN 的配置文件位置、格式、键集合（本地 `LaunchMultiplayerNet` 源码/config 静态确认）。
2. 迁移映射：哪些键与 BUE 设置模型（Settings Facet / 原子 revision）对应，哪些不可迁移（保留原文件）。
3. 回滚语义：迁移失败或用户回退时如何恢复原配置，不破坏旧配置文件（CONTEXT.md L77-79）。
4. 面板呈现：迁移状态/成功/失败在 BUE 管理面板的展示方式。
5. 证据：配置文件样例 + 源码引用 + 路径。

## 答案

### 查证结论（research 报告 `research/V2-T6-lmn-config-migration.md`）

- **LMN V5 无配置系统**：全仓库 `.cs` grep `Config|ConfigEntry|BepInEx.Configuration|.cfg|File.*|.json` 零命中；`LaunchMultiplayerNetPlugin.Awake()`（L50-74）零 Config 引用；发布包只含单 DLL（README L36-42 / RELEASE.md L16）。无配置文件位置/格式/键集合。
- **无运行时持久化**：会话/频道状态全为内存 static 表（ConnectionSessionManager/Session/NamespacedTransport/ModTransport），随会话销毁不落盘。
- **候选映射表 = 空**：可迁的只有协议/实现常数（MOD/LMN2 魔数、GUID、缓存上限），非用户配置，属 T3/T4 BUE 内固化范畴。
- **BUE 侧承接**：Settings Facet（`SettingsRuntime` + `ScopeState{Revision,Values,Replay}`）+ `FileSettingsPersistence` 原子提交（tmp+读回校验+`File.Replace`）天然回滚-safe；`ContractTypes.cs` L181 已预留 `SettingMigrationFailed=1309`。
- **空迁移语义**：无旧配置文件可破坏 → "不破坏旧配置"自动满足；"可回滚并记录结果"降级为记录一次空迁移。

### grilling 拍板（2026-09-03 人工）

- **Q1 空迁移语义 = A**：接受 no-op，不为无源配置预建 Settings Facet（网络模块开关属 T7）。
- **Q2 结果记录 = A**：结构化日志（`BueRuntimeLog` diagnosticId）+ 面板一行状态（与 T5「已由 BUE 接管」同款）；不持久化"已做过空迁移"标记（幂等空跑无害）。
- **Q3 面板呈现 = A**：显示一行说明"无独立配置可迁移（LMN 无配置文件）"，透明避免玩家误以为有配置丢失。
- **Q4 未来键预留 = A**：YAGNI——不预留迁移适配器接口，等真出现再设计（正文以 DEV 设计输入记录）。
- **Q5 协议常数 = A**：MOD/LMN2 魔数、GUID、缓存上限不作为可配置项暴露，由 T3/T4 在 BUE 帧格式内固化。

### 影响

- T6 resolved → **V2 第一阶段地图 7/7 全部解决**。
- 网络模块 Settings Facet 仍可存在（仅"核心/可选"开关，属 T7 决策落地），但不承载任何 LMN 配置迁移条目。
- 若 LMN 未来版本引入配置，按研究报告"未来键的预留"节重新评估（当前不实施）。
