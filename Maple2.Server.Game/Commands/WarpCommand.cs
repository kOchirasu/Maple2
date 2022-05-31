using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Text;
using Maple2.Database.Storage;
using Maple2.Model.Error;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class WarpCommand : Command {
    private const string NAME = "warp";
    private const string DESCRIPTION = "Map warping.";
    private const int PAGE_SIZE = 5;

    private readonly GameSession session;
    private readonly MapMetadataStorage mapStorage;

    public WarpCommand(GameSession session, MapMetadataStorage mapStorage) : base(NAME, DESCRIPTION) {
        this.session = session;
        this.mapStorage = mapStorage;

        var query = new Argument<string>("query", "MapId or string to query.");
        var page = new Option<int>(new[] {"--page", "-p"}, "Page of query results.");

        AddArgument(query);
        AddOption(page);
        this.SetHandler<InvocationContext, string, int>(Handle, query, page);
    }

    private void Handle(InvocationContext ctx, string query, int page) {
        try {
            if (int.TryParse(query, out int mapId)) {
                if (mapStorage.TryGet(mapId, out MapMetadata? map)) {
                    WarpMap(map);
                    ctx.ExitCode = 0;
                    return;
                }
            }

            List<MapMetadata> results = mapStorage.Search(query);
            if (results.Count == 1) {
                WarpMap(results[0]);
                ctx.ExitCode = 0;
                return;
            }

            int pages = (int)Math.Ceiling(results.Count / (float) PAGE_SIZE);
            page = Math.Clamp(page, 1, pages);
            var builder = new StringBuilder($"<b>{results.Count} results for '{query}' ({page}/{pages}):</b>");
            foreach (MapMetadata map in results.Skip(PAGE_SIZE * (page - 1)).Take(PAGE_SIZE)) {
                builder.Append($"\n• {map.Id}: {map.Name}");
            }

            session.Send(NoticePacket.Message(builder.ToString(), true));
            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }

    private void WarpMap(MapMetadata map) {
        session.Send(NoticePacket.Message($"Warping to '{map.Name}' ({map.Id})", true));
        session.Send(session.PrepareField(map.Id)
            ? FieldEnterPacket.Request(session.Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
    }
}
