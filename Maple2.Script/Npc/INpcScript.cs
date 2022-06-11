using System.Runtime.CompilerServices;
using Maple2.Model.Enum;

namespace Maple2.Script.Npc;

public abstract class NpcScript {
    private INpcScriptContext context = null!;
    private int id;

    protected int Id {
        get => id;
        private set {
            if (id == value) {
                return;
            }

            id = value;
            Index = 0;
        }
    }

    protected int Index;
    protected NpcTalkType Type = NpcTalkType.Chat;
    protected NpcTalkButton Button = NpcTalkButton.Close;

    public void Init(INpcScriptContext scriptContext) {
        context = scriptContext;
        (Id, Button) = FirstScript();
    }

    public void Close() => context.Close();

    public void Talk() => context.Respond(Type, id, Button);

    public void Select(int questId = 0) {
        if (Button == NpcTalkButton.None) {
            context.Close();
        } else {
            context.Continue(Type, id, Index, Button, questId);
        }
    }

    public void Advance(int selection = 0) {
        (Id, Button) = Next(selection);
    }

    protected abstract (int, NpcTalkButton) FirstScript();

    /// <summary>
    /// Handles the next talk interaction with the Npc.
    /// </summary>
    /// <param name="selection">The index of the option being selected.</param>
    /// <returns>true means the NpcTalk has finished</returns>
    protected abstract (int, NpcTalkButton) Next(int selection);

    #region API Wrapper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool MovePlayer(int portalId) => context.MovePlayer(portalId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OpenDialog(string name, string tags) => context.OpenDialog(name, tags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RewardItem(params (int Id, byte Rarity, int Amount)[] items) => context.RewardItem(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RewardExp(long exp) => context.RewardExp(exp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RewardMeso(long mesos) => context.RewardMeso(mesos);

    #endregion
}
