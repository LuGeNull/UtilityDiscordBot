namespace UtilsBot.Domain.Models;

public record InfoRequestDto(ulong GuildUserId);

public record InfoResponseDto(int Level, long Xp, long NextLevel, long XpToNextLevel, long Rank, long CurrentGain);

public record InterestedRequest(long Von, long Bis, ulong GuildUserId, string GuildUserDisplayName, ulong GuildId);