using UtilsBot.Datenbank;
using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;
using UtilsBot.Domain.ValueObjects;

namespace UtilsBot.Repository;

public class DatabaseRepository : IDisposable, IAsyncDisposable
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
   
    public void DebugInfo()
    {
        foreach (var person in _context.AllgemeinePerson)
        {
            Console.WriteLine("\n");
            Console.WriteLine(
                $"UserId: {person.UserId}, DisplayName: {person.DisplayName}, GuildId: {person.GuildId}, Zuletzt Online {person.LastTimeInChannel}, xp {person.Xp}");
        }
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

    public async Task<List<ulong>> GetUsersByGuildId(ulong guildId)
    {
        return await _context.AllgemeinePerson
            .Where(p => p.GuildId == guildId)
            .Select(p => p.UserId)
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
    
    public async Task<List<Bet>> GetBetWhichIsActive(ulong? requestChannelId, DateTime requestZeitJetzt)
    {
       return  _context.Bet.Where(b =>
                b.ChannelId == requestChannelId && requestZeitJetzt > b.StartedAt &&
                requestZeitJetzt < b.EndedAt).ToList();
    }
    
    public async Task<Bet?> GetBet(ulong? messageId)
    {
        return await _context.Bet.FirstOrDefaultAsync(b =>
            b.MessageId == messageId);
    }
    
    public bool HatDerUserGenugXpFuerAnfrage(ulong? userId, long einsatz)
    {
        var person = _context.AllgemeinePerson.FirstOrDefault(p => p.UserId == userId);
        var xp = person.Xp;
        if (xp >= einsatz)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> ExistiertDieWetteMitDerIdBereitsUndIstAktuell(int wettId)
    {
        return _context.Bet.Any(b => b.ReferenzId == wettId && DateTime.Now >= b.StartedAt && DateTime.Now <= b.EndedAt);
    }

    public Task<Bet?> GetBetAndPlacementsByMessageId(ulong? messageId)
    {
       return _context.Bet.Include(b => b.Placements).FirstOrDefaultAsync(b => b.MessageId == messageId);
    }

    public async Task<bool> IstDieserUserErstellerDerWette(ulong userId, ulong messageId)
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

    public async Task WettannahmenSchliessen(ulong messageId)
    {
        var wette = _context.Bet.First(b => b.MessageId == messageId);
        if (wette == null)
        {
            return;
        }

        wette.EndedAt = DateTime.Now.AddMinutes(-1);
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
            existiertWetteVomUser.Einsatz += einsatz;
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
                Einsatz = einsatz,
                Site = requestOption == BetSide.Yes,
                BetId = bet.Id,
                Bet = bet
            });
        }
        
        person.Xp -= einsatz;
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
}
