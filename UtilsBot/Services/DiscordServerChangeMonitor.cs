using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
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

    public async Task CheckServerChangesAsync(DiscordSocketClient _client)
    {
        _database.DebugInfo();
        foreach (var guild in _client.Guilds)
        foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 0))
        {
            NeueUserHinzufuegenFallsVorhanden(channel, _client);
            await UpdateInfoUser(channel, _client);
        }
    }

    public async Task UpdateInfoUser(SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        using (var context = new BotDbContext())
        {
            foreach (var user in connectedUsers)
            {
                var lokalePerson = await _database.HoleAllgemeinePersonMitIdAsync(user.Id, context);
                if (lokalePerson != null)
                {
                    lokalePerson.ZuletztImChannel = DateTime.Now;
                    var xpToGain = GetXpToGain(user);
                    lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                    lokalePerson.Xp += xpToGain;
                    Console.WriteLine($"{lokalePerson.DisplayName} hat {xpToGain} XP bekommen");
                }
            }
            await _database.SaveChanges(context);
        }
    }

    private int GetXpToGain(SocketGuildUser user)
    {
        if (UserIsStreamingAndVideoingAndNotMutedAndDeafended(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamAndVideoBonus;
            //lokalePerson.Xp += xpToGain;
            //lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
            //Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus} XP bekommen");
        }
        
        if (UserIsStreamingOrVideoingAndNotMutedOrDeafened(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus;
           // lokalePerson.Xp += xpToGain;
           // lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
           // Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus} XP bekommen");
        }
        else
        {
            if (UserIsFullMute(user))
            {
                return ApplicationState.FullMuteBaseXp;
                //lokalePerson.Xp += xpToGain;
                //lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                //Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.FullMuteBaseXp} XP bekommen");
            }
            else if (MutedNotDeafened(user))
            {
                return ApplicationState.OnlyMuteBaseXp;
                //lokalePerson.Xp += xpToGain;
                //lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                //Console.WriteLine($"{lokalePerson.DisplayName} hat { ApplicationState.OnlyMuteBaseXp} XP bekommen");
            }
            else
            {
                return ApplicationState.BaseXp;
                //lokalePerson.Xp += xpToGain;
                //lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                //Console.WriteLine($"{lokalePerson.DisplayName} hat {ApplicationState.BaseXp} XP bekommen");
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

    private async Task NeueUserHinzufuegenFallsVorhanden(SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(await _database.HoleAllgemeinePersonenIdsMitGuildIdAsync(channel.Guild.Id));
        if (neueUser.Any())
            foreach (var user in neueUser)
            {
                var userInQuestion = channel.ConnectedUsers.First(u => u.Id == user);
                await _database.AddUserAsync(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
    }

    public async Task StartPeriodicCheck(DiscordSocketClient client)
    {
        await CheckServerChangesAsync(client);
        _checkTimer = new Timer(ApplicationState.TickProXSekunden);
        _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }
}