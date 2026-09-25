# DEV-V7-02：弹药观察单源与总弹药 HUD

Type: task
Status: resolved
Parent: spec.md（V2 第七阶段规格·现有官方功能玩法手感定界与接线）
Blocked by: None (can start immediately)
Spec: `../spec.md`（「弹药观察单源与总弹药 HUD（V7-T3 → DEV-V7-02）」节 + 共享单源约束）
Red: `--bue-v7-02-total-ammo-hud-red`

## What to build

持枪时原版「当前/上限」还在，下面显示「总弹药 {0}」。总数 = 枪上匣当前发数 + 身上五页匹配匣 + 匹配箱。打一发，或背包里匹配弹药变了，数字不晚于下一帧更新。0 也显示。不必开关背包。

## Scope

- **本票拥有**更好的换弹体验内部的弹药观察与口径匹配**唯一生产事实源**（身上五页匣/箱如何观察、如何与压弹同一套匹配）。HUD 是该事实源的**第一个消费者**，不是事实源本身。
- 公式与两份事实源按规格：枪上发数跟原版弹药标签同一事实；身上五页跟压弹匹配同一套。不含容器和地面。
- 文案「总弹药 {0}」。废止「备匣 N · 备弹 M」。位置沿用原版信息框左列下半。
- 刷新不晚于下一帧。授权功能内部观察缝（库存数量、枪上弹药）。不进契约，不另建第二套宿主泵。
- 切枪带走附加读数；功能停用回到裸原版。U3DS 不画。
- 必须改掉会钉死「备匣/备弹」或「枪上无弹量字段」的旧测试。

## 隔离（事实源 ≠ HUD 公共服务）

- 观察/匹配事实源必须能被后续被动压弹消费，而不引用 HUD Adapter、HUD Surface、HUD Binder，或任何 HUD 生命周期类型。
- 禁止把事实源做成 HUD 专用类型后再让 03 去调它。
- 本票不实现 2 级被动周期，不改技能页，不改功能 A。

## 验收条件

- [ ] 红测先行：公式（含枪上发数、空匣贡献 0、不含容器）；0 也显示；开火后下一帧变；身上五页匹配箱/匣 amount 变化后下一帧变、且不依赖再开背包。先红后绿。组名 `--bue-v7-02-total-ammo-hud-red`
- [ ] 官方先行：更好的换弹体验运行时持枪能看见总弹药（真机几何随 07 具名缺口）
- [ ] 事实源与 HUD 表面分层可证：测试能在不构造 HUD 表面的情况下消费观察/匹配结果
- [ ] 双轴独立审查 CLEAN
- [ ] **候选纪律**：不授候选 / RELEASES / CaseId

## Comments

- 2026-09-22：主会话认领 DEV-V7-02，按红测先行、事实源与 HUD 分层、HostTick 下一帧刷新及双轴 CLEAN 闭环实施；不触碰 V7-03 被动周期、公开契约或候选发布物。

## Answer

- 红测先行：新增 `--bue-v7-02-total-ammo-hud-red`，首轮因 `AmmoEntry` 缺席编译红；补入事实源后通过公式、空匣/0 显示、页 2..6、匹配箱主路径/fallback、文案与变化指纹测试。旧 V5-06 中“枪上无弹量字段”和“备匣/备弹”硬钉已改为 V7 总弹药语义；V5-06 对照组通过。
- 实现：新增非 HUD `AmmoObservation` / `AmmoTotalProjection`，枪上发数读取原版 `GunStateIndices.AMMO`，身上五页沿用 `ReloadRuntimePolicy` 范围，`CollectCompatibleAmmoIds` 与 `MagSuppliesMatch` 继续作为压弹/HUD 共享匹配源。观察器移出 `Hud/`，HUD 仅消费 `AmmoTotalResult`。
- 刷新：保留唯一 `UseableGun.updateInfo` Harmony 面；复用既有 `HostTick` 通过变化指纹刷新枪上发数和身上匹配匣/箱，不新增 Unity 泵、Harmony 面或公开契约。停用仍 `RevokeAll` 回裸原版，U3DS/headless 不画。
- 验证：Rebuild exit 0；V7-02 单组、V5-06 对照组、Plugin 无参全套均 exit 0；FULLSUITE 15 PASS、0 failed、1 个既有 Contracts/Glazier KNOWN-BASELINE（不计失败）。突变 M1 loaded-AMMO、M2c 页下界、M3b 主路径优先均 build=0/运行红，恢复后 build=0/测试绿；证据见 `audit/2026-09-22/DEV-V7-02/`。
- 双轴独立复审：fresh Standards reviewer = `CLEAN`；fresh Spec-Reviewer = `CLEAN`；最终 `/code-review` 无阻断。具名非阻断气味：V5 兼容适配器的少量委托、HostTick 到 HUD Adapter 的单次挂载，均为迁移期可接受判断题。
- 具名接缝缺口：真机 Glazier 几何、真实 updateInfo 频率、真实资产/背包观察、停用端到端裸原版、P2P 客机观察及长会话弱引用 GC 时序仍留给后续真机票；本票不伪称已验。
- 本票未授候选、未改 `RELEASES.md` / `publish/` / CaseId，未改契约 2.1，未实现 V7-03 被动周期、技能页或功能 A。

## Comments

- 2026-09-22：重新认领退回修复轮；本轮只补真实观察、库存事件/HostTick 下一帧链、无枪/换枪/infoBox 生命周期和生产链测试，不恢复候选或扩大到 V7-03。
- 2026-09-22 R1：本地生产链红测、V5-06、Plugin 全套与 FULLSUITE 已绿；但 fresh Spec-Reviewer 阻断真实 SDG 观察/真实生命周期测试证据不足，且三环境人工日志未回收，票据继续保持待修复，不恢复 resolved。
- 2026-09-24 R3/R4：同一合并 DLL SHA `D015DA8CD9228DD033E9EF97C5F3AB51966979638708E6BF9162FD7C53AC4C72` 完成单人、本地联机客机/主机、U3DS 客户端/服务端闭环。单人 page 2/3、联机客机 page 2/5/6、联机主机 page 2/3、U3DS 客户端 page 2 均出现真实 `inventory-event → HostTick+inventory-dirty → Apply=True`；多枪实例、换枪、无枪/`RevokeAll`、总数变化均有日志。U3DS 服务端确认 `decision=Headless`、survival pump、AmmoHud 不武装、LIR START、`bue-runtime-arm` 与 `RepackSuccess(total=12)`；服务端无 HUD Apply 为预期。Fresh Standards=CLEAN，Fresh Spec=CLEAN；page 4 未单独人工触发不构成规格阻断。外部 SteamP2PFriends/NoOp/ResourceObs 日志不属于本票。票据恢复 `resolved`；本票仍不授候选、RELEASES、CaseId。

## DEV-V7-02 实机退回记录（2026-09-22）

单人实机使用候选 `3588EB7D…CA35F8C6` 发现本票行为未兑现：

- 身上匹配弹匣或弹药箱数量变化后，现有总弹药 HUD 没有被证明在下一帧刷新；当前 `HostTick` 路径只调用通用刷新入口，测试使用假观察器，未覆盖真实库存变化到观察器再到 `ApplyTotal` 的生产接线。
- 枪上匣、身上五页弹匣与弹药箱的真实观察未形成可审计的运行时闭环；现有纯公式测试不能证明 `PlayerInventory.items`、`ItemJar.item.amount`、枪上 `state[GunStateIndices.AMMO]` 在真实游戏中被同一帧读取。
- 无枪、换枪或装备对象销毁时的 HUD 生命周期未完成实机证明；现有实现对本地 `useable` 缺席只返回，不足以证明旧标签隐藏、切枪重绑和新枪首次应用的连续行为。
- `updateInfo → HostTick → AmmoObservation → ApplyTotal` 的完整生产链没有对应的真实生产路径测试；现有测试主要替换 `TotalScannerForTests` / `TotalApplyForTests`，不能作为接线通过证据。

退回原因：当前候选的 LIR 弹药 HUD 运行时接线与真实库存/换枪刷新不满足本票规格。纯公式已存在不等于生产观察与刷新已兑现。
本票不接受 DEV-V7-07 发布票中的临时修复，也不以重新打开背包、再次开火或手工触发 `updateInfo` 代替下一帧刷新。
修复后必须重新执行本票红测、FULLSUITE、双轴审查和三环境人工复核；其中至少要有真实生产链测试或具名、可回收的实机日志证据覆盖枪上匣、身上五页弹匣/弹药箱、无枪/换枪和 `ApplyTotal`。
