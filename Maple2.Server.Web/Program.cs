using System;
using System.Globalization;
using System.IO;
using System.Net;
using Maple2.Server.Web.Constants;
using Maple2.Server.Web.Endpoints;
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

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

WebApplication app = builder.Build();
app.MapGet("/data/profiles/avatar/{characterId}/{hash}.png", ProfileEndpoint.Get);
app.MapPost("/urq.aspx", UploadEndpoint.Post);
await app.RunAsync();
