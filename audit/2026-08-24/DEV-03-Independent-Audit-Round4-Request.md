# DEV-03 独立审计 Round 4 请求

请独立只读审计当前工作区修复后版本，必须复跑：
- BetterUnturnedExperience.sln Release Rebuild
- Contracts tests 与 DEV-03 Settings tests
- Contracts/Core UI-native token scan
- 核对当前 DLL/EXE SHA-256

重点检查 Round 3 阻断修复：ServerPolicy 只能收窄静态 Descriptor（数值边界、Step、Choice 子集、Choice 禁止 range/step）；FileSettingsPersistence 注入 replace failure 时旧文件和快照保持不变；generation clear 后旧 generation 拒绝；TDD 证据覆盖完整。

输出独立报告 GPT-DEV-03-Independent-Audit-Round4-Final.md，结论必须为 PASS/FAIL；若 FAIL 列出阻断文件/位置/根因。不得把构建 PASS 误写成运行环境 PASS。
