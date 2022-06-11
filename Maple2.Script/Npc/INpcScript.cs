using System.Runtime.CompilerServices;
using Maple2.Model.Enum;

namespace Maple2.Script.Npc;

public abstract class NpcScript {
    private readonly INpcScriptContext context;

    protected int Id;

    protected NpcScript(INpcScriptContext context) {
        this.context = context;
    }

    /// <summary>
    /// Handles the next talk interaction with the Npc.
    /// </summary>
    /// <param name="selection">The index of the option being selected.</param>
    /// <returns>true means the NpcTalk has finished</returns>
    public abstract bool Next(int selection = 0);

    #region API Wrapper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close() => context.Close();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Respond(NpcTalkType type, NpcTalkComponent component) => context.Respond(Id, type, component);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MovePlayer(int portalId) => context.MovePlayer(portalId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OpenDialog(string name, string tags) => context.OpenDialog(name, tags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RewardItem(params (int Id, byte Rarity, int Amount)[] items) => context.RewardItem(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RewardExp(long exp) => context.RewardExp(exp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RewardMeso(long mesos) => context.RewardMeso(mesos);

    #endregion
}
