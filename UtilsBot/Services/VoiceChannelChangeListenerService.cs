using Discord;
using Discord.WebSocket;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class VoiceChannelChangeListenerService
{
    private Timer _checkTimer;
    public DatabaseRepository _database;
    public IEnumerable<Guild> _guilds;

    public VoiceChannelChangeListenerService(DatabaseRepository database)
    {
        _database = database;
        _checkTimer = new Timer();
        _guilds = new List<Guild>();
    }

    public Task CheckServerChangesAsync(DiscordSocketClient _client)
    {
        _database.LoadData();
        _database.DebugInfo();
        foreach (var guild in _client.Guilds)
        foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 0))
        {
            NeueUserHinzufuegenFallsVorhanden(channel, _client);
            UpdateInfoUser(channel, _client);
        }

        _database.SaveData();

        return Task.CompletedTask;
    }

    public void UpdateInfoUser(SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var lokalePerson = _database.HoleAllgemeinePersonMitId(user.Id);
            if (SollFuerDiePersonEineBenachrichtigungVerschicktWerden(lokalePerson))
            {
                var ZuBenachrichtigendePerson = _database.PersonenDieBenachrichtigtWerdenWollen(user.Id,
                    user.DisplayName, connectedUsers.Select(cu => cu.Id).ToList());
                PersonenBenachrichtigen(ZuBenachrichtigendePerson, client, user.DisplayName, channel.Guild.Name);
            }

            lokalePerson.ZuletztImChannel = DateTime.Now;
            UpdateExp(lokalePerson, user);
        }
    }

    private async Task PersonenBenachrichtigen(List<AllgemeinePerson> zuBenachrichtigendePersonen,
        DiscordSocketClient client, string userDisplayName, string guildName)
    {
        foreach (var zuBenachrichtigendePerson in zuBenachrichtigendePersonen)
        {
            var user = await client.GetUserAsync(zuBenachrichtigendePerson.UserId);
            var sendTask =
                await user.SendMessageAsync($"Auf dem Server {guildName} ist {userDisplayName} beigetreten!");
            NachrichtenLöschenNachXMinuten(sendTask);
        }
    }

    private static void NachrichtenLöschenNachXMinuten(IUserMessage sendTask)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }

    private bool SollFuerDiePersonEineBenachrichtigungVerschicktWerden(AllgemeinePerson? lokalePerson)
    {
        if (ApplicationState.TestMode) return true;
        if (lokalePerson.ZuletztImChannel.AddMinutes(ApplicationState.UserXMinutenAusDemChannel) <
            DateTime.Now) return true;
        return false;
    }

    private void UpdateExp(AllgemeinePerson lokalePerson, SocketGuildUser user)
    {
        if (user.IsStreaming && !user.IsSelfMuted && !user.IsSelfDeafened)
        {
            lokalePerson.Xp += ApplicationState.BaseXp * ApplicationState.StreamMultiplier;
            Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp * ApplicationState.StreamMultiplier} XP bekommen");
        }
        else
        {
            if (user.IsSelfMuted && user.IsSelfDeafened)
            {
                lokalePerson.Xp += ApplicationState.FullMuteBaseXp;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.FullMuteBaseXp} XP bekommen");
            }
            else if (user.IsSelfMuted && !user.IsSelfDeafened)
            {
                lokalePerson.Xp += ApplicationState.OnlyMuteBaseXp;
                Console.WriteLine($"{lokalePerson.DisplayName} hat { ApplicationState.OnlyMuteBaseXp} XP bekommen");
            }
            else
            {
                lokalePerson.Xp += ApplicationState.BaseXp;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp} XP bekommen");
            }
        }
       
    }

    private void NeueUserHinzufuegenFallsVorhanden(SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(_database.HoleAllgemeinePersonenIdsMitGuildId(channel.Guild.Id));
        if (neueUser.Any())
            foreach (var user in neueUser)
            {
                var userInQuestion = channel.ConnectedUsers.First(u => u.Id == user);
                _database.AddUser(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
    }

    public void StartPeriodicCheck(DiscordSocketClient client)
    {
        CheckServerChangesAsync(client);
        _checkTimer = new Timer(ApplicationState.TickProXSekunden);
        _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }


    public void AddUserToInterestedPeopleList(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von,
        long bis)
    {
        _database.AddUserToInterestedList(guildUserId, guildUserDisplayName, guildId, von, bis);
        _database.SaveData();
    }
}