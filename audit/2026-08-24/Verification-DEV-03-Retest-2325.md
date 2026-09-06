# DEV-03 构建复测报告（2325）

**作者：GPT**  
**Baseline:** BUE-V1-RT01-20260824  
**SourceSet:** BUE-SS-20260824-02

## 结果

- Release Rebuild：PASS，5 项目，0 errors / 0 warnings。
- Contracts tests：PASS。
- DEV-03 Settings tests：PASS。
- Contracts/Core UI-native token scan：PASS（2 / 4 C# files）。
- 当前代码包含 Round 3 后的 ServerPolicy 收窄校验、generation watermark、replace failure injection 与对应测试。

## 产物哈希

- Contracts DLL：31FA2A9A0840CE12E7D2C496A8B1FA13A3433FF340C2E0F6B9B7B200DAF531F6
- Core DLL：2091E8266BE86AAAC6753CEA531A293FC3A09398B4BA4F45924416EF5F480EB1
- Plugin DLL：AF99800C3B44176213F59C38055FE65161162F26C7BABA2AB92B5AAE55A1D2AC
- Settings test EXE：50CC4F7BA93F3E8BDC2D1A5F0DCA896CF656651EB62291D98F50D2C7BFB3F0AB

## 审计门禁

Round 3 独立审计针对前一版仍为 FAIL；本次修复后的新独立审计报告尚未返回。因此本报告只裁定构建/测试复测 PASS，不裁定 DEV-03 resolved，不宣称三环境运行或发布通过。
