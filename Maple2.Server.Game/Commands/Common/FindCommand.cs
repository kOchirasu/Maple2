using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Text;
using Maple2.Database.Storage;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands.Common;

public class FindCommand<T> : Command where T : ISearchResult {
    private const int PAGE_SIZE = 5;

    private readonly GameSession session;
    private readonly ISearchable<T> storage;

    public FindCommand(GameSession session, ISearchable<T> storage) : base("find", "Search by querying metadata.") {
        this.session = session;
        this.storage = storage;

        var query = new Argument<string>("query", "Search query.");
        var page = new Option<int>(new[] {"--page", "-p"}, "Page of query results.");

        AddArgument(query);
        AddOption(page);
        this.SetHandler<InvocationContext, string, int>(Handle, query, page);
    }

    private void Handle(InvocationContext ctx, string query, int page) {
        try {
            List<T> results = storage.Search(query);
            int pages = (int)Math.Ceiling(results.Count / (float) PAGE_SIZE);
            page = Math.Clamp(page, 1, pages);
            var builder = new StringBuilder($"<b>{results.Count} results for '{query}' ({page}/{pages}):</b>");
            foreach (T result in results.Skip(PAGE_SIZE * (page - 1)).Take(PAGE_SIZE)) {
                builder.Append($"\n• {result.Id}: {result.Name}");
            }

            session.Send(NoticePacket.Message(builder.ToString(), true));
            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}
