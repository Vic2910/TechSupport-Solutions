using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using Xunit;

namespace TechSupport.Tests;

// Esta ruta solo existe en el ensamblado de pruebas; nunca se publica con la API.
[ApiController, AllowAnonymous]
public class FaultController : ControllerBase
{
    [HttpGet("/_test/throw")]
    public IActionResult Throw() => throw new InvalidOperationException("SECRETO: ruta/servidor/database");
}
public class ApiFactory : WebApplicationFactory<Program>
{
    readonly string database = Path.Combine(Path.GetTempPath(), $"techsupport-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:Default"] = $"Data Source={database}"
        }));
        builder.ConfigureServices(s => s.AddControllers().AddApplicationPart(typeof(FaultController).Assembly));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
public class IntegrationTests
{
    [Fact]
    public async Task MatrizCompleta_DeSeguridad_Estados_Persistencia_YErrores()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var evidence = new List<object>();
        async Task<JsonNode?> Call(string title, HttpMethod method, string url, int expected, string? token = null, object? body = null)
        {
            using var req = new HttpRequestMessage(method, url);
            if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) req.Content = JsonContent.Create(body);
            using var res = await client.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            var node = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            var safeResponse = node?.DeepClone();
            if (safeResponse is JsonObject obj && obj.ContainsKey("token")) obj["token"] = "[JWT generado; oculto en evidencia]";
            var safeRequest = body is null ? null : JsonSerializer.SerializeToNode(body);
            if (safeRequest is JsonObject input && input.ContainsKey("password")) input["password"] = "[oculta]";
            evidence.Add(new { title, request = new { method = method.Method, url,
                authorization = token is null ? null : "Bearer [JWT oculto]", body = safeRequest },
                response = new { status = (int)res.StatusCode, body = safeResponse }, passed = (int)res.StatusCode == expected });
            Assert.Equal(expected, (int)res.StatusCode);
            Assert.DoesNotContain("passwordHash", text, StringComparison.OrdinalIgnoreCase);
            return node;
        }
        async Task<string> Login(string name) => (await Call($"Login {name}", HttpMethod.Post, "/api/auth/login", 200,
            body: new { nombreUsuario = name, password = "Demo123!" }))!["token"]!.GetValue<string>();

        var admin = await Login("admin");
        var tecnico = await Login("tecnico");
        var usuario = await Login("usuario");
        var otroTecnico = await Login("tecnico2");
        var otroUsuario = await Login("usuario2");
        var bad = await Call("02 - Contraseña incorrecta", HttpMethod.Post, "/api/auth/login", 401,
            body: new { nombreUsuario = "admin", password = "incorrecta" });
        var missing = await Call("Usuario inexistente: mismo mensaje", HttpMethod.Post, "/api/auth/login", 401,
            body: new { nombreUsuario = "no-existe", password = "incorrecta" });
        Assert.Equal(bad!.ToJsonString(), missing!.ToJsonString());
        await Call("03 - Sin JWT", HttpMethod.Get, "/api/incidentes", 401);
        await Call("JWT alterado", HttpMethod.Get, "/api/incidentes", 401, admin[..^10] + "INVALIDXXX");
        await Call("05 - Sin título", HttpMethod.Post, "/api/incidentes", 400, usuario, new { descripcion = "Falla", prioridad = "Alta" });
        await Call("Título solo espacios", HttpMethod.Post, "/api/incidentes", 400, usuario, new { titulo = "   ", descripcion = "Falla" });
        await Call("Enum inválido", HttpMethod.Post, "/api/incidentes", 400, usuario, new { titulo = "Falla", descripcion = "Falla", prioridad = 99 });
        var created = await Call("06 - Crear correctamente; ignora UsuarioId del cuerpo", HttpMethod.Post, "/api/incidentes", 201,
            usuario, new { titulo = "Correo no disponible", descripcion = "No permite iniciar sesión.", prioridad = "Alta", usuarioId = 1 });
        int id = created!["id"]!.GetValue<int>();
        Assert.Equal(3, created["usuarioId"]!.GetValue<int>());
        Assert.Equal("Pendiente", created["estado"]!.GetValue<string>());
        Assert.Null(created["tecnicoId"]);
        await Call("04 - Técnico no elimina", HttpMethod.Delete, $"/api/incidentes/{id}", 403, tecnico);
        await Call("07 - Usuario consulta incidente ajeno", HttpMethod.Get, $"/api/incidentes/{id}", 404, otroUsuario);
        await Call("Usuario consulta propio", HttpMethod.Get, $"/api/incidentes/{id}", 200, usuario);
        await Call("08 - Cerrar sin resolver", HttpMethod.Put, $"/api/incidentes/{id}/estado", 400, admin, new { estado = "Cerrado" });
        await Call("Usuario no cambia estado", HttpMethod.Put, $"/api/incidentes/{id}/estado", 403, usuario, new { estado = "EnProceso" });
        await Call("Técnico no asigna", HttpMethod.Put, $"/api/incidentes/{id}/asignar", 403, tecnico, new { tecnicoId = 2 });
        await Call("No asignar a usuario", HttpMethod.Put, $"/api/incidentes/{id}/asignar", 400, admin, new { tecnicoId = 3 });
        await Call("No asignar técnico inexistente", HttpMethod.Put, $"/api/incidentes/{id}/asignar", 400, admin, new { tecnicoId = 999 });
        var assigned = await Call("Asignación válida", HttpMethod.Put, $"/api/incidentes/{id}/asignar", 200, admin, new { tecnicoId = 2 });
        Assert.Equal("Asignado", assigned!["estado"]!.GetValue<string>());
        await Call("No reasignar un incidente en atención", HttpMethod.Put, $"/api/incidentes/{id}/asignar", 400, admin, new { tecnicoId = 4 });
        await Call("Caso 1 - Técnico modifica ajeno", HttpMethod.Put, $"/api/incidentes/{id}/estado", 404, otroTecnico, new { estado = "EnProceso" });
        var ownList = await Call("Lista del técnico asignado", HttpMethod.Get, "/api/incidentes", 200, tecnico);
        Assert.Single(ownList!["items"]!.AsArray());
        var otherList = await Call("Lista del otro técnico", HttpMethod.Get, "/api/incidentes", 200, otroTecnico);
        Assert.Empty(otherList!["items"]!.AsArray());
        var otherUserList = await Call("Lista del otro usuario", HttpMethod.Get, "/api/incidentes", 200, otroUsuario);
        Assert.Empty(otherUserList!["items"]!.AsArray());
        await Call("No saltar de Asignado a Resuelto", HttpMethod.Put, $"/api/incidentes/{id}/estado", 400, tecnico, new { estado = "Resuelto" });
        await Call("Avanzar a EnProceso", HttpMethod.Put, $"/api/incidentes/{id}/estado", 200, tecnico, new { estado = "EnProceso" });
        await Call("No retroceder", HttpMethod.Put, $"/api/incidentes/{id}/estado", 400, tecnico, new { estado = "Asignado" });
        var resolved = await Call("Resolver y registrar fecha", HttpMethod.Put, $"/api/incidentes/{id}/estado", 200, tecnico, new { estado = "Resuelto" });
        Assert.NotNull(resolved!["fechaResolucion"]);
        var closed = await Call("Cerrar después de resolver", HttpMethod.Put, $"/api/incidentes/{id}/estado", 200, tecnico, new { estado = "Cerrado" });
        Assert.Equal(resolved["fechaResolucion"]!.ToString(), closed!["fechaResolucion"]!.ToString());
        await Call("Cerrado no se reabre", HttpMethod.Put, $"/api/incidentes/{id}/estado", 400, admin, new { estado = "Pendiente" });
        await Call("Límite de paginación", HttpMethod.Get, "/api/incidentes?pageSize=1000000", 400, admin);
        await Call("Página negativa", HttpMethod.Get, "/api/incidentes?page=-1", 400, admin);
        var filtered = await Call("Filtro por estado y página", HttpMethod.Get, "/api/incidentes?estado=Cerrado&page=1&pageSize=1", 200, admin);
        Assert.Single(filtered!["items"]!.AsArray());
        await Call("Estado de filtro inválido", HttpMethod.Get, "/api/incidentes?estado=Invalido", 400, admin);
        var error = await Call("09 - Excepción controlada (ruta solo de pruebas)", HttpMethod.Get, "/_test/throw", 500);
        Assert.False(error!["success"]!.GetValue<bool>());
        Assert.False(string.IsNullOrWhiteSpace(error["requestId"]!.GetValue<string>()));
        Assert.NotNull(error["timestamp"]);
        Assert.DoesNotContain("SECRETO", error.ToJsonString());
        Assert.DoesNotContain("InvalidOperationException", error.ToJsonString());
        // Un contexto nuevo consulta el archivo SQLite, no el change tracker de la petición.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
            Assert.Equal(TechSupport.Contracts.Estado.Cerrado, (await db.Incidentes.AsNoTracking().SingleAsync(x => x.Id == id)).Estado);
            var user = await db.Usuarios.SingleAsync(x => x.NombreUsuario == "admin");
            Assert.NotEqual("Demo123!", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Demo123!", user.PasswordHash));
        }
        await Call("Administrador elimina cerrado", HttpMethod.Delete, $"/api/incidentes/{id}", 204, admin);
        await Call("Eliminado devuelve 404", HttpMethod.Get, $"/api/incidentes/{id}", 404, admin);
        var techCreated = await Call("Técnico puede crear", HttpMethod.Post, "/api/incidentes", 201, tecnico,
            new { titulo = "Reporte técnico", descripcion = "Creado por técnico", prioridad = "Baja" });
        await Call("Técnico creador sin asignación no ve detalle", HttpMethod.Get,
            $"/api/incidentes/{techCreated!["id"]}", 404, tecnico);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TechSupport Solutions.slnx"))) root = root.Parent;
        var output = Path.Combine(root!.FullName, "docs", "evidencias");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "http.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }
}

