using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Maple2.PacketLib.Tools;
using Maple2.Server.Servers.Game;

namespace Maple2.Server.Commands {
    public class SendPacketCommand : Command, ILoggableCommand {
        private const string NAME = "send";
        private const string DESCRIPTION = "Sends a packet to connected clients.";

        private readonly GameServer gameServer;
        private string error;

        public SendPacketCommand(GameServer gameServer) : base(NAME, DESCRIPTION) {
            this.gameServer = gameServer;

            AddOption(new Option<string>(new []{"--id", "-i"}, "ID of session to send packet to."));
            AddOption(new Option<bool>(new []{"--verbose", "-v"}, "Prints packets being sent."));
            AddArgument(new Argument<string[]>("packet", "Packet to send (as hex)."));
            Handler = CommandHandler.Create<string, bool, string[]>(Handle);
        }

        private int Handle(string id, bool verbose, string[] packet) {
            try {
                using var pWriter = new PoolByteWriter();
                foreach (string hexStr in packet) {
                    // This currently does not fail even if string contains invalid hex chars.
                    pWriter.WriteBytes(hexStr.ToByteArray());
                }

                if (verbose) {
                    Console.WriteLine($"Sending packets to: {id}");
                    Console.WriteLine(pWriter);
                }

                foreach (GameSession session in gameServer.GetSessions()) {
                    session.Send(pWriter);
                }

                return 0;
            } catch (SystemException ex) {
                error = ex.Message;
                return 1;
            }
        }

        public string GetErrorString() => error;
    }
}