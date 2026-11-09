using Microsoft.EntityFrameworkCore;
using TechSupport.API.Models;
using TechSupport.Contracts;
namespace TechSupport.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Usuario>().HasIndex(x => x.NombreUsuario).IsUnique();
        model.Entity<Usuario>().Property(x => x.NombreUsuario).HasMaxLength(50);
        model.Entity<Usuario>().Property(x => x.Rol).HasConversion<string>();
        var incident = model.Entity<Incidente>();
        incident.Property(x => x.Titulo).HasMaxLength(150);
        incident.Property(x => x.Descripcion).HasMaxLength(3000);
        incident.Property(x => x.Estado).HasConversion<string>();
        incident.Property(x => x.Prioridad).HasConversion<string>();
        incident.Property(x => x.Version).IsConcurrencyToken();
        // SQLite conserva los ticks pero no DateTime.Kind; restaurar UTC al materializar.
        incident.Property(x => x.FechaCreacion).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        incident.Property(x => x.FechaResolucion).HasConversion(
            value => value, value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null);
        incident.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);
    }
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Usuarios.AnyAsync()) return;
        // Cuentas exclusivamente de demostración. Dos por rol operativo permiten probar aislamiento.
        foreach (var (name, role) in new[] { ("admin", Rol.Administrador), ("tecnico", Rol.Tecnico),
            ("usuario", Rol.Usuario), ("tecnico2", Rol.Tecnico), ("usuario2", Rol.Usuario) })
            db.Usuarios.Add(new Usuario { NombreUsuario = name, Rol = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", workFactor: 12) });
        await db.SaveChangesAsync();
    }
}
