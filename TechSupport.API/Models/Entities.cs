using TechSupport.Contracts;
namespace TechSupport.API.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Rol Rol { get; set; }
}
public class Incidente
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public Prioridad Prioridad { get; set; }
    public Estado Estado { get; set; } = Estado.Pendiente;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int? TecnicoId { get; set; }
    public Usuario? Tecnico { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaResolucion { get; set; }
    // Token de concurrencia administrado por la aplicación (SQLite no tiene rowversion).
    public Guid Version { get; set; } = Guid.NewGuid();
}
