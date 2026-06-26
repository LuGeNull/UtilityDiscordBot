using Discord.Rest;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;
using UtilsBot.Services;

namespace UtilsBot.Repository;

public class DatabaseRepository : HelperService, IDisposable, IAsyncDisposable
{
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    private BotDbContext _context;

    public DatabaseRepository(BotDbContext context)
    {
        _context = context;
    }

    public async Task AddUserAsync(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        var user = new AllgemeinePerson
        {
            UserId = guildUserId,
            DisplayName = guildUserDisplayName,
            GuildId = guildId
        };
        _context.AllgemeinePersonen.Add(user);
        await SaveChangesAsync();
    }

    public async Task<AllgemeinePerson?> GetUserById(ulong? userId)
    {
        return await _context.AllgemeinePersonen.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<ulong>> GetUserIdsByGuildIdAsync(ulong guildId)
    {
        return await _context.AllgemeinePersonen
            .Where(p => p.GuildId == guildId)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    public async Task<long> GetUserXpPlacementAsync(ulong guildUserId)
    {
        var user = await _context.AllgemeinePersonen.FirstOrDefaultAsync(p => p.UserId == guildUserId);
        if (user == null) return -1;
        return await _context.AllgemeinePersonen
            .CountAsync(p => p.Xp > user.Xp && p.GuildId == user.GuildId) + 1;
    }

    public async Task<List<AllgemeinePerson>> GetTop8UsersByXp(ulong requestGuildId)
    {
        return await _context.AllgemeinePersonen
            .Where(p => p.GuildId == requestGuildId)
            .OrderByDescending(p => p.Xp)
            .Take(8)
            .ToListAsync();
    }

    public async Task<WarEraContract?> GetContractAsync(string contractId)
    {
        return await _context.WarEraContracts.FirstOrDefaultAsync(c => c.ContractId == contractId);
    }

    public async Task AddNotifiedContractAsync(string contractId, decimal perK)
    {
        _context.WarEraContracts.Add(new WarEraContract
        {
            ContractId = contractId,
            NotifiedAt = DateTime.UtcNow,
            LastPerK = perK,
        });
        await SaveChangesAsync();
    }

    public async Task UpdateContractPerKAsync(string contractId, decimal perK)
    {
        var contract = await _context.WarEraContracts.FirstOrDefaultAsync(c => c.ContractId == contractId);
        if (contract != null)
        {
            contract.LastPerK = perK;
            await SaveChangesAsync();
        }
    }

    public async Task AddContractMessageAsync(string contractId, ulong channelId, ulong messageId)
    {
        _context.WarEraContractMessages.Add(new WarEraContractMessage
        {
            ContractId = contractId,
            ChannelId = channelId,
            MessageId = messageId,
        });
        await SaveChangesAsync();
    }

    public async Task<List<WarEraContractMessage>> GetContractMessagesAsync(string contractId)
    {
        return await _context.WarEraContractMessages
            .Where(m => m.ContractId == contractId)
            .ToListAsync();
    }

    public async Task<List<string>> GetAllTrackedContractIdsAsync()
    {
        return await _context.WarEraContracts.Select(c => c.ContractId).ToListAsync();
    }

    public async Task DeleteContractAsync(string contractId)
    {
        var messages = _context.WarEraContractMessages.Where(m => m.ContractId == contractId);
        _context.WarEraContractMessages.RemoveRange(messages);

        var contract = await _context.WarEraContracts.FirstOrDefaultAsync(c => c.ContractId == contractId);
        if (contract != null) _context.WarEraContracts.Remove(contract);

        await SaveChangesAsync();
    }

    public async Task UpsertSubscriptionAsync(ulong guildId, ulong channelId)
    {
        var subscription = await _context.WarEraSubscriptions.FirstOrDefaultAsync(s => s.GuildId == guildId);
        if (subscription == null)
        {
            _context.WarEraSubscriptions.Add(new WarEraSubscription
            {
                GuildId = guildId,
                ChannelId = channelId,
                IsEnabled = true,
                ExcludedTargetCountryCodes = string.Empty
            });
        }
        else
        {
            subscription.ChannelId = channelId;
            subscription.IsEnabled = true;
        }
        await SaveChangesAsync();
    }

    public async Task SetSubscriptionStateAsync(ulong guildId, bool isEnabled, decimal? minimumRate = null, long? maximumDamage = null, bool? includeProContracts = null)
    {
        var subscription = await _context.WarEraSubscriptions.FirstOrDefaultAsync(s => s.GuildId == guildId);
        if (subscription != null)
        {
            subscription.IsEnabled = isEnabled;
            if (minimumRate.HasValue) 
            {
                subscription.MinimumRate = minimumRate.Value;
            }
            else
            {
                subscription.MinimumRate = 0;
            }
            if (maximumDamage.HasValue)
            {
                subscription.MaximumDamage = maximumDamage.Value;
            }
            else
            {
                subscription.MaximumDamage = 0;
            }
            if (includeProContracts.HasValue)
            {
                subscription.IncludeProContracts = includeProContracts.Value;
            }
            else
            {
                subscription.IncludeProContracts = false;
            }
            await SaveChangesAsync();
        }
    }

    public async Task<bool> SetExcludedTargetCountryCodesAsync(ulong guildId, string excludedTargetCountryCodes)
    {
        var subscription = await _context.WarEraSubscriptions.FirstOrDefaultAsync(s => s.GuildId == guildId);
        if (subscription == null) return false;

        subscription.ExcludedTargetCountryCodes = excludedTargetCountryCodes;
        await SaveChangesAsync();
        return true;
    }

    public async Task<List<WarEraSubscription>> GetActiveSubscriptionsAsync()
    {
        return await _context.WarEraSubscriptions.Where(s => s.IsEnabled).ToListAsync();
    }

    public async Task<WarEraSubscription?> GetSubscriptionAsync(ulong guildId)
    {
        return await _context.WarEraSubscriptions.FirstOrDefaultAsync(s => s.GuildId == guildId);
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var setting = await _context.BotSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.BotSettings.Add(new BotSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await SaveChangesAsync();
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var setting = await _context.BotSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
       await _context.DisposeAsync();
    }
}
