
using Discord.WebSocket;

namespace UtilsBot.Request;

public record MessageSentRequest(ulong userId, SocketMessage message);
