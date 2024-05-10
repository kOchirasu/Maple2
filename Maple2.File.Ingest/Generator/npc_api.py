import re

_state_pattern = re.compile(r"_.+__(\d+)")


class Script:
    def __init__(self, ctx ...):
        self.ctx = ctx
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

    def execute(self, id: int, index: int, pick: int):
        if id in self.states:
            return self.states[id](index, pick)

        # Invalid id
        return -1
