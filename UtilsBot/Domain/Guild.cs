namespace UtilsBot;

public class Guild
{
    public string Name { get; set; }
    public ulong Id { get; set; }
    public IEnumerable<Channel> Channels { get; set; }
    
    public DateTime LastUserConnectedTime { get; set; }
}
