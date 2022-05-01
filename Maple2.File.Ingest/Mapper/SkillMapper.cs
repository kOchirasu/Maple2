using System.Diagnostics;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Skill;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class SkillMapper : TypeMapper<SkillMetadata> {
    private readonly SkillParser parser;

    public SkillMapper(M2dReader xmlReader) {
        parser = new SkillParser(xmlReader);
    }

    protected override IEnumerable<SkillMetadata> Map() {
        foreach ((int id, string name, SkillData data) in parser.Parse()) {
            if (data.basic == null) continue; // Old_JobChange_01
            Debug.Assert(data.basic.kinds.groupIDs.Length <= 1);
            // foreach (SkillLevelData level in data.level) {
            //     continue;
            // }

            yield return new SkillMetadata(
                Id:id,
                Name:name,
                Property:new SkillMetadataProperty(
                    Type:(SkillType)data.basic.kinds.type,
                    SubType:(SkillSubType)data.basic.kinds.subType,
                    SkillStyle:(SkillStyle)data.basic.kinds.rangeType,
                    Element:(Element)data.basic.kinds.element,
                    ContinueSkill:data.basic.kinds.continueSkill,
                    SpRecoverySkill:data.basic.kinds.spRecoverySkill,
                    ImmediateActive:data.basic.kinds.immediateActive,
                    UnrideOnHit:data.basic.kinds.unrideOnHit,
                    UnrideOnUse:data.basic.kinds.unrideOnUse,
                    ReleaseObjectWeapon:data.basic.kinds.releaseObjectWeapon,
                    SkillGroup:data.basic.kinds.groupIDs.FirstOrDefault()),
                State:new SkillMetadataState(),
                Levels:Array.Empty<SkillMetadataLevel>()
            );
        }
    }
}
