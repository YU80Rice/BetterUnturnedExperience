using Bue.FrameFenceSpike;

var tests = new (string Name, Action Run)[]
{
    ("old queued action is handled but cannot mutate after reconnect", Tests.OldQueuedActionIsRejectedAfterReconnect),
    ("generation mismatch is rejected", Tests.GenerationMismatchIsRejected),
    ("snapshot mismatch is rejected", Tests.SnapshotMismatchIsRejected),
    ("nonce mismatch is rejected", Tests.NonceMismatchIsRejected),
    ("combined mismatch is rejected", Tests.CombinedMismatchIsRejected),
    ("out-of-order stale frame is rejected after current frame", Tests.OutOfOrderStaleFrameIsRejected),
    ("duplicate current request mutates at most once", Tests.DuplicateCurrentRequestMutatesOnce),
    ("same request id is legal in a new connection generation", Tests.SameRequestIdIsLegalInNewGeneration),
    ("replay window is strictly bounded and fails closed", Tests.ReplayWindowIsStrictlyBounded),
    ("request id cannot substitute for connection identity", Tests.RequestIdCannotSubstituteForConnectionIdentity),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS | {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL | {test.Name} | {exception.Message}");
    }
}

Console.WriteLine($"SUMMARY | total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static class Tests
{
    public static void OldQueuedActionIsRejectedAfterReconnect()
    {
        var oldBinding = ReadyBinding.Create(41, 701);
        var oldFrame = ReadyFrame.Create(oldBinding, 9001, "old");
        var queued = new FakeDispatcher();
        var sink = new InMemorySettingsMutationGate();
        var fence = new ReadyFrameFence(sink);

        queued.Enqueue(() => fence.Handle(oldFrame));
        _ = ReadyBinding.Create(42, 702);
        fence.ReplaceBinding(ReadyBinding.Create(42, 702));
        queued.Drain();

        Equal(1, fence.HandlerInvocationCount, "handler invocation count");
        Equal(0, sink.MutationCount, "mutation count");
        Equal(FrameDecision.StaleGeneration, fence.LastDecision, "decision");
    }

    public static void GenerationMismatchIsRejected() => AssertRejectedMismatch(Mismatch.Generation, FrameDecision.StaleGeneration);
    public static void SnapshotMismatchIsRejected() => AssertRejectedMismatch(Mismatch.Snapshot, FrameDecision.StaleSnapshot);
    public static void NonceMismatchIsRejected() => AssertRejectedMismatch(Mismatch.Nonce, FrameDecision.NonceMismatch);
    public static void CombinedMismatchIsRejected() => AssertRejectedMismatch(Mismatch.All, FrameDecision.StaleGeneration);

    public static void OutOfOrderStaleFrameIsRejected()
    {
        var oldBinding = ReadyBinding.Create(10, 100);
        var currentBinding = ReadyBinding.Create(11, 101);
        var sink = new InMemorySettingsMutationGate();
        var fence = new ReadyFrameFence(sink);
        fence.ReplaceBinding(currentBinding);

        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(currentBinding, 2, "current")), "current result");
        Equal(FrameDecision.StaleGeneration, fence.Handle(ReadyFrame.Create(oldBinding, 1, "late-old")), "late old result");
        Equal(1, sink.MutationCount, "only current mutation");
    }

    public static void DuplicateCurrentRequestMutatesOnce()
    {
        var binding = ReadyBinding.Create(5, 50);
        var sink = new InMemorySettingsMutationGate();
        var fence = new ReadyFrameFence(sink);
        fence.ReplaceBinding(binding);
        var frame = ReadyFrame.Create(binding, 77, "same-payload");

        Equal(FrameDecision.Applied, fence.Handle(frame), "first result");
        Equal(FrameDecision.Duplicate, fence.Handle(frame), "duplicate result");
        Equal(1, sink.MutationCount, "mutation count");
    }

    public static void SameRequestIdIsLegalInNewGeneration()
    {
        var first = ReadyBinding.Create(30, 300);
        var second = ReadyBinding.Create(31, 301);
        var sink = new InMemorySettingsMutationGate(2);
        var fence = new ReadyFrameFence(sink);

        fence.ReplaceBinding(first);
        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(first, 55, "first-generation")), "first generation result");
        fence.ReplaceBinding(second);
        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(second, 55, "second-generation")), "second generation result");
        Equal(2, sink.MutationCount, "lifetime mutation count");
        Equal(1, sink.ReplayEntryCount, "only current generation replay entry remains");
    }

    public static void ReplayWindowIsStrictlyBounded()
    {
        var binding = ReadyBinding.Create(40, 400);
        var sink = new InMemorySettingsMutationGate(2);
        var fence = new ReadyFrameFence(sink);
        fence.ReplaceBinding(binding);

        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(binding, 1, "one")), "first result");
        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(binding, 2, "two")), "second result");
        Equal(FrameDecision.ReplayWindowFull, fence.Handle(ReadyFrame.Create(binding, 3, "three")), "overflow result");
        Equal(FrameDecision.Duplicate, fence.Handle(ReadyFrame.Create(binding, 1, "one")), "known duplicate after overflow");
        Equal(2, sink.MutationCount, "mutation count");
        Equal(2, sink.ReplayEntryCount, "bounded replay entry count");
    }

    public static void RequestIdCannotSubstituteForConnectionIdentity()
    {
        var oldBinding = ReadyBinding.Create(20, 200);
        var currentBinding = ReadyBinding.Create(21, 201);
        var sink = new InMemorySettingsMutationGate();
        var fence = new ReadyFrameFence(sink);
        fence.ReplaceBinding(currentBinding);

        Equal(FrameDecision.Applied, fence.Handle(ReadyFrame.Create(currentBinding, 88, "new")), "current result");
        Equal(FrameDecision.StaleGeneration, fence.Handle(ReadyFrame.Create(oldBinding, 88, "old")), "same request id stale result");
        Equal(1, sink.MutationCount, "mutation count");
    }

    private static void AssertRejectedMismatch(Mismatch mismatch, FrameDecision expected)
    {
        var current = ReadyBinding.Create(7, 70);
        var foreign = ReadyBinding.Create(8, 80);
        var frame = ReadyFrame.CreateWithIdentity(
            mismatch is Mismatch.Generation or Mismatch.All ? foreign.ConnectionGeneration : current.ConnectionGeneration,
            mismatch is Mismatch.Snapshot or Mismatch.All ? foreign.SnapshotId : current.SnapshotId,
            mismatch is Mismatch.Nonce or Mismatch.All ? foreign.CopyNonce() : current.CopyNonce(),
            123,
            "payload");
        var sink = new InMemorySettingsMutationGate();
        var fence = new ReadyFrameFence(sink);
        fence.ReplaceBinding(current);

        Equal(expected, fence.Handle(frame), "decision");
        Equal(1, fence.HandlerInvocationCount, "handler invocation count");
        Equal(0, sink.MutationCount, "mutation count");
    }

    private static void Equal<T>(T expected, T actual, string label) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}");
    }

    private enum Mismatch { Generation, Snapshot, Nonce, All }
}

sealed class FakeDispatcher
{
    private readonly Queue<Action> queue = new();
    public void Enqueue(Action action) => queue.Enqueue(action);
    public void Drain()
    {
        while (queue.TryDequeue(out var action)) action();
    }
}
