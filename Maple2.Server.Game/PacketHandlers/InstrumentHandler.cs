using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class InstrumentHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.PlayInstrument;

    private enum Command : byte {
        StartImprovise = 0,
        Improvise = 1,
        StopImprovise = 2,
        StartScore = 3,
        StopScore = 4,
        JoinEnsemble = 5,
        LeaveEnsemble = 6,
        ComposeScore = 8,
        ViewScore = 10,
        StartPerform = 11,
        Unknown = 12,
        Stage = 13,
        Fireworks = 14,
        Emote = 15,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public readonly record struct MidiMessage(byte Unknown, byte Note, short Volume);

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.StartImprovise:
                HandleStartImprovise(session, packet);
                return;
            case Command.Improvise:
                HandleImprovise(session, packet);
                return;
            case Command.StopImprovise:
                HandleStopImprovise(session);
                return;
            case Command.StartScore:
                HandleStartScore(session, packet);
                return;
            case Command.StopScore:
                HandleStopScore(session);
                return;
            case Command.JoinEnsemble:
                HandleJoinEnsemble(session, packet);
                return;
            case Command.LeaveEnsemble:
                HandleLeaveEnsemble(session);
                return;
            case Command.ComposeScore:
                HandleComposeScore(session, packet);
                return;
            case Command.ViewScore:
                HandleViewScore(session, packet);
                return;
            case Command.StartPerform:
                HandleStartPerform(session);
                return;
            case Command.Unknown:
                HandleUnknown(session);
                return;
            case Command.Stage:
                HandleStage(session);
                return;
            case Command.Fireworks:
                HandleFireworks(session);
                return;
            case Command.Emote:
                HandleEmote(session, packet);
                return;
        }
    }

    private void HandleStartImprovise(GameSession session, IByteReader packet) {
        if (session.Field == null || session.Instrument != null) {
            return;
        }

        long itemUid = packet.ReadLong();
        if (!TryUseInstrument(session, itemUid, out session.Instrument)) {
            return;
        }

        session.Instrument.Improvising = true;
        session.Field.Multicast(InstrumentPacket.StartImprovise(session.Instrument));
    }

    private void HandleImprovise(GameSession session, IByteReader packet) {
        if (session.Field == null || session.Instrument?.Improvising != true) {
            return;
        }

        var note = packet.Read<MidiMessage>();
        session.Field.Multicast(InstrumentPacket.Improvise(session.Instrument, note));
    }

    private void HandleStopImprovise(GameSession session) {
        if (session.Field == null || session.Instrument?.Improvising != true) {
            return;
        }

        session.Field.Multicast(InstrumentPacket.StopImprovise(session.Instrument));
        session.Instrument = null;
    }

    private void HandleStartScore(GameSession session, IByteReader packet) {
        if (session.Field == null || session.Instrument != null) {
            return;
        }

        long itemUid = packet.ReadLong();
        long scoreUid = packet.ReadLong();
        if (!TryGetScore(session, scoreUid, out Item? score)) {
            return;
        }
        if (!TryUseInstrument(session, itemUid, out session.Instrument)) {
            return;
        }

        score.RemainUses--;
        if (score.Music != null) {
            session.Field.Multicast(InstrumentPacket.StartScore(session.Instrument, true, ""));
        } else {
            session.Field.Multicast(InstrumentPacket.StartScore(session.Instrument, false, score.Metadata.Music?.FileName ?? ""));
        }
        session.Send(InstrumentPacket.RemainUses(score.Uid, score.RemainUses));
    }

    private void HandleStopScore(GameSession session) {
        if (session.Field == null || session.Instrument == null) {
            return;
        }

        // TODO: Exp gain (Prestige, Mastery, Exp)

        session.Field.Multicast(InstrumentPacket.StopScore(session.Instrument));
        session.Instrument = null;
    }

    private void HandleJoinEnsemble(GameSession session, IByteReader packet) { }

    private void HandleLeaveEnsemble(GameSession session) { }

    private void HandleComposeScore(GameSession session, IByteReader packet) {
        long scoreUid = packet.ReadLong();
        int scoreLength = packet.ReadInt();
        int instrumentType = packet.ReadInt();
        string scoreName = packet.ReadUnicodeString();
        string scoreCode = packet.ReadString();

        Item? score = session.Item.Inventory.Get(scoreUid, InventoryType.FishingMusic);
        if (score is not {RemainUses: > 0} || score.IsExpired()) {
            return;
        }
    }

    private void HandleViewScore(GameSession session, IByteReader packet) {
        long scoreUid = packet.ReadLong();
        Item? score = session.Item.Inventory.Get(scoreUid, InventoryType.FishingMusic);
        if (score is not {RemainUses: > 0} || score.IsExpired()) {
            return;
        }
    }

    private void HandleStartPerform(GameSession session) {

    }

    private void HandleUnknown(GameSession session) {

    }

    private void HandleStage(GameSession session) {

    }

    private void HandleFireworks(GameSession session) {

    }

    private void HandleEmote(GameSession session, IByteReader packet) {
        int skillId = packet.ReadInt();
    }

    private bool TryUseInstrument(GameSession session, long itemUid, [NotNullWhen(true)] out FieldInstrument? fieldInstrument) {
        Item? instrument = session.Item.Inventory.Get(itemUid, InventoryType.FishingMusic);
        if (instrument == null || instrument.Metadata.Function?.Type != ItemFunction.OpenInstrument) {
            fieldInstrument = null;
            return false;
        }

        if (!int.TryParse(instrument.Metadata.Function.Parameters, out int instrumentId)) {
            Logger.Warning("Invalid parameters for OpenInstrument:{Params}", instrument.Metadata.Function.Parameters);
            fieldInstrument = null;
            return false;
        }

        if (!TableMetadata.InstrumentTable.Entries.TryGetValue(instrumentId, out InstrumentMetadata? metadata)) {
            fieldInstrument = null;
            return false;
        }

        fieldInstrument = session.Field!.SpawnInstrument(session.Player, metadata);
        return true;
    }

    private bool TryGetScore(GameSession session, long scoreUid, [NotNullWhen(true)] out Item? score) {
        score = session.Item.Inventory.Get(scoreUid, InventoryType.FishingMusic);
        if (score is not {RemainUses: > 0} || score.IsExpired()) {
            return false;
        }

        return true;
    }
}
