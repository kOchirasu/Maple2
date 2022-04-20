using System.Collections.Generic;
using System.Net;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Game;
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

    private const int ServerMaxCharacters = 8;

    private enum Command : byte {
        ServerList = 1,
        CharacterList = 2,
    }

    private readonly GlobalClient global;
    private readonly GameStorage gameStorage;

    public LoginHandler(GlobalClient global, GameStorage gameStorage, ILogger<LoginHandler> logger) : base(logger) {
        this.global = global;
        this.gameStorage = gameStorage;
    }

    public override void Handle(LoginSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        string user = packet.ReadUnicodeString();
        string pass = packet.ReadUnicodeString();

        try {
            logger.LogDebug("Logging in with user:{User} pass:{Pass}", user, pass);
            LoginResponse response = global.Login(new LoginRequest {Username = user, Password = pass});
            if (response.Code != LoginResponse.Types.Code.Ok) {
                session.Send(LoginResultPacket.Error((byte) response.Code, response.Message, response.AccountId));
                session.Disconnect();
                return;
            }

            switch (command) {
                case Command.ServerList:
                    session.Send(BannerListPacket.SetBanner());
                    session.Send(ServerListPacket.Load(Target.SEVER_NAME, 
                        new []{new IPEndPoint(Target.LOGIN_IP, Target.LOGIN_PORT)}, 1));
                    return;
                case Command.CharacterList: {
                    using GameStorage.Request db = gameStorage.Context();
                    (Account account, IList<Character> characters) = db.ListCharacters(response.AccountId);
                    // TODO:
                    // Load Players by accountId+World?
                    //
                    // Console.WriteLine("Initializing login with " + session.Account.Id);
                    session.Send(LoginResultPacket.Success(account.Id));
                    //session.Send(UgcPacket.SetEndpoint("http://127.0.0.1/ws.asmx?wsdl", "http://127.0.0.1"));
                    session.Send(CharacterListPacket.SetMax(account.MaxCharacters, ServerMaxCharacters));
                    session.Send(CharacterListPacket.StartList());
                    // Send each character data
                    logger.LogDebug("Loading character list TODO");
                    session.Send(CharacterListPacket.AddEntries(new List<Character>()));
                    session.Send(CharacterListPacket.EndList());
                    return;
                }
                default:
                    logger.LogError("Invalid type: {Type}", command);
                    break;
            }
        } catch (RpcException ex) {
            logger.LogError(ex, "Failed to login");
        }

        // Disconnect by default if anything goes wrong.
        session.Disconnect();
    }
}
