# DEV-V5-05：Ctrl+右键接收侧恢复

Type: task
Status: resolved（2026-09-16 双轴评审链闭合：R1 Standards 0硬/3气味（S1 修，余具名递延）+ Spec CLEAN；R2 Standards 0硬/2具名 + Spec CLEAN。评审链 audit/2026-09-16/DEV-V5-05/review-loop.md）
Parent: spec.md（V2 第五阶段规格·现有官方功能优化定界与接线）
Blocked by: DEV-V5-01, DEV-V5-02, DEV-V5-03
Spec: `../spec.md`（「入包恢复与快速转移恢复」快速转移分支）

## What to build

玩家用 Ctrl+右键在背包和已打开容器之间挪东西，若目标因为碎洞失败（含客户端找不到空位所以没发包），主机会只整理接收的那一侧再试这一件。玩家→箱子就整理箱子；箱子→玩家就整理原版选定的那一页。失败则两边都不动。成功不自动压弹。

## Scope

- 快速转移恢复 adapter。原版玩家↔容器是 `sendDragItem`，不经 `tryAddItem`，不能靠 04 的入包补丁看见。客户端碎洞未发包时必须向主机请求恢复。
- 只整理接收侧。容器范围与 03 一致（世界箱 / 已授权后备箱）。地面摊 Ctrl+右键进容器是拾取，归 04。
- 消费 02 计划与 03 会话/版本。跟随背包整理生命周期。禁止 Prefix 内死开关。不挂拖放预览。
- 成功不发布整理完成。禁止转移失败但目标已被单独整理。
- 入包恢复归 04，本票不扩拾取。

## 验收条件

- [x] 红测先行：不经 tryAddItem 的路径仍能请求恢复；只改接收侧；失败源和目标都不变；功能停不执行。先红后绿（编译红 red-compile-errors.txt → 六组 ALL GREEN；突变 M1–M7 各证红并还原复绿）
- [x] 官方先行消费：真实 Ctrl+右键碎洞经 02 计划 + 03 会话（组4：策略出口计数+接收侧计划逐格对照 tagged-row-band-v1(+pending)+分类器缝；源侧 PreparePageLeave 恒等直测；03 verifier/指纹单源复用=组3 决策矩阵；意图对 vanilla 逐字守卫经双轴一手核对 U3-SDK 确认，真机探针形状具名缺口随 08）
- [x] 双轴独立审查 CLEAN（评审链 audit/2026-09-16/DEV-V5-05/review-loop.md：R1 Standards 0硬/3气味→S1 死常量修复+具名补记→R2 Standards 0硬/2具名 + Spec 两轮 CLEAN）
- [x] **候选纪律**：不授候选 / RELEASES / CaseId（本票无候选痕迹；08 唯一对外）

## 设计（2026-09-16 认领定案·供双轴评审·已闭环）

事实基线（U3-SDK 一手逐字，`PlayerDashboardInventoryUI.cs`/`PlayerInventory.cs`/`Items.cs`）：
Ctrl+右键 = `onSelectedItem(page,x,y)`（PDIUI.cs:849，右键统一入口）。前置短路：`page==255` 或
(page,x,y)==(selectedPage,selected_x,selected_y) → `closeSelection()`（已选中再 Ctrl+右键 = 关选框，
**不是**转移尝试——意图探针必须复现这条守卫）。否则 `InputEx.GetKey(ControlsSettings.other)` 且
`inventory.isStoring` 时分三支：AREA(8) → `ItemManager.takeItem(→STORAGE)`＝拾取 RPC，归 04（票面
已裁「地面摊归入包路径」）；STORAGE(7) → 本地 `tryFindSpace(sx,sy,out newPage,…)`（PI.cs:682：页
SLOTS..PAGES-2 **升序 2..7** 找第一块矩形空位）→ 成功才 `sendDragItem(7,x,y,newPage,nx,ny,nrot)`；
其余页(0..6) → `tryFindSpace(STORAGE,sx,sy,…)` → 成功才 `sendDragItem(page,x,y,7,nx,ny,nrot)`。
**找不到空位 = 什么都不发**（无失败包、无回执，纯静默）——这就是本票要加的缝。`sendDragItem`
(PI.cs:967) = 纯网络 `SendDragItem.Invoke(Unreliable,…)`；主机 `ReceiveDragItem`(PI.cs:701，
ONLY_FROM_OWNER，ratelimitHz=10) = `checkSpaceDrag`+`removeItem`+`addItem`，不经 `tryAddItem`。
`Items.removeItem(index)` 只摘 jar 不销毁 `Item` 实例 → 同事务「先摘源后放入」与原版
ReceiveDragItem 的 remove-then-add 同款引用保持。

- **客户端触发面 = 两处补丁 + 意图 scope**（`FastTransferRecoverPatches`/`FastTransferIntentScope`，
  随 LIT 生命周期 Start 登记、Stop/隔离注销 = 唯一开关，无任何功能 bool；U3DS 同样登记——真机
  U3DS 永不构造仪表盘 UI，onSelectedItem 永不执行，补丁零触发；权威执行面在消息处理器，story 25）：
  1. `onSelectedItem` 的 **Prefix+Finalizer**：Prefix 经 `FastTransferRecoverEngine.TryReadIntent`
     （NoInlining+异常防护，宿主走 IntentProbeForTests 假缝）判完整分支守卫——Ctrl 按下、isStoring、
     源页 ∈ {2..6,7}（**0/1 主副手排除**=票面「背包↔容器」范围具名排除；AREA 排除归 04）、
     选中守卫（反射读 PDIUI 私有静态 selectedPage/selected_x/selected_y，读不到=fail-closed 不请求）、
     jar 在场、容器会话观察（03 `ObserveClient` 复用）= 受支持 kind（WorldContainer/VehicleTrunk）
     +内容指纹。虚拟箱/展示柜 = 不发包（与 03「不画按钮」同一边界，权威侧再 fail-closed 一道）。
     判中 → scope.Enter(intent 事实)。Finalizer 收 scope：若期间 `sendDragItem` 被原版调过 → 静默撤回
     （原版这次转移走自己的路，恢复零介入=「成功转移不排」）；未调 → `RequestFromIntent`（碎洞未发包
     的唯一事实=原版自己没发，与 04「postfix 只认原版失败」同判据风格）。
  2. `sendDragItem` 的 **Postfix**（只对着开着的 scope 记「已发包」）：真拖放/BII/右键菜单「存放到」
     按钮（onClickedStore）的 sendDragItem 都在 scope 外 → 结构性「不挂拖放预览、只接 Ctrl+右键」。
     binder 显式解析 MethodBase（04 R2 教训：名-only/attribute 绑定真机不可证，绑定唯一事实源=binder，
     宿主可证；`onSelectedItem` 为 PDIUI 私有静态 (byte,byte,byte)，`sendDragItem` 为 PI 公共实例
     7×byte，各恰一条，面 = 两个 patch 类钉死）。

- **消息面 = 功能私有 kind 9/10**（同频道同信封，零契约扩面；请求者身份=会话 PeerSteamId，与全部
  LIT 协议同规）：`MsgRequestFastTransferRecover=9`：[proto=3][token][requestId][sourcePage][sourceX]
  [sourceY][kind][fingerprint][desc]——codec fail-closed：sourcePage ∉ {2..6,7} 拒（0/1/8/255 永不
  入框）、kind 只 1/2、token/requestId 非零、长度精确；`MsgFastTransferRecoverResult=10`：
  [token][requestId][result][reasonCode]——**复用 03 八类结构化原因单源**（ContentChanged=容器内容
  已变/源物已走、ContainerClosed、LostAccess、UnsupportedKind、LayoutFailed、FeatureUnavailable、
  InternalFailure、Busy），Committed⇔reason None 互斥律同形 8。账本/租约/准入 = 03 同一台机器
  （LitAdmissionGate：token 校验→账本幂等→每玩家一把租约→requestId 单调）——快速转移与按钮整理
  **共享同一租约**：每玩家同一时刻只有一个整理事务，撞上的第二个=Busy 拒绝零修改。账本条目与
  客户端 pending 表加第三种帧形标记（fast-transfer），三种结果帧互斥消费（线谎话按 kind 丢弃）。
  客机结果处理 = **只记诊断，不弹提示**：恢复不是按钮，失败语义=原版静默（Q1「任一失败→原版失败
  语义」），与 03「可点必须回执」不同场景（票面无 toast 要求，04 同规）。
- **权威执行核 engine-free**（`FastTransferRecoverAdapter.TryRecoverCore`，与 04 同宿主可跑面形）：
  门序 = 模块未登记/ShuttingDown → 熔断开 → 意图形非法（页/kind 无 adapter）→ 03 会话重验
  （会话→种类→权限→**指纹版本**，提交前权威现读，两侧共用一个 verifier）→ 源件在场（权威坐标
  直读真 jar，走了=ContentChanged 零修改）→ 计划（下）→ 提交（下）。失败一律 = 零修改 +
  结构化 reason；Critical/Concurrent = 同一把 FaultGate 熔断（与按钮/04/03 同闸）。
- **计划与提交 = 一笔两页事务，禁「转移失败但目标已被单独整理」**：
  - 源侧 = `ManualTidyService.PreparePageLeave`（本票事务刀=04 pending 的镜像 `Leaving` 字段）：
    恒等布局（其余物品**逐格原坐标原旋转**，不排版=「源网格不整理」的字面落实），只排除这一件；
    同一组静态验证（重叠/越界/Tag 一致性/指纹多重集）全部 leave-aware。
  - 接收侧 = 02 唯一出口 `PreparePage(…, pending=这一件)`：玩家→箱子 = 整理当前受支持容器网格
    （kind+fingerprint 会话身份，范围与 03 一致）；箱子→玩家 = 按原版固定页序 **2..6 升序**试到
    第一个「整理+放入」可排版成立的页 =「原版选定的那一页」（碎洞态原版没选页；恢复复现其顺序、
    不得升格全身整理——其余页不进事务、一格不动）；候选页与容器目标各带原版同款 200 件满闸。
  - 两 prep 进同一个 `CommitPreparations`（04 提取的共享原子出口：journal 可验证回滚 + 指纹守恒）
    ——源先目标后（原版 ReceiveDragItem remove-then-add 同序）；任一页失败 → 原子回滚 = 两边都不动。
    待转移物品必须出现在接收侧提交结果内（Pending 语义），否则不算成功（禁吞物=04 同律）。
  - 成功尾巴：不发布 TidyCompleted、不触发压弹/合匣、不新增公开事件（Q5）；接收侧=玩家页时本地
    玩家走快捷键重绑同链（映射域过滤：只重绑**被排版那页**的在册绑定，未被卷页/离场件的绑定
    一律不碰=原版拖放从不维护快捷键的同款遗留）+ listen-host 投影 reconcile；远端玩家服务器侧
    `_hotkeys`=null 无从重绑=04 同款具名遗留。

- **具名排除（本票不做，防评审扩面）**：0/1 主副手→箱子不恢复（票面范围=背包↔容器）；AREA→箱子
  归 04；7→7 箱内同页挪位不是转移恢复对象（原版碎洞态本就无处可移；箱内整理已有 03 按钮官方面，
  恢复悄悄改箱=「转移失败但目标已被单独整理」的伪成功变体，禁）；右键菜单「存放到」按钮与真拖放
  不在触发面（票面=Ctrl+右键；onClickedStore 同款静默留原版）；「客户端找到空位已发包、主机
  ReceiveDragItem 竞态拒」——客机无从观察、不请求，留原版失败（本票触发面=「碎洞未发包」一条，
  具名接缝缺口）；`sendDragItem` 本身 Unreliable 丢包=原版既有行为不补。
- **具名接缝缺口（宿主不可跑，实机随 DEV-V5-08，03/04 同界）**：真 Harmony 登记端到端（binder
  宿主可证=形状，装机解析随 08）；真提交写面（Items.addItem 触 SDG.Unturned.Assets 静态初始化，
  宿主注入同语义假事务钉计划消费形状）；真机 UI 意图探针（Ctrl 键/isStoring/选中守卫）与客机
  观察指纹；远端玩家快捷键=原版同款遗留。
- **具名递延的判断性气味（双轴 R1–R2，按 output-review-loop §3 记录放行）**：
  1. **refusal 词汇表双份**（04/05 各一套同值 const）——代码注释已具名「each recovery
     names its own gates」，值一致无行为分歧风险；第三个恢复面出现时再议抽取。
  2. **engine-tail 近重复**（`FastTransferRecoverEngine.CaptureLocalHotkeys/AfterFailed`
     与 04 同名方法体近逐字；`ReadPages` 已显式复用 04）——两票签名字段域不同（pending+
     equip 尾 vs mapping 过滤尾），现抽取共享基类收益有限且有跨票耦合；如后续再增恢复面，
     随专门收敛票处理。（R2-Standards 新发现，本条为补记具名。）
  3. **门控骨架平行**（TryRecover 与 TryRecoverPages 的 gate 序/try-catch 形状相似）——
     复制的是骨架不是算法，spec「入口不得复制算法」指排版层，单一出口纪律由组4 钉住；
     判定不违例，仅作形状记录。
- **红测组 ↔ 票面验收对位**（宿主可跑面，`DevV5FastTransferRecoverTests` 六组）：
  ①触发面与范围——binder 面恰两条+形状逐参核实、scope Enter/sent/withdraw 语义（发包=零介入、
  未发=恰一次请求、 AREA/0/1/未Ctrl/未isStoring/选中守卫/虚拟箱 = 不开 scope 不请求）；
  ②生命周期登记即开关——未登记/注销/Stop = 不请求不执行，无功能 bool；③决策矩阵与只整理接收侧——
  两方向成功=只动两侧该动的格子（源恒等布局逐格对照、非候选页逐格不动）、放不下/会话过期/源件
  已走/租约Busy=两边逐格零修改；④官方先行消费——接收侧计划逐格等于 tagged-row-band-v1(+pending)
  计划、策略出口计数、分类器缝消费、leave-aware 验证器直调；⑤事务层 leave 语义——Tag 一致性/
  指纹多重集/守恒 expectedCount=before-1、回滚恢复含离场件（零修改复现）；⑥不发整理完成 +
  codec 9/10 全形状 fail-closed + 回路双端全链（pending 三 kind 互斥/租约互斥/重放缓存/畸形帧）。
