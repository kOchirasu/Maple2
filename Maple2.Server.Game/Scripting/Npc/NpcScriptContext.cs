using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Script.Npc;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScriptContext : INpcScriptContext {
    // These MUST be initialized externally. They are not passed via constructor to simplify scripts.
    private readonly GameSession session;
    private readonly FieldNpc npc;

    public NpcScriptContext(GameSession session, FieldNpc npc) {
        this.session = session;
        this.npc = npc;
    }

    public void Respond(NpcTalkType type, int id, NpcTalkButton button) {
        var dialogue = new NpcDialogue(id, 0, button);
        session.Send(NpcTalkPacket.Respond(npc, type, dialogue));
    }

    public bool Continue(NpcTalkType type, int id, int index, NpcTalkButton button, int questId = 0) {
        if (button == NpcTalkButton.None) {
            session.Send(NpcTalkPacket.Close());
            return false;
        }

        var dialogue = new NpcDialogue(id, index, button);
        session.Send(NpcTalkPacket.Continue(type, dialogue, questId));
        return true;
    }

    public NpcTalkType GetTalkType() {
        int kind = npc.Value.Metadata.Basic.Kind;
        return kind switch {
            1 or (> 10 and < 20) => NpcTalkType.Dialog, // Shop
            2 => NpcTalkType.Dialog, // Storage
            86 => NpcTalkType.Dialog, // TODO: BlackMarket
            88 => NpcTalkType.Dialog, // TODO: Birthday
            >= 100 and <= 104 => NpcTalkType.Dialog, // TODO: Sky Fortress
            >= 105 and <= 107 => NpcTalkType.Dialog, // TODO: Kritias
            _ => NpcTalkType.Talk,
        };
    }

    public bool MovePlayer(int portalId) {
        if (session.Field == null || !session.Field.TryGetPortal(portalId, out Portal? portal)) {
            return false;
        }

        session.Send(NpcTalkPacket.MovePlayer(portalId));
        return true;
    }

    public void OpenDialog(string name, string tags) {
        session.Send(NpcTalkPacket.OpenDialog(name, tags));
    }

    public void RewardItem(params (int Id, byte Rarity, int Amount)[] items) {
        var results = new List<Item>();
        foreach ((int id, byte rarity, int amount) in items) {
            if (!session.ItemMetadata.TryGet(id, out ItemMetadata? metadata)) {
                continue;
            }

            results.Add(new Item(metadata, rarity, amount));
        }

        // TODO: We should send to mail if we can't add to inventory to guarantee reward.
        using GameStorage.Request db = session.GameStorage.Context();
        db.BeginTransaction();
        List<Item> created = results.Select(item => db.CreateItem(session.CharacterId, item))
            .Where(item => item != null)
            .ToList()!;
        db.Commit();

        foreach (Item item in created) {
            session.Item.Inventory.Add(item);
        }
        session.Send(NpcTalkPacket.RewardItem(created));
    }

    public void RewardExp(long exp) {
        // TODO: properly inc exp
        session.Player.Value.Character.Exp += exp;
        session.Send(NpcTalkPacket.RewardExp(exp));
    }

    public void RewardMeso(long mesos) {
        session.Currency.Meso += mesos;
        session.Send(NpcTalkPacket.RewardMeso(mesos));
    }
}
