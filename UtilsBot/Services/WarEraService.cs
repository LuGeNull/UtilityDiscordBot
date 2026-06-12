using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using UtilsBot.Domain;

namespace UtilsBot.Services;

public class WarEraService
{
    private const string BaseUrl = "https://api2.warera.io/trpc";
    private static readonly TimeSpan LookupTtl = TimeSpan.FromMinutes(30);

    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    private static readonly TimeSpan BattleTtl = TimeSpan.FromSeconds(60);

    private readonly Dictionary<string, WarEraCountry> _countries = new();
    private readonly Dictionary<string, string> _regionNames = new();
    private readonly ConcurrentDictionary<string, (WarEraBattle battle, DateTime fetchedAt)> _battles = new();
    private readonly ConcurrentDictionary<string, (WarEraRound round, DateTime fetchedAt)> _rounds = new();

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTime _lookupTablesLoadedAt = DateTime.MinValue;

    public WarEraService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("UtilityDiscordBot/1.0 (+warera-monitor)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task<List<AuctionItem>> GetNewContractsAsync()
    {
        var apiKey = ApplicationState.WarEraApiKey;
        if (string.IsNullOrEmpty(apiKey)) return new List<AuctionItem>();

        await EnsureLookupTablesAsync(apiKey);

        var auctionResp = await PostAsync<TrpcResponse<AuctionData>>(
            "mercenaryContractAuction.getPaginatedAuctions",
            new { status = "active", limit = 50 },
            apiKey);

        var items = auctionResp?.result?.data?.items;
        if (items == null) return null;

        await Task.WhenAll(items.Select(item => EnrichAsync(item, apiKey)));
        return items;
    }

    private async Task EnrichAsync(AuctionItem item, string apiKey)
    {
        item.countryName = ResolveCountry(item.country);

        if (string.IsNullOrEmpty(item.battle))
        {
            item.battleName = "Unknown";
            return;
        }

        var battle = await GetBattleAsync(item.battle, apiKey);
        item.battleName = BuildBattleName(battle, item.country, item.forCountrySide, item.battle);
        item.battleProgress = BuildBattleProgress(battle);

        WarEraRound round = null;
        if (!string.IsNullOrEmpty(battle?.currentRound))
            round = await GetRoundAsync(battle.currentRound, apiKey);
        item.roundPoints = BuildRoundPoints(battle, round);
    }

    // The current round's score: each side accumulates points, first to the threshold wins the round.
    private string BuildRoundPoints(WarEraBattle battle, WarEraRound round)
    {
        if (round == null) return null;

        var attacker = ResolveCountry(round.attacker?.country ?? battle?.attacker?.country);
        var defender = ResolveCountry(round.defender?.country ?? battle?.defender?.country);
        var attackerPoints = round.attacker?.points ?? 0;
        var defenderPoints = round.defender?.points ?? 0;
        var roundLabel = round.number > 0 ? $"Round {round.number}" : "Current round";

        return $"{roundLabel} — {attacker} `{attackerPoints}` – `{defenderPoints}` {defender}";
    }

    // Battles are first-to-roundsToWin (e.g. best of 3). Shows the round score plus how many
    // more rounds remain in the best case (one side dominates) and worst case (it goes the distance).
    private string BuildBattleProgress(WarEraBattle battle)
    {
        if (battle?.roundsToWin is not > 0) return null;

        var roundsToWin = battle.roundsToWin;
        var attackerWon = battle.attacker?.wonRoundsCount ?? 0;
        var defenderWon = battle.defender?.wonRoundsCount ?? 0;

        var attacker = ResolveCountry(battle.attacker?.country);
        var defender = ResolveCountry(battle.defender?.country);

        var minLeft = Math.Max(1, roundsToWin - Math.Max(attackerWon, defenderWon));
        var maxLeft = Math.Max(minLeft, (roundsToWin * 2 - 1) - (attackerWon + defenderWon));
        var leftText = minLeft == maxLeft
            ? $"{minLeft} round left"
            : $"{minLeft}–{maxLeft} rounds left";

        return $"{attacker} `{attackerWon}–{defenderWon}` {defender}  · {leftText}";
    }

    private string BuildBattleName(WarEraBattle battle, string issuerCountryId, string contractSide, string fallbackId)
    {
        if (battle == null) return fallbackId;

        var attacker = ResolveCountry(battle.attacker?.country);
        var defender = ResolveCountry(battle.defender?.country);
        var issuer = ResolveCountry(issuerCountryId);
        var sideLabel = string.IsNullOrEmpty(contractSide) ? "?" : contractSide;

        return $"{issuer} => {GetOpponent(attacker, defender, issuer)}";
    }

    private string GetOpponent(string attacker, string defender, string issuer)
    {
        if (attacker == issuer)
        {
            return defender;
        }
        
        return attacker;
    }

    private string ResolveCountry(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Unknown";
        if (!_countries.TryGetValue(id, out var c)) return id;
        var flag = CodeToFlag(c.code);
        return string.IsNullOrEmpty(flag) ? c.name : $"{flag} {c.name}";
    }

    private string ResolveRegion(string id) =>
        !string.IsNullOrEmpty(id) && _regionNames.TryGetValue(id, out var n) ? n : (id ?? "Unknown");

    public static string CodeToFlag(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 2) return "";
        var c0 = char.ToUpperInvariant(code[0]);
        var c1 = char.ToUpperInvariant(code[1]);
        if (c0 < 'A' || c0 > 'Z' || c1 < 'A' || c1 > 'Z') return "";
        return char.ConvertFromUtf32(0x1F1E6 + (c0 - 'A')) + char.ConvertFromUtf32(0x1F1E6 + (c1 - 'A'));
    }

    private async Task<WarEraBattle> GetBattleAsync(string battleId, string apiKey)
    {
        var hasCached = _battles.TryGetValue(battleId, out var cached);
        // Names are static, but round scores change mid-battle, so re-fetch after a short TTL.
        if (hasCached && DateTime.UtcNow - cached.fetchedAt < BattleTtl) return cached.battle;

        var resp = await PostAsync<TrpcResponse<WarEraBattle>>(
            "battle.getById",
            new { battleId },
            apiKey);

        var battle = resp?.result?.data;
        if (battle != null) _battles[battleId] = (battle, DateTime.UtcNow);
        // On fetch failure, fall back to the stale snapshot rather than losing the name.
        return battle ?? (hasCached ? cached.battle : null);
    }

    private async Task<WarEraRound> GetRoundAsync(string roundId, string apiKey)
    {
        var hasCached = _rounds.TryGetValue(roundId, out var cached);
        // Points change with every hit, so keep the TTL short.
        if (hasCached && DateTime.UtcNow - cached.fetchedAt < BattleTtl) return cached.round;

        var resp = await PostAsync<TrpcResponse<WarEraRound>>(
            "round.getById",
            new { roundId },
            apiKey);

        var round = resp?.result?.data;
        if (round != null) _rounds[roundId] = (round, DateTime.UtcNow);
        return round ?? (hasCached ? cached.round : null);
    }

    private async Task EnsureLookupTablesAsync(string apiKey)
    {
        if (DateTime.UtcNow - _lookupTablesLoadedAt < LookupTtl) return;

        await _refreshLock.WaitAsync();
        try
        {
            if (DateTime.UtcNow - _lookupTablesLoadedAt < LookupTtl) return;

            var countriesTask = PostAsync<TrpcResponse<List<WarEraCountry>>>(
                "country.getAllCountries", new { }, apiKey);
            var regionsTask = PostAsync<TrpcResponse<Dictionary<string, WarEraRegion>>>(
                "region.getRegionsObject", new { }, apiKey);

            await Task.WhenAll(countriesTask, regionsTask);

            var countries = countriesTask.Result?.result?.data;
            if (countries != null)
            {
                _countries.Clear();
                foreach (var c in countries)
                {
                    if (!string.IsNullOrEmpty(c?._id) && !string.IsNullOrEmpty(c.name))
                        _countries[c._id] = c;
                }
            }

            var regions = regionsTask.Result?.result?.data;
            if (regions != null)
            {
                _regionNames.Clear();
                foreach (var (id, region) in regions)
                {
                    if (!string.IsNullOrEmpty(region?.name))
                        _regionNames[id] = region.name;
                }
            }

            if (countries != null && regions != null)
                _lookupTablesLoadedAt = DateTime.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<T> PostAsync<T>(string procedure, object input, string apiKey)
    {
        var url = $"{BaseUrl}/{procedure}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(input),
            };
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[WarEraService] {procedure} -> HTTP {(int)response.StatusCode}");
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, _json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WarEraService] {procedure} failed: {ex.Message}");
            return default;
        }
    }
}
