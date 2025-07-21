using Discord;
using Discord.WebSocket;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class DiscordServerChangeMonitor
{
    private Timer _checkTimer;
    public DatabaseRepository _database;
    

    public DiscordServerChangeMonitor(DatabaseRepository database)
    {
        _database = database;
        _checkTimer = new Timer();
    }

    public Task CheckServerChangesAsync(DiscordSocketClient _client)
    {
        _database.DebugInfo();
        foreach (var guild in _client.Guilds)
        foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 0))
        {
            NeueUserHinzufuegenFallsVorhanden(channel, _client);
            UpdateInfoUser(channel, _client);
        }
        
        return Task.CompletedTask;
    }

    public void UpdateInfoUser(SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var lokalePerson = _database.HoleAllgemeinePersonMitId(user.Id);
            lokalePerson.ZuletztImChannel = DateTime.Now;
            UpdateExp(lokalePerson, user);
        }
        _database.SaveChanges();
    }

    private static void NachrichtenLöschenNachXMinuten(IUserMessage sendTask)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }

    private void UpdateExp(AllgemeinePerson lokalePerson, SocketGuildUser user)
    {
        
        if (UserIsStreamingAndVideoingAndNotMutedAndDeafended(user))
        {
            var xpToGain = ApplicationState.BaseXp + ApplicationState.StreamAndVideoBonus;
            lokalePerson.Xp += xpToGain;
            lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
            Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus} XP bekommen");
        }
        
        if (UserIsStreamingOrVideoingAndNotMutedOrDeafened(user))
        {
            var xpToGain = ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus;
            lokalePerson.Xp += xpToGain;
            lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
            Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus} XP bekommen");
        }
        else
        {
            if (UserIsFullMute(user))
            {
                var xpToGain = ApplicationState.FullMuteBaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.FullMuteBaseXp} XP bekommen");
            }
            else if (MutedNotDeafened(user))
            {
                var xpToGain = ApplicationState.OnlyMuteBaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat { ApplicationState.OnlyMuteBaseXp} XP bekommen");
            }
            else
            {
                var xpToGain = ApplicationState.BaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp} XP bekommen");
            }
        }
       
    }

    private static bool MutedNotDeafened(SocketGuildUser user)
    {
        return user.IsSelfMuted && !user.IsSelfDeafened;
    }

    private static bool UserIsFullMute(SocketGuildUser user)
    {
        return user.IsSelfMuted && user.IsSelfDeafened;
    }

    private static bool UserIsStreamingOrVideoingAndNotMutedOrDeafened(SocketGuildUser user)
    {
        return (user.IsStreaming || user.IsVideoing) && !user.IsSelfMuted && !user.IsSelfDeafened;
    }

    private static bool UserIsStreamingAndVideoingAndNotMutedAndDeafended(SocketGuildUser user)
    {
        return user.IsStreaming && user.IsVideoing && !user.IsSelfMuted && !user.IsSelfDeafened;
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
}