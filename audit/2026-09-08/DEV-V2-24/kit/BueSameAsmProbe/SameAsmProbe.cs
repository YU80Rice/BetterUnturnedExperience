using System.Reflection;
using BepInEx;

[assembly: AssemblyVersion("0.0.0.0")]

namespace BueSameAsmProbe
{
    // DEV-V2-24 T7 实机探针（证据仪器，非生产代码）：
    // 与 BUE 同 AssemblyName（BetterUnturnedExperience）、同 AssemblyVersion（0.0.0.0），
    // 但携带独立 BepInPlugin GUID。两个变体只差 GUID：
    //   A 变体 GUID 排序在 BUE 之前（aaprobe < betterunturnedexperience）——副本程序集
    //     预期先于 BUE Awake 进入 AppDomain（BUE-PLATFORM-001 正向双证场景）；
    //   Z 变体 GUID 排序在 BUE 之后（zprobe > betterunturnedexperience）——副本程序集
    //     预期后于 BUE Awake 才加载（SDK §6 冻结边界内的时序事实观察）。
    // 编译产物 AssemblyName = BetterUnturnedExperience（由 /out 文件名决定），
    // 部署文件名用 BueSameAsmProbe-{A,Z}After.dll——文件名 ≠ 程序集名正是实验对象。
#if PROBE_AFTER
    [BepInPlugin("io.github.yu80rice.zprobe.sameasm", "BUE SameAssemblyName Probe (loads after BUE)", "0.0.0")]
    internal static class ProbeGuid { internal const string Value = "io.github.yu80rice.zprobe.sameasm"; }
#else
    [BepInPlugin("io.github.yu80rice.aaprobe.sameasm", "BUE SameAssemblyName Probe (loads before BUE)", "0.0.0")]
    internal static class ProbeGuid { internal const string Value = "io.github.yu80rice.aaprobe.sameasm"; }
#endif

    public sealed class SameAsmProbe : BaseUnityPlugin
    {
        private void Awake()
        {
            var assembly = typeof(SameAsmProbe).Assembly;
            var name = assembly.GetName();
            Logger.LogInfo("[PROBE] awake probeGuid=" + ProbeGuid.Value
                + " assemblyName=" + name.Name
                + " version=" + name.Version
                + " location=" + assembly.Location);
        }
    }
}
