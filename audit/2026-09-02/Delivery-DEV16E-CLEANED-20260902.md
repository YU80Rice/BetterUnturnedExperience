# 交付归档 — DEV-16E 正式 DLL（插桩清理后干净候选）

> CaseId：`DEV-16E-CLEAN-20260902-CLEANED` · CandidateBuild：`DEV-16E-CLEAN-20260902-CLEANED`
> 归档：`audit/2026-09-02/artifacts/DEV-16E-CLEAN-20260902/BetterUnturnedExperience.dll`
> 源码快照：`e78dc3b`（remove `[DEBUG-*]` instrumentation）

## 1. 命名约定（用户要求）

- **DLL 文件名**：`BetterUnturnedExperience.dll`（插件全称，不再使用 `-DIAG-R13SILENCE-r6-rotgrab-...` 长名）。
- **开发阶段名**放文件夹：`artifacts/DEV-16E-CLEAN-20260902/`。
- 该约定用于正式归档；诊断/迭代轮的历史长名产物保留在各自 `artifacts/<case>-rN-<date>/` 目录内不改名。

## 2. 候选身份

| 项 | 值 |
|---|---|
| SHA-256 | `FFBA97B8180383CA9A38B19D0D4A581885D8F35AD973A32A8EC43E945C03AEBD` |
| BuildIdentity | `A92C9B8703F71EF760F95DAF011AA997BD49F28389517DE2F60D044CBD0035D9`（编译代码验证） |
| SourceSnapshotId | `e78dc3b` |
| SizeBytes | 234496 |
| DefinitionSetDigest | `A6351887...`（不变） |
| ToolchainIdentity | `MSBuild-18.9.1+a81b43525\|.NETFramework-4.7.2\|CSharp-10` |
| ReferenceSet | `Libs-ReferenceSet-CA9AFA1D...` |

## 3. 与 DEV-16E 三环境验证产物的关系

- DEV-16E 三环境证据绑定旧 DLL `6ABB7E0D...`（含 `[DEBUG-*]` 插桩）。
- 本 DLL `FFBA97B8...` 与旧产物**仅差 `[DEBUG-*]` 日志删除**（-75 行纯删除，控制流/生产 seam 完全不变，双轴 CLEAN）。
- **功能等价**：插桩是只读日志，不影响增强拖入/自动旋转/提交/投影/Headless 分流。
- 三环境证据（SP/P2P/U3DS）针对旧哈希；如需本哈希的严格三环境证据，可复用同机重新采集（推荐做一次单人快速冒烟 + 保留既有三环境结论作为功能等价性佐证）。

## 4. 验证

- Release 构建 0 errors / 0 warnings。
- 七项目测试运行器全 PASS；R13 定向 16/16；UI token 0；diff-check 0。
- 双轴独立审查（Standards/Spec）CLEAN。
- `[DEBUG-]`/`BUE-DIAG-*` grep-zero。

## 5. 部署

正式部署时复制本 DLL 到 `E:\Steam\steamapps\common\Unturned\BepInEx\plugins\BetterUnturnedExperience.dll`（或 U3DS 对应路径），核对 SHA-256 `FFBA97B8...`。
