using System.Runtime.CompilerServices;

// DEV-V6-02D（V6-T2 Q6 / 02A 组 3 形态）：真分工程后测试工程对被测工程的既有豁免——
// 只许指向 *.Tests，跨功能/宿主/界面友元一律在册即红。02A 组 3 红测按字符串字面量
// 扫描本文件（样例写法在此不复述，避免被友元扫描误记），声明必须保持字符串字面量形态。
[assembly: InternalsVisibleTo("BetterUnturnedExperience.Plugin.Tests")]
