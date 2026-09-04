using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterUnturnedExperience.Core.Network;
using BetterUnturnedExperience.Transport;

namespace BetterUnturnedExperience.Network.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Run();
                Console.WriteLine("DEV-06 network tests: PASS");
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("DEV-06 network tests: FAIL");
                Console.WriteLine(error.GetType().FullName);
                Console.WriteLine(error.Message);
                return 1;
            }
        }

        private static void Run()
        {
            var clientNonce = Bytes(0x10, 16);
            var serverNonce = Bytes(0x20, 16);
            var binding = ReadyBinding.Create(7, 99, clientNonce, serverNonce);
            var readyBytes = BueFrameCodec.EncodeReadyPayload(binding);
            Assert(readyBytes.Length == 48, "ready payload is 48 bytes");
            Assert(BueFrameCodec.TryDecodeReadyPayload(readyBytes, out var decodedBinding, out var readyError) && decodedBinding.ConnectionGeneration == 7 && readyError == FrameDecodeError.None, "ready payload roundtrip");
            var frame = FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, binding, 42, new byte[] { 1, 2, 3 });
            var wire = BueFrameCodec.Encode(frame);
            Assert(wire.Length == BueFrameCodec.PrefixLength + 8 + 3, "fenced settings frame has 52-byte prefix and request id");
            Assert(wire[0] == 1 && wire[1] == 0 && wire[2] == 0 && wire[3] == 0, "prefix version/flags/reserved");
            Assert(BueFrameCodec.TryDecode(BueNetworkMessageKind.UpdateModuleConfig, wire, out var decoded, out var decodeError), "valid frame decodes: " + decodeError);
            Assert(decoded.ConnectionGeneration == 7 && decoded.SnapshotId == 99, "decoded identity");
            Assert(decoded.Payload.Length == 3 && decoded.Payload[2] == 3, "decoded payload");
            var requestWire = BueFrameCodec.Encode(frame);
            Assert(BueFrameCodec.TryDecode(BueNetworkMessageKind.UpdateModuleConfig, requestWire, out var decodedRequest, out decodeError) && decodedRequest.RequestId == 42, "wire request id roundtrip");

            var malformed = (byte[])wire.Clone(); malformed[0] = 2;
            Assert(!BueFrameCodec.TryDecode(BueNetworkMessageKind.UpdateModuleConfig, malformed, out _, out decodeError) && decodeError == FrameDecodeError.UnsupportedFenceVersion, "unknown fence version rejected");
            malformed = (byte[])wire.Clone(); malformed[1] = 1;
            Assert(!BueFrameCodec.TryDecode(BueNetworkMessageKind.UpdateModuleConfig, malformed, out _, out decodeError) && decodeError == FrameDecodeError.NonZeroFlags, "non-zero flags rejected");
            Assert(!BueFrameCodec.TryDecode(BueNetworkMessageKind.UpdateModuleConfig, new byte[51], out _, out decodeError) && decodeError == FrameDecodeError.Truncated, "short frame rejected");
            Assert(!BueFrameCodec.TryDecode((BueNetworkMessageKind)0x0105, wire, out _, out decodeError) && decodeError == FrameDecodeError.InvalidMessageKind, "unknown fenced kind rejected");

            var envelope = new BueEnvelope(1, 0, BueEnvelopeMessageKind.CapabilityHello, new byte[] { 4, 5, 6 });
            var envelopeWire = BueEnvelopeCodec.Encode(envelope);
            Assert(envelopeWire.Length == BueEnvelopeCodec.HeaderLength + 3, "contract envelope has 10-byte header");
            Assert(BueEnvelopeCodec.TryDecode(envelopeWire, out var decodedEnvelope, out var envelopeError) && envelopeError == EnvelopeDecodeError.None, "contract envelope roundtrip");
            Assert(decodedEnvelope.MessageKind == BueEnvelopeMessageKind.CapabilityHello && decodedEnvelope.Payload[2] == 6, "envelope fields roundtrip");
            Assert(!BueEnvelopeCodec.TryDecode(new byte[9], out _, out envelopeError) && envelopeError == EnvelopeDecodeError.Truncated, "short envelope rejected");
            var forbiddenEnvelope = new BueEnvelope(1, 0, (BueEnvelopeMessageKind)0x0005, new byte[0]);
            Assert(!BueEnvelopeCodec.TryEncode(forbiddenEnvelope, out _, out envelopeError) && envelopeError == EnvelopeDecodeError.UnknownMessageKind, "reserved envelope kind rejected");
            Assert(!BueEnvelopeCodec.TryEncode(new BueEnvelope(1, 0, (BueEnvelopeMessageKind)0x0105, new byte[0]), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.UnknownMessageKind, "unallocated settings kind rejected");
            Assert(!BueEnvelopeCodec.TryEncode(new BueEnvelope(1, 0, (BueEnvelopeMessageKind)0x0106, new byte[0]), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.UnknownMessageKind, "future settings kind rejected");
            Assert(!BueEnvelopeCodec.TryDecode(UnknownEnvelopeWire(0x0105), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.UnknownMessageKind, "unallocated settings kind decode rejected");
            Assert(!BueEnvelopeCodec.TryDecode(UnknownEnvelopeWire(0x0106), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.UnknownMessageKind, "future settings kind decode rejected");
            Assert(!BueEnvelopeCodec.TryEncode(new BueEnvelope(1, 0, BueEnvelopeMessageKind.CapabilityHello, new byte[16385]), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.PayloadTooLarge, "oversized envelope rejected");
            Assert(BueEnvelopeCodec.TryEncode(new BueEnvelope(1, 0, BueEnvelopeMessageKind.UpdateModuleConfig, new byte[0]), out _, out envelopeError), "fenced message kind remains an envelope kind");
            Assert(!BueEnvelopeCodec.TryEncode(new BueEnvelope(0x5542, 0x4245, BueEnvelopeMessageKind.CapabilityHello, new byte[0]), out _, out envelopeError) && envelopeError == EnvelopeDecodeError.ReservedBootstrapCollision, "bootstrap magic collision is reserved");

            var reject = BueBootstrapReject.Create(7, 0x11, 0x22, 1000, 1);
            var rejectWire = BueBootstrapCodec.Encode(reject);
            Assert(rejectWire.Length == BueBootstrapCodec.FrameLength && rejectWire[0] == (byte)'B' && rejectWire[3] == (byte)'B', "bootstrap reject has fixed BUEB frame");
            Assert(BueBootstrapCodec.TryDecode(rejectWire, out var decodedReject, out var bootstrapError) && bootstrapError == BootstrapDecodeError.None, "bootstrap reject roundtrip");
            Assert(decodedReject.ConnectionGeneration == 7 && decodedReject.SupportedContractMajor == 1, "bootstrap reject fields roundtrip");

            var fence = new ReadyFrameFence(128, 16);
            fence.ReplaceBinding(binding);
            var handled = 0;
            Assert(fence.TryAccept(frame, _ => handled++).Decision == FrameDecision.Applied && handled == 1, "current frame reaches handler");
            Assert(fence.TryAccept(frame, _ => handled++).Decision == FrameDecision.Duplicate && handled == 1, "duplicate write is idempotent");
            var conflict = FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, binding, 42, new byte[] { 9 });
            Assert(fence.TryAccept(conflict, _ => handled++).Decision == FrameDecision.RequestIdConflict, "same request id different payload rejected");

            var staleBinding = ReadyBinding.Create(8, 100, clientNonce, serverNonce);
            var stale = FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, staleBinding, 43, new byte[] { 4 });
            Assert(fence.TryAccept(stale, _ => handled++).Decision == FrameDecision.StaleGeneration && handled == 1, "stale generation rejected before handler");

            var read = FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, binding, 700, new byte[0]);
            for (ulong request = 700; request < 716; request++)
                Assert(fence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, binding, request, new byte[0]), _ => { }).Decision == FrameDecision.Applied, "read in-flight accepted " + request);
            Assert(fence.TryAccept(read, _ => { }).Decision == FrameDecision.ReadInFlightDuplicate, "duplicate read in-flight rejected");
            Assert(fence.CompleteRead(700), "completed read releases slot");
            Assert(fence.TryAccept(read, _ => { }).Decision == FrameDecision.Applied, "completed read can be retried");

            var next = ReadyBinding.Create(9, 101, clientNonce, serverNonce);
            fence.ReplaceBinding(next);
            Assert(fence.TryAccept(frame, _ => handled++).Decision == FrameDecision.StaleGeneration, "old frame rejected after rotation");
            Assert(fence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, next, 42, new byte[] { 1 }), _ => handled++).Decision == FrameDecision.Applied, "request id may restart in new generation");
            Assert(fence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, next, 0, new byte[] { 1 }), _ => handled++).Decision == FrameDecision.InvalidRequestId, "write request id zero rejected");
            var projectionCount = 0;
            var projection = FencedFrame.Create(BueNetworkMessageKind.FeatureStatusProjection, next, 0, new byte[] { 8 });
            Assert(fence.TryAccept(projection, _ => projectionCount++).Decision == FrameDecision.Applied, "projection accepted");
            Assert(fence.TryAccept(projection, _ => projectionCount++).Decision == FrameDecision.Applied && projectionCount == 2, "projection does not consume write replay window");

            var fullReadFence = new ReadyFrameFence(1, 1);
            fullReadFence.ReplaceBinding(next);
            Assert(fullReadFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, next, 1, new byte[0]), _ => { }).Decision == FrameDecision.Applied, "first read occupies independent in-flight slot");
            Assert(fullReadFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, next, 2, new byte[0]), _ => { }).Decision == FrameDecision.ReadInFlightFull, "read in-flight capacity fails closed");
            fullReadFence.ReplaceBinding(ReadyBinding.Create(10, 102, clientNonce, serverNonce));
            Assert(fullReadFence.ReadInFlightCount == 0 && fullReadFence.WriteReplayCount == 0, "generation rotation clears both windows");

            var defaultFence = new ReadyFrameFence();
            defaultFence.ReplaceBinding(next);
            for (ulong request = 1; request <= 128; request++)
                Assert(defaultFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, next, request, new byte[] { (byte)request }), _ => { }).Decision == FrameDecision.Applied, "default write window accepts " + request);
            Assert(defaultFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, next, 129, new byte[] { 129 }), _ => { }).Decision == FrameDecision.ReplayWindowFull, "default write window fails closed at capacity");
            Assert(defaultFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, next, 900, new byte[0]), _ => { }).Decision == FrameDecision.Applied, "read snapshot remains available when write window is full");

            var dispatchFence = new ReadyFrameFence();
            dispatchFence.ReplaceBinding(next);
            var dispatchEntered = new ManualResetEventSlim(false);
            var dispatchRelease = new ManualResetEventSlim(false);
            var dispatchReplaced = new ManualResetEventSlim(false);
            var dispatchFrame = FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, next, 1000, new byte[] { 1 });
            var acceptTask = Task.Run(() => dispatchFence.TryAccept(dispatchFrame, _ => { dispatchEntered.Set(); dispatchRelease.Wait(); }));
            Assert(dispatchEntered.Wait(TimeSpan.FromSeconds(2)), "dispatch handler entered");
            var replacement = ReadyBinding.Create(11, 103, clientNonce, serverNonce);
            var replaceTask = Task.Run(() => { dispatchFence.ReplaceBinding(replacement); dispatchReplaced.Set(); });
            Assert(!dispatchReplaced.Wait(TimeSpan.FromMilliseconds(100)), "generation replacement waits for submitted handler");
            dispatchRelease.Set();
            acceptTask.Wait(); replaceTask.Wait();
            Assert(dispatchReplaced.IsSet && dispatchFence.WriteReplayCount == 0 && dispatchFence.ReadInFlightCount == 0, "replacement clears old dispatch state");
            Assert(dispatchFence.TryAccept(dispatchFrame, _ => { }).Decision == FrameDecision.StaleGeneration, "old frame rejected after concurrent replacement");
            Assert(dispatchFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, replacement, 1000, new byte[] { 2 }), _ => { }).Decision == FrameDecision.Applied, "new generation accepts reused request id");

            var throwingFence = new ReadyFrameFence();
            throwingFence.ReplaceBinding(replacement);
            try { throwingFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.RequestModuleConfigSnapshot, replacement, 2000, new byte[0]), _ => throw new InvalidOperationException("expected test failure")); throw new InvalidOperationException("handler exception was swallowed"); }
            catch (InvalidOperationException error) when (error.Message == "expected test failure") { }
            Assert(throwingFence.ReadInFlightCount == 0, "failed read releases in-flight slot");
            try { throwingFence.TryAccept(FencedFrame.Create(BueNetworkMessageKind.UpdateModuleConfig, replacement, 2001, new byte[] { 3 }), _ => throw new InvalidOperationException("expected mutation failure")); throw new InvalidOperationException("mutation exception was swallowed"); }
            catch (InvalidOperationException error) when (error.Message == "expected mutation failure") { }
            Assert(throwingFence.WriteReplayCount == 1, "failed mutation remains replay-occupied fail-closed");

            var pair = LocalLoopbackTransport.CreatePair();
            var received = 0;
            pair.Second.Receive += _ => received++;
            Assert(pair.First.Send(new byte[] { 5, 6 }, true, 0UL), "loopback send succeeds");
            pair.Second.Pump();
            Assert(received == 1, "loopback delivers queued frame");

            var delegateSent = 0;
            Action<byte[]> registeredReceiver = null;
            Action<Action<byte[]>> register = callback => registeredReceiver = callback;
            var adapter = new LmnTransportAdapter((bytes, reliable, target) => { delegateSent += bytes.Length; return true; }, register);
            var adapterReceived = 0;
            adapter.Receive += bytes => adapterReceived += bytes.Length;
            registeredReceiver(new byte[] { 9 });
            Assert(adapterReceived == 0, "LMN callback only queues before Pump");
            Assert(adapter.Pump() == 1, "LMN Pump dispatches queued callback");
            Assert(adapter.Send(new byte[] { 1, 2 }, true, 0UL), "experimental adapter send delegates");
            Assert(delegateSent == 2 && adapterReceived == 1, "experimental adapter remains a narrow delegate seam");
        }

        private static byte[] Bytes(byte value, int count)
        {
            var result = new byte[count];
            for (var i = 0; i < count; i++) result[i] = (byte)(value + i);
            return result;
        }

        private static byte[] UnknownEnvelopeWire(ushort kind)
        {
            var bytes = new byte[BueEnvelopeCodec.HeaderLength];
            bytes[0] = 1; bytes[1] = 0; bytes[2] = 0; bytes[3] = 0;
            bytes[4] = (byte)kind; bytes[5] = (byte)(kind >> 8);
            return bytes;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
