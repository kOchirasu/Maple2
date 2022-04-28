using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets; 

public static class BannerListPacket {
    public static ByteWriter SetBanner() {
        short count = 0; // TODO: Load banners

        var pWriter = Packet.Of(SendOp.BANNER_LIST);
        pWriter.WriteShort(count);
        for (int i = 0; i < count; i++) {
            pWriter.WriteInt(); // Id
            pWriter.WriteUnicodeString("name"); // Name
            pWriter.WriteUnicodeString("merat"); // Type
            pWriter.WriteUnicodeString(); // SubType
            pWriter.WriteUnicodeString(); // Unknown
            pWriter.WriteUnicodeString("url"); // Url
            pWriter.WriteInt(); // Language
            pWriter.WriteLong(); // Start Timestamp
            pWriter.WriteLong(); // End Timestamp
        }

        return pWriter;
    }
}
