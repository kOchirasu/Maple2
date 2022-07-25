using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public class FieldPortal : FieldEntity<Portal> {
    public bool Visible;
    public bool Enabled;
    public bool MinimapVisible;

    public FieldPortal(FieldManager field, int objectId, Portal value) : base(field, objectId, value) {
        Visible = value.Visible;
        Enabled = value.Enable;
        MinimapVisible = value.MinimapVisible;
    }
}
