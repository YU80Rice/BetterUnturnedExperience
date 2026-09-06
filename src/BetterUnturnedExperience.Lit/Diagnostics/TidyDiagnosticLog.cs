using System;
using System.Collections.Generic;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// 正常运行诊断的低频日志出口。
    /// 错误、阻断和安全拒绝绝不调用此类；它们必须保持原有的直接/安全节流输出。
    /// </summary>
    internal static class TidyDiagnosticLog
    {
        private const double NormalIntervalSeconds = 15.0;
        private const int MaxCategories = 64;

        private struct Window
        {
            internal DateTime LastLogAtUtc;
            internal int SuppressedCount;
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Window> Windows =
            new Dictionary<string, Window>(StringComparer.Ordinal);

        /// <summary>
        /// 同一类别在 15 秒内仅输出首条正常诊断；窗口结束后的下一条会携带抑制计数。
        /// </summary>
        internal static void Info(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            string key = string.IsNullOrWhiteSpace(category) ? "general" : category;
            DateTime now = DateTime.UtcNow;
            bool shouldWrite = false;
            int suppressed = 0;

            lock (Gate)
            {
                if (Windows.TryGetValue(key, out Window window))
                {
                    if ((now - window.LastLogAtUtc).TotalSeconds < NormalIntervalSeconds)
                    {
                        window.SuppressedCount++;
                        Windows[key] = window;
                        return;
                    }

                    suppressed = window.SuppressedCount;
                    Windows[key] = new Window
                    {
                        LastLogAtUtc = now,
                        SuppressedCount = 0,
                    };
                    shouldWrite = true;
                }
                else
                {
                    if (Windows.Count >= MaxCategories)
                    {
                        string oldestKey = null;
                        DateTime oldestAt = DateTime.MaxValue;
                        foreach (KeyValuePair<string, Window> pair in Windows)
                        {
                            if (pair.Value.LastLogAtUtc < oldestAt)
                            {
                                oldestAt = pair.Value.LastLogAtUtc;
                                oldestKey = pair.Key;
                            }
                        }

                        if (oldestKey != null) Windows.Remove(oldestKey);
                    }

                    Windows[key] = new Window
                    {
                        LastLogAtUtc = now,
                        SuppressedCount = 0,
                    };
                    shouldWrite = true;
                }
            }

            if (shouldWrite)
            {
                LitRuntime.LogInfo(
                    "[Tidy] " + message +
                    (suppressed > 0 ? "（已抑制 " + suppressed + " 条同类正常诊断）" : ""));
            }
        }

    }
}
