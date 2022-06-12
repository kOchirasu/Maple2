using System.Runtime.CompilerServices;
using Maple2.Model.Enum;
using Maple2.Model.Game;

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

    public void Init(INpcScriptContext scriptContext) {
        context = scriptContext;
        Id = First();
    }

    public void Respond() => context.Respond(Type, Id, Button());
    public void Continue() => context.Continue(Type, Id, Index, Button());

    /// <summary>
    /// Advances the script to the next Id
    /// </summary>
    /// <param name="selection"></param>
    public void Advance(int selection = 0) {
        Id = Execute(selection);
    }

    #region Script Methods
    /// <summary>
    /// Determines the Id of the script to begin at.
    /// </summary>
    /// <returns>The Id of the first script</returns>
    protected abstract int First();

    /// <summary>
    /// Get the <b>SELECT</b> script Id to use for prompting the user with (Quests/Shops).
    /// </summary>
    /// <returns>The Id of the select script</returns>
    protected abstract int Select();

    /// <summary>
    /// Executes any actions needed for the talk interaction with the Npc.
    /// </summary>
    /// <param name="selection">The index of the option being selected.</param>
    /// <returns>The Id of the next script to be executed</returns>
    protected abstract int Execute(int selection);

    /// <summary>
    /// Get the displayed buttons.
    /// </summary>
    /// <returns>The button to display for the current (Id, Index) pair.</returns>
    protected abstract NpcTalkButton Button();
    #endregion

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

    protected T Random<T>(params T[] options) => options[Environment.TickCount % options.Length];
    #endregion
}
