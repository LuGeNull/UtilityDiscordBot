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

public class EventHandlerService
{
    private readonly DiscordSocketClient _client;
    private readonly BetService _betService;
    private readonly LevelService _levelService;
    private readonly EmbedFactory _embedFactory;
    private readonly CommandRegistrationService _commandRegistrationService;

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
    }

    public void RegisterEventHandlers()
    {
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
            if (await BetOnBet(component, db)) return;
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
            await HandleGanzeWetteAbbrechen(component, db);
        }
    }

    private async Task HandleGanzeWetteAbbrechen(SocketMessageComponent component, DatabaseRepository db, string grund = "")
    {
        if (!await _betService.IsThisUserCreatorOfBet(component.User.Id, component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }

        var response = await _betService.HandleMessageAsync(new BetCancelRequest(component.Message.Id), db);

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

        await ButtonsDeaktivieren(component, true);

        // Hole aktuelle Wetten-Daten (z.B. von _betService)
        var bet = await _betService.GetBetByMessageId(component.Message.Id, db);
        // Baue das neue Embed mit aktualisierten Teilnehmern
        var embed = await _embedFactory.BuildBetEmbed(
            bet.Title,
            bet.Ereignis1Name,
            bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                .OrderByDescending(u => u.DisplayName).ToList(),
            bet.Ereignis2Name,
            bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                .OrderByDescending(u => u.DisplayName).ToList(),
            0, true, bet.MaxPayoutMultiplikator);

        var ursprungsnachrichtId = component.Message.Id;

        var channel = await _client.GetChannelAsync(component.Channel.Id) as IMessageChannel;
        if (channel == null)
        {
            return;
        }

        var message = await channel.GetMessageAsync(ursprungsnachrichtId) as IUserMessage;
        // Aktualisiere die Nachricht
        if (message == null)
        {
            await component.FollowupAsync("Wette wurde abgebrochen " + grund, ephemeral: true);
            return;
        }

        await message.ModifyAsync(msg => { msg.Embed = embed; });
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
                .AddTextInput("Einsatz", "zahl_input", TextInputStyle.Short, required: true);

            await selectMenu.RespondWithModalAsync(modal.Build());
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
        //TODO: WETTSEITE
        var response = await _betService.HandleMessageAsync(new BetPayoutRequest(component.Message.Id, BetSide.No), db);
        if (response.betIsNotFinished)
        {
            await component.FollowupAsync("Wettannahmen müssen vorher geschlossen werden", ephemeral: true);
            return;
        }

        if (response.betDoesNotExist)
        {
            await component.FollowupAsync("Wette existiert nicht", ephemeral: true);
            return;
        }

        if (response.BetWasAlreadyClosed)
        {
            await component.FollowupAsync("Wette wurde schon geschlossen", ephemeral: true);
            return;
        }

        var bet = await _betService.GetBetByMessageId(component.Message.Id, db);
        if (!_betService.ContainsBetsOnBothSides(bet))
        {
            await HandleGanzeWetteAbbrechen(component, db, "weil nur auf 1 Ereignis gewettet wurde");
            return;
        }
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
        await ButtonsDeaktivieren(component, false);
        await component.FollowupAsync("Wettannahmen wurden geschlossen!", ephemeral: true);
    }

    private async Task<bool> BetOnBet(SocketMessageComponent component, DatabaseRepository db)
    {
        //Ist wette bereits geschlossen ?
        if (await _betService.IsBetClosed(component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Die Wette ist bereits geschlossen.", ephemeral: true);
            return true;
        }

        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("option_select")
            .WithPlaceholder("Auf welche Seite möchtest du Wetten")
            .AddOption("Option A", "1")
            .AddOption("Option B", "2");
        await component.FollowupAsync("Wähle eine Option:",
            components: new ComponentBuilder().WithSelectMenu(selectMenu).Build(), ephemeral: true);
        return false;
    }

    private async Task HandleMessageReceived(SocketMessage message)
    {
        if (message.Author.Id == 478972260183441412)
        {
            if (message.Content.ToLower().Equals("!deletecommands"))
            {
                await DeleteSlashCommands(message);
                return;
            }
        }

        await using var db = new DatabaseRepository(new BotDbContext());
        if (message.Author.IsBot) return;
        await _levelService.HandleRequest(new MessageSentRequest(message.Author.Id, message), db);
        Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
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
            //DB
            await using var db = new DatabaseRepository(new BotDbContext());

            var betResponse =
                await _betService.HandleMessageAsync(
                    new BetRequest(ursprungsnachrichtId.Value, modal.User.Id, zahl, betSide), db);
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

            if (!betResponse.userHatGenugXp)
            {
                await modal.FollowupAsync("Du hast nicht genug Xp für die Wette, verringere den Einsatz",
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
                bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                    .OrderByDescending(u => u.DisplayName).ToList() ?? new List<(string user, int einsatz)>(),
                bet.Ereignis2Name,
                bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                    .OrderByDescending(u => u.DisplayName).ToList() ?? new List<(string user, int einsatz)>(),
                0, false, bet.MaxPayoutMultiplikator);

            // Aktualisiere die Nachricht
            if (message == null)
            {
                await modal.FollowupAsync($"Du hast {zahl} XP gewettet!", ephemeral: true);
                return;
            }

            await message.ModifyAsync(msg => { msg.Embed = embed; });

            await modal.FollowupAsync($"Du hast {zahl} XP gewettet!", ephemeral: true);
        }
    }

    private async Task ButtonsDeaktivieren(SocketMessageComponent component, bool alleButtonsDeaktivieren)
    {
        var disabledComponents = new ComponentBuilder();
        foreach (var row in component.Message.Components)
        {
            foreach (var btn in row.Components.OfType<ButtonComponent>())
            {
                bool disable = true;
                if (!alleButtonsDeaktivieren)
                {
                    disable = btn.CustomId != "wette_abschliessen" && btn.CustomId != "wette_abbrechen";
                }

                disabledComponents.WithButton(btn.Label, btn.CustomId, btn.Style, disabled: disable);
            }
        }

        await component.Message.ModifyAsync(msg => { msg.Components = disabledComponents.Build(); });
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

        if (command.CommandName == "xp")
        {
            var transparenz = GetOptionValue<string>(command, "transparenz");
            var ephimeral = transparenz == "transparent";
            await XpResponse(command, !ephimeral, db);
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
                new List<(string user, int betAmount)>(),
                ereignis2Name,
                new List<(string user, int betAmount)>(),
                annahmeschluss,
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
                        $"```(LVL {_levelService.BerechneLevelUndRestXp(leaderboardResponse.personen[i].Xp)}) {leaderboardResponse.personen[i].DisplayName} ```");
            }

            var embed = embedBuilder.Build();


            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private async Task XpResponse(SocketSlashCommand command, bool invisibleMessage, DatabaseRepository db)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var xpResponse =
                await _levelService.HandleRequest(
                    new XpRequest(guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id), db);

            var embed = await _embedFactory.BuildXpEmbed(xpResponse);

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
