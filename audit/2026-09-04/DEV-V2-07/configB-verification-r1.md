# DEV-V2-07 配置 B（接管矩阵）实机复核记录 — R1（2026-09-04）

> 采集人：用户；复核：agent。证据归档 `../evidence/DEV-V2-07-20260904/configB/`（3.0M）。
> 会话绑定（以文件 birth time 为准；**BepInEx 横幅时间戳实测不可信**——Player.log birth=16:57:56 而横幅写 9:14:55，弃用横幅作会话指纹）：

| 会话 | 文件 | 绑定 | 时间（birth→末次写） |
|---|---|---|---|
| 主机 interop 场 | `local-host-165513` + `player-logs/Player-prev.log…session1` | Player-prev.log birth 16:49:22→16:55:10 | 16:49–16:55 |
| VM 客机 | `local-vm-client-165453` | 客机自报 10:27:13 启动（VM 时钟） | 包导出 16:54 |
| 主机单人场 | `player-logs/Player.log…session2` | Player.log birth 16:57:56→17:02:38 | 16:57–17:02 |
| U3DS 客户端 | `u3ds-client-170257` | — | 包导出 17:02 |
| U3DS 服务器 | `u3ds-server-1703_LogOutput.log`（161 行，**完整**） | 含「加载成功（无界面）」 | 17:03 导出 |

## 判据核对（手册 §3 B1–B6 / §4 P1–P6 / §5 U-B1–B5）

| 判据 | 期望 | 实测 | 结论 |
|---|---|---|---|
| B2/P1/U-B1 fixture 就绪 | `[LMNFIX] ready … channels=v1-server=ok;v1-client=ok;v2-server=ok;v2-client=ok;` | 四端全部就绪，lmnOperational=True，四路注册全 ok | ✅ |
| B1/U-B1 接管安装 | `takeover-patch result=installed priority=first targets=NetMessages.ReceiveMessageFromClient,NetMessages.ReceiveMessageFromServer BUE-V2NET-003`（Debug） | LogOutput 未见（cfg Debug 仍未开）；**Player.log 双场均有**（L171） | ✅（Player.log 通道） |
| U-A1′ U3DS 加载 | 「加载成功（无界面）」 | 完整日志命中 | ✅（补上配置 A 缺口） |
| P3 V2 命名频道互通 | 双端 seq 对齐 ping/pong | 客机 send-to-server=15 / recv kind=pong=11；主机 send-broadcast=17 / recv-from-client=11；U3DS 35/14。V2 双向全通 | ✅ |
| P4a V1 兼容直连观察 | Host 应有 `recv-from-client` | **主机 recv-from-client=11、send-pong=11；客机 recv kind=pong=11；U3DS recv-from-client=14、send-pong=14——V1 双向互通** | ✅（机制见下） |
| P5/U-A3 零误报 | 无「BUE 错误：」行 | **四端各有恰好 1 条 ERROR**：`v1-table-mirror result=failed errorType=ArgumentException decision=drop-path BUE-V2NET-002` | ❌ **F-A finding** |
| P2/B4 面板接管卡 | 「已由 BUE 接管」+「无独立配置可迁移(LMN 无配置文件)」 | **侧栏无「BUE 网络模块」/「BUE V1 兼容层」条目**（应来自 `ClientUiCompositionRoot.ToManagementEntry` L98-99 的目录映射），用户只找到 LaunchMultiplayerNet 插件页（其「普通 BepInEx ConfigEntry」为空属预期——LMN 无配置系统） | ❌ **F-B finding** |
| B5/B6/P4b 可逆钮与重镜像 | 面板开关操作 | 无法执行（F-B 阻塞） | ⏸ 待 F-B 修复后补采 |

## V1 互通的机制解读（F-A 的严重度评估）

第一场 Player-prev.log 逐 seq 显示**每个 V1 ping 成对到达**：`unknown-channel-dropped channel=250`（BUE 前缀镜像未命中→**放行**）紧接 `[V1FIX] recv-from-client`（**LMN 原生前缀送达**，自愈链按设计生效）。即：

1. **功能层**：旧插件路径双向无损互通（BUE 放行、LMN 送达）——票面「V1 兼容路径两端收发」**实质达成**；`unknown-channel-dropped` 实为「BUE 让位」而非丢帧。
2. **守护层**：bootstrap 镜像（`MirrorLegacyHandlersSafe`）在 LMN 表未就绪时抛 `ArgumentException`→ 全程未生效，V1 全靠 fallback；且每端留 1 条 ERROR 级「BUE 错误：」违反 P5 零误报判据。时序根因与交付报告 §7.3 具名风险一致：BepInEx 按文件名序加载，BUE（B）先于 LMN（L） bootstrap，镜像跑在 LMN 建表之前。
3. Player.log 单人场（session2）同类：镜像失败 + 28 次 drop（该场为放行非丢帧），无客户端在场。

## 判定

**配置 B 功能面全通（接管安装/V2 双向/V1 双向/U3DS 完整/fixture 四端就绪），但按手册协议 P5 零误报未达成 → 触发 real-machine-test-loop 修复轮**，两个 finding：

- **F-A（必修，error 级）**：V1 镜像时机缺陷——bootstrap 镜像早于 LMN 建表，`ArgumentException` 致镜像全程失效 + 每端一条「BUE 错误：」行。修复方向：镜像在 LMN 未就绪时**静默延后/惰性重试**（不得抛错误行），或注册事件驱动重镜像；红测 = BUE 先载、LMN 晚注册 ch250 的时序锚点（复用 DEV-V2-05 的 no-op fixture 形状）。
- **F-B（必修，阻塞 P2/B4-B6/P4b）**：管理面板缺「BUE 网络模块」/「BUE V1 兼容层」条目（接管卡与可逆钮不可达）。根因待查：`RefreshManagementPanel` 仅在 Initialize（`ClientUiCompositionRoot.cs:68`）与 `BetterUnturnedExperiencePlugin.cs:278`（条件调用）刷新——疑似刷新时机早于网络功能注册，或目录条目未入 `bueFeatures`。与 F-A 同根（BUE 先于 LMN 的时序）可能性高。

两 finding 修复走红测先行 + 双轴 CLEAN → 出新候选 → 通知复测（补采面板四步 + 零错误行确认）。**候选身份锚点不变**（`966b53f` 树，DLL `14A98FC8…`）；修复产出为新 DLL/新身份。

## 附带核验

- 客机 15 发 11 收：前 4 ping 在连接建立前发出（10 秒节拍 × 4 ≈ 40 秒入网窗口），非丢帧。
- `result=removed decision=hand-back-to-lmn` 出现在 session2 末尾且与 `network-module-isolated` 相邻（L3270-3271）= **退出清理序列**，非用户操作。
- 主机 interop 场「双 9:14:55 横幅」之谜已解：横幅时间戳与 birth time 不符，弃用；以 birth time + 内容指纹绑定会话。

---

> **勘误（2026-09-05，DEV-V2-11 复核后追加）**：本文 §2 对 F-A 的归因「时序根因与交付报告 §7.3 具名风险一致：
> BepInEx 按文件名序加载，BUE（B）先于 LMN（L）bootstrap，镜像跑在 LMN 建表之前」**已被实机证据推翻**。
> DEV-V2-11 候选复测（见 `../DEV-V2-11/configB-retest-verification-r1.md`）实锤：BepInEx 在插件发现阶段
> 预载全部程序集（主机日志 L14 LaunchMultiplayerNet 早于 L142 BUE 实例化），时机窗口不存在；镜像/委托
> 失效的唯一根因是 BUE 常量类型名多写一级 `.Routing`（`LaunchMultiplayerNet.Routing.*` → 真名
> `LaunchMultiplayerNet.*`）。类型名更正后镜像在 ActivateCore 即成功（`result=mirrored channels=2`，
> deferred 零触发）。本文其余判据与机制解读（自愈链让位）不受影响。
