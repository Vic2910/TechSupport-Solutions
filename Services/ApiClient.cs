using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
namespace TechSupport_Solutions.Services;

public class ApiException(HttpStatusCode status, string message) : Exception(message)
{ public HttpStatusCode Status { get; } = status; }
public class ApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };
    public async Task<T> Send<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        var token = await accessor.HttpContext!.GetTokenAsync("access_token");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        HttpResponseMessage response;
        try { response = await http.SendAsync(request); }
        catch (HttpRequestException) { throw new ApiException(HttpStatusCode.ServiceUnavailable, "No se pudo conectar con la API. Compruebe que está iniciada e intente nuevamente."); }
        catch (TaskCanceledException) { throw new ApiException(HttpStatusCode.GatewayTimeout, "La API tardó demasiado. Intente nuevamente."); }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode switch {
                    HttpStatusCode.Unauthorized => "La sesión expiró o las credenciales son incorrectas.",
                    HttpStatusCode.Forbidden => "No tiene permiso para realizar esta operación.",
                    HttpStatusCode.NotFound => "Incidente no encontrado.",
                    _ => "No se pudo procesar la solicitud."
                };
                try {
                    using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (data.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? message;
                    if (data.RootElement.TryGetProperty("errors", out var errors))
                        message = string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()));
                    if (data.RootElement.TryGetProperty("requestId", out var id)) message += $" Referencia: {id.GetString()}";
                } catch (JsonException) { }
                throw new ApiException(response.StatusCode, message);
            }
            if (response.StatusCode == HttpStatusCode.NoContent) return default!;
            return (await response.Content.ReadFromJsonAsync<T>(Json))!;
        }
    }
}
