namespace Maple2.Model.Enum;

public enum MeretMarketSection {
    All = 0,
    Premium = 100000,
    RedMeret = 100001,
    Ugc = 110000,
}

public enum MeretMarketSort : byte {
    None = 0,
    MostPopularUgc = 1,
    PriceLowest = 2,
    PriceHighest = 3,
    MostRecent = 4,
    MostPopularPremium = 5,
    TopSeller = 6,
}

public enum MeretMarketItemLabel : byte {
    None = 0,
    New = 1,
    Hot = 2,
    Event = 3,
    Sale = 4,
    Special = 5,
}

public enum MeretMarketCurrencyType : byte {
    Meso = 0,
    Meret = 1,
    RedMeret = 2,
}

public enum MeretMarketCategory {
    None = 0,
    Promo = 10,
    Functional = 40300,
    Lifestyle = 40600,
}

public enum MeretMarketBannerLabel {
    None = 0,
    PinkGift = 1,
    BlueGift = 2,
}
