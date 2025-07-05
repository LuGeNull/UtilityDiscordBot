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
        var person = new AllgemeinePerson(0, "test", 100)
        {
            WillBenachrichtigungenBekommen = true,
            Xp = 10,
            ZuletztImChannel = new DateTime(2024,01,01,0,0,0),
            BenachrichtigenZeitVon = 0,
            BenachrichtigenZeitBis = 24
        };
        _service._database._personen.Add(person);
        
        var person2 = new AllgemeinePerson(1, "test2", 100)
        {
            WillBenachrichtigungenBekommen = true,
            Xp = 10,
            ZuletztImChannel = new DateTime(2024,01,01,0,0,0),
            BenachrichtigenZeitVon = 0,
            BenachrichtigenZeitBis = 24
        };
        _service._database._personen.Add(person);
       
    }

    [TestMethod]
    public void A()
    {
        _service._database._personen.Count.Should().Be(2);
    }
}