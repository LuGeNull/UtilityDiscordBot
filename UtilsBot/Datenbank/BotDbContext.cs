using Microsoft.EntityFrameworkCore;
using UtilsBot.Domain;
using UtilsBot.Domain.Models;

namespace UtilsBot.Datenbank;

public class BotDbContext : DbContext
{
    public DbSet<AllgemeinePerson>  AllgemeinePerson { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllgemeinePerson>().ToTable("AllgemeinePerson");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=Data/botdata.sqlite");
}