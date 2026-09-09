# DEV-V2-24 u3ds case·客户端(v7,2026-09-09 13:00,用户实测「四功能也无异常」)
- 诊断包 = UMM-诊断包_20260909_130038(本机客户端,直连 127.0.0.1:27015);sha256=b03a0067…cc7fbd;身份 = A1B339BF…71359(v7)✓(:136)
- **F-E 占位路径客户端实锤(本票核心验证点)**:`armed role=client localSteamId=76561199030780228`(:667)——直连 FakeIP 服务器(Provider.server=0/假身,litfb9 实证)下 ClientPeerDecision 仍产出非零对端键=占位收敛,arm 成功;**v6 轮此处恒零武装、四功能全死,本轮全活=F-E 修复实机验证成立**
- 会话:已接收并应用服务端会话 challenge(generation=2)(:763)——占位键两侧配对、握手完成的直接证据
- **F-A 网络整理 ×8**:RequestTidy(reqId=1..8, page=2, SameType)→快捷键已恢复并验证 ×8
- **U3 LIR 客机实弹 ✓**:[RepackB] `-> 服务器: RequestRepackAmmo(reqId=…482/…483)` ×2;客户端 dispatcher summary dispatches=1(:1352)=…482 成功回包 drain→toast「一键压弹:成功压入 26 发子弹」;…483=服务端已执行弹匣已满静默 no-op(见服务端 case),用户面正确
- **U4 LHT ✓**:HUD ISleekLabel 已注入 PlayerLifeUI.container(800x35 @ MiddleCenter)(:751);收到 Update seq=1..7(remaining 100→98 递减)零重复键(P6)
- BII:BUE-DRAG ×168 存活;environmentRole=Client scenario=RemoteClient(面板场景正确)
- 零错误:客户端 BUE Error 级 0 条;P2P 客机双击换弹(P3 严格口径)仍待 Steam-P2P 环境补采
- 完整日志=logoutput-v7-u3ds-client-20260909-130038.log
