using System;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;

namespace Maple2.Server.Core.Packets;

public static class UgcPacket {
    private enum Command : byte {
        ProfilePicture = 11,
        SetEndpoint = 17,
    }

    public static ByteWriter SetEndpoint(Uri uri, Locale locale = Locale.NA) {
        var pWriter = Packet.Of(SendOp.Ugc);
        pWriter.Write(Command.SetEndpoint);
        pWriter.WriteUnicodeString($"{uri.Scheme}://{uri.Authority}/ws.asmx?wsdl");
        pWriter.WriteUnicodeString($"{uri.Scheme}://{uri.Authority}");
        pWriter.WriteUnicodeString(locale.ToString().ToLower());
        pWriter.Write<Locale>(locale);

        return pWriter;
    }

    public static ByteWriter ProfilePicture(Player player) {
        var pWriter = Packet.Of(SendOp.Ugc);
        pWriter.Write<Command>(Command.ProfilePicture);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteLong(player.Character.Id);
        pWriter.WriteUnicodeString(player.Character.Picture);

        return pWriter;
    }

    public static ByteWriter ProfilePicture(Character character) {
        var pWriter = Packet.Of(SendOp.Ugc);
        pWriter.Write<Command>(Command.ProfilePicture);
        pWriter.WriteInt();
        pWriter.WriteLong(character.Id);
        pWriter.WriteUnicodeString(character.Picture);

        return pWriter;
    }
}
