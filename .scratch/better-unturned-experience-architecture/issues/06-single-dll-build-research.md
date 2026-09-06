# 选择独立功能工程合并为单 DLL 的构建方案

Type: research
Status: resolved
Author: GPT
Blocked by: GPT-05

## Question

在已确认的运行时基线上，如何让公共契约、核心运行时和多个独立功能工程保持源码隔离，同时稳定、可复现地输出一个 BepInEx 插件 DLL？比较源码编译聚合、程序集合并、内部化及其调试和兼容代价。

## Answer

采用源码级聚合：每个功能保留独立 `.csproj`、测试和模块拥有的 `.projitems`/等价 `.props` 源码清单，最终聚合工程导入同一清单并一次编译为唯一插件 DLL。最终程序集只暴露一个 BepInEx 入口；模块由核心显式注册并逐模块应用 Harmony 补丁，以保留故障隔离。

首版不采用“多个 ProjectReference 输出再合并”的正式发布链，不采用 ILMerge，也不默认采用 ILRepack/internalize。ILRepack 仅可作为未来受控实验，必须先证明 BepInEx 发现、Harmony 扫描、反射/序列化、PDB、确定性以及客户端/U3DS 兼容。

完整证据、方案比较、静态门禁与待验证边界见：`../research/06-single-dll-build.md`。

