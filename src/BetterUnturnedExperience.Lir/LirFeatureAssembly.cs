using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lir
{
    /// <summary>
    /// DEV-V6-02A 骨架 → DEV-V6-02C：Lir（原位换弹）工程唯一公开组装类型——宿主接线的
    /// 登记与组合缝（V6-T2 追加：每官方功能工程恰好一个 public 组装类型，其余保持 internal）。
    /// 该类型不是契约面（public ≠ 契约，T1），不进 C.1 加性账本；成员全部是宿主组合接缝：
    ///   - FeatureId：冻结身份常量（LirRuntime.FeatureIdValue 的编译期别名；单一事实源仍在
    ///     本工程——宿主 legacy 迁移别名与 facade 身份经此读取，不指回域内 internal 常量）；
    ///   - CreateRegistration()：登记缝（02A 定形；02C 真登记体迁入本工程后翻真值、签名不变）；
    ///   - BindHostComposition()：宿主组合注入（V6-T2 硬项拆法——日志口、无画面判定、Steam
    ///     身份、设置根由宿主注入，换弹工程不再直呼宿主内部类型）；
    ///   - WiredModule / ResetWiredModule()：宿主环过渡接缝（02E 收缩前面板组合与生命周期
    ///     锚重置仍需接线实例——经契约类型投影，宿主不触碰本工程内部类型；02E 以界面公开
    ///     组装面重新定形）。
    /// 注入缺席（测试宿主未绑）的消费语义各自诚实退化：日志静默（LirRuntime 既有
    /// 「未绑定即吞」契约）、画面判定不裁剪、Steam 解析跳过、设置根 fail-closed。
    /// 换弹无宿主节拍泵（帧工作走冻结 HostTick 事件缝）、无界面转达口（无 ClientUi 交互）——
    /// 与整理工程的缝面差异即此。
    /// </summary>
    public static class LirFeatureAssembly
    {
        /// <summary>冻结功能身份（LirRuntime.FeatureIdValue 的编译期别名；单一事实源仍在本工程）。</summary>
        public const string FeatureId = LirRuntime.FeatureIdValue;

        /// <summary>官方中文显示名（面板消费；LirRuntime.DisplayName 的编译期别名）。</summary>
        public const string DisplayName = LirRuntime.DisplayName;

        // ── 宿主组合注入（V6-T2 硬项拆法：注入方向=宿主→换弹；落点=LirRuntime 领域事实缝）──

        /// <summary>宿主组合注入：登记前由组装根调用一次。null 参数=该注入面缺位（测试宿主
        /// 常态），消费方按缺席语义退化，绝不静默发明默认值（设置根缺例外=fail-closed）。</summary>
        public static void BindHostComposition(Action<string> runtimeLog, Action<string> errorLog,
            Func<bool> headlessDecision, Func<ulong> localSteamId, Func<ulong, object> findSteamPlayer,
            Func<string> settingsRoot)
        {
            LirRuntime.HostRuntimeLogSink = runtimeLog;
            LirRuntime.HostErrorLogSink = errorLog;
            LirRuntime.HostHeadlessDecision = headlessDecision;
            LirRuntime.HostLocalSteamId = localSteamId;
            LirRuntime.HostFindSteamPlayer = findSteamPlayer;
            LirRuntime.HostSettingsRoot = settingsRoot;
        }

        /// <summary>接线实例的契约类型投影（面板组合参数取用面；工厂装配后由工厂簿记在座）。</summary>
        public static IFeatureModule WiredModule => LirReloadRegistration.WiredModule;

        /// <summary>重置接线实例（工厂重置语义；生命周期锚测试经宿主 facade 置空同款）。</summary>
        public static void ResetWiredModule()
        {
            LirReloadRegistration.WiredModule = null;
        }

        /// <summary>宿主登记缝（02A 定形；02C 真体迁入后翻真值、签名不变）。</summary>
        public static IFeatureRegistration CreateRegistration()
        {
            return LirReloadRegistration.CreateRegistration();
        }
    }
}
