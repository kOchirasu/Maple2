using System.CommandLine;
using System.CommandLine.Invocation;
using Maple2.Server.Servers.Game;
using Maple2.Server.Servers.Login;

namespace Maple2.Server.Commands {
    public class StopCommand : Command {
        private const string NAME = "stop";
        private const string DESCRIPTION = "Safely stops specified server.";

        private enum ServerType { Login, Game }

        private readonly LoginServer loginServer;
        private readonly GameServer gameServer;

        public StopCommand(LoginServer loginServer, GameServer gameServer) : base(NAME, DESCRIPTION) {
            this.loginServer = loginServer;
            this.gameServer = gameServer;

            AddArgument(new Argument<ServerType>("server", "Server type."));
            Handler = CommandHandler.Create<ServerType>(Handle);
        }

        private void Handle(ServerType server) {
            switch (server) {
                case ServerType.Login:
                    loginServer.Stop();
                    break;
                case ServerType.Game:
                    gameServer.Stop();
                    break;
            }
        }
    }
}