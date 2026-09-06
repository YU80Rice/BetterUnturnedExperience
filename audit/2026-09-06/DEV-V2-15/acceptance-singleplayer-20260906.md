# DEV-V2-15 单人实机验收记录（2026-09-06）

## 验收结论（用户原话留档）

> 「单人测试：……我验证LIT功能无异常」——用户，2026-09-06（主会话消息原文）

诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.1-win-x64\UMM-诊断包_20260906_183015`（含 `LogOutput.log` / `Client.log` 等）。

## 部署物身份绑定

`LogOutput.log:136`（BUE-PLATFORM-002 同型 identity 行）：

```
event=assembly-identity path=E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll
sha256=CACFA527BB4E593BD09885CBFA12997F4A192D601C6D08FC4270317b9b03b040
```

与 RELEASES 行 8 候选 `cacfa527…b040`（350208 字节）逐字符一致——验收证据绑定本票候选，无替换物。

## 功能链路日志证据（LogOutput.log 行号）

| 验收点 | 证据 |
| --- | --- |
| 模块注册经宿主面 | :165 `BUE Inventory Tidy featureId=io.github.yu80rice.bue.inventory-tidy accepted=True reason=None diagnosticId=BUE-REG-ACCEPT` |
| 补丁安装（Harmony ID=FeatureId） | :139 `[Tidy] 整理按钮补丁已安装（Harmony ID=io.github.yu80rice.bue.inventory-tidy）` |
| 按钮注入页 2–6 | :535-540 `headers[0..4] -> page 2..6 三按钮注入 OK`、`注入完成：共 5/5 组三按钮` |
| 点击→入队→执行链路 | :1253-1254 `点击 -> 整理 page 3 …[本地整理]`、`page 3 整理请求已入队（本地主线程执行）` |
| 整理事务提交 + 指纹守恒 | :1333 `本地整理已提交（page=3, mode=SameType, mappings=7）`（:1332 `placed=7 … 指纹守恒验证通过`）；FFD 模式 :1561、:1787 两次提交 |
| 关闭 → 原生回退 | :2160 `[Tidy] 整理按钮补丁已撤销（原生回退）`（面板开关关闭路径实测） |
| 全程无本模块异常 | 全日志唯一 Exception 来自无关旧插件 SteamP2PFriends（:558，SPF-0.2.4.8 结构基线观察，非 BUE/LIT） |

## 观察项（不阻塞验收，登记移交）

**降序 SameType 首次整理被安全拒绝**（:1253-1266）：`page 3 静态验证失败（重叠）→ Prepare 失败 → Rejected`——用户切换升序后同页提交成功、大件模式两次提交成功。判定：

- 该代码路径（`ValidateNoOverlap`/`ValidateBounds` → 「静态验证失败（重叠）」日志）与旧插件**逐字节相同**（R3 Standards 零算法 diff 实证），属**迁入前既有算法行为**，非迁移回归；注意 `ValidateNoOverlap` 对重叠与越界返回同值，日志文案统一打「重叠」，实际失败模式需内容复现才能区分。
- fail-closed 语义按设计工作：拒绝 + 零副作用（:1257 `本地整理被安全拒绝且未修改物品`）+ 不误触熔断（Rejected ≠ CriticalFailure）。
- 处置：移交未来「设置与策略治理/排序规则变体」票（spec Out of Scope 已预留），本票按 O-LIT-1 勘误口径不改算法。

## 验收覆盖说明

本次为**单人环境**验收（工单验收第 2 项 + 第 3 项面板条目随功能链路一并验证）。P2P / U3DS 联机路径属 DEV-V2-21（未实施）；三环境终验绑 DEV-V2-24。
