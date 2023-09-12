﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.PacketLib.Tools;
using Maple2.Model.Game.Shop;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game;

public class GameServer : Server<GameSession> {
    private readonly object mutex = new();
    private readonly FieldManager.Factory fieldFactory;
    private readonly HashSet<GameSession> connectingSessions;
    private readonly Dictionary<long, GameSession> sessions;
    private readonly Dictionary<string, GameEvent> eventCache = new();
    private readonly ImmutableList<SystemBanner> bannerCache;
    private readonly ConcurrentDictionary<int, PremiumMarketItem> premiumMarketCache;
    private readonly Dictionary<int, Shop> shopCache = new();
    private readonly GameStorage gameStorage;
    private readonly ItemMetadataStorage itemMetadataStorage;

    public int Channel => Target.GameChannel;

    public GameServer(FieldManager.Factory fieldFactory, PacketRouter<GameSession> router, IComponentContext context, GameStorage gameStorage, ItemMetadataStorage itemMetadataStorage)
            : base(Target.GamePort, router, context) {
        this.fieldFactory = fieldFactory;
        connectingSessions = new HashSet<GameSession>();
        sessions = new Dictionary<long, GameSession>();
        this.gameStorage = gameStorage;

        using GameStorage.Request db = gameStorage.Context();
        bannerCache = db.GetBanners().ToImmutableList();
        premiumMarketCache = new ConcurrentDictionary<int, PremiumMarketItem>(
            db.GetMarketItems().Select(item => new KeyValuePair<int, PremiumMarketItem>(item.Id, item)));
        this.itemMetadataStorage = itemMetadataStorage;
    }

    public override void OnConnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions[session.CharacterId] = session;
        }
    }

    public override void OnDisconnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions.Remove(session.CharacterId);
        }
    }

    public bool GetSession(long characterId, [NotNullWhen(true)] out GameSession? session) {
        lock (mutex) {
            return sessions.TryGetValue(characterId, out session);
        }
    }

    protected override void AddSession(GameSession session) {
        lock (mutex) {
            connectingSessions.Add(session);
        }

        Logger.Information("Game client connecting: {Session}", session);
        session.Start();
    }

    public GameEvent? FindEvent<T>() where T : GameEventInfo {
        if (eventCache.TryGetValue(typeof(T).Name, out GameEvent? gameEvent)) {
            return gameEvent;
        }

        using GameStorage.Request db = gameStorage.Context();
        gameEvent = db.FindEvent(typeof(T).Name);
        if (gameEvent != null) {
            gameEvent.EventInfo.Id = gameEvent.Id;
            eventCache[typeof(T).Name] = gameEvent;
        }

        return gameEvent;
    }

    public Shop? FindShop(int shopId) {
        if (!shopCache.TryGetValue(shopId, out Shop? shop)) {
            using GameStorage.Request db = gameStorage.Context();
            shop = db.GetShop(shopId);
            if (shop?.RestockTime == 0) { // everything else would be player-based shops that would get refreshed
                shopCache[shopId] = shop;
                foreach (ShopItem item in db.GetShopItems(shopId)) {
                    if (!itemMetadataStorage.TryGet(item.ItemId, out ItemMetadata? metadata)) {
                        continue;
                    }
                    item.Item = new Item(metadata, item.Rarity, item.Quantity);
                    shop.Items.Add(item);
                }
            } else {
                return shop;
            }
        }

        return shopCache[shopId];
    }

    public IList<SystemBanner> GetSystemBanners() => bannerCache;

    public ICollection<PremiumMarketItem> GetPremiumMarketItems(params int[] tabIds) {
        if (tabIds.Length == 0) {
            return premiumMarketCache.Values;
        }

        return premiumMarketCache.Values.Where(item => tabIds.Contains(item.TabId)).ToList();
    }

    public PremiumMarketItem? GetPremiumMarketItem(int id, int subId) {
        if (subId == 0) {
            return premiumMarketCache.GetValueOrDefault(id);
        }

        return premiumMarketCache.TryGetValue(id, out PremiumMarketItem? item) ? item.AdditionalQuantities.FirstOrDefault(subItem => subItem.Id == subId) : null;
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        lock (mutex) {
            foreach (GameSession session in connectingSessions) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("GameServer Maintenance")));
                session.Dispose();
            }
            foreach (GameSession session in sessions.Values) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("GameServer Maintenance")));
                session.Dispose();
            }
            fieldFactory.Dispose();
        }

        return base.StopAsync(cancellationToken);
    }

    public void Broadcast(ByteWriter packet) {
        foreach (GameSession session in sessions.Values) {
            session.Send(packet);
        }
    }
}
