using Discord;
using Discord.WebSocket;
using UtilsBot.Repository;
using UtilsBot.Request;
using UtilsBot.Response;

namespace UtilsBot.Services;

public class LevelService
{
    DatabaseRepository _db = new DatabaseRepository();

    public void HandleRequest(MessageSentRequest request)
    {
        var person = _db.HoleAllgemeinePersonMitId(request.userId);
        var xpToAdd = DetermineHowMuchXpToAddFromMessageType(request.message);
        
        if (person.LastXpGainDate.Date != DateTime.Today)
        {
            person.XpTodayByMessages = 0;
            person.LastXpGainDate = DateTime.Today;
        }
        int xpAvailable = 500 - person.XpTodayByMessages;
        int xpGranted = Math.Min(xpToAdd, xpAvailable);
        
        if (xpGranted > 0)
        {
            person.Xp += xpGranted;
            person.XpTodayByMessages += xpGranted;
            person.LastXpGainDate = DateTime.Today;
            _db.SaveChanges();
        }
    }

    private int DetermineHowMuchXpToAddFromMessageType(SocketMessage message)
    {
        if (MessageIsNormal(message))
        {
            return 20;
        }
        if (MessageIsPicture(message))
        {
            return 40;
        }
        if (MessageIsLink(message))
        {
            return 30;
        }
        if (MessageIsVideo(message))
        {
            return 40;
        }
        if (MessageIsGif(message))
        {
            return 30;
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

    public XpResponse HandleRequest(XpRequest request)
    {
        var person = _db.HoleAllgemeinePersonMitId(request.userId);
        person = WennPersonNichtExistiertDannErstellen(request, person);
        
        long currentGain = person.BekommtZurzeitSoVielXp;
        
        if (person.ZuletztImChannel.AddMinutes(3) < DateTime.Now)
        {
            currentGain = 0;
        }
                
        long platzDerPerson = _db.HolePlatzDesUsersBeiXp(person.UserId);

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
        
        return new XpResponse(level, person.Xp, xpToNextLevel, platzDerPerson, currentGain);
    }

    private AllgemeinePerson? WennPersonNichtExistiertDannErstellen(XpRequest request, AllgemeinePerson? person)
    {
        if (person == null)
        {
            _db.AddUser(request.userId, request.displayName,request.guildId);
            person = _db.HoleAllgemeinePersonMitId(request.userId);
        }

        return person;
    }
}

