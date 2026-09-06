# DEV-V2-10 候选实机复核记录 — R1（2026-09-04 夜）

> 采集人：用户（UMM v2.2.1 诊断包 ×3 + U3DS LogOutput ×1）；复核：agent。
> 证据归档 `../evidence/DEV-V2-10-20260904/configB-retest/`（1.1M）。
> 候选：DEV-V2-10-CLEAN-20260904（DLL `C3A35B07…E4224`，BuildIdentity `75DA7E6D…`，提交 e8c3a52）。

| 环境 | 包 | 会话绑定（包打包时间） |
|---|---|---|
| VM 客机 | `vm-client-215250/` | 21:53 |
| 房主主机 | `host-215303/` | 21:53 |
| U3DS 客户端 | `u3ds-client-215822/` | 21:58 |
| U3DS 服务器 | `u3ds-server-2200_LogOutput.log` | 21:59 导出 |

## 判据核对（结单报告 §8）

| 判据 | 期望 | 实测 | 结论 |
|---|---|---|---|
| 部署指纹 | 四端 assembly-identity sha256 = `C3A35B07…` | 四端 LogOutput 全部 `C3A35B07E7825002D8302D8D1135B78677806738367705CCE01979EFDA0E4224` | ✅ |
| P5 零误报（字面） | 无「BUE 错误：」行 / 无 result=failed | 四端均 0 / 0；BII 人工确认无异常 | ✅ |
| P3/P4a 功能面 | V1/V2 双向互通 | 四端 V1FIX/V2FIX recv/send 全部成对（如 host 34/34、U3DS 服 15/44） | ✅（但见 F-C 机制修正） |
| 镜像新锚 | `result=deferred` ×1 + `result=mirrored channels=N deferred=true` | LogOutput 无（**Debug 未开**，Debug 行不可见） | ⏸ 无法判定（见 F-D） |
| 反证锚 | 镜像生效后 `unknown-channel-dropped` 消失 | LogOutput 无该行（Debug 未开，同样不可见） | ⏸ |
| B5/B6 可逆 | `takeover-patch removed→installed` 序列 | LogOutput 无 takeover-patch 行（Debug 未开） | ⏸（见 F-D） |
| 重试噪音 | —（新观察项） | **四端 HarmonyX Warning `Could not find type …ModTransport` 63/124/169/63 条**，持续到会话尾部 | ❌ **F-C** |

## F-C（error 级，镜像与 LMN2 委托从未生效的真根因）

BUE 常量 `ModTransportTypeName = "LaunchMultiplayerNet.Routing.ModTransport"`、`ModRouterTypeName = "LaunchMultiplayerNet.Routing.ModRouter"`——**LMN 源码实证两类的真实命名空间是 `LaunchMultiplayerNet`（无 `.Routing` 一级）**：
`LaunchMultiplayerNet/Routing/ModTransport.cs` → `namespace LaunchMultiplayerNet` + `public static class ModTransport`；
`Routing/ModRouter.cs` → `namespace LaunchMultiplayerNet` + `internal static class ModRouter`。

推论（两轮实机同证，07 的 F-A 归因需修正为「时机 + 类型名双因」）：
1. **镜像从未成功**：类型解析永远失败 → 重试永久 pending → HarmonyX 每次解析失败喊一条 Warning（本轮修复把一次性 ERROR 变成了每 5s 一条 Warning 刷屏，63~169 条/会话，四端同现）。V1 实际仍全靠自愈链。
2. **LMN2 委托从未发生**：ModRouter 类型名同样错误 → `DelegateNamespacedFrame` 恒走 router-null 放行 → V2 named 互通一直是 **LMN 原生前缀**扛的，BUE「短路委托」从未执行。07 复核 P3 的「经 BUE 接管决策点委托」口径需更正为「经 BUE 决策点放行、LMN 原生送达」。
3. DEV-V2-06 测试全绿的原因：测试用注入桩类型，真名从未与 LMN 程序集对过——与 F-B 摘要手抄同类的「对照真源缺失」缺陷。

## F-D（证据缺口，非代码缺陷）

LogOutput 未开 Debug（`[Logging.Disk] LogLevels` 无 Debug）且 UMM 诊断包不含 Unity Player.log →
镜像 deferred/mirrored 行、takeover-patch 序列（B5/B6）、unknown-channel-dropped 反证全部不可见；
面板两条目/接管卡为 UI 事实，用户口头确认 BII/基础交互正常，但**无截图/日志留证**。

## 判定

**P5 字面通过 + 功能面全通，但 F-C 使 F-A/F-B 修复的守护价值（镜像生效、真委托）未达成，且新增警告刷屏 → 不能关闭，立 DEV-V2-11 修复轮**：
- 修复：类型名常量更正为真名（LMN 源码为 authority）+ LMN 类型解析静默化（弃 `AccessTools.TypeByName`，消 Warning 刷屏）；
- 红测：类型名锚（与 LMN 真名逐字一致，防再抄错）+ 静默解析 helper（不存在类型→null 且零日志）；
- 复测（下轮候选出后）：**开 Debug**（LogLevels 加 Debug 或归档 Player.log），补采面板四步截图 + B5/B6 + mirrored/deferred 行 + 零 HarmonyX 警告 + P3/P4a 仍通。
