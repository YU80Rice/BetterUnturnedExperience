# DEV-V2-19：契约件——TidyCompleted 功能事件 + HostTick 宿主时钟入 Contracts

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-14（契约 Major 升级与登记流程须先建立；可与 DEV-V2-16/17/18 并行）
Spec: `../spec.md`（「LIT ↔ LIR：TidyCompleted 功能事件」「宿主时钟」两节）

## What to build

官方与生态功能同权地消费「整理完成」事件与宿主时钟：跨功能协作走唯一公开缝（功能事件），帧级驱动走宿主统一时钟（只带序号与时间）——不再有官方私有通道，也不再有功能模块自建 Unity Update 泵。

## Scope

- `TidyCompleted` 事件类型入 Contracts 冻结面（规格定稿）：只读 struct，载荷至少含发布者 FeatureId、整理对象范围、完成结果、连接代际（如适用）、事务/操作标识；事件身份串由发布者 FeatureId 派生（具体命名实施期按契约登记流程定稿）。
- `HostTick` 宿主时钟入 Contracts 冻结面（规格定稿）：序号 + 时间增量 + 阶段；由宿主统一产生；载荷不含功能逻辑。
- 发布/订阅经既有事件总线（OwnedPublisher / Subscriber）；两者随 DEV-V2-14 的契约 Major 升级登记（条目④⑤）。
- 宿主时钟六条不变性：统一产生、频率与阶段固定、主线程回调、序号单调、停止自动注销、单订阅者异常不扩散；功能模块禁自建 Update 泵。

## 验收条件

- [ ] 红测先行：事件发布/订阅/载荷断言/发布者异常隔离；假时钟驱动下 HostTick 单调、阶段、停止注销、异常不扩散——先红后绿
- [ ] 生态侧消费者同权演示（非官方注册路径订阅同一事件与时钟）
- [ ] 冻结面变更登记条目④⑤追加
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
