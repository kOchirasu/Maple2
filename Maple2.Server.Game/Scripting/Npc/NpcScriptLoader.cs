using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using IronPython.Hosting;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Microsoft.Scripting.Hosting;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScriptLoader {
    private readonly ScriptEngine engine;
    private readonly ConcurrentDictionary<int, ScriptSource> scriptSources;

    public NpcScriptLoader() {
        engine = Python.CreateEngine();
        ICollection<string> paths = engine.GetSearchPaths();
        foreach (string dir in Directory.GetDirectories("Scripts/")) {
            paths.Add(dir);
        }
        engine.SetSearchPaths(paths);

        scriptSources = new ConcurrentDictionary<int, ScriptSource>();
    }

    public NpcScript? Get(int npcId, NpcScriptContext context) {
        if (!scriptSources.TryGetValue(npcId, out ScriptSource? script)) {
            script = engine.CreateScriptSourceFromFile($"Scripts/Npc/{npcId}.py");
            scriptSources[npcId] = script;
        }

        ScriptScope scope = engine.CreateScope();
        scope.SetVariable("ctx", context);
        script.Execute(scope);

        dynamic? type = scope.GetVariable("Main");
        if (type == null) {
            return null;
        }

        return new NpcScript(context, engine.Operations.CreateInstance(type));
    }
}
