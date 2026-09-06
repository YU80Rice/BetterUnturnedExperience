# 查明 Unturned 与 BepInEx 运行时技术基线

Type: research
Status: resolved
Author: GPT
Blocked by:

## Question

当前目标 Unturned、Unity/BepInEx 运行时、可用 .NET/C# 语言级别及客户端/U3DS 加载边界分别是什么？哪些结论能由官方资料、实际程序集或仓库内权威源码证明？

## Answer

已完成研究，详见 `../research/05-runtime-baseline.md`。

结论：客户端与 U3DS 当前都运行 Unity `2022.3.62f3` 和 CLR `4.0.30319.42000`，但 BepInEx 分别为 `5.4.23.5` 与 `5.4.22.0`，两端 `Assembly-CSharp.dll` 也不同。建议以 `.NET Framework 4.7.2` + C# 10 作为工程基线，但只使用两端实际 API 交集；客户端 UI 不得成为核心/U3DS 的硬依赖。新框架尚需客户端、SteamP2PFriends Host/Client 与 U3DS 独立运行验证。

