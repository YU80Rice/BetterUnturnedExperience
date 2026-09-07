# 自动化实机干跑记录(Tier-1,2026-09-07)

> 目的:验证「agent 全自动实机测试」链条的最小闭环——U3DS 专用服务器:部署指纹 → 解析启动参数 → 命令行启动 → 锚行轮询 → 日志采集 → 进程收尾。
> 本轮**不认领工单、不改工单状态、不产出候选**——纯链路验证。

## 已验证通过 ✓

| 环节 | 结果 | 证据 |
|---|---|---|
| 环境侦察 | 客户端/服务器/Steam/UMM 路径全部在位 | 本轮 bash 输出 |
| 部署指纹 | 客户端 = `cacfa527…b040`(15 候选,当前发布物);U3DS = `35670269…aef6`(13 候选,行 6) | certutil 输出 |
| 启动参数解析 | `Unturned.exe -NetTransport=SteamNetworking`(工作目录 U3DS 根)——从 .lnk 二进制解析成功 | `parse-u3ds-lnk3.ps1` 输出 |
| 命令行启动 | 后台启动成功,PID 18340 | `server-stdout.log` |
| takeover 锚行 | 启动后 **10 秒**内出现 `takeover-patch result=installed` + `assembly-identity` 指纹精确匹配 | `LogOutput-boot-capture.log` L42/L45 |
| LHT 就绪 | `startupRole=DedicatedServer; clientUi=False` + 心跳 `isServer=True` 持续输出 | 同上尾部 |
| 关卡加载 | Rocket 命令注册 + Loading level 5%→26%→完成(内存 1GB) | `server-stdout.log` |
| 强杀收尾 | taskkill /F 成功,进程退出 | 本轮 bash 输出 |

## 发现(结构性,影响后续 SOP 设计)

1. **stdin 管道喂命令对原版 U3DS 控制台无效**:`echo shutdown > stdin.fifo` **永久阻塞**——Unturned 服务端控制台读输入走 Windows 控制台 API(`ReadConsole`),对管道重定向的 stdin 不消费。这解释了用户快捷方式弹真实控制台窗口的设计(手敲 `shutdown`)。**管道关服此路不通,属结构性限制,非配置问题。**
2. 干净退出自动化的候选路径(下一轮迭代验证):
   - **首选**:`GenerateConsoleCtrlEvent`(PowerShell `AttachConsole(pid)` + CTRL_BREAK)——Unturned 控制台捕获 Ctrl+C/Break 走"保存并退出",等价于手敲关服;
   - 备选:启用实例 `Config.json` 的 RCON 节(当前未启用),TCP 发 `shutdown`;
   - 备选:无重定向独立控制台窗口 + computer-use 键入(脆弱,最后手段)。
3. 本轮强制杀进程 → `LogOutput-boot-capture.log` **没有** clean-exit 锚行(`takeover-patch removed`),属预期;clean-exit 锚行验证归下一轮。
4. U3DS 部署仍是 13 代候选(`35670269…aef6`);15 候选(`cacfa527…b040`,单人范围)未部署到 U3DS——将来联机票(21/22/24)部署步骤需含双端升级。
5. PowerShell 5.1 把无 BOM 的 UTF-8 .ps1 按 ANSI 读——**脚本内禁写中文字面量**(本次踩坑:中文路径在脚本里变乱码);改用 Get-ChildItem 排除法解析。

## 留存

- `LogOutput-boot-capture.log`(BepInEx 日志,含 assembly-identity/takeover/心跳锚行)
- `server-stdout.log`(Unity stdout,含 Rocket 注册/Loading level 进度)
- `parse-u3ds-lnk.ps1` / `parse-u3ds-lnk2.ps1`(踩坑样本)/ `parse-u3ds-lnk3.ps1`(可用版)

## 下一步

1. 迭代②:实现 Ctrl-Break 干净退出(`AttachConsole`+`GenerateConsoleCtrlEvent`),验证 `takeover-patch removed` 锚行出现;
2. SOP 扩展写入 `docs/agents/real-machine-test-loop.md`(自动化节);
3. computer-use 客户端试点(启动→主菜单→单人世界→交互)——等 U3DS 链全绿后再做;
4. SOP 验证通过后挂入 16~23 实施票验收条件;P2P 双端(需 VM/第二账号)仍归人工,除非另行突破。
