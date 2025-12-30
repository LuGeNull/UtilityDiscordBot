using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;

namespace UtilsBot.Datenbank;

public class BotDbContext : DbContext
{
    public DbSet<AllgemeinePerson> AllgemeinePersonen { get; set; }
    
    public BotDbContext(){}
    
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllgemeinePerson>().HasKey(p => new { p.UserId, p.GuildId });;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=Data/botdata.sqlite");
        }
    }
       
}