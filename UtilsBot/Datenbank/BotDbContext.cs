using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;

namespace UtilsBot.Datenbank;

public class BotDbContext : DbContext
{
    public DbSet<AllgemeinePerson> AllgemeinePersonen { get; set; }
    public DbSet<WarEraContract> WarEraContracts { get; set; }
    public DbSet<WarEraContractMessage> WarEraContractMessages { get; set; }
    public DbSet<WarEraSubscription> WarEraSubscriptions { get; set; }
    public DbSet<BotSetting> BotSettings { get; set; }
    
    public BotDbContext(){}
    
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllgemeinePerson>().HasKey(p => new { p.UserId, p.GuildId });
        modelBuilder.Entity<WarEraContract>().HasKey(c => c.ContractId);
        modelBuilder.Entity<WarEraContractMessage>().HasKey(m => new { m.ContractId, m.ChannelId });
        modelBuilder.Entity<WarEraSubscription>().HasKey(s => s.GuildId);
        modelBuilder.Entity<BotSetting>().HasKey(s => s.Key);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=Data/botdata.sqlite");
        }
    }
       
}