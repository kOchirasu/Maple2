using System;
using System.Collections.Generic;

namespace Maple2.Model.Game;

public class Player {
    public readonly Account Account;
    public readonly Character Character;

    public Currency Currency { get; init; }
    public Unlock Unlock { get; init; }

    public Player(Account account, Character character) {
        Account = account;
        Character = character;
    }
}

public class Unlock {
    public DateTime LastModified { get; init; }

    public readonly ISet<int> Maps = new SortedSet<int>();
    public readonly ISet<int> Taxis = new SortedSet<int>();
    public readonly ISet<int> Titles = new SortedSet<int>();
    public readonly ISet<int> Emotes = new SortedSet<int>();
    public readonly ISet<int> Stamps = new SortedSet<int>();
}

public record Currency(
    long Meret,
    long GameMeret,
    long Meso,
    long EventMeret,
    long ValorToken,
    long Treva,
    long Rue,
    long HaviFruit,
    long ReverseCoin,
    long MentorToken,
    long MenteeToken,
    long StarPoint,
    long MesoToken);
