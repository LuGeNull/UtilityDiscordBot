using UtilsBot.Datenbank;

namespace UtilsBot.Repository;

public class DatabaseRepository
{
    public BotDbContext _db;

    public DatabaseRepository()
    {
        _db = new BotDbContext();
    }

    public void SaveChanges()
    {
        _db.SaveChanges();
    }
    public void DebugInfo()
    {
        foreach (var person in _db.AllgemeinePerson)
        {
            Console.WriteLine("\n");
            Console.WriteLine(
                $"UserId: {person.UserId}, DisplayName: {person.DisplayName}, GuildId: {person.GuildId}, Zuletzt Online {person.ZuletztImChannel}, xp {person.Xp}");

        }
    }

    public void AddUser(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        var user = new AllgemeinePerson();
        user.UserId = guildUserId;
        user.DisplayName = guildUserDisplayName;
        user.GuildId = guildId;
        _db.AllgemeinePerson.Add(user);
        _db.SaveChanges();
    }

    public List<ulong> AlleIdsPersonen()
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

    public List<AllgemeinePerson> HoleTop8PersonenNachXp(ulong requestGuildId)
    {
        return _db.AllgemeinePerson.Where(p => p.GuildId == requestGuildId).OrderByDescending(p => p.Xp).Take(8).ToList();
    }
}