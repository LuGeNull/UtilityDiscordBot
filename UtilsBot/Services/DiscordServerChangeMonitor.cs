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
            // Es müssen mindestens 2 Personen im Voice Channel sein
            foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 1))
            {
                await NeueUserHinzufuegenFallsVorhanden(channel, db);
                await UpdateInfoUser(channel, db);
            }
        }
    }

    private async Task UpdateInfoUser(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var lokalePerson = await db.HoleAllgemeinePersonMitIdAsync(user.Id);
            if (lokalePerson != null)
            {
                lokalePerson.ZuletztImChannel = DateTime.Now;
                var xpToGain = GetXpToGain(user);
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                lokalePerson.Xp += xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {xpToGain} XP bekommen");

            }
        }
        await db.SaveChangesAsync();
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

    private async Task NeueUserHinzufuegenFallsVorhanden(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(await db.HoleAllgemeinePersonenIdsMitGuildIdAsync(channel.Guild.Id)).ToList();
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
        _checkTimer = new Timer(ApplicationState.TickProXSekunden);
        _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }
}