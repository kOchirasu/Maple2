using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;

namespace Maple2.Server.Game.Commands;

public class TutorialCommand : Command {
    private const string NAME = "tutorial";
    private const string DESCRIPTION = "Tutorial management.";

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public ItemStatsCalculator ItemStatsCalc { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public TutorialCommand(GameSession session) : base(NAME, DESCRIPTION) {
        Add(new RewardCommand(session, ItemStatsCalc));
    }

    private class RewardCommand : Command {
        private enum Type {
            Item,
            Skill,
            Map,
        }

        private readonly GameSession session;
        private readonly ItemStatsCalculator itemStatsCalc;

        public RewardCommand(GameSession session, ItemStatsCalculator itemStatsCalc) : base("reward", "Receive tutorial reward.") {
            this.session = session;
            this.itemStatsCalc = itemStatsCalc;

            var type = new Argument<Type>("type", "Type of tutorial reward.");

            AddArgument(type);
            this.SetHandler<InvocationContext, Type>(Handle, type);
        }

        private void Handle(InvocationContext ctx, Type type) {
            try {
                Player player = session.Player;
                JobTable jobTable = session.TableMetadata.JobTable;
                JobTable.Tutorial tutorial = jobTable.Entries[player.Character.Job.Code()].Tutorial;

                switch (type) {
                    case Type.Item: {
                        using GameStorage.Request db = session.GameStorage.Context();
                        foreach (JobTable.Item rewardItem in tutorial.StartItem.Concat(tutorial.Reward)) {
                            if (!session.ItemMetadata.TryGet(rewardItem.Id, out ItemMetadata? metadata)) {
                                continue;
                            }

                            var item = new Item(metadata, rewardItem.Rarity, rewardItem.Count) {
                                Stats = itemStatsCalc.Compute(metadata, rewardItem.Rarity),
                            };
                            item = db.CreateItem(player.Character.Id, item);
                            if (item == null) {
                                ctx.Console.Error.WriteLine($"Failed to create item: {rewardItem.Id}");
                                ctx.ExitCode = 1;
                                return;
                            }

                            session.Item.Inventory.Add(item);
                        }
                        break;
                    }
                    case Type.Skill:
                        break;
                    case Type.Map:
                        player.Unlock.Maps.UnionWith(tutorial.OpenMaps);
                        player.Unlock.Taxis.UnionWith(tutorial.OpenTaxis);
                        break;
                }
                ctx.ExitCode = 0;
            } catch (SystemException ex) {
                ctx.Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
        }
    }
}
