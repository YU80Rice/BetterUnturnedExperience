# 自动化实机测试 SOP(Agent 执行版,v1.1)

> **用途**:实施票交付候选后,由该票会话的主 Agent 按本 SOP 全自动执行 U3DS 实机链,产出证据包并**停等人工验收**。
> 本 SOP 是 `docs/agents/real-machine-test-loop.md` 的自动化扩展,与原人工循环并行有效;验收边界见文末。
> 来源:2026-09-07 Tier-1 干跑验证(`audit/2026-09-07/auto-dryrun/`);v1.1 干净退出经 computer-use 实证闭环。

## 适用范围(v1.1)

- ✅ **U3DS headless 链全闭环**:分离式启动 → 锚行轮询 → 行为期 → **computer-use 干净退出** → 证据包(全部已实证)
- ✅ 双端部署 + 指纹核对(客户端 + U3DS)
- ⏳ **已知缺口②**:客户端 SP 链(computer-use:启动→菜单→进世界→交互)未建,Tier-2
- ⏳ **已知缺口③**:P2P 双端(需 VM/第二 Steam 账号)未建,Tier-3,暂归人工

## 前置事实(机器环境,已实测)

| 项 | 值 |
|---|---|
| 客户端 | `E:\Steam\steamapps\common\Unturned`(插件目录 `BepInEx\plugins\`) |
| 专用服务器 | `E:\Steam\steamapps\common\U3DS`(插件目录 `BepInEx\plugins\`) |
| U3DS 启动命令 | 工作目录=U3DS 根,`./Unturned.exe -NetTransport=SteamNetworking`(来自用户快捷方式解析) |
| U3DS 测试实例 | `Servers/U3DS_Coop`(Rocket 在装;`Default` 为空壳) |
| BepInEx cfg | `[Logging.Disk] LogLevels = All` 已开(Debug 锚行直进 LogOutput.log) |
| Steam | `E:\Steam\steam.exe`,登录态由用户维持 |
| 指纹工具 | `certutil -hashfile <dll> SHA256` |

## 执行序列(不可跳步、不可调换)

0. **读票**:提取本票专属锚行清单(验收条件里定义的 feature 锚行)+ 读 `audit/RELEASES.md` 确定候选与 sha256。
1. **部署**:候选 DLL → 目标端 `plugins\`;`certutil` 双端指纹与 RELEASES 比对——**不一致即停**,按 real-machine-test-loop 处理。
2. **cfg 检查**:`LogLevels = All` 在位。
3. **启动 U3DS(v1.1:分离式,自带控制台窗口)**——**禁止管道重定向 stdin**(结构性不可行,见坑①):
   ```powershell
   # launch-detached.ps1(ASCII-only)
   $p = Start-Process -FilePath 'E:\Steam\steamapps\common\U3DS\Unturned.exe' -ArgumentList '-NetTransport=SteamNetworking' -WorkingDirectory 'E:\Steam\steamapps\common\U3DS' -PassThru
   Write-Output ('PID=' + $p.Id)
   ```
   控制台窗口以 **Windows Terminal 标签页**形态弹出(标题"Unturned")——这是关服的输入面,别关它。
4. **轮询锚行**(≤240s,每 10s):`takeover-patch result=installed` + `assembly-identity sha256=` 与候选精确匹配 + 本票专属锚行;**注意分界**——插件锚行在 `BepInEx/LogOutput.log`,引擎行(`Loading level` 等)在**控制台窗口**(v1.1 无 stdout 重定向;如需引擎行证据用 computer-use 截图窗口)。
5. **行为期**:按本票验收条件执行动作/维持时长(如广播锚、心跳计数)。
6. **采集**:`cp` `BepInEx/LogOutput.log` → `audit/<date>/<ticket>/auto-evidence/`(引擎行证据用 computer-use 截图窗口归档)。
7. **干净退出(v1.1,computer-use 已实证)**:
   ```text
   ① computer-use:激活 Unturned 控制台窗口(open_application pid→WindowsTerminal,activate=true)
   ② type "shutdown"(此时该窗口必须 frontmost,否则前置检查会拒绝——被抢焦点就再 activate)
   ③ key "return"
   ④ 等 ~15s:进程退出 + LogOutput 出现 takeover-patch result=removed decision=hand-back-to-lmn = 干净退出成功
   ```
   兜底:`taskkill //F //IM Unturned.exe`(记录缺 clean-exit 行属预期)。**管道喂命令(ReadConsole vs pipe)已实证不可行,勿再试。**
8. **断言清单**:写 `auto-evidence-notes.md` 表格(锚行/期望/实测/结论),逐项核对;任何 FAIL → 修复流程。
9. **回填**:票面 Comments 登记「自动实机证据包已产出(路径),停等人工验收」+ 给用户一句话验收指引(看哪些锚行/行为)。
10. **边界**:本 SOP **不 resolved、不关票、不动 RELEASES 的批准态**——人工验收通过后由用户(或用户明确授权的会话)收尾。

## 已知坑(必读,全部踩过)

- **服务器控制台读输入走 Windows 控制台 API(`ReadConsole`),管道重定向 stdin 不消费**——`echo shutdown > fifo` 永久阻塞(干跑实证)。干净退出唯一正道 = 真实控制台窗口键入(computer-use)或 RCON。
- 本机控制台宿主 = **Windows Terminal**(Start-Process 弹出的控制台窗口是它的标签页);聚焦窗口用 `open_application(pid, activate=true)`,焦点被抢就重新 activate。
- Bash `&` 链会把 `cd` 一起后台化 → 先单独 `cd`(cwd 跨工具调用持久)。
- PowerShell 5.1 把无 BOM UTF-8 .ps1 按 ANSI 读 → **脚本禁写中文字面量**,中文路径用 `Get-ChildItem` 排除法解析。
- `taskkill` 在 Git Bash 用双斜杠:`taskkill //F //IM Unturned.exe`。
- BepInEx `LogOutput.log` 覆盖式写入(每次启动重置)——启动前记录基线,采集取本轮。
- 多日志会话归属用文件 birth time,横幅时间戳不可信。

## 验收边界

本 SOP 产出 = **证据包**;「发布授权」「工单关闭」仍为用户人工裁决(CONTEXT「发布授权」「技术资格裁决」)。自动化消除的是采集与断言的人力,不是验收责任。
