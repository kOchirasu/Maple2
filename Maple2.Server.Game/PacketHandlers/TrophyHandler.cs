using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class TrophyHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Achieve;

    private enum Command : byte {
        Reward = 3,
        ToggleFavorite = 4,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Reward:
                HandleReward(session, packet);
                break;
            case Command.ToggleFavorite:
                HandleToggleFavorite(session, packet);
                break;
        }
    }

    private void HandleReward(GameSession session, IByteReader packet) {
        int trophyId = packet.ReadInt();
        
        session.Trophy.Reward(trophyId, true);
    }
    
    private void HandleToggleFavorite(GameSession session, IByteReader packet) {
        int trophyId = packet.ReadInt();
        bool favorite = packet.ReadBool();

        if (!session.Trophy.Values.TryGetValue(trophyId, out TrophyEntry? trophy)) {
            return;
        }
        
        trophy.Favorite = favorite;
    }
}
