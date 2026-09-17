using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUnturnedExperience.Lir;
using Mono.Cecil;

namespace BetterUnturnedExperience.Plugin.Tests
{
    /// <summary>
    /// DEV-V5-08 修复轮红测组：身份链 IL 守卫。真机缺陷（SP 轮1 探针 #2 定位）=
    /// 宿主 `SDG.Unturned.SteamPlayerID` 定义了**无判空的自定义 == 运算符**
    /// （op_Equality(a,b) 直接 a.steamID == b.steamID 两侧 callvirt），而它派生自
    /// System.Object——所以任何 `playerID == null` / `!= null` 写法在 Roslyn 下
    /// 编译成对该运算符的调用，运行时对 null 侧 callvirt get_steamID 必抛 NRE
    /// （无论 playerID 实值是否为 null）。这令技能窗判定与升级链在真机 100% 抛。
    /// 判据（机器化、非空转）：BUE 程序集内任何 BetterUnturnedExperience.* 类型的
    /// 方法体 IL 都不得含对 SteamPlayerID::op_Equality / op_Inequality 的调用；
    /// 判等一律 `is null` / ReferenceEquals（IL 层引用比较，不经运算符）。
    /// 非空转证明=突变 M1（把 CharacterKeyOfPlayer 的判空改回 `== null`）必令本组红。
    /// 先例：Lht OwnerResolver 生产形态即 ReferenceEquals(sp.playerID, null)。
    /// </summary>
    internal static class DevV508IdentityIlGuardTests
    {
        private const string SteamPlayerIdType = "SDG.Unturned.SteamPlayerID";

        internal static void Run(bool collectAllFailures = false)
        {
            var reds = new List<string>();
            try
            {
                void Check(bool condition, string message)
                {
                    if (condition) return;
                    if (collectAllFailures) reds.Add(message);
                    else throw new InvalidOperationException(message);
                }

                void Group(string name, System.Action body)
                {
                    try { body(); }
                    catch (Exception error) { reds.Add(name + " 组异常: " + error.Message); }
                }

                Group("SteamPlayerID 运算符 IL 守卫", () => V508GroupIlGuard(Check));
                Group("被守护方法在册（防改名空转）", () => V508GroupAnchoredMethods(Check));
            }
            finally
            {
                if (reds.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, reds));
            }
        }

        private static void V508GroupIlGuard(Action<bool, string> Check)
        {
            var violations = ScanAssemblyForSteamPlayerIdEqualityOperatorCalls();
            Check(violations.Count == 0,
                "BUE 程序集内存在对 SteamPlayerID 自定义 ==/!= 运算符的调用（真机必抛 NRE，判等须用 is null/ReferenceEquals）: "
                + string.Join(" | ", violations));
        }

        private static void V508GroupAnchoredMethods(Action<bool, string> Check)
        {
            // 守卫必须罩住已知踩坑方法：改名/删除=本组红（防 IL 扫描空转）。
            var hooks = typeof(LirSkillEngineHooks);
            var charKey = hooks.GetMethod("CharacterKeyOfPlayer",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check(charKey != null, "LirSkillEngineHooks.CharacterKeyOfPlayer 缺席——IL 守卫锚丢失");
            var normalize = typeof(ReloadSkillStore).GetMethod("NormalizeCharKey",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check(normalize != null, "ReloadSkillStore.NormalizeCharKey 缺席——身份链锚丢失");
        }

        /// <summary>Cecil 逐指令扫描 BUE 程序集：任何 BetterUnturnedExperience.* 类型
        /// 方法体里的 call/callvirt/ldvirtftn/ldftn 操作数若解析为
        /// SteamPlayerID::op_Equality / op_Inequality 即记违规（含显式调用与编译器
        /// 合成的运算符调用）。异常吞进违规清单（扫描自身失败=守卫失效=红）。</summary>
        private static List<string> ScanAssemblyForSteamPlayerIdEqualityOperatorCalls()
        {
            var violations = new List<string>();
            try
            {
                var location = typeof(LirSkillEngineHooks).Assembly.Location;
                if (string.IsNullOrEmpty(location))
                {
                    violations.Add("被测程序集无磁盘路径（动态装配？）——扫描不可运行");
                    return violations;
                }
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(location));
                var rp = new ReaderParameters { AssemblyResolver = resolver, ReadSymbols = false };
                using (var asm = AssemblyDefinition.ReadAssembly(location, rp))
                {
                    foreach (var type in asm.MainModule.GetTypes())
                    {
                        if (type.Namespace == null || !type.Namespace.StartsWith("BetterUnturnedExperience", StringComparison.Ordinal))
                            continue;
                        foreach (var method in type.Methods)
                        {
                            if (!method.HasBody) continue;
                            foreach (var instruction in method.Body.Instructions)
                            {
                                var mref = instruction.Operand as MethodReference;
                                if (mref == null) continue;
                                if (mref.DeclaringType == null) continue;
                                if (mref.DeclaringType.FullName != SteamPlayerIdType) continue;
                                if (mref.Name != "op_Equality" && mref.Name != "op_Inequality") continue;
                                violations.Add(type.FullName + "::" + method.Name + " @IL_0x"
                                    + instruction.Offset.ToString("X4") + " " + instruction.OpCode.Name
                                    + " -> " + mref.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                violations.Add("扫描异常（守卫失效）: " + error.Message);
            }
            return violations;
        }
    }
}
