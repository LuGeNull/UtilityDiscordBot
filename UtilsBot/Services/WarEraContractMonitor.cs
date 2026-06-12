using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class WarEraContractMonitor
{
    private Timer _checkTimer = new();
    private readonly WarEraService _warEraService;
    private DiscordSocketClient _client;

    public WarEraContractMonitor(WarEraService warEraService)
    {
        _warEraService = warEraService;
    }

    public async Task StartMonitoring(DiscordSocketClient client)
    {
        _client = client;
        _checkTimer.Interval = ApplicationState.WarEraCheckIntervalSeconds * 1000;
        _checkTimer.Elapsed += async (sender, e) => await CheckForNewContracts();
        _checkTimer.AutoReset = true;
        _checkTimer.Enabled = true;

        // Initial check
        await CheckForNewContracts();
    }

    private async Task CheckForNewContracts()
    {
        try
        {
            await using var db = new DatabaseRepository(new BotDbContext());
            var activeSubscriptions = await db.GetActiveSubscriptionsAsync();
            
            if (!activeSubscriptions.Any())
            {
                return;
            }

            var items = await _warEraService.GetNewContractsAsync();
            if (items == null)
            {
                Console.WriteLine("[WarEraMonitor] Failed to fetch contracts (API error or missing key).");
                return;
            }

            if (!items.Any())
            {
                return;
            }

            foreach (var item in items)
            {
                var existing = await db.GetContractAsync(item.id);
                if (existing == null)
                {
                    Console.WriteLine($"[WarEraMonitor] New contract detected: {item.id}. Notifying {activeSubscriptions.Count} subscriptions.");
                    await db.AddNotifiedContractAsync(item.id, item.currentPerK);
                    await NotifyContract(item, activeSubscriptions, db);
                }
                else if (existing.LastPerK != item.currentPerK)
                {
                    Console.WriteLine($"[WarEraMonitor] Rate change for {item.id}: {existing.LastPerK} -> {item.currentPerK}. Editing existing messages.");
                    await EditContractMessages(item, db);
                    await db.UpdateContractPerKAsync(item.id, item.currentPerK);
                }
            }

            await CleanupEndedContracts(items.Select(i => i.id).ToHashSet(), db);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WarEraMonitor] Critical error in CheckForNewContracts: {ex.Message}");
        }
    }

    private async Task NotifyContract(AuctionItem item, List<WarEraSubscription> subscriptions, DatabaseRepository db)
    {
        var embed = BuildContractEmbed(item);

        foreach (var sub in subscriptions)
        {
            if (item.professionalsOnly && !sub.IncludeProContracts) continue;
            if (item.currentPerK < sub.MinimumRate) continue;
            if (sub.MaximumDamage > 0 && item.minimumDamage > sub.MaximumDamage) continue;

            try
            {
                var channel = await _client.GetChannelAsync(sub.ChannelId) as IMessageChannel;
                if (channel == null) continue;

                var sent = await channel.SendMessageAsync(embed: embed);
                await db.AddContractMessageAsync(item.id, sub.ChannelId, sent.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying contract {item.id} to channel {sub.ChannelId} (Guild {sub.GuildId}): {ex.Message}");
            }
        }
    }

    private async Task CleanupEndedContracts(HashSet<string> activeIds, DatabaseRepository db)
    {
        var tracked = await db.GetAllTrackedContractIdsAsync();
        foreach (var id in tracked)
        {
            if (activeIds.Contains(id)) continue;

            Console.WriteLine($"[WarEraMonitor] Contract {id} no longer active. Deleting embeds and DB rows.");
            var messages = await db.GetContractMessagesAsync(id);
            foreach (var m in messages)
            {
                try
                {
                    var channel = await _client.GetChannelAsync(m.ChannelId) as IMessageChannel;
                    if (channel == null) continue;
                    await channel.DeleteMessageAsync(m.MessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting message {m.MessageId} in channel {m.ChannelId} for contract {id}: {ex.Message}");
                }
            }

            await db.DeleteContractAsync(id);
        }
    }

    private async Task EditContractMessages(AuctionItem item, DatabaseRepository db)
    {
        var embed = BuildContractEmbed(item);
        var messages = await db.GetContractMessagesAsync(item.id);

        foreach (var m in messages)
        {
            try
            {
                var channel = await _client.GetChannelAsync(m.ChannelId) as IMessageChannel;
                if (channel == null) continue;

                await channel.ModifyMessageAsync(m.MessageId, props => props.Embed = embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error editing contract {item.id} message {m.MessageId} in channel {m.ChannelId}: {ex.Message}");
            }
        }
    }

    public async Task<string> SendTestNotificationAsync(IMessageChannel channel)
    {
        var items = await _warEraService.GetNewContractsAsync();
        if (items == null) return "API call failed (check key / network).";
        if (!items.Any()) return "No active contracts right now.";

        var item = items[0];
        await channel.SendMessageAsync(embed: BuildContractEmbed(item));
        return $"Sent test notification for contract `{item.id}` (DB not updated).";
    }

    private static Embed BuildContractEmbed(AuctionItem item)
    {
        var title = item.battleName ?? item.battle ?? "Unknown Battle";
        if (item.professionalsOnly) title = $"🎖 {title}";

        var expiresUnix = ((DateTimeOffset)DateTime.SpecifyKind(item.expiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds();

        var scope = item.roundNumber.HasValue ? $"Round {item.roundNumber.Value}" : "Whole battle";

        var descLines = new List<string>
        {
            $"** Issued by:**  {item.countryName}",
            $"**Scope:**  {scope}",
        };
        if (!string.IsNullOrEmpty(item.battleProgress))
            descLines.Add($"**Rounds:**  {item.battleProgress}");
        if (!string.IsNullOrEmpty(item.roundPoints))
            descLines.Add($"**Points:**  {item.roundPoints}");
        descLines.Add($"**Expires:**  <t:{expiresUnix}:R>");

        return new EmbedBuilder()
            .WithTitle(title)
            .WithUrl($"https://warera.io/mercenaries/auctions/{item.id}")
            .WithDescription(string.Join("\n", descLines))
            .AddField("Budget", $"`{item.budget:0.##}` BTC", true)
            .AddField("Payout", $"`{item.currentPayout:0.##}` BTC", true)
            .AddField("Min Damage", $"`{FormatShort(item.minimumDamage)}`", true)
            .AddField("Current Rate", $"`{item.currentPerK:0.###}` / 1k dmg", true)
            .AddField("Initial Rate", $"`{item.initialPerK:0.###}` / 1k dmg", true)
            .AddField("​", "​", true)
            .WithColor(new Color(0x2563EB))
            .WithFooter($"ID  •  {item.id}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    private static string FormatShort(long value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000m:0.##}M";
        if (value >= 1_000) return $"{value / 1_000m:0.##}k";
        return value.ToString();
    }
}
