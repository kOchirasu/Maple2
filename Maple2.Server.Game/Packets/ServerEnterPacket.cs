using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets; 

public static class ServerEnterPacket {
    public static ByteWriter Request(Account account, Character character) {
        var pWriter = Packet.Of(SendOp.SERVER_ENTER);
        pWriter.WriteInt(session.FieldPlayer.ObjectId);
        pWriter.WriteLong(character.Id);
        pWriter.WriteShort();
        pWriter.WriteLong(character.Exp);
        pWriter.WriteLong(character.RestExp);
        pWriter.WriteLong(session.Player.Character.Mesos);
        // These Merets are added up
        pWriter.WriteLong(account.Merets); // Merets
        pWriter.WriteLong(); // Merets
        pWriter.WriteLong(); // Game Merets
        // These Merets are added up. If set, previous are ignored.
        pWriter.WriteLong(); // Game Merets
        pWriter.WriteLong(); // Event Merets

        pWriter.WriteLong(session.Player.Character.ValorToken);
        pWriter.WriteLong(session.Player.Character.Treva);
        pWriter.WriteLong(session.Player.Character.Rue);
        pWriter.WriteLong(session.Player.Character.HaviFruit);
        pWriter.WriteLong(); // Unknown Currency (Type 9)
        pWriter.WriteLong(); // Unknown Currency (Type 10)
        pWriter.WriteLong(); // Unknown Currency (Type 11)
        pWriter.WriteLong(); // Unknown Currency (Type 12)
        pWriter.WriteLong(session.Player.Character.MesoToken);
        pWriter.WriteUnicodeString(character.Picture);
        pWriter.WriteByte();
        pWriter.WriteByte();
        // REQUIRED OR CRASH
        // Unlocked Hidden Maps (Not on WorldMap)
        pWriter.WriteShort((short) session.Player.Progress.VisitedMaps.Count);
        foreach (int mapId in session.Player.Progress.VisitedMaps) {
            pWriter.WriteInt(mapId);
        }
        // Unlocked Maps (On WorldMap)
        pWriter.WriteShort((short) session.Player.Progress.Taxis.Count);
        foreach (int mapId in session.Player.Progress.Taxis) {
            pWriter.WriteInt(mapId);
        }
        pWriter.WriteLong();
        pWriter.WriteUnicodeString();
        pWriter.WriteUnicodeString("http://nxcache.nexon.net/maplestory2/maplenews/index.html");
        pWriter.WriteUnicodeString();
        pWriter.WriteUnicodeString(@"^https?://test-nxcache\.nexon\.net ^https?://nxcache\.nexon\.net");
        pWriter.WriteUnicodeString();


        return pWriter;
    }
}
