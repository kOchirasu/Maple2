using System.Diagnostics;

namespace Maple2.File.Ingest.Utils;

internal class AiDefinitionOverride {
    public static readonly Dictionary<string, Dictionary<string, (ScriptType, string?)>> TypeOverride = new();
    public static readonly Dictionary<string, Dictionary<string, string>> NameOverride = new();

    static AiDefinitionOverride() {
        // Nodes
        // AddNameOverride("announce");
        // AddTypeOverride("announce");
        // AddNameOverride("buff");
        AddTypeOverride("buff", ("prob", ScriptType.Int, "100"), ("type", ScriptType.Str, "add"));
        // AddNameOverride("conditions");
        // AddTypeOverride("conditions");
        // AddNameOverride("create_interact_object");
        // AddTypeOverride("create_interact_object");
        // AddNameOverride("create_random_room");
        // AddTypeOverride("create_random_room");
        // AddNameOverride("hide_vibrate_all");
        // AddTypeOverride("hide_vibrate_all");
        // AddNameOverride("jump");
        AddTypeOverride("jump", ("type", ScriptType.Str, "jumpA"));
        // AddNameOverride("minimum_hp");
        // AddTypeOverride("minimum_hp");
        // AddNameOverride("modify_room_time");
        // AddTypeOverride("modify_room_time");
        // AddNameOverride("move");
        AddTypeOverride("move", ("prob", ScriptType.Int, "100"));
        // AddNameOverride("remove");
        // AddTypeOverride("remove");
        // AddNameOverride("remove_slaves");
        // AddTypeOverride("remove_slaves");
        // AddNameOverride("ride");
        // AddTypeOverride("ride");
        // AddNameOverride("runaway");
        // AddTypeOverride("runaway");
        // AddNameOverride("say");
        AddTypeOverride("say", ("prob", ScriptType.Int, "100"));
        // AddNameOverride("select");
        AddTypeOverride("select", ("prob", ScriptType.IntList, "[100]"));
        // AddNameOverride("set_data");
        // AddTypeOverride("set_data");
        // AddNameOverride("set_master_value");
        // AddTypeOverride("set_master_value");
        // AddNameOverride("set_slave_value");
        // AddTypeOverride("set_slave_value");
        // AddNameOverride("set_value");
        // AddTypeOverride("set_value");
        // AddNameOverride("set_value_range_target");
        // AddTypeOverride("set_value_range_target");
        // AddNameOverride("show_vibrate");
        // AddTypeOverride("show_vibrate");
        // AddNameOverride("side_popup");
        AddTypeOverride("side_popup", ("type", ScriptType.Str, "talk"));
        // AddNameOverride("skill");
        AddTypeOverride("skill", ("prob", ScriptType.Int, "100"));
        // AddNameOverride("standby");
        AddTypeOverride("standby", ("prob", ScriptType.Int, "100"));
        // AddNameOverride("suicide");
        // AddTypeOverride("suicide");
        // AddNameOverride("summon");
        // AddTypeOverride("summon");
        AddNameOverride("target", ("from", "from_pos"), ("to", "to_pos"));
        AddTypeOverride("target", ("prob", ScriptType.Int, "100"), ("type", ScriptType.Str, "rand"), ("target", ScriptType.Str, "defaultTarget"));
        // AddNameOverride("target_effect");
        // AddTypeOverride("target_effect");
        // AddNameOverride("teleport");
        AddTypeOverride("teleport", ("prob", ScriptType.Int, "100"));
        // AddNameOverride("trace");
        // AddTypeOverride("trace");
        // AddNameOverride("trigger_modify_user_value");
        // AddTypeOverride("trigger_modify_user_value");
        // AddNameOverride("trigger_set_user_value");
        // AddTypeOverride("trigger_set_user_value");

        // Conditions
        // AddNameOverride("additional");
        // AddTypeOverride("additional");
        // AddNameOverride("combat_time");
        // AddTypeOverride("combat_time");
        // AddNameOverride("distance");
        // AddTypeOverride("distance");
        // AddNameOverride("extra_data");
        // AddTypeOverride("extra_data");
        // AddNameOverride("feature");
        // AddTypeOverride("feature");
        // AddNameOverride("hp");
        // AddTypeOverride("hp");
        // AddNameOverride("skill_range");
        // AddTypeOverride("skill_range");
        // AddNameOverride("slave_count");
        // AddTypeOverride("slave_count");
        // AddNameOverride("state");
        // AddTypeOverride("state");
        // AddNameOverride("true");
        // AddTypeOverride("true");
    }

    private static void AddTypeOverride(string name, params (string, ScriptType, string?)[] overrides) {
        if (!TypeOverride.TryGetValue(name, out Dictionary<string, (ScriptType, string?)>? mapping)) {
            mapping = new Dictionary<string, (ScriptType, string?)>();
            TypeOverride[name] = mapping;
        }

        foreach ((string argName, ScriptType argType, string? defaultValue) in overrides) {
            Debug.Assert(!mapping.ContainsKey(argName), $"Duplicate override key: {argName} for {name}");
            mapping.Add(argName, (argType, defaultValue));
        }
    }

    private static void AddNameOverride(string name, params (string, string)[] overrides) {
        if (!NameOverride.TryGetValue(name, out Dictionary<string, string>? mapping)) {
            mapping = new Dictionary<string, string>();
            NameOverride[name] = mapping;
        }

        foreach ((string oldName, string newName) in overrides) {
            Debug.Assert(!mapping.ContainsKey(oldName), $"Duplicate override key: {oldName} for {name}");
            mapping.Add(oldName, newName);
        }
    }
}
