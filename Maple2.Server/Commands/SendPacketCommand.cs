using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using Maple2.PacketLib.Tools;

namespace Maple2.Server.Commands;

public class SendPacketCommand : Command {
    private const string NAME = "send";
    private const string DESCRIPTION = "Sends a packet to connected clients.";

    public SendPacketCommand() : base(NAME, DESCRIPTION) {
        var id = new Option<string>(new[] { "--id", "-i" }, "ID of session to send packet to.");
        var verbose = new Option<bool>(new[] { "--verbose", "-v" }, "Prints packets being sent.");
        var packet = new Argument<string[]>("packet", "Packet to send (as hex).");

        AddOption(id);
        AddOption(verbose);
        AddArgument(packet);
        this.SetHandler<InvocationContext, string, bool, string[]>(Handle, id, verbose, packet);
    }

    private void Handle(InvocationContext ctx, string id, bool verbose, string[] packet) {
        try {
            using var pWriter = new PoolByteWriter();
            foreach (string hexStr in packet) {
                // This currently does not fail even if string contains invalid hex chars.
                pWriter.WriteBytes(hexStr.ToByteArray());
            }

            if (verbose) {
                ctx.Console.Out.WriteLine($"Sending packets to: {id}");
                ctx.Console.Out.WriteLine(pWriter.ToString());
            }

            Console.WriteLine("No game server to send packet");

            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}