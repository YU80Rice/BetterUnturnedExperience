# DEV-03 复测报告

**作者：GPT**  
**日期：2026-08-24**  
**Baseline：** `BUE-V1-RT01-20260824`  
**SourceSet：** `BUE-SS-20260824-02`  
**范围：** DEV-03 SettingsRuntime、原子持久化、LocalLoopback 复测；不包含三环境运行验收。

## 1. 复测结论

**代码/测试复测：PASS。**

本轮未修改生产代码；复测确认上一轮修复后的实现可重复构建和运行，未出现回归。DEV-03 仍保持 `ready-for-agent`，因为独立审计 Round 3 与 Gemini 消费复核尚未形成最终关闭证据。

## 2. 执行命令与结果

```text
MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：5 个项目，`0 errors / 0 warnings`，退出码 `0`。

```text
tests/BetterUnturnedExperience.Contracts.Tests/bin/Release/BetterUnturnedExperience.Contracts.Tests.exe
```

结果：`DEV-02 definition linker tests: PASS`。

```text
tests/BetterUnturnedExperience.Settings.Tests/bin/Release/BetterUnturnedExperience.Settings.Tests.exe
```

结果：`DEV-03 settings runtime tests: PASS`。

```text
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Contracts
Verify-NoUiTokens.ps1 -SourceRoot src/BetterUnturnedExperience.Core
```

结果：Contracts `PASS`（2 个 C# 文件），Core `PASS`（4 个 C# 文件）。

完整日志：[DEV-03-retest-20260824.log](./DEV-03-retest-20260824.log)

## 3. 已覆盖的关键门禁

- 三 authority 与 scope revision 独立。
- 多字段原子提交、无变化不增 revision。
- ExpectedRevision、未知设置、类型/权限失败保持旧快照。
- RequestId 相同 payload 重放、冲突后原记录保留、满容量拒绝新请求且不淘汰旧记录。
- Descriptor 默认值、range/step、Choice、Float finite 语义校验。
- Policy 更新失败原子性与旧 generation 拒绝。
- Connection generation 清理 overlay/replay，并保留 generation watermark。
- Descriptor、Policy、Snapshot 的嵌套 `AllowedValues` 隔离。
- 文件提交重读校验、损坏/未来 schema quarantine 与安全默认回退。
- LocalLoopback 不依赖 LMN。
- Contracts/Core UI/native token 隔离。

## 4. 产物 SHA-256

| 产物 | SHA-256 |
|---|---|
| `BetterUnturnedExperience.Contracts.dll` | `31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6` |
| `BetterUnturnedExperience.Core.dll` | `AABE33B04E81D1A746DBF8F8D77E4C43E7E33B01035D8624565F0393FC82407F` |
| `BetterUnturnedExperience.dll` | `AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC` |
| `BetterUnturnedExperience.Settings.Tests.exe` | `0DB468153364B7240DE04003E00DFAD71445CABF3ED278D1A12EB0A6FDBEBD58` |

## 5. 证据边界

本报告只证明本机静态构建、单元测试和源码 token 门禁复测通过；不证明 SP、SteamP2PFriends Host/Client、U3DS、LMN 或发布授权通过。DEV-03 关闭仍需独立审计 PASS 与 Gemini `ACCEPT`。
