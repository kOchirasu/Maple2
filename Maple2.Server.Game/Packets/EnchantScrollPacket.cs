using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class EnchantScrollPacket {
    private enum Command : byte {
        UseScroll = 0,
        Preview = 1,
        Enchant = 3,
        Reward = 4,
    }

    public static ByteWriter UseScroll(Item item, EnchantScrollMetadata metadata) {
        var pWriter = Packet.Of(SendOp.EnchantScroll);
        pWriter.Write<Command>(Command.UseScroll);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteShort(metadata.Type);
        pWriter.WriteBool(false);
        pWriter.WriteInt();
        pWriter.WriteInt(10000); // div 100 = Rate
        pWriter.WriteInt(metadata.ItemTypes.Length);
        foreach (int itemType in metadata.ItemTypes) {
            pWriter.WriteInt(itemType);
        }
        pWriter.WriteInt(metadata.Rarities.Length);
        foreach (int rarity in metadata.Rarities) {
            pWriter.WriteInt(rarity);
        }
        if (metadata.Type == 3) {
            pWriter.WriteInt(metadata.Enchants.Min());
            pWriter.WriteInt(metadata.Enchants.Max());
        }

        pWriter.WriteShort(metadata.MinLevel);
        pWriter.WriteShort(metadata.MaxLevel);

        return pWriter;
    }

    public static ByteWriter Preview(Item item, IDictionary<BasicAttribute, BasicOption> statDeltas) {
        var pWriter = Packet.Of(SendOp.EnchantScroll);
        pWriter.Write<Command>(Command.Preview);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteShort(1);

        pWriter.WriteInt(statDeltas.Count);
        foreach ((BasicAttribute attribute, BasicOption delta) in statDeltas) {
            pWriter.WriteShort((short) attribute);
            pWriter.Write<BasicOption>(delta);
        }

        return pWriter;
    }

    public static ByteWriter Enchant(Item item) {
        var pWriter = Packet.Of(SendOp.EnchantScroll);
        pWriter.Write<Command>(Command.Enchant);
        pWriter.Write<EnchantScrollError>(EnchantScrollError.s_enchantscroll_ok);
        pWriter.WriteLong(item.Uid);
        pWriter.WriteClass<Item>(item);

        return pWriter;
    }

    public static ByteWriter Error(EnchantScrollError error) {
        var pWriter = Packet.Of(SendOp.EnchantScroll);
        pWriter.Write<Command>(Command.Enchant);
        pWriter.Write<EnchantScrollError>(error);

        return pWriter;
    }

    // Don't know what this is used for
    public static ByteWriter Reward(ICollection<(int, short)> entries) {
        var pWriter = Packet.Of(SendOp.EnchantScroll);
        pWriter.Write<Command>(Command.Reward);
        pWriter.WriteInt(entries.Count);
        foreach (var entry in entries) {
            pWriter.WriteInt(entry.Item1);
            pWriter.WriteShort(entry.Item2);
        }

        return pWriter;
    }
}
