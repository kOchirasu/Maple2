using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;

namespace Maple2.Server.Commands;

public class CommandRouter {
    private static readonly IConsole DefaultConsole = new CommandConsole();

    private readonly ImmutableList<Command> commands;
    private readonly ImmutableDictionary<string, Command> aliasLookup;

    public CommandRouter(IEnumerable<Command> commands) {
        var listBuilder = ImmutableList.CreateBuilder<Command>();
        var dictionaryBuilder = ImmutableDictionary.CreateBuilder<string, Command>();
        foreach (Command command in commands) {
            listBuilder.Add(command);
            foreach (string alias in command.Aliases) {
                dictionaryBuilder.Add(alias, command);
            }
        }

        this.commands = listBuilder.ToImmutable();
        this.aliasLookup = dictionaryBuilder.ToImmutable();
    }

    public int Invoke(string commandLine, IConsole console = null) {
        return Invoke(CommandLineStringSplitter.Instance.Split(commandLine).ToArray(), console);
    }

    private int Invoke(string[] args, IConsole console = null) {
        if (args.Length == 0) {
            return 0;
        }

        console ??= DefaultConsole;
        if (aliasLookup.TryGetValue(args[0], out Command command)) {
            return command.Invoke(args, console);
        }

        if (args[0] != "help") {
            console.Error.WriteLine($"Unrecognized command '{args[0]}'");
        }

        console.Out.Write(GetCommandList());
        return 0;
    }

    private string GetCommandList() {
        int width = commands.Max(c => c.Name.Length);

        var builder = new StringBuilder();
        builder.Append("Commands:\n");
        foreach (Command command in commands) {
            if (command.IsHidden) continue;

            builder.Append($"  {command.Name.PadRight(width)}  {command.Description}\n");
        }

        builder.AppendLine();
        return builder.ToString();
    }
}
