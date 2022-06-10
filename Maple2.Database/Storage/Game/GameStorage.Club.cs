using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Club? GetClub(long clubId) {
            Model.Club? model = Context.Club.Find(clubId);
            if (model == null) {
                return null;
            }

            IList<ClubMember> members = GetClubMembers(clubId);
            Club club = model!;
            club.Leader = members.First(member => member.Info.CharacterId == model.LeaderId);
            members.Remove(club.Leader);
            club.Members = members;
            return club;
        }

        public IList<Tuple<long, string>> ListClubs(long characterId) {
            return Context.ClubMember.Where(member => member.CharacterId == characterId)
                .Join(
                    Context.Club,
                    member => member.ClubId,
                    club => club.Id,
                    (member, club) => new Tuple<long, string>(club.Id, club.Name)
                ).ToList();
        }

        public Club? CreateClub(Club club) {
            Model.Club model = club!;
            model.Id = 0;
            model.Members = club.Members.Select<ClubMember, Model.ClubMember>(member => member!).ToList();
            model.Members.Add(club.Leader!);

            Context.Club.Add(model);

            // I know this is an extra read, but the conversion logic is complicated.
            return Context.TrySaveChanges() ? GetClub(model.Id) : null;
        }

        public IList<ClubMember> GetClubMembers(long clubId) {
            return Context.ClubMember.Where(member => member.ClubId == clubId)
                .Include(member => member.Character)
                .Join(Context.Account,
                    member => member.Character.AccountId,
                    account => account.Id,
                    (member, account) => new ClubMember(
                        new PlayerInfo(member.Character!, new HomeInfo("", 0, 0, 0, 0), account.Trophy),
                        member.CreationTime.ToEpochSeconds(),
                        member.Character.LastModified.ToEpochSeconds())
                ).ToList();
        }
    }
}
