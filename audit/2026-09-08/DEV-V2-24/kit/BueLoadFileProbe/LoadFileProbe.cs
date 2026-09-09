using System;
using System.IO;
using System.Reflection;
using BepInEx;

[assembly: AssemblyVersion("0.0.0.0")]

namespace BueLoadFileProbe
{
    // DEV-V2-24 T7-4 C4' 修正仪器（证据仪器，非生产代码）：
    // C4 finding 实证——标准 BepInEx 装载路径上，Mono LoadFrom 按程序集身份折叠，
    // 同名副本（BueSameAsmProbe-A/Z）在类型扫描段即被跳过，从未进入 AppDomain，
    // 001 按冻结契约无从触发。本探针走「非标准装载通道」：先把 BUE DLL 复制到
    // 临时路径（conflictLocation ≠ selfPath），再 Assembly.LoadFile（不做身份绑定
    // 检查，同路径重复调用也产生新实例）注入第二个同名程序集实例。
    // GUID = io.github.yu80rice.aaa-loadfile-probe，序在 BUE 之前 → 本 Awake 先于
    // BUE 的 Awake 防双装扫描执行。预期：BUE-PLATFORM-001 Warning 行 + 管理面板
    // 红色状态行（= SDK §8 补注所称 001 对非标准装载路径的兜底，实机正向双证）。
    [BepInPlugin("io.github.yu80rice.aaa-loadfile-probe", "BUE LoadFile Duplicate Probe", "0.0.0")]
    public sealed class LoadFileProbe : BaseUnityPlugin
    {
        private void Awake()
        {
            const string target = "BetterUnturnedExperience";
            // C4' 实证:BepInEx 惰性装载——本 Awake 时 BUE 程序集尚未进 AppDomain,
            // 按 AppDomain 搜索必然 miss。C4'' 改盲种:从自身同目录找 BUE DLL,复制两份
            // 临时副本并 LoadFile(不做身份绑定检查)。无论 BepInEx 随后以何种语义装载
            // 真 BUE(身份检查→劫持到 temp1;或无检查→三实例并存),BUE Awake 扫描时
            // AppDomain 内同名实例必然 ≥2 → BUE-PLATFORM-001 以真实冲突状态触发。
            var ownDir = Path.GetDirectoryName(typeof(LoadFileProbe).Assembly.Location);
            if (string.IsNullOrEmpty(ownDir))
            {
                Logger.LogInfo("[PROBE-LF] event=seed result=miss reason=no-own-location");
                return;
            }
            var source = Path.Combine(ownDir, target + ".dll");
            if (!File.Exists(source))
            {
                Logger.LogInfo("[PROBE-LF] event=seed result=miss reason=bue-file-not-found path=" + source);
                return;
            }
            try
            {
                Seed(source, Path.Combine(Path.GetTempPath(), "BueLoadFileProbe-seed1.dll"));
                Seed(source, Path.Combine(Path.GetTempPath(), "BueLoadFileProbe-seed2.dll"));
                Logger.LogInfo("[PROBE-LF] event=seed result=loaded appdomainCopies=" + CountCopies(target));
            }
            catch (Exception error)
            {
                Logger.LogInfo("[PROBE-LF] event=seed result=failed errorType=" + error.GetType().Name);
            }
        }

        private void Seed(string sourcePath, string seedPath)
        {
            File.Copy(sourcePath, seedPath, overwrite: true);
            var seeded = Assembly.LoadFile(seedPath);
            Logger.LogInfo("[PROBE-LF] event=seed-item result=loaded location=" + seeded.Location
                + " assemblyName=" + seeded.GetName().Name);
        }

        private static bool IsNamed(Assembly assembly, string name)
        {
            try
            {
                return assembly != null && string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception) { return false; }
        }

        private static int CountCopies(string name)
        {
            var count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsNamed(assembly, name)) count++;
            }
            return count;
        }
    }
}
