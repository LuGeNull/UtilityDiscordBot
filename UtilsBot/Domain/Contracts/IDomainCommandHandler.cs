using UtilsBot.Domain.Models;

namespace UtilsBot.Domain.Contracts;

public interface IDomainCommandHandler
{
    Task<InfoResponseDto> InfoAsync(InfoRequestDto request);
    Task InterestedAsync(InterestedRequest request);
}