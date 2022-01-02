using System.CommandLine;
using Autofac.Features.AttributeFilters;
using Maple2.Server.Constants;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.Hosting;

namespace Maple2.Server.Commands;

public class StopCommand : Command {
    private const string NAME = "stop";
    private const string DESCRIPTION = "Safely stops specified server.";

    private readonly IHost worldHost;
    private readonly IHost loginHost;
    private readonly IHost gameHost;

    public StopCommand(
            [KeyFilter(HostType.World)] IHost worldHost,
            [KeyFilter(HostType.Login)] IHost loginHost,
            [KeyFilter(HostType.Game)] IHost gameHost) : base(NAME, DESCRIPTION) {
        this.worldHost = worldHost;
        this.loginHost = loginHost;
        this.gameHost = gameHost;

        var server = new Argument<HostType>("server", "Server type.");

        AddArgument(server);
        this.SetHandler<HostType>(Handle, server);
    }

    private void Handle(HostType host) {
        switch (host) {
            case HostType.World:
                worldHost.StopAsync().Wait();
                break;
            case HostType.Login:
                loginHost.StopAsync().Wait();
                break;
            case HostType.Game:
                gameHost.StopAsync().Wait();
                break;
        }
    }
}