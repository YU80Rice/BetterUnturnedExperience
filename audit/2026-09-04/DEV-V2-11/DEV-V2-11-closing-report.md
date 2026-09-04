# 结单报告 — DEV-V2-11：LMN 类型名修正 + 解析静默化 + 委托正向锚

> 票：`.scratch/bue-v2-lmn-adoption/issues/DEV-V2-11-lmn-type-name-and-silent-resolver.md`
> 触发：DEV-V2-10 候选四端实机复核（`../DEV-V2-10/configB-retest-verification-r1.md`）F-C + F-D。
> 日期：2026-09-04。审查循环：R1 Standards CLEAN / Spec FINDINGS(GAP-1) → R2 修复+重审 **双 CLEAN**。

## 1. 根因与修复

**F-C**：生产常量 `ModTransportTypeName/ModRouterTypeName` 多写一级 `.Routing`（真名
`LaunchMultiplayerNet.ModTransport` / `LaunchMultiplayerNet.ModRouter`，LMN 仓库源码实证）。后果链：
镜像与 LMN2 委托在**任何一版实机上从未生效**（07 与 DEV-V2-10 候选两轮同证），V1/V2 全靠自愈链；
DEV-V2-10 修复轮的 deferred 重试每 5s 调一次 `AccessTools.TypeByName`，其失败路径被 HarmonyX 记
Warning → 四端每会话 63~169 条刷屏。

修复（3 文件，见冻结件 `dev-v2-11-review-freeze-r2.diff`，183 行）：
1. 两常量更正为 LMN 真名，并加 `internal` 暴露 + 红测锚钉死（LMN 源码为 authority）。
2. `TryFindLoadedType`：遍历 `AppDomain.CurrentDomain.GetAssemblies()` + `GetType(name, false)`，
   缺失静默返回 null（替代 `AccessTools.TypeByName`，消除重试窗口的 Warning 刷屏；NetMessages/CSteamID
   的 AccessTools 解析不受影响——那些类型恒存在）。
3. Spec R1 GAP-1：LMN 无逐帧日志，委托路径与 LMN 原生前缀在 LMN 侧不可区分 →
   `DelegateNamespacedFrame` 首次成功消费时发一次性 Debug 记录
   `event=lmn2-delegate result=delegated decision=consume`（`delegatedRecorded` 一次性状态，异常/放行不置位）。
   票面 Scope 2b 固化口径：命中 LMN2 → BUE 反射调 `ModRouter.TryHandleFromClient/Server`
   （真签名 ITransportConnection/byte[]/int/int），true 短路消费、false/异常放行自愈链。

**F-D（证据缺口）**：非代码缺陷，并入下轮复测清单（开 Debug / 归档 Player.log / 面板四步截图）。

## 2. 红测证据链（留档本目录）

| 锚 | 红 | 绿 |
|---|---|---|
| 类型名（F-C 主锚） | `red6-flag-bue-lmn-type-names-red.log`（临时回退错名观察） | `green1/green2-flag-bue-lmn-type-names-red.log` |
| 委托一次性记录（GAP-1） | `red7-flag-bue-config-migration-red.log` | `green2-flag-bue-config-migration-red.log`（恰一条/跨向一条/放行不记） |
| 警告静默（F-C 噪声半边） | 实机证据=复核记录 63~169 条/会话 | 宿主锚：缺失类型零日志输出（`green2-flag-bue-lmn-type-names-red.log`）；实机零警告留待下轮复测 |

## 3. 审查循环

- **R1**：Standards **CLEAN**；Spec FINDINGS(GAP-1)（口径未固化+复测不可区分）。
- **R2**：修复后重审（`dev-v2-11-review-freeze-r2.diff`）：**Standards CLEAN**（一次性状态机/异常路径/
  旧测收紧/路由安全/TEMP 无残留）+ **Spec CLEAN**（GAP-1 闭环、锚属 F-C 合理组成、无票外改动）。

## 4. 门禁（最终树）

Release `-t:Rebuild` 0 警告 0 错误；七运行器全 exit=0；NoUiTokens Core/ClientUi PASS；`git diff --check` 干净。

## 5. 候选身份（提交后授予，补记于文末）

