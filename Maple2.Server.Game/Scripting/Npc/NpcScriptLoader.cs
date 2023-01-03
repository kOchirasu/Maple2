using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScriptLoader {
    private readonly ScriptEngine engine;
    private readonly ConcurrentDictionary<int, ScriptSource> scriptSources;

    public NpcScriptLoader() {
        engine = Python.CreateEngine();
        ICollection<string> paths = engine.GetSearchPaths();
        paths.Add(@"C:\Users\adria\OneDrive\Documents\MapleStory2\IronPython");
        foreach (string dir in Directory.GetDirectories("Scripts/")) {
            paths.Add(dir);
        }
        engine.SetSearchPaths(paths);

        scriptSources = new ConcurrentDictionary<int, ScriptSource>();
    }

    public NpcScript? Get(int npcId, NpcScriptContext context) {
        Console.WriteLine($"Load script for: {npcId}");
        if (!scriptSources.TryGetValue(npcId, out ScriptSource? script)) {
            script = engine.CreateScriptSourceFromFile($"Scripts/Npc/{npcId}.py");
            scriptSources[npcId] = script;
        }

        ScriptScope scope = engine.CreateScope();
        script.Execute(scope);

        dynamic? type = scope.GetVariable("Main");
        if (type == null) {
            return null;
        }

        return new NpcScript(context, engine.Operations.CreateInstance(type, context));
    }
}
