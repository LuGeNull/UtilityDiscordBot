using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Datenbank;

public class BotRepository : IBotRepository
{
    private readonly BotDbContext _db = new();

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task DebugInfoAsync()
    {
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        foreach (var person in await _db.AllgemeinePerson.ToListAsync())
            Console.WriteLine(
                $"UserId: {person.UserId}, DisplayName: {person.DisplayName}, GuildId: {person.GuildId}, WillBenachrichtigtWerden: {person.WillBenachrichtigungenBekommen}, Von: {person.BenachrichtigenZeitVon}, Bis: {person.BenachrichtigenZeitBis} , Zuletzt Online {person.ZuletztImChannel}, xp {person.Xp}");
    }

    public async Task AddUserToInterestedListAsync(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von,
        long bis)
    {
        var willBenachrichtigtWerden = true;
        if ((await AlleIdsPersonen()).Contains(guildUserId))
        {
            var person = await HoleAllgemeinePersonMitIdAsync(guildUserId);
            person.WillBenachrichtigungenBekommen = willBenachrichtigtWerden;
            person.BenachrichtigenZeitVon = von;
            person.BenachrichtigenZeitBis = bis;
        }
        else
        {
            var person = new AllgemeinePerson(guildUserId, guildUserDisplayName, guildId);
            person.WillBenachrichtigungenBekommen = willBenachrichtigtWerden;
            person.BenachrichtigenZeitVon = von;
            person.BenachrichtigenZeitBis = bis;
            _db.AllgemeinePerson.Add(person);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<AllgemeinePerson>> PersonenDieBenachrichtigtWerdenWollenAsync(
        ulong userIdDerBenachrichtigendenPerson,
        string displayNameDerBenachrichtigendenPerson, List<ulong> userImChannel)
    {
        var personen = await _db.AllgemeinePerson.Where(p => !userImChannel.Contains(p.UserId)).ToListAsync();
        return personen.Where(a =>
            a.KannUndWilldiePersonBenachrichtigtWerden(userIdDerBenachrichtigendenPerson,
                displayNameDerBenachrichtigendenPerson)).ToList();
    }

    public async Task AddUserAsync(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        var user = new AllgemeinePerson(guildUserId, guildUserDisplayName, guildId);
        user.WillBenachrichtigungenBekommen = false;
        await _db.AllgemeinePerson.AddAsync(user);
        await _db.SaveChangesAsync();
    }


    private async Task<List<ulong>> AlleIdsPersonen()
    {
        return await _db.AllgemeinePerson.Select(p => p.UserId).ToListAsync();
    }

    public async Task<AllgemeinePerson?> HoleAllgemeinePersonMitIdAsync(ulong userId)
    {
        return await _db.AllgemeinePerson.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<ulong>> HoleAllgemeinePersonenIdsMitGuildIdAsync(ulong guildId)
    {
        return await _db.AllgemeinePerson.Where(p => p.GuildId == guildId).Select(p => p.UserId).ToListAsync();
    }

    public async Task<AllgemeinePerson> HoleUserMitIdAsync(ulong guildUserId)
    {
        return await _db.AllgemeinePerson.FirstAsync(p => p.UserId == guildUserId);
    }

    public async Task<long> HolePlatzDesUsersBeiXpAsync(ulong guildUserId)
    {
        var list = await _db.AllgemeinePerson.OrderByDescending(p => p.Xp).ToListAsync();
        return list.FindIndex(p => p.UserId == guildUserId) + 1;
    }
}