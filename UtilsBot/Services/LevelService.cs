using UtilsBot.Domain;
using UtilsBot.Domain.Xp;
using UtilsBot.Domain.XpLeaderboard;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class LevelService : HelperService
{

    public async Task<XpLeaderboardResponse> HandleRequest(XpLeaderboardRequest request, DatabaseRepository db)
    {
        var personen = await db.GetTop8UsersByXp(request.guildId);
        return new XpLeaderboardResponse()
        {
            personen = personen,
        };

    }
    
    public async Task<InfoResponse> HandleRequest(XpRequest request, DatabaseRepository db)
    {
        var person = await db.GetUserById(request.userId);
        person = await WennPersonNichtExistiertDannErstellen(request, person, db);
        
        long currentXpGain = person.GetsSoMuchXpRightNow;
        if (person.LastTimeInChannel.AddMinutes(3) < DateTime.Now)
        {
            currentXpGain = 0;
        }
                
        long platzDerPerson = await db.GetUserXpPlacementAsync(person.UserId);

        int level = 1;
        long xpForNextLevel = ApplicationState.StartXp;
        var restXp = person.Xp;

        while (restXp >= xpForNextLevel)
        {
            restXp -= xpForNextLevel;
            level++;
            xpForNextLevel = (int)Math.Round(xpForNextLevel * ApplicationState.XpFaktorErhoehung);
        }

        var xpToNextLevel = xpForNextLevel - restXp;
        
        return new InfoResponse(level, ToIntDirect(person.Xp), ToIntDirect(xpToNextLevel), platzDerPerson, currentXpGain, person.XpTodayByMessages);
    }

    public int BerechneLevelUndRestXp(decimal xp)
    {
        int level = 1;
        long xpForNextLevel = ApplicationState.StartXp;
        var restXp = xp;

        while (restXp >= xpForNextLevel)
        {
            restXp -= xpForNextLevel;
            level++;
            xpForNextLevel = (int)Math.Round(xpForNextLevel * ApplicationState.XpFaktorErhoehung);
        }
        
        return level;
    }
    
    private async Task<AllgemeinePerson?> WennPersonNichtExistiertDannErstellen(XpRequest request,
        AllgemeinePerson? person, DatabaseRepository db)
    {
        if (person == null)
        {
            await db.AddUserAsync(request.userId, request.displayName,request.guildId);
            person = await db.GetUserById(request.userId);
        }
        
        return person;
    }
}



