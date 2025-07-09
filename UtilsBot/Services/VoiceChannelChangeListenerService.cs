using System.Formats.Asn1;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class VoiceChannelChangeListenerService(IServiceScopeFactory scopeFactory, BotConfig config) : IDisposable
{
    private Timer _checkTimer = new();

    private async Task WithRepositoryAsync(Func<IBotRepository, Task> action)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBotRepository>();
        await action(repository);
    }

    private async Task CheckServerChangesAsync(DiscordSocketClient client)
    {
        await WithRepositoryAsync(async repo =>
        {
            await repo.DebugInfoAsync();
            foreach (var guild in client.Guilds)
            foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 0))
            {
                await NeueUserHinzufuegenFallsVorhandenAsync(repo, channel);
                await UpdateInfoUserAsync(repo, channel, client);
            }
        });
    }

    private async Task UpdateInfoUserAsync(IBotRepository repo, SocketVoiceChannel channel, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var lokalePerson = await repo.HoleAllgemeinePersonMitIdAsync(user.Id);
            if (SollFuerDiePersonEineBenachrichtigungVerschicktWerden(lokalePerson))
            {
                var ZuBenachrichtigendePerson = await repo.PersonenDieBenachrichtigtWerdenWollenAsync(user.Id,
                    user.DisplayName, connectedUsers.Select(cu => cu.Id).ToList());
                await PersonenBenachrichtigenAsync(ZuBenachrichtigendePerson, client, user.DisplayName,
                    channel.Guild.Name);
            }

            lokalePerson.ZuletztImChannel = DateTime.Now;
            UpdateExp(lokalePerson, user);
        }

        await repo.SaveChangesAsync();
    }

    private async Task PersonenBenachrichtigenAsync(List<AllgemeinePerson> zuBenachrichtigendePersonen,
        DiscordSocketClient client, string userDisplayName, string guildName)
    {
        foreach (var zuBenachrichtigendePerson in zuBenachrichtigendePersonen)
        {
            var user = await client.GetUserAsync(zuBenachrichtigendePerson.UserId);
            if (config.NachrichtenVerschicken)
            {
                var sendTask =
                    await user.SendMessageAsync($"Auf dem Server {guildName} ist {userDisplayName} beigetreten!");
                NachrichtenLöschenNachXMinutenAsync(sendTask);
            }
            else
            {
                Console.WriteLine(
                    $"Jetzt wäre eine Nachricht verschickt worden an {user.Username} über {userDisplayName} auf dem Server {guildName}");
            }
        }
    }

    private void NachrichtenLöschenNachXMinutenAsync(IUserMessage sendTask)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(config.NachrichtenWerdenGelöschtNachXMinuten));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }

    private bool SollFuerDiePersonEineBenachrichtigungVerschicktWerden(AllgemeinePerson? lokalePerson)
    {
        if (config.TestMode) return true;
        return lokalePerson.ZuletztImChannel.AddMinutes(config.UserXMinutenAusDemChannel) <
               DateTime.Now;
    }

    private void UpdateExp(AllgemeinePerson lokalePerson, SocketGuildUser user)
    {
        if (UserIsStreamingAndVideoingAndNotMutedAndDeafended(user))
        {
            var xpToGain = config.BaseXp + config.StreamAndVideoBonus;
            lokalePerson.Xp += xpToGain;
            lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
            Console.WriteLine(
                $"{lokalePerson.DisplayName} hat {config.BaseXp + config.StreamOrVideoBonus} XP bekommen");
        }

        if (UserIsStreamingOrVideoingAndNotMutedOrDeafened(user))
        {
            var xpToGain = config.BaseXp + config.StreamOrVideoBonus;
            lokalePerson.Xp += xpToGain;
            lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
            Console.WriteLine(
                $"{lokalePerson.DisplayName} hat {config.BaseXp + config.StreamOrVideoBonus} XP bekommen");
        }
        else
        {
            if (UserIsFullMute(user))
            {
                var xpToGain = config.FullMuteBaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {config.FullMuteBaseXp} XP bekommen");
            }
            else if (MutedNotDeafened(user))
            {
                var xpToGain = config.OnlyMuteBaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {config.OnlyMuteBaseXp} XP bekommen");
            }
            else
            {
                var xpToGain = config.BaseXp;
                lokalePerson.Xp += xpToGain;
                lokalePerson.BekommtZurzeitSoVielXp = xpToGain;
                Console.WriteLine($"{lokalePerson.DisplayName} hat {config.BaseXp} XP bekommen");
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

    private async Task NeueUserHinzufuegenFallsVorhandenAsync(IBotRepository repo, SocketVoiceChannel channel)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(await repo.HoleAllgemeinePersonenIdsMitGuildIdAsync(channel.Guild.Id)).ToList();
        if (neueUser.Any())
            foreach (var user in neueUser)
            {
                var userInQuestion = channel.ConnectedUsers.First(u => u.Id == user);
                await repo.AddUserAsync(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
    }

    public async Task StartPeriodicCheck(DiscordSocketClient client)
    {
        await CheckServerChangesAsync(client);
        _checkTimer = new Timer(config.TickProXSekunden);
        _checkTimer.Elapsed += async (_, _) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }

    public void Dispose()
    {
        _checkTimer.Dispose();
    }
}