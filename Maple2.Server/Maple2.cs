using System;
using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Commands;
using Maple2.Server.Constants;
using Maple2.Server.Modules;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;
using Maple2.Server.Servers.World;
using Maple2.Server.Servers.World.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var rootBuilder = new ContainerBuilder();
rootBuilder.RegisterModule<NLogModule>();
using ILifetimeScope rootScope = rootBuilder.Build().BeginLifetimeScope();

ILogger logger = rootScope.Resolve<ILogger<Program>>();
logger.LogInformation("MapleServer started with {Length} args: {Args}", args.Length, string.Join(", ", args));

IHost worldHost = CreateStartupHost<WorldStartup>();
IHost loginHost = CreateStartupHost<LoginStartup>();
IHost gameHost = CreateStartupHost<GameStartup>();
using ILifetimeScope mapleScope = rootScope.BeginLifetimeScope(builder => {
    builder.Register(_ => worldHost)
        .Keyed<IHost>(HostType.World);
    builder.Register(_ => loginHost)
        .Keyed<IHost>(HostType.Login);
    builder.Register(_ => gameHost)
        .Keyed<IHost>(HostType.Game);
    builder.RegisterModule<CliModule>();
});

worldHost.StartAsync();
loginHost.StartAsync();
gameHost.StartAsync();

var client = loginHost.Services.GetAutofacRoot().Resolve<Greeter.GreeterClient>();
Debug.Assert(client != null);

HelloReply reply = client.SayHello(new HelloRequest { Name = "user" });
Console.WriteLine($"Greeting: {reply.Message}");

reply = client.SayHelloAgain(new HelloRequest { Name = "user" });
Console.WriteLine($"Greeting: {reply.Message}");


// const string connectionString = "Server=localhost;Database=server-data;User=root;Password=maplestory";
//
// DbContextOptions options = new DbContextOptionsBuilder()
//     .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options;
// using var initContext = new InitializationContext(options);
// // Initialize database if needed
// if (!initContext.Initialize()) {
//     logger.Info("Database has already been initialized.");
// }
//
// using var testContext = new TestContext(options);
// var writeAccount = new Account();
// var userStorage = new UserStorage(options, null);
// using (UserStorage.Request request = userStorage.Context()) {
//     writeAccount = request.CreateAccount(writeAccount);
//     Console.WriteLine($"Write {writeAccount.Id}");
// }
//
// using (UserStorage.Request request = userStorage.Context()) {
//     Account readAccount = request.GetAccount(writeAccount.Id);
//     Console.WriteLine($"Read {readAccount.Id}");
// }

var commandRouter = mapleScope.Resolve<CommandRouter>();
while (true) {
    string command = Console.ReadLine() ?? string.Empty;

    try {
        commandRouter.Invoke(command);
    } catch (SystemException ex) {
        logger.LogError(ex, "Uncaught exception handling command");
    }
}

static IHost CreateStartupHost<T>() where T : class {
    return Host.CreateDefaultBuilder()
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureWebHostDefaults(builder => builder.UseStartup<T>())
        .Build();
}