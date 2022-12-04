from enum import Enum
import re

_state_pattern = re.compile(r"_.+__(\d+)")


class Script:
    def __init__(self):
        self.index = 0
        self.state = self.first()
        self.button = Option.CLOSE
        self.states = {}

        for d in dir(self):
            result = _state_pattern.search(d)
            if result:
                id = int(result.group(1))
                self.states[id] = getattr(self, d)

    def first(self) -> int:
        """Returns the first script id for this Npc."""
        raise NotImplementedError()

    def select(self) -> int:
        """Returns the select script id for this Npc."""
        raise NotImplementedError()

    def execute(self, pick: int):
        """Executes the current state and performs necessary transitions."""
        # Execute current State
        if self.state in self.states:
            result = self.states[self.state](pick)
        else:
            result = -1

        # Update State/Index
        if result is None:
            raise NotImplementedError()
        elif result == self.state:
            self.index += 1
        elif result == -1:
            pass
        else:
            self.state = result
            self.index = 0

    def button(self) -> 'Option':
        return Option.NONE


class Option(Enum):
    # No options
    EMPTY = 1
    # <c>s_itemenchant_cinematic_btn</c> Quit:$key:1$
    STOP = 2
    # <c>s_quest_talk_end</c> Close\n$key:57$
    CLOSE = 3
    # <c>s_quest_talk_progress</c> Next\n$key:57$|Close\n$key:1$
    NEXT = 4
    # Used for Select script
    SELECTABLE_TALK = 5
    # <c>s_quest_talk_accept</c> Accept\n$key:57$|Decline\n$key:1$
    QUEST_ACCEPT = 6
    # <c>s_quest_talk_complete</c> Complete\n$key:57$|Close\n$key:1$
    QUEST_COMPLETE = 7
    # <c>s_quest_talk_end</c> Close\n$key:57$
    QUEST_PROGRESS = 8
    # s_quest_talk_progress OR s_quest_talk_end
    SELECTABLE_DISTRACTOR = 9
    # s_quest_talk_progress OR s_quest_talk_end
    SELECTABLE_BEAUTY = 10
    # <c>s_changejob_accept</c> Perform Job Advancement\n($key:57$)|Nevermind\n($key:1$)
    CHANGE_JOB = 11
    # <c>s_quest_talk_accept</c> Accept\n$key:57$|Decline\n$key:1$
    UGC_SIGN = 12
    # <c>s_resolve_panelty_accept</c> Get Treatment\n$key:57$|Decline\n$key:1$
    PENALTY_RESOLVE = 13
    # <c>s_take_boat_accept</c> Go\n$key:57$|Stay\n$key:1$
    TAKE_BOAT = 14
    # <c>s_sell_ugc_map_accept</c> Confirm\n$key:57$|Cancel\n$key:1$
    SELL_UGC_MAP = 15
    # <c>s_roulette_accept</c> Spin\n$key:57$
    ROULETTE = 16
    # <c>s_roulette_talk_skip</c> Skip\n$key:57$
    ROULETTE_SKIP = 17
    # <c>s_resolve_panelty_accept</c> Get Treatment\n$key:57$|Decline\n$key:1$
    HOME_DOCTOR = 18
    # s_quest_talk_progress OR s_quest_talk_end
    CUSTOM_SELECTABLE_DISTRACTOR = 19
