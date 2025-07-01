using System.Data.SqlTypes;
using System.Text.Json;
using Discord.WebSocket;

namespace UtilsBot.Repository;

public class DatabaseRepository
{
    private HashSet<InterestedPerson> _interestedPeopleInVoiceChannelChanges = new();

    public void InterestedPersonGotMessaged(InterestedPerson person)
    {
        person.LetztesMalBenachrichtigt = DateTime.Now;
        SaveData();
    }
    public void AddInterestedPeople(InterestedPerson interestedPerson)
    {
        _interestedPeopleInVoiceChannelChanges.Add(interestedPerson);
        SaveData();
    }

    public IEnumerable<InterestedPerson> HoleInteressiertePersoneDieNichtImVoiceChannelSind(ulong guildId, List<SocketGuildUser> alleUserDieBereitsImVoiceChannelSind)
    {
        LoadData();
        return _interestedPeopleInVoiceChannelChanges.Where(p =>
            p.GuildId == guildId &&
            IstDerBenachrichtigungsZeitraumInDemDesBenutzers(
                p) && !alleUserDieBereitsImVoiceChannelSind.Select(u => u.Id).Contains(p.UserId) );
    }

    private bool IstDerBenachrichtigungsZeitraumInDemDesBenutzers(InterestedPerson interestedPerson)
    {
        if (interestedPerson.NichtBenachrichtigenZeitBis == interestedPerson.ImmerBenachrichtigen)
        {
            Console.WriteLine($"Debug: person {interestedPerson.UserId} will immer benachrichtigt werden");
            return true;
        }

        if (interestedPerson.NichtBenachrichtigenZeitVon < DateTime.Now.Hour  && DateTime.Now.Hour <
            interestedPerson.NichtBenachrichtigenZeitBis)
        {
            Console.WriteLine($"Debug: person {interestedPerson.UserId} will zwischen {interestedPerson.NichtBenachrichtigenZeitVon} und {interestedPerson.NichtBenachrichtigenZeitBis} Uhr benachrichtigt werden");
            return false;
        }

        return true;
    }
    
    private void LoadData()
    {
        if (File.Exists("interestedPeople.json"))
        {
            _interestedPeopleInVoiceChannelChanges = JsonSerializer.Deserialize<HashSet<InterestedPerson>>(
                File.ReadAllText("interestedPeople.json")) ?? new();
        }
    }

    public void SaveData()
    {
        File.WriteAllText("interestedPeople.json", JsonSerializer.Serialize(_interestedPeopleInVoiceChannelChanges));
    }
    
    
}