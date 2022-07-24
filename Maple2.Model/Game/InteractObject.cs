using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public interface IInteractObject : IByteSerializable {
    public InteractType Type { get; }
    public string EntityId { get; }
    public int Id { get; }
    public InteractState State { get; set; }
}

public abstract class InteractObject<T> : IInteractObject where T : InteractObject {
    public abstract InteractType Type { get; }

    protected readonly T Metadata;
    public int Id => Metadata.InteractId;

    public string EntityId { get; }
    public InteractState State { get; set; }

    protected InteractObject(string entityId, T metadata) {
        EntityId = entityId;
        Metadata = metadata;
        State = InteractState.Idle;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteString(EntityId);
        writer.Write<InteractState>(State);
        writer.Write<InteractType>(Type);
        writer.WriteInt(Id);
        writer.Write<Vector3>(Metadata.Position);
        writer.Write<Vector3>(Metadata.Rotation);
        writer.WriteUnicodeString(); // e.g. InteractMeshObject
        writer.WriteUnicodeString(); // e.g. interaction_chestA_02
        writer.WriteUnicodeString(); // e.g. Opened_A
        writer.WriteUnicodeString(); // e.g. Idle_A
        writer.WriteFloat(Metadata.Scale);
        writer.WriteBool(false);
    }
}

public sealed class InteractMeshObject : InteractObject<Ms2InteractMesh> {
    public override InteractType Type => InteractType.Mesh;

    public InteractMeshObject(string entityId, Ms2InteractMesh metadata) : base(entityId, metadata) { }
}

public sealed class InteractTelescopeObject : InteractObject<Ms2Telescope> {
    public override InteractType Type => InteractType.Telescope;

    public InteractTelescopeObject(string entityId, Ms2Telescope metadata) : base(entityId, metadata) { }
}

// sw_co_fi_funct_roulette_A01_
// co_fi_funct_roulette_A01_
// co_in_funct_extract_A01_
public sealed class InteractUiObject : InteractObject<Ms2SimpleUiObject> {
    public override InteractType Type => InteractType.Ui;

    public InteractUiObject(string entityId, Ms2SimpleUiObject metadata) : base(entityId, metadata) { }
}

// public sealed class InteractWebObject : InteractObject<InteractObject> {
//     public override InteractType Type => InteractType.Web;
//
//     public InteractWebObject(string entityId, InteractObject metadata) : base(entityId, metadata) { }
// }

public sealed class InteractDisplayImage : InteractObject<Ms2InteractDisplay> {
    public override InteractType Type => InteractType.DisplayImage;

    public InteractDisplayImage(string entityId, Ms2InteractDisplay metadata) : base(entityId, metadata) { }
}

public sealed class InteractGatheringObject : InteractObject<Ms2InteractActor> {
    public override InteractType Type => InteractType.Gathering;

    public int Count;

    public InteractGatheringObject(string entityId, Ms2InteractActor metadata) : base(entityId, metadata) { }
}

public sealed class InteractGuildPosterObject : InteractObject<Ms2InteractDisplay> {
    public override InteractType Type => InteractType.GuildPoster;

    public InteractGuildPosterObject(string entityId, Ms2InteractDisplay metadata) : base(entityId, metadata) { }
}

public sealed class InteractBillBoardObject : InteractObject<Ms2InteractMesh> {
    public override InteractType Type => InteractType.BillBoard;

    public long OwnerId { get; init; }           // CharacterId
    public string OwnerName { get; init; } = ""; // CharacterName

    public InteractBillBoardObject(string entityId, Ms2InteractMesh metadata) : base(entityId, metadata) { }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(OwnerId);
        writer.WriteUnicodeString(OwnerName);
    }
}

// public sealed class InteractWatchTowerObject : InteractObject<InteractObject> {
//     public override InteractType Type => InteractType.WatchTower;
//
//     public InteractWatchTowerObject(string entityId, InteractObject metadata) : base(entityId, metadata) { }
// }
