# DEV-13 调试版人工双 DLL 冒烟复核报告

## 判定

**PASS：调试版真实客户端注册屏障门禁通过；正式清理版仍待重新采集证据。**

## 证据

- 诊断包：`UMM-诊断包_20260825_201336`
- 实际 BUE DLL：`F799EA4F429B90CC4231BC6F6798D5A9842B9352F53BB1EA082B1F9F248AC422`
- 实际 No-op DLL：`CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02`
- BepInEx 2 plugins to load：BUE + No-op
- No-op：`accepted=True reason=None diagnosticId=BUE-REG-ACCEPT`
- BUE：`sceneLoaded` → `TryCompleteRuntime runtime=bound phase=RegistrationOpen`
- BUE：`status=RuntimeReady diagnosticId=BUE-BOOTSTRAP-003`
- UMM 摘要：`Normal`，退出码 `0`
- 目标异常扫描：无 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException` 或 BepInEx Error/Fatal/Exception

## 代码清理与回归

- `[DEBUG-DEV13]` 临时 instrumentation 已从源码移除。
- 正式实现保留 `SceneManager.sceneLoaded` Host barrier，并在成功后退订；`Start/Update` 作为兼容路径保留。
- 清理后 Release：0 errors / 0 warnings。
- 7/7 测试：PASS。

由于移除临时 instrumentation 后正式 BUE DLL 哈希变为 `166B6C488F609BA933A02BB62432FD6079352B05CBD9F0FD40B6ABADF1DC8B32`，依据变更后必须重新绑定运行证据的门禁规则，本报告不得作为正式 DLL 的运行 PASS。

## 边界

本报告只关闭 BUE + No-op 独立注册、Catalog barrier 和客户端 BepInEx 运行门禁；不宣称 U3DS、SP/P2P、ClientUi Satellite、Better Item Interaction 或三环境发布资格通过。
