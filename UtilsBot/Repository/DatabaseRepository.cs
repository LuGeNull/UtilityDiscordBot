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
    
    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
       await _context.DisposeAsync();
    }
}
