namespace UtilsBot;

public class Channel
{
    public string Name { get; set; }
    public ulong Id { get; set; }
    public IEnumerable<ActiveMember> MembersActive { get; set; }
    public int MemberCount => MembersActive.Count();
}

