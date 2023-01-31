using System;
using System.Numerics;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using static Maple2.Model.Error.BuddyEmoteError;

namespace Maple2.Server.Game.PacketHandlers;

public class PremiumClubHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.PremiumClub;

    private enum Command : byte {
        LoadClaimedItems = 1,
        ClaimItem = 2,
        LoadPackages = 3,
        PurchasePackage = 4
    }
    
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var function = packet.Read<Command>();
        switch (function) {
            case Command.LoadClaimedItems:
                HandleLoadClaimedItems(session);
                return;
            case Command.ClaimItem:
                HandleClaimItem(session, packet);
                return;
            case Command.LoadPackages:
                HandleLoadPackages(session);
                return;
            case Command.PurchasePackage:
                HandlePurchasePackage(session, packet);
                break;
        }
    }

    private void HandleLoadClaimedItems(GameSession session) {
        session.Send(PremiumCubPacket.LoadItems(session.Player.Value.Account.PremiumRewardsClaimed));
    }

    private void HandleClaimItem(GameSession session, IByteReader packet) {
        int benefitId = packet.ReadInt();

        if (session.Player.Value.Account.PremiumTime < DateTime.Now.ToEpochSeconds()) {
            return;
        }

        if (!TableMetadata.PremiumClubTable.Items.TryGetValue(benefitId, out PremiumClubTable.Item? premiumMetadata) || 
            !ItemMetadata.TryGet(premiumMetadata.Id, out ItemMetadata? itemMetadata)) {
            return;
        }
        
        var item = new Item(itemMetadata, premiumMetadata.Rarity, premiumMetadata.Amount);
        if (!session.Item.Inventory.CanAdd(item)) {
            return;
        }

        if (session.Player.Value.Account.PremiumRewardsClaimed.Contains(benefitId)) {
            return;
        }
        
        session.Player.Value.Account.PremiumRewardsClaimed.Add(benefitId);

        session.Item.Inventory.Add(item, true);
        session.Send(PremiumCubPacket.ClaimItem(benefitId));
    }
    
    private void HandleLoadPackages(GameSession session) {
        session.Send(PremiumCubPacket.LoadPackages());
    }

    private void HandlePurchasePackage(GameSession session, IByteReader packet) {
        int packageId = packet.ReadInt();

        if (!TableMetadata.PremiumClubTable.Packages.TryGetValue(packageId, out PremiumClubTable.Package? premiumMetadata)) {
            return;
        }
        
        if (session.Currency.Meret < premiumMetadata.Price) {
            return;
        }
        
        session.Currency.Meret -= premiumMetadata.Price;

        foreach (PremiumClubTable.Item item in premiumMetadata.BonusItems) {
            if (!ItemMetadata.TryGet(item.Id, out ItemMetadata? itemMetadata)) {
                continue;
            }
            
            var bonusItem = new Item(itemMetadata, item.Rarity, item.Amount);
            if (!session.Item.Inventory.CanAdd(bonusItem)) {
                // Mail?
                return;
            }

            session.Item.Inventory.Add(bonusItem, true);
        }
        
        session.Send(PremiumCubPacket.PurchasePackage(packageId));
        session.Config.UpdatePremiumTime(premiumMetadata.Period);
    }
}
