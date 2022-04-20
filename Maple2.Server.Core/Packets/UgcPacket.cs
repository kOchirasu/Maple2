using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets; 

public static class UgcPacket {
    public static ByteWriter SetEndpoint(string unknownEndpoint, string resourceEndpoint, Locale locale = Locale.NA) {
        var pWriter = Packet.Of(SendOp.UGC);
        pWriter.WriteByte(0x11); // Function
        pWriter.WriteUnicodeString(unknownEndpoint); // Serves some random irrq.aspx
        pWriter.WriteUnicodeString(resourceEndpoint); // Serves resources
        pWriter.WriteUnicodeString(locale.ToString().ToLower());
        pWriter.Write<Locale>(locale);

        return pWriter;
    }
}
