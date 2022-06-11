using System;
using System.Collections.Generic;
using System.Reflection;
using Maple2.Script.Npc;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScriptLoader {
    private readonly Dictionary<int, Func<INpcScriptContext, NpcScript>> scriptLookup;

    public NpcScriptLoader() {
        scriptLookup = new Dictionary<int, Func<INpcScriptContext, NpcScript>>();

        var assembly = Assembly.GetAssembly(typeof(INpcScriptContext));
        if (assembly == null) {
            throw new InvalidOperationException("Failed to load assembly for NpcScripts");
        }

        foreach (Type type in assembly.GetExportedTypes()) {
            ConstructorInfo? constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
                Array.Empty<Type>(), null);

            // No matching constructor for dungeon_common
            if (constructor == null) {
                continue;
            }

            if (!int.TryParse(type.Name[1..], out int npcId)) {
                continue;
            }

            scriptLookup[npcId] = context => {
                var script = (NpcScript) constructor.Invoke(Array.Empty<object?>());
                script.Init(context);
                return script;
            };
        }
    }

    public NpcScript? Get(int npcId, INpcScriptContext context) {
        return scriptLookup.GetValueOrDefault(npcId)?.Invoke(context);
    }
}
