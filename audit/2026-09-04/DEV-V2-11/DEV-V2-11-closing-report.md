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

## 5. 候选身份（提交后授予）

候选从提交树 `6907a1a831ca4708e32153ac0f873f4145ea197c`（`6907a1a`，含本票全部源码/测试/票面/本报告）Release 重建，
确定性复核通过（二次重建 SHA-256 逐字节一致）。**DefinitionSetDigest 与 DEV-V2-10 候选一致**（`38D66989…B854`）——
本票只改类型名常量与运行时行为，官方定义集未动，交叉验证自洽。

| 项 | 值 |
|---|---|
| CandidateBuild | `DEV-V2-11-CLEAN-20260904` |
| CaseId | `DEV-V2-11-20260904` |
| SourceSnapshotId | `6907a1a831ca4708e32153ac0f873f4145ea197c`（6907a1a） |
| DLL SHA-256 | `5B4E948E5A81FB75B05D017BF0958C11B4B0C75279DB6F21D2151CA8C84C5BCD`（268800 字节） |
| BuildIdentity | `01BFF64000C29FC1FEA8B2C13C8A4EC19EFD0A1A55D8743E512B8D6797FC86B5` |
| DefinitionSetDigest | `38D66989136D008A1AE27732544F9680F35C5C75BB12BFDDA570BD5844C8B854` |
| ReferenceSet（Client+U3DS） | `Libs-ReferenceSet-951EFCD4E73C37E2D514B6B7D05AE8FDF2141F3C18A9C60D37192BD068775030` |
| ToolchainIdentity | `MSBuild-18.9.0.32302|.NETFramework-4.7.2|CSharp-10` |

归档：`audit/2026-09-04/artifacts/DEV-V2-11-20260904/{BetterUnturnedExperience.dll, candidate.json}`；
身份记录 `audit/2026-09-04/DEV-V2-11-dll-sha256.txt`。前两轮候选（`14A98FC8…`/`C3A35B07…`）归档原样未动。

## 6. 人工复测清单（本票完成后停于此）

部署新 DLL（两端 plugins 替换，certutil 核对 `5B4E948E…`；assembly-identity 行须一致）后：
1. **开 Debug**：`BepInEx\config\BepInEx.cfg` → `[Logging.Disk]` → `LogLevels` 加 `Debug`（或每会话归档 Unity Player.log）。
2. **P5**：四端零「BUE 错误：」行，且**零 `AccessTools.TypeByName … ModTransport` 警告**（上轮 63~169 条）。
3. **正向锚**：`event=v1-table-mirror result=mirrored channels=N deferred=true` + `event=lmn2-delegate result=delegated decision=consume` 各恰一条（Debug 通道）。
4. **面板四步**：两条目可见+接管卡两行+「让我改回独立 LMN」关→开（`removed`→`installed`），全部截图。
5. **P3/P4a/P4b**：V1/V2 seq 对齐仍通（P3 证据须含委托锚——仅 [V2FIX] 对齐不再足以证明 BUE 委托路径）。

