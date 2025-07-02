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
        if (_interestedPeopleInVoiceChannelChanges.Select(i => i.UserId).Contains(interestedPerson.UserId))
        {
            _interestedPeopleInVoiceChannelChanges.RemoveWhere(e => e.UserId == interestedPerson.UserId);
            
        }
        _interestedPeopleInVoiceChannelChanges.Add(interestedPerson);
        SaveData();
    }

    public IEnumerable<InterestedPerson> HoleInteressiertePersoneDieNichtImVoiceChannelSind(ulong guildId, List<SocketGuildUser> alleUserDieBereitsImVoiceChannelSind)
    {
        LoadData();
        if (ApplicationState.TestMode)
        {
            return _interestedPeopleInVoiceChannelChanges.Where(p =>
                p.GuildId == guildId &&
                IstDerBenachrichtigungsZeitraumInDemDesBenutzers(
                    p));
        }
        
        return _interestedPeopleInVoiceChannelChanges.Where(p =>
            p.GuildId == guildId &&
            IstDerBenachrichtigungsZeitraumInDemDesBenutzers(
                p) && !alleUserDieBereitsImVoiceChannelSind.Select(u => u.Id).Contains(p.UserId) );
    }

    private bool IstDerBenachrichtigungsZeitraumInDemDesBenutzers(InterestedPerson interestedPerson)
    {
        if (interestedPerson.NichtBenachrichtigenZeitVon < DateTime.Now.Hour  && DateTime.Now.Hour <
            interestedPerson.NichtBenachrichtigenZeitBis)
        {
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


    public bool RemoveUserFromList(ulong userId)
    {
        if (_interestedPeopleInVoiceChannelChanges.Select(i => i.UserId).Contains(userId))
        {
            _interestedPeopleInVoiceChannelChanges.RemoveWhere(e => e.UserId == userId);
            SaveData();
            return true;
        }

        return false;
    }
}