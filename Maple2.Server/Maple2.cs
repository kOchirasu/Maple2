using System;
using Autofac;
using Maple2.Database.Context;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Server.Commands;
using Maple2.Server.Modules;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = new ContainerBuilder();
builder.RegisterModule<CliModule>();
using ILifetimeScope mapleScope = builder.Build().BeginLifetimeScope();

Log.Information("MapleServer started with {Length} args: {Args}", args.Length, string.Join(", ", args));

// using var channel = GrpcChannel.ForAddress("https://localhost:20101");
// var worldClient = new World.WorldClient(channel);
//
// HealthResponse health = worldClient.Health(new Empty(), new CallOptions().WithWaitForReady());
// Console.WriteLine("HEALTHY");
//
// HelloResponse reply = worldClient.SayHello(new HelloRequest { Name = "user" });
// Console.WriteLine($"Greeting: {reply.Message}");

// var tester = loginHost.Services.GetAutofacRoot().Resolve<Tester.TesterClient>();
// Debug.Assert(tester != null);
//
// TestReply testReply = tester.SayTest(new TestRequest { Name = "user" });
// Console.WriteLine($"Testing: {testReply.Message}");


const string connectionString = "Server=localhost;Database=game-server;User=root;Password=maplestory";

DbContextOptions options = new DbContextOptionsBuilder()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options;
using var initContext = new InitializationContext(options);
// Initialize database if needed
if (!initContext.Initialize()) {
    Log.Information("Database has already been initialized");
}

using var testContext = new Ms2Context(options);
var writeAccount = new Account();
var userStorage = new GameStorage(options, null, null);
using (GameStorage.Request request = userStorage.Context()) {
    writeAccount = request.CreateAccount(writeAccount);
    Console.WriteLine($"Write {writeAccount.Id}");
}

using (GameStorage.Request request = userStorage.Context()) {
    Account readAccount = request.GetAccount(writeAccount.Id);
    Console.WriteLine($"Read {readAccount.Id}");
}

var commandRouter = mapleScope.Resolve<CommandRouter>();
while (true) {
    string command = Console.ReadLine() ?? string.Empty;

    try {
        commandRouter.Invoke(command);
    } catch (SystemException ex) {
        Log.Error(ex, "Uncaught exception handling command");
    }
}
