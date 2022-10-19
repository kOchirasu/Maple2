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
            writer.WriteLine($"class {Name}(State):");
            writer.Indent++;
            if (OnEnter.Count > 0) {
                writer.WriteLine("def on_enter():");
                writer.Indent++;
                foreach (Action action in OnEnter) {
                    action.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
            }
            if (Conditions.Count > 0) {
                writer.WriteLine("def on_tick():");
                writer.Indent++;
                foreach (Condition condition in Conditions) {
                    condition.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
            }
            if (OnExit.Count > 0) {
                writer.WriteLine("def on_exit():");
                writer.Indent++;
                foreach (Action action in OnExit) {
                    action.WriteTo(writer);
                }
                writer.Indent--;
                WriteBlankLine(writer);
            }
            writer.Indent--;
            WriteBlankLine(writer);
        }
    }

    public class Action {
        public string Name = string.Empty;
        public IDictionary<string, string> Args = new Dictionary<string, string>();

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"{Name}({string.Join(", ", Args.Select(arg => $"{arg.Key}={arg.Value}"))})");
        }
    }

    public class Condition {
        public string Name = string.Empty;
        public bool Negated;
        public IDictionary<string, string> Args = new Dictionary<string, string>();
        public IList<Action> Actions = new List<Action>();
        public string? Transition;

        public void WriteTo(IndentedTextWriter writer) {
            writer.WriteLine($"if {(Negated ? "not " : "")}{Name}({string.Join(", ", Args.Select(arg => $"{arg.Key}={arg.Value}"))}):");
            writer.Indent++;
            foreach (Action action in Actions) {
                action.WriteTo(writer);
            }
            writer.WriteLine($"return '{Transition}'");
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
