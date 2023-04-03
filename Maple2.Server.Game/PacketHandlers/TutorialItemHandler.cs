using System;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class TutorialItemHandler : PacketHandler<GameSession>
{
    public override RecvOp OpCode => RecvOp.RequestTutorialItem;

    public override void Handle(GameSession session, IByteReader packet)
    {
        JobTable jobTable = session.TableMetadata.JobTable;

        JobCode jobCode = session.Player.Value.Character.Job.Code();

        Console.WriteLine(jobCode);

        if (!jobTable.Entries.TryGetValue(jobCode, out JobTable.Entry? jobMetadata))
        {
            return;
        }
        
        foreach (JobTable.Item tutorialItem in jobMetadata.Tutorial.StartItem)
        {
            if (!session.ItemMetadata.TryGet(tutorialItem.Id, out ItemMetadata? itemMetadata))
            {
                continue;
            }
            
            Item item = new Item(itemMetadata, tutorialItem.Rarity, tutorialItem.Count);
            session.Item.Inventory.Add(item, true);
        }
    }
}