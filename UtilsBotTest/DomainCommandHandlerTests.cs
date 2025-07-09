using Moq;
using UtilsBot.Domain;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;
using Xunit;
using Assert = Xunit.Assert;

namespace UtilsBotTest;

public class DomainCommandHandlerTests
{
    private const string TestUserName = "TestUser";
    
    [Fact]
    public async Task InterestedAsync_ValidInput_CallsRepositoryWithCorrectValues()
    {
        var mockRepo = new Mock<IBotRepository>();
        var handler = new DomainCommandHandler(mockRepo.Object);

        var request = new InterestedRequest(0, 2, 10, TestUserName, 20);

        await handler.InterestedAsync(request);

        mockRepo.Verify(r => r.AddUserToInterestedListAsync(10, TestUserName, 20, 0, 2), Times.Once);
    }

    [Fact]
    public async Task InterestedAsync_SameVonUndBis_SetsRangeToFullDay()
    {
        var mockRepo = new Mock<IBotRepository>();
        var handler = new DomainCommandHandler(mockRepo.Object);

        var request = new InterestedRequest(0, 24, 10, TestUserName, 20);

        await handler.InterestedAsync(request);

        mockRepo.Verify(r => r.AddUserToInterestedListAsync(10, TestUserName, 20, 0, 24), Times.Once);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(5, 25)]
    [InlineData(30, -3)]
    public async Task InterestedAsync_InvalidRange_ThrowsArgumentException(int von, int bis)
    {
        var mockRepo = new Mock<IBotRepository>();
        var handler = new DomainCommandHandler(mockRepo.Object);

        var request = new InterestedRequest(von, bis, 10, TestUserName, 20);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.InterestedAsync(request));
    }
}