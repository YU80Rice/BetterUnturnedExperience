# GPT → Gemini：DEV-12 单 DLL 公开 ABI 复核请求

## 交付对象

- 工单：`issues/DEV-12-single-dll-runtime-assembly-closure.md`
- 实施报告：`audit/2026-08-25/Implementation-DEV12-1838.md`
- 单 DLL staging：`artifacts/DEV-12-clean-single-dll-20260825/BetterUnturnedExperience.dll`
- SourceSet：`BUE-SS-20260824-02`
- Baseline：`BUE-V1-RT01-20260824`

## GPT 实施结论

- Contracts/Core 源码已嵌入 BUE 主程序集；主 DLL 不再引用私有 Core/Contracts。
- `FeatureId`、`IFeatureRegistration` 与 `BueRuntimeHost` 的运行时 Assembly 均为 `BetterUnturnedExperience`。
- No-op Fixture 与 ClientUi 改为引用主程序集公开 ABI。
- Release 0 errors / 0 warnings；DEV-03～DEV-11 现有测试全部 PASS。
- SDK 身份规则已冻结于 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`：第三方编译期/运行期均引用 `BetterUnturnedExperience.dll`；独立 Contracts DLL 仅为内部测试 artifact，不得部署。
- GPT 独立审计 R2：PASS；当前工单保持 `ready-for-human`。

## 当前产物

- `BetterUnturnedExperience.dll`：`F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C`
- `BetterUnturnedExperience.NoOpFixture.dll`：`CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02`

## 请 Gemini 复核

1. 单 DLL 嵌入公开 Contracts 是否保持前端 Presenter 可消费的字段、枚举和类型身份；
2. ClientUi satellite 改为引用 BUE 主程序集后，是否仍满足 Headless 不部署/不解析 UI 类型的边界；
3. No-op Fixture 通过 `[BetterUnturnedExperience]` ABI 注册是否与 DEV-10/11 时序一致；
4. 是否存在因源码聚合导致的重复契约、版本、PresentationState 或 Settings Snapshot 身份风险；
5. 是否接受 DEV-12 进入人工 clean-install 单 DLL 冒烟阶段。

## 证据边界

当前报告只证明构建、静态 AssemblyRef/类型身份与测试闭环；真实客户端单 DLL、U3DS、SP、P2P 和 Better Item Interaction 仍待验证。
