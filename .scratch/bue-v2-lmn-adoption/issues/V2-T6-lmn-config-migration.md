# LMN 配置迁移映射

Type: wayfinder:research
Status: claimed（2026-09-03 本会话认领，research 子代理已触发）
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: V2-T5-lmn-coexistence-takeover（已 resolved；接管机制定了才能谈迁移时序）

## Question

BUE 接管独立 LMN 时，哪些 LMN 配置键迁移到 BUE 设置？如何保留有效值、可回滚、可诊断？

## 查证要点

1. 独立 LMN 的配置文件位置、格式、键集合（本地 `LaunchMultiplayerNet` 源码/config 静态确认）。
2. 迁移映射：哪些键与 BUE 设置模型（Settings Facet / 原子 revision）对应，哪些不可迁移（保留原文件）。
3. 回滚语义：迁移失败或用户回退时如何恢复原配置，不破坏旧配置文件（CONTEXT.md L77-79）。
4. 面板呈现：迁移状态/成功/失败在 BUE 管理面板的展示方式。
5. 证据：配置文件样例 + 源码引用 + 路径。

## 答案

（resolved 时记录映射表 + 回滚方案）
