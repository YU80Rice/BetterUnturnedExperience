# 05：DEV-16E 三环境真实运行证据与资格门禁

**What to build:** 使用 DEV-16A～D 产生的新单 DLL CandidateBuild，在单人、SteamP2PFriends Host/Client 和 U3DS Headless 中采集可追溯运行证据，并完成新的技术资格裁决。

**Blocked by:**

- 01：DEV-16A 单 DLL Runtime Composition Root 与 Client/Headless 装配
- 02：DEV-16B BUE 内置插件管理面板与设置编辑
- 03：DEV-16C 原生库存 UI 生命周期与容器上下文接线
- 04：DEV-16D 拖拽预览、真实图标、原生提交与投影收敛（✅ resolved，2026-09-02 关闭）
- DEV-16D-R13：DEV-16D Native Conformance Remediation（✅ resolved；R13-6 CLEAN 后产生新候选，R7-BAND/ROTGRAB 修复轮完成）

**Status:** claimed

> 2026-09-02 认领（agent）：DEV-16D 父工单已关闭（验收清单 9 项逐项打勾，含 AREA/装备源 Pass-Through 边界注释），阻塞解除。当前候选 = ROTGRAB 修复轮（rotgrab DLL，单人实机已确认功能正常）。

- [x] 生成新的 CandidateBuild、LoadSetIdentity、BUE 主 DLL SHA-256 和新的 CaseId；不得继承 DEV-15E 旧证据。
      —— 候选：rotgrab DLL，SHA-256 `6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`，BuildIdentity `600A6926F0AA47ADA0902AA5D64F69E2BBFE2F10FDA817F99C2FEC039FC9B26A`（编译代码验证），SourceSnapshotId `61737df`，CaseId `DEV-16E-20260902`，CandidateBuild `DEV-16E-CLEAN-20260902`。LoadSetIdentity 与证据包在人工实机证据采集后绑定。
- [x] 单人完成管理面板、设置开关、绿色/红色预览、真实图标、拖入提交和投影收敛验证。
      —— 用户 2026-09-02 实机确认"功能没什么异常"（rotgrab 修复后横/竖拿起均有渲染）；R6 诊断包 `UMM-诊断包_20260902_133050` 已留存。
- [ ] SteamP2PFriends Host 与 Client 使用同一 CaseId、同一候选身份、同一 DLL 哈希和严格重叠 UTC 时间窗，双方均完成拖入与投影验证。
- [ ] U3DS Headless 使用同一主 DLL 完成启动/运行/关闭证据，证明不创建 UI、不安装客户端 Hook、不解析客户端表现层。
- [ ] 每个案例包含环境指纹、版本、部署来源、命令/步骤、原始日志、诊断包、截图/录像引用和文件 SHA-256。
- [ ] GPT 导入证据包并得到与当前 CandidateBuild 绑定的技术资格裁决；Gemini 前端消费复核 ACCEPT。
- [ ] 人工开发者批准具体 CandidateBuild、BuildIdentity、LoadSetIdentity 和 DLL 哈希后，才允许进入发布门禁。
- [ ] 不将三环境证据误报为自动发布授权、Stable 或其它未验证功能的通过。
