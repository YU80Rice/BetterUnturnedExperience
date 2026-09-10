# DEV-V3-06：BueSettings 接线与面板动态路由（官方生态共用+双 scope）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-03（BueLifecycle）
Spec: `../spec.md`（「设置（V3-T7 → DEV-V3-06）」节）

## What to build

生态作者的设置与官方功能同纪律：经注入的 `bootstrap.Settings` 读快照、提交变更并观察 revision 推进与校验拒绝；自己的设置在管理面板与官方设置同样可见可编辑（面板按注册目录动态路由）；ClientPreference/ServerAuthority 双 scope 下 U3DS 与 P2P 主机权威端同语义，会话覆盖断线清除不污染持久化 revision。

## Scope

- `bootstrap.Settings` 接线（可用性矩阵行，红线钉「接线前 null+接线后可用」两侧）：view 限当前功能作用域（GetSnapshot/TryGet/Submit）。
- 官方与生态共用 SettingsRuntime 规则：校验/revision 单调/损坏安全默认/原子提交/作用域隔离；类本体不列契约；生态五不得（不另造配置格式、不绕过作用域、不直写持久化、不假设他人作用域可读、不做跨机同步）。
- 面板按注册目录动态路由（官方硬编码清单退役）；面板=编辑 adapter 非第二事实源；未提供设置的功能不伪造设置页。
- ClientPreference/ServerAuthority 双 scope：权威端两环境（U3DS 与 P2P 主机）同语义；客户端会话覆盖断线清除、永不污染持久化 revision；不做跨机同步协议。
- schemaVersion 通道保留，迁移由功能自理；`ExpectedRevision` 防旧 UI 覆盖新值。
- 不做：SettingsRuntime 与 BueNetwork 职责混合；跨机同步；平台统一迁移框架。

## 验收条件

- [ ] 红测先行：InMemorySettingsPersistence 组（快照/提交/校验失败/ExpectedRevision 过期/损坏安全默认/作用域隔离/断线覆盖清除），各先红后绿（先例=Settings 测试工程既有组）
- [ ] 矩阵接线两侧红测：Settings 接线前 null+接线后可用；面板动态路由（官方+生态条目并列、未提供设置者无页）
- [ ] 官方先行消费锚：官方功能走真注入 Settings view（非内部控制面）断言
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）
