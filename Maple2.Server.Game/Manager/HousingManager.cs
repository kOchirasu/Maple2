using System;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Manager;

public class HousingManager {
    private readonly GameSession session;

    private readonly ILogger logger = Log.Logger.ForContext<HousingManager>();

    public HousingManager(GameSession session) {
        this.session = session;
    }

    public void SetPlot(Plot? plot) {
        session.Player.Value.Home.Plot = plot;

        session.Field?.Multicast(CubePacket.UpdateHomeProfile(session.Player));
    }

    public void SetHomeName(string name) {
        session.Player.Value.Home.Name = name;
        Plot? plot = session.Player.Value.Home.Plot;
        if (plot != null) {
            plot.Name = name;
        }

        session.Field?.Multicast(CubePacket.SetHomeName(session.Player.Value.Home), sender: session);
        session.Field?.Multicast(CubePacket.UpdateHomeProfile(session.Player));
    }

    public bool BuyPlot(int plotNumber) {
        Plot? plot = session.Player.Value.Home.Plot;
        if (plot != null) {
            session.Send(plot.Number == plotNumber
                ? CubePacket.Error(UgcMapError.s_ugcmap_my_house)
                : CubePacket.Error(UgcMapError.s_ugcmap_cant_buy_more_than_two_house));
            return false;
        }

        if (session.Field == null || !session.Field.Plots.TryGetValue(plotNumber, out plot)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_not_a_buyable));
            return false;
        }

        if (plot.OwnerId != 0 || plot.State != PlotState.Open) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_already_owned));
            return false;
        }

        UgcMapGroup.Cost contract = plot.Metadata.ContractCost;
        if (!DeductCost(session, contract)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_not_enough_money));
            return false;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        plot = db.BuyPlot(session.AccountId, plot, TimeSpan.FromDays(contract.Days));
        if (plot == null) {
            logger.Warning("Failed to buy plot");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return false;
        }
        if (session.Field.UpdatePlot(plot) != true) {
            logger.Warning("Failed to update map plot in field");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return false;
        }

        session.Field.Multicast(CubePacket.BuyPlot(plot, session.Player.Value.Home.Name));
        SetPlot(plot);
        return true;
    }

    public Plot? ForfeitPlot() {
        Plot? plot = session.Player.Value.Home.Plot;
        if (plot == null) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_dont_have_ownership));
            return null;
        }

        if (DateTime.UtcNow - plot.ExpiryTime.FromEpochSeconds() > TimeSpan.FromDays(3)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_not_extension_date));
            return null;
        }

        if (session.Field == null || !session.Field.Plots.TryGetValue(plot.Number, out plot)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return null;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        plot = db.ForfeitPlot(session.AccountId, plot);
        if (plot == null) {
            logger.Warning("Failed to forfeit plot");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return null;
        }
        if (session.Field.UpdatePlot(plot) != true) {
            logger.Warning("Failed to update map plot in field");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return null;
        }

        session.Field.Multicast(CubePacket.ForfeitPlot(plot));
        SetPlot(null);

        return plot;
    }

    public void ExtendPlot() {
        Plot? plot = session.Player.Value.Home.Plot;
        if (plot == null) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_dont_have_ownership));
            return;
        }

        if (plot.State != PlotState.Taken || plot.ExpiryTime <= DateTime.UtcNow.ToEpochSeconds()) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_expired_salable_group));
            return;
        }

        if (plot.ExpiryTime.FromEpochSeconds() - DateTime.UtcNow > TimeSpan.FromDays(30)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_not_extension_date));
            return;
        }

        UgcMapGroup.Cost extension = plot.Metadata.ExtensionCost;
        if (!DeductCost(session, extension)) {
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_need_extansion_pay));
            return;
        }

        TimeSpan extendTime = TimeSpan.FromDays(extension.Days);
        plot.ExpiryTime += (int) extendTime.TotalSeconds;
        using GameStorage.Request db = session.GameStorage.Context();
        if (!db.ExtendPlot(plot, extendTime)) {
            logger.Warning("Failed to extend plot");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return;
        }
        if (session.Field?.UpdatePlot(plot) != true) {
            logger.Warning("Failed to update map plot in field");
            session.Send(CubePacket.Error(UgcMapError.s_ugcmap_system_error));
            return;
        }

        session.Send(CubePacket.ExtendPlot(plot));
    }

    private static bool DeductCost(GameSession session, UgcMapGroup.Cost cost) {
        switch (cost.ItemId) {
            case >= 90000001 and <= 90000003:
                if (session.Currency.Meso < cost.Amount) {
                    session.Send(CubePacket.Error(UgcMapError.s_err_ugcmap_not_enough_meso_balance));
                    return false;
                }

                session.Currency.Meso -= cost.Amount;
                return true;
            case 90000004 or 90000011 or 90000015 or 90000016:
                if (session.Currency.Meret < cost.Amount) {
                    session.Send(CubePacket.Error(UgcMapError.s_err_ugcmap_not_enough_merat_balance));
                    return false;
                }

                session.Currency.Meret -= cost.Amount;
                return true;
        }

        return false;
    }
}
