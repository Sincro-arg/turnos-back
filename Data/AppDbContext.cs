using Microsoft.EntityFrameworkCore;
using Turnos.Api.Models;

namespace Turnos.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Turno> Turnos => Set<Turno>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Turno>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Cliente).IsRequired();
                e.Property(t => t.Telefono).IsRequired();
                e.Property(t => t.Servicio).IsRequired();
                e.Property(t => t.Fecha).IsRequired().HasMaxLength(10);
                e.Property(t => t.Hora).IsRequired().HasMaxLength(5);

                // Para chequear rapido si ya hay un turno en esa fecha+hora.
                e.HasIndex(t => new { t.Fecha, t.Hora });
            });
        }
    }
}
