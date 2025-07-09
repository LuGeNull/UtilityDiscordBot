using UtilsBot.Domain.Models;

namespace UtilsBot.Domain.Contracts;

public interface IBotRepository
{
    Task SaveChangesAsync();
    Task DebugInfoAsync();
    Task<AllgemeinePerson?> HoleAllgemeinePersonMitIdAsync(ulong userId);
    Task<List<AllgemeinePerson>> PersonenDieBenachrichtigtWerdenWollenAsync(ulong userId, string userDisplayName, List<ulong> toList);
    Task<List<ulong>> HoleAllgemeinePersonenIdsMitGuildIdAsync(ulong guildId);
    Task AddUserAsync(ulong id, string displayName, ulong guildId);
    Task AddUserToInterestedListAsync(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von, long bis);
    Task<AllgemeinePerson> HoleUserMitIdAsync(ulong guildUserId);
    Task<long> HolePlatzDesUsersBeiXpAsync(ulong guildUserId);
}