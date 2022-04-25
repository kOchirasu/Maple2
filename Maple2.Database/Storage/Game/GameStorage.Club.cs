using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;

namespace Maple2.Database.Storage;

public partial class GameStorage {
    public partial class Request {
        public Club GetClub(long clubId) {
            Model.Club model = context.Club.Find(clubId);
            if (model == null) {
                return null;
            }
            
            IList<ClubMember> members = GetClubMembers(clubId);
            Club club = model;
            club.Leader = members.First(member => member.Info.CharacterId == model.LeaderId);
            members.Remove(club.Leader);
            club.Members = members;
            return club;
        }

        public IList<Tuple<long, string>> ListClubs(long characterId) {
            return context.ClubMember.Where(member => member.CharacterId == characterId)
                .Join(context.Club, member => member.ClubId, club => club.Id, 
                    (member, club) => new Tuple<long, string>(club.Id, club.Name)).ToList();
        }
        
        public Club CreateClub(Club club) {
            Model.Club model = club;
            model.Id = 0;
            model.Members = club.Members.Select<ClubMember, Model.ClubMember>(member => member).ToList();
            model.Members.Add(club.Leader);
            
            context.Club.Add(model);
            
            // I know this is an extra read, but the conversion logic is complicated.
            return context.TrySaveChanges() ? GetClub(model.Id) : null;
        }

        public IList<ClubMember> GetClubMembers(long clubId) {
            return context.ClubMember.Where(member => member.ClubId == clubId)
                .Include(member => member.Character)
                .Include(member => member.Character.Account)
                .Select(member => new ClubMember(
                    new CharacterInfo(
                        member.Character.Account.Id,
                        member.Character.Id,
                        member.Character.Name,
                        member.Character.Gender,
                        member.Character.Job,
                        member.Character.Level,
                        member.Character.MapId,
                        member.Character.Profile.Picture,
                        0,
                        0,
                        0,
                        0,
                        member.Character.Account.Trophy),
                    member.CreationTime.ToEpochSeconds(),
                    member.Character.LastModified.ToEpochSeconds(),
                    false)
                ).ToList();
        }
    }
}
