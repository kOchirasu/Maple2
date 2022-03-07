using System.Net;
using Grpc.Core;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Core.Packets;
using Maple2.Server.Global.Service;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;
using GlobalClient = Maple2.Server.Global.Service.Global.GlobalClient;

namespace Maple2.Server.Login.PacketHandlers;

public class LoginHandler : PacketHandler<LoginSession> {
    public override ushort OpCode => RecvOp.RESPONSE_LOGIN;

    private enum Type : byte {
        ServerList,
        CharacterList,
    }

    private readonly GlobalClient global;

    public LoginHandler(GlobalClient global, ILogger<LoginHandler> logger) : base(logger) {
        this.global = global;
    }

    public override void Handle(LoginSession session, IByteReader packet) {
        var type = packet.Read<Type>();
        string user = packet.ReadUnicodeString();
        string pass = packet.ReadUnicodeString();
        
        try {
            logger.LogDebug("Logging in with user:{User} pass:{Pass}", user, pass);
            LoginResponse response = global.Login(new LoginRequest {Username = user, Password = pass});
            if (response.Code != LoginResponse.Types.Code.Ok) {
                LoginResultPacket.Error((byte) response.Code, response.Message, response.AccountId);
                session.Disconnect();
                return;
            }

            switch (type) {
                case Type.ServerList:
                    session.Send(BannerListPacket.SetBanner());
                    session.Send(ServerListPacket.SetServers(Target.SEVER_NAME, 
                        new []{new IPEndPoint(Target.LOGIN_IP, Target.LOGIN_PORT)}, 1));
                    return;
                case Type.CharacterList:
                    // TODO:
                    // Load Players by accountId+World?
                    //
                    // Console.WriteLine("Initializing login with " + session.Account.Id);
                    session.Send(LoginResultPacket.Success(response.AccountId));
                    // session.Send(UgcPacket.SetEndpoint("http://127.0.0.1/ws.asmx?wsdl", "http://127.0.0.1"));
                    // session.Send(CharacterListPacket.SetMax(4, 6));
                    // session.Send(CharacterListPacket.StartList());
                    // // Send each character data
                    // session.Send(CharacterListPacket.AddEntries(players));
                    // session.Send(CharacterListPacket.EndList());
                    return;
                default:
                    logger.LogError("Invalid type: {Type}", type);
                    break;
            }
        } catch (RpcException ex) {
            logger.LogError(ex, "Failed to login");
        }

        // Disconnect by default if anything goes wrong.
        session.Disconnect();
    }
}
