using System.Numerics;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Model;

public class FieldGuideObject : FieldEntity<IGuideObject>, IByteSerializable {
    public long CharacterId { get; init; }

    public FieldGuideObject(FieldManager field, int objectId, IGuideObject value) : base(field, objectId, value) { }

    public void WriteTo(IByteWriter writer) {
        writer.Write<GuideObjectType>(Value.Type);
        writer.WriteInt(ObjectId);
        writer.WriteLong(CharacterId);
        writer.Write<Vector3>(Position);
        writer.Write<Vector3>(Rotation);
        writer.WriteClass<IGuideObject>(Value);
    }
}
