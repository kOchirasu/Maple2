using System.CodeDom.Compiler;

namespace Maple2.File.Ingest.Utils;

internal class TriggerScript {
    public readonly IList<State> States;

    public TriggerScript() {
        States = new List<State>();
    }

    public class State {
        public string Name = string.Empty;
        public IList<Action> OnEnter = new List<Action>();
        public IList<Condition> Conditions = new List<Condition>();
        public IList<Action> OnExit = new List<Action>();

        public void WriteTo(IndentedTextWriter writer) {
            if (Name == "DungeonStart") {
                writer.WriteLine($"class {Name}(state.DungeonStart):");
            } else {
                writer.WriteLine($"class {Name}(state.State):");
            }

            bool hasBody = false;
            writer.Indent++;
            if (OnEnter.Count > 0) {
                writer.WriteLine("def on_enter(self):");
                writer.Indent++;
                foreach (Action action in OnEnter) {
                    action.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
                hasBody = true;
            }
            if (Conditions.Count > 0) {
                writer.WriteLine("def on_tick(self) -> state.State:");
                writer.Indent++;
                foreach (Condition condition in Conditions) {
                    condition.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
                hasBody = true;
            }
            if (OnExit.Count > 0) {
                writer.WriteLine("def on_exit(self):");
                writer.Indent++;
                foreach (Action action in OnExit) {
                    action.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
                hasBody = true;
            }

            if (!hasBody) {
                writer.WriteLine("pass");
                WriteBlankLine(writer);
            }
            writer.Indent--;
            if (Name == "DungeonStart") {
                writer.WriteLine("state.DungeonStart = DungeonStart");
                WriteBlankLine(writer);
            }
            WriteBlankLine(writer);
        }
    }

    public class Action {
        public string Name = string.Empty;
        public IList<Parameter> Args = new List<Parameter>();

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"{Name}({string.Join(", ", Args.Select(arg => $"{arg.Name}={arg.FormatValue()}"))})");
        }
    }

    public class Condition {
        public string Name = string.Empty;
        public bool Negated;
        public IList<Parameter> Args = new List<Parameter>();
        public IList<Action> Actions = new List<Action>();
        public string? Transition;
        public string? TransitionComment;

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"if {(Negated ? "not " : "")}{Name}({string.Join(", ", Args.Select(arg => $"{arg.Name}={arg.FormatValue()}"))}):");
            writer.Indent++;
            foreach (Action action in Actions) {
                action.WriteTo(writer);
            }

            if (Transition == null) {
                if (TransitionComment != null) {
                    writer.WriteLine($"return None # Missing {TransitionComment}");
                } else {
                    writer.WriteLine("return None");
                }
            } else {
                writer.WriteLine($"return {Transition}()");
            }
            writer.Indent--;
        }
    }

    public void WriteTo(IndentedTextWriter writer) {
        foreach (State state in States) {
            state.WriteTo(writer);
        }
    }

    public static void WriteBlankLine(IndentedTextWriter writer) {
        int indent = writer.Indent;
        writer.Indent = 0;
        writer.WriteLine();
        writer.Indent = indent;
    }
}

internal class TriggerScriptCommon {
    public readonly SortedDictionary<string, Function> Actions = new();
    public readonly SortedDictionary<string, Function> Conditions = new();

    public void WriteTo(IndentedTextWriter writer) {
        writer.WriteLine("import state");
        writer.WriteLine("from typing import List");
        writer.WriteLine();

        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine(@""""""" Actions """"""");
        foreach (Function action in Actions.Values) {
            action.WriteTo(writer);
            TriggerScript.WriteBlankLine(writer);
        }

        TriggerScript.WriteBlankLine(writer);
        writer.WriteLine(@""""""" Conditions """"""");
        foreach (Function condition in Conditions.Values) {
            condition.WriteTo(writer);
            TriggerScript.WriteBlankLine(writer);
        }
    }

    internal class Function : IComparable<Function> {
        public string? Comment = null;
        public readonly string Name;
        public ScriptType ReturnType { get; init; }
        private readonly List<Parameter> parameters = new();

        private readonly Dictionary<string, (ScriptType, string?)> typeOverrides;
        private readonly Dictionary<string, string> nameOverrides;

        public Function(string name, bool isCondition) {
            Name = name;
            if (isCondition) {
                nameOverrides = TriggerDefinitionOverride.ConditionNameOverride.GetValueOrDefault(name, new Dictionary<string, string>());
                typeOverrides = TriggerDefinitionOverride.ConditionTypeOverride.GetValueOrDefault(name, new Dictionary<string, (ScriptType, string?)>());
            } else {
                nameOverrides = TriggerDefinitionOverride.ActionNameOverride.GetValueOrDefault(name, new Dictionary<string, string>());
                typeOverrides = TriggerDefinitionOverride.ActionTypeOverride.GetValueOrDefault(name, new Dictionary<string, (ScriptType, string?)>());
            }
        }

        public int CompareTo(Function? other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            int nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;

            return ReturnType.CompareTo(other.ReturnType);
        }

        // Returns normalized parameter name
        public (ScriptType, string) AddParameter(ScriptType type, string name, string? defaultValue = null) {
            if (nameOverrides.ContainsKey(name)) {
                name = nameOverrides[name];
            }

            Parameter? existing = parameters.FirstOrDefault(param => param.Name == name);
            if (existing != null) {
                return (existing.Type, existing.Name);
            }

            if (typeOverrides.ContainsKey(name)) {
                (ScriptType, string?) typeOverride = typeOverrides[name];
                type = typeOverride.Item1;
                defaultValue = typeOverride.Item2;
            }

            parameters.Add(new Parameter(type, name, defaultValue));
            return (type, name);
        }

        public void WriteTo(IndentedTextWriter writer) {
            if (Comment != null) {
                writer.WriteLine($"# {Comment}");
            }
            writer.Write($"def {Name}(");
            bool firstLoop = true;
            foreach (Parameter parameter in parameters) {
                if (parameter.Type == ScriptType.None) {
                    continue;
                }

                if (!firstLoop) {
                    writer.Write(", ");
                }
                writer.Write($"{parameter.Name}: ");
                switch (parameter.Type) {
                    case ScriptType.Str:
                        writer.Write("str");
                        break;
                    case ScriptType.Int:
                        writer.Write("int");
                        break;
                    case ScriptType.Float:
                        writer.Write("float");
                        break;
                    case ScriptType.IntList:
                        writer.Write("List[int]");
                        break;
                    case ScriptType.StrList:
                        writer.Write("List[str]");
                        break;
                    case ScriptType.Vector3:
                        writer.Write("List[float]");
                        break;
                    case ScriptType.Bool:
                        writer.Write("bool");
                        break;
                    case ScriptType.State:
                        writer.Write("state.State");
                        break;
                    default:
                        throw new ArgumentException($"Invalid parameter type: {parameter.Type}");
                }

                string defaultValue = parameter.Value ?? parameter.Type switch {
                    ScriptType.Str => "None",
                    ScriptType.Int => "0",
                    ScriptType.Float => "0.0",
                    ScriptType.IntList => "[]",
                    ScriptType.StrList => "[]",
                    ScriptType.Vector3 => "[0,0,0]",
                    ScriptType.Bool => "False",
                    ScriptType.State => "None",
                    _ => throw new ArgumentException($"Invalid parameter type: {parameter.Type}"),
                };
                writer.Write($"={defaultValue}");

                firstLoop = false;
            }

            string returnTypeStr = ReturnType switch {
                ScriptType.None => "",
                ScriptType.Str => " -> str",
                ScriptType.Int => " -> int",
                ScriptType.Float => " -> float",
                ScriptType.Bool => " -> bool",
                _ => throw new ArgumentException($"Invalid return type: {ReturnType}")
            };
            writer.WriteLine($"){returnTypeStr}:");
            writer.Indent++;
            if (ReturnType == ScriptType.Bool) {
                writer.WriteLine("return False");
            } else {
                writer.WriteLine("pass");
            }
            writer.Indent--;
        }
    }
}
