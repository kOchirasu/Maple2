using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;

namespace Maple2.Server.Commands {
    public class CommandRouter {
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

        public int Invoke(string[] args, IConsole console = null) {
            console ??= new SystemConsole();
            if (args.Length == 0) {
                return 0;
            }

            if (aliasLookup.TryGetValue(args[0], out Command command)) {
                int errorResult = command.Invoke(args, console);
                if (errorResult != 0 && command is ILoggableCommand loggableCommand) {
                    WriteError(loggableCommand.GetErrorString(), console);
                }
                return errorResult;
            }

            if (args[0] != "help") {
                WriteError($"Unrecognized command '{args[0]}'", console);
            }

            console.Out.WriteLine(GetCommandList());
            return 1;
        }

        public int Invoke(string commandLine, IConsole console = null) {
            return Invoke(CommandLineStringSplitter.Instance.Split(commandLine).ToArray(), console);
        }

        private string GetCommandList() {
            int width = commands.Max(c => c.Name.Length);

            var builder = new StringBuilder();
            builder.Append("Commands:\n");
            foreach (Command command in commands) {
                if (command.IsHidden) continue;

                builder.Append($"  {command.Name.PadRight(width)}  {command.Description}\n");
            }

            return builder.ToString();
        }

        private static void WriteError(string message, IStandardOut console) {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            console.Out.WriteLine(message);
            console.Out.WriteLine();
            Console.ResetColor();
        }
    }
}