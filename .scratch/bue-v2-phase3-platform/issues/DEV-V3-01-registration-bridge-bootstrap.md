# DEV-V3-01：注册桥与 Bootstrap 基线（官方身份白名单+十成员可用性矩阵+内部登记记录）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线，两轮澄清修订后版本）
Blocked by: 无（先行票，可立即开始）
Spec: `../spec.md`（「注册桥与 Bootstrap（V3-T2 → DEV-V3-01）」节+Implementation Decisions 共享规则）

## What to build

生态功能作者经 `BueRuntimeHost.Register` 注册时，冒用官方 FeatureId 保留段会被确定性拒绝并拿到明确原因码（`ReservedFeatureId`/`BUE-REG-010`），全部拒绝码在 SDK 文档逐码可查；功能启动拿到的 `IFeatureBootstrap` 十成员可用性符合矩阵基线——五成员永非 null，五个待接线成员为 null 且文档明示。宿主内部从本票起持有 owner-scoped 登记记录，为后续生命周期票提供状态/代际/资源所有权的事实载体。

## Scope

- 官方身份白名单：保留段 FeatureId 须 ∈ 白名单（BII/LIT/LIR/LHT/BUE Network/ClientUi satellite 等）；判定顺序=基础校验→格式校验→保留段→合同版本→重复→结果；不反射 caller、不读路径；白名单不授予官方资格以外的任何特权。
- Admission 三类型（Result/Phase/Reason）与 `BUE-REG-001..010` 码表整体入冻结面；拒绝=显式结果不抛异常。
- Bootstrap 十成员可用性矩阵（spec 显式表为唯一口径）：Identity/LifecycleGeneration/Events/OwnedEvents/Network 永非 null（红测钉住）；Lifetime/Dependencies/MainThread/Settings/Logger 基线为 null（红测钉住基线侧；接线侧归 03/04/06/07 各票红线）。
- 宿主内部 owner-scoped registration record：FeatureId/注册来源/当前状态/LifecycleGeneration/资源所有权/停止与隔离结果；不公开 registration session，不退化为 FeatureId 全局查找+散装静态表。
- `SupportedContractMajor=2`/`Minor=1`，2.0 模块继续可注册（兼容红测）。
- 不做：自建 scanner/loader；扩 Major 契约；改动 BepInEx 发现职责。

## 验收条件

- [ ] 红测先行：逐码锚 BUE-REG-001..010（含保留段拒绝 BUE-REG-010 正反例）、白名单判定顺序、矩阵基线两侧（五成员非 null+五成员 null）、2.0 模块注册兼容，各红测先红后绿
- [ ] 注册测试组全绿（显式结果四元组断言，先例=注册运行时既有宿主测试）；全套测试工程 0 警告 0 错误
- [ ] 官方先行消费锚：官方身份白名单正例（BII/LIT/LIR/LHT 至少其一真实注册通过）
- [ ] 双轴独立审查（standards-reviewer / Spec-Reviewer，每轮全新实例）CLEAN
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线，非对外 SDK 版本、非生态可引用发布物）
