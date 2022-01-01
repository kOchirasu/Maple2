using System;
using Autofac;
using Maple2.Database.Data;
using Maple2.Database.Storage;
using Maple2.Model.User;
using Maple2.Server.Commands;
using Maple2.Server.Modules;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;
using Microsoft.EntityFrameworkCore;
using NLog;

// No DI here because MapleServer is static
ILogger logger = LogManager.GetCurrentClassLogger();
logger.Info($"MapleServer started with {args.Length} args: {string.Join(", ", args)}");

var rootBuilder = new ContainerBuilder();
rootBuilder.RegisterModule<LogModule>();
using ILifetimeScope rootScope = rootBuilder.Build().BeginLifetimeScope();
using ILifetimeScope loginScope = rootScope.BeginLifetimeScope(builder => {
    builder.RegisterModule<LoginModule>();
});
using ILifetimeScope gameScope = rootScope.BeginLifetimeScope(builder => {
    builder.RegisterModule<GameModule>();
});

var loginServer = loginScope.Resolve<LoginServer>();
var gameServer = gameScope.Resolve<GameServer>();
using ILifetimeScope mapleScope = rootScope.BeginLifetimeScope(builder => {
    builder.RegisterInstance(loginServer);
    builder.RegisterInstance(gameServer);
    builder.RegisterModule<CliModule>();
});

loginServer.Start();
gameServer.Start();

const string connectionString = "Server=localhost;Database=server-data;User=root;Password=maplestory";

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options;
using var initContext = new InitializationContext(options);
// Initialize database if needed
if (!initContext.Initialize()) {
    logger.Info("Database has already been initialized.");
}

using var testContext = new TestContext(options);
var writeAccount = new Account();
var userStorage = new UserStorage(options, null);
using (UserStorage.Request request = userStorage.Context()) {
    writeAccount = request.CreateAccount(writeAccount);
    Console.WriteLine($"Write {writeAccount.Id}");
}

using (UserStorage.Request request = userStorage.Context()) {
    Account readAccount = request.GetAccount(writeAccount.Id);
    Console.WriteLine($"Read {readAccount.Id}");
}

var commandRouter = mapleScope.Resolve<CommandRouter>();
while (true) {
    string command = Console.ReadLine() ?? string.Empty;

    try {
        commandRouter.Invoke(command);
    } catch (SystemException ex) {
        logger.Error(ex, "Uncaught exception handling command");
    }
}