using System;

namespace Maple2.Model.Enum; 

[Flags]
public enum NpcTalkType {
    /// <summary>
    /// Simple NpcTalk without CinematicComponent, used for UIDialogs.
    ///     sub_649B00(uiTalkMgr, npcId, 1)
    /// </summary>
    Chat = 1,
    Talk = 2,
    Quest = 4,
    /// <summary>
    /// Similar to '1':
    ///     sub_649B00(uiTalkMgr, npcId, 0)
    /// </summary>
    Flag4 = 8,
    Component = 16,
    /// <summary>
    /// Seems to affect 'SelectableTalk' only
    /// </summary>
    Flag6 = 32,
}

public enum NpcTalkComponent {
    None = 0,
    /// <summary>
    /// No options
    /// </summary>
    Empty = 1,
    /// <summary>
    /// <c>s_itemenchant_cinematic_btn</c> Quit:$key:1$
    /// </summary>
    Stop = 2,
    /// <summary>
    /// <c>s_quest_talk_end</c> Close\n$key:57$
    /// </summary>
    Close = 3,
    /// <summary>
    /// <c>s_quest_talk_progress</c> Next\n$key:57$|Close\n$key:1$
    /// </summary>
    Next = 4,
    /// <summary>
    /// A lot of branching logic, handles many cases
    /// </summary>
    SelectableTalk = 5,
    /// <summary>
    /// <c>s_quest_talk_accept</c> Accept\n$key:57$|Decline\n$key:1$
    /// </summary>
    QuestAccept = 6,
    /// <summary>
    /// <c>s_quest_talk_complete</c> Complete\n$key:57$|Close\n$key:1$
    /// </summary>
    QuestComplete = 7,
    /// <summary>
    /// <c>s_quest_talk_end</c> Close\n$key:57$
    /// </summary>
    QuestProgress = 8,
    /// <summary>
    /// s_quest_talk_progress OR s_quest_talk_end
    /// </summary>
    SelectableDistractor = 9,
    /// <summary>
    /// s_quest_talk_progress OR s_quest_talk_end
    /// </summary>
    SelectableBeauty = 10,
    /// <summary>
    /// <c>s_changejob_accept</c> Perform Job Advancement\n($key:57$)|Nevermind\n($key:1$)
    /// </summary>
    ChangeJob = 11,
    /// <summary>
    /// <c>s_quest_talk_accept</c> Accept\n$key:57$|Decline\n$key:1$
    /// </summary>
    UgcSign = 12,
    /// <summary>
    /// <c>s_resolve_panelty_accept</c> Get Treatment\n$key:57$|Decline\n$key:1$
    /// </summary>
    PenaltyResolve = 13,
    /// <summary>
    /// <c>s_take_boat_accept</c> Go\n$key:57$|Stay\n$key:1$
    /// </summary>
    TakeBoat = 14,
    /// <summary>
    /// <c>s_sell_ugc_map_accept</c> Confirm\n$key:57$|Cancel\n$key:1$
    /// </summary>
    SellUgcMap = 15,
    /// <summary>
    /// <c>s_roulette_accept</c> Spin\n$key:57$
    /// </summary>
    Roulette = 16,
    /// <summary>
    /// <c>s_quest_talk_end</c> Close\n$key:57$
    /// <c>s_roulette_talk_skip</c> Skip\n$key:57$
    /// </summary>
    RouletteSkip = 17,
    /// <summary>
    /// <c>s_resolve_panelty_accept</c> Get Treatment\n$key:57$|Decline\n$key:1$
    /// </summary>
    HomeDoctor = 18,
    /// <summary>
    /// s_quest_talk_progress OR s_quest_talk_end
    /// </summary>
    CustomSelectableDistractor = 19,
}
