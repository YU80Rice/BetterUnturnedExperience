# DEV-12 人工 clean-install 单 DLL 验证报告

## 一、验证目的

验证 `DEV-12-single-dll-runtime-assembly-closure` 的真实客户端门禁：BepInEx 插件目录仅部署 `BetterUnturnedExperience.dll` 时，BUE 能够启动并输出 `BootstrapReady`，且不依赖旧的 Core/Contracts DLL。

## 二、证据与哈希

| 项目 | 事实 |
| :--- | :--- |
| 部署目录 | `E:\Steam\steamapps\common\Unturned\BepInEx\plugins` |
| 实际 BUE DLL | `BetterUnturnedExperience.dll`，86,016 bytes |
| BUE DLL SHA-256 | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` |
| Core DLL | 不存在 |
| Contracts DLL | 不存在 |
| UMM 诊断包 | `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\启动器\UnturnedModManager\publish\UMM-v2.2.0-win-x64\UMM-诊断包_20260825_190429` |
| `LogOutput.log` SHA-256 | `5F0FA2E7C77ACD29478CE54ED84F7849E3A39FF2D60D5F2D1371A9A2F86EA839` |
| `UMM-诊断摘要.txt` SHA-256 | `75BDC1879DB32DD74A5FEA3FF7D98A2EAB87413667EB9619F5E5B6B32EEF54` |

## 三、运行核对

- BepInEx 发现并加载 `Better Unturned Experience 0.0.0`。
- BUE 输出：`featureId=io.github.yu80rice.betterunturnedexperience status=BootstrapReady decision=Client diagnosticId=BUE-BOOTSTRAP-001`。
- 对 BepInEx `LogOutput.log` 扫描 `TypeLoadException`、`FileNotFoundException`、`MissingMethodException`、`[Error]`、`[Fatal]`、`Exception`：无命中。
- UMM 诊断摘要结论为 `Normal`，受管会话退出码为 `0`。
- 其他 `Client.log` 中的 Workshop 资源告警不属于 BUE 单 DLL 启动链，不改变本票判定。

## 四、独立审计与判定

- GPT 静态独立审计 R2：PASS。
- Gemini 前端/公开 ABI/Headless 复核：ACCEPT。
- 人工 clean-install 单 DLL 冒烟：PASS。

最终判定：**PASS，DEV-12 可标记 `resolved`。**

## 五、边界声明

本报告只关闭单 DLL 物理程序集闭包与客户端 BepInEx 启动门禁，不宣称 U3DS、单人、SteamP2PFriends Host/Client、No-op Fixture 联合部署、Better Item Interaction 或三环境发布资格通过。
