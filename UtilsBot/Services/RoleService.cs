using System.Drawing;
using Discord.Rest;
using Discord.WebSocket;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class RoleService
{
    private readonly ColorService _colorService = new ColorService();
    public async Task<ulong> CreateRole(SocketGuild guild, int level)
    {
        int blue = Math.Max(50, 255 - (level * 4));
        int red = Math.Max(0, 30 + (level * 2));
        int green = Math.Max(0, 60 + (level));
        var colorRGB = _colorService.GetColorFromLevel(level);
        
        // Erzeuge die Discord-Farbe mit den RGB-Werten
        var color = new Discord.Color(colorRGB.Item1, colorRGB.Item2, colorRGB.Item3);
        var role = await guild.CreateRoleAsync(
            name: $"Level {level}",
            permissions: null, // Standard-Berechtigungen
            color: color,       
            isHoisted: false,   // Nicht separat anzeigen
            options: null       // Keine speziellen Optionen
        );
        var maxPosition = guild.CurrentUser.Roles.Max(r => r.Position);
        await role.ModifyAsync(props => {
            props.Position = maxPosition;
        });
        
        return role.Id;
    }
    
    public async Task<RestRole?> GetRoleByIdAsync(ulong roleId, SocketVoiceChannel channel)
    {
        RestRole role = null;
        try
        {
            role = await channel.Guild.GetRoleAsync(roleId);
        }
        catch (Exception ex)
        {
            return null;
        }

        return role;
    }
    
    public async Task DeleteRole(RestRole role)
    {
        await role.DeleteAsync();
    }

    public async Task AssignRoleToUser(ulong roleId, ulong userId, SocketGuild guild)
    {
        var user = guild.GetUser(userId);
        if (user != null)
        {
            await user.AddRoleAsync(roleId);
        }
    }

    public async Task DeleteRoles(List<SocketRole> inActiveRoles)
    {
        foreach (var role in inActiveRoles)
        {
            await role.DeleteAsync();
        }
    }

    public async Task RemoveUnoccupiedRolesFromUser(ulong userId, SocketGuild guild, int userLevel)
    {
        var user = guild.GetUser(userId);
        
        if (user == null)
        {
            return;
        }

        var rolesToBeRemoved = user.Roles.Where(r => !r.Name.ToUpper().Contains($"LEVEL {userLevel.ToString()}") && r.Name.ToUpper().Contains("LEVEL"));
        if (rolesToBeRemoved.Any())
        {
            foreach (var role in rolesToBeRemoved)
            {
                await user.RemoveRoleAsync(role.Id);
                // Wenn keiner mehr in der Rolle ist, lösche die Rolle - <= 1 weil wenn einer rausgeslöscht wurde wurde die Info noch nicht weitergegeben
                if (role.Members.Count() <= 1)
                {
                    await role.DeleteAsync();
                }
            }
        }
    }
    
    public SocketRole? GetRoleAsync(int userLevel, SocketGuild guildId)
    {
        return guildId.Roles.FirstOrDefault(r => r.Name == $"Level {userLevel}");
    }
}