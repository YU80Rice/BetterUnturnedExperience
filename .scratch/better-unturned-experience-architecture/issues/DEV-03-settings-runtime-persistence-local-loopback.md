# DEV-03：SettingsRuntime + 原子持久化 + LocalLoopback

**Owner:** GPT  
**Required reviewer:** Gemini  
**Status:** ready-for-agent  
**Baseline:** `BUE-V1-RT01-20260824`  
**SourceSet:** `BUE-SS-20260824-02`  
**Depends on:** DEV-01 (`resolved`), DEV-02 (`resolved`), RT-06 (`resolved`)  
**Implementation mode:** TDD（Red → Green，逐 seam 垂直切片）

## Scope

实现后端 SettingsRuntime 的最小生产骨架：静态 SettingDescriptor 注册与校验、完整不可变 FeatureSettingsSnapshot、同功能同作用域原子多字段提交、单调 revision、RequestId 幂等/冲突、版本化本地持久化与损坏隔离，以及无需 LMN 的进程内 LocalLoopback 提交路径。

SettingsRuntime 不读取或生成 Glazier/Sleek/Unity 类型，不写生命周期状态，不持久化库存状态，不实现 LMN adapter、网络 codec、ClientUi 或 DEV-04 候选算法。

## Acceptance

- [ ] 仅接受已登记且属于同一 FeatureId 的 descriptor；重复 SettingId、类型/范围/Choice 语义非法时原子拒绝。
- [ ] 初始快照从持久化值或安全默认值构造，包含完整 Entries、权限、来源、Revision；快照和输入集合不可被外部后续修改。
- [ ] 多字段提交全成功或全失败；ExpectedRevision 冲突、未知键、类型不匹配、校验失败和不允许的 authority/scope 均返回完整当前快照且不增加 revision。
- [ ] 成功事务一次性增加对应 scope revision；无实际变化不增加 revision；ClientPreference 与 ServerAuthority revision 独立。
- [ ] RequestId 在作用域内幂等；同 payload 重放原结果，不重复写盘/增 revision；同 id 不同 payload 返回 `RequestIdConflict`。
- [ ] 持久化采用版本化文档、临时文件、重读校验与同卷替换；写入失败保留上一份有效值，损坏/未来 schema 隔离并回退安全默认值。
- [ ] `ServerPolicyWithClientPreference` 保留客户端偏好；政策仅为 session overlay，不覆盖客户端持久化文件；连接代际变更清理 overlay/pending replay。
- [ ] LocalLoopback 仅进程内转发 Settings command/result，不依赖 LMN，不伪造网络身份或授权。
- [ ] Contracts/Core 不引用 Unity、Glazier、Sleek、LMN、BepInEx、Harmony 或 Unturned native 类型；主线程事务边界由可注入执行器/调用方保证。
- [ ] TDD 覆盖三种 authority、原子事务、revision、RequestId、持久化故障/损坏/迁移、policy overlay 和 generation 清理。
- [ ] Release rebuild 目标 `0 errors / 0 warnings`；独立子智能体审计 PASS；Gemini 消费复核 ACCEPT 后才可关闭。

## Non-goals

不实现 DEV-04 PlacementCandidateEngine、DEV-05 ClientUi/Glazier、DEV-06 LMN transport、DEV-07 三环境发布验收；不修改 LMN，不宣称任何运行环境或发布授权通过。

## Agreed seams under test

1. `SettingsRuntime`：`IScopedFeatureSettings.GetSnapshot/TryGet/Submit`。
2. `ISettingsPersistence`（Core 内部持久化 seam）：加载、原子提交、损坏隔离结果。
3. `LocalLoopbackSettingsTransport`：进程内 command → result 投影，不经网络。

## Retest evidence
- 2026-08-24 复测报告：udit/2026-08-24/Verification-DEV-03-Retest-2308.md。
- Release rebuild、Contracts/DEV-03 tests、Contracts/Core token scan 均 PASS；无代码回归。
- DEV-03 仍等待独立审计 Round 3 与 Gemini 消费复核。
