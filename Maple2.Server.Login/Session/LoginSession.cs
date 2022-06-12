using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Microsoft.Extensions.Logging;
using static Maple2.Model.Error.CharacterCreateError;

namespace Maple2.Server.Login.Session;

public class LoginSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Delete;

    public long AccountId { get; private set; }
    public Guid MachineId { get; private set; }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    private Account account = null!;

    public LoginSession(TcpClient tcpClient, ILogger<LoginSession> logger) : base(tcpClient, logger) {
        State = SessionState.Connected;
    }

    public void Init(long accountId, Guid machineId) {
        AccountId = accountId;
        MachineId = machineId;
    }

    public void ListServers() {
        Send(BannerListPacket.SetBanner());
        Send(ServerListPacket.Load(Target.SEVER_NAME,
            new []{new IPEndPoint(Target.LOGIN_IP, Target.LOGIN_PORT)}, 1));
    }

    public void ListCharacters() {
        using GameStorage.Request db = GameStorage.Context();
        (account, IList<Character> characters) = db.ListCharacters(AccountId);

        var entries = new List<(Character, IDictionary<EquipTab, List<Item>>)>();
        foreach (Character character in characters) {
            IDictionary<EquipTab, List<Item>> equips =
                db.GetEquips(character.Id, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge);
            entries.Add((character, equips));
        }

        Send(CharacterListPacket.SetMax(account.MaxCharacters, Constant.ServerMaxCharacters));
        Send(CharacterListPacket.StartList());
        // Send each character data
        Send(CharacterListPacket.AddEntries(account, entries));
        Send(CharacterListPacket.EndList());
    }

    public void CreateCharacter(Character character, List<Item> outfits) {
        using (GameStorage.Request db = GameStorage.Context()) {
            db.BeginTransaction();
            character = db.CreateCharacter(character);

            var unlock = new Unlock();
            unlock.Emotes.UnionWith(new[] {
                90200011, // Greet
                90200004, // Scheme
                90200024, // Reject
                90200041, // Sit
                90200042, // Ledge Sit
                90200043, // Epiphany
            });
            db.InitNewCharacter(character.Id, unlock);

            outfits = db.CreateItems(character.Id, outfits.ToArray());

            if (!db.Commit()) {
                Send(CharacterListPacket.CreateError(s_char_err_system));
                return;
            }
        }

        Send(CharacterListPacket.SetMax(account.MaxCharacters, Constant.ServerMaxCharacters));
        Send(CharacterListPacket.AppendEntry(account, character,
            new Dictionary<EquipTab, List<Item>> {{EquipTab.Outfit, outfits}}));
    }
}
