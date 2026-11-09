using System.ComponentModel.DataAnnotations;

namespace TechSupport.Contracts;

public enum Rol { Administrador, Tecnico, Usuario }
public enum Estado { Pendiente, Asignado, EnProceso, Resuelto, Cerrado }
public enum Prioridad { Baja, Media, Alta, Critica }

public sealed class LoginRequest
{
    [Required, StringLength(50)] public string NombreUsuario { get; set; } = "";
    [Required, StringLength(72)] public string Password { get; set; } = "";
}
public record LoginResponse(string Token, DateTime ExpiraEn, int Id, string NombreUsuario, Rol Rol);
public sealed class CrearIncidenteRequest
{
    [Required, StringLength(150)] public string Titulo { get; set; } = "";
    [Required, StringLength(3000)] public string Descripcion { get; set; } = "";
    [EnumDataType(typeof(Prioridad))] public Prioridad Prioridad { get; set; } = Prioridad.Media;
}
public sealed class AsignarRequest
{
    [Range(1, int.MaxValue)] public int TecnicoId { get; set; }
}
public sealed class CambiarEstadoRequest
{
    [EnumDataType(typeof(Estado))] public Estado Estado { get; set; }
}
public record UsuarioResumen(int Id, string NombreUsuario);
public record IncidenteDto(int Id, string Titulo, string Descripcion, Prioridad Prioridad,
    Estado Estado, int UsuarioId, string Usuario, int? TecnicoId, string? Tecnico,
    DateTime FechaCreacion, DateTime? FechaResolucion);
public record Pagina<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
