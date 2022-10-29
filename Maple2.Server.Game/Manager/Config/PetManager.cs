using System;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Serilog;

namespace Maple2.Server.Game.Manager.Config;

public sealed class PetManager : IDisposable {
    private readonly GameSession session;
    private readonly FieldPet pet;
    private readonly ItemCollection items;

    public Item Pet => pet.Pet;
    private readonly PetConfig config;

    private ItemPet ItemPet => Pet.Pet!;
    public int OwnerId => session.Player.ObjectId;

    public PetManager(GameSession session, FieldPet pet) {
        this.session = session;
        this.pet = pet;

        items = new ItemCollection(14); // TODO: PetSlotNum from xml

        using GameStorage.Request db = session.GameStorage.Context();
        config = db.GetPetConfig(Pet.Uid);
        foreach (Item item in db.GetStorage(Pet.Uid)) {
            if (items.Add(item).Count == 0) {
                Log.Error("Failed to add storage item:{Uid}", item.Uid);
            }
        }

        session.Field?.Broadcast(PetPacket.Summon(pet));
    }

    public void Load() {
        session.Send(PetPacket.Load(OwnerId, ItemPet, config));
    }

    public void BadgeChanged(ItemBadge? badge) {
        pet.UpdateSkin(badge?.PetSkinId ?? 0);
    }

    public void Rename(string name) {
        ItemPet.Name = name;
        ItemPet.RenameRemaining = 0;

        session.Send(PetPacket.Rename(OwnerId, ItemPet));
    }

    public void UpdatePotionConfig(PetPotionConfig[] potionConfig) {
        config.PotionConfig = potionConfig;

        session.Send(PetPacket.UpdatePotionConfig(OwnerId, config.PotionConfig));
    }

    public void UpdateLootConfig(PetLootConfig lootConfig) {
        config.LootConfig = lootConfig;

        session.Send(PetPacket.UpdateLootConfig(OwnerId, config.LootConfig));
    }

    public void Dispose() {
        using GameStorage.Request db = session.GameStorage.Context();
        lock (session.Item) {
            db.SavePetConfig(Pet.Uid, config);
            db.SaveItems(session.AccountId, items.ToArray());
        }

        session.Field?.RemovePet(pet.ObjectId);
        session.Field?.Broadcast(PetPacket.UnSummon(pet));
        session.Pet = null;
    }
}
