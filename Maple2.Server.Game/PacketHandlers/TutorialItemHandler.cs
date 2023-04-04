using System.Linq;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class TutorialItemHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestTutorialItem;

    public override void Handle(GameSession session, IByteReader packet) {
        var jobTable = session.TableMetadata.JobTable;

        var jobCode = session.Player.Value.Character.Job.Code();

        if (!jobTable.Entries.TryGetValue(jobCode, out var jobMetadata)) return;
        
        if (session.Field != null && (session.Player.Value.Character.Level > 1 || jobMetadata.Tutorial.StartField != session.Field.MapId)) return;

        foreach (var tutorialItem in jobMetadata.Tutorial.StartItem) {
            var tutorialItemsInInventory = session.Item.Inventory.Find(tutorialItem.Id).Count();

            tutorialItemsInInventory += session.Item.Equips.Gear.Count(x => x.Value.Id == tutorialItem.Id);

            var tutorialItemsToSpawn = jobMetadata.Tutorial.StartItem.Count(x => x.Id == tutorialItem.Id);

            if (tutorialItemsInInventory >= tutorialItemsToSpawn) continue;

            if (!session.ItemMetadata.TryGet(tutorialItem.Id, out var itemMetadata)) continue;

            var item = new Item(itemMetadata, tutorialItem.Rarity);
            session.Item.Inventory.Add(item, true);
        }
    }
}
