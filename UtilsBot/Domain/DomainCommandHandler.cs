using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Domain;

public class DomainCommandHandler(IBotRepository repository) : IDomainCommandHandler
{
    public async Task<InfoResponseDto> InfoAsync(InfoRequestDto request)
    {
        var startXp = 1000;
        var faktor = 1.3;
        var person = await repository.HoleUserMitIdAsync(request.GuildUserId);
        var xp = person.Xp;
        var currentGain = person.BekommtZurzeitSoVielXp;
        if (person.ZuletztImChannel.AddMinutes(1) < DateTime.Now)
        {
            currentGain = 0;
        }

        var rank = await repository.HolePlatzDesUsersBeiXpAsync(request.GuildUserId);

        var level = 1;
        var xpForNextLevel = startXp;
        var restXp = xp;

        while (restXp >= xpForNextLevel)
        {
            restXp -= xpForNextLevel;
            level++;
            xpForNextLevel = (int)Math.Round(xpForNextLevel * faktor);
        }

        var xpToNextLevel = xpForNextLevel - restXp;

        return new InfoResponseDto(level, xp, level + 1, xpToNextLevel, rank, currentGain);
    }


    public async Task InterestedAsync(InterestedRequest request)
    {
        var von = request.Von;
        var bis = request.Bis;
        if (request.Von == request.Bis)
        {
            von = 0;
            bis = 24;
        }

        if (von < 0 || von > 24 || bis < 0 || bis > 24)
        {
            throw new ArgumentException("Es sind nur Werte von 0 bis 24 erlaubt.");
        }

        await repository.AddUserToInterestedListAsync(
            request.GuildUserId, request.GuildUserDisplayName, request.GuildId, von, bis);
    }
}