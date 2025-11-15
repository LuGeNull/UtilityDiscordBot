using Discord.Rest;
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
        _context.AllgemeinePerson.Add(user);
        await SaveChangesAsync();
    }

    public async Task<AllgemeinePerson?> GetUserById(ulong? userId)
    {
        return await _context.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<ulong>> GetUserIdsByGuildIdAsync(ulong guildId)
    {
        return await _context.AllgemeinePerson
            .Where(p => p.GuildId == guildId)
            .Select(p => p.UserId)
            .ToListAsync();
    }
    
    public async Task<List<ulong>> GetActiveRoleIdsByGuildIdAsync(ulong guildId)
    {
        return await _context.AllgemeinePerson
            .Where(p => p.GuildId == guildId && p.RoleId != 0ul).Select(p=> p.RoleId).Distinct()
            .ToListAsync();
    }
    

    public async Task<long> HolePlatzDesUsersBeiXpAsync(ulong guildUserId)
    {
        var user = await _context.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == guildUserId);
        if (user == null) return -1;
        return await _context.AllgemeinePerson
            .CountAsync(p => p.Xp > user.Xp && p.GuildId == user.GuildId) + 1;
    }

    public async Task<List<AllgemeinePerson>> HoleTop8PersonenNachXpAsync(ulong requestGuildId)
    {
        return await _context.AllgemeinePerson
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

    public async Task<Role?> GetRoleAsync(int userLevel, ulong guildId)
    {
        return await _context.Rollen.FirstOrDefaultAsync(r => r.Level == userLevel && r.GuildId == guildId);
    }

    public async Task<Role> AddRoleAsync(ulong roleId, ulong channelId, int level, ulong guildId)
    {
        var newRole = new Role
        {
            Name = $"Level {level}",
            Id = roleId,
            ChannelId = channelId,
            GuildId = guildId,
            Level = level
        };
        await _context.Rollen.AddAsync(newRole);
        return newRole; 
    }


    public async Task<List<ulong>> GetInactiveRoleIds(List<ulong> activeRoleIds)
    {
        return _context.Rollen.Where(r => !activeRoleIds.Contains(r.Id)).Select(r => r.Id).ToList();
    }

    public async Task RemoveInactiveRoles(List<ulong> inactiveRoles)
    {
        var rolesToRemove = _context.Rollen.Where(r => inactiveRoles.Contains(r.Id)).ToList();
        _context.Rollen.RemoveRange(rolesToRemove);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Role>> getAllRoles()
    {
        return await _context.Rollen.ToListAsync();
    }
}
