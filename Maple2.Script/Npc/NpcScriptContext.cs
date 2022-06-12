using Maple2.Model.Enum;

namespace Maple2.Script.Npc;

public interface INpcScriptContext {
    public void Respond(NpcTalkType type, int id, NpcTalkButton button);
    public void Continue(NpcTalkType type, int id, int index, NpcTalkButton button, int questId = 0);

    public bool MovePlayer(int portalId);
    public void OpenDialog(string name, string tags);
    public void RewardItem(params (int Id, byte Rarity, int Amount)[] items);
    public void RewardExp(long exp);
    public void RewardMeso(long mesos);
}
