/*using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScript2 : ITalkScript {

    public readonly FieldNpc Npc;
    public SortedDictionary<int, QuestMetadata>? Quests;
    public ScriptState? ScriptState { get; private set; }

    public NpcScript2(GameSession session, FieldNpc npc, ScriptMetadata metadata, dynamic? script) : base (session, npc, metadata, (object) script) {
    }

    public new bool Begin() {
        SetTalkTypeFlags();
        NpcRespond();
        Console.WriteLine($"TalkType: {TalkType}, State: {State}");
        return true;
    }

    /*public bool Beginxx() {
        if (TalkType.HasFlag(NpcTalkType.Select)) {
            return NpcRespond(script?.select());
        }

        if (NpcRespond(script?.first())) {
            int functionId = GetFunctionId;
            if (functionId > 0) {
                script.enter_state(functionId);
            }
            return true;
        }
        return false;
    }#1#

    public bool BeginQuest() {
        TalkType = NpcTalkType.Quest;
        return QuestRespond();
    }

    public bool Continue(int pick) {
        int nextState = 0;
        if (TalkType == NpcTalkType.Quest) {
            nextState = NextState(pick);
        } else if (TalkType.HasFlag(NpcTalkType.Select)) {


        } else {
            nextState = Script.execute(State, Index, pick);
            if (nextState < 0) {
                nextState = NextState(pick);
            }
        }

        int questId = 0;
        if (Metadata.Type == ScriptType.Quest) {
            questId = Metadata.Id;
        }
        if ((nextState == 0) || (nextState != State && !Metadata.States.ContainsKey(nextState))) {
            Session.Send(NpcTalkPacket.Close());
            return false;
        }

        if (nextState == State) {
            Index++;
        } else {
            State = nextState;
            Index = 0;
        }

        ScriptState = Metadata.States[State];

        var dialogue = new NpcDialogue(State, Index, GetButton());
        Console.WriteLine($"Continue Normal: {TalkType}, State: {State}, Button: {dialogue.Button}");
        Session.Send(NpcTalkPacket.Continue(TalkType, dialogue, questId));
        return true;

        return true;
    }

    public void ExitState() {
        int functionId = GetFunctionId;
        if (functionId > 0) {
            Script.exit_state(functionId);
        }
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

    public bool NpcRespond() {
        /*if (!Metadata.States.ContainsKey(state)) {
            session.Send(NpcTalkPacket.Respond(Npc, TalkType, default));
            return false;
        }#1#

        var dialogue = new NpcDialogue(State, 0, GetButton());

        if (TalkType.HasFlag(NpcTalkType.Quest)) {
            Session.Send(QuestPacket.Talk(Npc, Quests!.Values));
        }

        Session.Send(NpcTalkPacket.Respond(Npc, TalkType, dialogue));
        return true;
    }

    public bool QuestRespond() {
        State = GetQuestScriptState(Metadata);
        ScriptState = Metadata.States[State];

        var dialogue = new NpcDialogue(State, Index, GetButton());
        Console.WriteLine($"Continue: {TalkType}, State: {State}, Button: {dialogue.Button}");
        Session.Send(NpcTalkPacket.Continue(TalkType, dialogue, Metadata.Id));

        return true;
    }

    /*public bool Continue(int nextState, int questId = 0) {
        if (Metadata.Type == ScriptType.Quest) {
            questId = Metadata.Id;
        }
        if ((nextState == 0) || (nextState != State && !Metadata.States.ContainsKey(nextState))) {
            session.Send(NpcTalkPacket.Close());
            return false;
        }

        if (nextState == State) {
            Index++;
        } else {
            State = nextState;
            Index = 0;
        }

        var dialogue = new NpcDialogue(State, Index, GetButton());
        session.Send(NpcTalkPacket.Continue(TalkType, dialogue, questId));
        return true;
    }#1#

    public void SetTalkTypeFlags() {
        if (Metadata.Type == ScriptType.Quest) {
            TalkType = NpcTalkType.Quest;
            return;
        }

        int options = 0;
        switch (Npc.Value.Metadata.Basic.Kind) {
            case 1 or > 10 and < 20: // Shop
                TalkType |= NpcTalkType.Dialog;
                options++;
                break;
            case >= 30 and < 40: // Beauty
            case 2: // Storage
            case 86: // TODO: BlackMarket
            case 88: // TODO: Birthday
            case >= 100 and <= 104: // TODO: Sky Fortress
            case >= 105 and <= 107: // TODO: Kritias
            case 108: // TODO: Humanitas
            case 501: // TODO: Roulette
                TalkType |= NpcTalkType.Dialog;
                break;
        }

        Quests = Session.Quest.GetAvailableQuests(Npc.Value.Id);
        if (Quests?.Count > 0) {
            TalkType |= NpcTalkType.Quest;
            options++;
        }

        ScriptState? scriptState = GetFirstScriptState();
        ScriptState? selectState = GetSelectScriptState();
        ScriptState? questState = GetFirstQuestScriptState();

        if (scriptState == null) {
            // pick select
        } else if (scriptState.Type == ScriptStateType.Job) {
            TalkType |= NpcTalkType.Dialog;
        } else {
            TalkType |= NpcTalkType.Talk;
            options++;
        }

        if (options > 1 && selectState != null) {
            TalkType |= NpcTalkType.Select;
        }

        // Determine which script to use.
        if (TalkType.HasFlag(NpcTalkType.Quest)) {
            if (TalkType.HasFlag(NpcTalkType.Select)) {
                State = selectState.Id;
                ScriptState = selectState;
                return;
            }

            State = questState.Id;
            ScriptState = questState;
            TalkType = NpcTalkType.Quest;
            return;
        }

        if (TalkType.HasFlag(NpcTalkType.Select)) {
            State = selectState.Id;
            ScriptState = selectState;
            return;
        }

        State = scriptState.Id;
        ScriptState = scriptState;
    }

    public ScriptState? GetFirstScriptState() {
        if (Metadata.States.Values.Any(state => state.Type == ScriptStateType.Job) ||
            Metadata.States.Values.Count(state => state.Pick) > 1) {
            // TODO: Fail check if script is somehow null.
            int scriptFirst = Script!.first();
            return Metadata.States.TryGetValue(scriptFirst, out ScriptState? scriptState) ? scriptState : null;
        }

        return Metadata.States.Values.FirstOrDefault(state => state.Pick);
    }

    public ScriptState? GetSelectScriptState() {
        if (Metadata.States.Values.Count(state => state.Type == ScriptStateType.Select) > 1) {
            // TODO: check if null
            int select = Script!.select();
            return Metadata.States.TryGetValue(select, out ScriptState? scriptState) ? scriptState : null;
        }
        return Metadata.States.Values.FirstOrDefault(select => select.Type == ScriptStateType.Select);
    }

    public ScriptState? GetFirstQuestScriptState() {
        if (Quests == null || Quests.Count == 0) {
            return null;
        }

        if (!Session.ScriptMetadata.TryGet(Quests.Min().Key, out ScriptMetadata? metadata)) {
            return null;
        }

        int state = GetQuestScriptState(metadata);
        return metadata.States.TryGetValue(state, out ScriptState? scriptState) ? scriptState : null;
    }

    public int GetQuestScriptState(ScriptMetadata metadata) {
        if (!Session.Quest.TryGetQuest(metadata.Id, out Quest? quest)) {
            // return -1;
        }

        QuestState questState = quest?.State ?? QuestState.None;

        State = questState switch {
            QuestState.None => GetFirstStateScript(metadata.States.Keys.ToArray(), 100, 200),
            QuestState.Started => GetFirstStateScript(metadata.States.Keys.ToArray(), 200, 300),
            _ => State,
        };

        if (quest != null && Session.Quest.CanStart(quest.Metadata.Require)) {
            State = GetFirstStateScript(metadata.States.Keys.ToArray(), 200, 300);
        }

        if (quest != null && quest.Metadata.Basic.CompleteNpc == Npc.Value.Id) {
            State = GetFirstStateScript(metadata.States.Keys.ToArray(), 300, 400);
        }

        return State;

        int GetFirstStateScript(int[] questStates, int lowerBound, int upperBound) {
            var statesInRange = questStates.Where(id => id >= lowerBound && id < upperBound);
            return statesInRange.Min();
        }
    }

    public int GetFunctionId => Metadata.States[State].Contents.ElementAt(Index).FunctionId;

    private NpcTalkButton GetButton() {
        if (ScriptState == null) {
            return NpcTalkButton.None;
        }

        CinematicContent? content = ScriptState.Contents.ElementAtOrDefault(Index);
        if (content == null) {
            return NpcTalkButton.None;
        }
        if (content.ButtonType != NpcTalkButton.None) {
            return content.ButtonType;
        }

        if (content.Distractors.Length > 0) {
            return NpcTalkButton.SelectableDistractor;
        }

        if (Index < ScriptState.Contents.Length - 1) {
            return NpcTalkButton.Next;
        }

        if (TalkType.HasFlag(NpcTalkType.Select)) {
            if (ScriptState.Contents.Length > 0) {
                return NpcTalkButton.SelectableTalk;
            }
            return NpcTalkButton.None;
        }

        switch (ScriptState.Type) {
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
                return (State / 100) switch {
                    1 => NpcTalkButton.QuestAccept,
                    2 => NpcTalkButton.QuestProgress,
                    3 => NpcTalkButton.QuestComplete,
                    _ => NpcTalkButton.None,
                };
        }

        return NpcTalkButton.Close;
    }
}

public class QuestScript : TalkScript {
    
    public QuestScript(GameSession session, FieldNpc npc, ScriptMetadata metadata, dynamic? script) : base (session, npc, metadata, (object) script) {
    }

    public new bool Begin() {
        TalkType = NpcTalkType.Quest;
        return QuestRespond();
    }
    
    public bool Continue(int pick) {
        int nextState = 0;
        if (TalkType == NpcTalkType.Quest) {
            nextState = NextState(pick);
        } else if (TalkType.HasFlag(NpcTalkType.Select)) {


        } else {
            nextState = Script.execute(State, Index, pick);
            if (nextState < 0) {
                nextState = NextState(pick);
            }
        }

        int questId = 0;
        if (Metadata.Type == ScriptType.Quest) {
            questId = Metadata.Id;
        }
        if ((nextState == 0) || (nextState != State && !Metadata.States.ContainsKey(nextState))) {
            Session.Send(NpcTalkPacket.Close());
            return false;
        }

        if (nextState == State) {
            Index++;
        } else {
            State = nextState;
            Index = 0;
        }

        ScriptState = Metadata.States[State];

        var dialogue = new NpcDialogue(State, Index, GetButton());
        Console.WriteLine($"Continue Normal: {TalkType}, State: {State}, Button: {dialogue.Button}");
        Session.Send(NpcTalkPacket.Continue(TalkType, dialogue, questId));
        return true;

        return true;
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

    public bool Respond() {
        State = GetQuestScriptState(Metadata);
        ScriptState = Metadata.States[State];

        var dialogue = new NpcDialogue(State, Index, GetButton());
        Console.WriteLine($"Continue: {TalkType}, State: {State}, Button: {dialogue.Button}");
        Session.Send(NpcTalkPacket.Continue(TalkType, dialogue, Metadata.Id));

        return true;
    }

    public ScriptState? GetFirstScriptState() {
        if (Metadata.States.Values.Any(state => state.Type == ScriptStateType.Job) ||
            Metadata.States.Values.Count(state => state.Pick) > 1) {
            // TODO: Fail check if script is somehow null.
            int scriptFirst = Script!.first();
            return Metadata.States.TryGetValue(scriptFirst, out ScriptState? scriptState) ? scriptState : null;
        }

        return Metadata.States.Values.FirstOrDefault(state => state.Pick);
    }

    public ScriptState? GetSelectScriptState() {
        if (Metadata.States.Values.Count(state => state.Type == ScriptStateType.Select) > 1) {
            // TODO: check if null
            int select = Script!.select();
            return Metadata.States.TryGetValue(select, out ScriptState? scriptState) ? scriptState : null;
        }
        return Metadata.States.Values.FirstOrDefault(select => select.Type == ScriptStateType.Select);
    }

    public ScriptState? GetFirstQuestScriptState() {
        if (Quests == null || Quests.Count == 0) {
            return null;
        }

        if (!Session.ScriptMetadata.TryGet(Quests.Min().Key, out ScriptMetadata? metadata)) {
            return null;
        }

        int state = GetQuestScriptState(metadata);
        return metadata.States.TryGetValue(state, out ScriptState? scriptState) ? scriptState : null;
    }

    public int GetQuestScriptState(ScriptMetadata metadata) {
        if (!Session.Quest.TryGetQuest(metadata.Id, out Quest? quest)) {
            // return -1;
        }

        QuestState questState = quest?.State ?? QuestState.None;

        State = questState switch {
            QuestState.None => GetFirstStateScript(metadata.States.Keys.ToArray(), 100, 200),
            QuestState.Started => GetFirstStateScript(metadata.States.Keys.ToArray(), 200, 300),
            _ => State,
        };

        if (quest != null && Session.Quest.CanStart(quest.Metadata.Require)) {
            State = GetFirstStateScript(metadata.States.Keys.ToArray(), 200, 300);
        }

        if (quest != null && quest.Metadata.Basic.CompleteNpc == Npc.Value.Id) {
            State = GetFirstStateScript(metadata.States.Keys.ToArray(), 300, 400);
        }

        return State;

        int GetFirstStateScript(int[] questStates, int lowerBound, int upperBound) {
            var statesInRange = questStates.Where(id => id >= lowerBound && id < upperBound);
            return statesInRange.Min();
        }
    }
}*/