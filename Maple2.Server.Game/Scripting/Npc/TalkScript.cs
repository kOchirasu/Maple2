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

public interface ITalkScript {
    protected dynamic? script { get; }
    protected GameSession session { get; }
    protected ScriptMetadata Metadata { get; set; }

    public NpcTalkType TalkType { get; set; }
    public int State { get; set; }
    public int Index { get; set; }
    
    public bool Begin();
    public bool Continue(int pick);
}
