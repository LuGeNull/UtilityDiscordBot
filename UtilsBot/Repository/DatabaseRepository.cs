using System.Data.SqlTypes;
using System.Text.Json;
using Discord.WebSocket;

namespace UtilsBot.Repository;

public class DatabaseRepository
{
    public List<AllgemeinePerson?> _personen = new();

    public void DebugInfo()
    {
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        Console.WriteLine("\n");
        foreach (var person in _personen)
        {
            if (person != null)
            {
                Console.WriteLine($"UserId: {person.UserId}, DisplayName: {person.DisplayName}, GuildId: {person.GuildId}, WillBenachrichtigtWerden: {person.WillBenachrichtigungenBekommen}, Von: {person.BenachrichtigenZeitVon}, Bis: {person.BenachrichtigenZeitBis} , Zuletzt Online {person.ZuletztImChannel}, xp {person.Xp}");
            }
        }
    }
    public void AddUserToInterestedList(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von, long bis)
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
            _personen.Add(person);
        }
    }

    public List<AllgemeinePerson> PersonenDieBenachrichtigtWerdenWollen(ulong userIdDerBenachrichtigendenPerson, string displayNameDerBenachrichtigendenPerson, List<ulong> userImChannel)
    {
        return _personen.Where(p => !userImChannel.Contains(p.UserId) && p.KannUndWilldiePersonBenachrichtigtWerden(userIdDerBenachrichtigendenPerson,displayNameDerBenachrichtigendenPerson) ).ToList();
    }
    public void AddUser(ulong guildUserId, string guildUserDisplayName, ulong guildId)
    {
        var user = new AllgemeinePerson(guildUserId, guildUserDisplayName, guildId);
        user.WillBenachrichtigungenBekommen = false;
        _personen.Add(user);
    }


    public List<ulong> AlleIdsPersonen()
    {
        return this._personen.Select(p => p.UserId).ToList();
    }

    public AllgemeinePerson? HoleAllgemeinePersonMitId(ulong userId)
    {
        return this._personen.FirstOrDefault(p => p.UserId == userId);
    }
    
    public List<AllgemeinePerson?> HoleAllgemeinePersonenMitGuildId(ulong guildId)
    {
        return this._personen.Where(p => p.GuildId == guildId).ToList();
    }
    
    public List<ulong> HoleAllgemeinePersonenIdsMitGuildId(ulong guildId)
    {
        return this._personen.Where(p => p.GuildId == guildId).Select(a => a.UserId).ToList();
    }

   
    
    public void LoadData()
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "data", "People.json")))
        {
            _personen = JsonSerializer.Deserialize<List<AllgemeinePerson>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "People.json"))) ?? new();
        }
    }
   
    public void SaveData()
    {
        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "data")))
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "data"));
        }
        
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "data", "People.json"), JsonSerializer.Serialize(_personen));
    }

    public long HoleUserXpMitId(ulong guildUserId)
    {
        return _personen.First(p => p.UserId == guildUserId).Xp;
    }
}