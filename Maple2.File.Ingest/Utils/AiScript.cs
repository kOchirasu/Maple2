using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using Maple2.File.Parser.Xml.AI;

namespace Maple2.File.Ingest.Utils;

internal class AiScript {

}

internal class AiScriptCommon {
    public readonly SortedDictionary<string, PythonFunction> Nodes = new();
    public readonly SortedDictionary<string, PythonFunction> Conditions = new();

    public AiScriptCommon() {
        var assembly = Assembly.GetAssembly(typeof(Entry));
        Debug.Assert(assembly != null, $"Failed to find assembly for {typeof(Entry)}");

        var nodeTypes = new SortedDictionary<string, Type>();
        foreach (Type type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(NodeEntry)))) {
            string name = TriggerTranslate.ToSnakeCase(type.Name.Replace("Node", ""));
            nodeTypes.Add(name, type);
        }
        foreach ((string name, Type type) in nodeTypes) {
            Nodes.Add(name, CreateFunction(name, type));
        }

        Nodes["select"].SetBody("return random.choices(sequence=range(len(prob)), weights=prob)");

        var conditionTypes = new SortedDictionary<string, Type>();
        foreach (Type type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ConditionEntry)))) {
            string name = TriggerTranslate.ToSnakeCase(type.Name.Replace("Condition", ""));
            conditionTypes.Add(name, type);
        }
        foreach ((string name, Type type) in conditionTypes) {
            // Custom functions generated here
            switch (name) {
                case "distance_less" or "distance_over":
                    var distanceFunction = new PythonFunction("distance", AiDefinitionOverride.NameOverride, AiDefinitionOverride.TypeOverride) {
                        Description = "",
                        ReturnType = ScriptType.Int,
                    };
                    Conditions.TryAdd("distance", distanceFunction);
                    break;
                case "hp_less" or "hp_over":
                    var hpFunction = new PythonFunction("hp", AiDefinitionOverride.NameOverride, AiDefinitionOverride.TypeOverride) {
                        Description = "",
                        ReturnType = ScriptType.Int,
                    };
                    Conditions.TryAdd("hp", hpFunction);
                    break;
                case "extra_data":
                    var extraDataFunction = new PythonFunction(name, AiDefinitionOverride.NameOverride, AiDefinitionOverride.TypeOverride) {
                        Description = "",
                        ReturnType = ScriptType.Int,
                    };
                    extraDataFunction.AddParameter(ScriptType.Str, "key");
                    extraDataFunction.AddParameter(ScriptType.Bool, "is_keep_battle");
                    Conditions.Add(name, extraDataFunction);
                    break;
                case "slave_count":
                    var slaveCount = new PythonFunction(name, AiDefinitionOverride.NameOverride, AiDefinitionOverride.TypeOverride) {
                        Description = "",
                        ReturnType = ScriptType.Int,
                    };
                    slaveCount.AddParameter(ScriptType.Bool, "use_summon_group");
                    slaveCount.AddParameter(ScriptType.Int, "summon_group");
                    Conditions.Add(name, slaveCount);
                    break;
                case "feature" or "true":
                    break;
                default:
                    Conditions.Add(name, CreateFunction(name, type));
                    break;
            }
        }
    }

    public void WriteTo(IndentedTextWriter writer) {
        writer.WriteLine("import random");
        writer.WriteLine("from typing import List");
        writer.WriteLine();

        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine("class Ai:");
        writer.Indent++;
        writer.WriteLine("def __init__(self, ctx: ...):");
        writer.Indent++;
        writer.WriteLine("self.ctx = ctx");
        writer.Indent--;
        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine("def battle(self):");
        writer.Indent++;
        writer.WriteLine(@"""""""Npc Battle Ai.""""""");
        writer.WriteLine("pass");
        writer.Indent--;
        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine("def battle_end(self):");
        writer.Indent++;
        writer.WriteLine(@"""""""Npc BattleEnd Ai.""""""");
        writer.WriteLine("pass");
        writer.Indent--;

        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine(@""""""" Nodes """"""");
        foreach (PythonFunction node in Nodes.Values) {
            node.WriteTo(writer);
            TriggerScript.WriteBlankLine(writer);
        }

        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine(@""""""" Conditions """"""");
        foreach (PythonFunction condition in Conditions.Values) {
            condition.WriteTo(writer);
            TriggerScript.WriteBlankLine(writer);
        }

        writer.Indent--;
    }

    private static PythonFunction CreateFunction(string name, Type type) {
        var function = new PythonFunction(name, AiDefinitionOverride.NameOverride, AiDefinitionOverride.TypeOverride) {
            Description = "",
            ReturnType = ScriptType.Bool,
        };

        foreach (FieldInfo field in type.GetFields()) {
            string fieldName = TriggerTranslate.ToSnakeCase(field.Name);
            if (fieldName is "name" or "entries") {
                continue;
            }

            function.AddParameter(GetScriptType(field), fieldName);
        }

        return function;
    }

    private static ScriptType GetScriptType(FieldInfo info) {
        if (info.FieldType.IsEnum) {
            return ScriptType.Str;
        }

        return info.FieldType.Name switch {
            "Boolean" => ScriptType.Bool,
            "Int16" or "Int32" or "Int64" => ScriptType.Int,
            "Int16[]" or "Int32[]" or "Int64[]" => ScriptType.IntList,
            "Single" or "Double" => ScriptType.Float,
            "String" => ScriptType.Str,
            "String[]" => ScriptType.StrList,
            "Vector3" => ScriptType.Vector3,
            _ => ScriptType.None,
        };
    }
}
