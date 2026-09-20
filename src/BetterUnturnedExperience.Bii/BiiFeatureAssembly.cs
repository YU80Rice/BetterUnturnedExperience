using System;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.Bii
{
    /// <summary>
    /// DEV-V6-04 真值翻面：Bii（更好的物品交互）工程唯一公开组装类型——宿主接线的
    /// 登记缝与组合注入口（02B/C/D 形态）。02A 骨架期「调用即抛并点名迁移票」的
    /// 过渡态终止（作废票：04）。真登记体与两个适配器（拖拽预览/提交、库存表面
    /// 生命周期）随本票迁入本工程；模块 Start 创建并武装交互 rig、Stop 拆自身
    /// （T3 Q2），headless 门禁下开工零武装不抛（T3 Q5）。该类型不是契约面
    /// （public ≠ 契约，T1），不进 C.1 加性账本。
    /// </summary>
    public static class BiiFeatureAssembly
    {
        /// <summary>组装根注入的缝袋快照：bind 时定形，后到 bind 不改写活环
        /// （02E 服务袋快照同族契约）。</summary>
        private static BiiCompositionMouths mouths;
        private static BiiInterfaceComposition interfaceComposition;

        /// <summary>公开组装类型缝：宿主 facade 经此取登记（签名自 02A 骨架冻结不变）。</summary>
        public static IFeatureRegistration CreateRegistration()
        {
            return BiiRegistration.CreateRegistration();
        }

        /// <summary>
        /// 组合注入口（宿主组装根动作）：宿主日志口、headless 门禁与放置评估器工厂。
        /// 缺席=headless 门禁 fail-closed（危险方向是武装——缺席绝不武装，02C 裁剪类
        /// 「缺席=不裁剪」的反方向，记名审计）。
        /// </summary>
        public static void BindHostComposition(
            Action<string> logRuntime = null,
            Action<string> logWarn = null,
            Action<string> logError = null,
            Action<string> logErrorFriendly = null,
            Func<string, bool> isCriticalNotReadyReason = null,
            Func<bool> headlessDecision = null,
            Func<IPlacementCandidateEvaluator> placementEvaluatorFactory = null)
        {
            mouths = new BiiCompositionMouths
            {
                LogRuntime = logRuntime,
                LogWarn = logWarn,
                LogError = logError,
                LogErrorFriendly = logErrorFriendly,
                IsCriticalNotReadyReason = isCriticalNotReadyReason,
                HeadlessDecision = headlessDecision
            };
            BiiRegistration.PlacementEvaluatorFactory = placementEvaluatorFactory;
            BiiRegistration.HostMouths = mouths;
        }

        /// <summary>
        /// 界面缝注入口（宿主 client 分支动作，组合就绪后）：表面开/关的界面协调
        /// 委托与设置快照读取器。缺席=rig 不武装（不创建界面、不打库存界面补丁）——
        /// T3 Q5「不创建界面」的缝缺席形态。
        /// </summary>
        public static void BindInterfaceComposition(
            Action surfaceOpened = null,
            Action surfaceClosed = null,
            Func<FeatureSettingsSnapshot> settingsSnapshot = null)
        {
            interfaceComposition = new BiiInterfaceComposition
            {
                SurfaceOpened = surfaceOpened,
                SurfaceClosed = surfaceClosed,
                SettingsSnapshot = settingsSnapshot
            };
            BiiRegistration.InterfaceComposition = interfaceComposition;
        }

        /// <summary>面板状态投影缝：官方条目的事实源保持组件侧（「面板状态侧非第二
        /// 事实源」）；rig 缺席=null（组合根保持迁移前 component==null 的 fallback
        /// 分支语义）。宿主转接给组合根注入。</summary>
        public static Func<FeatureState> ComponentState
        {
            get
            {
                var rig = BiiRegistration.WiredRig;
                return rig != null && rig.Component != null ? (Func<FeatureState>)(() => rig.Component.Lifecycle.State) : null;
            }
        }

        /// <summary>面板表现投影缝（同 ComponentState 的缺席语义）。</summary>
        public static Func<FeaturePresentationView> ComponentPresentation
        {
            get
            {
                var rig = BiiRegistration.WiredRig;
                return rig != null && rig.Component != null ? (Func<FeaturePresentationView>)(() => rig.Component.Lifecycle.Presentation) : null;
            }
        }

        /// <summary>宿主 Update 驱动的拖拽预览节拍（迁移前经界面公开面
        /// ClientUiFeatureAssembly.TickDragPreview 转发——作废票 04，宿主直调本缝，
        /// Plugin→Bii 组装根合法边）。</summary>
        public static void TickPreview()
        {
            var rig = BiiRegistration.WiredRig;
            if (rig != null) rig.TickDragPreview();
        }

        /// <summary>宿主 OnDestroy 的显式隔离步（迁移前经界面公开面
        /// ClientUiFeatureAssembly.IsolateInventoryAdapters 转发——作废票 04）。</summary>
        public static void IsolateAdapters()
        {
            var rig = BiiRegistration.WiredRig;
            if (rig != null) rig.Teardown();
        }

        /// <summary>组装器簿记投影（internal——宿主测试套件经 IVT 驱动结构断言；
        /// 生产组合路径只有模块 Start 的工厂装配，V6-T2）。</summary>
        internal static BiiInteractionRig WiredRig
        {
            get { return BiiRegistration.WiredRig; }
        }

        /// <summary>测试隔离缝（internal——经 InternalsVisibleTo 的 *.Tests 通道使用；
        /// 非公开组装面成员，不接受生产调用方）。清 rig 簿记与注入绑定，供每组测试从
        /// 干净基线开局。**不执行 Teardown 也不拆补丁**：生产的收工拆除唯一路径是模块
        /// Stop → BiiInteractionRig.Teardown（适配器内 UnpatchSelf），本缝只清簿记——
        /// 调用方不得把它当收工替代。
        /// 与 02B/C/D 先例的差异（本票 Spec 轴 R1 裁定的收窄）：先例在公开组装面保留
        /// null-only 伪 setter；本缝额外清注入绑定（更强的测试隔离语义），故收窄为
        /// internal，避免公开面上出现「清绑定即静默失联」的生产可调用形态。</summary>
        internal static void ResetWiredModule()
        {
            BiiRegistration.WiredRig = null;
            BiiRegistration.HostMouths = null;
            BiiRegistration.InterfaceComposition = null;
            BiiRegistration.PlacementEvaluatorFactory = null;
        }
    }
}
