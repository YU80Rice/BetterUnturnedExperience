# DEV-07 实施报告：CandidateBuild 与 Qualification Gate 第一纵切片

## 一、范围与结论

- 基线：`BUE-V1-RT01-20260824`
- SourceSet：`BUE-SS-20260824-02`
- 结果：静态 CandidateBuild/证据/资格模块完成，Round 2 独立审计 PASS。
- 工单：`ready-for-human`。
- 真实 SP、SteamP2PFriends Host/Client、U3DS Headless 运行证据仍未完成，因此不宣称 ReleaseReady、Stable 或整体发布通过。

## 二、TDD 闭环

1. Red：先建立 `BetterUnturnedExperience.Release.Tests`，在实现项目缺失时得到编译错误。
2. Green：实现 CandidateBuild 身份、严格摘要校验、EvidenceCase、QualificationPolicy、QualificationEvaluator。
3. Round 1 独立审计 FAIL：发现证据元数据不足、摘要格式过宽、NotApplicable 无政策、P2P 只取首项且无时间窗、缺少完整 Candidate 绑定。
4. 修复：补齐候选绑定与完整证据元数据、UTC 时间窗、64 位 ASCII hex 校验、政策驱动 NotApplicable、所有案例配对。
5. 额外确定性修复：Release.Tests 项目启用 `Deterministic=true`；两次 Rebuild 测试 EXE 哈希一致。
6. Round 2 独立审计 PASS，无阻断项。

## 三、源码溯源

| 需求 | 落实位置 |
|---|---|
| BuildIdentity 排除 DLL hash | `src/BetterUnturnedExperience.Release/CandidateBuild.cs` |
| Definition/Artifact/Toolchain/Reference-set 绑定 | `CandidateBuildDescriptor` |
| 64 位 ASCII hex canonical digest | `CandidateBuildDescriptor.DigestValue` |
| 完整证据元数据与 UTC 时间窗 | `src/BetterUnturnedExperience.Release/Qualification.cs:EvidenceCase` |
| 政策驱动 NotApplicable、U3DS Headless 强制适用 | `QualificationPolicy` |
| Missing/Stale/Failed 与 P2P 多案例时间窗匹配 | `QualificationEvaluator` |
| TDD、确定性与负向断言 | `tests/BetterUnturnedExperience.Release.Tests/Program.cs` |

## 四、构建、测试与静态门禁

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：`0 errors / 0 warnings`。

测试：DEV-02、DEV-03、DEV-04、DEV-05、DEV-06、DEV-07 全部 PASS。

类型层面隔离扫描：Transport、Release 未发现 Unity/Glazier/Sleek/BepInEx/Harmony/Unturned/LMN/Adapter 类型引用、`Assembly.GetTypes` 或 `PatchAll`。通用 token 脚本对 `BepInExVersion` 字段名会误报，已用 `using`/ProjectReference/类型模式扫描复核为 PASS。

## 五、确定性与哈希

两次连续 Rebuild：

- Release DLL：`7AFAECF1C569AA57150E7CE113810E18271AE6DF9850D7C4682779DB1336ED7A`，两次相同。
- Release.Tests EXE：`1D24CEB43609B73CF344FF240EA9C9DDEAEDFBB3CEE125455FA17441437A6A60`，两次相同。

源码哈希：

- `CandidateBuild.cs`：`31001081D4E17202207C7BBC5CC73919C5778D0D24A921EEAB400FDAA0CCF8D7`
- `Qualification.cs`：`DC95DB1795B6E015A10341C5003F671DC6BD480DE07D655014013C6B713BEF3D`
- `Release.Tests/Program.cs`：`CB7A7C68F14A42434F58E9AA18A918FC18AA00E656665EB5284EB7F2EB4B65E5`

## 六、独立审计

- `DEV-07-Independent-Audit-R1.md`：FAIL，B-01～B-05。
- `DEV-07-Independent-Audit-R2.md`：PASS；确认修复完整、确定性构建成立、证据边界清晰。

## 七、未完成门禁

尚未采集真实 CandidateBuild 的单人、SteamP2PFriends Host/Client、U3DS Headless 运行证据；未执行真实部署、玩家交互、原生库存投影或网络互通验收。下一步交 Gemini 前端消费复核；复核通过后仍需人工批准和三环境证据，才能关闭 DEV-07。

