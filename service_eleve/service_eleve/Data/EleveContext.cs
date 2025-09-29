using Microsoft.EntityFrameworkCore;
using service_eleve.Models;

namespace service_eleve.Data;

public class EleveContext : DbContext
{
    public EleveContext(DbContextOptions<EleveContext> options) : base(options) { }

    public DbSet<Eleve> Eleves { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pour InMemory Database, ToTable() n'est pas nécessaire
        // modelBuilder.Entity<Eleve>().ToTable("Eleves");
    }
}