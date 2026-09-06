# DEV-V2-07 配置 A（零误报基线）实机复核记录 — R1（2026-09-04）

> 采集人：用户（real-machine-test-loop）；复核：agent。证据已归档 `../evidence/DEV-V2-07-20260904/configA/`（源=UMM 诊断包，961K）。
> 会话映射：`local-vm-client-155941`=虚拟机客机（本地联机客户端）、`local-host-160703`=主机（单人+本地联机）、`u3ds-client-161432`=U3DS 客户端、`u3ds-server-1615_LogOutput.log`=U3DS 服务器。

## 核对结果（对照手册 §3 A1–A3 / §5 U-A1–A3）

| 项 | 期望 | 实测 | 结论 |
|---|---|---|---|
| A1 加载成功+界面注入 | 「加载成功，界面已注入」 | 三份客户端包均有；主菜单「BUE 插件管理」按钮可见（截图） | ✅ |
| A1(零 patch) | 无任何 takeover-patch 行 | 三客户端 + U3DS 全部 0 行 | ✅ |
| A2 空迁移行 | `lmn-config-migration result=no-op mapping=empty reason=lmn-has-no-config BUE-V2NET-001`（**Debug 级**） | LogOutput.log 未捕获（`[Debug` 行数=0×3，磁盘 LogLevels 未加 Debug）；**但 Unity `Player.log` 通道捕获成功**（`AppData\LocalLow\Smartly Dressed Games\Unturned\Player.log` 及 `Player-prev.log`，BepInEx Debug 行经 Unity 通道落盘、不受磁盘 LogLevels 过滤）：两会话均有该行，与手册期望逐字一致 | ✅ 本机双客户端实例均证实（客机/U3DS 侧随补采以同通道补记） |
| A2+ 退出清理线 | （手册速查表未列）`network-module-isolated decision=stop-and-hand-back BUE-V2NET-003` | 两会话各一条；源码定位 `BetterUnturnedExperiencePlugin.cs:354-358`（DEV-V2-06 卸载路径：插件 unload 前全拆解 hand-back）——**正常清理行为，非故障**；手册速查表漏列，如实记录为观察项（不改冻结手册，随资格裁决报告说明） | ✅ 预期行为，卸载可逆性实锤 |
| 会话绑定 | 证据与会话一一对应 | BepInEx 横幅时间戳指纹对齐：`Player.log`/`Player-prev.log` 与 主机包 160703、U3DS 客户端包 161432 同为 **9:14:55**（本机双客户端实例：本地联机主机 + 后连 U3DS 的客户端）；客机包 155941 = VM 实例 **10:27:13**。两份 Player 日志 Mono path 均为 `…/Unturned/Unturned_Data/Managed` → **均非 U3DS**（哈希对比不适用：Player.log 与 LogOutput.log 是不同 sink；会话指纹才是正确对比键）。另发现 UMM 目录存在更早两包 103302/103321（上午场），一并归档 | ✅ 归属理清 |
| A3 零误报 | 无「BUE 错误：」行；BII/原版玩法正常 | 三包「BUE 错误」=0、BUE-V2NET-002=0、无 LMN/fixture 痕迹；用户确认三环境 BII 功能无问题 | ✅ |
| U-A1 U3DS 加载 | 「加载成功（无界面）」 | **日志不完整**：仅 BepInEx 启动头 + Chainloader startup complete（16:10:52 起，1132 字节），缺该行与会话主体 | ⚠️ 缺证，待补采 |
| U-A2 无 UI 实例化 | 无 ClientUi/Sleek/Glazier 报错 | 现有片段内零命中（但片段不完整，随补采复核） | ⚠️ 待补采确认 |
| 身份绑定 | 三环境同一候选 DLL | `BUE-MANAGEMENT-TRACE-002 event=assembly-identity sha256=14A98FC8…E5EF6` 在 主机/客机/U3DS 客户端/U3DS 服务器 四端全部命中，与候选授予哈希逐字一致 | ✅ 四端同候选实锤 |

## 判定

**配置 A 基本全绿（A1/A2/A3 + 四端身份绑定 + 卸载可逆线全部证实；仅 U-A1/U-A2 的 U3DS 完整会话日志待补）。** 配置 B 可开始部署准备；正式采集时：

1. 客户端（主机+客机）与 U3DS 的 `BepInEx/config/BepInEx.cfg` → `[Logging.Disk]` → `LogLevels` 追加 `Debug`——让 LogOutput.log 自含全部判据行（`takeover-patch`、`unknown-channel-dropped` 等），不依赖旁路通道。
2. 每次会话**额外归档 Unity `Player.log`（+`Player-prev.log`）**——已证实其为 Debug 级行的冗余通道，与诊断包一并提交。
3. U3DS 补采：起服冒烟一次，**正常退出后**取完整 `LogOutput.log`（拿「加载成功（无界面）」、A2 行与会话主体）。
4. 现有 A1/A3 证据不作废。

## 同轮新发现（不阻塞，已立票）

- **主菜单「BUE 插件管理」按钮与相邻条目（创意工坊/商店位）的垂直间距与原版图标节奏不一致**（用户报告+截图，2026-09-04）。已立票 `DEV-V2-09`（needs-triage）；07 冻结不改源码，修复走红测先行+双轴。
