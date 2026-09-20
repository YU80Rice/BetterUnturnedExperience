using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V6-02A 骨架 → DEV-V6-02D：Lht（尸潮计数）工程唯一公开组装类型——宿主接线的
    /// 登记与组合缝（V6-T2 追加：每官方功能工程恰好一个 public 组装类型，其余保持
    /// internal）。该类型不是契约面（public ≠ 契约，T1），不进 C.1 加性账本；成员全部是
    /// 宿主组合接缝：
    ///   - FeatureId / DisplayName：冻结身份常量（LhtRuntime 的编译期别名；单一事实源仍在
    ///     本工程——宿主 legacy 迁移别名与面板身份经此读取，不指回域内 internal 常量）；
    ///   - CreateRegistration()：登记缝（02A 定形；02D 真登记体迁入本工程后翻真值、签名不变）；
    ///   - BindHostComposition()：宿主组合注入（V6-T2 硬项拆法——本票唯一注入面=宿主日志口，
    ///     尸潮工程不再直呼宿主内部日志类型）；
    ///   - WiredModule / ResetWiredModule()：接线实例契约投影与重置（宿主环过渡接缝，
    ///     02E 以界面公开组装面重新定形）；
    ///   - WiredPresentationState：面板 Lht 表现缝（DEV-V2-20 Available/HeadlessOnly 是模块
    ///     自有判定，不属契约面——宿主面板经此读取，不触碰本工程内部类型；无接线实例=
    ///     Available，与迁移前面板 lhtModule==null 分支同语义）。
    /// 注入缺席（测试宿主未绑）的消费语义诚实退化：日志静默（LhtRuntime 既有「未绑定即吞」
    /// 契约）。尸潮无宿主节拍泵（帧工作走冻结 HostTick 事件缝）、无界面转达口（无 ClientUi
    /// 交互）——与整理工程的缝面差异即此。
    /// </summary>
    public static class LhtFeatureAssembly
    {
        /// <summary>冻结功能身份（LhtRuntime.FeatureIdValue 的编译期别名；单一事实源仍在本工程）。</summary>
        public const string FeatureId = LhtRuntime.FeatureIdValue;

        /// <summary>官方中文显示名（面板消费；LhtRuntime.DisplayName 的编译期别名）。</summary>
        public const string DisplayName = LhtRuntime.DisplayName;

        // ── 宿主组合注入（V6-T2 硬项拆法：注入方向=宿主→尸潮；落点=LhtRuntime 领域事实缝）──

        /// <summary>宿主组合注入：登记前由组装根调用一次。null 参数=该注入面缺位（测试宿主
        /// 常态），消费方按缺席语义退化（日志静默），绝不静默发明默认值。</summary>
        public static void BindHostComposition(Action<string> runtimeLog, Action<string> errorLog)
        {
            LhtRuntime.HostRuntimeLogSink = runtimeLog;
            LhtRuntime.HostErrorLogSink = errorLog;
        }

        /// <summary>接线实例的契约类型投影（面板组合参数取用面；工厂装配后由工厂簿记在座）。</summary>
        public static IFeatureModule WiredModule => HordeTrackerRegistration.WiredModule;

        /// <summary>重置接线实例（工厂重置语义；生命周期锚测试经宿主 facade 置空同款）。</summary>
        public static void ResetWiredModule()
        {
            HordeTrackerRegistration.WiredModule = null;
        }

        /// <summary>面板 Lht 表现缝：接线模块的 Available/HeadlessOnly 自有判定；无接线实例
        /// （模块停止/未装配）=Available——与迁移前面板「lhtModule==null → Available」分支
        /// 同语义，不是缺席被发明成「有画面」。</summary>
        public static FeaturePresentationState WiredPresentationState
        {
            get
            {
                var wired = HordeTrackerRegistration.WiredModule;
                return wired == null ? FeaturePresentationState.Available : wired.PresentationState;
            }
        }

        /// <summary>宿主登记缝（02A 定形；02D 真体迁入后翻真值、签名不变）。</summary>
        public static IFeatureRegistration CreateRegistration()
        {
            return HordeTrackerRegistration.CreateRegistration();
        }
    }
}
