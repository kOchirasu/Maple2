﻿using System;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using Maple2.Lua;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Modules;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game;
using Maple2.Server.Game.Commands;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Scripting.Npc;
using Maple2.Server.Game.Service;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;
using Maple2.Server.Game.Util.Sync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Force Globalization to en-US because we use periods instead of commas for decimals
CultureInfo.CurrentCulture = new("en-US");

IConfigurationRoot configRoot = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configRoot)
    .CreateLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options => {
    options.Listen(new IPEndPoint(IPAddress.Any, Target.GrpcChannelPort), listen => {
        listen.Protocols = HttpProtocols.Http2;
    });
});
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

builder.Services.AddGrpc();
builder.Services.RegisterModule<WorldClientModule>();
builder.Services.AddSingleton<GameServer>();
builder.Services.AddHostedService<GameServer>(provider => provider.GetService<GameServer>()!);

builder.Services.AddGrpcHealthChecks();
builder.Services.Configure<HealthCheckPublisherOptions>(options => {
    options.Delay = TimeSpan.Zero;
    options.Period = TimeSpan.FromSeconds(10);
});
builder.Services.AddHealthChecks()
    .AddCheck<GameServer>("game_channel_health_check");

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(autofac => {
    autofac.RegisterType<PacketRouter<GameSession>>()
        .As<PacketRouter<GameSession>>()
        .SingleInstance();
    autofac.RegisterType<GameSession>()
        .PropertiesAutowired()
        .AsSelf();
    autofac.RegisterInstance(new Lua(Target.LOCALE));
    autofac.RegisterType<ItemStatsCalculator>()
        .PropertiesAutowired()
        .SingleInstance();
    autofac.RegisterType<PlayerInfoStorage>()
        .SingleInstance();
    autofac.RegisterType<NpcScriptLoader>()
        .SingleInstance();

    // Database
    autofac.RegisterModule<GameDbModule>();
    autofac.RegisterModule<DataDbModule>();
    autofac.RegisterModule<WebDbModule>();

    // Make all packet handlers available to PacketRouter
    autofac.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        .Where(type => typeof(PacketHandler<GameSession>).IsAssignableFrom(type))
        .As<PacketHandler<GameSession>>()
        .PropertiesAutowired()
        .SingleInstance();

    // ChatCommand Handlers
    autofac.RegisterType<CommandRouter>();

    autofac.RegisterAssemblyTypes(typeof(CommandRouter).Assembly)
        .PublicOnly()
        .WithAttributeFiltering()
        .Where(type => typeof(Command).IsAssignableFrom(type))
        .As<Command>()
        .PropertiesAutowired();

    // Managers
    autofac.RegisterType<FieldManager.Factory>()
        .PropertiesAutowired()
        .SingleInstance();
});

WebApplication app = builder.Build();
app.UseRouting();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
app.MapGrpcService<ChannelService>();
app.MapGrpcHealthChecksService();

await app.RunAsync();
