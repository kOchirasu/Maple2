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
        public IDictionary<string, string> Args = new Dictionary<string, string>();

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"{Name}({string.Join(", ", Args.Select(arg => $"{arg.Key}='{arg.Value}'"))})");
        }
    }

    public class Condition {
        public string Name = string.Empty;
        public bool Negated;
        public IDictionary<string, string> Args = new Dictionary<string, string>();
        public IList<Action> Actions = new List<Action>();
        public string? Transition;
        public string? TransitionComment;

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"if {(Negated ? "not " : "")}{Name}({string.Join(", ", Args.Select(arg => $"{arg.Key}='{arg.Value}'"))}):");
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

    internal enum Type { None = 0, Str, Int, Float, IntList, Bool }
    internal record Parameter(Type Type, string Name);

    internal class Function : IComparable<Function> {
        public string Name { get; init; }
        public Type ReturnType { get; init; }
        private readonly List<Parameter> parameters = new();

        public int CompareTo(Function? other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            int nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;

            return ReturnType.CompareTo(other.ReturnType);
        }

        public bool AddParameter(Parameter parameter) {
            if (parameters.Contains(parameter)) {
                return false;
            }

            parameters.Add(parameter);
            return true;
        }

        public void WriteTo(IndentedTextWriter writer) {
            writer.Write($"def {Name}(");
            bool firstLoop = true;
            foreach (Parameter parameter in parameters) {
                if (!firstLoop) {
                    writer.Write(", ");
                }
                writer.Write($"{parameter.Name}: ");
                switch (parameter.Type) {
                    case Type.Str:
                        writer.Write("str");
                        break;
                    case Type.Int:
                        writer.Write("int");
                        break;
                    case Type.Float:
                        writer.Write("float");
                        break;
                    case Type.IntList:
                        writer.Write("List[int]");
                        break;
                    case Type.Bool:
                        writer.Write("bool");
                        break;
                    default:
                        throw new ArgumentException($"Invalid parameter type: {parameter.Type}");
                }

                firstLoop = false;
            }

            string returnTypeStr = ReturnType switch {
                Type.None => "",
                Type.Str => " -> str",
                Type.Int => " -> int",
                Type.Float => " -> float",
                Type.Bool => " -> bool",
                _ => throw new ArgumentException($"Invalid return type: {ReturnType}")
            };
            writer.WriteLine($"){returnTypeStr}:");
            writer.Indent++;
            writer.WriteLine("pass");
            writer.Indent--;
        }
    }
}
