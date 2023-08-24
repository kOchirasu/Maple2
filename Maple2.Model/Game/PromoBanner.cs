using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class PromoBanner : IByteSerializable {
    public int Id { get; init; }
    public string Name { get; init; } // name must start with "homeproduct_" for Meret Market banners
    public PromoBannerType Type { get; init; }
    public string SubType { get; init; }
    public string Url { get; init; } // Meret Market banner resolution: 538x301
    public PromoBannerLanguage Language { get; init; }
    public long BeginTime { get; init; }
    public long EndTime { get; init; }

    public PromoBanner(int id) {
        Id = id;
        Name = string.Empty;
        SubType = string.Empty;
        Url = string.Empty;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteUnicodeString(Name);
        writer.WriteUnicodeString(Type.ToString());
        writer.WriteUnicodeString(SubType);
        writer.WriteUnicodeString();
        writer.WriteUnicodeString(Url);
        writer.Write<PromoBannerLanguage>(Language);
        writer.WriteLong(BeginTime);
        writer.WriteLong(EndTime);
    }
}
