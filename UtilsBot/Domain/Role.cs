namespace UtilsBot.Domain;

public class Role
{
    public string Name { get; set; }
    public ulong Id { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public int Level { get; set; }
}