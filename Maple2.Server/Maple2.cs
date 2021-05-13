using System;
using Autofac;
using Maple2.Database.Context;
using Maple2.Model.User;
using Maple2.Server.Commands;
using Maple2.Server.Config;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;
using Microsoft.EntityFrameworkCore;
using NLog;

// No DI here because MapleServer is static
ILogger logger = LogManager.GetCurrentClassLogger();
logger.Info($"MapleServer started with {args.Length} args: {string.Join(", ", args)}");

IContainer loginContainer = LoginContainerConfig.Configure();
using ILifetimeScope loginScope = loginContainer.BeginLifetimeScope();
var loginServer = loginScope.Resolve<LoginServer>();

IContainer gameContainer = GameContainerConfig.Configure();
using ILifetimeScope gameScope = gameContainer.BeginLifetimeScope();
var gameServer = gameScope.Resolve<GameServer>();

IContainer maple2Container = Maple2ContainerConfig.Configure(loginServer, gameServer);
using ILifetimeScope mapleScope = maple2Container.BeginLifetimeScope();
var commandRouter = mapleScope.Resolve<CommandRouter>();

loginServer.Start();
gameServer.Start();

const string connectionString = "Server=localhost;Database=maple-data;User=root;Password=maplestory";

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options;
using var initContext = new InitializationContext(options);
// Initialize database if needed
if (!initContext.Initialize()) {
    logger.Info("Database has already been initialized.");
}

using var testContext = new TestContext(options);
var account = new Account();
testContext.Account.Add(account);
testContext.SaveChanges();
Console.WriteLine($"Write {account.Id}");

Account readAccount = testContext.Account.Find(account.Id);
Console.WriteLine($"Read {readAccount.Id}");

while (true) {
    string command = Console.ReadLine() ?? string.Empty;

    try {
        commandRouter.Invoke(command);
    } catch (SystemException ex) {
        logger.Error($"Uncaught exception handling command: '{command}'", ex);
    }
}