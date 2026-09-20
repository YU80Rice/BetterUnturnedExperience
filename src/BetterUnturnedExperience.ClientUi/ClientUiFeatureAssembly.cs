using System;
using System.Collections.Generic;
using BetterUnturnedExperience.ClientUi.Internal;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi
{
    /// <summary>
    /// DEV-V6-02E: the UI project's ONE public assembly type (V6-T2 追加裁决),
    /// now the real composition face. The host (Plugin) assembles and drives
    /// the client UI exclusively through these members — the UI ring no longer
    /// references the host project, the host no longer compiles UI sources,
    /// and every UI-internal type stays internal. This type is NOT a contract
    /// surface (public ≠ 契约，T1): it does not enter C.1, and ecosystem code
    /// keeps using the SDK registration face instead.
    ///
    /// 组合注入：BindHostComposition 快照一份宿主服务（日志口/面板日志/启停
    /// handler/机器事实/显示名/直显判定/facet 快照/目录条目投影/设置路由），
    /// CreateComposition 构造时快照进实例——后到的 bind 不改写已组装的环，
    /// 缺席=诚实退化（未绑定即吞，02B/C/D 同族契约）。
    /// 生产 JIT 门：BindEngineDispatcher 只由生产入口调用（与迁移前
    /// Plugin.Awake 的绑定点同一语义）——测试宿主不调，引擎路径保持静默 no-op。
    /// </summary>
    public static class ClientUiFeatureAssembly
    {
        private static ClientUiHostServices pendingHostComposition;

        /// <summary>当前已组装的界面组合根（组装后为 null=未组装/已销毁）。internal 供宿主
        /// harness 在测试进程内驱动组合语义；生产只经本类公开成员。</summary>
        internal static BueClientUiCompositionRoot Composition { get; private set; }

        /// <summary>宿主组合注入：全部参数具名契约型；任一缺席=该缝诚实退化。</summary>
        public static void BindHostComposition(
            Action<string> logRuntime = null,
            Action<string> logWarn = null,
            Action<string> logError = null,
            Action<string> logErrorFriendly = null,
            Func<string, bool> isCriticalNotReadyReason = null,
            BepInEx.Logging.ManualLogSource hostLog = null,
            Func<FeatureId, bool, bool> featureToggleHandler = null,
            Func<FeatureId, FeatureStatusView?> tryGetMachineStatus = null,
            Func<FeatureId, IReadOnlyList<SettingDescriptor>, FeatureSettingsSnapshot> getFacetSnapshot = null,
            Func<IReadOnlyList<(FeatureId Feature, bool? ClientUiSatellite,
                IReadOnlyList<SettingDescriptor> SettingDescriptors, Func<FeaturePresentationState> PresentationOverride,
                string DisplayName, bool DirectPresentation)>> getCatalogEntries = null,
            Func<FeatureId, FeatureSettingsSnapshot?> routeGetSnapshot = null,
            Func<FeatureId, IReadOnlyList<SettingDescriptor>> routeGetDescriptors = null,
            Func<FeatureId, uint, SettingMutation, SettingChangeResult> routeApply = null,
            Func<FeatureId, uint, IReadOnlyList<SettingMutation>, SettingChangeResult> routeApplyBatch = null,
            Func<FeatureId> getNetworkFeature = null,
            Func<string> getTakeoverStatus = null,
            Func<string> getConfigMigrationStatus = null,
            Func<bool> isTakeoverActive = null,
            Action refreshNetworkSwitches = null,
            Func<string, bool> isRuntimeEvent = null,
            Func<FeatureState?> officialComponentState = null,
            Func<FeaturePresentationView?> officialComponentPresentation = null)
        {
            var services = new ClientUiHostServices();
            services.LogRuntime = logRuntime;
            services.LogWarn = logWarn;
            services.LogError = logError;
            services.LogErrorFriendly = logErrorFriendly;
            services.IsCriticalNotReadyReason = isCriticalNotReadyReason;
            services.HostLog = hostLog;
            services.FeatureToggleHandler = featureToggleHandler;
            services.TryGetMachineStatus = tryGetMachineStatus;
            services.GetFacetSnapshot = getFacetSnapshot;
            services.GetCatalogEntries = getCatalogEntries;
            services.RouteGetSnapshot = routeGetSnapshot;
            services.RouteGetDescriptors = routeGetDescriptors;
            services.RouteApply = routeApply;
            services.RouteApplyBatch = routeApplyBatch;
            services.GetNetworkFeature = getNetworkFeature;
            services.GetTakeoverStatus = getTakeoverStatus;
            services.GetConfigMigrationStatus = getConfigMigrationStatus;
            services.IsTakeoverActive = isTakeoverActive;
            services.RefreshNetworkSwitches = refreshNetworkSwitches;
            services.IsRuntimeEvent = isRuntimeEvent;
            // DEV-V6-04: the official BII component's lifecycle facts ride the
            // host bridge (the host reads the Bii public seam; the composition
            // root consumes these for the official panel entry).
            services.OfficialComponentState = officialComponentState;
            services.OfficialComponentPresentation = officialComponentPresentation;
            pendingHostComposition = services;
            // The listen-host reconciler is a static seam holder (its engine
            // dispatcher and test hook are static too) — its host log mouths
            // bind here, rebind-on-bind (F1b semantics), unbound = swallow.
            Internal.ListenHostProjectionReconciler.HostLogSink = logRuntime;
            Internal.ListenHostProjectionReconciler.HostWarnSink = logWarn;
        }

        // Test affordance (harness-only, same family as BueRuntimeHost.Clear()):
        // lets a test process observe the unbound-degradation path after any
        // earlier bind. Production never calls this.
        internal static void ResetPendingHostCompositionForTests()
        {
            pendingHostComposition = null;
        }

        /// <summary>组装界面组合根（幂等：已有活环时保持并返回 true；DestroyComposition 后可重
        /// 组装）。DEV-V6-04：放置评估器工厂随 BII 组件迁 Bii 工程（宿主经 Bii 组合注入口
        /// 传入），本面不再收工厂——作废票 04。</summary>
        public static bool CreateComposition()
        {
            if (Composition != null) return true;
            Composition = new BueClientUiCompositionRoot(
                pendingHostComposition ?? new ClientUiHostServices());
            pendingHostComposition = null;
            return true;
        }

        /// <summary>合成+激活（迁移前宿主入口 client 分支的编排整体迁入：组合合成、面板模型/
        /// 原生面板、清单/拖拽配器注册与激活、诊断打标绑定、接线日志）。返回组合门禁结果。</summary>
        public static bool Initialize(bool isBatchMode, bool headless, bool nativeUiAvailable)
        {
            var composition = Composition;
            if (composition == null) return false;
            return composition.InitializeAndActivate(isBatchMode, headless, nativeUiAvailable);
        }

        /// <summary>宿主转达口接收端（Lit 投影转达的落点；迁移前由宿主 lambda 直呼内部协调器）。</summary>
        public static void OnTidyPagesCommitted(byte firstPage, byte lastPage)
        {
            Internal.ListenHostProjectionReconciler.OnTidyPagesCommitted(firstPage, lastPage);
        }

        /// <summary>页码范围绑定（单源=整理工程公开缝，由宿主组装根在 Awake 从 Lit 缝取值绑定）。</summary>
        public static void BindTidyableRange(byte min, byte max)
        {
            Internal.ListenHostProjectionReconciler.BindTidyableRange(min, max);
        }

        /// <summary>生产 JIT 门：绑定引擎对账路径。测试宿主不调（引擎路径静默 no-op）。</summary>
        public static void BindEngineDispatcher()
        {
            Internal.ListenHostProjectionReconciler.EngineDispatcher =
                Internal.ListenHostProjectionReconciler.BindEngine();
        }

        /// <summary>原生 UI 可用性探测（引擎成员在位检查；面板挂接的 fail-closed 前提）。</summary>
        public static bool CanBindNativeUi()
        {
            return Internal.BueNativeManagementPanel.CanBindNativeUi();
        }

        /// <summary>宿主触发面板目录刷新（完成链/设置应用后的重建时刻）。</summary>
        public static void RefreshManagementPanel()
        {
            Composition?.RefreshManagementPanel();
        }

        /// <summary>双装自检提示上板（DEV-V2-23）。</summary>
        public static void SetDoubleInstallNotice(string noticeLine)
        {
            var composition = Composition;
            if (composition == null || composition.ManagementPanel == null) return;
            composition.ManagementPanel.Model.SetDoubleInstallNotice(noticeLine);
        }

        /// <summary>面板节拍：宿主泵驱动（true=runtime pump 源，false=插件 Update 源）。返回是否
        /// 派发成功；隔离态经 PanelTickIsolated 观察。</summary>
        public static bool DispatchPanelTick(bool fromRuntimePump)
        {
            var composition = Composition;
            if (composition == null) return false;
            return composition.DispatchPanelTick(fromRuntimePump
                ? Internal.BueNativeManagementPanel.TickSource.RuntimePump
                : Internal.BueNativeManagementPanel.TickSource.Update);
        }

        public static bool PanelTickIsolated
        {
            get { return Composition != null && Composition.PanelTickIsolated; }
        }

        /// <summary>原生面板销毁（OnDestroy 顺序：先面板后组合，与迁移前宿主销毁序一致）。</summary>
        public static void DestroyNativePanel()
        {
            Composition?.DestroyNativePanel();
        }

        public static void DestroyComposition()
        {
            var composition = Composition;
            Composition = null;
            composition?.Destroy();
        }

        /// <summary>DEV-V6-04：Bii 界面缝桥——表面开协调委托（listen-host 修复时刻与
        /// 面板刷新的界面半边；组合未就绪=null→Bii 缺席语义）。</summary>
        public static Action BiiSurfaceOpened
        {
            get
            {
                var composition = Composition;
                return composition == null ? null : (Action)composition.OnBiiSurfaceOpened;
            }
        }

        /// <summary>DEV-V6-04：Bii 界面缝桥——设置快照读取器（设置单事实源留在界面工程，
        /// Bii 组件决策时拉取；组合未就绪=null）。</summary>
        public static Func<FeatureSettingsSnapshot> BiiSettingsSnapshot
        {
            get
            {
                var composition = Composition;
                return composition == null ? null : (Func<FeatureSettingsSnapshot>)composition.TakeSettingsSnapshot;
            }
        }

        /// <summary>官方 BII FeatureId（宿主 legacy 迁移别名与设置路由的 BII 判定取这里；
        /// 迁移前宿主直读内部 BetterItemInteractionSettingsState.Feature）。</summary>
        public static FeatureId OfficialFeature
        {
            get { return Internal.BetterItemInteractionSettingsState.Feature; }
        }
    }
}
