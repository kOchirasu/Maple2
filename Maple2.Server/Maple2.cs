using System;
using Autofac;
using Maple2.Database.Data;
using Maple2.Database.Storage;
using Maple2.Model.User;
using Maple2.Server.Commands;
using Maple2.Server.Core.Modules;
using Maple2.Server.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = new ContainerBuilder();
builder.RegisterModule<NLogModule>();
builder.RegisterModule<CliModule>();
using ILifetimeScope mapleScope = builder.Build().BeginLifetimeScope();

ILogger logger = mapleScope.Resolve<ILogger<Program>>();
logger.LogInformation("MapleServer started with {Length} args: {Args}", args.Length, string.Join(", ", args));

// var worldClient = loginHost.Services.GetAutofacRoot().Resolve<World.WorldClient>();
// Debug.Assert(worldClient != null);
//
// HealthResponse health = worldClient.Health(new Empty(), new CallOptions().WithWaitForReady());
// Debug.Assert(health.Ok, "Maple2.Server.World is not healthy.");
//
// HelloResponse reply = worldClient.SayHello(new HelloRequest { Name = "user" });
// Console.WriteLine($"Greeting: {reply.Message}");

// var tester = loginHost.Services.GetAutofacRoot().Resolve<Tester.TesterClient>();
// Debug.Assert(tester != null);
//
// TestReply testReply = tester.SayTest(new TestRequest { Name = "user" });
// Console.WriteLine($"Testing: {testReply.Message}");


const string connectionString = "Server=localhost;Database=server-data;User=root;Password=maplestory";

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options;
using var initContext = new InitializationContext(options);
// Initialize database if needed
if (!initContext.Initialize()) {
    logger.LogInformation("Database has already been initialized");
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
        logger.LogError(ex, "Uncaught exception handling command");
    }
}
