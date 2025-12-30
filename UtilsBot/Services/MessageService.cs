using Discord;
using Discord.WebSocket;
using UtilsBot.Domain;
using UtilsBot.Domain.MessageSent;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class MessageService()
{
    
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
        int xpAvailable = ApplicationState.MessagePointsDaily - person.XpTodayByMessages;
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
}