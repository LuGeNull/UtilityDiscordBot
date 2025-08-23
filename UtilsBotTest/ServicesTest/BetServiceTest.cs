using Microsoft.EntityFrameworkCore;
using UtilsBot.Datenbank;
using UtilsBot.Domain.BetCancel;
using UtilsBot.Domain.BetClose;
using UtilsBot.Domain.BetPayout;
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
    
    [TestMethod("Bet can be created")]
    public async Task T1()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const string title = "Wer wird gewinnen?";
        const int annahmeschlussAbJetztInStunden = 1;
        const ulong messageId = 4u;
        const ulong channelId = 5u;
        const string ereignis1Name = "Deutschland";
        const string ereignis2Name = "Spanien";
        var maxPayoutMultiplikator = 3;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        await _betService.HandleMessageAsync(betStartRequest, _db);
        Assert.IsTrue(await _db.GetBetAndPlacementsByMessageId(messageId) != null);
    }

    [TestMethod("Bet is created with correct values")]
    public async Task T2()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const string title = "Wer wird gewinnen?";
        const int annahmeschlussAbJetztInStunden = 1;
        const ulong messageId = 4u;
        const ulong channelId = 5u;
        const string ereignis1Name = "Deutschland";
        const string ereignis2Name = "Spanien";
        const int maxPayoutMultiplikator = 3;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        var betStartResponse = await _betService.HandleMessageAsync(betStartRequest, _db);
        var bet = await _db.GetBetAndPlacementsByMessageId(messageId);
        Assert.IsTrue(betStartResponse.anfrageWarErfolgreich);
        Assert.IsTrue(bet.UserIdStartedBet == betStartRequest.userIdStartedBet);
        Assert.IsTrue(bet.MessageId == betStartRequest.messageId);
    }

    public async Task CreateBet(ulong userId = 1u, ulong guildId = 3u, ulong messageId = 4u, int payoutMultiplikator = 3)
    {
        const string title = "Wer wird gewinnen?";
        const int annahmeschlussAbJetztInStunden = 1;
        const ulong channelId = 5u;
        const string ereignis1Name = "Deutschland";
        const string ereignis2Name = "Spanien";
        var maxPayoutMultiplikator = payoutMultiplikator;
        var betStartRequest = new BetStartRequest(userId, guildId,title, annahmeschlussAbJetztInStunden, messageId, channelId, ereignis1Name,ereignis2Name, maxPayoutMultiplikator);
        await _betService.HandleMessageAsync(betStartRequest, _db);
    }
    [TestMethod("User wants to bet on a bet but there is no bet")]
    public async Task T3()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        var betRequest = new BetRequest(messageId, userId, 100, BetSide.Yes) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.existiertEineBet);
        Assert.IsFalse(betResponse.requestWasSuccesful);
    }
    
    [TestMethod("User wants to bet on a bet but has not enough Gold")]
    public async Task T4()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        await CreateBet(userId, guildId, messageId);
        var betRequest = new BetRequest(messageId, userId, 100, BetSide.Yes) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.requestWasSuccesful);
        Assert.IsFalse(betResponse.userHatGenugGold);
    }

    [TestMethod("User bets on a bet but the betting is already closed")]
    public async Task T5()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        var person = await _db.GetUserById(userId);
        person!.Gold = 100;
        await _db.SaveChangesAsync();
        await CreateBet(userId, guildId, messageId);
        var wette = await _db.GetBet(messageId);
        wette!.EndedAt = DateTime.Now.AddHours(-1);
        await _db.SaveChangesAsync();
        var betRequest = new BetRequest(messageId, userId, 101, BetSide.Yes) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsFalse(betResponse.requestWasSuccesful);
        Assert.IsTrue(betResponse.BetIsAlreadyClosed);
    }
    
    [TestMethod("User bets on a bet and its working")]
    public async Task T6()
    {
        const ulong userId = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId, "TestUser", guildId);
        var person = await _db.GetUserById(userId);
        person!.Gold = 100;
        await _db.SaveChangesAsync();
        await CreateBet(userId, guildId, messageId);
        var betRequest = new BetRequest(messageId, userId, 100, BetSide.Yes) ;
        var betResponse = await _betService.HandleMessageAsync(betRequest, _db);
        Assert.IsTrue(betResponse.requestWasSuccesful);
        var wette = await _db.GetBetAndPlacementsByMessageId(messageId);
        
        Assert.IsTrue(person.Gold == 0);
        Assert.IsTrue(wette != null);
        Assert.IsFalse(wette.WetteWurdeBeendet);
        Assert.IsFalse(wette.WetteWurdeAbgebrochen);
        Assert.IsTrue(wette.MaxPayoutMultiplikator == 3);
        Assert.IsTrue(wette.Placements.Count == 1);
        Assert.IsTrue(wette.Placements.First().UserId == userId);
        Assert.IsTrue(wette.Placements.First().betAmount == 100);
        Assert.IsTrue(wette.Placements.First().Site);
        Assert.IsTrue(wette.Placements.First().DisplayName == "TestUser");
    }
    
    [TestMethod("User erstellt eine Wette und anderer user will sie schließen")]
    public async Task T7()
    {
        const ulong userId1 = 1u;
        const ulong userId2 = 2u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await _db.AddUserAsync(userId2, "TestUser2", guildId);
        
        await _db.SaveChangesAsync();
        
        await CreateBet(userId1, guildId, messageId);
        var istUserErstellerDerWette = await _betService.IsThisUserCreatorOfBet(userId2, messageId, _db);
        Assert.IsFalse(istUserErstellerDerWette);
        
    }
    
    [TestMethod("User erstellt eine Wette und ist auch Ersteller dieser")]
    public async Task T8()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);
        var istUserErstellerDerWette = await _betService.IsThisUserCreatorOfBet(userId1, messageId, _db);
        Assert.IsTrue(istUserErstellerDerWette);
    }
    
    [TestMethod("User erstellt eine Wette und bricht diese ab aber diese ist noch nicht geschlossen")]
    public async Task T9()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);
        var betCancelRequest = new BetCancelRequest(messageId);
        var betCancelResponse = await _betService.HandleMessageAsync(betCancelRequest, _db);
        Assert.IsFalse(betCancelResponse.anfrageWarErfolgreich);
        Assert.IsTrue(betCancelResponse.wetteIstNichtZuende);

    }
    
    [TestMethod("User erstellt eine Wette und schließt die Wettannahmen")]
    public async Task T10()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);

        await _betService.HandleMessageAsync(new BetCloseRequest(messageId, _db));
        var bet = await _db.GetBet(messageId);
        Assert.IsTrue(bet.EndedAt < DateTime.Now);

    }
    
    [TestMethod("User erstellt eine Wette und schließt die Wettannahmen")]
    public async Task T11()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);
        var bet = await _db.GetBet(messageId);
        Assert.IsFalse(bet!.EndedAt < DateTime.Now);
        await _betService.HandleMessageAsync(new BetCloseRequest(messageId, _db));
        Assert.IsTrue(bet.EndedAt < DateTime.Now);

    }
    
    [TestMethod("User erstellt eine Wette und schließt die Wettannahmen und darf nicht mehr drauf bieten")]
    public async Task T12()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);
        //var bet = await _db.GetBet(messageId);
        await _betService.HandleMessageAsync(new BetCloseRequest(messageId, _db));
        var betResponse = await _betService.HandleMessageAsync(new BetRequest(messageId,userId1,1, BetSide.Yes), _db);
        Assert.IsFalse(betResponse.requestWasSuccesful);
        Assert.IsTrue(betResponse.BetIsAlreadyClosed);
    }
    
    [TestMethod("User creates bet but cant bet on both sides")]
    public async Task T13()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        await CreateBet(userId1, guildId, messageId);
        var user = await _db.GetUserById(userId1);
        user!.Gold = 2;
        await _db.SaveChangesAsync();
        var betResponse1 = await _betService.HandleMessageAsync(new BetRequest(messageId,userId1,1, BetSide.Yes), _db);
        var betResponse2 = await _betService.HandleMessageAsync(new BetRequest(messageId,userId1,1, BetSide.No), _db);
        
        Assert.IsTrue(betResponse1.requestWasSuccesful);
        Assert.IsFalse(betResponse2.requestWasSuccesful);
        Assert.IsTrue(betResponse2.userBetsOnBothSides);
    }
    
    [TestMethod("User creates bet and cancels it so he should get refunded")]
    public async Task T14()
    {
        const ulong userId1 = 1u;
        const ulong guildId = 3u;
        const ulong messageId = 4u;
        await _db.AddUserAsync(userId1, "TestUser", guildId);
        const ulong userId2 = 2u;
        await _db.AddUserAsync(userId2, "TestUser2", guildId);
        
        await CreateBet(userId1, guildId, messageId);
        var user = await _db.GetUserById(userId1);
        user!.Gold = 2;
        
        var user2 = await _db.GetUserById(userId2);
        user2!.Gold = 5;
        
        await _db.SaveChangesAsync();
        
        await _betService.HandleMessageAsync(new BetRequest(messageId,userId1,1, BetSide.Yes), _db);
        await _betService.HandleMessageAsync(new BetRequest(messageId,userId2,1, BetSide.No), _db);
        
        Assert.IsTrue(user.Gold == 1);
        Assert.IsTrue(user2.Gold == 4);
        await _betService.HandleMessageAsync(new BetCloseRequest(messageId, _db));
        await _betService.HandleMessageAsync(new BetCancelRequest(messageId), _db);
        
        Assert.IsTrue(user.Gold == 2);
        Assert.IsTrue(user2.Gold == 5);
    }
    
    [TestMethod("User creates bet and 2 players bet on it and win is calculated correctly")]
    public async Task T15()
    {
        const ulong userId1 = 1u;
        const ulong userId2 = 2u;
        
        const ulong guildId = 3u;
        const ulong messageIdBet = 4u;
        
        await _db.AddUserAsync(userId1, "TestUser", 3u);
        await _db.AddUserAsync(userId2, "TestUser2", 3u);
        
        await CreateBet(userId1, guildId, messageIdBet);
        var user = await _db.GetUserById(userId1);
        user!.Gold = 100;
        
        var user2 = await _db.GetUserById(userId2);
        user2!.Gold = 500;
        
        await _db.SaveChangesAsync();
        
        await _betService.HandleMessageAsync(new BetRequest(messageIdBet,userId1,100, BetSide.Yes), _db);
        await _betService.HandleMessageAsync(new BetRequest(messageIdBet,userId2,100, BetSide.No), _db);
        
        await _betService.HandleMessageAsync(new BetCloseRequest(messageIdBet, _db));
        await _betService.HandleMessageAsync(new BetPayoutRequest(messageIdBet, BetSide.Yes), _db);
        
        Assert.IsTrue(user.Gold == 200);
        Assert.IsTrue(user2.Gold == 400);
    }
    
    [TestMethod("User creates bet and 3 players bet on it and win is calculated correctly and losing team gets refunded the balance over max payout")]
    public async Task T16()
    {
        const ulong userId1 = 1u;
        const ulong userId2 = 2u;
        const ulong userId3 = 3u;
        
        const ulong guildId = 3u;
        const ulong messageIdBet = 4u;
        
        await _db.AddUserAsync(userId1, "TestUser", 3u);
        await _db.AddUserAsync(userId2, "TestUser2", 3u);
        await _db.AddUserAsync(userId3, "TestUser3", 3u);
        
        await CreateBet(userId1, guildId, messageIdBet);
        var user = await _db.GetUserById(userId1);
        user!.Gold = 100;
        
        var user2 = await _db.GetUserById(userId2);
        user2!.Gold = 500;
        
        var user3 = await _db.GetUserById(userId3);
        user3!.Gold = 500;
        
        await _db.SaveChangesAsync();
        
        await _betService.HandleMessageAsync(new BetRequest(messageIdBet,userId1,100, BetSide.Yes), _db);
        await _betService.HandleMessageAsync(new BetRequest(messageIdBet,userId2,100, BetSide.No), _db);
        await _betService.HandleMessageAsync(new BetRequest(messageIdBet,userId3,200, BetSide.No), _db);
        
        await _betService.HandleMessageAsync(new BetCloseRequest(messageIdBet, _db));
        await _betService.HandleMessageAsync(new BetPayoutRequest(messageIdBet, BetSide.Yes), _db);
        
        Assert.IsTrue(user.Gold == 300);
        Assert.IsTrue(user2.Gold == 433);
        Assert.IsTrue(user3.Gold == 367);

        var bet = await _db.GetBetAndPlacementsByMessageId(messageIdBet);
        Assert.IsTrue(bet.Placements.First(p => p.UserId == 1u).GoldWon == 200);
        
        Assert.IsTrue(bet.Placements.First(p => p.UserId == 2u).GoldWon == 0);
        Assert.IsTrue(bet.Placements.First(p => p.UserId == 3u).GoldWon == 0);
        
        Assert.IsTrue(bet.Placements.First(p => p.UserId == 2u).GoldRefunded == 33);
        Assert.IsTrue(bet.Placements.First(p => p.UserId == 3u).GoldRefunded == 67);

    }
    
}