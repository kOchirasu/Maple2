
namespace Maple2.Model.Game;

public class Fish {
    public int TotalCaught { get; set; }
    public int TotalPrizeFish { get; set; }
    public int LargestFish { get; set; }
    public long ExpiryTime { get; init; }

    public Fish() { }
}
