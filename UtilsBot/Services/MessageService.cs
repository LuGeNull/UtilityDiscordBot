using Discord;
using Discord.WebSocket;

namespace UtilsBot.Services;

public class MessageService(DiscordSocketClient _client)
{
    public async Task<IUserMessage?> GetMessageByMessageIdAndChannelIdAsync(ulong messageId, ulong channelId)
    {
        //Get referenceMessage
        var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
        if (channel == null)
        {
            return null;
        }
        return await channel.GetMessageAsync(messageId) as IUserMessage;
    }
    
    public async Task RemoveButtonsOnMessage(ulong messageId, ulong channelId, List<Button> buttonsToDeactivate)
    {
        var builder = new ComponentBuilder();
        var message = await GetMessageByMessageIdAndChannelIdAsync(messageId, channelId);
        if (message == null)
        {
            return;
        }
        var components = message.Components;
        if (components != null)
        {
            foreach (var actionRow in components)
            {
                if (actionRow is ActionRowComponent arc)
                {
                    var newRow = new ActionRowBuilder();
                    foreach (var component in arc.Components)
                    {
                        if (component is ButtonComponent button)
                        {
                            if (!buttonsToDeactivate.Select(b => b.ToString()).Contains(component.CustomId))
                            {
                                newRow.WithButton(button.Label, button.CustomId, button.Style, button.Emote);
                            }
                        }
                    }
                    builder.AddRow(newRow);
                }
            }
        }
        await message.ModifyAsync(msg => { msg.Components = builder.Build(); });
    }
    
    public async Task RemoveButtonsOnMessage(IUserMessage message, List<Button> buttonsToDeactivate)
    {
        var builder = new ComponentBuilder();
        var components = message.Components;
        if (components != null)
        {
            foreach (var actionRow in components)
            {
                if (actionRow is ActionRowComponent arc)
                {
                    var newRow = new ActionRowBuilder();
                    foreach (var component in arc.Components)
                    {
                        if (component is ButtonComponent button)
                        {
                            if (!buttonsToDeactivate.Select(b => b.ToString()).Contains(component.CustomId))
                            {
                                newRow.WithButton(button.Label, button.CustomId, button.Style, button.Emote);
                            }
                        }
                    }
                    builder.AddRow(newRow);
                }
            }
        }
        await message.ModifyAsync(msg => { msg.Components = builder.Build(); });
    }
}