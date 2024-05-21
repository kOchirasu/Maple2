﻿using System.Globalization;
using System.IO;
using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Database.Storage;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Modules;
using Maple2.Server.Global.Service;
using Maple2.Server.World;
using Maple2.Server.World.Containers;
using Maple2.Server.World.Service;
using Maple2.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Force Globalization to en-US because we use periods instead of commas for decimals
CultureInfo.CurrentCulture = new("en-US");

DotEnv.Load();

IConfigurationRoot configRoot = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configRoot)
    .CreateLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options => {
    options.Listen(new IPEndPoint(IPAddress.Any, Target.GrpcWorldPort), listen => {
        listen.Protocols = HttpProtocols.Http2;
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

builder.Services.AddGrpc();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<WorldServer>();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(autofac => {
    autofac.RegisterType<WorldServer>()
        .SingleInstance();
    // Database
    autofac.RegisterModule<GameDbModule>();
    autofac.RegisterModule<DataDbModule>();

    autofac.RegisterType<ChannelClientLookup>()
        .SingleInstance();
    autofac.RegisterType<PlayerInfoLookup>()
        .SingleInstance();
    autofac.RegisterType<GuildLookup>()
        .SingleInstance();
    autofac.RegisterType<PartyLookup>()
        .SingleInstance();
    autofac.RegisterType<GroupChatLookup>()
        .SingleInstance();
    autofac.RegisterType<PartySearchLookup>()
        .SingleInstance();
    autofac.RegisterType<BlackMarketLookup>()
        .SingleInstance();
});

WebApplication app = builder.Build();
app.Services.GetService<WorldServer>();
app.UseRouting();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
app.MapGrpcService<WorldService>();
app.MapGrpcService<GlobalService>();


ILifetimeScope root = app.Services.GetAutofacRoot();
var gameStorage = root.Resolve<GameStorage>();
var mapStorage = root.Resolve<MapMetadataStorage>();

using (GameStorage.Request db = gameStorage.Context()) {
    if (!db.InitUgcMap(mapStorage.GetAllUgc())) {
        Log.Fatal("Failed to initialize UgcMap");
        return;
    }
}

await app.RunAsync();
