using System;
using System.CommandLine;

namespace Maple2.Server.Commands;

public class QuitCommand : Command {
    private const string NAME = "quit";
    private const string DESCRIPTION = "Safely stops all running servers and quits the application.";

    public QuitCommand()
            : base(NAME, DESCRIPTION) {
        this.SetHandler(() => {
            Console.WriteLine("Can't stop world");
            Console.WriteLine("Can't stop login");
            Console.WriteLine("Can't stop game");
            Environment.Exit(0);
        });
    }
}
