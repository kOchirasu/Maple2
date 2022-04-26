using System.Collections.Generic;
using Maple2.Database.Model;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Account = Maple2.Database.Model.Account;
using Character = Maple2.Database.Model.Character;
using Club = Maple2.Database.Model.Club;
using ClubMember = Maple2.Database.Model.ClubMember;

namespace Maple2.Database.Context;

public sealed class InitializationContext : Ms2Context {
    public InitializationContext(DbContextOptions options) : base(options) {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public bool Initialize() {
        bool created = Database.EnsureCreated();
        if (!created) {
            return false;
        }

        Database.ExecuteSqlRaw("ALTER TABLE account AUTO_INCREMENT = 10000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `character` AUTO_INCREMENT = 20000000000");
        Database.ExecuteSqlRaw("ALTER TABLE club AUTO_INCREMENT = 30000000000");
        
        // item is the entity that could grow the most, so put it last
        Database.ExecuteSqlRaw("ALTER TABLE item AUTO_INCREMENT = 1000000000000");

        TestTables();

        return true;
    }

    public void TestTables() {
        var account = new Account {
            Username = "Username"
        };
        Account.Add(account);
        SaveChanges();
        
        var character = new Character {
            AccountId = account.Id,
            Name = "First",
            Experience = new Experience {
                Exp = 1000000,
                Mastery = new Mastery {
                    Fishing = 100
                }
            },
            Profile = new Profile(),
            Cooldown = new Cooldown(),
        };
        Character.Add(character);
        var character2 = new Character {
            AccountId = account.Id,
            Name = "Second",
            Experience = new Experience(),
            Profile = new Profile(),
            Cooldown = new Cooldown(),
        };
        Character.Add(character2);
        SaveChanges();

        var club = new Club();
        club.Name = "Club";
        club.LeaderId = character.Id;
        club.Members = new List<ClubMember>();
        club.Members.Add(new ClubMember {
            CharacterId = character.Id,
        });
        club.Members.Add(new ClubMember {
            CharacterId = character2.Id,
        });
        Club.Add(club);
        SaveChanges();
    }
}