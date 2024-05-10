using System.CodeDom.Compiler;

namespace Maple2.File.Ingest.Utils;

internal class PythonFunction : IComparable<PythonFunction> {
    public string? Description = null;
    public readonly string Name;
    public ScriptType ReturnType { get; init; }
    private readonly List<Parameter> parameters = new();

    private readonly Dictionary<string, (ScriptType, string?)> typeOverrides;
    private readonly Dictionary<string, string> nameOverrides;
    private string bodyOverride = string.Empty;

    public PythonFunction(string name,
                          IReadOnlyDictionary<string, Dictionary<string, string>> nameOverrides,
                          IReadOnlyDictionary<string, Dictionary<string, (ScriptType, string?)>> typeOverrides) {
        Name = name;

        this.nameOverrides = nameOverrides.GetValueOrDefault(name, new Dictionary<string, string>());
        this.typeOverrides = typeOverrides.GetValueOrDefault(name, new Dictionary<string, (ScriptType, string?)>());
    }

    public int CompareTo(PythonFunction? other) {
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
            defaultValue = typeOverride.Item1 == ScriptType.Str ? $"'{typeOverride.Item2}'" : typeOverride.Item2;
        }

        parameters.Add(new Parameter(type, name, defaultValue));
        return (type, name);
    }

    public void SetBody(string body) {
        bodyOverride = body;
    }

    public void WriteTo(IndentedTextWriter writer) {
        writer.Write($"def {Name}(self");
        foreach (Parameter parameter in parameters) {
            if (parameter.Type == ScriptType.None) {
                continue;
            }

            writer.Write(", ");
            writer.Write($"{parameter.Name}: ");
            writer.Write(parameter.TypeStr());
            writer.Write($"={parameter.DefaultStr()}");
        }

        writer.Write(")");
        string returnTypeStr = ReturnType switch {
            ScriptType.None => "",
            ScriptType.Str => "str",
            ScriptType.Int => "int",
            ScriptType.Float => "float",
            ScriptType.Bool => "bool",
            _ => throw new ArgumentException($"Invalid return type: {ReturnType}")
        };
        if (!string.IsNullOrWhiteSpace(returnTypeStr)) {
            writer.Write($" -> {returnTypeStr}");
        }
        writer.WriteLine(":");
        writer.Indent++;

        // Write docstring
        if (parameters.Count == 0 && string.IsNullOrWhiteSpace(returnTypeStr)) { // Single-line
            writer.WriteLine($@"""""""{Description}""""""");
        } else {
            writer.WriteLine($@"""""""{Description}");
            if (parameters.Count > 0) {
                TriggerScript.WriteBlankLine(writer);
                writer.WriteLine("Args:");
                writer.Indent++;
                foreach (Parameter parameter in parameters) {
                    if (parameter.Type == ScriptType.None) {
                        continue;
                    }

                    writer.WriteLine($"{parameter.Name} ({parameter.TypeStr()}): _description_. Defaults to {parameter.DefaultStr()}.");
                }
                writer.Indent--;
            }
            if (!string.IsNullOrWhiteSpace(returnTypeStr)) {
                TriggerScript.WriteBlankLine(writer);
                writer.WriteLine("Returns:");
                writer.Indent++;
                writer.WriteLine($"{returnTypeStr}: _description_");
                writer.Indent--;
            }
            writer.WriteLine(@"""""""");
        }

        if (string.IsNullOrWhiteSpace(bodyOverride)) {
            if (ReturnType == ScriptType.Bool) {
                writer.WriteLine("return False");
            } else {
                writer.WriteLine("pass");
            }
        } else {
            foreach (string line in bodyOverride.Split("\n")) {
                writer.WriteLine(line);
            }
        }

        writer.Indent--;
    }
}
