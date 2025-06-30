using System.Data.SqlTypes;
using System.Text.Json;
using Discord.WebSocket;

namespace UtilsBot.Repository;

public class DatabaseRepository
{
    private HashSet<InterestedPeople> _interestedPeopleInVoiceChannelChanges = new();

    public void AddInterestedPeople(InterestedPeople interestedPeople)
    {
        _interestedPeopleInVoiceChannelChanges.Add(interestedPeople);
        SaveData();
    }

    public IEnumerable<InterestedPeople> HoleInteressiertePersoneDieNichtImVoiceChannelSind(ulong guildId, List<SocketGuildUser> alleUserDieBereitsImVoiceChannelSind)
    {
        LoadData();
        return _interestedPeopleInVoiceChannelChanges.Where(p =>
            p.GuildId == guildId &&
            IstDerBenachrichtigungsZeitraumInDemDesBenutzers(
                p)); //&& !alleUserDieBereitsImVoiceChannelSind.Select(u => u.Id).Contains(p.UserId) );
    }

    private bool IstDerBenachrichtigungsZeitraumInDemDesBenutzers(InterestedPeople interestedPerson)
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
            _interestedPeopleInVoiceChannelChanges = JsonSerializer.Deserialize<HashSet<InterestedPeople>>(
                File.ReadAllText("interestedPeople.json")) ?? new();
        }
    }

    public void SaveData()
    {
        File.WriteAllText("interestedPeople.json", JsonSerializer.Serialize(_interestedPeopleInVoiceChannelChanges));
    }
    
    
}