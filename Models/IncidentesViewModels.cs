using TechSupport.Contracts;
namespace TechSupport_Solutions.Models;
public record ListadoViewModel(Pagina<IncidenteDto> Pagina, Estado? Estado);
public record DetalleViewModel(IncidenteDto Incidente, IReadOnlyList<UsuarioResumen> Tecnicos);
