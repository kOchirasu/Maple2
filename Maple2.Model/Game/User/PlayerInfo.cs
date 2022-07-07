using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public record PlayerInfo(CharacterInfo Character, int PlotMapId, int PlotNumber, string HomeName, Trophy Trophy) {
    public long AccountId => Character.AccountId;
    public long CharacterId => Character.CharacterId;
    public string Name => Character.Name;
    public Gender Gender => Character.Gender;
    public Job Job => Character.Job;
    public short Level => Character.Level;
    public int MapId => Character.MapId;
    public string Picture => Character.Picture;
    public string Motto => Character.Motto;
    public int Channel => Character.Channel;
    public bool Online => Character.Online;

    // Not used...
    public int ApartmentNumber => 0;

    public static implicit operator PlayerInfo(Player player) {
        return new PlayerInfo(player, player.Home.PlotMapId, player.Home.PlotNumber, player.Home.Name, player.Account.Trophy);
    }
}

public record CharacterInfo(long AccountId, long CharacterId, string Name, Gender Gender, Job Job, short Level, int MapId, string Picture, string Motto) {
    public short Channel;
    public bool Online;

    public static implicit operator CharacterInfo(Player player) {
        return new CharacterInfo(
            AccountId: player.Account.Id,
            CharacterId: player.Character.Id,
            Name: player.Character.Name,
            Gender: player.Character.Gender,
            Job: player.Character.Job,
            Level: player.Character.Level,
            MapId: player.Character.MapId,
            Picture: player.Character.Picture,
            Motto: player.Character.Motto
        ) {
            Channel = player.Character.Channel,
            Online = player.Account.Online,
        };
    }
}
