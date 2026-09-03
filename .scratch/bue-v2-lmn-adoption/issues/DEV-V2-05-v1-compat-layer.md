# DEV-V2-05：V1 兼容层

Type: task
Status: ready-for-agent
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

- [ ] 红测先行：`--bue-v1-compat-red` 断言 V1 帧识别与路由——先红后绿。
- [ ] V1 no-op fixture（用 LMN V1 API 编译的测试插件）不改代码收发成功（主机测试或实机）。
- [ ] V1 兼容关闭时帧交还 vanilla；故障时只隔离 V1。
- [ ] 构建 0/0；七项目测试 PASS。

## 不做

- 不提供新的 V1 注册入口（只兼容已存在消费方）；不修改旧插件。
