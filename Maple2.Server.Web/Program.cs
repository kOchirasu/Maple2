using System.Globalization;
using Maple2.Web.Constants;
using Maple2.Web.Endpoints;
using Microsoft.AspNetCore.Builder;
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
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

WebApplication app = builder.Build();
app.MapGet("/data/profiles/avatar/{characterId}/{hash}.png", ProfileEndpoint.Get);
app.MapPost("/urq.aspx", UploadEndpoint.Post);
await app.RunAsync($"http://{Target.WebIp}:{Target.WebPort}");
