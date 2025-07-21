using UtilsBot.Domain.Models;

namespace UtilsBot.Domain.Contracts;

public interface IBotRepository
{
    void SaveChanges();
    Task DebugInfoAsync();
    AllgemeinePerson? HoleAllgemeinePersonMitId(ulong userId);
    List<AllgemeinePerson> PersonenDieBenachrichtigtWerdenWollen(ulong userId, string userDisplayName, List<ulong> toList);
    List<ulong> HoleAllgemeinePersonenIdsMitGuildId(ulong guildId);
    void AddUser(ulong id, string displayName, ulong guildId);
    void AddUserToInterestedList(ulong guildUserId, string guildUserDisplayName, ulong guildId, long von, long bis);
    AllgemeinePerson HoleUserMitId(ulong guildUserId);
    long HolePlatzDesUsersBeiXp(ulong guildUserId);
}