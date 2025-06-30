using System.Data.SqlTypes;
using System.Text.Json;
using Discord.WebSocket;

namespace UtilsBot.Repository;

public class DatabaseRepository
{
    private HashSet<InterestedPeople> _interestedPeopleInVoiceChannelChanges = new();

    public void AddInterestedPeople(InterestedPeople interestedPeople)
    {
        SaveData();
        _interestedPeopleInVoiceChannelChanges.Add(interestedPeople);
    }

    public IEnumerable<InterestedPeople> GetInterestedPeople(ulong guildId, List<SocketGuildUser> alleUserDieBereitsImVoiceChannelSind)
    {
        LoadData();
        return _interestedPeopleInVoiceChannelChanges.Where(p =>
            p.GuildId == guildId && !alleUserDieBereitsImVoiceChannelSind.Select(u => u.Id).Contains(p.UserId) && IstDerBenachrichtigungsZeitraumInDemDesBenutzers(p));
    }

    private bool IstDerBenachrichtigungsZeitraumInDemDesBenutzers(InterestedPeople interestedPeople)
    {
        if (interestedPeople.NichtBenachrichtigenZeitBis == SqlDateTime.MinValue)
        {
            return true;
        }

        if (interestedPeople.NichtBenachrichtigenZeitVon < DateTime.Now  && DateTime.Now <
            interestedPeople.NichtBenachrichtigenZeitBis)
        {
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