using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Repository;
using UtilsBot.Request;
using UtilsBot.Response;

namespace UtilsBot.Services;

public class LevelService
{
    DatabaseRepository _db = new DatabaseRepository();

    public async Task<XpLeaderboardResponse> HandleRequest(XpLeaderboardRequest request)
    {
        var personen = await _db.HoleTop8PersonenNachXpAsync(request.guildId);
        return new XpLeaderboardResponse()
        {
            personen = personen,
        };

    }
    public async Task HandleRequest(MessageSentRequest request)
    {
        using (var context = new BotDbContext())
        {
            var person = await _db.HoleAllgemeinePersonMitIdAsync(request.userId, context);
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
                await _db.SaveChanges(context);
            }
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

    
    public async Task<XpResponse> HandleRequest(XpRequest request)
    {
        var context = new BotDbContext();
        var person = await _db.HoleAllgemeinePersonMitIdAsync(request.userId, context);
        person = await WennPersonNichtExistiertDannErstellen(request, person, context);
        
        long currentGain = person.BekommtZurzeitSoVielXp;
        
        if (person.ZuletztImChannel.AddMinutes(3) < DateTime.Now)
        {
            currentGain = 0;
        }
                
        long platzDerPerson = await _db.HolePlatzDesUsersBeiXpAsync(person.UserId);

        int level = 1;
        long xpForNextLevel = ApplicationState.StartXp;
        long restXp = person.Xp;

        while (restXp >= xpForNextLevel)
        {
            restXp -= xpForNextLevel;
            level++;
            xpForNextLevel = (int)Math.Round(xpForNextLevel * ApplicationState.XpFaktorErhoehung);
        }

        long xpToNextLevel = xpForNextLevel - restXp;
        
        return new XpResponse(level, person.Xp, xpToNextLevel, platzDerPerson, currentGain, person.XpTodayByMessages);
    }

    public int BerechneLevelUndRestXp(long xp)
    {
        int level = 1;
        long xpForNextLevel = ApplicationState.StartXp;
        long restXp = xp;

        while (restXp >= xpForNextLevel)
        {
            restXp -= xpForNextLevel;
            level++;
            xpForNextLevel = (int)Math.Round(xpForNextLevel * ApplicationState.XpFaktorErhoehung);
        }
        
        return level;
    }
    
    private async Task<AllgemeinePerson?> WennPersonNichtExistiertDannErstellen(XpRequest request,
        AllgemeinePerson? person, BotDbContext context)
    {
        if (person == null)
        {
            await _db.AddUserAsync(request.userId, request.displayName,request.guildId);
            person = await _db.HoleAllgemeinePersonMitIdAsync(request.userId, context);
        }
        
        return person;
    }
}

