using System;
using System.Collections.Generic;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using static Maple2.Model.Error.MesoMarketError;

namespace Maple2.Server.Game.PacketHandlers;

public class MesoMarketHandler : PacketHandler<GameSession> {
    // TODO: Periodically update this by querying `meso-market-sold` table
    private const int AVERAGE_PRICE = 200;

    public override RecvOp OpCode => RecvOp.MesoMarket;

    private enum Command : byte {
        Load = 3,
        Create = 5,
        Cancel = 6,
        Search = 7,
        Purchase = 8,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Load:
                HandleLoad(session);
                return;
            case Command.Create:
                HandleCreate(session, packet);
                return;
            case Command.Cancel:
                HandleCancel(session, packet);
                return;
            case Command.Search:
                HandleSearch(session, packet);
                return;
            case Command.Purchase:
                HandlePurchase(session, packet);
                return;
        }
    }

    private static void HandleLoad(GameSession session) {
        session.Send(MesoMarketPacket.Load(AVERAGE_PRICE));
        session.Send(MesoMarketPacket.Quota(session.Player.Value.Account.MesoMarketListed, session.Player.Value.Account.MesoMarketPurchased));

        using GameStorage.Request db = session.GameStorage.Context();
        ICollection<MesoListing> myListings = db.GetMyMesoListings(session.AccountId);
        session.Send(MesoMarketPacket.MyListings(myListings));
    }

    private static void HandleCreate(GameSession session, IByteReader packet) {
        long amount = packet.ReadLong();
        long price = packet.ReadLong();

        if (amount != Constant.MesoMarketBasePrice) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_invalidSaleMoney));
            return;
        }

        const int delta = (int) (AVERAGE_PRICE * Constant.MesoMarketRangeRate);
        if (AVERAGE_PRICE + delta < price || price < AVERAGE_PRICE - delta) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_invalidBuyMerat));
            return;
        }

        // TODO Also check MyListings Count against Constant.MesoMarketListLimit (s_mesoMarket_error_maxCountOwnProduct)
        if (session.Player.Value.Account.MesoMarketListed >= Constant.MesoMarketListLimitDay) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_maxCountRegister));
            return;
        }

        if (session.Currency.Meso < amount) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_hasNotMeso));
            return;
        }

        var listing = new MesoListing(TimeSpan.FromDays(Constant.MesoMarketSellEndDay)) {
            AccountId = session.AccountId,
            Price = price,
            Amount = amount,
        };

        using GameStorage.Request db = session.GameStorage.Context();
        listing = db.CreateMesoListing(listing);
        if (listing == null) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_errorDB));
            return;
        }

        session.Player.Value.Account.MesoMarketListed++;
        session.Currency.Meso -= Constant.MesoMarketBasePrice;
        session.Send(MesoMarketPacket.Create(listing));
        session.Send(MesoMarketPacket.Quota(session.Player.Value.Account.MesoMarketListed, session.Player.Value.Account.MesoMarketPurchased));
    }

    private static void HandleCancel(GameSession session, IByteReader packet) {
        long listingId = packet.ReadLong();

        using GameStorage.Request db = session.GameStorage.Context();
        MesoListing? listing = db.GetMesoListing(listingId);
        if (listing == null || listing.AccountId != session.AccountId) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_notFoundProduct));
            return;
        }

        if (session.Currency.CanAddMeso(listing.Amount) != listing.Amount) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_unknown));
            return;
        }

        if (!db.DeleteMesoListing(listingId)) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_errorDB));
            return;
        }

        session.Currency.Meso += listing.Amount;
        session.Send(MesoMarketPacket.Cancel(listingId));
    }

    private static void HandleSearch(GameSession session, IByteReader packet) {
        long minMesos = packet.ReadLong();
        long maxMesos = packet.ReadLong();

        if (maxMesos < minMesos) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_invalidSearchParam));
            return;
        }

        try {
            using GameStorage.Request db = session.GameStorage.Context();
            ICollection<MesoListing> listings = db.SearchMesoListings(Constant.MesoMarketPageSize, minMesos, maxMesos);

            session.Send(MesoMarketPacket.Search(session.AccountId, listings));
        } catch(SystemException) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_errorDB));
        }
    }

    private static void HandlePurchase(GameSession session, IByteReader packet) {
        long listingId = packet.ReadLong();

        if (session.Player.Value.Account.MesoMarketPurchased >= Constant.MesoMarketPurchaseLimitMonth) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_maxCountBuy));
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        MesoListing? listing = db.GetMesoListing(listingId);
        if (listing == null) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_notFoundProduct));
            return;
        }

        if (listing.AccountId == session.AccountId) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_cannotBuyOwnProduct));
            return;
        }

        if (listing.ExpiryTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_cannotBuyExpireDate));
            return;
        }

        if (session.Currency[CurrencyType.MesoToken] < listing.Price) {
            session.Send(MesoMarketPacket.Error(s_err_lack_meso_makret_token_ask));
            return;
        }

        if (!db.DeleteMesoListing(listingId, true)) {
            session.Send(MesoMarketPacket.Error(s_mesoMarket_error_alreadySoldProduct));
            return;
        }

        // TODO: Send mail with merets to lister
        session.Player.Value.Account.MesoMarketPurchased++;
        session.Currency[CurrencyType.MesoToken] -= listing.Price;
        session.Currency.Meso += listing.Amount;
        session.Send(MesoMarketPacket.Purchase(listing.Id));
        session.Send(MesoMarketPacket.Quota(session.Player.Value.Account.MesoMarketListed, session.Player.Value.Account.MesoMarketPurchased));
    }
}
