using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Pet;
using Maple2.File.Parser.Xml.String;
using Maple2.File.Parser.Xml.Table;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class PetMapper : TypeMapper<PetMetadata> {
    private readonly PetParser parser;
    private readonly Dictionary<int, string> petNames;

    public PetMapper(M2dReader xmlReader) {
        parser = new PetParser(xmlReader);

        // TODO: This should be handled by Maple2.File
        var nameSerializer = new XmlSerializer(typeof(StringMapping));
        XmlReader reader = xmlReader.GetXmlReader(xmlReader.GetEntry("en/petname.xml"));
        var mapping = nameSerializer.Deserialize(reader) as StringMapping;
        Debug.Assert(mapping != null);

        petNames = mapping.key.ToDictionary(key => int.Parse(key.id), key => key.name);
    }

    protected override IEnumerable<PetMetadata> Map() {
        Dictionary<int, PetData> datas = parser.Parse()
            .ToDictionary(entry => entry.Id, entry => entry.data);

        foreach (PetProperty property in parser.ParseProperty()) {
            if (!datas.TryGetValue(property.code, out PetData? data)) {
                // Defaults
                data = new PetData {
                    code = property.code,
                    slotNum = property.slotNum,
                    skill = new Skill(),
                    distance = new Distance {
                      pick = 1050,
                      warp = property.warpDistance,
                      trace = property.traceDistance,
                      battleTrace = property.battleTraceDistance,
                    },
                    time = new Time {
                        bore = 120000,
                        idle = 70000,
                        skill = 13000,
                        tired = 10000,
                        summonCast = 700,
                    },
                };
            }
            //Debug.Assert(property.slotNum == data.slotNum, $"{id} inventory slots mismatch: {property.slotNum} != {data.slotNum}");
// 60000026
            yield return new PetMetadata(
                Id:property.code,
                Name:petNames.GetValueOrDefault(property.code),
                Type:property.type,
                NpcId:property.npcID,
                ItemSlots:property.slotNum,
                EnableExtraction:property.enablePetExtraction,
                OptionLevel:property.optionLevel,
                OptionFactor:property.constantOptionFactor,
                Skill:data.skill.id == 0 ? null : new PetMetadataSkill(data.skill.id, data.skill.level),
                Effect:property.additionalEffectID == null ? Array.Empty<PetMetadataEffect>()
                    : property.additionalEffectID.Zip(property.additionalEffectLevel,
                        (effectId, level) => new PetMetadataEffect(effectId, level)).ToArray(),
                Distance: new PetMetadataDistance(
                    Warp:property.warpDistance,
                    Trace:property.traceDistance,
                    BattleTrace:property.battleTraceDistance),
                Time: new PetMetadataTime(
                    Idle:data.time.idle,
                    Bore: data.time.bore,
                    Summon: data.time.summonCast,
                    Tired: data.time.tired,
                    Skill: data.time.skill)
            );
        }
    }
}
