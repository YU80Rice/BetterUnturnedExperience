# DEV-15E-HUMAN：Better Item Interaction 真实运行证据采集

Type: task
Status: ready-for-human
Owner: 人工开发者（运行环境采集）
Reviewer: GPT（证据包校验与资格裁决） / Gemini（前端消费复核）
Depends on: DEV-15E
Candidate DLL: `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll`
Candidate DLL SHA-256: `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16`

## 目标

使用同一 Candidate DLL、同一 BUE 版本与新的 CaseId，分别采集：

1. 单人（SinglePlayer）；
2. SteamP2PFriends Host；
3. SteamP2PFriends Client；
4. U3DS Headless。

证据必须包含原始日志/诊断包、环境指纹、部署来源、精确 UTC 时间窗、执行命令/步骤和
当前 DLL SHA-256。P2P Host 与 Client 必须共享同一 CaseId，且时间窗存在严格正交重叠。

## 部署锁定

- 只部署上面列出的 `BetterUnturnedExperience.dll`；不要混用旧 artifacts 目录中的 DLL；
- 每个环境启动前重新核验文件 SHA-256；
- 若源码或 DLL 发生任何变化，必须创建新的 CandidateBuild、CaseId 和全套证据，旧证据不得继承；
- U3DS 不部署 ClientUi satellite，不得实例化任何 UI 类型；
- 记录实际 BepInEx、Unturned、SteamP2PFriends/LMN 版本，不凭记忆填写。

## 采集案例

### SP-15E-{UTC}

- 角色：`SinglePlayer`
- 新建世界，确认 BUE 插件加载；
- 打开玩家背包（G），执行普通网格内移动/旋转；
- 执行地面物品 → 玩家背包；
- 执行玩家背包 → 普通容器/车辆后备箱（如单人可用）；
- 验证关闭功能后恢复原生拖拽，再开启功能；
- 记录无效位置、满容器、关闭/重开容器后的陈旧预览行为；
- 导出原始 BepInEx 日志、UMM 诊断包和可观察截图/录像引用。

### P2P-15E-{UTC}

- `CaseId` 必须同时写入 Host 与 Client；
- Host 与 Client 使用完全相同的 DLL SHA-256；
- 两端记录各自环境指纹、版本、部署来源和 UTC 开始/结束时间；
- 完成 Host/Client 背包、箱子、车辆后备箱拖入与投影收敛；
- 导出两端完整日志，不能只提供一端；
- 两端时间窗必须实际重叠，否则资格裁决为 `Failed`。

### U3DS-15E-{UTC}

- 仅部署服务端允许的 BUE 主 DLL；
- 记录 U3DS Headless 启动、BepInEx Chainloader、BUE 加载和关闭日志；
- 证明无 ClientUi/Glazier/Sleek 类型解析或实例化；
- 记录 U3DS 版本、BepInEx 版本、部署路径、命令和完整日志；
- `0 plugins`、无 BUE 加载记录或只有启动无运行记录均不能算通过。

## 交付材料结构

```text
DEV-15E-runtime-evidence-{yyyyMMdd-HHmmss}/
  candidate/
    BetterUnturnedExperience.dll
    candidate.sha256.txt
  cases/
    sp-15e-<caseid>/
      evidence.log
      diagnostics.zip
      screenshots-or-video.txt
      case.json
    p2p-15e-<caseid>-host/
      evidence.log
      diagnostics.zip
      case.json
    p2p-15e-<caseid>-client/
      evidence.log
      diagnostics.zip
      case.json
    u3ds-15e-<caseid>/
      evidence.log
      diagnostics.zip
      case.json
```

`case.json` 需由 GPT 按既有 `EvidenceCase` 构造，不要自行改变字段语义。所有证据引用
使用安全相对路径；文件 SHA-256 必须与 `RuntimeEvidenceArtifact` 一致。

## 验收条件

- [ ] SP 真实运行证据完整；
- [ ] P2P Host 与 Client 双端完整、同 CaseId、同 DLL hash、时间窗严格重叠；
- [ ] U3DS Headless 真实加载/运行证据完整且无 UI 解析；
- [ ] 四角色均绑定当前 CandidateBuild 与 DLL SHA-256；
- [ ] GPT 导入 `RuntimeEvidencePackage` 并得到 `TechnicallyQualified`；
- [ ] Gemini 前端消费复核 ACCEPT；
- [ ] 人工开发者批准具体 CandidateBuild、BuildIdentity 与 DLL SHA-256；
- [ ] 未满足以上条件前，不得宣称 DEV-15 整体完成、ReleaseReady 或 Stable。

## 备注

DEV-15E 代码实现和 Gemini 复核已完成；本票是后续真实环境门禁，不是新的算法或 UI 开发票。
