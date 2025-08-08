//using Microsoft.EntityFrameworkCore;
//using UtilsBot;
//using UtilsBot.Datenbank;
//using UtilsBot.Repository;
//using UtilsBot.Request;
//using UtilsBot.Services;
//
//namespace UtilsBotTest.ServicesTest;
//[TestClass]
//public class BetServiceTest
//{
//    private BetService _betService;
//    private BotDbContext _db;
//
//    [TestInitialize]
//    public void Setup()
//    {
//        var dbName = Guid.NewGuid().ToString(); // Einzigartiger Name pro Test
//        var options = new DbContextOptionsBuilder<BotDbContext>()
//            .UseInMemoryDatabase(databaseName: dbName)
//            .Options;
//        var context = new BotDbContext(options);
//        var db = new DatabaseRepository(context);
//        _db = context;
//        _betService = new BetService( db);
//    }
//    
//    [TestMethod("Bet wird angelegt")]
//    public async Task T1()
//    {
//        var betStartRequest = new BetStartRequest(userIdStartedBet: 1, guildId: 2, title: "test", annahmeschlussAbJetztInMinuten: 2, messageId: 1, channelId: 2);
//        await _betService.HandleMessageAsync(betStartRequest);
//        Assert.IsTrue(_db.Bet.Count().Equals(1));
//    }
//    
//    [TestMethod("Bet wird mit richtigen Werten angelegt")]
//    public async Task T2()
//    {
//        var betStartRequest = new BetStartRequest(userIdStartedBet: 2, guildId: 3, title: "test2", annahmeschlussAbJetztInMinuten: 5, messageId: 2, channelId: 3);
//        await _betService.HandleMessageAsync(betStartRequest);
//        Assert.IsTrue(_db.Bet.Any(b => b.UserIdStartedBet == betStartRequest.userIdStartedBet));
//        Assert.IsTrue(_db.Bet.Any(b => b.MessageId == betStartRequest.messageId));
//    }
//    
//    [TestMethod("User will auf eine Wette wetten, es existieren aber keine Wetten")]
//    public async Task T3()
//    {
//        //var betStartRequest = new BetStartRequest(userIdStartedBet:3, guildId:4, title:"test3", annahmeschlussAbJetztInMinuten: 5, messageId:6, channelId:9);
//        //await _betService.HandleMessageAsync(betRequest);
//        
//        _db.AllgemeinePerson.Add(new AllgemeinePerson()
//        {
//            UserId = 10,
//            DisplayName = "TestUser",
//            GuildId = 2,
//            Xp = 1000,
//            XpTodayByMessages = 0,
//            LastXpGainDate = default,
//            BekommtZurzeitSoVielXp = 0,
//            ZuletztImChannel = default
//        });
//        await _db.SaveChangesAsync();
//        var betRequest = new BetRequest(channelId: 0, guildId: 2, userId: 10, einsatz: 10, zeitJetzt: DateTime.Now, wettId: 1);
//        var response = await _betService.HandleMessageAsync(betRequest);
//        Assert.IsTrue(!response.existiertEineBet);
//
//    }
//    
//    [TestMethod("User will auf eine Wette wetten, es existiert 1 Wette, aber User hat nicht genug XP")]
//    public async Task T4()
//    {
//        var betStartRequest = new BetStartRequest(userIdStartedBet: 3, guildId: 4, title: "test3", annahmeschlussAbJetztInMinuten: 5, messageId: 6, channelId: 9);
//        await _betService.HandleMessageAsync(betStartRequest);
//        
//        _db.AllgemeinePerson.Add(new AllgemeinePerson()
//        {
//            UserId = 10,
//            DisplayName = "TestUser",
//            GuildId = 4,
//            Xp = 1000,
//            XpTodayByMessages = 0,
//            LastXpGainDate = default,
//            BekommtZurzeitSoVielXp = 0,
//            ZuletztImChannel = default
//        });
//        await _db.SaveChangesAsync();
//        var betRequest = new BetRequest(channelId: 9, guildId: 4, userId: 10, einsatz: 1000, zeitJetzt: DateTime.Now, wettId: 5);
//        var response = await _betService.HandleMessageAsync(betRequest);
//        Assert.IsTrue(!response.userHatGenugXp);
//
//    }
//}
//