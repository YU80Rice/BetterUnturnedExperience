# DEV-V2-22：LIR 纳入——更好的换弹体验（ReloadContextGuard + 宿主时钟 + 事件消费）

Type: task
Status: resolved（2026-09-08，/implement 会话；双轴最终 CLEAN——Standards=R3（3 SMELL 具名可延期）、Spec=R3（REBUTTAL-GAP1 维持接受，零 GAP/DEVIATION/SMELL）；候选 a04b52eb…5d65（489984B 三轮 Rebuild 字节一致，CaseId DEV-V2-22-CANDIDATE-20260908）；结单报告 audit/2026-09-08/DEV-V2-22/）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: DEV-V2-18（BUE 帧绑定）、DEV-V2-19（HostTick + TidyCompleted 契约件）
Spec: `../spec.md`（「LIR：补丁、守卫与常量」「LIT ↔ LIR：TidyCompleted 功能事件」两节）

## What to build

裸 BUE 的原位换弹：双击换弹键触发原位压弹，行为与旧独立插件一致；用更好的物品交互拖入物品绝不误触发换弹；背包整理完成后无条件自动压弹（不加新开关）；面板中文名「更好的换弹体验」，关闭 = 原生回退且只撤销自己的补丁。

## Scope

- 身份：FeatureId `io.github.yu80rice.bue.in-place-reload`（频道身份同串，旧频道退役）；显示名「更好的换弹体验」。
- 三枚补丁保留为模块内部实现（UseableGun 接弹匣 Prefix/Postfix、forceAddItem Prefix）；结构：LIR 生命周期 → ReloadContextGuard → 三个 Harmony adapter → 换弹行为；Harmony ID 收编 FeatureId；`Stop` 只撤销自身 Harmony ID。
- `TidyCompleted` 消费：TidyCompletedConsumer → ReloadAction（本期 adapter = 整理后自动压弹）；必须验事件结果成功 + 范围符合，保持自身幂等与异常隔离，不见事件就盲执行被禁止；本期不加开关（enabled 总开关已存在）。
- 周期驱动：订阅 HostTick（DEV-V2-19），自负责双击检测、换弹键轮询、换弹状态推进、超时判断；不自建 Unity Update 泵。
- 常量集中 `ReloadRuntimePolicy`（双击窗口 0.3s、队列上限 64、诊断间隔 5s），不散落补丁；主线程 dispatcher 队列保留为内部实现。
- 网络：客机压弹请求 → 服务器；服务器回包按会话定向；注册失败半注册回滚；注册延迟到首帧游戏线程（语义保持，落在新生命周期内）。
- 设置：只持久化 `enabled`（ClientLocal，关 → 原生回退）。
- 扩展三分法：换弹策略走 `IReloadAction` adapter；独立新能力立新 FeatureId 模块；跨功能协作只走公开契约（不恢复反射寻类型 / 跨功能 Harmony postfix）。

## 验收条件

- [x] 红测先行：**叠加点钉死——BII 拖入 → forceAddItem Prefix 触发 → 上下文 = false → LIR 不执行换弹逻辑** / `Stop` 只撤自身 / 事件消费验成功+范围+幂等 / HostTick 驱动的双击与状态推进——先红后绿
- [x] 整理 → 自动压弹联调：与 DEV-V2-21 的事件发布端到端（假 transport 下全链绿）
- [x] 冲突审查复核留档：BUE / BII 现有补丁目标与 LIR 三补丁零交集
- [x] 面板条目：FeatureId 身份 + 中文名；构建 0 警告；全套测试 PASS；双轴独立审查 CLEAN

## Comments

- **2026-09-08 实施会话（/implement）**：红绿链=编译红 15 错（独立锚点 compile-red.log）→桩级红 34 条（stub-red-run.log）→运行时红 7→1→ALL GREEN（实现中途揪出五处：模块角色探针须注入缝（测试进程 Provider.isServer 不可用）、旧双击算法触发后下一按只重锚基线（连击防护,测试初版读错）、dispatcher 每帧 16 上限须循环排空、SettingsRuntime Submit 的 RequestId 重放保护、检查顺序）→修复轮新断言先红（fix1-red-run.log 2 条）→绿。实现期两处设计自纠:dispatcher 由进程静态改每服务代际实例（双模块实例互排队列陷阱）、测试组 2 弃双模块同启（静态 ActiveModule 单槽）改钉句柄清空。
- **R1 双轴 NOT CLEAN**：Standards 2 BLOCKING（未用 using/服务树 public 泄漏公开面）+4 SMELL+1 INFO；Spec GAP-1（停止注销未落实）+DEVIATION-1（非法页范围 6..2 未拒）。F1 全修,DEVIATION-1 带红→绿锚点;GAP-1 以冻结交接缝反驳（DEV-V2-19 F3:宿主 Stop 返回后 UnsubscribeAll,BueFeatureStartRuntime:105-110 已落地;模块自存 IDisposable=双重所有权）。
- **R2**：Standards **CLEAN**（反驳裁定接受;残留 SMELL×3+INFO×1）;**Spec R2 派发失败作废**（基础设施失败:模型零输出零工具调用,无判词,fresh-instance 规则不得续用,如实留痕后由 R3 全新实例补轮）。F2 修 SMELL/INFO:面板路由闸补 lirModule（潜在缺陷:仅注入 LIR 时开关不打到 RefreshSwitches）、dispatcher 计数一律 Interlocked、guard 直存 ReloadSlotContext、请求路径 Sessions catch 补节流诊断。
- **R3 双轴双 CLEAN（终审,全新实例）**：Standards CLEAN（3 SMELL 具名可延期:action Feature Envy 薄缝/事务骨架重复系旧源保真/闸·日志缝·请求序列进程静态系 LIT 同构）;Spec CLEAN（REBUTTAL-GAP1 维持接受,票面 Scope+验收逐条零 GAP 零 DEVIATION,行为保持对照旧插件通过）。
- **闭单**：全套 7/7 PASS 0 警告（fix2-build.log+sln-rebuild-r2.log）;候选 `a04b52eb…5d65`（489984B 三轮 Rebuild 字节一致,CaseId DEV-V2-22-CANDIDATE-20260908,RELEASES 换标随 DEV-V2-24）;冲突复核零交集（全仓唯一非 LIR Harmony 目标=LIT 面板按钮 PlayerDashboardInventoryUI）;具名延期五项见结单报告 §4;判词与红绿证据存档 audit/2026-09-08/DEV-V2-22/（review-rounds.md、red-evidence.md）。
