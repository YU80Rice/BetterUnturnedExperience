# GPT-11 网络能力决策独立审核报告（第 1 轮）

## 【需求执行概述】

冻结双层握手、能力降级、原版连接保留、服务器权威、分片和状态投影；当前仅为 Wayfinder 规划。

## 【编译验证记录】

当前仓库没有生产工程或可执行构建命令。本轮只修改 Markdown 决策文档，不构成实现、编译或运行 PASS。

## 【子智能体审核记录】

- 审核员：`audit_gpt11_network`
- 判定：FAIL
- 阻断项：
  1. GPT-11 工单仍为 claimed，但地图和交接提前宣称完成。
  2. `UpdateModuleConfigCommand.Mutations` 上限未明确冻结。
  3. 线路使用 `System.Version`，缺少确定性组件宽度和编码。

## 【修复动作】

- 审计期间从地图 Decisions so far 暂时撤下 GPT-11，并把交接标记为候选。
- 冻结单命令最多 64 个 mutation、每功能快照最多 256 项。
- 新增固定 `WireSemanticVersion(u16 Major/Minor/Patch)`，禁止直接序列化 `System.Version`。
- 增加固定格式 `HandshakeReject` 与消息/频道唯一映射。

## 【本轮结论】

FAIL 已保留；修复后必须重新独立审核。

## 【第 2 轮复审追加】

- 判定：FAIL
- 剩余阻断：Major 拒绝帧尚未脱离普通 Contract envelope；`0x0001/0x0002` 无法判别裸 DTO 与分片 envelope。
- 修复：冻结独立 `BUEB` bootstrap reject 二进制帧；规定 Hello/Snapshot 单片和多片始终使用 `SnapshotChunkEnvelope`，同一 Hello 重试复用 SnapshotId。

## 【第 3 轮复审追加】

- 判定：FAIL
- 剩余阻断：频道映射仍残留普通 `0x0005`。
- 修复：映射改为普通 `0x0001–0x0004` 加独立 `BUEB/bootstrapKind=1`；普通 `0x0005` 保留禁发，并永久保留会与 BUEB 魔数碰撞的 Contract 版本字节组合。
