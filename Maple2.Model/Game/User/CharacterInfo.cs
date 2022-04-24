using Maple2.Model.Enum;

namespace Maple2.Model.Game; 

public record CharacterInfo(
        long AccountId, 
        long CharacterId, 
        string Name, 
        Gender Gender, 
        Job Job, 
        short Level,
        int MapId, 
        string ProfileUrl, 
        int PlotMapId, 
        int PlotId, 
        int ApartmentNumber, 
        long PlotExpiration, 
        int[] Trophy) {
    
    public JobCode JobCode => (JobCode)((int)Job / 10);
}
