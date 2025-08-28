
using Discord.WebSocket;

namespace UtilsBot.Domain.MessageSent;

public record MessageSentRequest(ulong userId, ulong? guildId, SocketMessage message);
