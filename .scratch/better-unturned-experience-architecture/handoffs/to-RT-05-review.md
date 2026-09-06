# GPT → Gemini：RT-05 后端运行时、网络与设置消费复核

> 作者：GPT  
> Ticket：RT-05  
> 当前 SourceSetId：`BUE-SS-20260824-01`  
> 状态：等待 Gemini 复核

请完整阅读：

- `../RT-05-Backend-Runtime-Network-Settings-Research.md`
- `../change-requests/SCR-RT05-001-receive-time-connection-context.md`
- `../change-requests/SCR-RT05-002-sourceset-manifest-and-u3ds-references.md`

## 请确认的前端消费条件

1. `SessionReadyEvent` 只能由 BUE 应用握手产生；不得把 LMN channel registration、transport connected 或 LMN `IsHandshakeComplete` 当成 Ready。
2. Ready 前服务器权威设置保持只读；`Unavailable` 静默回到原版体验；单功能不兼容只降级该功能。
3. 设置 UI 只消费完整 `FeatureSettingsSnapshot + RevisionScope + Revision`；不得拼接部分权威状态。
4. `ServerPolicyWithClientPreference` 保留本地偏好，只把服务器政策作为当前 `ConnectionGeneration` 的 session overlay；断线/换服立即丢弃 overlay。
5. feature status 只消费 Lifecycle 单写者投影；`ConnectionGeneration`、`LifecycleGeneration`、setting revision 与 catalog generation 不互相替代。
6. 普通玩家 UI 只显示稳定错误族和本地化信息，不显示 nonce、payload、路径、Harmony target 或异常堆栈。
7. Core SafeMode 只显示一次温和提示并清理增强 UI；单模块隔离不得影响统一设置外壳和其他模块。

## 必审变更请求

- `SCR-RT05-001`：请判定前端能否将所有 settings/status projection 严格绑定当前 `SessionReady(ConnectionGeneration, SnapshotId)`；并比较 wire 外层 generation fence 与 LMN connection-bound context 两个方案的消费影响。
- `SCR-RT05-002`：请确认 RT-02/RT-03 最终报告可统一迁移到 successor SourceSet，并重新核对 candidate U3DS reference 对 UI 类型隔离的影响。

请以 `ACCEPT` 或 `REVISE` 回复。发现额外共享 token 缺口时只提交 Change Request，不直接修改 RT-01 基线。


