using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScript {
    private readonly dynamic? script;
    private readonly GameSession session;

    public readonly FieldNpc Npc;
    private readonly ScriptMetadata metadata;
    public SortedDictionary<int, QuestMetadata> Quests = new();

    public NpcTalkType TalkType;
    public NpcTalkButton Button;
    
    // Need to find a way to remove this. It's mainly used if the
    private ScriptState? ScriptState { get; set; }
    public int State { get; private set; } = -1;
    public int Index { get; private set; } = 0;

    public NpcScript(GameSession session, FieldNpc npc, ScriptMetadata metadata, dynamic? script) {
        this.script = script;
        this.session = session;
        Npc = npc;
        this.metadata = metadata;
    }

    public bool Begin() {
        if (!SetInitialScript()) {
            return false;
        }
        NpcRespond();
        return true;
    }

    public bool BeginQuest() {
        TalkType = NpcTalkType.Quest;
        return QuestRespond();
    }

    public void EnterDialog() {
        // Basic shops are treated slightly different
        if (Npc.Value.Metadata.Basic.Kind is 1 or > 10 and < 20) {
            TalkType = NpcTalkType.Talk;
            State = 0;
            Button = GetButton();
            session.Send(NpcTalkPacket.Continue(TalkType, new NpcDialogue(State, Index, Button)));
            return;
        }

        EnterTalk();
    }

    public void EnterTalk() {
        ScriptState? scriptState = GetFirstScriptState();
        if (scriptState == null) {
            return;
        }
        TalkType = scriptState.Type == ScriptStateType.Script ? NpcTalkType.Talk : NpcTalkType.Dialog;

        ScriptState = scriptState;
        State = scriptState.Id;
        Button = GetButton();
    }

    public bool Continue(int pick) {
        int nextState = NextState(pick);
        if (nextState < 0) {
            session.Send(NpcTalkPacket.Close());
            return false;
        }

        int questId = 0;
        if (metadata.Type == ScriptType.Quest) {
            questId = metadata.Id;
        }
        if ((nextState == 0) || (nextState != State && !metadata.States.ContainsKey(nextState))) {
            session.Send(NpcTalkPacket.Close());
            return false;
        }

        if (nextState == State) {
            Index++;
        } else {
            State = nextState;
            Index = 0;
        }

        ScriptState = metadata.States[State];
        Button = GetButton();

        var dialogue = new NpcDialogue(State, Index, Button);
        session.Send(NpcTalkPacket.Continue(TalkType, dialogue, questId));
        return true;
    }
    
    public void EnterState() {
        int functionId = GetFunctionId;
        if (functionId > 0 && script != null) {
            script!.enter_state(functionId);
        }
    }

    public void ExitState() {
        int functionId = GetFunctionId;
        if (functionId > 0 && script != null) {
            script!.exit_state(functionId);
        }
    }

    private int NextState(int pick) {
        if (!metadata.States.TryGetValue(State, out ScriptState? state)) {
            return -1;
        }

        CinematicContent? content = state.Contents.ElementAtOrDefault(Index);
        if (content == null) {
            return -1;
        }

        if (content.Distractors.Length > 0) {
            CinematicDistractor? distractor = content.Distractors.ElementAtOrDefault(pick);
            if (distractor == null || distractor.Goto.Length + distractor.GotoFail.Length != 1) {
                return script?.execute(State, Index, pick) ?? -1;
            }

            // If there is a 1x Goto and 0x GotoFail we can infer the next state.
            return distractor.Goto.SingleOrDefault(-1);
        }

        if (Index >= state.Contents.Length - 1) {
            return -1;
        }

        return State;
    }

    private void NpcRespond() {
        var dialogue = new NpcDialogue(State, 0, GetButton());

        if (TalkType.HasFlag(NpcTalkType.Quest)) {
            session.Send(QuestPacket.Talk(Npc, Quests!.Values));
        }

        session.Send(NpcTalkPacket.Respond(Npc, TalkType, dialogue));
        return;
    }

    private bool QuestRespond() {
        State = GetQuestScriptState(metadata);
        if (State == 0) {
            session.Send(NpcTalkPacket.Close());
            return false;
        }
        ScriptState = metadata.States[State];
        Button = GetButton();

        var dialogue = new NpcDialogue(State, Index, Button);
        session.Send(NpcTalkPacket.Continue(TalkType, dialogue, metadata.Id));

        return true;
    }

    private bool SetInitialScript() {
        if (metadata.Type == ScriptType.Quest) {
            TalkType = NpcTalkType.Quest;
            return true;
        }

        ScriptState? scriptState = GetFirstScriptState();
        ScriptState? selectState = GetSelectScriptState();
        ScriptState? questState = GetFirstQuestScriptState();

        int options = 0;
        if (Npc.Value.Metadata.Basic.ShopId > 0) {
            TalkType |= NpcTalkType.Dialog;
            options++;
        }

        Quests = session.Quest.GetAvailableQuests(Npc.Value.Id);
        if (Quests?.Count > 0) {
            TalkType |= NpcTalkType.Quest;
            options++;
        }


        if (scriptState?.Type == ScriptStateType.Job) {
            // Job script only counts as an additional option if quests are present.
            if (TalkType.HasFlag(NpcTalkType.Quest)) {
                options++;
            }
            TalkType |= NpcTalkType.Dialog;
        } else {
            TalkType |= NpcTalkType.Talk;
            options++;
        }

        if (options > 1 && selectState != null) {
            TalkType |= NpcTalkType.Select;
        }

        switch (Npc.Value.Metadata.Basic.Kind) {
            case >= 30 and < 40: // Beauty
            case 2: // Storage
            case 86: // TODO: BlackMarket
            case 88: // TODO: Birthday
            case 501: // TODO: Roulette
                TalkType |= NpcTalkType.Dialog;
                break;
            case >= 100 and <= 104: // Sky Fortress
            case >= 105 and <= 107: // Kritias
            case 108: // Humanitas
                TalkType = NpcTalkType.Dialog;
                State = selectState?.Id ?? 0; 
                ScriptState = selectState;
                return true;
        }

        // Determine which script to use.
        if (TalkType.HasFlag(NpcTalkType.Select)) {
            State = selectState!.Id;
            ScriptState = selectState;
            return true;
        }
        
        if (TalkType.HasFlag(NpcTalkType.Quest)) {
            if (questState == null) {
                return false;
            }
            State = questState.Id;
            ScriptState = questState;
            TalkType = NpcTalkType.Quest;
            return true;
        }

        if (scriptState == null && selectState == null) {
            return false;
        }
        State = scriptState?.Id ?? selectState?.Id ?? 0;
        ScriptState = scriptState ?? selectState;
        return true;
    }

    private ScriptState? GetFirstScriptState() {
        if (metadata.States.Values.Any(state => state.Type == ScriptStateType.Job) ||
            metadata.States.Values.Count(state => state.Pick) > 1) {
            if (script == null) {
                return null;
            }
            int scriptFirst = script.first();
            return metadata.States.TryGetValue(scriptFirst, out ScriptState? scriptState) ? scriptState : null;
        }

        return metadata.States.Values.FirstOrDefault(state => state.Pick);
    }

    private ScriptState? GetSelectScriptState() {
        if (metadata.States.Values.Count(state => state.Type == ScriptStateType.Select) > 1) {
            if (script == null) {
                return null;
            }
            int select = script.select();
            return metadata.States.TryGetValue(select, out ScriptState? scriptState) ? scriptState : null;
        }
        return metadata.States.Values.FirstOrDefault(select => select.Type == ScriptStateType.Select);
    }

    private ScriptState? GetFirstQuestScriptState() {
        if (Quests.Count == 0) {
            return null;
        }

        if (!session.ScriptMetadata.TryGet(Quests.Keys.Min(), out ScriptMetadata? questMetadata)) {
            return null;
        }

        int state = GetQuestScriptState(questMetadata);
        return questMetadata.States.TryGetValue(state, out ScriptState? scriptState) ? scriptState : null;
    }

    private int GetQuestScriptState(ScriptMetadata scriptMetadata) {
        session.Quest.TryGetQuest(scriptMetadata.Id, out Quest? quest);
        QuestState questState = quest?.State ?? QuestState.None;

        State = questState switch {
            QuestState.None => GetFirstStateScript(scriptMetadata.States.Keys.ToArray(), 100, 200),
            QuestState.Started => GetFirstStateScript(scriptMetadata.States.Keys.ToArray(), 200, 300),
            _ => State,
        };

        if (quest != null && session.Quest.CanStart(quest.Metadata.Require)) {
            State = GetFirstStateScript(scriptMetadata.States.Keys.ToArray(), 200, 300);
        }

        if (quest != null && quest.Metadata.Basic.CompleteNpc == Npc.Value.Id) {
            State = GetFirstStateScript(scriptMetadata.States.Keys.ToArray(), 300, 400);
        }

        return State;

        int GetFirstStateScript(IEnumerable<int> questStates, int lowerBound, int upperBound) {
            IEnumerable<int> statesInRange = questStates.Where(id => id >= lowerBound && id < upperBound);
            return statesInRange.Min();
        }
    }

    private int GetFunctionId => metadata.States[State].Contents.ElementAt(Index).FunctionId;

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
                switch (State / 100) {
                    case 1:
                        return NpcTalkButton.QuestAccept;
                    case 2:
                        return NpcTalkButton.QuestProgress;
                    case 3:
                        return NpcTalkButton.QuestComplete;
                }
                break;
        }

        return NpcTalkButton.Close;
    }
}
