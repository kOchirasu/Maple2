﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Core.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    options.Listen(new IPEndPoint(IPAddress.Any, 80), listen => {
        listen.Protocols = HttpProtocols.Http1;
    });
    // Omitting for now since HTTPS requires a certificate
    // options.Listen(new IPEndPoint(IPAddress.Any, 443), listen => {
    //     listen.UseHttps();
    //     listen.Protocols = HttpProtocols.Http1;
    // });
});
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));
builder.Services.AddControllers();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(autofac => {
    // Database
    autofac.RegisterModule<WebDbModule>();
});

WebApplication app = builder.Build();
app.MapControllers();

var provider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
IEnumerable<ActionDescriptor> routes = provider.ActionDescriptors.Items
    .Where(x => x.AttributeRouteInfo != null);

Log.Logger.Debug("========== ROUTES ==========");
foreach(ActionDescriptor route in routes) {
    Log.Logger.Debug("{Route}", route.AttributeRouteInfo?.Template);
}

await app.RunAsync();
