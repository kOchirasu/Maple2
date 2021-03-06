﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;

namespace Maple2.Server.Commands {
    public class QuitCommand : Command {
        private const string NAME = "quit";
        private const string DESCRIPTION = "Safely stops all running servers and quits the application.";

        public QuitCommand(LoginServer loginServer, GameServer gameServer) : base(NAME, DESCRIPTION) {
            Handler = CommandHandler.Create(() => {
                loginServer.Stop();
                gameServer.Stop();
                Environment.Exit(0);
            });
        }
    }
}