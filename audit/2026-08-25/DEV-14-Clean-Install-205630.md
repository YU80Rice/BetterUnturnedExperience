# DEV-14 官方 Better Item Interaction 注册与准入最终运行审计

## 一、判定

**PASS；DEV-14 正式关闭。**

本轮绑定的是 DEV-14 新构建哈希，未继承 DEV-13 旧运行证据。

## 二、证据

- 诊断包：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260825_205630`
- BepInEx：`5.4.23.5`
- 部署目录：`E:\Steam\steamapps\common\Unturned\BepInEx\plugins\`
- UMM 摘要：`Normal`，退出码 `0`

## 三、双向哈希核验

| DLL | staging SHA-256 | 实际部署 SHA-256 | 判定 |
|---|---|---|---|
| `BetterUnturnedExperience.dll` | `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16` | `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16` | PASS |
| `BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` | PASS |

## 四、运行时调用链核验

`LogOutput.log` 完整出现：

1. BUE `status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001`
2. 官方 Better Item Interaction `accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`
3. No-op Fixture `accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`
4. BUE `status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003`

这证明官方功能和外部 Fixture 均在注册窗口通过公开 Host Bridge 登记，并由 BUE Host barrier 进入 `RuntimeReady`。

## 五、异常与边界

- 未发现 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException`、Unhandled/Fatal BepInEx 错误。
- UMM 诊断分类为 `Normal`。
- `Client.log` 中的 Elver `Metro_Oven.dat` 缺少原版 GameObject 是既有 Workshop 内容异常，不属于 BUE/BepInEx 加载链；BepInEx 日志未将其报告为插件错误。

## 六、独立审计结论

GPT 独立核验与 Gemini `ACCEPT` 均通过。DEV-14「官方 Better Item Interaction 公开注册与准入 tracer bullet」正式关闭。

本结论仅覆盖官方注册平权、公开 ABI、Catalog barrier 和客户端 BepInEx 双 DLL 运行；不代表 Better Item Interaction 的实际拖拽/UI/原生库存玩法、SP、SteamP2PFriends、U3DS 或三环境发布资格已通过。
