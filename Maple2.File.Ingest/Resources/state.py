# Base class for all States
class State:
    def __init__(self):
        pass

    def on_enter(self):
        """Invoked after transitioning to this state."""
        pass

    def on_tick(self) -> 'State':
        """Periodically invoked while in this state."""
        pass

    def on_exit(self):
        """Invoked before transitioning to another state."""
        pass


# Used by dungeon_common
class DungeonStart(State):
    pass
