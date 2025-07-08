using Discord;
using Discord.WebSocket;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Services;

public class DiscordCommandHandler(IDomainCommandHandler domainCommandHandler) : IDiscordCommandHandler
{
    public async Task InfoAsync(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: true);

            var response = await domainCommandHandler.InfoAsync(new InfoRequestDto(guildUser.Id));

            var embed = new EmbedBuilder()
                .WithTitle("Dein Level-Fortschritt")
                .WithColor(Color.DarkRed)
                .AddField("Level", $"```{response.Level}```", true)
                .AddField("XP", $"```{response.Xp}```", true)
                .AddField($"XP bis Level {response.NextLevel}", $"```{response.XpToNextLevel}```")
                .AddField("Dein Platz in Vergleich zu allen", $"```{response.Rank}```")
                .AddField($"Du bekommst zurzeit", $"```{response.CurrentGain} XP / MIN```")
                .Build();

            await command.FollowupAsync(embed: embed, ephemeral: true);
        }
    }

    public async Task InterestedAsync(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            var von = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "von")?.Value ?? 0L;
            var bis = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "bis")?.Value ?? 0L;
            try
            {
                await domainCommandHandler.InterestedAsync(new InterestedRequest(von, bis, guildUser.Id,
                    guildUser.DisplayName, guildUser.Guild.Id));
            }
            catch (ArgumentException ex)
            {
                await command.RespondAsync(ex.Message, ephemeral: true);
            }

            await command.RespondAsync("I'll notify you!", ephemeral: true);
        }
    }
}