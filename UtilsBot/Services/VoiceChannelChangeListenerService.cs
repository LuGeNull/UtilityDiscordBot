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
    private const int MinutenAbstandVorBenachrichtigung = 30;
    public VoiceChannelChangeListenerService(DatabaseRepository database, Timer checkTimer)
    {
        _database = database;
        _checkTimer = checkTimer;
        _guilds = new List<Guild>();
    }

    public void AddUserToInterestedPeopleList(ulong userId, ulong guildId, int nichtbenachrichtigungsZeitraumVon, int nichtbenachrichtigungsZeitraumBis)
    {
        _database.AddInterestedPeople(new InterestedPeople(userId,guildId, nichtbenachrichtigungsZeitraumVon,nichtbenachrichtigungsZeitraumBis));
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

    private async Task NachrichtenVerschicken(List<InterestedPeople> interesiertePersonen,
        List<SocketVoiceChannel> inFrageKommendeVoiceChannel, DiscordSocketClient client)
    {
        foreach (var interessiertePerson in interesiertePersonen)
        {
            var channelMitMeistenUsern = inFrageKommendeVoiceChannel.Where(c => c.Users.Select(u => u.Id).Contains(interessiertePerson.UserId)).OrderByDescending(c => c.ConnectedUsers).FirstOrDefault();
            if (channelMitMeistenUsern == null)
            {
                continue;
            } 
            var sendTask = await client.GetUser(interessiertePerson.UserId).SendMessageAsync($"Auf dem Server {channelMitMeistenUsern.Guild.Name} im Channel {channelMitMeistenUsern.Name} geht was ab!");
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
            });
        }
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
            return true;
            if ( server.LastUserConnectedTime < DateTime.Now - TimeSpan.FromMinutes(MinutenAbstandVorBenachrichtigung))
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
    {   //var a  = CheckServerChangesAsync(client);
       _checkTimer = new System.Timers.Timer(5000); // Überprüft alle 5 Sekunden
       _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
       _checkTimer.AutoReset = true;
       _checkTimer.Start();
    }
}