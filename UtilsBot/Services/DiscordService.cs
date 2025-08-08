using System.Net.Mime;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Domain.BetCancel;
using UtilsBot.Domain.BetPayout;
using UtilsBot.Domain.BetRequest;
using UtilsBot.Domain.BetStart;
using UtilsBot.Domain.MessageSent;
using UtilsBot.Domain.ValueObjects;
using UtilsBot.Domain.Xp;
using UtilsBot.Domain.XpLeaderboard;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly LevelService _levelService;
    private readonly string _token;
    private readonly DiscordServerChangeMonitor _discordServerChangeListener;
    private readonly BetService _betService;

    public DiscordService(DiscordServerChangeMonitor discordServerChangeListener, string token, BetService betService)
    {
        _levelService = new LevelService();
        _token = token;
        _betService = betService;
        _discordServerChangeListener = discordServerChangeListener;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
    }

    public async Task StartWorking()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        //Message Received
        _client.MessageReceived += async (message) => { await HandleMessageReceived(message); };

        _client.ModalSubmitted += ModalSubmittedHandler;

        // SelectMenu-Handler: Zeigt nach Auswahl das Modal für die Zahl
        _client.SelectMenuExecuted += async (selectMenu) => { await SelectMenuExecutedHandler(selectMenu); };

        _client.ButtonExecuted += async (component) =>
        {
            //DB
            await component.DeferAsync(true);
            await using var db = new DatabaseRepository(new BotDbContext());
            if (component.Data.CustomId == "wette_bet")
            {
                if (await HandleSetzeAufWette(component, db)) return;
            }

            if (component.Data.CustomId == "annahmen_abschliessen")
            {
                await HandleAnnahmenAbschliessen(component, db);
            }
            else if (component.Data.CustomId == "wette_abschliessen")
            {
                await HandleGanzeWetteAbschliessen(component, db);
            }
            else if (component.Data.CustomId == "wette_abbrechen")
            {
                await HandleGanzeWetteAbbrechen(component, db);
            }
        };

        RegistriereCommands(_client);
        await _discordServerChangeListener.StartPeriodicCheck(_client);
    }

    private async Task HandleGanzeWetteAbbrechen(SocketMessageComponent component, DatabaseRepository db,
        string grund = "")
    {
        if (!await _betService.IstDieserUserErstellerDerWette(component.User.Id, component.Message.Id, db))
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
        var embed = WetteEmbed(
            bet.Title,
            bet.Ereignis1Name,
            bet.Placements.Where(b => b.Site == true).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                .OrderByDescending(u => u.DisplayName).ToList() ?? new List<(string user, int einsatz)>(),
            bet.Ereignis2Name,
            bet.Placements.Where(b => b.Site == false).Select(u => (u.DisplayName, int.Parse(u.Einsatz.ToString())))
                .OrderByDescending(u => u.DisplayName).ToList() ?? new List<(string user, int einsatz)>(),
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

    private static async Task SelectMenuExecutedHandler(SocketMessageComponent selectMenu)
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

    private async Task HandleGanzeWetteAbschliessen(SocketMessageComponent component, DatabaseRepository db)
    {
        if (!await _betService.IstDieserUserErstellerDerWette(component.User.Id, component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }

        var response = await _betService.HandleMessageAsync(new BetPayoutRequest(component.Message.Id), db);
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

        if (response.wetteWurdeSchonBeendet)
        {
            await component.FollowupAsync("Wette wurde schon geschlossen", ephemeral: true);
            return;
        }

        var bet = await _betService.GetBetByMessageId(component.Message.Id, db);
        if (!EnthaeltBetWettenAufBeidenSeiten(bet))
        {
            await HandleGanzeWetteAbbrechen(component, db, "weil nur auf 1 Ereignis gewettet wurde");
            return;
        }
    }

    private bool EnthaeltBetWettenAufBeidenSeiten(Bet? bet)
    {
        var seiteAVorhanden = false;
        var seiteBVorhanden = false;
        foreach (var placement in bet.Placements)
        {
            if (placement.Site)
            {
                seiteAVorhanden = true;
            }

            if (!placement.Site)
            {
                seiteBVorhanden = true;
            }

            if (seiteAVorhanden && seiteBVorhanden)
            {
                return true;
            }
        }

        return false;
    }

    private async Task HandleAnnahmenAbschliessen(SocketMessageComponent component, DatabaseRepository db)
    {
        if (!await _betService.IstDieserUserErstellerDerWette(component.User.Id, component.Message.Id, db))
        {
            // Antwort an den User
            await component.FollowupAsync("Änderungen kann nur der Wettersteller machen", ephemeral: true);
            return;
        }

        await _betService.WettannahmenSchliessen(component.Message.Id, db);
        await ButtonsDeaktivieren(component, false);
        await component.FollowupAsync("Wettannahmen wurden geschlossen!", ephemeral: true);
    }

    private async Task<bool> HandleSetzeAufWette(SocketMessageComponent component, DatabaseRepository db)
    {
        //Ist wette bereits geschlossen ?
        if (await _betService.IstWetteGeschlossen(component.Message.Id, db))
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
                await LoescheCommands(message);
                return;
            }
        }

        await using var db = new DatabaseRepository(new BotDbContext());
        if (message.Author.IsBot) return;
        await _levelService.HandleRequest(new MessageSentRequest(message.Author.Id, message), db);
        Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
    }

    private async Task LoescheCommands(SocketMessage message)
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
        return;
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

            WettOption wettOption = WettOption.Nein;
            if (option == "1")
            {
                wettOption = WettOption.Ja;
            }
            else
            {
                wettOption = WettOption.Nein;
            }


            var ursprungsnachrichtId = modal.Message.Reference.MessageId;
            //DB
            await using var db = new DatabaseRepository(new BotDbContext());

            var betResponse =
                await _betService.HandleMessageAsync(
                    new BetRequest(ursprungsnachrichtId.Value, modal.User.Id, zahl, wettOption), db);
            if (betResponse.userWettetAufGegenseite)
            {
                await modal.FollowupAsync("Du kannst nicht auf die Gegenseite wetten", ephemeral: true);
                return;
            }

            if (betResponse.wetteBereitsVorbei)
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

            //Update message so User gets shown in embed
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
            var embed = WetteEmbed(
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

            var msg = await modal.FollowupAsync($"Du hast {zahl} XP gewettet!", ephemeral: true);
        }
    }

    private static async Task ButtonsDeaktivieren(SocketMessageComponent component, bool alleButtonsDeaktivieren)
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

    private async void RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("xp")
                .WithDescription("Auskunft über deinen Fortschritt")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("transparenz")
                    .WithDescription("Wähle die Transparenz")
                    .WithType(ApplicationCommandOptionType.String)
                    .AddChoice("Transparent", "transparent")
                    .AddChoice("Nicht Transparent", "not_transparent")
                    .WithRequired(false))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("leaderboardxp")
                .WithDescription("Auskunft über die XP der Top 8")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("transparenz")
                    .WithDescription("Wähle die Transparenz")
                    .WithType(ApplicationCommandOptionType.String)
                    .AddChoice("Transparent", "transparent")
                    .AddChoice("Nicht Transparent", "not_transparent")
                    .WithRequired(false))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("betstart")
                .WithDescription("Wettestarten")
                .AddOption("titel", ApplicationCommandOptionType.String, "Titel der Wette", isRequired: true)
                .AddOption("ereignis1", ApplicationCommandOptionType.String, "Name Ereignis A z.B Spanien gewinnt",
                    isRequired: true)
                .AddOption("ereignis2", ApplicationCommandOptionType.String, "Name Ereignis B z.B Deutschland gewinnt",
                    isRequired: true)
                .AddOption("annahmeschluss", ApplicationCommandOptionType.Integer, "Annahmeende in Stunden ab jetzt",
                    isRequired: true)
                .AddOption("maxpayoutmultiplikator", ApplicationCommandOptionType.Integer,
                    "Was kann jemand höchstens gewinnen von seinem Einsatz - Standard = 3 für 3X Max Payout",
                    isRequired: false)
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        await using var db = new DatabaseRepository(new BotDbContext());

        if (command.CommandName == "xp")
        {
            var transparenz = command.Data.Options.FirstOrDefault(x => x.Name == "transparenz")?.Value?.ToString();
            // transparenz ist entweder "transparent", "not_transparent" oder null (wenn nichts gewählt)
            var ephimeral = transparenz == "transparent";
            await XpResponse(command, !ephimeral, db);
        }

        if (command.CommandName == "leaderboardxp")
        {
            var transparenz = command.Data.Options.FirstOrDefault(x => x.Name == "transparenz")?.Value?.ToString();
            var ephimeral = transparenz == "transparent";
            await LeaderboardXpResponse(command, !ephimeral, db);
        }

        if (command.CommandName == "betstart")
        {
            await command.DeferAsync();
            var titel = command.Data.Options.FirstOrDefault(x => x.Name == "titel")?.Value?.ToString() ?? "Wette";
            var annahmeschluss = (long)command.Data.Options.FirstOrDefault(x => x.Name == "annahmeschluss")?.Value;
            var maxPayoutOptional = command.Data.Options.FirstOrDefault(x => x.Name == "maxpayoutmultiplikator");
            var maxPayoutMultiplikator = 3L;
            if (maxPayoutOptional?.Value != null && (long)maxPayoutOptional?.Value > 1)
            {
                maxPayoutMultiplikator = (long)maxPayoutOptional.Value;
            }

            var ereignis1Name = (string)command.Data.Options.FirstOrDefault(x => x.Name == "ereignis1")?.Value;
            var ereignis2Name = (string)command.Data.Options.FirstOrDefault(x => x.Name == "ereignis2")?.Value;

            var component = new ComponentBuilder()
                .WithButton("Auf diese Wette setzen", "wette_bet", ButtonStyle.Success, emote: new Emoji("💸"))
                .WithButton("Wettannahmen schließen", "annahmen_abschliessen", ButtonStyle.Primary,
                    emote: new Emoji("🔒"))
                .WithButton("Wette abschließen", "wette_abschliessen", ButtonStyle.Danger, disabled: false,
                    emote: new Emoji("🏁"))
                .WithButton("Wette abbrechen", "wette_abbrechen", ButtonStyle.Primary, disabled: false,
                    emote: new Emoji("❌"));

            var embed = WetteEmbed(
                $"{titel}",
                ereignis1Name,
                new List<(string user, int einsatz)>()
                {
                },
                ereignis2Name,
                new List<(string user, int einsatz)>()
                {
                },
                annahmeschluss,
                false,
                maxPayoutMultiplikator);
            var followupMessage = await command.FollowupAsync(embed: embed, components: component.Build());
            var betStartResponse = await _betService.HandleMessageAsync(
                new BetStartRequest(command.User.Id, command.GuildId.Value, titel, annahmeschluss, followupMessage.Id,
                    followupMessage.Channel.Id, ereignis1Name, ereignis2Name,
                    int.Parse(maxPayoutMultiplikator.ToString())), db);
        }
    }

    public Embed WetteEmbed(string wettTitel, string seiteAName, List<(string user, int einsatz)> seiteA,
        string seiteBName, List<(string user, int einsatz)> seiteB, long annahmeschluss,
        bool wetteWirdAbgebrochen = false, long maxPayoutMultiplikator = 3)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle($"{wettTitel}")
            .WithColor(Color.DarkBlue);

        string usersA = "";
        string usersB = "";

        if (wetteWirdAbgebrochen)
        {
            usersA = seiteA.Count == 0
                ? "⚠️ Kein Teilnehmer hat gesetzt"
                : string.Join("\n", seiteA.Select(x => $"{x.user}: {x.einsatz} XP (zurückerstattet)"));
            usersB = seiteB.Count == 0
                ? "⚠️ Kein Teilnehmer hat gesetzt"
                : string.Join("\n", seiteB.Select(x => $"{x.user}: {x.einsatz} XP (zurückerstattet)"));
        }
        else
        {
            usersA = seiteA.Count == 0
                ? "Noch keine Teilnehmer"
                : string.Join("\n", seiteA.Select(x => $"{x.user}: {x.einsatz} XP"));
            usersB = seiteB.Count == 0
                ? "Noch keine Teilnehmer"
                : string.Join("\n", seiteB.Select(x => $"{x.user}: {x.einsatz} XP"));
        }

        string seiteAContent = $"```{usersA}```";
        string seiteBContent = $"```{usersB}```";

        var closingDate = DateTime.UtcNow.AddHours(annahmeschluss);
        var unixTimestamp = ((DateTimeOffset)closingDate).ToUnixTimeSeconds();

        if (wetteWirdAbgebrochen)
        {
            var cancelDate = DateTime.UtcNow;
            var unixTimestampCancelDate = ((DateTimeOffset)cancelDate).ToUnixTimeSeconds();

            embedBuilder
                .AddField($"{seiteAName} (Option A)", seiteAContent, true)
                .AddField($"{seiteBName} (Option B)", seiteBContent, true)
                .AddField($"Maximale Gewinnmöglichkeit", $"```{maxPayoutMultiplikator}X der Einsatz```")
                .AddField("❌ **Wette wurde abgebrochen**", $"<t:{unixTimestampCancelDate}:f>");
        }
        else
        {
            embedBuilder
                .AddField($"{seiteAName} (Option A)", seiteAContent, true)
                .AddField($"{seiteBName} (Option B)", seiteBContent, true)
                .AddField($" Gesamteinsatz", $"**{seiteA.Sum(x => x.einsatz) + seiteB.Sum(x => x.einsatz)} XP**")
                .AddField($"Einsatz für **{seiteAName}**",
                    $"```\n{seiteA.Sum(x => x.einsatz)} XP {BerechneQuote(seiteA, seiteB)}```", true)
                .AddField($"Einsatz für **{seiteBName}**",
                    $"```\n{seiteB.Sum(x => x.einsatz)} XP {BerechneQuote(seiteB, seiteA)} ```", true)
                .AddField($"Maximale Gewinnmöglichkeit", $"```{maxPayoutMultiplikator}X der Einsatz```")
                .AddField("⏳ Wettannahme endet am", $"<t:{unixTimestamp}:f>");
        }

        return embedBuilder.Build();
    }

    private string BerechneQuote(List<(string user, int einsatz)> seiteA, List<(string user, int einsatz)> seiteB)
    {
        if (seiteA.Sum(sa => sa.einsatz) == 0 || seiteB.Sum(sb => sb.einsatz) == 0)
        {
            return "";
        }

        return $"\nQuote: {(seiteA.Sum(x => x.einsatz) + seiteB.Sum(x => x.einsatz)) / seiteA.Sum(x => x.einsatz)} X";
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

            var embed = XpEmbed(xpResponse);

            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private static Embed XpEmbed(XpResponse xpResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Dein Level-Fortschritt")
            .WithColor(Color.DarkRed)
            .AddField("Level", $"```{xpResponse.level}```", true)
            .AddField("XP", $"```{xpResponse.xp}```", true)
            .AddField($"XP bis Level {xpResponse.level + 1}", $"```{xpResponse.xpToNextLevel}```")
            .AddField("Dein Platz in Vergleich zu allen", $"```{xpResponse.platzDerPerson}```")
            .AddField($"Du bekommst zurzeit", $"```{xpResponse.currentGain} XP / MIN```")
            .AddField($"Nachrichtenpunkte heute verdient",
                $"```{xpResponse.nachrichtenPunkte} XP / {ApplicationState.NachrichtenpunkteTaeglich} XP```")
            .Build();
    }

    private static void NachrichtenLoeschenNachXSekunden(IUserMessage sendTask, int sekunden = 300)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(sekunden));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}