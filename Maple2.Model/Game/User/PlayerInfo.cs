using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class PlayerInfo : CharacterInfo, IPlayerInfo {
    // Home/Plot
    public string HomeName { get; set; }
    public int PlotMapId { get; set; }
    public int PlotNumber { get; set; }
    public int ApartmentNumber { get; set; }
    public long PlotExpiryTime { get; set; }
    // Trophy
    public Trophy Trophy { get; set; }

    public static implicit operator PlayerInfo(Player player) {
        return new PlayerInfo(player, player.Home.Name, player.Account.Trophy) {
            PlotMapId = player.Home.PlotMapId,
            PlotNumber = player.Home.PlotNumber,
            ApartmentNumber = player.Home.ApartmentNumber,
            PlotExpiryTime = player.Home.PlotExpiryTime,
        };
    }

    public PlayerInfo(CharacterInfo character, string homeName, Trophy trophy) : base(character) {
        HomeName = string.IsNullOrWhiteSpace(homeName) ? "Unknown" : homeName;
        Trophy = trophy;
    }

    public PlayerInfo Clone() {
        return (PlayerInfo) MemberwiseClone();
    }
}

public class CharacterInfo {
    public long AccountId { get; }
    public long CharacterId { get; }

    public string Name { get; set; }
    public string Motto { get; set; }
    public string Picture { get; set; }
    public Gender Gender { get; set; }
    public Job Job { get; set; }
    public short Level { get; set; }
    public int GearScore { get; set; }
    // Health
    public long CurrentHp { get; set; }
    public long TotalHp { get; set; }
    // Location
    public int MapId { get; set; }
    public short Channel { get; set; }

    public long UpdateTime { get; set; }
    public bool Online => Channel != 0;

    public CharacterInfo(long accountId, long characterId, string name, string motto, string picture, Gender gender, Job job, short level) {
        AccountId = accountId;
        CharacterId = characterId;
        Name = name;
        Motto = motto;
        Picture = picture;
        Gender = gender;
        Job = job;
        Level = level;
    }

    public CharacterInfo(CharacterInfo other) {
        AccountId = other.AccountId;
        CharacterId = other.CharacterId;
        Name = other.Name;
        Motto = other.Motto;
        Picture = other.Picture;
        Gender = other.Gender;
        Job = other.Job;
        Level = other.Level;
        MapId = other.MapId;
        Channel = other.Channel;
    }

    public static implicit operator CharacterInfo(Player player) {
        return new CharacterInfo(
            accountId: player.Account.Id,
            characterId: player.Character.Id,
            name: player.Character.Name,
            motto: player.Character.Motto,
            picture: player.Character.Picture,
            gender: player.Character.Gender,
            job: player.Character.Job,
            level: player.Character.Level) {
                MapId = player.Character.MapId,
                Channel = player.Character.Channel,
            };
    }
}
