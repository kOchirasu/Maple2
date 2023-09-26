namespace Maple2.Model.Enum;

public enum PortalType : byte {
    Field = 0,
    DungeonReturnToLobby = 1,
    Event = 3, // Pocket Realms & Player Hosted Mini games
    Boss = 8,
    DungeonEnter = 9,
    Home = 11,
    LeaveDungeon = 13,
}

public enum PortalActionType {
    Interact = 0,
    Touch = 1,
}
