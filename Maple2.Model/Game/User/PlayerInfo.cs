using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public record PlayerInfo(CharacterInfo Character, HomeInfo Home, Trophy Trophy) {
    public long AccountId => Character.AccountId;
    public long CharacterId => Character.CharacterId;
    public string Name => Character.Name;
    public Gender Gender => Character.Gender;
    public Job Job => Character.Job;
    public short Level => Character.Level;
    public int MapId => Character.MapId;
    public string Picture => Character.Picture;
    public string Motto => Character.Motto;
    public bool Online => Character.Online;
}

public record HomeInfo(string Name, int PlotMapId, int PlotId, int ApartmentNumber, long PlotExpiration);

public record CharacterInfo(long AccountId, long CharacterId, string Name, Gender Gender, Job Job, short Level,
    int MapId, string Picture, string Motto, bool Online);
