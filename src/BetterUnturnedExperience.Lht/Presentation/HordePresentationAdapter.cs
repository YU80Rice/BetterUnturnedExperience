using System;

namespace BetterUnturnedExperience.Lht
{
    /// <summary>
    /// DEV-V2-20: the presentation internal piece (spec「内部双件」之一：HUD
    /// 注入 / 10Hz 更新). Renders the shared horde state through the
    /// IHordeHudSurface seam at the 10Hz implementation constant. A HUD
    /// failure degrades the PRESENTATION ONLY — the fault is isolated inside
    /// the tick and never reaches the authority path (spec「HUD 失败只降表现
    /// 不伤权威追踪」). The rate-limit clock is the host-clock accumulated
    /// seconds the module passes in (the frozen frame seam replaces the old
    /// Time.unscaledTime reads).
    /// </summary>
    internal sealed class HordePresentationAdapter
    {
        /// <summary>The HUD refresh cadence — an implementation constant, not a setting.</summary>
        internal const float UpdateIntervalSeconds = 0.1f; // 10Hz

        private readonly IHordeHudSurface surface;
        private float nextUpdateTime;
        private string lastText = string.Empty;
        private bool lastVisible;

        internal HordePresentationAdapter(IHordeHudSurface surface)
        {
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        /// <summary>The 10Hz render tick (now = host-clock accumulated seconds).
        /// 分支 A（IsActive）：显示并刷新富文本；分支 B：隐藏。</summary>
        internal void Tick(float now)
        {
            if (now < nextUpdateTime) return;
            nextUpdateTime = now + UpdateIntervalSeconds;

            try
            {
                if (!surface.IsLabelReady()) return;

                HordeSnapshot snapshot = HordeStateTracker.Read();
                if (snapshot.IsActive)
                {
                    string text = FormatHudText(snapshot.Location, snapshot.Initiator, snapshot.Killed, snapshot.Total);
                    if (text != lastText)
                    {
                        surface.SetText(text);
                        lastText = text;
                    }

                    if (!lastVisible)
                    {
                        surface.SetVisible(true);
                        lastVisible = true;
                    }
                }
                else if (lastVisible)
                {
                    surface.SetVisible(false);
                    lastVisible = false;
                    lastText = string.Empty;
                }
            }
            catch (Exception error)
            {
                LhtRuntime.LogError("[HordeHud] 表现层异常已隔离（只降表现不伤追踪）: " + error.Message);
            }
        }

        /// <summary>The disconnect-reset drain forwarded to the surface.</summary>
        internal void DrainDisconnectReset()
        {
            try
            {
                surface.DrainDisconnectReset();
            }
            catch (Exception error)
            {
                LhtRuntime.LogError("[HordeHud] 断线重置异常已隔离: " + error.Message);
            }
        }

        /// <summary>The generation-boundary presentation reset.</summary>
        internal void Reset()
        {
            nextUpdateTime = 0f;
            lastText = string.Empty;
            lastVisible = false;
        }

        /// <summary>富文本格式化（旧 HordeHudRenderer 原样迁移）：
        /// "⚠警告 尸潮爆发: {地区名} (发起者: {玩家名}) | 进度: {已击杀}/{总数} 警告⚠"</summary>
        internal static string FormatHudText(string location, string initiator, int killed, int total)
        {
            string safeLocation = string.IsNullOrEmpty(location) ? "未知地点" : location;
            string safeInitiator = string.IsNullOrEmpty(initiator) ? "未知" : initiator;

            return
                "<color=#ff3333>⚠警告 尸潮爆发: </color>" +
                "<color=#ffffff>" + safeLocation + "</color> " +
                "<color=#e6e600>(发起者: " + safeInitiator + ")</color> " +
                "<color=#aaaaaa>| 进度: </color>" +
                "<color=#5ce65c>" + killed + "</color>" +
                "<color=#aaaaaa>/</color>" +
                "<color=#ffffff>" + total + "</color>" +
                " <color=#ff3333>警告⚠</color>";
        }
    }
}
