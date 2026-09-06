using PtRt05.ConnectionTokens;

var tests = new (string Name, Action Body)[]
{
    ("revoke-before-dequeue rejects the queued mutation", RevokeBeforeDequeue),
    ("dequeue-before-revoke completes before revocation", DequeueBeforeRevoke),
    ("dequeued callback paused before token entry loses to revoke", InterleavedRevokeWins),
    ("same SteamID reconnect cannot revive the old capability", SameSteamIdReconnect),
    ("revoked token reuse remains rejected", RevokedTokenReuse),
    ("disconnect-reconnect-old-callback-revoke exposes unsafe owner ordering", UnsafeOwnerOrdering),
    ("default or caller-forged capability cannot be supplied", CapabilitySurfaceIsOpaque),
    ("revoke waits for an entered mutation and rejects every later mutation", RevokeWaitsForEnteredMutation),
    ("safe current-map disconnect closes admission before reconnect publication", SafeCurrentMapOrdering),
    ("late revoke after reconnect publication leaves an exploitable window", LateRevokeOrdering),
    ("consumer context exposes no revoke operation", ConsumerContextCannotRevoke),
    ("null receive context rejects mutation fail-closed", NullContextFailsClosed),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS | {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL | {test.Name} | {exception.Message}");
        Console.WriteLine(failures[^1]);
    }
}

Console.WriteLine($"RESULT | passed={tests.Length - failures.Count} failed={failures.Count}");
return failures.Count == 0 ? 0 : 1;

static void RevokeBeforeDequeue()
{
    var owner = new ConnectionCapabilityOwner();
    var dispatcher = new DeterministicDispatcher();
    var connection = owner.Open(76561198000000001UL);
    var mutations = 0;

    dispatcher.Enqueue(connection.CaptureReceive(() => mutations++));
    connection.Revoke();
    dispatcher.DrainOne();

    Equal(0, mutations);
}

static void DequeueBeforeRevoke()
{
    var owner = new ConnectionCapabilityOwner();
    var dispatcher = new DeterministicDispatcher();
    var connection = owner.Open(76561198000000001UL);
    var mutations = 0;

    dispatcher.Enqueue(connection.CaptureReceive(() => mutations++));
    dispatcher.DrainOne();
    connection.Revoke();

    Equal(1, mutations);
}

static void InterleavedRevokeWins()
{
    var owner = new ConnectionCapabilityOwner();
    var dispatcher = new DeterministicDispatcher();
    var connection = owner.Open(76561198000000001UL);
    var mutations = 0;

    dispatcher.Enqueue(connection.CaptureReceive(() => mutations++));
    var dequeued = dispatcher.Dequeue();
    connection.Revoke();
    dequeued();

    Equal(0, mutations);
}

static void SameSteamIdReconnect()
{
    var owner = new ConnectionCapabilityOwner();
    var oldConnection = owner.Open(76561198000000001UL);
    var oldCallback = oldConnection.CaptureReceive(() => throw new InvalidOperationException("stale mutation reached"));
    oldConnection.Revoke();
    var newConnection = owner.Open(76561198000000001UL);
    var newMutations = 0;

    oldCallback();
    newConnection.CaptureReceive(() => newMutations++)();

    Equal(1, newMutations);
    NotSame(oldConnection, newConnection);
}

static void RevokedTokenReuse()
{
    var owner = new ConnectionCapabilityOwner();
    var connection = owner.Open(76561198000000001UL);
    var mutations = 0;
    var callback = connection.CaptureReceive(() => mutations++);

    connection.Revoke();
    callback();
    callback();

    Equal(0, mutations);
}

static void UnsafeOwnerOrdering()
{
    var owner = new ConnectionCapabilityOwner();
    var dispatcher = new DeterministicDispatcher();
    var oldConnection = owner.Open(76561198000000001UL);
    var mutations = 0;
    dispatcher.Enqueue(oldConnection.CaptureReceive(() => mutations++));

    // This deliberately models the forbidden ordering: reconnect is published
    // before the old lease is revoked, then the old callback runs.
    _ = owner.Open(76561198000000001UL);
    dispatcher.DrainOne();
    oldConnection.Revoke();

    Equal(1, mutations);
}

static void CapabilitySurfaceIsOpaque()
{
    var type = typeof(ConnectionCapabilityOwner).Assembly.GetType(
        "PtRt05.ConnectionTokens.ConnectionReceiveContext",
        throwOnError: true)!;

    Equal(false, type.IsValueType);
    Equal(0, type.GetConstructors().Length);
}

static void RevokeWaitsForEnteredMutation()
{
    var owner = new ConnectionCapabilityOwner();
    var connection = owner.Open(76561198000000001UL);
    using var mutationEntered = new ManualResetEventSlim();
    using var allowMutationExit = new ManualResetEventSlim();
    var mutations = 0;

    var callback = connection.CaptureReceive(() =>
    {
        mutationEntered.Set();
        allowMutationExit.Wait();
        Interlocked.Increment(ref mutations);
    });

    var callbackTask = Task.Run(callback);
    True(mutationEntered.Wait(TimeSpan.FromSeconds(5)), "mutation did not enter");
    var revokeTask = Task.Run(connection.Revoke);
    False(revokeTask.Wait(TimeSpan.FromMilliseconds(100)), "revoke returned while mutation was active");

    allowMutationExit.Set();
    True(Task.WaitAll(new[] { callbackTask, revokeTask }, TimeSpan.FromSeconds(5)), "tasks did not finish");
    callback();

    Equal(1, mutations);
}

static void SafeCurrentMapOrdering()
{
    var manager = new CurrentConnectionMap();
    var dispatcher = new DeterministicDispatcher();
    var oldConnection = manager.Publish(76561198000000001UL);
    var mutations = 0;
    dispatcher.Enqueue(oldConnection.CaptureReceive(() => mutations++));

    manager.Disconnect(oldConnection);
    var newConnection = manager.Publish(76561198000000001UL);
    dispatcher.DrainOne();

    Equal(0, mutations);
    Same(newConnection, manager.Current(76561198000000001UL));
}

static void LateRevokeOrdering()
{
    var manager = new UnsafeLateRevokeConnectionMap();
    var dispatcher = new DeterministicDispatcher();
    var oldConnection = manager.Publish(76561198000000001UL);
    var mutations = 0;
    dispatcher.Enqueue(oldConnection.CaptureReceive(() => mutations++));

    manager.RemoveWithoutRevoke(oldConnection);
    _ = manager.Publish(76561198000000001UL);
    dispatcher.DrainOne();
    oldConnection.Revoke();

    Equal(1, mutations);
}

static void ConsumerContextCannotRevoke()
{
    var publicMethods = typeof(ConnectionReceiveContext).GetMethods()
        .Where(method => method.DeclaringType == typeof(ConnectionReceiveContext))
        .Select(method => method.Name)
        .ToArray();

    Equal(false, publicMethods.Contains("Revoke", StringComparer.Ordinal));
}

static void NullContextFailsClosed()
{
    var mutations = 0;
    var accepted = ConnectionBoundDispatch.TryInvoke(null, () => mutations++);
    Equal(false, accepted);
    Equal(0, mutations);
}

static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"expected={expected}, actual={actual}");
}

static void NotSame(object expectedDifferent, object actual)
{
    if (ReferenceEquals(expectedDifferent, actual))
        throw new InvalidOperationException("expected distinct connection capabilities");
}

static void Same(object expected, object actual)
{
    if (!ReferenceEquals(expected, actual))
        throw new InvalidOperationException("expected identical reference");
}

static void True(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void False(bool condition, string message) => True(!condition, message);
