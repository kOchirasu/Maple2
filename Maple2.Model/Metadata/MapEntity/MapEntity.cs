using System;
using System.Text.Json.Serialization;

namespace Maple2.Model.Metadata;

public class MapEntity {
    public string XBlock { get; set; }
    public Guid Guid { get; set; }
    public string Name { get; set; }

    public MapBlock Block { get; set; }

    public MapEntity(string xBlock, Guid guid, string name) {
        XBlock = xBlock;
        Guid = guid;
        Name = name;
    }
}

public abstract partial record MapBlock([JsonDiscriminator] MapBlock.Discriminator Class) {
    public enum Discriminator : uint {
        Portal = 19716277,
        TaxiStation = 2234881030,
        Liftable = 52914141,
        Breakable = 3551547141,
        Ms2RegionSpawn = 2625779056,
        ObjectWeapon = 3638470414,
        BreakableActor = 2510283231,


        // BASE: Ms2InteractObject = 1928632421,
        InteractActor = 3797506670,
        InteractMesh = 1638661275,
        Telescope = 1660396588,

        // BASE: SpawnPoint = 2593567611,
        SpawnPointPC = 476587788,
        SpawnPointNPC = 2354491253,
        EventSpawnPointNPC = 4186340407,

        Ms2RegionSkill = 821242714,

        // BASE: Ms2TriggerObject = 244177309,
        Ms2TriggerActor = 1192557034,
        Ms2TriggerAgent = 3789099171,
        // Ms2TriggerBlock = 4034923737,
        Ms2TriggerBox = 1606545175,
        Ms2TriggerCamera = 1697877699,
        Ms2TriggerCube = 2031712866,
        Ms2TriggerEffect = 1728709847,
        Ms2TriggerLadder = 3330340952,
        Ms2TriggerMesh = 1957913511,
        Ms2TriggerPortal = 1960805826,
        Ms2TriggerRope = 2325100735,
        Ms2TriggerSkill = 737806629,
        Ms2TriggerSound = 558345729,

        TriggerModel = 3583829728,

        Ms2Bounding = 1539875768,
    }
}
