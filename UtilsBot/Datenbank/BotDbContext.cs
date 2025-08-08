using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;

namespace UtilsBot.Datenbank;

public class BotDbContext : DbContext
{
    public DbSet<AllgemeinePerson>  AllgemeinePerson { get; set; }
    public DbSet<Bet> Bet { get; set; }
    public DbSet<BetPlacements> Placements { get; set; }
    public BotDbContext(){}
    
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllgemeinePerson>().ToTable("AllgemeinePerson");
        modelBuilder.Entity<BetPlacements>()
            .HasOne(bp => bp.Bet)
            .WithMany(b => b.Placements)
            .HasForeignKey(bp => bp.BetId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=Data/botdata.sqlite");
        }
    }
       
}