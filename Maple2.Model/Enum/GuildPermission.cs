using System;

namespace Maple2.Model.Enum;

[Flags]
public enum GuildPermission {
    Default = 1,
    InviteMembers = 2,
    EditNotice = 8,
    EditEmblem = 64,
    SendMail = 128,
    StartMiniGame = 1024,
    SendAlert = 2048,

    All = Default | InviteMembers | EditNotice | EditEmblem | SendMail | StartMiniGame | SendAlert,
}
