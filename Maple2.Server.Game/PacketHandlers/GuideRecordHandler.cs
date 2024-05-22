﻿using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class GuideRecordHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.GuideRecord;


    public override void Handle(GameSession session, IByteReader packet) {
        int recordCount = packet.ReadInt();
        for (int record = 0; record < recordCount; record++) {
            int guideId = packet.ReadInt();
            int step = packet.ReadInt();

            session.Config.GuideRecords[guideId] = step;
        }
    }
}
