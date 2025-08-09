using Microsoft.EntityFrameworkCore;
using UtilsBot.Datenbank;
using UtilsBot.Domain.BetRequest;
using UtilsBot.Domain.BetStart;
using UtilsBot.Domain.ValueObjects;
using UtilsBot.Repository;
using UtilsBot.Services;

namespace UtilsBotTest.ServicesTest;

[TestClass]
public class BetServiceTest
{
    private BetService _betService;
    private DatabaseRepository _db;

    [TestInitialize]
    public void Setup()
    {
        var dbName = Guid.NewGuid().ToString(); // Einzigartiger Name pro Test
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new BotDbContext(options);
        var db = new DatabaseRepository(context);
        _db = db;
        _betService = new BetService();
    }

    [TestMethod("Bet wird angelegt")]
    public async Task T1()
    {
        var userId = 1u;
        var guildId = 3u;
        var title = "Wer wird gewinnen?";
        var annahmeschlussAbJetztInStunden = 1;
        var messageId = 4u;
        var channelId = 5u;
        var ereignis1Name = "Deutschland";
        var ereignis2Name = "Spanien";
        var maxPayoutMultiplikator = 3;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        await _betService.HandleMessageAsync(betStartRequest, _db);
        Assert.IsTrue(await _db.GetBetAndPlacementsByMessageId(messageId) != null);
    }

    [TestMethod("Bet wird mit richtigen Werten angelegt")]
    public async Task T2()
    {
        var userId = 1u;
        var guildId = 3u;
        var title = "Wer wird gewinnen?";
        var annahmeschlussAbJetztInStunden = 1;
        var messageId = 4u;
        var channelId = 5u;
        var ereignis1Name = "Deutschland";
        var ereignis2Name = "Spanien";
        var maxPayoutMultiplikator = 3;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        var betStartResponse = await _betService.HandleMessageAsync(betStartRequest, _db);
        var bet = await _db.GetBetAndPlacementsByMessageId(messageId);
        Assert.IsTrue(betStartResponse.anfrageWarErfolgreich);
        Assert.IsTrue(bet.UserIdStartedBet == betStartRequest.userIdStartedBet);
        Assert.IsTrue(bet.MessageId == betStartRequest.messageId);
    }

    public async Task LegeWetteAn(ulong userId = 1u, ulong guildId = 3u, ulong messageId = 4u)
    {
        var title = "Wer wird gewinnen?";
        var annahmeschlussAbJetztInStunden = 1;
        var channelId = 5u;
        var ereignis1Name = "Deutschland";
        var ereignis2Name = "Spanien";
        var maxPayoutMultiplikator = 3;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        await _betService.HandleMessageAsync(betStartRequest, _db);
    }
    [TestMethod("User will auf eine Wette wetten, es existiert aber keine entsprechende Wette")]
    public async Task T3()
    {
        var userId = 1u;
        var guildId = 3u;
        var messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        var betRequest = new BetRequest(messageId, userId, 100, WettOption.Ja) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.existiertEineBet);
        Assert.IsFalse(betResponse.anfrageWarErfolgreich);
    }
    
    [TestMethod("User will auf eine Wette wetten, er hat aber nicht genug Xp")]
    public async Task T4()
    {
        var userId = 1u;
        var guildId = 3u;
        var messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        await LegeWetteAn(userId, guildId, messageId);
        var betRequest = new BetRequest(messageId, userId, 100, WettOption.Ja) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.anfrageWarErfolgreich);
        Assert.IsFalse(betResponse.userHatGenugXp);
    }

    [TestMethod("User will auf eine Wette wetten, er hat genug Xp aber wette ist bereits vorbei")]
    public async Task T5()
    {
        var userId = 1u;
        var guildId = 3u;
        var messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        var person = await _db.HoleAllgemeinePersonMitIdAsync(userId);
        person.Xp = 100;
        await _db.SaveChangesAsync();
        await LegeWetteAn(userId, guildId, messageId);
        
        var betRequest = new BetRequest(messageId, userId, 100, WettOption.Ja) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.anfrageWarErfolgreich);
        Assert.IsFalse(betResponse.userHatGenugXp);
    }
    //[TestMethod("User will auf eine Wette wetten, es existieren aber keine Wetten")]
    //public async Task T3()
    //{
    //    //var betStartRequest = new BetStartRequest(userIdStartedBet:3, guildId:4, title:"test3", annahmeschlussAbJetztInMinuten: 5, messageId:6, channelId:9);
    //    //await _betService.HandleMessageAsync(betRequest);
//
    //    _db.AllgemeinePerson.Add(new AllgemeinePerson()
    //    {
    //        UserId = 10,
    //        DisplayName = "TestUser",
    //        GuildId = 2,
    //        Xp = 1000,
    //        XpTodayByMessages = 0,
    //        LastXpGainDate = default,
    //        BekommtZurzeitSoVielXp = 0,
    //        ZuletztImChannel = default
    //    });
    //    await _db.SaveChangesAsync();
    //    var betRequest = new BetRequest(channelId: 0, guildId: 2, userId: 10, einsatz: 10, zeitJetzt: DateTime.Now,
    //        wettId: 1);
    //    var response = await _betService.HandleMessageAsync(betRequest);
    //    Assert.IsTrue(!response.existiertEineBet);
    //}
//
    //[TestMethod("User will auf eine Wette wetten, es existiert 1 Wette, aber User hat nicht genug XP")]
    //public async Task T4()
    //{
    //    var betStartRequest = new BetStartRequest(userIdStartedBet: 3, guildId: 4, title: "test3",
    //        annahmeschlussAbJetztInMinuten: 5, messageId: 6, channelId: 9);
    //    await _betService.HandleMessageAsync(betStartRequest);
//
    //    _db.AllgemeinePerson.Add(new AllgemeinePerson()
    //    {
    //        UserId = 10,
    //        DisplayName = "TestUser",
    //        GuildId = 4,
    //        Xp = 1000,
    //        XpTodayByMessages = 0,
    //        LastXpGainDate = default,
    //        BekommtZurzeitSoVielXp = 0,
    //        ZuletztImChannel = default
    //    });
    //    await _db.SaveChangesAsync();
    //    var betRequest = new BetRequest(channelId: 9, guildId: 4, userId: 10, einsatz: 1000, zeitJetzt: DateTime.Now,
    //        wettId: 5);
    //    var response = await _betService.HandleMessageAsync(betRequest);
    //    Assert.IsTrue(!response.userHatGenugXp);
    //}
}