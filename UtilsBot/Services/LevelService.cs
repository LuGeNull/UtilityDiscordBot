using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Domain.MessageSent;
using UtilsBot.Domain.Xp;
using UtilsBot.Domain.XpLeaderboard;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class LevelService : HelperService
{

    public async Task<XpLeaderboardResponse> HandleRequest(XpLeaderboardRequest request, DatabaseRepository db)
    {
        var personen = await db.HoleTop8PersonenNachXpAsync(request.guildId);
        return new XpLeaderboardResponse()
        {
            personen = personen,
        };

    }
    public async Task HandleRequest(MessageSentRequest request, DatabaseRepository db)
    {
        var person = await db.GetUserById(request.userId);
        if (person == null)
        {
            return;
        }
        var xpToAdd = DetermineHowMuchXpToAddFromMessageType(request.message);

        if (person.LastXpGainDate.Date != DateTime.Today)
        {
            person.XpTodayByMessages = 0;
            person.LastXpGainDate = DateTime.Today;
        }
        int xpAvailable = ApplicationState.NachrichtenpunkteTaeglich - person.XpTodayByMessages;
        int xpGranted = Math.Min(xpToAdd, xpAvailable);

        if (xpGranted > 0)
        {
            person.Xp += xpGranted;
            person.XpTodayByMessages += xpGranted;
            person.LastXpGainDate = DateTime.Today;
            await db.SaveChangesAsync();
        }
    }

    private int DetermineHowMuchXpToAddFromMessageType(SocketMessage message)
    {
        if (MessageIsNormal(message))
        {
            return ApplicationState.NormalMessageXpGain;
        }
        if (MessageIsPicture(message))
        {
            return ApplicationState.PictureMessageXpGain;
        }
        if (MessageIsLink(message))
        {
            return ApplicationState.LinkMessageXpGain;
        }
        if (MessageIsVideo(message))
        {
            return ApplicationState.VideoMessageXpGain;
        }
        if (MessageIsGif(message))
        {
            return ApplicationState.GifMessageXpGain;
        }

        return 0;
    }

    private bool MessageIsGif(SocketMessage message)
    {
        if (message.CleanContent.ToLower().Contains("gif") || message.Embeds.Any(e => e.Type == EmbedType.Gifv))
        {
            return true;
        }
        return false;
    }

    private bool MessageIsVideo(SocketMessage message)
    {
        if(message.Attachments.Any(e => e.ContentType.ToLower().Contains("video")))
        {
            return true;
        }
        
        return false;
    }

    private bool MessageIsLink(SocketMessage message)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(message.Content, @"https?://\S+") && !message.CleanContent.ToLower().Contains("gif"))
        {
            return true;
        }

        return false;
    }

    private bool MessageIsPicture(SocketMessage message)
    {
        if (message.Attachments.Count > 0 && message.Attachments.Any(e => e.Filename.Contains("image")))
        {
            return true;
        }

        return false;
    }

    private bool MessageIsNormal(SocketMessage message)
    {
        if (message.Attachments.Count == 0 && message.Embeds.Count == 0 && !message.CleanContent.ToLower().Contains("gif"))
        {
            return true;
        }

        return false;
    }

    
    public async Task<InfoResponse> HandleRequest(XpRequest request, DatabaseRepository db)
    {
        var person = await db.GetUserById(request.userId);
        person = await WennPersonNichtExistiertDannErstellen(request, person, db);
        
        long currentXpGain = person.GetsSoMuchXpRightNow;
        var currentGoldGain = ApplicationState.DefaultGoldEarning;
        if (person.LastTimeInChannel.AddMinutes(3) < DateTime.Now)
        {
            currentGoldGain = 0;
            currentXpGain = 0;
        }
                
        long platzDerPerson = await db.HolePlatzDesUsersBeiXpAsync(person.UserId);

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
        
        return new InfoResponse(level, ToIntDirect(person.Xp), ToIntDirect(xpToNextLevel), platzDerPerson, currentXpGain,  currentGoldGain, person.XpTodayByMessages, ToIntDirect(person.Gold));
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



