using System;
using System.Collections.Generic;

namespace BetterUnturnedExperience.Contracts
{
    public readonly struct FeatureId { public string Value { get; } public FeatureId(string value) { Value = value; } }
    public readonly struct ContractVersion { public ushort Major { get; } public ushort Minor { get; } public ContractVersion(ushort major, ushort minor) { Major = major; Minor = minor; } }
    public readonly struct FeatureDependency { public FeatureId Feature { get; } public Version MinimumFeatureVersion { get; } public ContractVersion MinimumContract { get; } public bool Required { get; } }
    public readonly struct Digest256 { public ulong Part0 { get; } public ulong Part1 { get; } public ulong Part2 { get; } public ulong Part3 { get; } public Digest256(ulong part0, ulong part1, ulong part2, ulong part3) { Part0 = part0; Part1 = part1; Part2 = part2; Part3 = part3; } }
    public readonly struct FeatureScopeIdentity { public FeatureId Id { get; } public Version FeatureVersion { get; } public string CurrentSlug { get; } public string DefinitionSetId { get; } public Digest256 DefinitionSetDigest { get; } }

    public enum FeatureRegistrationPhase : byte { HostStarting = 0, RegistrationOpen = 1, CatalogFrozen = 2, RuntimeReady = 3, CoreSafeMode = 4 }
    public enum FeatureRegistrationReason : ushort
    {
        None = 0, HostUnavailable = 100, PhaseClosed = 101, DuplicateFeature = 200, InvalidDefinitionArtifact = 201,
        ContractIncompatible = 202, MissingRequiredDependency = 203, InvalidModuleFactory = 204,
        InvalidClientUiRegistration = 205, PreflightRejected = 300, CoreUnavailable = 900
    }

    public sealed class FeatureDefinitionArtifact
    {
        public FeatureId Feature { get; }
        public ushort FormatVersion { get; }
        public string DefinitionSetId { get; }
        public Digest256 DefinitionSetDigest { get; }
        public Digest256 ArtifactPayloadDigest { get; }
        public IReadOnlyList<byte> CanonicalPayload { get; }

        public FeatureDefinitionArtifact(FeatureId feature, ushort formatVersion, string definitionSetId, Digest256 definitionSetDigest, Digest256 artifactPayloadDigest, IEnumerable<byte> canonicalPayload)
        {
            Feature = feature;
            FormatVersion = formatVersion;
            DefinitionSetId = definitionSetId;
            DefinitionSetDigest = definitionSetDigest;
            ArtifactPayloadDigest = artifactPayloadDigest;
            CanonicalPayload = new System.Collections.ObjectModel.ReadOnlyCollection<byte>((canonicalPayload ?? new byte[0]) is byte[] bytes ? (byte[])bytes.Clone() : new List<byte>(canonicalPayload ?? new byte[0]).ToArray());
        }
    }

    public interface IFeatureModuleFactory { IFeatureModule Create(); }
    public interface IClientUiSatelliteRegistration
    {
        string SatelliteId { get; }
        ContractVersion MinimumBueContract { get; }
        string RegistrationToken { get; }
    }
    public interface IFeatureRegistration
    {
        FeatureDefinitionArtifact Definition { get; }
        ContractVersion MinimumBueContract { get; }
        IFeatureModuleFactory ModuleFactory { get; }
        IClientUiSatelliteRegistration ClientUi { get; }
    }
    public interface IBueFeatureRegistrationHost
    {
        FeatureRegistrationPhase Phase { get; }
        FeatureRegistrationResult Register(IFeatureRegistration registration);
    }
    public readonly struct FeatureRegistrationResult
    {
        public bool Accepted { get; }
        public FeatureId Feature { get; }
        public FeatureRegistrationReason Reason { get; }
        public string DiagnosticId { get; }
        public FeatureRegistrationResult(bool accepted, FeatureId feature, FeatureRegistrationReason reason, string diagnosticId)
        { Accepted = accepted; Feature = feature; Reason = reason; DiagnosticId = diagnosticId; }
    }

    public enum FeaturePresentationState : byte { NotApplicable = 0, Available = 1, PresentationDegraded = 2, HeadlessOnly = 3, Failed = 4 }
    public readonly struct FeaturePresentationView
    {
        public FeatureId Feature { get; }
        public FeaturePresentationState State { get; }
        public string DiagnosticId { get; }
        public ulong PresentationRevision { get; }
        public FeaturePresentationView(FeatureId feature, FeaturePresentationState state, string diagnosticId, ulong presentationRevision)
        { Feature = feature; State = state; DiagnosticId = diagnosticId; PresentationRevision = presentationRevision; }
    }

    public enum FeatureStopReason : byte { None, PluginStopping, UserDisabled, VersionIncompatible, RuntimeIsolated, EnvironmentUnavailable, DependencyUnavailable, CoreSafeMode }
    public readonly struct FeatureStartResult { public bool Started { get; } public FrameworkErrorCode Error { get; } public string DiagnosticId { get; } }

    public interface IFeatureModule { FeatureStartResult Start(IFeatureBootstrap bootstrap); void Stop(FeatureStopReason reason); }
    public interface IFeatureBootstrap
    {
        FeatureScopeIdentity Identity { get; }
        ulong LifecycleGeneration { get; }
        IScopedFeatureSettings Settings { get; }
        IFeatureEventSubscriber Events { get; }
        IOwnedFeatureEventPublisher OwnedEvents { get; }
        IFeatureLogger Logger { get; }
        IDependencyCapabilityView Dependencies { get; }
        IFeatureLifetime Lifetime { get; }
        // DEV-V2-14 ②: the feature's network entry point. Frozen: never null
        // (fail-fast at the host composition), the same IBueNetworkApi
        // instance for the module's whole lifetime, explicit results while
        // the network module is not ready, no Host/LMN/Unity type leakage.
        BueNetwork.IBueNetworkApi Network { get; }
    }
    public interface IFeatureLifetime { bool TryTrack(IDisposable registration); }
    public interface IScopedFeatureSettings
    {
        FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope);
        bool TryGet(string settingId, out SettingValue value, out uint revision);
        SettingChangeResult Submit(ScopedSettingChangeRequest request);
    }
    public interface IFeatureEventSubscriber { IDisposable Subscribe<TEvent>(Action<TEvent> handler); }
    public interface IOwnedFeatureEventPublisher { bool TryPublish<TEvent>(string declaredEventId, TEvent value); }
    public interface IFeatureLogger
    {
        void Info(string eventName, string diagnosticId);
        void Warning(string eventName, FrameworkErrorCode error, string diagnosticId);
        void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception);
    }
    public interface IDependencyCapabilityView
    {
        bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion);
        bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature);
    }

    public enum FeatureState : byte { Discovered, Incompatible, Disabled, Starting, Running, Isolating, Isolated, Stopping, Stopped }
    public readonly struct FeatureStatusView { public FeatureId Feature { get; } public FeatureState State { get; } public FrameworkErrorCode Error { get; } public FeatureStopReason StopReason { get; } public string DiagnosticId { get; } public ulong StateRevision { get; } }
    public enum CoreRuntimeState : byte { Initializing, Running, SafeMode, Stopping, Stopped }
    public readonly struct CoreRuntimeStatusView { public CoreRuntimeState State { get; } public FrameworkErrorCode Error { get; } public string DiagnosticId { get; } public ulong StateRevision { get; } }
    public readonly struct CoreRuntimeStatusChangedEvent { public CoreRuntimeStatusView Status { get; } }

    public readonly struct ItemGridPosition { public byte Page { get; } public byte X { get; } public byte Y { get; } public byte Rotation { get; } public ItemGridPosition(byte page, byte x, byte y, byte rotation) { Page = page; X = x; Y = y; Rotation = rotation; } }
    public enum ContainerKind : byte { PlayerInventory, Storage, Equipment }
    public readonly struct ContainerReference { public ContainerKind Kind { get; } public byte Page { get; } public uint SessionGeneration { get; } public ContainerReference(ContainerKind kind, byte page, uint sessionGeneration) { Kind = kind; Page = page; SessionGeneration = sessionGeneration; } }
    public readonly struct ItemPlacementIntent { public uint DragGeneration { get; } public ItemGridPosition Source { get; } public ContainerReference TargetContainer { get; } public ItemGridPosition Candidate { get; } }
    public enum PlacementPreviewState : byte { Hidden, Candidate, LocallyInvalid, PendingAuthoritativeProjection }
    public enum PlacementReason : ushort { None = 0, OutsideGrid = 100, Occupied = 101, UnsupportedContainer = 102, StaleDrag = 103, NoCandidate = 104, ServerStateChanged = 200, ServerRejected = 201, FeatureUnavailable = 300, VersionMismatch = 301, RateLimited = 302, InternalFailure = 900 }
    public readonly struct ItemPlacementPreview { public uint DragGeneration { get; } public PlacementPreviewState State { get; } public ItemGridPosition Candidate { get; } public byte Width { get; } public byte Height { get; } public PlacementReason Reason { get; } public ItemPlacementPreview(uint dragGeneration, PlacementPreviewState state, ItemGridPosition candidate, byte width, byte height, PlacementReason reason) { DragGeneration = dragGeneration; State = state; Candidate = candidate; Width = width; Height = height; Reason = reason; } }
    public interface IGridOccupancyView { byte Width { get; } byte Height { get; } bool IsOccupied(byte x, byte y); }
    public readonly struct PlacementCandidateInput { public uint DragGeneration { get; } public ItemGridPosition Source { get; } public ContainerReference TargetContainer { get; } public float CursorGridX { get; } public float CursorGridY { get; } public byte ItemWidth { get; } public byte ItemHeight { get; } public byte CurrentRotation { get; } public bool AllowAutomaticRotation { get; } public IGridOccupancyView Occupancy { get; } public PlacementCandidateInput(uint dragGeneration, ItemGridPosition source, ContainerReference targetContainer, float cursorGridX, float cursorGridY, byte itemWidth, byte itemHeight, byte currentRotation, bool allowAutomaticRotation, IGridOccupancyView occupancy) { DragGeneration = dragGeneration; Source = source; TargetContainer = targetContainer; CursorGridX = cursorGridX; CursorGridY = cursorGridY; ItemWidth = itemWidth; ItemHeight = itemHeight; CurrentRotation = currentRotation; AllowAutomaticRotation = allowAutomaticRotation; Occupancy = occupancy; } }
    public interface IPlacementCandidateEvaluator { ItemPlacementPreview Evaluate(PlacementCandidateInput input); }
    public enum DragInteractionState : byte { Idle, Dragging, Hovering, Dropping, AwaitingProjection }
    public readonly struct DragInteractionView { public uint DragGeneration { get; } public DragInteractionState State { get; } public ItemPlacementPreview Preview { get; } }

    public enum SettingRevisionScope : byte { ClientPreference, ServerAuthority }
    public enum SettingKind : byte { Toggle, Integer, Float, Text, KeyBinding, Choice }
    public enum SettingAuthority : byte { ClientLocal, ServerAuthoritative, ServerPolicyWithClientPreference }
    public readonly struct SettingValue
    {
        public SettingKind Kind { get; }
        public bool Boolean { get; }
        public int Integer { get; }
        public float Float { get; }
        public string Text { get; }
        public SettingValue(SettingKind kind, bool boolean, int integer, float @float, string text) { Kind = kind; Boolean = boolean; Integer = integer; Float = @float; Text = text; }
        public static SettingValue Toggle(bool value) { return new SettingValue(SettingKind.Toggle, value, 0, 0f, null); }
        public static SettingValue IntegerValue(int value) { return new SettingValue(SettingKind.Integer, false, value, 0f, null); }
        public static SettingValue FloatValue(float value) { return new SettingValue(SettingKind.Float, false, 0, value, null); }
        public static SettingValue TextValue(string value) { return new SettingValue(SettingKind.Text, false, 0, 0f, value ?? string.Empty); }
        public static SettingValue Choice(string value) { return new SettingValue(SettingKind.Choice, false, 0, 0f, value ?? string.Empty); }
        public static SettingValue KeyBinding(string value) { return new SettingValue(SettingKind.KeyBinding, false, 0, 0f, value ?? string.Empty); }
    }
    public readonly struct SettingValueOption { public bool HasValue { get; } public SettingValue Value { get; } public SettingValueOption(bool hasValue, SettingValue value) { HasValue = hasValue; Value = value; } }
    public readonly struct SettingMutation { public string SettingId { get; } public SettingValue Value { get; } public SettingMutation(string settingId, SettingValue value) { SettingId = settingId; Value = value; } }
    public readonly struct ScopedSettingChangeRequest { public ulong RequestId { get; } public SettingRevisionScope RevisionScope { get; } public uint ExpectedRevision { get; } public IReadOnlyList<SettingMutation> Mutations { get; } public ScopedSettingChangeRequest(ulong requestId, SettingRevisionScope revisionScope, uint expectedRevision, IReadOnlyList<SettingMutation> mutations) { RequestId = requestId; RevisionScope = revisionScope; ExpectedRevision = expectedRevision; Mutations = mutations; } }
    public readonly struct SettingDescriptor
    {
        public FeatureId Feature { get; } public string SettingId { get; } public string DisplayNameKey { get; } public string DescriptionKey { get; } public SettingKind Kind { get; } public SettingAuthority Authority { get; } public SettingValue DefaultValue { get; } public SettingValueOption Minimum { get; } public SettingValueOption Maximum { get; } public SettingValueOption Step { get; } public IReadOnlyList<SettingValue> AllowedValues { get; } public ushort MaximumUtf8Bytes { get; } public string ValidationRuleId { get; } public uint SchemaVersion { get; } public int SortOrder { get; } public string VisibilityRuleId { get; } public string EnablementRuleId { get; }
        public SettingDescriptor(FeatureId feature, string settingId, string displayNameKey, string descriptionKey, SettingKind kind, SettingAuthority authority, SettingValue defaultValue, SettingValueOption minimum, SettingValueOption maximum, SettingValueOption step, IReadOnlyList<SettingValue> allowedValues, ushort maximumUtf8Bytes, string validationRuleId, uint schemaVersion, int sortOrder, string visibilityRuleId, string enablementRuleId)
        { Feature = feature; SettingId = settingId; DisplayNameKey = displayNameKey; DescriptionKey = descriptionKey; Kind = kind; Authority = authority; DefaultValue = defaultValue; Minimum = minimum; Maximum = maximum; Step = step; AllowedValues = allowedValues; MaximumUtf8Bytes = maximumUtf8Bytes; ValidationRuleId = validationRuleId; SchemaVersion = schemaVersion; SortOrder = sortOrder; VisibilityRuleId = visibilityRuleId; EnablementRuleId = enablementRuleId; }
    }
    public readonly struct SettingPolicyView { public SettingValueOption Minimum { get; } public SettingValueOption Maximum { get; } public SettingValueOption Step { get; } public IReadOnlyList<SettingValue> AllowedValues { get; } public ushort MaximumUtf8Bytes { get; } public string ValidationRuleId { get; } public SettingPolicyView(SettingValueOption minimum, SettingValueOption maximum, SettingValueOption step, IReadOnlyList<SettingValue> allowedValues, ushort maximumUtf8Bytes, string validationRuleId) { Minimum = minimum; Maximum = maximum; Step = step; AllowedValues = allowedValues; MaximumUtf8Bytes = maximumUtf8Bytes; ValidationRuleId = validationRuleId; } }
    public readonly struct SettingEntryView { public string SettingId { get; } public SettingAuthority Authority { get; } public SettingValueOption ClientPreference { get; } public bool HasPolicy { get; } public SettingPolicyView Policy { get; } public SettingValue EffectiveValue { get; } public bool IsVisible { get; } public bool CanEdit { get; } public SettingEntryView(string settingId, SettingAuthority authority, SettingValueOption clientPreference, bool hasPolicy, SettingPolicyView policy, SettingValue effectiveValue, bool isVisible, bool canEdit) { SettingId = settingId; Authority = authority; ClientPreference = clientPreference; HasPolicy = hasPolicy; Policy = policy; EffectiveValue = effectiveValue; IsVisible = isVisible; CanEdit = canEdit; } }
    public enum SettingSyncState : byte { Ready, AwaitingAuthoritativeSnapshot, Unavailable }
    public enum SettingSnapshotSource : byte { LocalPersistent, SingleplayerAuthority, P2PHostAuthority, DedicatedServerAuthority, SessionProjection, SafeDefault }
    public readonly struct FeatureSettingsSnapshot { public FeatureId Feature { get; } public uint SchemaVersion { get; } public SettingRevisionScope RevisionScope { get; } public uint Revision { get; } public SettingSyncState SyncState { get; } public SettingSnapshotSource Source { get; } public IReadOnlyList<SettingEntryView> Entries { get; } public FeatureSettingsSnapshot(FeatureId feature, uint schemaVersion, SettingRevisionScope revisionScope, uint revision, SettingSyncState syncState, SettingSnapshotSource source, IReadOnlyList<SettingEntryView> entries) { Feature = feature; SchemaVersion = schemaVersion; RevisionScope = revisionScope; Revision = revision; SyncState = syncState; Source = source; Entries = entries; } }
    public readonly struct SettingChangeResult { public bool Accepted { get; } public FrameworkErrorCode Error { get; } public uint Revision { get; } public FeatureSettingsSnapshot Snapshot { get; } public SettingChangeResult(bool accepted, FrameworkErrorCode error, uint revision, FeatureSettingsSnapshot snapshot) { Accepted = accepted; Error = error; Revision = revision; Snapshot = snapshot; } }
    public readonly struct UpdateModuleConfigCommand { public ulong RequestId { get; } public FeatureId Feature { get; } public SettingRevisionScope RevisionScope { get; } public uint ExpectedRevision { get; } public IReadOnlyList<SettingMutation> Mutations { get; } public UpdateModuleConfigCommand(ulong requestId, FeatureId feature, SettingRevisionScope revisionScope, uint expectedRevision, IReadOnlyList<SettingMutation> mutations) { RequestId = requestId; Feature = feature; RevisionScope = revisionScope; ExpectedRevision = expectedRevision; Mutations = mutations; } }
    public readonly struct RequestModuleConfigSnapshotCommand { public ulong RequestId { get; } public FeatureId Feature { get; } public SettingRevisionScope RevisionScope { get; } public uint KnownRevision { get; } }
    public readonly struct ModuleConfigChangedEvent { public ulong RequestId { get; } public FeatureId Feature { get; } public SettingRevisionScope RevisionScope { get; } public uint Revision { get; } public FeatureSettingsSnapshot Snapshot { get; } }
    public readonly struct ModuleConfigRejectedEvent { public ulong RequestId { get; } public FeatureId Feature { get; } public SettingRevisionScope RevisionScope { get; } public FrameworkErrorCode Error { get; } public uint CurrentRevision { get; } public FeatureSettingsSnapshot Snapshot { get; } }
    public readonly struct FeatureStatusChangedEvent { public FeatureStatusView Status { get; } }

    public enum FrameworkErrorCode : ushort
    {
        None = 0, ContractMajorMismatch = 1000, ContractMinorUnsupported = 1001, CapabilityMissing = 1002, FeatureVersionMismatch = 1003,
        NetworkCapabilitiesUnavailable = 1004, HandshakeTimedOut = 1005, HandshakeNonceMismatch = 1006, StaleConnectionGeneration = 1007,
        SnapshotIncomplete = 1008, SnapshotIntegrityFailed = 1009, MalformedPayload = 1100, PayloadTooLarge = 1101, UnknownMessageKind = 1102,
        UnauthorizedSender = 1200, RateLimited = 1201, SettingRejected = 1300, SettingUnknown = 1301, SettingTypeMismatch = 1302,
        SettingValidationFailed = 1303, SettingRevisionConflict = 1304, RequestIdConflict = 1305, SettingSnapshotNotReady = 1306,
        SettingSchemaIncompatible = 1307, SettingPersistenceFailed = 1308, SettingMigrationFailed = 1309, ModuleStartFailed = 2000,
        ModuleRuntimeIsolated = 2001, DependencyUnavailable = 2002, InvalidStateTransition = 2003, DependencyCycle = 2004,
        DependencyVersionMismatch = 2005, CleanupIncomplete = 2006, CoreRuntimeFailure = 9000
    }
    public enum CapabilityDirection : byte { LocalOnly, ServerToClient, Bidirectional }
    public enum CapabilityRequirement : byte { Optional, RequiredForFeature, RequiredForSession }
    [Flags] public enum CapabilityEnvironment : byte { None = 0, Client = 1, SingleplayerAuthority = 2, P2PHostAuthority = 4, DedicatedServerAuthority = 8 }
    public readonly struct CapabilityDescriptor { public FeatureId Provider { get; } public string CapabilityId { get; } public ushort Version { get; } public CapabilityDirection Direction { get; } public CapabilityRequirement Requirement { get; } public CapabilityEnvironment Environments { get; } public ContractVersion RequiredContract { get; } }
    public readonly struct WireSemanticVersion { public ushort Major { get; } public ushort Minor { get; } public ushort Patch { get; } }
    public readonly struct FeatureCapabilityManifest { public FeatureId Feature { get; } public WireSemanticVersion FeatureVersion { get; } public IReadOnlyList<CapabilityDescriptor> Capabilities { get; } }
    public enum NegotiationState : byte { Pending, Available, Degraded, Incompatible, Unavailable }
    public readonly struct NegotiatedFeatureView { public FeatureId Feature { get; } public WireSemanticVersion LocalVersion { get; } public WireSemanticVersion RemoteVersion { get; } public NegotiationState State { get; } public FrameworkErrorCode Error { get; } public ulong NegotiationRevision { get; } public IReadOnlyList<CapabilityDescriptor> AcceptedCapabilities { get; } }
    public readonly struct ConnectionHandshakeId { public ulong ConnectionGeneration { get; } public ulong NonceHigh { get; } public ulong NonceLow { get; } }
    public readonly struct CapabilityHello { public ConnectionHandshakeId ClientHandshake { get; } public WireSemanticVersion FrameworkVersion { get; } public ContractVersion Contract { get; } public IReadOnlyList<FeatureCapabilityManifest> Features { get; } }
    public readonly struct CapabilitySnapshot { public ulong ConnectionGeneration { get; } public ulong ClientNonceHigh { get; } public ulong ClientNonceLow { get; } public ulong ServerNonceHigh { get; } public ulong ServerNonceLow { get; } public ulong SnapshotId { get; } public WireSemanticVersion ServerFrameworkVersion { get; } public ContractVersion ServerContract { get; } public IReadOnlyList<NegotiatedFeatureView> Features { get; } }
    public readonly struct CapabilityAck { public ulong ConnectionGeneration { get; } public ulong SnapshotId { get; } public ulong ServerNonceHigh { get; } public ulong ServerNonceLow { get; } }
    public readonly struct SessionReadyEvent { public ulong ConnectionGeneration { get; } public ulong SnapshotId { get; } }
    public readonly struct HandshakeReject { public ulong ConnectionGeneration { get; } public ulong ClientNonceHigh { get; } public ulong ClientNonceLow { get; } public FrameworkErrorCode Error { get; } public ushort SupportedContractMajor { get; } }
    public enum SnapshotKind : byte { CapabilityHello, CapabilitySnapshot, SettingsSnapshot }
    public readonly struct SnapshotChunkEnvelope
    {
        public ulong ConnectionGeneration { get; }
        public ulong SnapshotId { get; }
        public SnapshotKind Kind { get; }
        public ushort ChunkIndex { get; }
        public ushort ChunkCount { get; }
        public uint TotalLength { get; }
        public ushort ChunkLength { get; }
        public byte[] Sha256 { get; }
        public byte[] ChunkBytes { get; }
    }
}

// DEV-V2-02: BueNetworkApi public contract surface (T3 Q1-Q12 frozen shape).
// Public types live in the BueNetwork namespace (Q12); they are pure .NET
// 4.7.2 / C# 10 and must not reference Unity/Glazier/Sleek/LMN/Unturned/
// BepInEx types (Q6). Frame codec / IClientTransport reflection / Steam
// details stay inside the BUE Host (DEV-V2-03), never here.
namespace BetterUnturnedExperience.Contracts.BueNetwork
{
    // Q11: explicit, localizable send outcome. Hot paths never throw.
    public enum NetworkSendResult : ushort
    {
        None = 0,
        Sent = 1,
        ChannelNotRegistered = 200,
        NoSession = 201,
        PeerUnreachable = 202,
        PayloadTooLarge = 203,
        LocalTransportUnavailable = 900
    }

    // Q1: one module = one named channel; FeatureId is the channel name.
    // Q2: registration declares the minimum BUE contract and the module's
    // feature version; version negotiation happens inside BueNetworkApi and a
    // mismatch surfaces as FeatureRegistrationReason.ContractIncompatible.
    public readonly struct ChannelRegistrationResult
    {
        public bool Accepted { get; }
        public FeatureId Channel { get; }
        public FeatureRegistrationReason Reason { get; }
        public string DiagnosticId { get; }

        public ChannelRegistrationResult(bool accepted, FeatureId channel, FeatureRegistrationReason reason, string diagnosticId)
        { Accepted = accepted; Channel = channel; Reason = reason; DiagnosticId = diagnosticId; }
    }

    // Q10: one entry of the negotiated channel version table.
    public readonly struct ChannelVersionEntry
    {
        public FeatureId Channel { get; }
        public ushort Version { get; }
        public ChannelVersionEntry(FeatureId channel, ushort version) { Channel = channel; Version = version; }
    }

    // Q4 + Q10: a live connection to one peer. SessionId is the connection
    // generation (CONTEXT「连接代际」). Events let a module react to lifecycle;
    // peer identity and the negotiated channel table support per-module
    // authorization without a peer-FeatureId addressing scheme (Q9).
    public interface IConnectionSession
    {
        ulong SessionId { get; }
        ulong PeerSteamId { get; }
        ContractVersion PeerContract { get; }
        ushort PeerFeatureVersion { get; }
        IReadOnlyList<ChannelVersionEntry> Channels { get; }
        event Action Connected;
        event Action Disconnected;
        event Action<ulong> GenerationChanged;
        NetworkSendResult Send(byte[] payload, bool reliable);
    }

    // DEV-V2-14 ①: direction of an INBOUND frame's source — where the frame
    // came from, never the local role. A host answering the handshake
    // receives FromClients frames; a handshake initiator receives FromServer
    // frames. Same channel, two independent subscriptions.
    public enum ChannelDirection : byte { FromClients = 0, FromServer = 1 }

    // Q1/Q2/Q4/Q9/Q12: the public BueNetworkApi surface. Registration is
    // explicit; send targets are expressed by connection context, never by a
    // peer FeatureId (Q9).
    public interface IBueNetworkApi
    {
        ChannelRegistrationResult RegisterChannel(FeatureId channel, ContractVersion minimumBueContract, ushort featureVersion);
        bool UnregisterChannel(FeatureId channel);
        // DEV-V2-14 ①: subscribe an inbound handler per channel and frame
        // source. Frozen: every call returns its own idempotent handle whose
        // Dispose removes only its own delegate; the handler table is
        // decoupled from channel registration (subscribing to an
        // unregistered channel is legal — frames dispatch once the channel
        // is registered and traffic arrives); handlers run outside the state
        // lock and one handler's exception never reaches its peers; a null
        // handler or an undefined direction value is a developer error and
        // throws its argument exception (fail-fast).
        IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler);
        IReadOnlyList<IConnectionSession> Sessions { get; }
        NetworkSendResult SendToServer(FeatureId channel, byte[] payload, bool reliable);
        NetworkSendResult SendToClients(FeatureId channel, byte[] payload, bool reliable);
        NetworkSendResult SendToClient(FeatureId channel, IConnectionSession session, byte[] payload, bool reliable);
    }
}
