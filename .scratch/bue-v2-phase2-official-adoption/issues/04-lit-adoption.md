# T4：LIT（背包整理）纳入方式

Type: grilling
Status: open
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 02, 03

## Question

LIT（LaunchInventoryTidy）官方纳入的方式决策（「吃掉并消化」：以 BUE 自己的契约/生命周期重新表达，原项目停维护）：

1. LMN 命名频道调用面 → `BueNetworkApi` 的重写映射（按 T2 盘点的调用点逐个定）;
2. `[BepInDependency(LMN,Hard)]` 摘除方式与项目落位（迁入本仓库后的 csproj 形态、单 DLL 装配）;
3. 设置/面板接入（Settings Facet、官方中文名「背包整理」、条目身份）;
4. **O-LIT-1（独立拍板）**：排列算法质量——原样移植保行为 vs 借机重设计;若重设计，验收口径怎么定（行为兼容 vs 新行为）。

产出：LIT 纳入设计决策，可交 `/to-spec`。
