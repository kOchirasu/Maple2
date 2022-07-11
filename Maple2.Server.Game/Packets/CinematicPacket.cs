using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Packets;

public static class CinematicPacket {
    private enum Command : byte {
        ToggleUi = 1,
        Hide = 2,
        View = 3,
        SetSkip = 4,
        StartSkip = 5,
        Conversation = 6,
        HideScript = 7,
        BalloonTalk = 8,
        RemoveBalloonTalk = 9,
        Caption = 10,
        Opening = 11,
        Intro = 12,
    }

    public static ByteWriter ToggleUi(bool hide) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.ToggleUi);
        pWriter.WriteBool(hide);

        return pWriter;
    }

    // hideScript, hideDirect
    public static ByteWriter Hide() {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.Hide);

        return pWriter;
    }

    public static ByteWriter View(int type, string script) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.View);
        pWriter.WriteInt(type);
        pWriter.WriteUnicodeString(script);
        pWriter.WriteUnicodeString();

        return pWriter;
    }

    public static ByteWriter SetSkipScene(string scene) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.SetSkip);
        pWriter.WriteByte(1); // $s_cutscene_skip_scene
        pWriter.WriteString(scene);

        return pWriter;
    }

    public static ByteWriter SetSkipState(string state) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.SetSkip);
        pWriter.WriteByte(2); // $s_cutscene_skip_state
        pWriter.WriteString(state);

        return pWriter;
    }

    public static ByteWriter StartSkip() {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.StartSkip);

        return pWriter;
    }

    public static ByteWriter Conversation(int npcId, string illustration, string script, int delay, Align align) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.Conversation);
        pWriter.WriteInt(npcId);
        pWriter.WriteString(illustration);
        pWriter.WriteUnicodeString(script);
        pWriter.WriteInt(delay);
        pWriter.WriteByte((byte) align);

        return pWriter;
    }

    public static ByteWriter HideScript() {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.HideScript);

        return pWriter;
    }

    public static ByteWriter BalloonTalk(bool isNpc, int objectId, string script, int duration, int delay) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.BalloonTalk);
        pWriter.WriteBool(isNpc);
        pWriter.WriteInt(objectId);
        pWriter.WriteUnicodeString(script);
        pWriter.WriteInt(duration);
        pWriter.WriteInt(delay);

        return pWriter;
    }

    public static ByteWriter RemoveBalloonTalk(int objectId) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.RemoveBalloonTalk);
        pWriter.WriteInt(objectId);

        return pWriter;
    }

    public static ByteWriter Caption(CaptionType type, string title, string script, Align align, float offsetRateX, float offsetRateY, int duration, float scale) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.Caption);
        pWriter.WriteUnicodeString($"{type}Caption");
        pWriter.WriteUnicodeString(title);
        pWriter.WriteUnicodeString(script);
        pWriter.WriteUnicodeString(align.ToString().Replace(", ", ""));
        pWriter.WriteInt(duration);
        pWriter.WriteFloat(offsetRateX);
        pWriter.WriteFloat(offsetRateY);
        pWriter.WriteFloat(scale);

        return pWriter;
    }

    public static ByteWriter Opening(string script, bool unknown = false) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.Opening);
        pWriter.WriteUnicodeString(script);
        pWriter.WriteBool(unknown);

        return pWriter;
    }

    public static ByteWriter Intro(string script) {
        var pWriter = Packet.Of(SendOp.Cinematic);
        pWriter.Write<Command>(Command.Intro);
        pWriter.WriteUnicodeString(script);

        return pWriter;
    }

    public static ByteWriter OneTimeEffect(int id, bool enable, string effectPath) {
        var pWriter = Packet.Of(SendOp.OneTimeEffect);
        pWriter.WriteInt(id);
        pWriter.WriteBool(enable);
        if (enable) {
            pWriter.WriteUnicodeString(effectPath);
        }

        return pWriter;
    }
}
