namespace Maple2.Model.Metadata;

public record Breakable(
    bool Visible,
    int Id,
    int HideTime,
    int ResetTime)
: MapBlock(Discriminator.Breakable);
