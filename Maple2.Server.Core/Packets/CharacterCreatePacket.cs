using Maple2.Model.Error;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets; 

public static class CharacterCreatePacket {
    public static ByteWriter Error(CharacterCreateError error, string message = "") {
        var pWriter = Packet.Of(SendOp.CHARACTER_CREATE);
        pWriter.Write<CharacterCreateError>(error);
        pWriter.WriteUnicodeString(message);

        return pWriter;
    }
}
