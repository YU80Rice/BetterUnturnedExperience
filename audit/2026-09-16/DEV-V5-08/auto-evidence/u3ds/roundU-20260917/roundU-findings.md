# U3DS 轮（v3 589300e2 + 探针 #4 仅客机，2026-09-17）

双端 identity：U3DS=`589300E2`；客机探针轮=`51C2DC94`（非候选，随后已恢复 v3）。

## 成立

- U1 headless 负面：decision=Headless，不武装 HUD/分区/整理钮，LIT/LIR to=Running，Error=0。
- 入包恢复权威：服务端 `[入包恢复] …提交成功`。
- 压弹权威：多次 `RepackSuccess`（11/21/6/9/…）。
- 容器按钮投影：U2 客机 `[TidyUI] 容器整理按钮已画出（能力投影=可用）`，用户目视「现在有整理了」。U1 的 `IsVisible` TargetInvocationException 本轮未复现（探针 #4 Inner 未打出——按钮已可见，失败路径未走）。
- 2 级自动轮**排队**多次成立。

## 自动轮未看到 toast 的日志解释

U2 最后一次 `已排队等待 8s` 之后，客机在到点前 `PluginStopping`，服务端 `peer scope 已关闭`——8 秒窗被会话结束截断，不会出现 `reqId=0 auto=1`。此前多次排队后 8 秒内又有新的手动 `RepackSuccess`（规格：新手动成功替换未到点条目）。P2P v3 已有一次完整自动轮：`RepackSuccess(reqId=0, total=12, auto=1)` + 客机 parseErrors=0。

## 定性（待用户口头确认）

- 容器整理按钮：用户已确认「现在有整理了」→ G03-2 U3DS 客机投影销账。U1 显隐异常视为偶发/已自愈，具名观察（不升格阻断，除非用户说按钮仍不稳定）。
- 自动轮：权威排队成立；完整 8s 成交回包在 U3DS 本轮未采到（会话提前结束）。P2P 已采到。若用户接受「P2P 实证 + U3DS 排队锚」为跨环境充分，则 U3DS 自动轮不单开缺陷。
