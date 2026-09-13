# DEV-V4-07：官方文案、设置中文与 NoOp Choice

Type: task
Status: resolved（2026-09-12 红测先行（编译红 CS0103/CS1061=chrome 投影缝 → 运行时红 BII 显示名回退+06 交接断言翻转红）→实现 GREEN；文案全经面板外显缝断言；双轴审查四轮闭环：R1 Spec 1 blocking（KnownFeatureIds 读内部键集）→删内表面改全经 GetFeatureDescription；R2 Spec 1 blocking（直读描述符/注册面绕过行缝）→三直读组删除、文案断言全收敛 GetSettingRows 行投影缝（锚③真实宿主 LIT+NoOp/BII 行级归 ClientUi.Tests）；R3 双轴 CLEAN+2 deferrable→R4 修复后双轴 CLEAN 零递延；构建 0/0+全套 7 绿；NoOp probe-choice 落地（甲/乙默认甲·字节门 16）+RunSettingsStep 按 id 定位解顺序耦合；06 消费锚描述断言移交 07 行投影组；本票按纪律不授候选/RELEASES/CaseId，契约仍 2.1。审计 `audit/2026-09-12/DEV-V4-07/audit-report.md`）
Parent: spec.md（V2 第四阶段规格·可视化体验定界与接线）
Blocked by: DEV-V4-02, DEV-V4-06
Spec: `../spec.md`（「官方文案与 NoOp（V4-T6 → DEV-V4-07）」节）

## What to build

玩家打开官方功能详情能读到一句话说明；LIT 的模式/方向和 BII 的自动旋转有中文显示名和描述。NoOpFixture 有一句话、一个 Toggle 和一个甲/乙循环切换，供生态作者对照。没有对照表的生态条目不画功能级描述。

## Scope

- chrome 对照表七句原文以 spec 表为准（BII/LIT/LIR/LHT/Network/v1compat/NoOp）。管理面板自身不在目录，不写条目。
- LIT：`inventorytidy.mode` / `inventorytidy.direction` 显示名与描述原文以 spec 为准。
- BII `AutoRotate`：显示名「自动旋转」，描述「拖入时自动旋转物品以适配空位。」退役 enabled / BII Enabled 不进 descriptor、不进草稿。
- NoOp：保留 `noop.probe-toggle`；新增 `noop.probe-choice`（甲/乙，默认甲）。二者都不是生命周期代理，不登记 legacy alias。
- 网络接管文案本阶段不重写。LIR/LHT/Network/v1compat 可以没有普通设置行，详情仍有一句话+状态+启停（启停外观归 05）。
- 不做：i18n 资源表；把对照表写进 SDK；用 NoOp 替代 LIT 的 Choice 官方先行消费。

## 验收条件

- [x] 红测先行：对照表七句逐字可定位；LIT/BII 显示名与描述；`noop.probe-choice` 甲/乙 Cycle 改草稿；无对照表的生态条目不画功能级描述
- [x] 退役 enabled 不作为可编辑设置行出现（与 04 对拍）；全套测试 0 警告 0 错误
- [x] 官方先行消费：NoOp 描述+Toggle+Choice（T1 检验点 ③，不替代 LIT）
- [x] 双轴独立审查 CLEAN（四轮闭环，R4 双轴 CLEAN 零递延）
- [x] **候选纪律**：不授候选、不加 RELEASES、不授 CaseId

## Comments

- 2026-09-12：实施结票。三缝分开落地：chrome 对照表（PanelChromeCopy，internal 非契约，七句逐字）、设置文案（LIT Q61/BII Q62/NoOp Q64 全经行投影缝验证）、NoOp 样板（probe-toggle 保留补文案+probe-choice 新增）。评审链驱动测试缝两次收敛（内部键集→外显缝；descriptor 直读→行投影缝），见 `audit/2026-09-12/DEV-V4-07/red-first-evidence.md` 与 audit-report.md。
