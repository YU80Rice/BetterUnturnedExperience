using System;
using System.Collections.Generic;
using System.Threading;

// DEV-V2-15: migrated from LaunchInventoryTidy (author: YU80Rice, MIT License,
// Copyright (c) 2026 YU80Rice; local source of truth: Archive/2-未闭环验证项目/LaunchInventoryTidy,
// retired by the wayfinder single-source decision). Attribution:
// docs/third-party/LaunchInventoryTidy-attribution.md. Behavior migrated as-is
// unless a DEV-V2-15 note says otherwise.
namespace BetterUnturnedExperience.Lit
{
    /// <summary>
    /// v2.0.6.10 新增（Codex v2.0.6.9 审计 §三 Medium / §五阻断项 4 修复）：
    /// 可取消且可终态化的主线程任务对象。
    ///
    /// 替代旧的 Queue&lt;Action&gt;：Queue.Clear() 会静默丢弃已入队任务，
    /// 已获得 lease 且尚未执行的请求不会走 CancelNew，也不会回送终态。
    ///
    /// QueuedTidyRequest 携带：
    ///   - Work：要执行的主线程 Action
    ///   - Cancel：插件卸载时调用的取消回调，由调用方提供
    ///     （本地路径仅记录诊断（无账本），联机补偿属 DEV-V2-21）
    ///   - Tag：诊断用标识（如 "TidyRequest"）
    /// </summary>
    internal sealed class QueuedTidyRequest
    {
        public Action Work;
        public Action Cancel;
        public string Tag;
    }

    /// <summary>
    /// v2.0.6.5 新增（Codex v2.0.6.4 审计 §五阻断项 3）：
    /// Unity 主线程调度入口。网络回调只验证和入队，主线程执行端取得带 owner/requestId 的 lease，
    /// 覆盖库存读写和回滚。
    ///
    /// v2.0.6.7 修订（Codex v2.0.6.6 审计 §三 Medium 4 修复）：
    ///   - ProcessAll 新增主线程断言：非主线程调用立即返回并记录 Error
    ///   - Enqueue 新增队列容量上限：MAX_QUEUE_LENGTH=200，超出后丢弃新任务并记录 Error
    ///   - ProcessAll 新增每帧任务数上限：MAX_TASKS_PER_FRAME=10，防止单帧任务过多引起卡顿
    ///   - 未知库存状态隔离：旧插件由网络层检测 ConcurrentMutationAfterCommit 后打开
    ///     TidyFaultCircuit 持久熔断并提供 /tidy_faults 管理员命令；单人路径由
    ///     LocalTidyExecutor 打开模块的 LocalTidyFaultGate（最小熔断），完整电路属 DEV-V2-21
    ///
    /// v2.0.6.8 修订（Codex v2.0.6.7 审计 §三 Critical 1 模板 A 修复）：
    ///   - Enqueue 升级为 TryEnqueue 返回 bool，让调用方能够感知入队失败并执行补偿事务
    ///   - 队列满或 Shutdown 时返回 false，调用方必须释放 lease + MarkResult Failed + 回送 Rejected
    ///
    /// v2.0.6.10 修订（Codex v2.0.6.9 审计 §三 Medium / §五阻断项 4 修复）：
    ///   - Queue&lt;Action&gt; 改为 Queue&lt;QueuedTidyRequest&gt;，支持可取消任务对象
    ///   - Shutdown() 不再直接 Queue.Clear()，而是先 drain 所有 queued request，
    ///     对每个调用 Cancel 回调（释放 lease + MarkResult Failed + 回送终态）
    ///   - 旧 Enqueue(Action) 入口原样迁移保留（当前 DLL 内无调用点），生产代码一律走 TryEnqueue
    ///   - 新 TryEnqueue(QueuedTidyRequest) 接受可取消任务对象
    /// </summary>
    internal static class MainThreadDispatcher
    {
        private const int MAX_QUEUE_LENGTH = 200;
        private const int MAX_TASKS_PER_FRAME = 10;

        private static int _droppedDueToOverflow = 0;
        private static int _rejectedNonMainThreadCalls = 0;
        private static int _cancelledDuringShutdown = 0;  // v2.0.6.10 新增

        private static volatile bool _shuttingDown;

        // v2.0.6.10：Queue<Action> 改为 Queue<QueuedTidyRequest>
        private static readonly Queue<QueuedTidyRequest> _queue = new Queue<QueuedTidyRequest>();
        private static readonly object _lock = new object();

        /// <summary>
        /// v2.0.6.10 新增：尝试入队可取消的任务对象。
        /// 返回 false 时调用方必须执行补偿事务（或依赖 Cancel 回调在 Shutdown 时被调用）。
        /// </summary>
        public static bool TryEnqueue(QueuedTidyRequest request)
        {
            if (request == null || request.Work == null) return false;
            lock (_lock)
            {
                if (_shuttingDown)
                {
                    LitRuntime.LogWarning(
                        "[Tidy] MainThreadDispatcher.TryEnqueue 拒绝入队：插件正在卸载");
                    return false;
                }
                if (_queue.Count >= MAX_QUEUE_LENGTH)
                {
                    System.Threading.Interlocked.Increment(ref _droppedDueToOverflow);
                    LitRuntime.LogError(
                        $"[Tidy] MainThreadDispatcher.TryEnqueue 队列已满（{MAX_QUEUE_LENGTH}），拒绝入队。" +
                        $"调用方必须执行补偿事务（CancelNew：释放 lease + MarkResult Failed + 回送 Rejected）。" +
                        $"累计丢弃计数={System.Threading.Volatile.Read(ref _droppedDueToOverflow)}");
                    return false;
                }
                _queue.Enqueue(request);
                return true;
            }
        }

        /// <summary>
        /// v2.0.6.8 保留（Codex v2.0.6.7 审计 §三 Critical 1 模板 A 修复）：
        /// 尝试入队一个 Action，返回是否成功。
        /// v2.0.6.10：内部包装为 QueuedTidyRequest（Cancel=null，调用方仍需处理 Enqueue 失败）。
        /// </summary>
        public static bool TryEnqueue(Action work)
        {
            if (work == null) return false;
            return TryEnqueue(new QueuedTidyRequest { Work = work, Cancel = null, Tag = "ActionOnly" });
        }

        /// <summary>
        /// v2.0.6.7 旧入口（fire-and-forget），内部转 TryEnqueue 并丢弃结果。
        /// 迁入后的生产代码一律走 TryEnqueue 并处理失败；本方法仅为旧调用形状保留。
        /// </summary>
        public static void Enqueue(Action action)
        {
            TryEnqueue(action);
        }

        /// <summary>
        /// 由宿主每帧 Update() 在 Unity 主线程中调用。
        /// 取出并执行待处理任务，每个任务独立 try-catch，单个失败不影响后续。
        /// </summary>
        public static void ProcessAll()
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            int mainThreadId = LitRuntime.MainThreadId;
            if (currentThreadId != mainThreadId)
            {
                System.Threading.Interlocked.Increment(ref _rejectedNonMainThreadCalls);
                LitRuntime.LogError(
                    $"[Tidy] MainThreadDispatcher.ProcessAll 被非主线程调用（current={currentThreadId}, expected={mainThreadId}），拒绝执行。" +
                    $"累计拒绝计数={System.Threading.Volatile.Read(ref _rejectedNonMainThreadCalls)}");
                return;
            }

#if TIDY_TEST_HARNESS
            // v2.0.6.13 Codex 第三轮 §3.1：SP-SD 屏障真正冻结取队列，保证被测请求保留在队列中
            // 直到测试主动 ReleaseHold + 触发 Shutdown drain，使真实 CancelNew 回调可被验证
            if (ShutdownBarrier.ShouldHoldNextQueuedRequest())
            {
                return;
            }
#endif

            QueuedTidyRequest[] batch = null;
            int count;
            lock (_lock)
            {
                count = _queue.Count;
                if (count == 0) return;
                int processCount = Math.Min(count, MAX_TASKS_PER_FRAME);
                batch = new QueuedTidyRequest[processCount];
                for (int i = 0; i < processCount; i++)
                    batch[i] = _queue.Dequeue();
            }

            for (int i = 0; i < batch.Length; i++)
            {
                try
                {
                    batch[i]?.Work?.Invoke();
                }
                catch (Exception e)
                {
                    LitRuntime.LogError(
                        $"[Tidy] MainThreadDispatcher 任务异常: {e}");
                }
            }
        }

        /// <summary>测试/诊断用：当前待处理任务数。</summary>
        internal static int PendingCount
        {
            get
            {
                lock (_lock) return _queue.Count;
            }
        }

        internal static int DroppedDueToOverflowCount => System.Threading.Volatile.Read(ref _droppedDueToOverflow);
        internal static int RejectedNonMainThreadCallCount => System.Threading.Volatile.Read(ref _rejectedNonMainThreadCalls);
        internal static int CancelledDuringShutdownCount => System.Threading.Volatile.Read(ref _cancelledDuringShutdown);

        /// <summary>
        /// v2.0.6.10 重写（Codex v2.0.6.9 审计 §三 Medium / §五阻断项 4 修复）：
        /// 插件卸载时调用。不再直接 Queue.Clear()，而是：
        ///   1. 设置 _shuttingDown=true，阻止新任务入队
        ///   2. 从队列 drain 所有 queued request
        ///   3. 对每个有 Cancel 回调的 request 调用 Cancel，执行补偿事务
        ///      （本地路径无账本补偿，仅记录诊断；联机补偿属 DEV-V2-21）
        ///   4. 若 transport 已不可用，至少保留 ledger 终态至 TTL，
        ///      明确客户端 pending 会超时
        /// 由模块 Stop 调用。
        /// </summary>
        internal static void Shutdown()
        {
            QueuedTidyRequest[] drained = null;
            int drainedCount = 0;
            lock (_lock)
            {
                _shuttingDown = true;
                drainedCount = _queue.Count;
                if (drainedCount > 0)
                {
                    drained = new QueuedTidyRequest[drainedCount];
                    for (int i = 0; i < drainedCount; i++)
                        drained[i] = _queue.Dequeue();
                }
            }

            if (drained != null && drainedCount > 0)
            {
                LitRuntime.LogInfo(
                    $"[Tidy] MainThreadDispatcher.Shutdown：drain {drainedCount} 个已入队任务，对每个执行 Cancel 回调");

                for (int i = 0; i < drained.Length; i++)
                {
                    var req = drained[i];
                    if (req == null) continue;
                    if (req.Cancel != null)
                    {
                        try
                        {
                            req.Cancel.Invoke();
                            System.Threading.Interlocked.Increment(ref _cancelledDuringShutdown);
                        }
                        catch (Exception e)
                        {
                            LitRuntime.LogError(
                                $"[Tidy] MainThreadDispatcher.Shutdown Cancel 回调异常 (tag={req.Tag}): {e}");
                        }
                    }
                    else
                    {
                        // 无 Cancel 回调（旧 Enqueue(Action) 路径）：仅记录日志
                        LitRuntime.LogWarning(
                            $"[Tidy] MainThreadDispatcher.Shutdown：任务无 Cancel 回调 (tag={req.Tag})，静默丢弃");
                    }
                }
            }

            LitRuntime.LogInfo(
                $"[Tidy] MainThreadDispatcher 已关闭，累计 Shutdown 期间取消任务数={System.Threading.Volatile.Read(ref _cancelledDuringShutdown)}");
        }

        /// <summary>插件卸载时清空队列，防止遗留任务在卸载后执行。</summary>
        internal static void ClearAll()
        {
            Shutdown();
        }

        /// <summary>
        /// Generation re-arm: a module generation boundary (Start) reopens
        /// the queue after a previous generation's Stop shut it down. The
        /// static queue belongs to the module generation, not the process —
        /// without this, a rearmed module would face a permanently closed
        /// queue (BepInEx reload / new registration in the same process).
        /// The queue is empty after Shutdown's drain, so reopening is just
        /// the flag; idempotent while already open.
        /// </summary>
        internal static void EnsureOpen()
        {
            lock (_lock)
            {
                _shuttingDown = false;
            }
        }

        /// <summary>
        /// Host-test seam only: a host test that exercised Stop needs an
        /// explicit reset (flag + queue) before another module instance in
        /// the same test process drives the queue. Production reopens the
        /// generation queue via EnsureOpen at the module start boundary.
        /// </summary>
        internal static void ResetForTests()
        {
            lock (_lock)
            {
                _shuttingDown = false;
                _queue.Clear();
            }
        }
    }
}
