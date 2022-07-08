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
    public InitializationContext(DbContextOptions options) : base(options) { }

    public bool Initialize() {
        bool created = Database.EnsureCreated();
        if (!created) {
            return false;
        }

        TestTables();

        Database.ExecuteSqlRaw("ALTER TABLE `account` AUTO_INCREMENT = 10000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `character` AUTO_INCREMENT = 20000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `club` AUTO_INCREMENT = 30000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `buddy` AUTO_INCREMENT = 40000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `ugcmap` AUTO_INCREMENT = 50000000000");

        // potentially large tables
        Database.ExecuteSqlRaw("ALTER TABLE `ugcmap-cube` AUTO_INCREMENT = 1000000000000");
        Database.ExecuteSqlRaw("ALTER TABLE `item` AUTO_INCREMENT = 2000000000000");

        return true;
    }

    private void TestTables() {
        var account = new Account {
            Id = 1,
            Username = "System",
            Trophy = new Trophy(),
            Currency = new AccountCurrency(),
        };
        Account.Add(account);
        SaveChanges();

        var character = new Character {
            AccountId = account.Id,
            Id = 1,
            Name = "System",
            Experience = new Experience {
                Exp = 1000000,
                Mastery = new Mastery {
                    Fishing = 100,
                },
            },
            Profile = new Profile(),
            Cooldown = new Cooldown(),
            Currency = new CharacterCurrency(),
        };
        Character.Add(character);
        var character2 = new Character {
            Id = 2,
            AccountId = account.Id,
            Name = "Admin",
            Experience = new Experience(),
            Profile = new Profile(),
            Cooldown = new Cooldown(),
            Currency = new CharacterCurrency(),
        };
        Character.Add(character2);
        SaveChanges();

        var club = new Club();
        club.Id = 0;
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
