﻿using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class BreakableHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Breakable;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required SkillMetadataStorage SkillMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();
        long skillUid = packet.ReadLong();
        int skillId = packet.ReadInt();
        short level = packet.ReadShort();
        if (!SkillMetadata.TryGet(skillId, level, out SkillMetadata? metadata)) {
            Logger.Error("Invalid skill use: {SkillId},{Level}", skillId, level);
            return;
        }

        var record = new SkillRecord(metadata, skillUid, session.Player);
        byte motionPoint = packet.ReadByte();
        if (!record.TrySetMotionPoint(motionPoint)) {
            Logger.Error("Invalid MotionPoint({MotionPoint}) for {Record}", motionPoint, record);
            return;
        }
        byte attackPoint = packet.ReadByte();
        if (!record.TrySetAttackPoint(attackPoint)) {
            Logger.Error("Invalid AttackPoint({AttackPoint}) for {Record}", attackPoint, record);
            return;
        }

        if (session.Field?.TryGetBreakable(entityId, out FieldBreakable? breakable) == true) {
            if (breakable.Value.GlobalDropBoxId != 0) {
                IList<Item> items = session.Field.ItemDrop.GetGlobalDropItem(breakable.Value.GlobalDropBoxId, session.Field.Metadata.Drop.Level);
                foreach (Item item in items) {
                    FieldItem fieldItem = session.Field.SpawnItem(breakable, breakable.Position, breakable.Rotation, item, session.CharacterId);
                    session.Field.Broadcast(FieldPacket.DropItem(fieldItem));
                }
            }
            breakable.UpdateState(BreakableState.Break);
        } else {
            Console.WriteLine(entityId + " does not exist...");
        }
    }
}
