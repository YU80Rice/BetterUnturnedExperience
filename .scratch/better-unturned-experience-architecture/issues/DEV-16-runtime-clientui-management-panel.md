# DEV-16：真实运行时 ClientUi、原生库存接线与 BUE 管理面板

Type: task
Status: closed（2026-09-03 V1 闭环收尾：A~G 全阶段完成并验收，父工单关闭）
Owner: GPT（后端、主插件运行时与共享契约） / Gemini（前端表现层与消费复核）
Depends on: DEV-15A, DEV-15B, DEV-15C, DEV-15D, DEV-15E（纯 C# Seam 已完成；真实运行证据不继承）
Spec: `../spec-DEV-16-runtime-clientui-management-panel.md`

## 目标

把当前只有 Bootstrap/注册骨架的 BUE 主 DLL 推进为真实单 DLL 运行时：内化 `UnturnedPluginManager` 的管理面板能力，接入真实 ClientUi、Unturned 原生库存 UI/拖拽链和 BUE 设置，同时保持 U3DS Headless 安全隔离。

## 实施阶段

- DEV-16A：主 DLL 装配与 Runtime Composition Root；
- DEV-16B：BUE 管理面板、收藏/排序和 ConfigEntry 兼容设置编辑；
- DEV-16C：原生库存 UI 生命周期、容器上下文和受控 Hook；
- DEV-16D：拖拽预览、真实图标、原生提交和投影收敛；
- DEV-16E：单人、SteamP2PFriends Host/Client、U3DS Headless 实机证据与资格门禁。

## 验收摘要

- 玩家发布物仅为 `BetterUnturnedExperience.dll`；
- 主菜单和暂停菜单均可打开 BUE 管理面板；
- BUE 功能和已加载 BepInEx 插件可见，收藏/A-Z/Z-A 偏好持久化；
- Better Item Interaction 设置可见可编辑；
- 绿色/红色占据框、真实浮动物品图标、原生拖入和投影收敛可运行；
- 失败时功能局部隔离并保持原生回退；
- U3DS 不创建 UI、不安装客户端 Hook；
- Release 编译、测试、独立审计和 Gemini 前端复核通过；
- 新 DLL 使用新的 CandidateBuild、LoadSetIdentity、哈希和 CaseId。

## 边界

不修改 U3-SDK、Unturned 原生源码或 SteamP2PFriends 源码；不建立平行库存 RPC；不实现拖出地面、自动整理、自动交换、批量移动、运行时插件卸载/热重载或任意 DLL 扫描。

## Comments

- 2026-08-27：DEV-16 规格经人工开发者确认，已完成 `ready-for-agent` 发布；下一步为 `/to-tickets` 拆票。
- 2026-09-03（V1 闭环收尾）：A~G 全阶段完成并验收：
  - DEV-16A 单 DLL Composition Root（resolved）；DEV-16B/C/D 管理面板/生命周期/拖拽（resolved）；DEV-16D-R13 系列（native conformance + edge-fit，resolved）。
  - DEV-16E 三环境资格（单人 + SteamP2P Host/Client + U3DS Headless）证据采集与人工批准（resolved，证据见 `audit/2026-09-02/`）。
  - DEV-16F 拿起源解耦 + 目标页扩展（closed 2026-09-02）；DEV-16G 日志规范化 A/B/C/D（closed 2026-09-03）。
  - 父工单随 V1 整体收尾关闭；发布/Stable 授权未授予。
