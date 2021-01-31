using Maple2.PacketLib.Tools;
using Maple2.Server.Servers.Login;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.PacketHandlers.Login {
    public abstract class LoginPacketHandler : IPacketHandler<LoginSession> {
        public abstract ushort OpCode { get; }

        protected readonly ILogger logger;

        protected LoginPacketHandler(ILogger logger) {
            this.logger = logger;
        }

        public abstract void Handle(LoginSession session, IByteReader packet);

        public override string ToString() => $"[0x{OpCode:X4}] Login.{GetType().Name}";
    }
}