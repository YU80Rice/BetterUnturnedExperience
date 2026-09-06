using System.Security.Cryptography;
using System.Text;

namespace Bue.FrameFenceSpike;

public enum FrameDecision
{
    Applied,
    Duplicate,
    RequestIdConflict,
    ReplayWindowFull,
    NoReadyBinding,
    StaleGeneration,
    StaleSnapshot,
    NonceMismatch,
}

public sealed class ReadyBinding
{
    private const int NonceLength = 32;
    private readonly byte[] nonce;

    private ReadyBinding(ulong connectionGeneration, ulong snapshotId, byte[] nonce)
    {
        if (connectionGeneration == 0) throw new ArgumentOutOfRangeException(nameof(connectionGeneration));
        if (snapshotId == 0) throw new ArgumentOutOfRangeException(nameof(snapshotId));
        if (nonce.Length != NonceLength) throw new ArgumentException("Nonce must be 32 bytes.", nameof(nonce));

        ConnectionGeneration = connectionGeneration;
        SnapshotId = snapshotId;
        this.nonce = (byte[])nonce.Clone();
    }

    public ulong ConnectionGeneration { get; }
    public ulong SnapshotId { get; }

    public static ReadyBinding Create(ulong connectionGeneration, ulong snapshotId)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        return new ReadyBinding(connectionGeneration, snapshotId, nonce);
    }

    public byte[] CopyNonce() => (byte[])nonce.Clone();

    internal bool HasNonce(ReadOnlySpan<byte> candidate) =>
        candidate.Length == nonce.Length && CryptographicOperations.FixedTimeEquals(candidate, nonce);
}

public sealed class ReadyFrame
{
    private readonly byte[] nonce;

    private ReadyFrame(
        ulong connectionGeneration,
        ulong snapshotId,
        byte[] nonce,
        ulong requestId,
        string payload)
    {
        ConnectionGeneration = connectionGeneration;
        SnapshotId = snapshotId;
        this.nonce = (byte[])nonce.Clone();
        RequestId = requestId;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public ulong ConnectionGeneration { get; }
    public ulong SnapshotId { get; }
    public ulong RequestId { get; }
    public string Payload { get; }

    public static ReadyFrame Create(ReadyBinding binding, ulong requestId, string payload)
    {
        ArgumentNullException.ThrowIfNull(binding);
        return CreateWithIdentity(
            binding.ConnectionGeneration,
            binding.SnapshotId,
            binding.CopyNonce(),
            requestId,
            payload);
    }

    public static ReadyFrame CreateWithIdentity(
        ulong connectionGeneration,
        ulong snapshotId,
        byte[] nonce,
        ulong requestId,
        string payload) =>
        new(connectionGeneration, snapshotId, nonce, requestId, payload);

    internal ReadOnlySpan<byte> Nonce => nonce;
}

public sealed class ReadyFrameFence
{
    private readonly InMemorySettingsMutationGate mutationGate;
    private ReadyBinding? currentBinding;

    public ReadyFrameFence(InMemorySettingsMutationGate mutationGate)
    {
        this.mutationGate = mutationGate ?? throw new ArgumentNullException(nameof(mutationGate));
    }

    public int HandlerInvocationCount { get; private set; }
    public FrameDecision LastDecision { get; private set; } = FrameDecision.NoReadyBinding;

    public void ReplaceBinding(ReadyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        mutationGate.RotateToGeneration(binding.ConnectionGeneration);
        currentBinding = binding;
    }

    public FrameDecision Handle(ReadyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        HandlerInvocationCount++;

        if (currentBinding is null)
            return SetDecision(FrameDecision.NoReadyBinding);
        if (frame.ConnectionGeneration != currentBinding.ConnectionGeneration)
            return SetDecision(FrameDecision.StaleGeneration);
        if (frame.SnapshotId != currentBinding.SnapshotId)
            return SetDecision(FrameDecision.StaleSnapshot);
        if (!currentBinding.HasNonce(frame.Nonce))
            return SetDecision(FrameDecision.NonceMismatch);

        return SetDecision(mutationGate.TryMutate(frame));
    }

    private FrameDecision SetDecision(FrameDecision decision)
    {
        LastDecision = decision;
        return decision;
    }
}

public sealed class InMemorySettingsMutationGate
{
    private readonly Dictionary<ulong, byte[]> acceptedRequests = new();
    private readonly int replayCapacity;
    private ulong activeConnectionGeneration;

    public InMemorySettingsMutationGate(int replayCapacity = 128)
    {
        if (replayCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(replayCapacity));
        this.replayCapacity = replayCapacity;
    }

    public int MutationCount { get; private set; }
    public int ReplayEntryCount => acceptedRequests.Count;

    public void RotateToGeneration(ulong connectionGeneration)
    {
        if (connectionGeneration == 0) throw new ArgumentOutOfRangeException(nameof(connectionGeneration));
        if (connectionGeneration == activeConnectionGeneration) return;

        acceptedRequests.Clear();
        activeConnectionGeneration = connectionGeneration;
    }

    public FrameDecision TryMutate(ReadyFrame frame)
    {
        if (frame.ConnectionGeneration != activeConnectionGeneration)
            return FrameDecision.StaleGeneration;

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(frame.Payload));
        if (acceptedRequests.TryGetValue(frame.RequestId, out var priorDigest))
        {
            return CryptographicOperations.FixedTimeEquals(digest, priorDigest)
                ? FrameDecision.Duplicate
                : FrameDecision.RequestIdConflict;
        }

        if (acceptedRequests.Count >= replayCapacity)
            return FrameDecision.ReplayWindowFull;

        acceptedRequests.Add(frame.RequestId, digest);
        MutationCount++;
        return FrameDecision.Applied;
    }
}
