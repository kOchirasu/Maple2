using System;
using System.CommandLine;

namespace Maple2.Server.Commands;

public class StopCommand : Command {
    private const string NAME = "stop";
    private const string DESCRIPTION = "Safely stops specified server.";

    public StopCommand() : base(NAME, DESCRIPTION) {
        Console.WriteLine("Not supported");
        // var server = new Argument<HostType>("server", "Server type.");
        //
        // AddArgument(server);
        // this.SetHandler<HostType>(Handle, server);
    }
}
