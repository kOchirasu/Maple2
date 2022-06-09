using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.PacketHandlers;

public class NpcTalkHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.NPC_TALK;

    private enum Command : byte {
        EndTalk = 0,
        Talk = 1,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public NpcMetadataStorage NpcMetadata { get; init; } = null!;
    // ReSharper restore All
    #endregion

    public NpcTalkHandler(ILogger<NpcTalkHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.EndTalk:
                return;
            case Command.Talk:
                HandleTalk(session, packet);
                return;
        }
    }

    private void HandleTalk(GameSession session, IByteReader packet) {
        int objectId = packet.ReadInt();
        if (session.Field == null || !session.Field.TryGetNpc(objectId, out FieldNpc? npc)) {
            return; // Invalid Npc
        }

        if (!NpcMetadata.TryGet(npc.Value.Id, out NpcMetadata? metadata)) {
            return;
        }
    }
}
