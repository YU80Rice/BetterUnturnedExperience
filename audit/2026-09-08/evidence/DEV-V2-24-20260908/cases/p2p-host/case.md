> 【已归档·待重采(v6 时点)】本文件既有证据为历史轮所采(时点注记见下,哈希均属作废前身);重采一律绑候选 v6=626BC330236481AA8EF357579D9B685E7AA2472C25E6CE4B19511F666BCD2715(544256B,CaseId DEV-V2-24-CANDIDATE-20260909),全量重采后本文件整体替换。
# case: p2p-host（SteamP2P 主机端）— CaseId `DEV-V2-24-20260908`

> 【已归档·待重采】本 case 绑定作废候选 3cbd6268…9e4d;已采 v2 候选,v3 出后全量重采替换(本 case 含 F-A/F-B 缺陷记录,保留为立案证据)(新候选 = 2d3de91d…762e1)。
> 对应手册 §4（配置 A）。采集日期 2026-09-08;UMM 诊断包 `UMM-诊断包_20260908_165949`。
> **本 case 结果 = 部分 FAIL（LIT 两项真机缺陷 F-A/F-B,整理停止采集)**;LIR/LHT 项通过。

- collector: 用户（agent 代部署+复核锚行）
- gameVersion / bepInExVersion: 3.26.3.11 / 5.4.23.5（同 sp 会话期）
- startedUtc / endedUtc: TODO（本机 Host 侧会话,时间窗与 Client 正交重叠;UMM 包时间 2026-09-08 16:59:49）
- 部署配置: 配置 A（仅候选 BUE + 休眠 SPF）,身份绑定行 :136 = `3CBD6268…9E4D` ✓（本文件 LogOutput.log SHA-256 `2ef14426…b510`）

## 锚行摘录（本机 Host 侧）

| 步骤 | 结果 | 证据 |
|---|---|---|
| P1 启动锚 | 通过 | :180 LIT 频道注册 / :179 补丁安装 / :181 模块启动(代际=4);REG-ACCEPT×5 与 sp 同构 |
| P2 LIT 跨端整理 | **FAIL（F-A:客机整理全程不可用)** | Host :2889 `[TidyFault] peer scope 已开启(peer=76561199721762479, generation=2)` → :2890 `[Error] WARN [TidyNet] 定向发送未送达（generation=2, result=LocalTransportUnavailable）`——**挑战签发定向发送失败且全程无重试**(全日志恰此一条发送失败);Client 侧 80 次「尚未收到有效服务端 session challenge」拒绝(:691-:1084),整理请求从未发出。断线清理正常(:6447 会话代际已清) |
| P2' Host 本地整理 | **FAIL（F-B:幽灵贴图堆叠)** | Host 点击整理 :5134-:5137 本地路径提交成功(`placed=3 指纹守恒验证通过`+`本地整理已提交(page=2, mode=SameType, mappings=3)`),但 UI 出现多容器幽灵贴图堆叠(截图 `screenshots/host-ghost-stacking-after-tidy.png`,SHA-256 `5bbb130e…efeea`);SP 环境(见 cases/sp)同路径无此现象 |
| P3 LIR 跨端压弹 | 通过 | Host :4214/:4369/:4400/:4475/:4558/:4584 dispatcher summaries(dispatches≥1,rejected=2 冷却);:4610 `-> 客机 RepackSuccess(reqId=…222, total=10)`——**同会话定向发送成功**,反证 F-A 的发送失败是暂时性/状态性而非传输持续故障 |
| P4 LHT 信标广播 | 通过（分离采集面） | :5150+ `广播 Update: epoch=1 seq=7/8 remaining=100/100 result=Sent`(组播含客机会话);HUD 客户端侧用户确认无异常 |
| P5-P7 | 见 Client case | — |
| 防双装基线 | 通过 | 全程零 `BUE-PLATFORM-001` 行 |
| 零误报 | BUE 侧零故障 | 全部 Error 行均属 SteamP2PFriends 旧插件(SPF 资源观察),非 BUE;BUE 唯一 Error = :2890(即 F-A 本体) |

## 结论

- LIR / LHT 两件三环境口径下 P2P 环节**通过**;LIT 两项**真机缺陷立案**:
  - **F-A**:LIT 服务端挑战签发定向发送失败后无重试/无恢复(候选 `3cbd6268…9e4d`,GenerationChanged/首连采纳即拍发送,失败即终局)——客机整理全程锁死;
  - **F-B**:主机本地整理提交成功但 UI 幽灵贴图堆叠(SP 无、P2P 主机有,提交后界面刷新/预览清理路径差异待查)。
- 按 real-machine-test-loop 处置:停止采集→修复轮(红测先行+双轴 CLEAN)→新候选→换绑全量重采。
