using Discord.Rest;
using UtilsBot.Datenbank;
using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;
using UtilsBot.Domain.ValueObjects;
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
            Id = Guid.NewGuid(),
            UserId = guildUserId,
            DisplayName = guildUserDisplayName,
            GuildId = guildId
        };
        _context.AllgemeinePerson.Add(user);
        await SaveChangesAsync();
    }

    public async Task<AllgemeinePerson?> GetUserById(ulong? userId, ulong? guildId)
    {
        return await _context.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == userId && p.GuildId == guildId);
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

    public async Task AddBetAsync(Bet bet)
    {
        await _context.Bet.AddAsync(bet);
        await _context.SaveChangesAsync();
    }
    
    public async Task<Bet?> GetBet(ulong? messageId)
    {
        return await _context.Bet.FirstOrDefaultAsync(b =>
            b.MessageId == messageId);
    }
    
    public bool DoesTheUserHaveEnoughGoldForRequest(ulong? userId, ulong? guildId, long betAmount)
    {
        var person = _context.AllgemeinePerson.FirstOrDefault(p => p.UserId == userId && p.GuildId == guildId);
        if (person == null)
        {
            return false;
        }
        var gold = person.Gold;
        if (gold >= betAmount)
        {
            return true;
        }

        return false;
    }

    public Task<Bet?> GetBetAndPlacementsByMessageId(ulong? messageId)
    {
       return _context.Bet.Include(b => b.Placements).FirstOrDefaultAsync(b => b.MessageId == messageId);
    }

    public async Task<bool> IsThisUserCreatorOfBet(ulong userId, ulong messageId)
    {
        var bet =  await _context.Bet.FirstOrDefaultAsync(b => b.MessageId == messageId);
        if(bet == null)
        {
            return false;
        }

        if (bet.UserIdStartedBet == userId)
        {
            return true;
        }

        return false;
    }

    public async Task CloseAcceptingBets(ulong messageId)
    {
        var bet = _context.Bet.First(b => b.MessageId == messageId);
        if (bet == null)
        {
            return;
        }

        bet.EndedAt = DateTime.Now.AddMinutes(-1);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> AddUserToBet(ulong? userId, long einsatz, ulong? messageIdBet, BetSide requestOption)
    {
        var person = _context.AllgemeinePerson.First(u => u.UserId == userId);
        var bet =  _context.Bet.Include(b => b.Placements).FirstOrDefault(b => b.MessageId == messageIdBet);
        var wettSeite = requestOption == BetSide.Yes;
        //Existiert schon wette ?
        var existiertGegenwetteVomUser = bet.Placements.FirstOrDefault(b => b.UserId == userId && b.Site == !wettSeite) ;
        if (existiertGegenwetteVomUser != null)
        {
            return false;
        }
        var existiertWetteVomUser = bet.Placements.FirstOrDefault(b => b.UserId == userId && b.Site == wettSeite);
        
        if (existiertWetteVomUser != null)
        {
            existiertWetteVomUser.betAmount += einsatz;
        }
        else
        {
            if (bet.Placements == null)
            {
                bet.Placements = new List<BetPlacements>();
            }
            
            _context.Placements.Add(new BetPlacements
            {
                Id = Guid.NewGuid(),
                DisplayName = person.DisplayName,
                UserId = userId,
                betAmount = einsatz,
                Site = requestOption == BetSide.Yes,
                BetId = bet.Id,
                Bet = bet
            });
        }
        
        person.Gold -= einsatz;
        await _context.SaveChangesAsync();
        return true;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
       await _context.DisposeAsync();
    }

    public async Task<Role?> GetRoleAsync(int userLevel)
    {
        return await _context.Rollen.FirstOrDefaultAsync(r => r.Level == userLevel);
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

    public async Task<List<Role>> GetAllRoles()
    {
        return await _context.Rollen.ToListAsync();
    }

    public async Task DeleteAllRoles(List<Role> roles)
    {
        _context.Rollen.RemoveRange(roles);
    }
}
