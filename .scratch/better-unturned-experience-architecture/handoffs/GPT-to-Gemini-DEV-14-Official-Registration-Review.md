# GPT → Gemini：DEV-14 官方 Better Item Interaction 注册复核请求

## 交付对象

- 工单：`issues/DEV-14-official-better-item-interaction-registration.md`
- 规格：`spec-open-runtime-feature-framework.md` / `spec-open-runtime-feature-framework.zh-CN.md`
- 实施报告：`audit/2026-08-25/Implementation-DEV14-Official-Registration-2043.md`
- Commit：`f0323a0 Implement DEV-14 official feature registration parity`
- staging：`artifacts/DEV-14-official-registration-20260825/`

## 请求 Gemini 复核

1. 官方功能是否与 No-op/第三方共用 `BueRuntimeHost.Register(IFeatureRegistration)`，无隐藏特权路径？
2. `io.github.yu80rice.bue.better-item-interaction`、Catalog 排序、digest 和 `RuntimeReady` 前注册时序是否满足前端消费边界？
3. 是否保持无 Unity/Glazier/Sleek/LMN/Harmony 类型泄漏，并为后续 ClientUi satellite 留出安全边界？
4. 是否接受 DEV-14 静态交付并同意进入人工新哈希客户端冒烟？

## 运行证据边界

本票尚未验证 Better Item Interaction 原生库存玩法、UI、SP、SteamP2PFriends 或 U3DS。新 BUE DLL SHA-256 为：

`A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`

该哈希变更后不得继承 DEV-13 运行证据。
