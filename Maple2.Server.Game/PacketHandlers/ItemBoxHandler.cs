using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class ItemBoxHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestItemBox;

    public override void Handle(GameSession session, IByteReader packet) {
        int itemId = packet.ReadInt();
        short unk = packet.ReadShort();
        int count = packet.ReadInt();
        short unk2 = packet.ReadShort(); // is 1 on select item box, 0 on open item box
        int index = 0;
        Item? item = session.Item.Inventory.Find(itemId).FirstOrDefault();
        if (item == null) {
            return;
        }
        
        if (item.Metadata.Function?.Type == ItemFunction.SelectItemBox) {
            index = packet.ReadShort() - 48;
        }
        ItemBoxError error = session.ItemBox.Open(item, count, index);
        session.Send(ItemBoxPacket.Open(itemId, session.ItemBox.BoxCount, error));
        session.ItemBox.Reset();
    }
}
