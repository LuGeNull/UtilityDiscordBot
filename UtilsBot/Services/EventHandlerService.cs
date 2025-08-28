using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Domain.BetCancel;
using UtilsBot.Domain.BetClose;
using UtilsBot.Domain.BetPayout;
using UtilsBot.Domain.BetRequest;
using UtilsBot.Domain.BetStart;
using UtilsBot.Domain.MessageSent;
using UtilsBot.Domain.ValueObjects;
using UtilsBot.Domain.Xp;
using UtilsBot.Domain.XpLeaderboard;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class EventHandlerService : HelperService
{
    private readonly DiscordSocketClient _client;
    private readonly BetService _betService;
    private readonly LevelService _levelService;
    private readonly EmbedFactory _embedFactory;
    private readonly CommandRegistrationService _commandRegistrationService;
    private readonly MessageService _messageService;
    private readonly RoleService _roleService;

    public EventHandlerService(
        DiscordSocketClient client, 
        BetService betService, 
        LevelService levelService, 
        EmbedFactory embedFactory,
        CommandRegistrationService commandRegistrationService)
    {
        _client = client;
        _betService = betService;
        _levelService = levelService;
        _embedFactory = embedFactory;
        _commandRegistrationService = commandRegistrationService;
        _roleService = new RoleService();
        _messageService= new MessageService(_client);
    }

    public void RegisterEventHandlers()
    {
        _client.SlashCommandExecuted -= SlashCommandHandlerAsync;
        _client.MessageReceived -= HandleMessageReceived;
        _client.ModalSubmitted -= ModalSubmittedHandler;
        _client.SelectMenuExecuted -= SelectMenuExecutedHandler;
        _client.ButtonExecuted -= ButtonExecutedHandler;
        
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;
        _client.MessageReceived += HandleMessageReceived;
        _client.ModalSubmitted += ModalSubmittedHandler;
        _client.SelectMenuExecuted += SelectMenuExecutedHandler;
        _client.ButtonExecuted += ButtonExecutedHandler;
    }

    private async Task ButtonExecutedHandler(SocketMessageComponent component)
    {
        await component.DeferAsync(true);
        await using var db = new DatabaseRepository(new BotDbContext());
        if (component.Data.CustomId == "wette_bet")
        {
            await BetOnBet(component, db);
        }

        if (component.Data.CustomId == "annahmen_abschliessen")
        {
            await HandleClosingBetRequest(component, db);
        }
        else if (component.Data.CustomId == "wette_abschliessen")
        {
            await HandleCloseBet(component, db);
        }
        else if (component.Data.CustomId == "wette_abbrechen")
        {
            await HandleGanzeWetteAbbrechen(component.Message.Id, component.GuildId, component, db);
        }
    }

    private async Task HandleGanzeWetteAbbrechen(ulong messageId, ulong? guildId, SocketMessageComponent component,
        DatabaseRepository db, string grund = "")
    {
        if (!await _betService.IsThisUserCreatorOfBet(component.User.Id, messageId, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }

        var response = await _betService.HandleMessageAsync(new BetCancelRequest(messageId, guildId), db);
        
        if (response.wetteIstBereitsBeendet)
        {
            await component.FollowupAsync("Wette ist bereits beendet", ephemeral: true);
            return;
        }
        
        if (response.wetteIstNichtZuende)
        {
            await component.FollowupAsync("Wettannahmen müssen vorher geschlossen werden", ephemeral: true);
            return;
        }

        if (response.wetteExistiertNicht)
        {
            await component.FollowupAsync("Wette existiert nicht", ephemeral: true);
            return;
        }

      
        //Get referenceMessage
        var message = await _messageService.GetMessageByMessageIdAndChannelIdAsync(messageId, component.Channel.Id);
        if (message == null)
        {
            await component.FollowupAsync("Wette wurde abgebrochen " + grund, ephemeral: true);
            return;
        }  
        
        // Hole aktuelle Wetten-Daten (z.B. von _betService)
        var bet = await _betService.GetBetByMessageId(message.Id, db);
        // Baue das neue Embed mit aktualisierten Teilnehmern
        var embed = await _embedFactory.BuildBetEmbed(
            bet.Title,
            bet.Ereignis1Name,
            bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            bet.Ereignis2Name,
            bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            0, DateTime.MinValue,true, bet.MaxPayoutMultiplikator);
        
        // Aktualisiere die Nachricht

        await message.ModifyAsync(msg => { msg.Embed = embed; });
        await _messageService.RemoveButtonsOnMessage(message, new List<Button>(){Button.wette_bet, Button.annahmen_abschliessen, Button.wette_abschliessen, Button.wette_abbrechen});
        await component.FollowupAsync("Wette wurde abgebrochen " + grund, ephemeral: true);
    }

    private async Task SelectMenuExecutedHandler(SocketMessageComponent selectMenu)
    {
        if (selectMenu.Data.CustomId == "option_select")
        {
            var selected = selectMenu.Data.Values.First();
            var modal = new ModalBuilder()
                .WithTitle("Gebe deinen Wetteinsatz ein")
                .WithCustomId($"zahl_modal_{selected}") // Option im CustomId speichern
                .AddTextInput("Einsatz", "zahl_input", required: true);

            await selectMenu.RespondWithModalAsync(modal.Build());
        }
        
        if (selectMenu.Data.CustomId == "bet_payout_option_select")
        {
            await HandleCloseBetWithResult(selectMenu);
        }
    }

    private async Task HandleCloseBet(SocketMessageComponent component, DatabaseRepository db)
    {
        if (!await _betService.IsThisUserCreatorOfBet(component.User.Id, component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }
        
        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("bet_payout_option_select")
            .WithPlaceholder("Welche Seite hat gewonnen ?")
            .AddOption("Option A", "1")
            .AddOption("Option B", "2");
        await component.FollowupAsync("Wähle eine Option:",
            components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(), ephemeral: true);
        
    }

    public async Task HandleCloseBetWithResult(SocketMessageComponent selectMenu)
    {
        await selectMenu.DeferAsync(true);
        await using var db = new DatabaseRepository(new BotDbContext());
        var payoutSide = selectMenu.Data.Values.First() == "1" ? BetSide.Yes : BetSide.No;
        
        if (!selectMenu.Message.Reference.MessageId.IsSpecified)
        {
            await selectMenu.FollowupAsync("Wette existiert nicht", ephemeral: true);
            return;
        }

        var message =
            await _messageService.GetMessageByMessageIdAndChannelIdAsync(selectMenu.Message.Reference.MessageId.Value,
                (ulong)selectMenu.ChannelId!);
        
        if (message == null)
        {
            await selectMenu.FollowupAsync("Wette existiert nicht", ephemeral: true);
            return;
        }
        
        var response = await _betService.HandleMessageAsync(new BetPayoutRequest(message.Id, selectMenu.GuildId, payoutSide), db);
        if (response.betIsNotFinished)
        {
            await selectMenu.FollowupAsync("Wettannahmen müssen vorher geschlossen werden", ephemeral: true); 
            return;
        }

        if (response.betDoesNotExist)
        {
            await selectMenu.FollowupAsync("Wette existiert nicht", ephemeral: true);
            await _messageService.RemoveButtonsOnMessage(message, new List<Button>(){Button.wette_bet, Button.annahmen_abschliessen, Button.wette_abschliessen, Button.wette_abbrechen});
            return;
        }

        if (response.BetWasAlreadyClosed)
        {
            await selectMenu.FollowupAsync("Wette wurde schon geschlossen", ephemeral: true);
            await _messageService.RemoveButtonsOnMessage(message, new List<Button>(){Button.wette_bet, Button.annahmen_abschliessen, Button.wette_abschliessen, Button.wette_abbrechen});
            return;
        }

        if (response.containsBetsOnlyOnOneSide)
        {
            await HandleGanzeWetteAbbrechen(selectMenu.Message.Reference.MessageId.Value, selectMenu.GuildId, selectMenu, db, "weil nur auf 1 Ereignis gewettet wurde");
            await _messageService.RemoveButtonsOnMessage(message, new List<Button>(){Button.wette_bet, Button.annahmen_abschliessen, Button.wette_abschliessen, Button.wette_abbrechen});
            return;
        }

        await _messageService.RemoveButtonsOnMessage(message, new List<Button>(){Button.wette_bet, Button.annahmen_abschliessen, Button.wette_abschliessen, Button.wette_abbrechen});
        await HandleEmbedAendern(selectMenu.Message.Reference.MessageId.Value, selectMenu, db, payoutSide);
        await selectMenu.FollowupAsync($"{(payoutSide == BetSide.Yes ? "Seite A" : "Seite B")} gewinnt ");
    }

    private async Task HandleEmbedAendern(ulong messageId, SocketMessageComponent component, DatabaseRepository db,
        BetSide payoutSide)
    {
        var bet = await db.GetBetAndPlacementsByMessageId(messageId);
        var channel = await _client.GetChannelAsync(bet.ChannelId) as IMessageChannel;
        if (channel == null)
        {
            return;
        }

        var message = await channel.GetMessageAsync(messageId) as IUserMessage;
        if (message == null)
        {
            return;
        }
        
        var embed = await _embedFactory.BuildBetEmbed(
            bet.Title,
            bet.Ereignis1Name,
            bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            bet.Ereignis2Name,
            bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            0, bet.EndedAt,false, bet.MaxPayoutMultiplikator, betIsInPayout: true, winningSide: payoutSide );
        
        // Aktualisiere die Nachricht
        await message.ModifyAsync(msg => { msg.Embed = embed; });
    }


    private async Task HandleClosingBetRequest(SocketMessageComponent component, DatabaseRepository db)
    {
        if (!await _betService.IsThisUserCreatorOfBet(component.User.Id, component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }

        await _betService.HandleMessageAsync(new BetCloseRequest(component.Message.Id, db));
        var listOfButtonsToDeactivate = new List<Button>()
        {
            Button.wette_bet,
            Button.annahmen_abschliessen
        };
        var message = await _messageService.GetMessageByMessageIdAndChannelIdAsync(component.Message.Id,  (ulong)component.ChannelId);
        
        // Hole aktuelle Wetten-Daten (z.B. von _betService)
        var bet = await _betService.GetBetByMessageId(message.Id, db);
        // Baue das neue Embed mit aktualisierten Teilnehmern
        var embed = await _embedFactory.BuildBetEmbed(
            bet.Title,
            bet.Ereignis1Name,
            bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            bet.Ereignis2Name,
            bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                .OrderByDescending(u => u.DisplayName).ToList(),
            0, bet.EndedAt,false, bet.MaxPayoutMultiplikator);
        await message.ModifyAsync(msg => { msg.Embed = embed; });
        await _messageService.RemoveButtonsOnMessage(component.Message.Id,component.Channel.Id, listOfButtonsToDeactivate);
    }

    
    
    private async Task BetOnBet(SocketMessageComponent component, DatabaseRepository db)
    {
        //Ist wette bereits geschlossen ?
        if (await _betService.IsBetClosed(component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Die Wette ist bereits geschlossen.", ephemeral: true);
            return;
        }

        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("option_select")
            .WithPlaceholder("Auf welche Seite möchtest du Wetten")
            .AddOption("Option A", "1")
            .AddOption("Option B", "2");
        await component.FollowupAsync("Wähle eine Option:",
            components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(), ephemeral: true);
    }

    private async Task HandleMessageReceived(SocketMessage message)
    {
        await using var db = new DatabaseRepository(new BotDbContext());
        if (message.Author.Id == 478972260183441412)
        {
            if (message.Content.ToLower().Equals("!deletecommands"))
            {
                if (!ApplicationState.TestMode)
                {
                    return;
                }
                await DeleteSlashCommands(message);
                return;
            }
            if (message.Content.ToLower().Equals("!deletecommandsprod"))
            {
                if (ApplicationState.TestMode)
                {
                    return;
                }
                await DeleteSlashCommands(message);
                return;
            }
            if (message.Content.ToLower().Equals("!deleteroles"))
            {
                if (!ApplicationState.TestMode)
                {
                    return;
                }
                ApplicationState.DeleteGuildRoles = true;
                await message.DeleteAsync();
                return;
            }
            if (message.Content.ToLower().Equals("!deleterolesprod"))
            {
                if (ApplicationState.TestMode)
                {
                    return;
                }
                ApplicationState.DeleteGuildRoles = true;
                await message.DeleteAsync();
                return;
            }
            if (message.Content.ToLower().Equals("!dontdeleteroles"))
            {
                if (!ApplicationState.TestMode)
                {
                    return;
                }
                ApplicationState.DeleteGuildRoles = false;
                await message.DeleteAsync();
                return;
            }
            if (message.Content.ToLower().Equals("!dontdeleterolesprod"))
            {
                if (ApplicationState.TestMode)
                {
                    return;
                }
                ApplicationState.DeleteGuildRoles = false;
                await message.DeleteAsync();
                return;
            }
        }

        if (message.Author.IsBot)
        {
            return;
        }
        
        await _levelService.HandleRequest(new MessageSentRequest(message.Author.Id, GetGuildIdFromMessage(message), message), db);
    }
    public ulong? GetGuildIdFromMessage(SocketMessage message)
    {
        // Überprüfen, ob die Nachricht in einem Guild-Channel ist (nicht in Direktnachrichten)
        if (message.Channel is SocketGuildChannel guildChannel)
        {
            return guildChannel.Guild.Id;
        }
    
        // Alternative Methode, wenn der Channel ein IGuildChannel ist
        if (message.Channel is IGuildChannel iguildChannel)
        {
            return iguildChannel.GuildId;
        }
    
        // Wenn die Nachricht in Direktnachrichten ist, gibt es keine Guild-ID
        return null;
    }
    private async Task DeleteSlashCommands(SocketMessage message)
    {
        foreach (var guild in message.Author.MutualGuilds)
        {
            var commands = _client.Rest.GetGuildApplicationCommands(guild.Id).Result;
            foreach (var command in commands)
            {
                await command.DeleteAsync();
            }
        }

        await message.DeleteAsync();
    }

    private async Task ModalSubmittedHandler(SocketModal modal)
    {
        if (modal.Data.CustomId.StartsWith("zahl_modal_"))
        {
            await modal.DeferAsync(ephemeral: true);
            var option = modal.Data.CustomId.Replace("zahl_modal_", "");
            var zahlStr = modal.Data.Components.First(x => x.CustomId == "zahl_input").Value;
            if (!int.TryParse(zahlStr, out var zahl) || zahl <= 0)
            {
                await modal.RespondAsync("Bitte gib eine positive Zahl ein.", ephemeral: true);
                return;
            }

            BetSide betSide = BetSide.No;
            if (option == "1")
            {
                betSide = BetSide.Yes;
            }
            else
            {
                betSide = BetSide.No;
            }


            var ursprungsnachrichtId = modal.Message.Reference.MessageId;
            
            if (!ursprungsnachrichtId.IsSpecified)
            {
                await modal.FollowupAsync("Die Ursprungsnachricht für die Wette existiert nicht mehr", ephemeral: true);
                return;
            }
            //DB   
            await using var db = new DatabaseRepository(new BotDbContext());

            var betResponse =
                await _betService.HandleMessageAsync(
                    new BetRequest(ursprungsnachrichtId.Value, modal.User.Id, modal.GuildId, zahl, betSide), db);
            if (betResponse.userBetsOnBothSides)
            {
                await modal.FollowupAsync("Du kannst nicht auf die Gegenseite wetten", ephemeral: true);
                return;
            }

            if (betResponse.BetIsAlreadyClosed)
            {
                await modal.FollowupAsync("Die Wettannnahmen sind bereits vorbei", ephemeral: true);
                return;
            }

            if (!betResponse.existiertEineBet)
            {
                await modal.FollowupAsync(
                    "Es existieren zurzeit keine Wetten in diesem Channel, erstelle doch eine mit /betstart",
                    ephemeral: true);
                return;
            }

            if (!betResponse.userHatGenugGold)
            {
                await modal.FollowupAsync("Du hast nicht genug Gold für die Wette, verringere den Einsatz",
                    ephemeral: true);
                return;
            }

            // Update message so User gets shown in embed
            // Hole Channel und Nachricht
            var channel = await _client.GetChannelAsync(modal.Channel.Id) as IMessageChannel;
            if (channel == null)
            {
                return;
            }

            var message = await channel.GetMessageAsync(ursprungsnachrichtId.Value) as IUserMessage;

            // Hole aktuelle Wetten-Daten (z.B. von _betService)
            var bet = await _betService.GetBetByMessageId(ursprungsnachrichtId.Value, db);
            // Baue das neue Embed mit aktualisierten Teilnehmern

            var embed = await _embedFactory.BuildBetEmbed(
                bet.Title,
                bet.Ereignis1Name,
                bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                    .OrderByDescending(u => u.DisplayName).ToList() ,
                bet.Ereignis2Name,
                bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.betAmount.ToString()), u.GoldWon, u.GoldRefunded))
                    .OrderByDescending(u => u.DisplayName).ToList(),
                0, bet.EndedAt, false, bet.MaxPayoutMultiplikator, betIsInPayout:false);

            // Aktualisiere die Nachricht
            if (message == null)
            {
                await modal.FollowupAsync($"Die Nachricht zur Wette existiert nicht mehr", ephemeral: true);
                return;
            }

            await message.ModifyAsync(msg => { msg.Embed = embed; });
        }
    }

    public async void RegisterCommands()
    {
        await _commandRegistrationService.RegisterCommands();
    }

    private static T GetOptionValue<T>(SocketSlashCommand command, string name, T defaultValue = default)
    {
        var option = command.Data.Options.FirstOrDefault(x => x.Name == name)?.Value;
        if (option == null) return defaultValue;
        return (T)option;
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        await using var db = new DatabaseRepository(new BotDbContext());

        if (command.CommandName == "info")
        {
            var transparenz = GetOptionValue<string>(command, "transparenz");
            var ephimeral = transparenz == "transparent";
            await InfoResponse(command, !ephimeral, db);
        }

        if (command.CommandName == "leaderboardxp")
        {
            var transparenz = GetOptionValue<string>(command, "transparenz");
            var ephemeral = transparenz == "transparent";
            await LeaderboardXpResponse(command, !ephemeral, db);
        }
        
        if (command.CommandName == "betstart")
        {
            await command.DeferAsync();

            var titel = GetOptionValue<string>(command, "titel", "Wette");
            var annahmeschluss = GetOptionValue<long>(command, "annahmeschluss");
            var maxPayoutMultiplikator = Math.Max(GetOptionValue<long>(command, "maxpayoutmultiplikator", 3L), 1);
            var ereignis1Name = GetOptionValue<string>(command, "ereignis1");
            var ereignis2Name = GetOptionValue<string>(command, "ereignis2");

            var component = new ComponentBuilder()
                .WithButton("Auf diese Wette setzen", "wette_bet", ButtonStyle.Success, emote: new Emoji("💸"))
                .WithButton("Wettannahmen schließen", "annahmen_abschliessen", ButtonStyle.Primary,
                    emote: new Emoji("🔒"))
                .WithButton("Wette abschließen", "wette_abschliessen", ButtonStyle.Danger, emote: new Emoji("🏁"))
                .WithButton("Wette abbrechen", "wette_abbrechen", ButtonStyle.Primary, emote: new Emoji("❌"));

            var embed = await _embedFactory.BuildBetEmbed(
                titel,
                ereignis1Name,
                new List<(string user, int betAmount, long goldWon, long goldRefunded)>(),
                ereignis2Name,
                new List<(string user, int betAmount, long goldWon, long goldRefunded)>(),
                annahmeschluss,DateTime.MinValue,
                false,
                maxPayoutMultiplikator);

            var followupMessage = await command.FollowupAsync(embed: embed, components: component.Build());
            await _betService.HandleMessageAsync(
                new BetStartRequest(command.User.Id, command.GuildId.Value, titel, annahmeschluss, followupMessage.Id,
                    followupMessage.Channel.Id, ereignis1Name, ereignis2Name,
                    (int)maxPayoutMultiplikator), db);
        }
    }

    private async Task LeaderboardXpResponse(SocketSlashCommand command, bool invisibleMessage, DatabaseRepository db)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var leaderboardResponse =
                await _levelService.HandleRequest(new XpLeaderboardRequest(guildUser.Guild.Id), db);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("XP Leaderboard")
                .WithColor(Color.DarkRed);

            for (int i = 0; i < leaderboardResponse.personen.Count; i++)
            {
                embedBuilder
                    .AddField($"Platz {i + 1}:",
                        $"```(LVL {_levelService.BerechneLevelUndRestXp(ToIntDirect(leaderboardResponse.personen[i].Xp))}) {leaderboardResponse.personen[i].DisplayName} ```");
            }

            var embed = embedBuilder.Build();


            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private async Task InfoResponse(SocketSlashCommand command, bool invisibleMessage, DatabaseRepository db)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var xpResponse =
                await _levelService.HandleRequest(
                    new XpRequest(guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id), db);

            var embed = await _embedFactory.BuildInfoEmbed(xpResponse);

            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private void NachrichtenLoeschenNachXSekunden(IUserMessage sendTask, int sekunden = 300)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(sekunden));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }
}
