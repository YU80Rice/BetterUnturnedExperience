# GPT-Settings-Model-Authority-Spec：设置模型、持久化与权威性

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-10 决策基线；实现、编译与运行未验证**  
**输入确认:** 人工开发者于 2026-08-24 接受全部七项设置政策

## 1. 目标与边界

本规范冻结功能设置的描述、校验、权威作用域、revision、原子持久化、迁移、连接快照及统一设置外壳消费规则。各功能模块只注册设置描述、默认值、可见性和校验规则；GPT 维护的 SettingsRuntime 负责有效值、权威仲裁和持久化；Gemini 维护统一设置 UI。

本规范不冻结 Glazier 控件布局，也不冻结 LMN 能力握手与具体跨版本降级编码；后者由 GPT-11 决定。

## 2. 领域模型

### 2.1 三种设置权威

| 权威 | 含义 | 可写方 | 有效值来源 |
| --- | --- | --- | --- |
| `ClientLocal` | 只影响本机 UI、HUD、按键或视觉偏好 | 本地玩家 | 本机持久化值或默认值 |
| `ServerAuthoritative` | 影响游戏规则、权限或多人一致性 | 单人本地权威、P2P Host、U3DS | 权威端持久化值或默认值 |
| `ServerPolicyWithClientPreference` | 服务器给出允许范围，客户端在范围内保留个人偏好 | 服务器写政策；客户端写并持久化本机偏好 | `Validate(客户端偏好, 服务器政策)`；不允许静默 clamp |

模块不得定义第四种权威。客户端收到服务器快照后形成当前连接的“会话权威覆盖”；该覆盖不回写客户端持久化偏好，断线后必须删除。

`ServerPolicyWithClientPreference` 在 V1 只用于不改变服务器共享世界状态的每客户端体验偏好。服务器发送动态 `SettingPolicyView`，客户端 SettingsRuntime 用本机持久化偏好计算有效值；偏好不符合政策时使用描述器默认值作为会话有效值，并返回可见校验错误，绝不静默截断或改写持久化偏好。任何会改变服务器规则或需要服务器保存玩家选择的设置必须使用 `ServerAuthoritative`，不得滥用第三种权威。

### 2.2 描述与值

每个 `SettingDescriptor` 必须包含稳定 `FeatureId + SettingId`、本地化键、类型、默认值、权威、Schema 版本、排序、可见性、启用条件和适用校验规则。

- `Minimum/Maximum/Step` 只适用于 `Integer` 和 `Float`，以显式 `SettingValueOption.HasValue` 表示是否存在，且存在时类型必须与设置类型一致。
- `Choice` 使用非空、无重复的允许值集合；不复用 Min/Max 表示枚举。
- `KeyBinding` 使用框架规范化、平台无关的字符串编码；不得直接暴露 Unity 类型。
- `Toggle` 不得携带范围；`Text` 使用 `MaximumUtf8Bytes` 与可选 `ValidationRuleId`，模式实现保留在核心规则注册表而非线路正则表达式。
- 不适用字段必须缺省，不能以零值假装“未设置”。
- 描述器在模块启动前整体校验；任一描述器无效时拒绝该功能的设置注册，不让 UI 猜测修复。

可见性和启用条件只能引用同一功能内已声明的设置与只读能力，必须无副作用、可确定计算、无循环。不可见不等于删除：其有效值仍存在；禁用只阻止编辑，不隐式重置。

## 3. 设置快照与提交事务

### 3.1 每功能快照

SettingsRuntime 对每个 `FeatureId + SettingRevisionScope` 维护不可变 `FeatureSettingsSnapshot`：描述 Schema 版本、当前有效值集合、权限/可编辑性、单调 `Revision` 和快照来源。前端只消费快照，不持有可变权威状态。

`Revision` 以“功能 + revision 作用域”为粒度，而不是以单字段为粒度：`ClientPreference` 管理本机偏好，`ServerAuthority` 管理服务器权威值与政策。任一成功改变该作用域值集合的事务使 revision 加一；拒绝、重复请求重放和无实际变化不增加 revision。前端组合两份快照时保留两个 revision，不能伪造一个跨作用域总序号。

### 3.2 原子提交

一次 `UpdateModuleConfigCommand` 可包含同一功能、同一 `SettingRevisionScope` 的一个或多个 `SettingMutation`，禁止在一个事务中混合本地偏好与服务器权威写入：

1. 验证 Feature 正在 `Running`，发送者有权修改，`ExpectedRevision` 等于当前 revision。
2. 验证所有 SettingId 唯一、存在、类型匹配、符合描述规则及服务器政策。
3. 在副本上应用全部 mutation，并重新计算条件校验。
4. 全部成功后先把候选快照原子持久化；持久化成功后才一次替换内存快照。任一失败则保持旧内存快照和旧有效文件，整个事务拒绝，不发布部分值。
5. 成功发布一个携带完整最新快照的 changed 结果；失败发布 rejected 并携带完整当前快照。

同一连接会话内 `RequestId` 唯一且非零；观察者广播使用 `RequestId = 0`。相同 id 与相同 payload 重放原结果；相同 id 与不同 payload 以 `RequestIdConflict` 拒绝。请求关联不是认证或授权。

### 3.3 串行化与线性化点

- 每个 `FeatureId + SettingRevisionScope + AuthorityScope` 只有一个主线程事务执行器；网络 handler 只验证有界封包并入队，不直接修改设置。
- 执行器按队列顺序完成：登记 RequestId 为 `InFlight` → 读取/校验 revision → 构造候选 → 持久提交 → 发布内存快照 → 缓存终态结果 → 发布事件。
- 相同 RequestId 在 `InFlight` 时不重复执行；后到请求挂接到原事务并复用其终态结果。相同 id、不同 payload 立即拒绝。
- revision 检查和内存发布都在同一串行化边界内；两个相同期望 revision 的不同请求最多一个成功，后一个收到 revision 冲突与最新快照。
- 持久文件原子替换成功是耐久提交点；内存快照发布是进程内可见线性化点。若进程在二者之间崩溃，重启从已提交文件恢复新 revision；崩溃前未发布 changed 不构成客户端成功证据，客户端重连以完整快照收敛。

## 4. 三环境作用域

| 环境 | `ClientLocal` | 服务器权威/政策 |
| --- | --- | --- |
| 单人 | 本机用户作用域 | 当前单人世界作用域，由本地权威运行时仲裁 |
| SteamP2PFriends Host | Host 自己的客户端作用域 | Host 当前世界作用域 |
| SteamP2PFriends Client | Client 本机用户作用域；含政策型设置的本机偏好 | 消费 Host 权威值/政策快照；不能写 Host 持久化文件 |
| U3DS | 不实例化客户端 UI 设置 | 服务器实例或世界作用域；具体选择由部署 adapter 明示 |

同一描述器不能因环境而改变 `SettingAuthority` 含义。政策由服务器作用域持久化并拥有 `ServerAuthority` revision；偏好由每个客户端本机持久化并拥有 `ClientPreference` revision；由二者推导的有效值不另行持久化。若某项在当前环境不可用，通过能力/适用性投影为只读或不可见，而不是重定义权威。

## 5. 持久化与迁移

### 5.1 文件边界

- 按 `FeatureId + AuthorityScope` 独立保存，防止一个功能损坏污染其他功能。
- 文件至少含格式标识、Schema 版本、Feature 版本、持久化 revision、值集合和完整性校验信息。
- 配置根路径由部署 adapter 提供；Core 不硬编码绝对路径。
- 不持久化库存状态、连接对象、服务器会话覆盖或 UI 临时状态。

### 5.2 原子写入

在同一卷内执行：序列化到临时文件 → flush 文件数据 → 校验可重读 → 原子替换目标 → 尽力 flush 父目录元数据。任何步骤失败都保留上一份有效文件，内存事务返回持久化失败；不得宣称成功后悄悄丢盘。

### 5.3 损坏与迁移

- 启动时先校验格式、Schema、类型、值域、重复键和完整性，再构造有效快照。
- 迁移必须显式逐版本执行，每一步输入输出均重新校验；未知未来 Schema 和缺失迁移不得跳过。
- 损坏或迁移失败时，把原文件改名/复制为带时间与 DiagnosticId 的故障证据，不静默覆盖。
- 回退到当前 Schema 的安全默认值，并尝试写入新的有效文件。
- 单功能配置失败默认禁用并隔离该功能的设置启动，其他功能继续；只有配置根、原子替换机制或 SettingsRuntime 核心不变量无法保证一致性时进入 Core SafeMode。

## 6. 连接快照与同步

1. 完成能力/契约协商后，服务器对客户端可见功能发送完整权威设置快照及 revision。
2. 快照完成前，前端将服务器权威控件标记为“同步中”并只读。
3. 增量 changed 只有 revision 大于本地当前 revision 时应用；相同 revision 的同内容消息幂等忽略，revision 跳跃则请求/等待完整快照，不能猜测缺失状态。
4. revision 冲突拒绝必须返回最新完整快照，供 UI 一次性回滚。
5. 断线、换服或连接 generation 改变时立即删除旧会话覆盖与重复请求缓存。
6. V1 不离线排队服务器设置修改，也不在重连后自动重放旧请求。

具体消息集合上限、快照分片、能力不匹配行为和跨 Minor 编码由 GPT-11 冻结；在此之前不得把网络设置协议标记为 Stable。

## 7. 前端消费契约

Gemini 的统一设置外壳：

- 设置描述器由 GPT-14 的 Settings Schema fragment 与 Settings facet 提供；统一设置外壳读取静态 schema facet 和运行时完整快照。功能模块内部只通过已绑定身份的 `IScopedFeatureSettings.GetSnapshot/TryGet/Submit` 访问自身值，不存在运行时 `Describe(FeatureId)`。
- 可以在控件层做同规则预校验，但提交后只以 SettingsRuntime 的 changed/rejected 投影为最终事实。
- changed 使用完整快照刷新；rejected 使用返回快照原子回滚，显示本地化错误，不保留部分乐观值。
- `Starting/Stopping/Isolating/Isolated/Incompatible` 状态禁止提交；`Disabled` 只允许框架明确授权的启用开关。
- 不读写配置文件，不解释迁移，不显示异常堆栈、绝对路径或敏感 payload。
- UI 是否用滑块、输入框、下拉框及具体排序/分组表现属于 Gemini-02；但不得改变描述器语义。

## 8. 错误语义

设置错误至少区分：未知设置、类型不匹配、值域/规则失败、无权限、revision 冲突、RequestId 冲突、Schema 不兼容、持久化失败、迁移失败和快照未就绪。前端使用错误码映射友好文案；日志用 DiagnosticId 关联详细原因。

## 9. 测试不变量

- 三种权威在四种角色环境中的有效值来源正确。
- 描述器类型与 Min/Max/Step/Choice 适用性矩阵。
- 多字段全成功或全失败，revision 只在真实提交时增加一次。
- 重复 RequestId 幂等与冲突 payload 拒绝。
- 临时文件写入、flush、替换各阶段故障均保留旧有效文件。
- 损坏、未来 Schema、逐版本迁移失败均保留证据并安全回退。
- 乱序、重复、跳跃 revision 和断线 generation 清理。
- U3DS 不实例化 UI 类型；客户端本地偏好不被服务器快照覆盖落盘。

## 10. 后续依赖

- GPT-11：冻结能力握手、网络快照编码、集合上限、分片和降级。
- Gemini-02：基于本规范原型化统一设置外壳。
- GPT-13 已统一前端失败/超时提示，并规定 3 秒同 RequestId 重试一次、总计 8 秒后用 `RequestModuleConfigSnapshotCommand` 请求一次完整快照。
- GPT-16：以第三方参考模块验证描述器注册与持久化隔离。
