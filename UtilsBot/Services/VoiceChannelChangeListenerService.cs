using Discord;
using Discord.WebSocket;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class VoiceChannelChangeListenerService
{
    public DatabaseRepository _database;
    private System.Timers.Timer _checkTimer;
    public IEnumerable<Guild> _guilds;
    public VoiceChannelChangeListenerService(DatabaseRepository database)
    {
        _database = database;
        _checkTimer = new Timer();
        _guilds = new List<Guild>();
    }

    public void AddUserToInterestedPeopleList(ulong userId, string userDisplayName, ulong guildId,
        long nichtbenachrichtigungsZeitraumVon, long nichtbenachrichtigungsZeitraumBis)
    {
        _database.AddInterestedPeople(new InterestedPerson(userId, userDisplayName, guildId, nichtbenachrichtigungsZeitraumVon, nichtbenachrichtigungsZeitraumBis));
    }
    
    public Task CheckServerChangesAsync(DiscordSocketClient _client)
    {
        foreach (var guild in _client.Guilds)
        {
            if (GuildIsNotActivelyWatched(guild))
            {
                StartWatchingGuild(guild);
                continue;
            }


            var inFrageKommendeVoiceChannel = guild.VoiceChannels.Where(c => ChannelFilter(c)).ToList();

            if (inFrageKommendeVoiceChannel.Any())
            {
                if (IstEsZeitBenutzerZuBenachrichtigen(guild.Id))
                {
                    InformiereBenutzer(guild.Id, inFrageKommendeVoiceChannel,_client);
                }
            }

            
            foreach (var channel in guild.VoiceChannels.Where(c => ChannelFilter(c)))
            {
                var connectedUsers = channel.ConnectedUsers;
                Console.WriteLine($"Channel: {channel.Name}, Members: {connectedUsers.Count}");

                // Hier kannst du Änderungen überprüfen, z.B. neue Benutzer
                foreach (var user in connectedUsers)
                {
                    Console.WriteLine($"User: {user.DisplayName}");
                }
            }
        }
        
        return Task.CompletedTask;
    }

    private void InformiereBenutzer(ulong guildId, List<SocketVoiceChannel> inFrageKommendeVoiceChannel,
        DiscordSocketClient client)
    {
        
        var alleUserDieBereitsImVoiceChannelSind = inFrageKommendeVoiceChannel.SelectMany(c => c.ConnectedUsers).ToList();
        var interesiertePersonen =
            _database.HoleInteressiertePersoneDieNichtImVoiceChannelSind(guildId, alleUserDieBereitsImVoiceChannelSind).ToList();
        
        NachrichtenVerschicken(interesiertePersonen, inFrageKommendeVoiceChannel, client);
    }

    private async Task NachrichtenVerschicken(List<InterestedPerson> interesiertePersonen,
        List<SocketVoiceChannel> inFrageKommendeVoiceChannel, DiscordSocketClient client)
    {
        foreach (var interessiertePerson in interesiertePersonen)
        {
            var channelMitMeistenUsern = inFrageKommendeVoiceChannel.Where(c => CheckIfUserCanSeeTheChannel(interessiertePerson, c)).OrderByDescending(c => c.ConnectedUsers).FirstOrDefault();
            if (channelMitMeistenUsern == null)
            {
                continue;
            }

            if (interessiertePerson.LetztesMalBenachrichtigt.AddMinutes(ApplicationState.MindestestAnzahlAnMinutenBevorWiederBenachrichtigtWird) > DateTime.Now)
            {
                continue;
            }
            var sendTask = await client.GetUser(interessiertePerson.UserId).SendMessageAsync($"Auf dem Server {channelMitMeistenUsern.Guild.Name} im Channel {channelMitMeistenUsern.Name} geht was ab!");
            _database.InterestedPersonGotMessaged(interessiertePerson);
            NachrichtenLöschenNachXMinuten(sendTask);
        }
    }

    private bool CheckIfUserCanSeeTheChannel(InterestedPerson interessiertePerson, SocketVoiceChannel channel)
    {
        var a = channel.GetUser(interessiertePerson.UserId).Roles.Select(r => r.Id);
        if (channel.GetUser(interessiertePerson.UserId).Roles.Select(r => r.Id).ToList().Any( rid => channel.PermissionOverwrites.Any(p => p.TargetId == rid)))
        {
            return true;
        }
       return false;
    }

    private static void NachrichtenLöschenNachXMinuten(IUserMessage sendTask)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }

    private SocketVoiceChannel HoleChannelMitMeistenUsern(IEnumerable<SocketVoiceChannel> inFrageKommendeVoiceChannel)
    {
        return inFrageKommendeVoiceChannel.OrderByDescending(i => i.Users.Count ).First();
    }

    private bool IstEsZeitBenutzerZuBenachrichtigen(ulong guildId)
    {
        var server = _guilds.FirstOrDefault(g => g.Id == guildId) ;
        if (server != null)
        {
            if (ApplicationState.TestMode)
            {
                server.LastUserConnectedTime = DateTime.Now;
                return true;
            }
            if ( server.LastUserConnectedTime < DateTime.Now - TimeSpan.FromMinutes(ApplicationState.MindestestAnzahlAnMinutenBevorWiederBenachrichtigtWird))
            {
                server.LastUserConnectedTime = DateTime.Now;
                return true;
            }
            
            server.LastUserConnectedTime = DateTime.Now;
        }
        return false;
    }

    private void StartWatchingGuild(SocketGuild guild)
    {
        if(_guilds.Any(g => g.Id == guild.Id))
        {
            _guilds = _guilds.Where(g => g.Id != guild.Id);
        }
        _guilds = _guilds.Append(ErstelleGuild(guild));
    }

    private Guild ErstelleGuild(SocketGuild guild)
    {
        return new Guild
        {
            Name = guild.Name,
            Id = guild.Id,
            Channels = ErstelleChannel(guild.Channels)
        };
    }

    private IEnumerable<Channel> ErstelleChannel(IReadOnlyCollection<SocketGuildChannel> channels)
    {
        foreach (var channel in channels.Where(c => ChannelFilterAfkAndVoiceChannelOnly(c)))
        {
            yield return new Channel
            {
                Name = channel.Name,
                Id = channel.Id,
                MembersActive = ErstelleUser(channel.Users)
            };
        }
    }

    private IEnumerable<ActiveMember> ErstelleUser(IReadOnlyCollection<SocketGuildUser> channelUsers)
    {
        foreach (var user in channelUsers)
        {
            yield return new ActiveMember
            {
                Name = user.DisplayName,
                Id = user.Id,
            };
        }
    }

    private bool GuildIsNotActivelyWatched(SocketGuild guild)
    {
        if (_guilds.Any(g => g.Id == guild.Id))
        {
            return false;
        }

        return true;
    }

    private static bool ChannelFilter(SocketVoiceChannel channel)
    {
        return !channel.Name.ToLower().Contains("afk") &&
               channel.ConnectedUsers.Count > 0;
    }
    
    private static bool ChannelFilterAfkAndVoiceChannelOnly(SocketGuildChannel channel)
    {
        return !channel.Name.ToLower().Contains("afk") && channel.ChannelType == ChannelType.Voice;
    }

    public void StartPeriodicCheck(DiscordSocketClient client)
    {
        if (ApplicationState.TestMode)
        {
            CheckServerChangesAsync(client);
            CheckServerChangesAsync(client);
        }
        else
        {
            _checkTimer = new System.Timers.Timer(60000); // Überprüft alle 60 Sekunden
            _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
            _checkTimer.AutoReset = true;
            _checkTimer.Start();
        }
     
    }
}