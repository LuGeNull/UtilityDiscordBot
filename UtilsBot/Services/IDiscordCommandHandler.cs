using Discord.WebSocket;

namespace UtilsBot.Services;

public interface IDiscordCommandHandler
{
    Task InterestedAsync(SocketSlashCommand command);
    Task InfoAsync(SocketSlashCommand command);
}