using System.Numerics;

namespace Maple2.Server.Game.Model; 

public interface IFieldObject {
    public int ObjectId { get; init; }
    
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}

public interface IActor<T> : IFieldObject {
    public T Value { get; init; }
    
    // Syncs the object to the field
    public virtual void Sync() { }
}
