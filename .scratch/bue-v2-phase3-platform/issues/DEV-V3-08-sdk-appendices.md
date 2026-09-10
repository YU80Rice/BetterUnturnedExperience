# DEV-V3-08：SDK 契约文档附录总装（附录 A/B/C+自检清单+NoOp 统一 probe 扩链）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01、DEV-V3-02、DEV-V3-03、DEV-V3-04、DEV-V3-05、DEV-V3-06、DEV-V3-07（全部平台缝实施票）
Spec: `../spec.md`（「SDK 契约文档（V3-T9 → DEV-V3-08）」节）

## What to build

生态作者只凭一份 SDK 契约文档+一个活样板就能接入全部平台服务：附录 A（平台服务参考七节）、附录 B（诊断与身份码表）、附录 C（契约版本与迁移）成文；「生态 DLL 上架前自检清单」（11 项人工核对）可逐项打勾；NoOpFixture 升格为统一生态契约 probe，注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离全链可运行、失败分 seam 可定位。

## Scope

- 正文八节冻结不动；新增附录 A（Admission/Events/Lifecycle/Network/HostTick/Settings/Diagnostics 七节）、附录 B（BUE-REG-001..010、BUE-PLATFORM-001/002、前缀纪律、FeatureId 保留段及合法/非法示例）、附录 C（2.0→2.1 条目、安全降级原则、Major 纪律、四条件门禁、RELEASES 注记要求）。
- T1..T8 移交总账逐条映射到附录 A/B/C 具体章节；结票前逐条核对无遗漏。
- Contracts 拆分四条件全部未触发→继续暂缓，逐条登记为门禁条款（定义/事实判定/触发信号/重评义务；先触发预判=①编译脱耦、③发布节奏分化）。
- 上架前自检清单 11 项（引用方式、身份合规、降级义务等人工核对项）。
- NoOpFixture 统一 probe 扩链（覆盖各票扩链清单+可用性矩阵票后终态路径）；probe 失败必须分 seam 可定位（注册/事件/Lifecycle/Network/Settings/Logger 各自独立判据与诊断行），不得全链 PASS/FAIL 遮蔽。
- SDK 文档随主 DLL 契约版本走，不独立发版；文档示例锚定 NoOpFixture 真实代码（双向）。
- 不做：SDK 项目模板；编译期验证工具；第二样板；独立 SDK 程序集。

## 验收条件

- [ ] 附录 A/B/C 成文且与 T1..T8 移交总账逐条对得上（每条可指出落档章节）
- [ ] NoOp probe 全链绿+分 seam 定位断言（各 seam 独立判据红测证明可定位）
- [ ] 文档示例与 NoOpFixture 代码双向锚定（示例可编译、样板在文档有出处）
- [ ] 双轴独立审查（每轮全新实例）CLEAN
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线；v8 交付包 2.0 基线保持原状，换新归 09 票）
