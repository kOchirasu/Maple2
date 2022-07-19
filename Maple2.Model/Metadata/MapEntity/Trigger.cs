using System.Numerics;

namespace Maple2.Model.Metadata;

public abstract record Trigger(MapBlock.Discriminator Class, int TriggerId, bool Visible) : MapBlock(Class);

public record Ms2TriggerActor(
    string InitialSequence,
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerActor, TriggerId, Visible);

public record Ms2TriggerAgent(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerAgent, TriggerId, Visible);

public record Ms2TriggerBox(
    Vector3 Position,
    Vector3 Dimensions,
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerBox, TriggerId, Visible);

public record Ms2TriggerCamera(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerCamera, TriggerId, Visible);

public record Ms2TriggerCube(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerCube, TriggerId, Visible);

public record Ms2TriggerEffect(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerEffect, TriggerId, Visible);

public record Ms2TriggerLadder(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerLadder, TriggerId, Visible);

public record Ms2TriggerMesh(
    float Scale,
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerMesh, TriggerId, Visible);

public record Ms2TriggerPortal(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerPortal, TriggerId, Visible);

public record Ms2TriggerRope(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerRope, TriggerId, Visible);

public record Ms2TriggerSkill(
    int SkillId,
    short Level,
    Vector3 Position,
    Vector3 Rotation,
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerSkill, TriggerId, Visible);

public record Ms2TriggerSound(
    int TriggerId,
    bool Visible)
: Trigger(Discriminator.Ms2TriggerSound, TriggerId, Visible);
