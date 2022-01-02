using System;
using System.CommandLine;
using Autofac.Features.AttributeFilters;
using Maple2.Server.Constants;
using Microsoft.Extensions.Hosting;

namespace Maple2.Server.Commands;

public class QuitCommand : Command {
    private const string NAME = "quit";
    private const string DESCRIPTION = "Safely stops all running servers and quits the application.";

    public QuitCommand(
            [KeyFilter(HostType.World)] IHost worldHost,
            [KeyFilter(HostType.Login)] IHost loginHost,
            [KeyFilter(HostType.Game)] IHost gameHost) : base(NAME, DESCRIPTION) {
        this.SetHandler(() => {
            worldHost.StopAsync();
            loginHost.StopAsync();
            gameHost.StopAsync();
            Environment.Exit(0);
        });
    }
}