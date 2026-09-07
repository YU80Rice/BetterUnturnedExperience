# DEV-V2-18：平台——BUE 帧生产消费绑定 + LMN 接管 seam 拆分 + 魔数 BUE1（Q3）

Type: task
Status: resolved（2026-09-07，双轴最终 CLEAN：Standards=R3、Spec=R3；候选 0f2336e8…c7b）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-17（握手与会话生命周期须先收进运行时）
Spec: `../spec.md`（「平台：BUE 帧消费 seam 与 LMN 接管 seam 拆分」「平台：线帧魔数与频道政策」两节）

## What to build

裸 BUE 的 BUE 帧真正上线：客户端→服务器与服务器→客户端双向的生产传输绑定闭环（三插件作为第一批真实消费者的地基）；BUE 帧消费与 LMN 接管拆成两个独立 seam；线帧魔数改名 BUE1，产品语言一律「BUE 帧」。至此目的地验收第③条「生产传输绑定」达成。

## Scope

- 决策核扩展：BUE 帧识别分支 + 六步决策顺序冻结（① 识别 BUE 帧 → ② 网络模块开 → BUE 消费 → ③ 识别 MOD/LMN2 → ④ LMN live → 放行原生 → ⑤ LMN inert → BUE 处理 → ⑥ 非目标帧交还 vanilla）。
- patch 安装门 = 网络模块启用（与 LMN 探针解耦）；探针 false → 零 LMN 相关 patch/反射/镜像；两 seam（BUE 帧消费 / LMN 接管）不得共用 takeover 布尔。
- 契约不变性：网络关闭时不消费 BUE 帧且 patch 被移除；BUE 帧与 MOD/LMN2 不误分类；LMN live 每帧恰一次派发；live/inert 每方向独立判断；异常路径 hand-back 不吞原生流量。
- 双向收发落实：client→server 与 server→client 的生产路径（发送侧按会话寻径，衔接 DEV-V2-16/17 语义）。
- 魔数 BUE2 → BUE1（4 字节帧头布局不变）；全仓「BUE2」字样清扫（常量、注释、测试、文档）；数字频道只兼容不注册重申（三官方功能禁入 V1 层）。

## 验收条件

- [x] 红测先行（决策核直驱、不装 Harmony 补丁，沿用「生产 prefix 是决策核唯一调用者」纪律）：四类帧分类 / 六步顺序 / 网络关闭不消费且 patch 移除 / live 每帧恰一次 / 探针 false 零 LMN 动作 / 异常 hand-back 不吞流量——先红后绿
- [x] 生产传输绑定端到端：假 transport 下双向收发全链绿（订阅 → 帧上线 → 派发）
- [x] 魔数清扫零残留（token 扫描）；帧编解码回归通过
- [x] V1 兼容层与既有接管测试零回归
- [x] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN

## 实施结单（2026-09-07，agent /implement，全链闭环）

**交付**：决策核六步冻结（BUE 分支前置、门=模块开关本身；两 seam 零共享布尔，live/inert 每方向独立）；`BueFrameClassifier` 新建（FrameMagic="BUE1" 唯一源，MagicBytes 派生，offset-aware）；patch 安装门=网络模块启用（探针 false → LMN seam 零 patch/反射/镜像，三个调用面全加门+计数探针钉零）；异常 hand-back 结构化（决策核顶层级 catch→故障行+交还 vanilla，游戏泵永不中断）；生产传输绑定：`BueEngineNetBinding`/`BueEngineNet` 新建（按名静默反射五解析器+Send 全 never-throw，clientTransport/transportConnection 线形沿 LMN 源权威，免 Steamworks 编译引用保持）、`HostNetworkTransportAdapter` 生命周期实装（PeerStateSource+PollPeerState 差分+Raise 缝，pragma 撤除）、`TickNetwork` 四级隔离泵（插件 Update 驱动）、运行时一次成型武装（角色首决/本地身份 fail-closed/注入时钟下传）、开关联动 Disarm/Rearm（14 停用语义+17 重启用语义）；魔数 BUE2→BUE1 全仓清扫零残留（src/tests/docs/CONTEXT.md；audit/.scratch 历史档案不扫，Spec 轴裁定正当）；数字频道无新注册入口。

**红绿链**：红0 编译红（CS0246）→ 桩级红 9 条（五组收集式，`red-runtime-transcript.log`）→ 绿（转绿路上修实现缺陷一处：Arm 时钟注入传错来源；测试缺陷两处：六步组缺会话建立、E2E 组武装顺序）→ R1 后修复轮增补（网络关闭组/计数探针/绿态成功行）→ round3 注释级修复 → 全程 `--bue-v2-frame-binding-red` ALL GREEN 七组 + 全套 7/7 PASS 0 警告。

**审查链（Fresh-instance 全保持）**：R1 双 NOT CLEAN（S:3 BLOCKING+2 SMELL；Spec:DEVIATION 安装路径 LMN 反射+GAP 绿态证据）→ 修复轮 → R2 双 CLEAN（S:3 SMELL 列名；Spec 零发现五验收 PASS）→ round3 三 SMELL 全修 → R3 双 CLEAN 零发现。**双轴最终 CLEAN（Standards=R3、Spec=R3）**；standards-reviewer 类型本轮可用（17 兜底破例未触发；R1 前 1 次派发上游中断留痕）。报告归档 `audit/2026-09-07/DEV-V2-18/`（R1/R2/R3 双轴 + 红绿链 + round1/2/3-increment.diff）。

**候选**：`0f2336e83b8a927ba3f7c75a08fa50e59dd893297262b20db8ef9e382c489c7b`（364544 字节，两轮 Rebuild 逐字节一致）；不继承 14–17 批准，RELEASES 换标随实机验收（24）。

**具名延期六项**：同进程角色翻转（client→host 保持 initiator，实机面随 24）/ FeatureBootstrap 生产交付（21/22 沿 14 边界）/ patch 真装移除的生产证据（24）/ 引擎绑定实机核验清单（24：Provider.client/server/isConnected/clientTransport/transportConnection 五面）/ main-thread-only 模型声明（不声称 off-main send）/ IConnectionSession.Send 桩（沿 16）。

