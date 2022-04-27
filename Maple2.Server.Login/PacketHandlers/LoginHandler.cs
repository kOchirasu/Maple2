using System;
using System.Collections.Generic;
using System.Net;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
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
        packet.ReadShort(); // 1
        var machineId = packet.Read<Guid>();

        try {
            logger.LogDebug("Logging in with user:{User} pass:{Pass}", user, pass);
            LoginResponse response = global.Login(new LoginRequest {
                Username = user,
                Password = pass,
                MachineId = machineId.ToString(),
            });
            if (response.Code != LoginResponse.Types.Code.Ok) {
                session.Send(LoginResultPacket.Error((byte) response.Code, response.Message, response.AccountId));
                session.Disconnect();
                return;
            }

            switch (command) {
                case Command.ServerList: {
                    session.Send(BannerListPacket.SetBanner());
                    session.Send(ServerListPacket.Load(Target.SEVER_NAME, 
                        new []{new IPEndPoint(Target.LOGIN_IP, Target.LOGIN_PORT)}, 1));
                    return;
                }
                case Command.CharacterList: {
                    using GameStorage.Request db = gameStorage.Context();
                    (session.Account, IList<Character> characters) = db.ListCharacters(response.AccountId);

                    var entries = new List<(Character, IDictionary<EquipTab, List<Item>>)>();
                    foreach (Character character in characters) {
                        IDictionary<EquipTab, List<Item>> equips =
                            db.GetEquips(character.Id, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge);
                        entries.Add((character, equips));
                    }
                    
                    session.Send(LoginResultPacket.Success(session.Account.Id));
                    //session.Send(UgcPacket.SetEndpoint("http://127.0.0.1/ws.asmx?wsdl", "http://127.0.0.1"));
                    session.Send(CharacterListPacket.SetMax(session.Account.MaxCharacters, Constant.ServerMaxCharacters));
                    session.Send(CharacterListPacket.StartList());
                    // Send each character data
                    session.Send(CharacterListPacket.AddEntries(session.Account, entries));
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
