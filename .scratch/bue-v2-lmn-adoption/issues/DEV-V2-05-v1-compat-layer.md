# DEV-V2-05：V1 兼容层

Type: task
Status: resolved（2026-09-03 本会话交付；05 范围 = Host 内部兼容层 + 帧识别路由 + 测试；LmnTakeoverCoordinator 实例化/面板开关接线归 DEV-V2-06；双轴 R1 CLEAN）
Parent: spec-V2-phase1-lmn-adoption（V2 第一阶段）
Blocked by: DEV-V2-04-lmn-takeover（先接管再落兼容层）
Spec: `../spec-V2-phase1-lmn-adoption.md`（Implementation Decisions「V1 兼容路径」）

## Scope

实现 V1 数字频道兼容路径（T4 决策），使旧插件无改动运行：

- 帧识别在 BUE API 接收面：识别 LMN V1 帧（`MOD` 魔数 + int 频道号，LMN `ModRouter.cs:13-16,40-51`）并路由到兼容层。
- 兼容注册表：模拟 LMN V1 旧注册语义（`RegisterServerHandler(int,...)` 等），旧插件二进制不动的注册调用落到兼容层。
- "是否启用 V1 兼容"作为官方功能（面板可关，与网络模块开关独立）。
- 故障行为：V1 兼容层故障只丢弃 V1 帧 + 诊断，不拖累 V2/本地功能。
- V1 帧结构在 Host 内部代码/注释固化（不进 Contracts，来源引用 LMN 源码行号）。
- 验收 fixture：干净环境装 BUE + 旧 V1 no-op 插件（LMN V1 API 编译），插件不改代码能收发。

## 验收条件

- [x] 红测先行：`--bue-v1-compat-red`（`AssertBueV1Compat`）断言 V1 帧识别与路由——先红后绿。（红=red-v1-compat-r1.log CS0234 exit=1；绿=green-v1-compat-r2.log 重建 + green-v1-compat-anchor-r3.log exit=0；已log：`audit/2026-09-03/DEV-V2-05/`）
- [x] V1 no-op fixture（用 LMN V1 API 编译的测试插件）不改代码收发成功（主机测试或实机）。（主机测试=`LmnV1NoOpPluginFixture` 频道 103：出站字面 MOD 帧、入站 sender+payload 原样；拍板 2026-09-03：验收入口不依赖真实旧 DLL；真机旧 V1 DLL 验证=可选项**待补**，不阻塞闭环）
- [x] V1 兼容关闭时帧交还 vanilla；故障时只隔离 V1。（断言：Enabled=false→帧不消费+出站拒绝；handler 抛异常→帧丢弃+诊断 BUE-V1COMPAT-001+层保持健康；LMN2/非 LMN 帧永不消费）
- [x] 构建 0/0；七项目测试 PASS。（build-sln-release-r1.log exit 0 零 warn/err 行；7 运行器 exit=0；token 扫描 Core=16/ClientUi=11 PASS；git diff --check 0；汇总=gates-summary-r1.log）

## 不做

- 不提供新的 V1 注册入口（只兼容已存在消费方）；不修改旧插件。

## 拍板口径（2026-09-03 会话，human 定稿；写入票以落盘）

- **验收入口不依赖真实旧 DLL**：V1 no-op fixture（用 LMN V1 数字频道 API 编译的测试插件）模拟旧插件，不改代码走兼容层收发即为验收通过；「真机旧 V1 插件验证」为**可选项**，若日后能找到真实旧 V1 DLL（如 LIT/LIR/LHT 旧版本）作为真机补充证据，标注在交付报告的证据链上，不阻塞本票闭环。
- **实施顺序**：本票（DEV-V2-05）先于 DEV-V2-06（接线+配置迁移）。05 与 04/06 无顺序依赖，是独立并行线；04 已完成，05 前置 `Blocked by: DEV-V2-04` 已解除。
- **接线 seam 归属**：04 拍板交割到 06 — LmnTakeoverCoordinator 的实例化/面板开关接线不在本票，本票只做 Host 内部兼容层 + 帧识别路由 + 测试。
