# DEV-V7-02 repair loop R1

日期：2026-09-22
状态：修复轮未关单；票据保持 `ready-for-agent`/当前修复处理中。

## 退回背景

旧轮只证明纯公式、指纹和假观察器链；候选 `3588EB7D…CA35F8C6` 的实机表现未证明真实库存变化、枪上/五页/弹药箱观察、换枪/无枪生命周期和完整生产链。因此旧轮 CLEAN 不等于本轮接线通过。

## 本轮红→绿

- 新增 HostTick 生产链测试：只替换底层引擎事实读口，调用真实 `RefreshOnHostTick`、指纹、Apply 路由和无枪隐藏分支。
- 新增库存页闸测试：2..6 进入刷新，1/7/8 排除。
- 初始红态：缺少 `ReaderForTests`、`LocalGunForTests`、HostTick 测试入口等 seam，Rebuild 为 CS0117。
- 修复后：V7-02 专项 PASS；V5-06 对照 PASS；Plugin 全套 PASS；FULLSUITE 15 PASS、0 failed、1 个既有 Contracts/Glazier KNOWN-BASELINE。

## 生产修复

- `AmmoObservationEngine`：新增底层读口 seam；生产仍走 NoInlining SDG 观察；枪上当前发数优先读取原版 `UseableGun.ammo`，state[AMMO] 作为 fallback；保留 2..6 inventory 观察。
- `InPlaceReloadModule`：按 BII 模式在 HostTick 惰性绑定/解绑 `onInventoryAdded/onInventoryRemoved/onInventoryUpdated`；只对 2..6 事件标记 dirty；Player.LocalPlayer 读取失败局部隔离，不中断 HostTick。
- `AmmoReserveHudAdapter`：库存 dirty 会清除指纹，保证下一拍重新观察；无枪触发 RevokeAll；Apply 只有成功呈现后才缓存指纹。
- `AmmoReserveHudSurface`：Slot 记录当前 infoBox；infoBox 重建时重新创建标签；HideAll 重置 ConditionalWeakTable，支持二次启停。

## 突变

- M1 去掉无枪 Hide：build=0、V7-02 test=1 红；恢复后 V7-02 PASS。
- M2 去掉 inventory page gate：build=0、V7-02 test=1 红；恢复后 V7-02 PASS。

## 审查

- R1 Standards reviewer：CLEAN；仅列出测试 seam/生命周期 gate 的判断题气味。
- R1 Spec-Reviewer：阻断，理由：
  1. 库存事件此前只在模块内清 dirty，已修为 Adapter dirty 强制下一拍观察；
  2. 生产链测试仍通过底层 ReaderForTests/TotalApplyForTests 与 BypassLifecycleForTests，尚不能证明真实 SDG 观察与真实模块生命周期；
  3. 换枪/infoBox 重建没有真实可回收测试；
  4. 三环境实机日志尚未回收。

## 当前状态

本地构建与回归已绿，但票据不恢复 `resolved`。必须补真实运行时接线证据：实际 DLL SHA-256 绑定的 SP/P2P/U3DS（有画面的客户端）日志，覆盖开火、换匣、匹配匣/箱 amount 变化、空手、换枪、重新装备、infoBox 延迟/重建；并重新派 fresh Standards/Spec 审查后才可关单。不得用重新打开背包、再次开火或 V7-07 临时修复替代。

## R2 增量（2026-09-23）

- 红测先行：将 `HostTickProductionChain` 改为真实 `InPlaceReloadModule.Start` + `OnHostTick` 驱动，覆盖唯一 `AmmoReserveHudPatch.Postfix(updateInfo)`、同指纹去重、库存 dirty 强制重观察、身上 amount 变化、换枪实例和无枪隐藏；模块安装仍使用既有 V5-06 安装替身，未启用 `BypassLifecycleForTests`/`RefreshOnHostTickForTests`。
- 观察缝收窄：`ReaderForTests` 改为 `FactsReaderForTests -> AmmoEngineFacts`，只注入原始枪/库存事实字段；`AmmoObservationEngine.FromFacts` 继续由生产代码组装 `AmmoObservation`，不再直接替换完整 DTO。
- 生产修复：HostTick 将联机/输入异常与本地弹药观察分为两个隔离段；`AmmoReserveHudAdapter` 修正 dirty 刷新缩进并将测试适配器命名为 `InvokeApplyAndSucceed`；`AmmoReserveHudSurface` 增加 `NeedsSlotRebind`，同枪 infoBox 重建先 `ConditionalWeakTable.Remove(gun)` 再重新登记，修复重复 key 回滚风险。
- 新增突变：删除 dirty 指纹失效逻辑时 build=0、V7-02 test=1 红（dirty 总数不变与后续 amount 变化均失败）；恢复后 build=0、V7-02 test=0 绿。既有 M1/M2 证红仍保留。
- 静态验证：V7-02 PASS；V5-06 PASS；Plugin 全套 PASS；FULLSUITE 三轮均 PASS（16 PASS、0 failed、Contracts/Glazier 既有 KNOWN-BASELINE 1 条、防火墙 0 命中）。
- R2 Standards reviewer：CLEAN（仅判断题气味：AmmoEngineFacts 数据泥团、`IsAmmoInventoryPageForTests` 命名、反射回退分支缺专测）。
- R2 Spec reviewer：阻断。当前测试仍以 `FactsReaderForTests` 提供预制原始事实、`LocalGunForTests` 提供假枪、`TotalApplyForTests` 记录呈现结果，尚未证明真实 SDG `Player.inventory/items/ItemJar.item.amount` 读取与真实 `AmmoReserveHudSurface.ApplyTotal`；三环境日志也未回收。
- 结论：代码修复与本地回归完成，但本轮仍保持 `claimed`，不授候选、不写 `RELEASES.md`、不生成 CaseId、不交付 DLL；下一步必须先获得真实生产观察/Surface 的可回收证据，再按 `real-machine-test-loop.md` 重新双轴审查。
