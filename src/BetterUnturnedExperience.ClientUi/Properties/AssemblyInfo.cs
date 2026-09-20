using System.Runtime.CompilerServices;

// DEV-V6-02E: the host harness (Plugin.Tests) drives the moved UI internals
// across the assembly boundary — the sanctioned *.Tests friend shape
// (T2 Q6「测试对被测工程的 InternalsVisibleTo 可保留」; firewall-exempt).
[assembly: InternalsVisibleTo("BetterUnturnedExperience.ClientUi.Tests")]
[assembly: InternalsVisibleTo("BetterUnturnedExperience.Plugin.Tests")]
