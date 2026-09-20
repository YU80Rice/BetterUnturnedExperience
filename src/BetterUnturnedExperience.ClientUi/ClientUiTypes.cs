using System;
using System.Collections.Generic;
using BetterUnturnedExperience.Contracts;

namespace BetterUnturnedExperience.ClientUi.Internal
{
    internal readonly struct ClientUiEnvironment
    {
        public bool ClientUiAvailable { get; }
        public bool IsBatchMode { get; }
        public bool Headless { get; }

        public ClientUiEnvironment(bool clientUiAvailable, bool isBatchMode, bool headless)
        {
            ClientUiAvailable = clientUiAvailable;
            IsBatchMode = isBatchMode;
            Headless = headless;
        }

        public bool CanCompose
        {
            get { return ClientUiAvailable && !IsBatchMode && !Headless; }
        }
    }

    // DEV-V6-04: the registry component orchestration (IClientUiRoot /
    // IClientUiInventorySurface / IClientUiFeatureComponent /
    // ClientUiRegistration / GeneratedClientUiRegistry) retired here — its
    // sole consumer was the Better Item Interaction feature component, which
    // moved to the Bii project in DEV-V6-04; keeping an un-consumed
    // orchestration mechanism would be the same empty-shell shape the ticket
    // bans for the module itself. The composition STATE machine below stays
    // (environment gate + safe mode + destroy semantics for the panel ring).

    internal enum ClientUiCompositionState : byte
    {
        NotInitialized,
        Unavailable,
        Ready,
        Destroyed
    }

    internal sealed class ClientUiCompositionRoot
    {
        private ClientUiCompositionState state;
        private bool safeMode;
        private int safeModeDiagnosticCount;

        // DEV-V6-04: the registry parameter retired with the orchestration —
        // the state machine now only owns the environment gate and the
        // safe-mode/destroy transitions for the panel ring.
        public ClientUiCompositionRoot()
        {
            state = ClientUiCompositionState.NotInitialized;
        }

        // ClientUi diagnostic seam (DEV-V2-24 F-B1): the composition and the
        // feature components cannot reach the plugin log directly, so they
        // emit structured lines through this sink; the plugin binds it to the
        // runtime log at wiring time, honoring the requested level. Lines
        // that already carry diagnosticId= are passed through unmodified so
        // feature-owned ids are never double-tagged. DEV-V6-04: the last
        // production emitter (the BII component) moved to the Bii project and
        // rides injected mouths now; the seam stays for the UI ring (test
        // anchors + future UI components).
        internal enum ClientUiDiagnosticLevel { Debug = 0, Error = 1 }
        internal static Action<string, ClientUiDiagnosticLevel> DiagnosticSink = null;
        private static void Emit(string line, ClientUiDiagnosticLevel level)
        {
            var sink = DiagnosticSink;
            if (sink != null) { try { sink(line, level); } catch (Exception) { } }
        }

        // Internal forwarder so feature components can reach the same sink.
        internal static void EmitDiagnostic(string line, ClientUiDiagnosticLevel level)
        {
            Emit(line, level);
        }

        internal ClientUiCompositionState State { get { return state; } }
        internal bool IsSafeMode { get { return safeMode; } }
        internal int SafeModeDiagnosticCount { get { return safeModeDiagnosticCount; } }

        internal bool Initialize(ClientUiEnvironment environment)
        {
            if (state == ClientUiCompositionState.Ready) return true;
            if (state == ClientUiCompositionState.Destroyed || safeMode) return false;
            if (!environment.CanCompose)
            {
                state = ClientUiCompositionState.Unavailable;
                return false;
            }

            state = ClientUiCompositionState.Ready;
            return true;
        }

        internal void Destroy()
        {
            if (state == ClientUiCompositionState.Destroyed) return;
            state = ClientUiCompositionState.Destroyed;
        }

        internal void EnterSafeMode(Action<string> diagnostic)
        {
            if (safeMode) return;
            safeMode = true;
            safeModeDiagnosticCount++;
            if (diagnostic != null)
            {
                try { diagnostic("BUE-CLIENTUI-SAFEMODE"); }
                catch (Exception) { }
            }
            state = ClientUiCompositionState.Unavailable;
        }

    }
}
