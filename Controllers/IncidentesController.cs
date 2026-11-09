using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.Contracts;
using TechSupport_Solutions.Models;
using TechSupport_Solutions.Services;
namespace TechSupport_Solutions.Controllers;
[Authorize]
public class IncidentesController(ApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(Estado? estado, int page = 1)
    {
        try {
            var data = await api.Send<Pagina<IncidenteDto>>(HttpMethod.Get, $"api/incidentes?page={page}&pageSize=10&estado={estado}");
            return View(new ListadoViewModel(data, estado));
        } catch (ApiException ex) { return Failure(ex); }
    }
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        try {
            var incident = await api.Send<IncidenteDto>(HttpMethod.Get, $"api/incidentes/{id}");
            var tecnicos = User.IsInRole("Administrador") && incident.Estado == TechSupport.Contracts.Estado.Pendiente
                ? await api.Send<List<UsuarioResumen>>(HttpMethod.Get, "api/usuarios/tecnicos") : [];
            return View(new DetalleViewModel(incident, tecnicos));
        } catch (ApiException ex) { return Failure(ex); }
    }
    [HttpGet] public IActionResult Crear() => View(new CrearIncidenteRequest());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearIncidenteRequest input)
    {
        if (!ModelState.IsValid) return View(input);
        try {
            var item = await api.Send<IncidenteDto>(HttpMethod.Post, "api/incidentes", input);
            TempData["Success"] = $"Incidente #{item.Id} registrado correctamente.";
            // Un técnico solo puede consultar incidentes asignados a él, aunque sea el creador.
            return User.IsInRole("Tecnico") ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Detalle), new { id = item.Id });
        } catch (ApiException ex) { if (ex.Status == HttpStatusCode.Unauthorized) return Failure(ex); ModelState.AddModelError("", ex.Message); return View(input); }
    }
    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(int id, AsignarRequest input) => await Mutate(id, "asignar", input);
    [Authorize(Roles = "Administrador,Tecnico"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Estado(int id, CambiarEstadoRequest input) => await Mutate(id, "estado", input);
    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try { await api.Send<object>(HttpMethod.Delete, $"api/incidentes/{id}"); TempData["Success"] = "Incidente eliminado."; return RedirectToAction(nameof(Index)); }
        catch (ApiException ex) { return Failure(ex); }
    }
    async Task<IActionResult> Mutate(int id, string action, object input)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Revise los datos ingresados."; return RedirectToAction(nameof(Detalle), new { id }); }
        try { await api.Send<IncidenteDto>(HttpMethod.Put, $"api/incidentes/{id}/{action}", input); TempData["Success"] = "Incidente actualizado."; }
        catch (ApiException ex) { if (ex.Status == HttpStatusCode.Unauthorized) return Failure(ex); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detalle), new { id });
    }
    IActionResult Failure(ApiException ex)
    {
        if (ex.Status == HttpStatusCode.Unauthorized) return RedirectToAction("Login", "Cuenta");
        Response.StatusCode = (int)ex.Status;
        return View("ApiError", ex.Message);
    }
}
