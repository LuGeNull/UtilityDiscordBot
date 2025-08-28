using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;
using Timer = System.Timers.Timer;

namespace UtilsBot.Services;

public class DiscordServerChangeMonitor
{
    private Timer _checkTimer;
    private RoleService _roleService = new RoleService();
    private LevelService _levelService = new LevelService();

    public DiscordServerChangeMonitor()
    {
        _checkTimer = new Timer();
    }

    private async Task CheckServerChangesAsync(DiscordSocketClient client)
    {
        await using var db = new DatabaseRepository(new BotDbContext());
        foreach (var guild in client.Guilds)
        {
            // There have to be atleast 1 in one Channel
            foreach (var channel in guild.VoiceChannels.Where(vc => vc.ConnectedUsers.Count > 0))
            {
                await AddNewUserIfNecessary(channel, db);
                if (ApplicationState.DeleteGuildRoles)
                {
                    await DeleteAllRoles(channel, db, client);
                }
                else
                {
                    await UpdateInfoUser(channel, db, client);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task DeleteAllRoles(SocketVoiceChannel channel, DatabaseRepository db, DiscordSocketClient client)
    {
        var roles = await db.GetAllRoles();
        var rolesDiscord = channel.Guild.Roles.Where(r => r.Name.StartsWith("Level")).ToList();
        await _roleService.DeleteRoles(rolesDiscord);
        
        await db.DeleteAllRoles(roles);
        await db.SaveChangesAsync();
    }

   

    private async Task UpdateInfoUser(SocketVoiceChannel channel, DatabaseRepository db, DiscordSocketClient client)
    {
        var connectedUsers = channel.ConnectedUsers;
        foreach (var user in connectedUsers)
        {
            var localUser = await db.GetUserById(user.Id, channel.Guild.Id);
            if (localUser != null)
            {
                localUser.LastTimeInChannel = DateTime.Now;
                var xpToGain = GetXpToGain(user);
                localUser.GetsSoMuchXpRightNow = xpToGain;
                localUser.Xp += xpToGain;
                localUser.Gold += ApplicationState.DefaultGoldEarning;
                await CheckIfRolesNeedToBeAdjusted(localUser, client, channel, db);
                
                await db.SaveChangesAsync();
            }
        }
        await db.SaveChangesAsync();
    }

    private async Task CheckIfRolesNeedToBeAdjusted(AllgemeinePerson localUser, DiscordSocketClient client,
        SocketVoiceChannel channel, DatabaseRepository db)
    {
        if (channel.Guild.Id != 1391414100298436698)
        {
            return;
        }
        var userLevel = _levelService.BerechneLevelUndRestXp(localUser.Xp);
        //Does the Role Exist in the Database
        var role = await db.GetRoleAsync(userLevel);
        if (role == null )
        {
            //If Not Create The Role in Discord and Local And Assign to the User
            var roleId = await _roleService.CreateRole(client, channel, db, userLevel);
            await db.AddRoleAsync(roleId, channel.Id, userLevel, channel.Guild.Id);
            await _roleService.AssignRoleToUser(roleId, localUser.UserId, channel);
            localUser.RoleId = roleId;
            await db.SaveChangesAsync();
        }
        else
        {
           //Does the role still exist on the server
           var roleDiscord = await _roleService.GetRoleByIdAsync(role.Id, channel);
           var roleId = 0ul;
           if (roleDiscord == null)
           {
               roleId = await _roleService.CreateRole(client, channel, db, userLevel);
               localUser.RoleId = roleId;
               await db.SaveChangesAsync();
           }
           else
           {
               roleId = roleDiscord.Id;
           }
           
           //Does the user have the role assigned ? 
           var user = channel.Guild.GetUser(localUser.UserId);
           var roleWhichShouldBeAssigned = user.Roles.FirstOrDefault(r => r.Id == roleId);
           if (roleWhichShouldBeAssigned == null)
           {
               await _roleService.AssignRoleToUser(roleId, localUser.UserId, channel);
               localUser.RoleId = roleId;
               await db.SaveChangesAsync();
           }
        }

        await CleanUpRolesIfNecessary(channel, db);
    }

    private async Task CleanUpRolesIfNecessary(SocketVoiceChannel channel, DatabaseRepository db)
    {
        var activeRoleIds = await db.GetActiveRoleIdsByGuildIdAsync(channel.Guild.Id);
        var inactiveRoles = await db.GetInactiveRoleIds(activeRoleIds);
        
        if (inactiveRoles.Any())
        {
            var InActiveRoles = channel.Guild.Roles.Where(r => inactiveRoles.Contains(r.Id)).ToList();
            await db.RemoveInactiveRoles(inactiveRoles);
            await _roleService.DeleteRoles(InActiveRoles);
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