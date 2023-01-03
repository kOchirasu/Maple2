using System;
using System.Collections.Generic;
using System.Linq;
using IronPython.Runtime;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScriptContext {
    // These MUST be initialized externally. They are not passed via constructor to simplify scripts.
    public readonly GameSession Session;
    public readonly FieldNpc Npc;
    public readonly ScriptMetadata Metadata;

    public NpcTalkType TalkType;
    public int State { get; private set; } = -1;
    public int Index { get; private set; } = 0;

    public NpcScriptContext(GameSession session, FieldNpc npc, ScriptMetadata metadata) {
        Session = session;
        Npc = npc;
        Metadata = metadata;

        TalkType = npc.Value.Metadata.Basic.Kind switch {
            1 or > 10 and < 20 => NpcTalkType.Dialog, // Shop
            >= 30 and < 40 => NpcTalkType.Dialog, // Beauty
            2 => NpcTalkType.Dialog, // Storage
            86 => NpcTalkType.Dialog, // TODO: BlackMarket
            88 => NpcTalkType.Dialog, // TODO: Birthday
            >= 100 and <= 104 => NpcTalkType.Dialog, // TODO: Sky Fortress
            >= 105 and <= 107 => NpcTalkType.Dialog, // TODO: Kritias
            108 => NpcTalkType.Dialog, // TODO: Humanitas
            501 => NpcTalkType.Dialog, // TODO: Roulette
            _ => NpcTalkType.None,
        };
    }

    public int NextState(int pick) {
        if (!Metadata.States.TryGetValue(State, out ScriptState? state)) {
            return -1;
        }

        CinematicContent? content = state.Contents.ElementAtOrDefault(Index);
        if (content == null) {
            return -1;
        }

        if (content.Distractors.Length > 0) {
            CinematicDistractor? distractor = content.Distractors.ElementAtOrDefault(pick);
            if (distractor == null || distractor.Goto.Length + distractor.GotoFail.Length != 1) {
                return -1;
            }

            // If there is a 1x Goto and 0x GotoFail we can infer the next state.
            return distractor.Goto.SingleOrDefault(-1);
        }

        if (Index >= state.Contents.Length - 1) {
            return -1;
        }

        return State;
    }

    public bool Respond(int state) {
        if (!Metadata.States.ContainsKey(state)) {
            Session.Send(NpcTalkPacket.Respond(Npc, TalkType, default));
            return false;
        }

        State = state;
        GetTalkType();
        var dialogue = new NpcDialogue(state, 0, GetButton());
        Session.Send(NpcTalkPacket.Respond(Npc, TalkType, dialogue));
        return true;
    }

    public bool Continue(int nextState, int questId = 0) {
        if (nextState != State && !Metadata.States.ContainsKey(nextState)) {
            Session.Send(NpcTalkPacket.Close());
            return false;
        }

        if (nextState == State) {
            Index++;
        } else {
            State = nextState;
            Index = 0;
        }

        var dialogue = new NpcDialogue(State, Index, GetButton());
        Session.Send(NpcTalkPacket.Continue(TalkType, dialogue, questId));
        return true;
    }

    private NpcTalkButton GetButton() {
        if (!Metadata.States.TryGetValue(State, out ScriptState? state)) {
            return NpcTalkButton.None;
        }

        CinematicContent? content = state.Contents.ElementAtOrDefault(Index);
        if (content == null) {
            return NpcTalkButton.None;
        }
        if (content.ButtonType != NpcTalkButton.None) {
            return content.ButtonType;
        }

        if (content.Distractors.Length > 0) {
            return NpcTalkButton.SelectableDistractor;
        }

        if (Index < state.Contents.Length - 1) {
            return NpcTalkButton.Next;
        }

        if (TalkType.HasFlag(NpcTalkType.Select)) {
            if (state.Contents.Length > 0) {
                return NpcTalkButton.SelectableTalk;
            }
            return NpcTalkButton.None;
        }

        switch (state.Type) {
            case ScriptStateType.Job:
                switch (Npc.Value.Metadata.Basic.Kind) {
                    case >= 30 and < 40: // Beauty
                        return NpcTalkButton.SelectableBeauty;
                    case 80:
                        return NpcTalkButton.ChangeJob;
                    case 81:
                        return NpcTalkButton.PenaltyResolve;
                    case 82:
                        return NpcTalkButton.TakeBoat;
                    case 501:
                        return NpcTalkButton.Roulette;
                }
                break;
            case ScriptStateType.Select:
                switch (Npc.Value.Metadata.Basic.Kind) {
                    case 1 or > 10 and < 20: // Shop
                        return NpcTalkButton.None;
                    case 2: // Storage
                        return NpcTalkButton.None;
                }
                return NpcTalkButton.SelectableTalk;
            case ScriptStateType.Quest:
                // return NpcTalkButton.QuestAccept;
                // return NpcTalkButton.QuestComplete;
                // return NpcTalkButton.QuestProgress;
                break;
        }

        return NpcTalkButton.Close;
    }

    public NpcTalkType GetTalkType() {

        if (!Metadata.States.TryGetValue(State, out ScriptState? state)) {
            return NpcTalkType.None;
        }

        CinematicContent? content = state.Contents.ElementAtOrDefault(Index);
        if (content == null) {
            return NpcTalkType.None;
        }

        if (state.Type == ScriptStateType.Select) {

            // Add options if there are scripts
            if (Metadata.States.Any(scriptState => scriptState.Value.Type == ScriptStateType.Script)) {
                switch (Npc.Value.Metadata.Basic.Kind) {
                    case 1 or > 10 and < 20: // Shop
                        TalkType |= NpcTalkType.Select | NpcTalkType.Talk;
                        return TalkType;
                    case >= 100 and <= 104:
                    case >= 105 and <= 107:
                    case 108:
                        TalkType |= NpcTalkType.Dialog;
                        return TalkType;
                }
            }
        }
        
        if (state.Type == ScriptStateType.Job) {
            TalkType |= NpcTalkType.Dialog;
        } else {
            TalkType |= NpcTalkType.Talk;
        }
        return TalkType;
    }

    public bool MovePlayer(int portalId) {
        if (Session.Field?.TryGetPortal(portalId, out FieldPortal? _) != true) {
            return false;
        }

        Session.Send(NpcTalkPacket.MovePlayer(portalId));
        return true;
    }

    public void OpenDialog(string name, string tags) {
        Session.Send(NpcTalkPacket.OpenDialog(name, tags));
    }

    public bool RewardItem(PythonList list) {
        var rewards = new List<(int Id, int Rarity, int Amount)>();
        foreach (PythonTuple tuple in list) {
            if (tuple.Count != 3 || tuple[0] is not int itemId || tuple[1] is not int rarity || tuple[2] is not int amount) {
                return false;
            }

            rewards.Add((itemId, rarity, amount));
        }

        var results = new List<Item>();
        foreach ((int id, int rarity, int amount) in rewards) {
            if (!Session.ItemMetadata.TryGet(id, out ItemMetadata? metadata)) {
                continue;
            }

            results.Add(new Item(metadata, rarity, amount));
        }

        // Validate that reward is possible
        foreach (IGrouping<InventoryType, Item> group in results.GroupBy(item => item.Inventory)) {
            int requireSlots = group.Count();
            int freeSlots = Session.Item.Inventory.FreeSlots(group.Key);
            if (requireSlots > freeSlots) {
                return false;
            }
        }

        // TODO: We should send to mail if we can't add to inventory to guarantee reward.
        using GameStorage.Request db = Session.GameStorage.Context();
        db.BeginTransaction();
        List<Item> created = results.Select(item => db.CreateItem(Session.CharacterId, item))
            .Where(item => item != null)
            .ToList()!;
        db.Commit();

        foreach (Item item in created) {
            Session.Item.Inventory.Add(item);
        }
        Session.Send(NpcTalkPacket.RewardItem(created));
        return true;
    }

    public void RewardExp(long exp) {
        // TODO: properly inc exp
        Session.Player.Value.Character.Exp += exp;
        Session.Send(NpcTalkPacket.RewardExp(exp));
    }

    public void RewardMeso(long mesos) {
        Session.Currency.Meso += mesos;
        Session.Send(NpcTalkPacket.RewardMeso(mesos));
    }

    public bool HasItem(int itemId, int rarity) {
        return Session.Item.Inventory.Find(itemId, rarity).Any();
    }

    public bool HasMesos(long amount) {
        return Session.Currency.Meso >= amount;
    }
}
