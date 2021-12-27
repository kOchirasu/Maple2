using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Maple2.Server.Commands;

public class CommandConsole : IConsole {
    public IStandardStreamWriter Out { get; }
    public bool IsOutputRedirected => Console.IsOutputRedirected;
    public IStandardStreamWriter Error { get; }
    public bool IsErrorRedirected => Console.IsErrorRedirected;
    public bool IsInputRedirected => Console.IsInputRedirected;

    public CommandConsole() {
        Error = StandardErrorStreamWriter.Instance;
        Out = StandardOutStreamWriter.Instance;
    }

    private struct StandardErrorStreamWriter : IStandardStreamWriter {
        public static readonly StandardErrorStreamWriter Instance = new();

        public void Write(string value) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(value);
            Console.ResetColor();
        }
    }

    private struct StandardOutStreamWriter : IStandardStreamWriter {
        public static readonly StandardOutStreamWriter Instance = new();

        public void Write(string value) => Console.Out.Write(value);
    }
}
