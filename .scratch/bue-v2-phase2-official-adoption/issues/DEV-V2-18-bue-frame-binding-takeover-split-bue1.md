# DEV-V2-18：平台——BUE 帧生产消费绑定 + LMN 接管 seam 拆分 + 魔数 BUE1（Q3）

Type: task
Status: ready-for-agent
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

- [ ] 红测先行（决策核直驱、不装 Harmony 补丁，沿用「生产 prefix 是决策核唯一调用者」纪律）：四类帧分类 / 六步顺序 / 网络关闭不消费且 patch 移除 / live 每帧恰一次 / 探针 false 零 LMN 动作 / 异常 hand-back 不吞流量——先红后绿
- [ ] 生产传输绑定端到端：假 transport 下双向收发全链绿（订阅 → 帧上线 → 派发）
- [ ] 魔数清扫零残留（token 扫描）；帧编解码回归通过
- [ ] V1 兼容层与既有接管测试零回归
- [ ] 构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN
