using Autofac;
using Maple2.Server.Servers.Login;
using Maple2.Server.Servers.Game;
using NLog;

// No DI here because MapleServer is static
ILogger logger = LogManager.GetCurrentClassLogger();
logger.Info($"MapleServer started with {args.Length} args: {string.Join(", ", args)}");

IContainer loginContainer = LoginContainerConfig.Configure();
using ILifetimeScope loginScope = loginContainer.BeginLifetimeScope();
var loginServer = loginScope.Resolve<LoginServer>();
loginServer.Start();

IContainer gameContainer = GameContainerConfig.Configure();
using ILifetimeScope gameScope = gameContainer.BeginLifetimeScope();
var gameServer = gameScope.Resolve<GameServer>();
gameServer.Start();
