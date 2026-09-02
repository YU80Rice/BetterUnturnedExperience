# DEV-16E U3DS Headless 运行证据 — 2026-09-02

> CaseId：`DEV-16E-20260902` · CandidateBuild：`DEV-16E-CLEAN-20260902`
> 候选 DLL：`audit/2026-09-02/BetterUnturnedExperience-DIAG-R13SILENCE-r6-rotgrab-20260902.dll`
> SHA-256：`6ABB7E0D930D5560EFE46F27A5058DF9F7000A2EC0DB3CF4E08B94CD2AF3615C`
> 状态：**U3DS Headless 运行验证 PASS**（用户 2026-09-02 确认）。

## 1. 证据来源

- **U3DS 服务器日志**：`...\UMM-v2.2.0-win-x64\LogOutput.log`（LastWriteTime 2026-09-02 14:42:36，20 行）
- **客户端诊断包**：`UMM-诊断包_20260902_144557`（连接 U3DS 服务器，增强渲染验证）

## 2. 服务器日志验证（QualificationGate U3dsHeadless 义务）

### ✅ 出现（Headless 正确分流）

| 日志行 | 意义 |
|---|---|
| `assembly-identity ... sha256=6ABB7E0D...` | 部署同一 rotgrab 候选 DLL ✓ |
| `runtime-gate decision=Headless batchMode=True headless=True`（BUE-BOOTSTRAP-002） | BepInEx 启动判定 Headless，未进 Client 分支 |
| `status=BootstrapReady decision=Headless`（BUE-BOOTSTRAP-001） | 主插件以 Headless 就绪 |
| `Better Item Interaction ... accepted=True`（BUE-REG-ACCEPT） | 官方功能注册成功（服务器侧仍注册 Catalog，但不创建 UI） |

### ✅ 不应出现项（全部通过）

| 检查 | 结果 |
|---|---|
| `ClientUi` / `PlayerUI` / `Glazier` / `SleekItems` / `SleekSlot` 类型解析/实例化 | 无 ✓ |
| `hooks-installed` / `placed-item-delegate` / 客户端 Harmony Hook | 无 ✓ |
| `surface-context-dispatched` / `drag-started` / `placement-decision` / 增强预览 | 无 ✓（服务器不 dispatch 库存 surface，不渲染） |
| 唯一匹配 `host-destroyed state=preserved patches-kept=true`（BUE-CLIENTUI-005） | 服务器 `OnDestroy` 清理日志，非 UI 创建，预期 ✓ |

## 3. 客户端连接验证（`UMM-诊断包_20260902_144557`）

- 部署 DLL hash `6ABB7E0D...`（同候选）✓
- surface dispatch：`PlayerInventory page=3 (7×4)` 正常 ✓
- 增强渲染：gen 3 起 `state=Candidate` + `preview-visible` 多次（绿框/图标正常）；gen 1/2 `enhanced=False` 为启动初期 sink 未绑定段，预期
- 用户确认："在强化渲染页内，功能实现无异常"

## 4. 与验收项对应

- [x] U3DS Headless 使用同一主 DLL 完成启动/运行/关闭证据，证明不创建 UI、不安装客户端 Hook、不解析客户端表现层。
  —— 同一 DLL hash ✓ / Headless 分流 ✓ / 无 UI/Hook/表现层解析 ✓ / 客户端经 U3DS 服务器连接功能正常 ✓。

## 5. 剩余事项（DEV-16E 收尾）

1. 三环境证据包合并（SP + P2P Host/Client + U3DS Headless）→ `RuntimeEvidencePackageValidator` 校验。
2. `QualificationEvidenceGate` 裁决 → `TechnicallyQualified`（四角色 Fulfilled）。
3. Gemini 前端消费复核 ACCEPT。
4. 人工开发者批准具体 BuildIdentity/LoadSetIdentity/DLL 哈希 → 关闭 DEV-16E → 启动 DEV-16F。
