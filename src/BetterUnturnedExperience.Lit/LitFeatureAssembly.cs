using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// DEV-V6-02B：Lit（背包整理）工程唯一公开组装类型——宿主接线的登记缝（V6-T2 追加：
    /// 每官方功能工程恰好一个 public 组装类型，其余保持 internal）。该类型不是契约面
    /// （public ≠ 契约，T1），不进 C.1 加性账本；成员全部是宿主组合接缝：
    ///   - CreateRegistration()：登记缝（02A 定形；02B 真登记体迁入本工程后翻真值、签名不变）；
    ///   - BindHostComposition()：宿主组合注入（V6-T2 硬项拆法——宿主日志口、无画面判定、
    ///     Steam 身份由宿主注入，整理工程不再直呼宿主内部类型）；
    ///   - BindProjectionRelay()：听主机投影对账宿主转达口（V6-T2 硬项拆法——整理与界面
    ///     互调改为宿主转达；组装根绑定，与界面引擎分派器同一绑定家族）；
    ///   - WiredModule / ResetWiredModule()：接线实例契约投影与代际重置（宿主环过渡接缝；
    ///     02E 收缩后主机侧零消费，测试锚与面板组合参数仍经此读）。
    /// DEV-V6-11：宿主私有节拍泵缝已拆除——本功能的帧工作走冻结的 HostTick 事件缝
    /// （与换弹/尸潮同一缝），宿主不再保存或直接泵动整理运行实例。
    /// 注入缺席（测试宿主未绑）的消费语义各自诚实退化：日志静默（LitRuntime 既有
    /// 「未绑定即吞」契约）、画面判定不裁剪、Steam 解析跳过、转达 no-op。
    /// </summary>
    public static class LitFeatureAssembly
    {
        /// <summary>冻结功能身份（LitRuntime.FeatureIdValue 的编译期别名；单一事实源仍在本工程）。</summary>
        public const string FeatureId = LitRuntime.FeatureIdValue;

        /// <summary>官方中文显示名（面板消费；LitRuntime.DisplayName 的编译期别名）。</summary>
        public const string DisplayName = LitRuntime.DisplayName;

        /// <summary>可整理页范围（冻结区间；HotkeySnapshotUtil 常量的编译期别名，单一事实源
        /// 在本工程）。宿主取值后经组合绑定给界面协调器（02B 宿主转达——范围数据不进契约，
        /// 界面层不指回整理工程）。</summary>
        public const byte TidyablePageMin = HotkeySnapshotUtil.TIDYABLE_PAGE_MIN;
        public const byte TidyablePageMax = HotkeySnapshotUtil.TIDYABLE_PAGE_MAX;

        // ── 宿主组合注入（V6-T2 硬项拆法：注入方向=宿主→整理）──
        internal static Action<string> HostRuntimeLogSink;
        internal static Action<string> HostErrorLogSink;
        internal static Func<bool> HostHeadlessDecision;
        internal static Func<ulong> HostLocalSteamId;
        internal static Func<ulong, object> HostFindSteamPlayer;

        /// <summary>宿主组合注入：登记前由组装根调用一次。null 参数=该注入面缺位（测试宿主
        /// 常态），消费方按缺席语义退化，绝不静默发明默认值。</summary>
        public static void BindHostComposition(Action<string> runtimeLog, Action<string> errorLog,
            Func<bool> headlessDecision, Func<ulong> localSteamId, Func<ulong, object> findSteamPlayer)
        {
            HostRuntimeLogSink = runtimeLog;
            HostErrorLogSink = errorLog;
            HostHeadlessDecision = headlessDecision;
            HostLocalSteamId = localSteamId;
            HostFindSteamPlayer = findSteamPlayer;
        }

        // ── 听主机投影对账宿主转达口（整理与界面互调改宿主转达，V6-T2）──
        internal static Action<byte, byte> ProjectionRelay;

        /// <summary>宿主绑定投影对账转达（生产绑定于组装根 Awake）。未绑定（测试宿主）=
        /// 提交后的对账转达为静默 no-op——与界面协调器「两个分派器都缺席即静默 no-op」的
        /// 既有语义一致（宿主测试路径永不 JIT 引擎方法）。</summary>
        public static void BindProjectionRelay(Action<byte, byte> relay)
        {
            ProjectionRelay = relay;
        }

        /// <summary>整理侧唯一转达入口（整理提交三处共用；页码范围随转达参数携带）。</summary>
        internal static void RelayProjectionReconcile(byte firstPage, byte lastPage)
        {
            ProjectionRelay?.Invoke(firstPage, lastPage);
        }

        // ── 宿主环过渡接缝（02E 收缩后主机侧零消费；作废票：DEV-V6-02E 以界面公开组装面重新定形）──

        /// <summary>接线实例的契约类型投影（面板组合参数取用面；测试锚与代际重置经此读）。</summary>
        public static IFeatureModule WiredModule => LitTidyRegistration.WiredModule;

        /// <summary>重置接线实例（工厂重置语义；生命周期锚测试经宿主 facade 置空同款）。</summary>
        public static void ResetWiredModule()
        {
            LitTidyRegistration.WiredModule = null;
        }

        // DEV-V6-11: the host-ring tick pump seam is GONE — the host completion
        // chain no longer drives this feature's run instance. The beat rides the
        // frozen HostTick event seam (the LIR/LHT shape): the module subscribes
        // at Start and the host clock publishes; nothing in the host names this
        // feature's run instance any more.

        /// <summary>宿主登记缝（02A 定形；02B 真体迁入后翻真值、签名不变）。</summary>
        public static IFeatureRegistration CreateRegistration()
        {
            return LitTidyRegistration.CreateRegistration();
        }
    }
}
