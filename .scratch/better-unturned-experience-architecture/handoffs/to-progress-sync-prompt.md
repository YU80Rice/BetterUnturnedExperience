# GPT → Gemini 项目进度同步 Prompt

> 作者：GPT  
> 日期：2026-08-24  
> 用途：将以下正文完整交给负责前端的 Gemini，使其读取双方现有设计、核对路径并返回前端对齐报告。

---

你是“更好的未转变者体验”项目的前端负责人 Gemini。当前处于 Wayfinder 决策阶段，只同步认知和发现差异，不实现生产代码。

## 一、先读取唯一事实源

按顺序完整阅读：

1. `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\AGENTS.md`
2. `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\CONTEXT.md`
3. `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\.scratch\better-unturned-experience-architecture\map.md`
4. 同目录 `issues\GPT-01-*.md` 至 `issues\GPT-07-*.md`
5. 同目录下三份研究：
   - `research\05-runtime-baseline.md`
   - `research\06-single-dll-build.md`
   - `research\07-inventory-authority.md`

其中 `better-unturned-experience-architecture\map.md` 是当前唯一规范决策地图。GPT 负责后端和共享契约；Gemini 负责前端。固定基础设施文件可保留固定名称，其他 Gemini 文档必须使用 `Gemini-` 前缀并在正文标注作者。

## 二、再复读你自己的前端设计路径

完整阅读你已经创建的材料：

1. `.scratch\better-un-experience-architecture\map.md`
2. `.scratch\better-un-experience-architecture\spec.md`
3. `.scratch\better-un-experience-architecture\Frontend-Architecture-Spec.md`
4. `.scratch\better-un-experience-architecture\issues\01-glazier-inventory-preview-rendering.md`
5. `.scratch\better-un-experience-architecture\issues\02-settings-and-feature-toggle-ui.md`
6. `.scratch\better-un-experience-architecture\issues\03-multi-creator-frontend-module-registration.md`
7. 演示视频：`E:\下载内容\QQ下载\Screenrecorder-2026-08-24-00-22-52-132.mp4`

这些内容是前端输入草案，不自动成为全局已批准决策。保留其中有价值的 UI 研究，但逐项与唯一决策地图及 GPT 研究证据对照。

## 三、必须核对的接口边界

逐项给出“一致 / 冲突 / 尚待决策”及证据路径：

1. 前端只负责玩家可见 UI、HUD、输入采集、预览和失败提醒；不直接修改库存权威状态。
2. 模糊落点候选、自动旋转顺序与确定性排序属于 GPT 维护的后端/共享规则；前端消费候选结果并负责呈现。
3. 提交必须复用候选 `(page, x, y, rot)`，最终仍走原版 `sendDragItem → ReceiveDragItem`，由服务器重新校验；客户端结果不具有授权性。
4. 原版 drag RPC 是 `Unreliable`，当前源码未证明存在逐次 `ItemPlacementCommittedEvent` / `ItemPlacementRejectedEvent`。你设计中的成功确认、拒绝事件和回滚动画只能作为待决策提案，不得标为既定契约。
5. `LaunchMultiplayerNet`、具体 Harmony 拦截点、红色无效预览、全局 Glazier Root 挂载点、对象池和每帧 Presenter 均尚未完成共同决策或运行验证，不得作为全局硬约束。
6. U3DS 使用无图形环境；前端程序集和 UI 类型不能成为公共契约、核心运行时或 U3DS 后端的硬依赖。
7. 统一设置外壳由 Gemini 维护；设置模型、持久化、校验和服务器权威值由 GPT 维护。具体字段等待 GPT-08、GPT-09、GPT-10 决策。
8. 单 DLL 首版采用“模块独立工程 + 模块自有共享源码清单 + 单一聚合工程一次编译”，不是运行时加载多个子 DLL。

## 四、输出一份前端对齐报告

只新建以下文件，不修改 GPT 文件和唯一决策地图：

`.scratch\better-unturned-experience-architecture\handoffs\to-progress-sync.md`

报告必须包含：

1. **已阅读清单**：逐个列出实际读取的文件。
2. **Gemini 当前设计路径**：用不超过 12 条说明前端从 UI 注入、拖拽输入、候选呈现、设置界面到模块注册的设计顺序。
3. **一致项**：与唯一决策地图一致的内容及对应路径。
4. **冲突项**：每项写明 Gemini 原设计、GPT/唯一地图约束、冲突原因和建议处理。
5. **尚待共享契约**：列出前端真正需要 GPT-08 提供的 DTO、状态、错误码和生命周期信息；不要自行定稿字段。
6. **票据依赖修正建议**：说明 Gemini-01、Gemini-02、Gemini-03 分别应被哪些 GPT 票据阻塞，以及哪些部分现在可以独立研究或做原型。
7. **文件命名检查**：指出不符合 Gemini 前缀规则的自有文档，并提出迁移方案；本轮只报告，不执行删除或覆盖。
8. **验证边界**：明确哪些只是设计、源码推断或视频观察，哪些仍需客户端、SteamP2PFriends、U3DS 的实际验证。
9. **给 GPT 的问题**：只列真正阻塞前端继续决策的问题。

## 五、完成标准

当且仅当以下条件全部满足时结束：

- 已完整阅读双方列出的材料。
- 每个接口边界均有明确判定。
- 每个冲突均有证据路径和建议。
- 没有把草案包装成已批准契约。
- 没有修改 GPT 文件、唯一决策地图或生产代码。
- 输出文件使用 `Gemini-` 前缀并标注作者 Gemini。

完成后，在回复中只提供：对齐结论、阻断问题数量、输出文件绝对路径，以及建议下一步先处理的票据名称。

---


