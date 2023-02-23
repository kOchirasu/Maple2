using Maple2.Database.Storage;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class ShopHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Shop;
    
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required GameStorage GameStorage { private get; init; }
    // ReSharper restore All
    #endregion

    private enum Command : byte {
        PurchaseBuyBack = 3,
        Buy = 4,
        Sell = 5,
        InstantRestock = 9,
        Refresh = 10,
        LoadNew = 13,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var function = packet.Read<Command>();
        switch (function) {
            case Command.Buy:
                HandleBuy(session, packet);
                return;
        }
    }

    private void HandleBuy(GameSession session, IByteReader packet) {
        int shopItemId = packet.ReadInt();
        int quantity = packet.ReadInt();
    }
}
