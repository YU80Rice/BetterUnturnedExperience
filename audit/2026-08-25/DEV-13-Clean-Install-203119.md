# DEV-13 正式清理版人工双 DLL 冒烟与独立审计报告

## 一、判定

**PASS；DEV-13 可标记 `resolved`。**

本轮证据绑定的是移除调试 instrumentation 后的正式清理版 DLL，未继承此前调试版运行证据。

## 二、证据范围

- 诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260825_203119`
- 部署目录：`E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`
- BepInEx：`5.4.23.5`
- BUE：`BetterUnturnedExperience.dll`
- No-op：`BetterUnturnedExperience.NoOpFixture.dll`

## 三、哈希核验

| 产物 | 预期 SHA-256 | 实际 SHA-256 | 判定 |
|---|---|---|---|
| BetterUnturnedExperience.dll | `166B6C488F609BA933A02BB62432FD6079352B05CBD9F0FD40B6ABADF1DC8B32` | `166B6C488F609BA933A02BB62432FD6079352B05CBD9F0FD40B6ABADF1DC8B32` | PASS |
| BetterUnturnedExperience.NoOpFixture.dll | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | PASS |

核验命令：

```powershell
$d='E:\Steam\steamapps\common\Unturned\BepInEx\plugins'
Get-FileHash "$d\BetterUnturnedExperience.dll" -Algorithm SHA256
Get-FileHash "$d\BetterUnturnedExperience.NoOpFixture.dll" -Algorithm SHA256
```

## 四、运行时门禁核验

`LogOutput.log` 给出完整时序：

1. `BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001`
2. `BUE no-op fixture ... accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`
3. `RuntimeReady diagnosticId=BUE-BOOTSTRAP-003`

这证明 BUE Host 先开启注册阶段，No-op 在公开 Host Bridge 注册成功，随后 Host-owned barrier 冻结 Catalog 并进入 `RuntimeReady`。

UMM 诊断摘要为 `Normal`，受管会话退出码为 `0`。本轮日志未命中 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException`、Fatal 或 Unhandled BepInEx 错误。

## 五、独立审计

| 审计项 | 结果 | 证据 |
|---|---|---|
| 正式 DLL 哈希绑定 | PASS | 两枚实际部署文件与正式产物哈希完全一致 |
| BepInEx 加载 | PASS | 日志显示 `2 plugins to load` 且 BUE/No-op 均加载 |
| No-op 公开注册 | PASS | `accepted=True reason=None diagnosticId=BUE-REG-ACCEPT` |
| Catalog barrier | PASS | `status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003` |
| 异常/ABI 回归 | PASS | 目标异常模式未命中，UMM `Normal`/退出码 `0` |
| 调试日志清理 | PASS | 正式运行日志无 `[DEBUG-DEV13]`；源码扫描仅命中历史票据说明 |

## 六、最终结论与边界

DEV-13「独立 No-op Feature 注册运行时与 Catalog Barrier 冒烟」正式通过并关闭。该结论仅覆盖正式清理版客户端 BepInEx 双 DLL 注册与 barrier 运行证据；不代表 U3DS、单人、SteamP2PFriends P2P、ClientUi Satellite、Better Item Interaction 或三环境发布资格已通过。
