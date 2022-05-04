using System.Net;
using Grpc.Core;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using Microsoft.Extensions.Logging;
using static Maple2.Model.Error.MigrationError;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class QuitHandler : PacketHandler<GameSession> {
    public override ushort OpCode => RecvOp.REQUEST_QUIT;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public WorldClient World { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public QuitHandler(ILogger<QuitHandler> logger) : base(logger) { }

    public override void Handle(GameSession session, IByteReader packet) {
        bool quitGame = packet.ReadBool();

        // Fully close client
        if (quitGame) {
            session.Disconnect();
            return;
        }

        try {
            var request = new MigrateOutRequest {
                AccountId = session.AccountId,
                CharacterId = 0,
                MachineId = session.MachineId.ToString(),
                Server = MigrateOutRequest.Types.Server.Login,
            };

            MigrateOutResponse response = World.MigrateOut(request);
            var endpoint = new IPEndPoint(Target.LOGIN_IP, Target.LOGIN_PORT);
            session.Send(MigrationPacket.GameToLogin(endpoint, response.Token));
        } catch (RpcException) {
            session.Send(MigrationPacket.GameToLoginError(s_move_err_default));
        } finally {
            session.Disconnect();
        }
    }
}
