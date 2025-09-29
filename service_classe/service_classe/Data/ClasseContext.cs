using Microsoft.EntityFrameworkCore;
using service_classe.Models;

namespace service_classe.Data;

public class ClasseContext : DbContext
{
    public ClasseContext(DbContextOptions<ClasseContext> options) : base(options) { }

    public DbSet<Classe> Classes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pour InMemory Database, ToTable() n'est pas nécessaire
        // modelBuilder.Entity<Classe>().ToTable("Classes");
    }
}