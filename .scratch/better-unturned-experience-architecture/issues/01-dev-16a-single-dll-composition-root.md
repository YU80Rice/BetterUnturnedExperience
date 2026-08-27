# 01：DEV-16A 单 DLL Runtime Composition Root 与 Client/Headless 装配

**What to build:** 让玩家部署的单一 `BetterUnturnedExperience.dll` 同时承载 BUE Core、官方 Better Item Interaction 和 ClientUi 组合根；客户端能够创建 BUE 运行时，U3DS Headless 能加载同一 DLL 但不会创建 UI 或安装客户端 Hook。

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] 主 DLL 内含官方 ClientUi 组件和 Runtime Composition Root，官方注册返回非空且有效的 ClientUi 描述。
- [x] 主 DLL 不依赖独立 Contracts/Core/ClientUi 运行时 DLL；单 DLL ABI 身份和程序集闭包门禁通过。
- [x] 客户端、BatchMode、Headless、不可用环境在 UI 工厂和原生 Hook 前完成确定性分流。
- [x] 客户端 Composition Root 可幂等初始化、销毁、SafeMode 和局部隔离，并输出结构化诊断。
- [x] U3DS Headless 加载同一主 DLL 时不创建 Glazier/Sleek/UI 对象，不安装客户端库存 Hook。
- [x] .NET Framework 4.7.2/C# 10 Release 编译达到 0 errors，尽可能 0 warnings；相关 TDD 测试、静态门禁和独立审计通过。
- [x] 不修改 U3-SDK、Unturned 原生源码或 SteamP2PFriends 源码。

## Answer

DEV-16A 已完成。主 DLL 链接编译 Contracts/Core/ClientUi Seam，官方注册提供有效 ClientUi Satellite；客户端 Composition Root 具备客户端/BatchMode/Headless/Unavailable 门禁、初始化幂等、销毁与 SafeMode 隔离。Release 全量编译为 0 errors / 0 warnings，7 项测试、静态扫描和独立审计均 PASS。

证据报告：`audit/2026-08-27/Implementation-DEV-16A-1936.md`。

DEV-16A 仍不宣称真实 Glazier/Sleek/Harmony/库存接线或三环境玩法通过；这些由 DEV-16B～D/E 继续完成。
