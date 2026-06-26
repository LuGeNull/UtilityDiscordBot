using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain;

public class WarEraContract
{
    [Key]
    public string ContractId { get; set; }
    public DateTime NotifiedAt { get; set; }
    public decimal LastPerK { get; set; }
}

public class WarEraContractMessage
{
    public string ContractId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
}

public class TrpcResponse<T>
{
    public TrpcResult<T> result { get; set; }
}

public class TrpcResult<T>
{
    public T data { get; set; }
}

public class AuctionData
{
    public List<AuctionItem> items { get; set; }
    public string nextCursor { get; set; }
}

public class AuctionItem
{
    public string _id { get; set; }
    public string id => _id;
    public string country { get; set; }
    public string battle { get; set; }
    public string forCountrySide { get; set; }
    public decimal budget { get; set; }
    public decimal currentPayout { get; set; }
    public decimal initialPerK { get; set; }
    public decimal currentPerK { get; set; }
    public long minimumDamage { get; set; }
    public string status { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime expiresAt { get; set; }
    public bool professionalsOnly { get; set; }
    public int? roundNumber { get; set; }
    public List<Bid> bids { get; set; } = new();

    public string countryName { get; set; }
    public string targetCountryCode { get; set; } = string.Empty;
    public string battleName { get; set; }
    public string battleProgress { get; set; }
    public string roundPoints { get; set; }
}

public class Bid
{
    public string mu { get; set; }
    public string user { get; set; }
    public decimal perK { get; set; }
    public decimal payout { get; set; }
    public DateTime bidAt { get; set; }
}

public class WarEraCountry
{
    public string _id { get; set; }
    public string name { get; set; }
    public string code { get; set; }
}

public class WarEraRegion
{
    public string _id { get; set; }
    public string name { get; set; }
    public string country { get; set; }
}

public class WarEraBattle
{
    public string _id { get; set; }
    public int roundsToWin { get; set; }
    public string currentRound { get; set; }
    public BattleSide defender { get; set; }
    public BattleSide attacker { get; set; }
}

public class BattleSide
{
    public string region { get; set; }
    public string country { get; set; }
    public int wonRoundsCount { get; set; }
    public long damages { get; set; }
}

public class WarEraRound
{
    public string _id { get; set; }
    public int number { get; set; }
    public bool isActive { get; set; }
    public RoundSide defender { get; set; }
    public RoundSide attacker { get; set; }
}

public class RoundSide
{
    public string country { get; set; }
    public long points { get; set; }
    public long damages { get; set; }
}

public class WarEraSubscription
{
    [Key]
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public bool IsEnabled { get; set; }
    public decimal MinimumRate { get; set; }
    public long MaximumDamage { get; set; }
    public bool IncludeProContracts { get; set; }
    public string ExcludedTargetCountryCodes { get; set; } = string.Empty;
}

public class BotSetting
{
    [Key]
    public string Key { get; set; }
    public string Value { get; set; }
}
