using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class DiscordServerChangeMonitor
{
    private Timer _checkTimer;

    public DiscordServerChangeMonitor()
    {
        _checkTimer = new Timer();
    }

    private async Task CheckServerChangesAsync(DiscordSocketClient client)
    {
        await using var db = new DatabaseRepository(new BotDbContext());
        db.DebugInfo();
        foreach (var guild in client.Guilds)
        {
            // There have to be atleast 2 in one Channel
            foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 1))
            {
                await AddNewUserIfNecessary(channel, db);
                await UpdateInfoUser(channel, db);
            }
        }
    }

    private async Task UpdateInfoUser(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var localUser = await db.GetUserById(user.Id);
            if (localUser != null)
            {
                localUser.LastTimeInChannel = DateTime.Now;
                var xpToGain = GetXpToGain(user);
                localUser.GetsSoMuchXpRightNow = xpToGain;
                localUser.Xp += xpToGain;
                localUser.Gold += ApplicationState.DefaultGoldEarning;
                Console.WriteLine($"{localUser.DisplayName} got {xpToGain} XP and {ApplicationState.DefaultGoldEarning} Gold ");

            }
        }
        await db.SaveChangesAsync();
    }

    private int GetXpToGain(SocketGuildUser user)
    {
        if (UserIsStreamingAndVideoingAndNotMutedAndDeafended(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamAndVideoBonus;
        }
        
        if (UserIsStreamingOrVideoingAndNotMutedOrDeafened(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus;
        }
        else
        {
            if (UserIsFullMute(user))
            {
                return ApplicationState.FullMuteBaseXp;
            }
            else if (MutedNotDeafened(user))
            {
                return ApplicationState.OnlyMuteBaseXp;
            }
            else
            {
                return ApplicationState.BaseXp;
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

    private async Task AddNewUserIfNecessary(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(await db.GetUsersByGuildId(channel.Guild.Id)).ToList();
        if (neueUser.Any())
        {
            foreach (var user in neueUser)
            {
                var userInQuestion = channel.ConnectedUsers.First(u => u.Id == user);
                await db.AddUserAsync(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
        }
    }

    public async Task StartPeriodicCheck(DiscordSocketClient client)
    {
        await CheckServerChangesAsync(client);
        _checkTimer = new Timer(ApplicationState.TickPerXSeconds);
        _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }
}