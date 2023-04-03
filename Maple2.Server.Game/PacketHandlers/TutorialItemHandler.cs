using System.Linq;
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

        if (!jobTable.Entries.TryGetValue(jobCode, out JobTable.Entry? jobMetadata))
        {
            return;
        }

        foreach (JobTable.Item tutorialItem in jobMetadata.Tutorial.StartItem)
        {
            int tutorialItemsCount = session.Item.Inventory.Find(tutorialItem.Id).Count();

            tutorialItemsCount += session.Item.Equips.Gear.Count(x => x.Value.Id == tutorialItem.Id);

            if (tutorialItemsCount >= tutorialItem.Count)
            {
                continue;
            }

            if (!session.ItemMetadata.TryGet(tutorialItem.Id, out ItemMetadata? itemMetadata))
            {
                continue;
            }

            int countRemaining = tutorialItem.Count - tutorialItemsCount;

            Item item = new(itemMetadata, tutorialItem.Rarity, countRemaining);
            session.Item.Inventory.Add(item, true);
        }
    }
}
