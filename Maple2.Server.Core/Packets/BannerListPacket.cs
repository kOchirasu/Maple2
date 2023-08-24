using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets;

public static class BannerListPacket {
    public static ByteWriter Load(IList<PromoBanner> banners) {
        var pWriter = Packet.Of(SendOp.BannerList);
        pWriter.WriteShort((short) banners.Count);
        foreach(PromoBanner banner in banners) {
            pWriter.WriteInt(banner.Id);
            pWriter.WriteUnicodeString(banner.Name);
            pWriter.WriteUnicodeString(banner.Type.ToString());
            pWriter.WriteUnicodeString(banner.SubType);
            pWriter.WriteUnicodeString();
            pWriter.WriteUnicodeString(banner.Url);
            pWriter.Write<PromoBannerLanguage>(banner.Language);
            pWriter.WriteLong(banner.BeginTime);
            pWriter.WriteLong(banner.EndTime);
        }

        return pWriter;
    }
}
