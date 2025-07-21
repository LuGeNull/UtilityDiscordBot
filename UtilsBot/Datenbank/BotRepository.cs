using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Datenbank;

public class BotRepository : IBotRepository
{
    private readonly BotDbContext _db = new();

    public void SaveChanges()
    {
        _db.SaveChanges();
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

    public void AddUserToInterestedList(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von,
        long bis)
    {
        var willBenachrichtigtWerden = true;
        if (AlleIdsPersonen().Contains(guildUserId))
        {
            var person = HoleAllgemeinePersonMitId(guildUserId);
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

        _db.SaveChanges();
    }

    public List<AllgemeinePerson> PersonenDieBenachrichtigtWerdenWollen(ulong userIdDerBenachrichtigendenPerson,
        string displayNameDerBenachrichtigendenPerson, List<ulong> userImChannel)
    {
        var personen = _db.AllgemeinePerson.Where(p => !userImChannel.Contains(p.UserId)).ToList();
        return personen.Where(a =>
            a.KannUndWilldiePersonBenachrichtigtWerden(userIdDerBenachrichtigendenPerson,
                displayNameDerBenachrichtigendenPerson)).ToList();
    }

    public void AddUser(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        var user = new AllgemeinePerson(guildUserId, guildUserDisplayName, guildId);
        user.WillBenachrichtigungenBekommen = false;
        _db.AllgemeinePerson.Add(user);
        _db.SaveChanges();
    }


    private List<ulong> AlleIdsPersonen()
    {
        return _db.AllgemeinePerson.Select(p => p.UserId).ToList();
    }

    public AllgemeinePerson? HoleAllgemeinePersonMitId(ulong userId)
    {
        return _db.AllgemeinePerson.FirstOrDefault(p => p.UserId == userId);
    }

    public List<AllgemeinePerson?> HoleAllgemeinePersonenMitGuildId(ulong guildId)
    {
        return _db.AllgemeinePerson.Where(p => p.GuildId == guildId).ToList();
    }

    public List<ulong> HoleAllgemeinePersonenIdsMitGuildId(ulong guildId)
    {
        return _db.AllgemeinePerson.Where(p => p.GuildId == guildId).Select(p => p.UserId).ToList();
    }

    public AllgemeinePerson HoleUserMitId(ulong guildUserId)
    {
        return _db.AllgemeinePerson.FirstOrDefault(p => p.UserId == guildUserId);
    }

    public long HolePlatzDesUsersBeiXp(ulong guildUserId)
    {
        return _db.AllgemeinePerson.OrderByDescending(p => p.Xp).ToList().FindIndex(p => p.UserId == guildUserId) + 1;
    }
}