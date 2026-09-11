# DEV-V4-01：未保存草稿与配置保存模型

Type: task
Status: resolved（2026-09-11 红测先行→GREEN；双轴 Round 3 全 CLEAN；构建 0/0 + 全套 7 绿；本票按 01 纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-11/DEV-V4-01/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: 无（先行票，可立即开始）
Spec: `../spec.md`（「未保存草稿与配置保存（V4-T2 → DEV-V4-01）」节 + 共享规则）

## What to build

玩家在管理面板详情里改 BUE 设置、功能级启停意图或外部可编辑配置时，改动先进入未保存草稿，点「保存配置」才交给权威源。脏了关面板、换条目或刷新列表会问「修改尚未保存，要保存吗？」；拨回去则不再脏。收藏仍即时。ServerAuthority、只读行和状态投影不进草稿。

## Scope

- 草稿范围：ClientPreference、启停意图、外部允许编辑的配置（含 RequiresRestart 的值）。不进：只读、状态投影、接管提示、收藏、ServerAuthority。
- 逻辑面板会话：草稿只活在当前条目内存；Glazier 重挂不丢草稿、不弹确认。
- 确认三选一：保存 / 不保存 / 取消。保存失败不导航。
- 脏 = 最终值 ≠ 权威快照。一脏即标「未保存」。不脏点保存 = 空操作，文案「没有需要保存的修改。」全成功：「配置已保存。」
- 写入顺序：BUE 设置一次原子 Submit（基准 revision）→ 外部 ConfigEntry 逐条 → 最后启停。启停不是事务尾。无后台自动保存。
- 本票交付模型与命令缝（保存/放弃/确认），原生 Glazier 可先接到模型命令；Cycle 形状、启停开关外观、外部 Cycle 识别分别归 02/05/08。
- 不做：扩 `IFeatureRegistration`；后台自动保存；草稿改 ServerAuthority。

## 验收条件

- [x] 红测先行：改设置不立刻写权威源；脏判定按最终值；确认三选一后果；跨源部分成功（设置失败仍尝试后续源）；单次 SettingsRuntime 提交原子 + ExpectedRevision 过期文案「未保存：设置已在别处变更。」；不脏空操作文案；无自动保存。先红后绿
- [x] 既有面板立刻写入路径退役（不再是唯一提交路径）；全套测试工程 0 警告 0 错误
- [x] 官方先行消费锚：面板自身能走草稿→保存（至少一条官方 ClientPreference）
- [x] 双轴独立审查（standards-reviewer / Spec-Reviewer，每轮全新实例）CLEAN
- [x] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId

## Comments

- 2026-09-11 关单（/implement 独立会话）：交付未保存草稿模型 + 命令缝（保存 / 放弃 / 确认），原生 Glazier 接到模型命令、行编辑退役为草稿。审计链：R1 Spec F1–F7 → R2 Spec N1/N2 → R3 双轴 CLEAN。契约仍 2.1（仅新增 internal `IBueSettingsEditor.ApplyBatch`）。启停开关外观/Cycle/外部 Cycle 分别留 DEV-V4-05/02/08。解锁 DEV-V4-02/03/08。
