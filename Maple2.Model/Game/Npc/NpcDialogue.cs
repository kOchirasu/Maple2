using System.Runtime.InteropServices;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public readonly struct NpcDialogue {
    public readonly int Id; // ScriptId
    public readonly int Index; // Selection
    public readonly NpcTalkComponent Component;

    public NpcDialogue(int id, int index, NpcTalkComponent component) {
        Id = id;
        Index = index;
        Component = component;
    }
}
