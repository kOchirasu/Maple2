using System;
using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Game;

public class Player {
    public readonly Account Account;
    public readonly Character Character;

    public Currency Currency { get; init; }
    public Unlock? Unlock { get; init; }

    public Player(Account account, Character character) {
        Account = account;
        Character = character;
    }
}

public class Unlock {
    public DateTime LastModified { get; init; }

    public IDictionary<InventoryType, short> Expand { get; init; } = new Dictionary<InventoryType, short>();

    public readonly ISet<int> Maps = new SortedSet<int>();
    public readonly ISet<int> Taxis = new SortedSet<int>();
    public readonly ISet<int> Titles = new SortedSet<int>();
    public readonly ISet<int> Emotes = new SortedSet<int>();
    public readonly ISet<int> Stamps = new SortedSet<int>();
}

public class Currency {
    public long Meret;
    public long GameMeret;
    public long Meso;
    public long EventMeret;
    public long ValorToken;
    public long Treva;
    public long Rue;
    public long HaviFruit;
    public long ReverseCoin;
    public long MentorToken;
    public long MenteeToken;
    public long StarPoint;
    public long MesoToken;
};
