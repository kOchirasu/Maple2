using System.Collections.Generic;
using System.Linq;
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

    public readonly NpcTalkType TalkType;
    public int State { get; private set; } = -1;
    public int Index { get; private set; } = 0;

    public NpcScriptContext(GameSession session, FieldNpc npc, ScriptMetadata metadata) {
        Session = session;
        Npc = npc;
        Metadata = metadata;

        TalkType = npc.Value.Metadata.Basic.Kind switch {
            1 or > 10 and < 20 => NpcTalkType.Dialog, // Shop
            2 => NpcTalkType.Dialog, // Storage
            86 => NpcTalkType.Dialog, // TODO: BlackMarket
            88 => NpcTalkType.Dialog, // TODO: Birthday
            >= 100 and <= 104 => NpcTalkType.Dialog, // TODO: Sky Fortress
            >= 105 and <= 107 => NpcTalkType.Dialog, // TODO: Kritias
            108 => NpcTalkType.Dialog, // TODO: Humanitas
            501 => NpcTalkType.Dialog, // TODO: Roulette
            _ => NpcTalkType.Talk,
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

    public void RewardItem(params (int Id, byte Rarity, int Amount)[] items) {
        var results = new List<Item>();
        foreach ((int id, byte rarity, int amount) in items) {
            if (!Session.ItemMetadata.TryGet(id, out ItemMetadata? metadata)) {
                continue;
            }

            results.Add(new Item(metadata, rarity, amount));
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
}
