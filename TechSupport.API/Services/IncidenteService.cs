using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.Models;
using TechSupport.Contracts;
namespace TechSupport.API.Services;

public sealed class ReglaException(int status, string message) : Exception(message)
{ public int Status { get; } = status; }

public class IncidenteService(AppDbContext db)
{
    static int Actor(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    IQueryable<Incidente> Visibles(ClaimsPrincipal user)
    {
        var query = db.Incidentes.Include(x => x.Usuario).Include(x => x.Tecnico).AsQueryable();
        if (user.IsInRole(nameof(Rol.Administrador))) return query;
        var id = Actor(user);
        return user.IsInRole(nameof(Rol.Tecnico)) ? query.Where(x => x.TecnicoId == id) : query.Where(x => x.UsuarioId == id);
    }
    static IncidenteDto Dto(Incidente x) => new(x.Id, x.Titulo, x.Descripcion, x.Prioridad, x.Estado,
        x.UsuarioId, x.Usuario.NombreUsuario, x.TecnicoId, x.Tecnico?.NombreUsuario, x.FechaCreacion, x.FechaResolucion);
    async Task<Incidente> Buscar(int id, ClaimsPrincipal actor) => await Visibles(actor).FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new ReglaException(404, "Incidente no encontrado.");
    public async Task<Pagina<IncidenteDto>> Listar(ClaimsPrincipal actor, Estado? estado, int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || page > 1000000)
            throw new ReglaException(400, "page debe estar entre 1 y 1000000 y pageSize entre 1 y 100.");
        var query = Visibles(actor).AsNoTracking();
        if (estado.HasValue) query = query.Where(x => x.Estado == estado);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new(rows.Select(Dto).ToList(), total, page, pageSize);
    }
    public async Task<IncidenteDto> Detalle(int id, ClaimsPrincipal actor) => Dto(await Buscar(id, actor));
    public async Task<IncidenteDto> Crear(CrearIncidenteRequest input, ClaimsPrincipal actor)
    {
        var user = await db.Usuarios.SingleAsync(x => x.Id == Actor(actor));
        var item = new Incidente { Titulo = input.Titulo.Trim(), Descripcion = input.Descripcion.Trim(),
            Prioridad = input.Prioridad, UsuarioId = user.Id, Usuario = user };
        db.Add(item);
        await db.SaveChangesAsync();
        return Dto(item);
    }
    public async Task<IncidenteDto> Asignar(int id, int tecnicoId, ClaimsPrincipal actor)
    {
        if (!actor.IsInRole(nameof(Rol.Administrador))) throw new ReglaException(403, "Operación no permitida.");
        var item = await Buscar(id, actor);
        if (item.Estado != Estado.Pendiente) throw new ReglaException(400, "Solo se asignan incidentes Pendientes; no se permite reasignación.");
        var tecnico = await db.Usuarios.SingleOrDefaultAsync(x => x.Id == tecnicoId && x.Rol == Rol.Tecnico)
            ?? throw new ReglaException(400, "Debe seleccionar un usuario con rol Tecnico.");
        item.TecnicoId = tecnico.Id;
        item.Tecnico = tecnico;
        item.Estado = Estado.Asignado;
        await Guardar(item);
        return Dto(item);
    }
    public async Task<IncidenteDto> Cambiar(int id, Estado estado, ClaimsPrincipal actor)
    {
        if (!actor.IsInRole(nameof(Rol.Administrador)) && !actor.IsInRole(nameof(Rol.Tecnico)))
            throw new ReglaException(403, "Operación no permitida.");
        var item = await Buscar(id, actor);
        if (item.Estado == Estado.Pendiente || item.Estado == Estado.Cerrado || (int)estado != (int)item.Estado + 1)
            throw new ReglaException(400, "El estado solo avanza al siguiente. Primero asigne un técnico; para cerrar debe estar Resuelto. Un Cerrado no se reabre.");
        item.Estado = estado;
        if (estado == Estado.Resuelto) item.FechaResolucion = DateTime.UtcNow;
        await Guardar(item);
        return Dto(item);
    }
    public async Task Eliminar(int id, ClaimsPrincipal actor)
    {
        if (!actor.IsInRole(nameof(Rol.Administrador))) throw new ReglaException(403, "Operación no permitida.");
        db.Remove(await Buscar(id, actor));
        await db.SaveChangesAsync();
    }
    async Task Guardar(Incidente item)
    {
        item.Version = Guid.NewGuid();
        await db.SaveChangesAsync();
    }
}
