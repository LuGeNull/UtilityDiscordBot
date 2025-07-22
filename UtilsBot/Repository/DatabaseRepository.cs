using UtilsBot.Datenbank;
using Microsoft.EntityFrameworkCore;
namespace UtilsBot.Repository;

public class DatabaseRepository
{
    public async Task SaveChanges(BotDbContext context)
    {
        await context.SaveChangesAsync();
    }

    public void DebugInfo()
    {
        var context = new BotDbContext();
        foreach (var person in context.AllgemeinePerson)
        {
            Console.WriteLine("\n");
            Console.WriteLine(
                $"UserId: {person.UserId}, DisplayName: {person.DisplayName}, GuildId: {person.GuildId}, Zuletzt Online {person.ZuletztImChannel}, xp {person.Xp}");
        }
    }

    public async Task AddUserAsync(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        using var context = new BotDbContext();
        var user = new AllgemeinePerson
        {
            UserId = guildUserId,
            DisplayName = guildUserDisplayName,
            GuildId = guildId
        };
        context.AllgemeinePerson.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<AllgemeinePerson?> HoleAllgemeinePersonMitIdAsync(ulong userId, BotDbContext context)
    {
        return await context.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<ulong>> HoleAllgemeinePersonenIdsMitGuildIdAsync(ulong guildId)
    {
        using var context = new BotDbContext();
        return await context.AllgemeinePerson
            .Where(p => p.GuildId == guildId)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    public async Task<long> HolePlatzDesUsersBeiXpAsync(ulong guildUserId)
    {
        using var context = new BotDbContext();
        var user = await context.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == guildUserId);
        if (user == null) return -1;
        return await context.AllgemeinePerson
            .CountAsync(p => p.Xp > user.Xp && p.GuildId == user.GuildId) + 1;
    }

    public async Task<List<AllgemeinePerson>> HoleTop8PersonenNachXpAsync(ulong requestGuildId)
    {
        using var context = new BotDbContext();
        return await context.AllgemeinePerson
            .Where(p => p.GuildId == requestGuildId)
            .OrderByDescending(p => p.Xp)
            .Take(8)
            .ToListAsync();
    }
}
