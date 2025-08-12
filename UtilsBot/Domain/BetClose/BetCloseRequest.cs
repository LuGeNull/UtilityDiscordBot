using Microsoft.EntityFrameworkCore;
using UtilsBot.Repository;

namespace UtilsBot.Domain.BetClose;

public record BetCloseRequest(ulong messageId, DatabaseRepository db);