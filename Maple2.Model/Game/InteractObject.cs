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
}

public abstract class InteractObject<T> : IInteractObject where T : InteractObject {
    public abstract InteractType Type { get; }

    protected T Metadata { get; init; }
    public int Id { get; init; }
    public string Model { get; init; }
    public string Asset { get; init; }
    public string NormalState { get; init; }
    public string Reactable { get; init; }
    public float Scale { get; init; }

    public string EntityId { get; init; }

    protected InteractObject(string entityId, T metadata, string model = "", string asset = "", string normalState = "", string reactable = "", float scale = 1f) {
        EntityId = entityId;
        Metadata = metadata;
        Id = metadata.InteractId;
        Model = model;
        Asset = asset;
        NormalState = normalState;
        Reactable = reactable;
        Scale = scale;
    }

    public virtual void WriteTo(IByteWriter writer) {
        writer.WriteString(EntityId);
        writer.Write<InteractState>(InteractState.Reactable);
        writer.Write<InteractType>(Type);
        writer.WriteInt(Id);
        writer.Write<Vector3>(Metadata.Position);
        writer.Write<Vector3>(Metadata.Rotation);
        writer.WriteUnicodeString(Model); // e.g. InteractMeshObject
        writer.WriteUnicodeString(Asset); // e.g. interaction_chestA_02
        writer.WriteUnicodeString(NormalState); // e.g. Opened_A
        writer.WriteUnicodeString(Reactable); // e.g. Idle_A
        writer.WriteFloat(Scale);
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

    public long OwnerAccountId { get; init; }
    public long OwnerCharacterId { get; init; }
    public string OwnerName { get; init; }
    public string OwnerPicture { get; init; }
    public short OwnerLevel { get; init; }
    public JobCode OwnerJob { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public bool PublicHouse { get; init; }
    public long CreationTime { get; init; }
    public long ExpirationTime { get; init; }

    public InteractBillBoardObject(string entityId, Ms2InteractMesh metadata, Character owner) : base(entityId, metadata) {
        OwnerAccountId = owner.AccountId;
        OwnerCharacterId = owner.Id;
        OwnerName = owner.Name;
        OwnerPicture = owner.Picture;
        OwnerLevel = owner.Level;
        OwnerJob = owner.Job.Code();
    }

    public override void WriteTo(IByteWriter writer) {
        base.WriteTo(writer);
        writer.WriteLong(OwnerCharacterId);
        writer.WriteUnicodeString(OwnerName);
    }
}

// public sealed class InteractWatchTowerObject : InteractObject<InteractObject> {
//     public override InteractType Type => InteractType.WatchTower;
//
//     public InteractWatchTowerObject(string entityId, InteractObject metadata) : base(entityId, metadata) { }
// }
