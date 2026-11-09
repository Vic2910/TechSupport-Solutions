using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.Services;
using TechSupport.Contracts;
namespace TechSupport.API.Controllers;

[ApiController, Authorize, Route("api/incidentes")]
public class IncidentesController(IncidenteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Pagina<IncidenteDto>>> Listar(Estado? estado, int page = 1, int pageSize = 10)
        => Ok(await service.Listar(User, estado, page, pageSize));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidenteDto>> Detalle(int id) => Ok(await service.Detalle(id, User));
    [HttpPost]
    public async Task<ActionResult<IncidenteDto>> Crear(CrearIncidenteRequest input)
    {
        var result = await service.Crear(input, User);
        return CreatedAtAction(nameof(Detalle), new { id = result.Id }, result);
    }
    [Authorize(Roles = "Administrador"), HttpPut("{id:int}/asignar")]
    public async Task<ActionResult<IncidenteDto>> Asignar(int id, AsignarRequest input) => Ok(await service.Asignar(id, input.TecnicoId, User));
    [Authorize(Roles = "Administrador,Tecnico"), HttpPut("{id:int}/estado")]
    public async Task<ActionResult<IncidenteDto>> Estado(int id, CambiarEstadoRequest input) => Ok(await service.Cambiar(id, input.Estado, User));
    [Authorize(Roles = "Administrador"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id) { await service.Eliminar(id, User); return NoContent(); }
}
[ApiController, Authorize(Roles = "Administrador"), Route("api/usuarios")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    [HttpGet("tecnicos")]
    public async Task<ActionResult<List<UsuarioResumen>>> Tecnicos() => await db.Usuarios
        .Where(x => x.Rol == Rol.Tecnico).Select(x => new UsuarioResumen(x.Id, x.NombreUsuario)).ToListAsync();
}
