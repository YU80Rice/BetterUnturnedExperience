# P2P 探针 #3 结果（f4bdd440，包 102832/102944）

identity=探针构建（非候选）。

## 事实链

1. 客机 0 级多次 RequestRepackAmmo → 主机仅一条 PROBE3=`技能窗拒绝 remaining=5.0`（窗已开）。无 `Committed total=0`、无 `事务静默结果`。
2. 升 1 级后 reqId …459 → 主机 `RepackSuccess total=45`（权威事务跨端能成交）。
3. 升 2 级后 reqId …461 → 主机 `RepackSuccess total=9` + `2 级自动压弹已排队 steam=…2479 等待 8s`。
4. 8s 后主机 `RepackSuccess reqId=639252078257432703 total=6`（自动轮权威成交 6 发）。
5. 客机 `收到未知或重复 requestId=639252078257432703 的回包，忽略`——自动轮 requestId 不在客机 pending 表（主机 `NextRequestId()` 自造），toast 被丢。

## 缺陷定性

- **F8 0 级窗先于成交武装**：`TryAdmitDoubleTap` 放行即写 windowReadyAt。闸拒/0 发仍开 9.5s 窗 → 「没压进子弹却进 CD」。票面「0 级仍能双击、只是多一段冷却」被破坏。
- **F9 自动轮回包被客机当未知 id 丢掉**：主机成交+回包，客机 pending 守卫拒收。G07-6「回包定向」部分成立（冷却/升级通），自动轮腿不成立。

v2 `D7549806` 作废。修复进 v3 `589300E2`。
