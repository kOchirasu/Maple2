class State:
    def __init__(self):
        pass

    def on_enter(self):
        pass

    def on_tick(self) -> 'State':
        pass

    def on_exit(self):
        pass


class DungeonStart(State):
    pass
