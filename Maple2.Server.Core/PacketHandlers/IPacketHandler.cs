using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Network;

namespace Maple2.Server.Core.PacketHandlers;

// All implementing classes should be thread safe and stateless.
// All state should be stored in Session
public interface IPacketHandler<in T> where T : Session {
    public ushort OpCode { get; }

    public void Handle(T session, IByteReader packet);
}