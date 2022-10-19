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

    private static void WriteBlankLine(IndentedTextWriter writer) {
        int indent = writer.Indent;
        writer.Indent = 0;
        writer.WriteLine();
        writer.Indent = indent;
    }
}
