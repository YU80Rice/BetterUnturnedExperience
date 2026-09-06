namespace PtRt05.ConnectionTokens;

public sealed class ConnectionCapabilityOwner
{
    public ConnectionLease Open(ulong diagnosticSteamId) =>
        new(diagnosticSteamId, new ConnectionReceiveContext());
}

/// <summary>Prototype owner handle. Production consumers receive only the context.</summary>
public sealed class ConnectionLease
{
    private readonly ConnectionReceiveContext context;

    internal ConnectionLease(ulong diagnosticSteamId, ConnectionReceiveContext context)
    {
        DiagnosticSteamId = diagnosticSteamId;
        this.context = context;
    }

    public ulong DiagnosticSteamId { get; }

    public Action CaptureReceive(Action mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        return () => context.TryInvokeIfCurrent(mutation);
    }

    public void Revoke()
    {
        CloseAdmission();
        WaitForQuiescence();
    }

    internal void CloseAdmission() => context.CloseAdmission();
    internal void WaitForQuiescence() => context.WaitForQuiescence();
}

/// <summary>
/// Opaque connection-bound consumer context. It is non-defaultable, has no
/// public constructor and exposes no revoke operation.
/// </summary>
public sealed class ConnectionReceiveContext
{
    private readonly object gate = new();
    private bool admissionOpen = true;
    private int activeMutations;

    internal ConnectionReceiveContext()
    {
    }

    public bool TryInvokeIfCurrent(Action mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        lock (gate)
        {
            if (!admissionOpen)
                return false;
            activeMutations++;
        }

        try
        {
            // Arbitrary consumer code runs outside capability and manager locks.
            mutation();
            return true;
        }
        finally
        {
            lock (gate)
            {
                activeMutations--;
                if (activeMutations == 0)
                    Monitor.PulseAll(gate);
            }
        }
    }

    internal void CloseAdmission()
    {
        lock (gate)
            admissionOpen = false;
    }

    internal void WaitForQuiescence()
    {
        lock (gate)
        {
            while (activeMutations != 0)
                Monitor.Wait(gate);
        }
    }
}

public static class ConnectionBoundDispatch
{
    public static bool TryInvoke(ConnectionReceiveContext? context, Action mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        return context is not null && context.TryInvokeIfCurrent(mutation);
    }
}

/// <summary>
/// Safe current map: close admission and remove the exact lease atomically
/// under the manager lock, then wait outside the manager lock.
/// </summary>
public sealed class CurrentConnectionMap
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ConnectionLease> current = new();
    private readonly ConnectionCapabilityOwner owner = new();

    public ConnectionLease Publish(ulong steamId)
    {
        lock (gate)
        {
            if (current.ContainsKey(steamId))
                throw new InvalidOperationException("a current connection is already published");
            var lease = owner.Open(steamId);
            current.Add(steamId, lease);
            return lease;
        }
    }

    public void Disconnect(ConnectionLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        lock (gate)
        {
            if (!current.TryGetValue(lease.DiagnosticSteamId, out var mapped) ||
                !ReferenceEquals(mapped, lease))
                return;

            // Disconnect linearizes here, before removal/reconnect publication.
            lease.CloseAdmission();
            current.Remove(lease.DiagnosticSteamId);
        }

        // Never wait for consumer work while holding the manager lock.
        lease.WaitForQuiescence();
    }

    public ConnectionLease Current(ulong steamId)
    {
        lock (gate)
            return current[steamId];
    }
}

/// <summary>Unsafe remove -> reconnect -> old callback -> late revoke model.</summary>
public sealed class UnsafeLateRevokeConnectionMap
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ConnectionLease> current = new();
    private readonly ConnectionCapabilityOwner owner = new();

    public ConnectionLease Publish(ulong steamId)
    {
        lock (gate)
        {
            var lease = owner.Open(steamId);
            current[steamId] = lease;
            return lease;
        }
    }

    public void RemoveWithoutRevoke(ConnectionLease lease)
    {
        lock (gate)
        {
            if (current.TryGetValue(lease.DiagnosticSteamId, out var mapped) &&
                ReferenceEquals(mapped, lease))
                current.Remove(lease.DiagnosticSteamId);
        }
    }
}

public sealed class DeterministicDispatcher
{
    private readonly Queue<Action> queue = new();
    public void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        queue.Enqueue(action);
    }
    public Action Dequeue() => queue.Dequeue();
    public void DrainOne() => Dequeue()();
}
