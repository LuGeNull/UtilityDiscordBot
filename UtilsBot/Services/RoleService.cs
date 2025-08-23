using System.Drawing;
using Discord.Rest;
using Discord.WebSocket;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class RoleService
{
    private readonly ColorService _colorService = new ColorService();
    public async Task<ulong> CreateRole(DiscordSocketClient client, SocketVoiceChannel channel, DatabaseRepository db, int level)
    {
        var guild = channel.Guild;
        int blue = Math.Max(50, 255 - (level * 4));
        int red = Math.Max(0, 30 + (level * 2));
        int green = Math.Max(0, 60 + (level));
        var colorRGB = _colorService.GetColorFromLevel(level);
        
        // Erzeuge die Discord-Farbe mit den RGB-Werten
        var color = new Discord.Color(colorRGB.Item1, colorRGB.Item2, colorRGB.Item3);
        var role = await guild.CreateRoleAsync(
            name: $"Level {level}",
            permissions: null, // Standard-Berechtigungen
            color: color,        // Standard-Farbe
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

    public async Task AssignRoleToUser(ulong roleId, ulong userId, SocketVoiceChannel channel)
    {
        var user = channel.Guild.GetUser(userId);
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
}