using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;

namespace UtilsBot.Datenbank;

public class BotDbContext : DbContext
{
    public DbSet<AllgemeinePerson>  AllgemeinePerson { get; set; }
    public DbSet<Role> Rollen { get; set; }
    public BotDbContext(){}
    
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllgemeinePerson>().ToTable("AllgemeinePerson");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=Data/botdata.sqlite");
        }
    }
       
}