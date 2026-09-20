using System.Runtime.CompilerServices;

// DEV-V6-04（V6-T2 Q6 / 02A 组 3 形态，02B/02C/02D 先例同款）：真分工程后测试工程对被测
// 工程的既有豁免——只许指向 *.Tests，跨功能/宿主/界面友元一律在册即红。BII 测试随类型
// 迁入宿主测试套件（spec 166 行「插件套件锁 BII 开工收工后的可观察启停」），故友元恰为
// 宿主测试工程一个。02A 组 3 红测按字符串字面量扫描本文件（样例写法在此不复述，避免被
// 友元扫描误记），声明必须保持字符串字面量形态。
[assembly: InternalsVisibleTo("BetterUnturnedExperience.Plugin.Tests")]
