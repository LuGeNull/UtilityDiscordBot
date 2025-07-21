using Discord.WebSocket;
using FluentAssertions;
using UtilsBot.Repository;
using UtilsBot.Services;

namespace UtilsBot.ServiceTest;
[TestClass]
public class VoiceChannelChangeListenerTest
{

    public VoiceChannelChangeListenerService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _service = new VoiceChannelChangeListenerService(new DatabaseRepository());
        InitializeMembers();
    }

    private void InitializeMembers()
    {
    }

    [TestMethod]
    public void A()
    {
    }
}