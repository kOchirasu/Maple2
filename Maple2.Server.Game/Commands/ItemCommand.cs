using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Text;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class ItemCommand : Command {
    private const string NAME = "item";
    private const string DESCRIPTION = "Item spawning.";

    private const int MAX_RARITY = 6;
    private const int MAX_SOCKET = 5;

    private readonly GameSession session;
    private readonly ItemMetadataStorage itemStorage;

    public ItemCommand(GameSession session, ItemMetadataStorage itemStorage) : base(NAME, DESCRIPTION) {
        this.session = session;
        this.itemStorage = itemStorage;

        AddCommand(new FindCommand(session, itemStorage));

        var id = new Argument<int>("id", "Id of item to spawn.");
        var amount = new Option<int>(new[] {"--amount", "-a"}, () => 1, "Amount of the item.");
        var rarity = new Option<int>(new[] {"--rarity", "-r"}, () => 1, "Rarity of the item.");
        var socket = new Option<int[]>(new[] {"--socket", "-s"}, "Number of sockets: '-s max -s unlocked'");

        AddArgument(id);
        AddOption(amount);
        AddOption(rarity);
        AddOption(socket);
        this.SetHandler<InvocationContext, int, int, int, int[]>(Handle, id, amount, rarity, socket);
    }

    private void Handle(InvocationContext ctx, int itemId, int amount, int rarity, int[] socket) {
        try {
            if (!itemStorage.TryGet(itemId, out ItemMetadata? metadata)) {
                ctx.ExitCode = 1;
                return;
            }

            if (metadata.Property.SlotMax == 0) {
                ctx.Console.Error.WriteLine($"{itemId} has SlotMax of 0, ignoring...");
                amount = Math.Clamp(amount, 1, int.MaxValue);
            } else {
                amount = Math.Clamp(amount, 1, metadata.Property.SlotMax);
            }
            rarity = Math.Clamp(rarity, 1, MAX_RARITY);

            var item = new Item(metadata, rarity, amount);
            if (item.Inventory is InventoryType.Gear or InventoryType.Outfit) {
                byte maxSockets = (byte) Math.Clamp(socket.Length >= 1 ? socket[0] : 0, 0, MAX_SOCKET);
                byte unlockSockets = (byte) Math.Clamp(socket.Length >= 2 ? socket[1] : 0, 0, maxSockets);
                item.Socket = new ItemSocket(maxSockets, unlockSockets);
            }

            using (GameStorage.Request db = session.GameStorage.Context()) {
                item = db.CreateItem(session.CharacterId, item);
            }

            if (!session.Item.Inventory.Add(item, true)) {
                session.Item.Inventory.Discard(item);
                ctx.Console.Error.WriteLine($"Failed to add item:{item.Id} to inventory");
                ctx.ExitCode = 1;
                return;
            }

            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }

    private class FindCommand : Command {
        private const int PAGE_SIZE = 5;

        private readonly GameSession session;
        private readonly ItemMetadataStorage itemStorage;

        public FindCommand(GameSession session, ItemMetadataStorage itemStorage) : base("find", "Finds an item by querying metadata.") {
            this.session = session;
            this.itemStorage = itemStorage;

            var query = new Argument<string>("query", "Item search query.");
            var page = new Option<int>(new[] {"--page", "-p"}, "Page of query results.");

            AddArgument(query);
            AddOption(page);
            this.SetHandler<InvocationContext, string, int>(Handle, query, page);
        }

        private void Handle(InvocationContext ctx, string query, int page) {
            try {
                List<ItemMetadata> results = itemStorage.Search(query);
                int pages = (int)Math.Ceiling(results.Count / (float) PAGE_SIZE);
                page = Math.Clamp(page, 1, pages);
                var builder = new StringBuilder($"<b>{results.Count} results for '{query}' ({page}/{pages}):</b>");
                foreach (ItemMetadata item in results.Skip(PAGE_SIZE * (page - 1)).Take(PAGE_SIZE)) {
                    builder.Append($"\n• {item.Id}: {item.Name}");
                }

                session.Send(NoticePacket.Message(builder.ToString(), true));
                ctx.ExitCode = 0;
            } catch (SystemException ex) {
                ctx.Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
        }
    }
}
