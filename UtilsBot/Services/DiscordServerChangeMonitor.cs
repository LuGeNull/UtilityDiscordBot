using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class DiscordServerChangeMonitor
{
    private Timer _checkTimer = new();
    private RoleService _roleService = new ();
    private LevelService _levelService = new ();

    private async Task CheckServerChangesAsync(DiscordSocketClient client)
    {
        await using var db = new DatabaseRepository(new BotDbContext());
        await client.DownloadUsersAsync(client.Guilds);
        foreach (var guild in client.Guilds)
        {
            if(!AnyUserWithLevelRoleAlreadyAndInProduction(guild))
            {
                await GiveAllUsersRoles(guild.Users, db);
            }
            
            foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count >= 1))
            {
                await AddNewUserIfNecessary(channel, db);
                await UpdateInfoUser(channel, db, client);
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task GiveAllUsersRoles(IReadOnlyCollection<SocketGuildUser> guildUsers, DatabaseRepository db)
    {
        var neueUser = guildUsers.Select(c => c.Id)
            .Except(await db.GetUserIdsByGuildIdAsync(guildUsers.First().Guild.Id)).ToList();
        if (neueUser.Any())
        {
            foreach (var user in neueUser)
            {
                var userInQuestion = guildUsers.First(u => u.Id == user);
                await db.AddUserAsync(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
        }
        
        foreach(var guildUser in guildUsers)
        {
            var localUser = await db.GetUserById(guildUser.Id);
            var userLevel = _levelService.BerechneLevelUndRestXp(localUser.Xp);
            //Does the Role Exist in the Database
            var role = _roleService.GetRoleAsync(userLevel, guildUser.Guild);
            await _roleService.RemoveUnoccupiedRolesFromUser(localUser.UserId, guildUser.Guild, userLevel);
        
            if (role == null)
            {
                //If Not Create The Role in Discord and Local And Assign to the User
                var roleId = await _roleService.CreateRole(guildUser.Guild, userLevel);
                //Check if User is in any role which should not be the current role
                await _roleService.AssignRoleToUser(roleId, localUser.UserId, guildUser.Guild);
            }
            else
            { 
                //Does the user have the role assigned ?
                var user = guildUser;
                var doesTheUserHaveTheRoleToBeAssigned = user.Roles.FirstOrDefault(r => r.Id == role.Id) != null;
                if (!doesTheUserHaveTheRoleToBeAssigned)
                {
                    await _roleService.AssignRoleToUser(role.Id, localUser.UserId, guildUser.Guild);
                }
            }
        }
    }

    private static bool AnyUserWithLevelRoleAlreadyAndInProduction(SocketGuild guild)
    {
        return !guild.Users.Any(u => u.Roles.Where(r => r.Name.ToUpper().StartsWith("LEVEL")).Any()) && ApplicationState.ProdToken != null;
    }

    private async Task UpdateInfoUser(SocketVoiceChannel channel, DatabaseRepository db, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var localUser = await db.GetUserById(user.Id);
            if (localUser != null)
            {
                localUser.LastTimeInChannel = DateTime.Now;
                var xpToGain = GetXpToGain(user);
                localUser.GetsSoMuchXpRightNow = xpToGain;
                localUser.Xp += xpToGain;
                await CheckIfRolesNeedToBeAdjusted(localUser, client, channel, db);
                
                await db.SaveChangesAsync();
            }
        }
        await db.SaveChangesAsync();
    }

    private async Task CheckIfRolesNeedToBeAdjusted(AllgemeinePerson localUser, DiscordSocketClient client,
        SocketVoiceChannel channel, DatabaseRepository db)
    {
        var userLevel = _levelService.BerechneLevelUndRestXp(localUser.Xp);
        //Does the Role Exist in the Database
        var role = _roleService.GetRoleAsync(userLevel, channel.Guild);
        await _roleService.RemoveUnoccupiedRolesFromUser(localUser.UserId, channel.Guild, userLevel);
        
        if (role == null)
        {
            //If Not Create The Role in Discord and Local And Assign to the User
            var roleId = await _roleService.CreateRole(channel.Guild, userLevel);
            //Check if User is in any role which should not be the current role
            await _roleService.AssignRoleToUser(roleId, localUser.UserId, channel.Guild);
        }
        else
        { 
           //Does the user have the role assigned ?
           var user = channel.Guild.GetUser(localUser.UserId);
           var doesTheUserHaveTheRoleToBeAssigned = user.Roles.FirstOrDefault(r => r.Id == role.Id) != null;
           if (!doesTheUserHaveTheRoleToBeAssigned)
           {
               await _roleService.AssignRoleToUser(role.Id, localUser.UserId, channel.Guild);
           }
        }
    }

    private int GetXpToGain(SocketGuildUser user)
    {
        if (UserIsStreamingAndVideoingAndNotMutedAndDeafended(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamAndVideoBonus;
        }
        
        if (UserIsStreamingOrVideoingAndNotMutedOrDeafened(user))
        {
            return ApplicationState.BaseXp + ApplicationState.StreamOrVideoBonus;
        }
        else
        {
            if (UserIsFullMute(user))
            {
                return ApplicationState.FullMuteBaseXp;
            }
            else if (MutedNotDeafened(user))
            {
                return ApplicationState.OnlyMuteBaseXp;
            }
            else
            {
                return ApplicationState.BaseXp;
            }
        }
    }

    private static bool MutedNotDeafened(SocketGuildUser user)
    {
        return user.IsSelfMuted && !user.IsSelfDeafened;
    }

    private static bool UserIsFullMute(SocketGuildUser user)
    {
        return user.IsSelfMuted && user.IsSelfDeafened;
    }

    private static bool UserIsStreamingOrVideoingAndNotMutedOrDeafened(SocketGuildUser user)
    {
        return (user.IsStreaming || user.IsVideoing) && !user.IsSelfMuted && !user.IsSelfDeafened;
    }

    private static bool UserIsStreamingAndVideoingAndNotMutedAndDeafended(SocketGuildUser user)
    {
        return user.IsStreaming && user.IsVideoing && !user.IsSelfMuted && !user.IsSelfDeafened;
    }

    private async Task AddNewUserIfNecessary(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var neueUser = channel.ConnectedUsers.Select(c => c.Id)
            .Except(await db.GetUserIdsByGuildIdAsync(channel.Guild.Id)).ToList();
        if (neueUser.Any())
        {
            foreach (var user in neueUser)
            {
                var userInQuestion = channel.ConnectedUsers.First(u => u.Id == user);
                await db.AddUserAsync(userInQuestion.Id, userInQuestion.DisplayName, userInQuestion.Guild.Id);
            }
        }
    }

    public async Task StartPeriodicCheck(DiscordSocketClient client)
    {
        await CheckServerChangesAsync(client);
        _checkTimer = new Timer(ApplicationState.TickPerXSeconds);
        _checkTimer.Elapsed += async (sender, e) => await CheckServerChangesAsync(client);
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
    }
}