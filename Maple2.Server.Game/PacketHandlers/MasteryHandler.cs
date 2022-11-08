using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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

public class MAsteryHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Mastery;

    private enum Command : byte {
        Reward = 1,
        Craft = 2,
    }
    
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Craft:
                HandleCraft(session, packet);
                break;
        }
    }

    private void HandleCraft(GameSession session, IByteReader packet) {
        int recipeId = packet.ReadInt();

        if (!TableMetadata.MasteryRecipeTable.Entries.TryGetValue(recipeId, out MasteryRecipeTable.Entry? entry)) {
            return;
        }
        
        // TODO: Check if player has completed the required quests.
        
        //if (session.Player.Value.Character.Mastery < entry.RequiredMastery)
        
        if (entry.RequiredItems.Any(ingredient => session.Item.Inventory.Find(ingredient.ItemId, ingredient.Rarity).ToList().Count < ingredient.Amount)) {
            return;
        }
        
        if (session.Currency.CanAddMeso(-entry.RequiredMeso) != -entry.RequiredMeso) { 
            return;
        }
    }
}
