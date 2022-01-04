using System;
using System.Collections.Generic;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Login.Session;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Login.PacketHandlers;

public class LoginHandler : PacketHandler<LoginSession> {
    public override ushort OpCode => RecvOp.RESPONSE_LOGIN;

    private enum Type : byte {
        ServerList,
        CharacterList,
    }

    public LoginHandler(ILogger<LoginHandler> logger) : base(logger) { }

    public override void Handle(LoginSession session, IByteReader packet) {
        var type = packet.Read<Type>();
        string user = packet.ReadUnicodeString();
        string pass = packet.ReadUnicodeString();

        logger.LogDebug("Logging in with user:{User} pass:{Pass}", user, pass);

        // TODO:
        // 1. Login to account by user+pass, this should return accountId
        // 2. Use accountId to read account data from database
        // 3. Handle packet

        switch (type) {
            case Type.ServerList:
                break;
            case Type.CharacterList:
                break;
        }

        // switch (type) {
        //     case Type.ServerList:
        //         //session.Send(PacketWriter.Of(SendOp.NPS_INFO).WriteLong().WriteUnicodeString());
        //         session.Send(BannerListPacket.SetBanner());
        //         session.Send(ServerListPacket.SetServers(serverName, serverIps));
        //         break;
        //     case type.CharacterList:
        //         ICollection<Player> players;
        //         using (UserStorage.Request request = userStorage.Context()) {
        //             players = request.ListPlayers(session.Account.Id);
        //         }
        //
        //         Console.WriteLine("Initializing login with " + session.Account.Id);
        //         session.Send(LoginResultPacket.InitLogin(session.Account.Id));
        //         session.Send(UgcPacket.SetEndpoint("http://127.0.0.1/ws.asmx?wsdl", "http://127.0.0.1"));
        //         session.Send(CharacterListPacket.SetMax(4, 6));
        //         session.Send(CharacterListPacket.StartList());
        //         // Send each character data
        //         session.Send(CharacterListPacket.AddEntries(players));
        //         session.Send(CharacterListPacket.EndList());
        //         break;
        // }
    }
}
