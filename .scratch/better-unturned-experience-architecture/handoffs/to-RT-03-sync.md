> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini → GPT：RT-03 前端设置 UI、生命周期与 Headless 隔离同步报告（修订版）

> **作者**: Gemini（前端负责人）  
> **接收方**: GPT（生命周期与设置契约强制 Reviewer）  
> **适用任务**: RT-03  
> **SourceSet 身份**: `BUE-SS-20260824-01`  
> **状态**: 已吸收 GPT 复核意见完成 B-03、B-04 及 N-01～N-04 修订，票据状态暂设为 `ready-for-human`  

---

## 1. 阻断项与建议修订对账（B-03, B-04, N-01~N-04）

1. **B-03（Core SafeMode 表现纠偏）**：
   - 彻底删除“设置模态全功能只读并显示横条”的表述。
   - 明确 Core SafeMode 触发时**卸载全部自定义功能 UI**；若独立的最小安全 Fallback Adapter 可用，显示一次温和提示（带 `DiagnosticId`）；否则仅记入日志。绝不假定设置窗口本身可用，Unturned 原生游戏与暂停菜单 100% 保持可用。
2. **B-04（Headless 五重强隔离与证据等级）**：
   - 降级“完全保证”措辞为“五重结构设计义务与待验证架构假设”。
   - 将装配门禁固化为 `ClientUiAvailable && !Application.isBatchMode && !Headless`；明确 Core 零 UI Token、显式注册表、CI IL 可达性扫描与真实 U3DS 运行是独立门禁。
3. **N-01（证据记录格式补全）**：
   - 补充完善了报告级 Evidence Manifest 元数据（`FileIdentity`, `EnvironmentRole`, `EnvironmentLimits`, `CapturedBy/At`）。
4. **N-02（术语修正）**：
   - 将“双事实源”全面纠正为“静态 Schema（Settings Facet） + 动态值投影（`FeatureSettingsSnapshot`）”。
5. **N-03（受控私有字段注入）**：
   - 明确设置按钮注入通过受控的私有字段 Accessor 在 `PlayerPauseUI` 与 `MenuConfigurationUI` 构造期注入，绝不使用反射扫描程序集。
6. **N-04（传输解耦下的设置行为）**：
   - 补充引用 RT-05 结论：LMN 不可用时不影响纯本地设置与本地功能；联网权威设置置灰为只读/Unavailable，并展示最后确认快照；在 `SCR-RT05-001` 解决前禁止开放生产级网络设置写入。

---

## 2. 产物路径

- **完整调研报告（修订版）**: [`RT-03-Frontend-Settings-Lifecycle-Headless-Research.md`](../RT-03-Frontend-Settings-Lifecycle-Headless-Research.md)
- **已更新票据**: [`issues/RT-03-frontend-settings-lifecycle-headless-research.md`](../issues/RT-03-frontend-settings-lifecycle-headless-research.md)


