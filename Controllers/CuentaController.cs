using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.Contracts;
using TechSupport_Solutions.Services;
namespace TechSupport_Solutions.Controllers;
public class CuentaController(ApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginRequest());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest input)
    {
        if (!ModelState.IsValid) return View(input);
        try {
            var login = await api.Send<LoginResponse>(HttpMethod.Post, "api/auth/login", input);
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, login.Id.ToString()),
                new Claim(ClaimTypes.Name, login.NombreUsuario), new Claim(ClaimTypes.Role, login.Rol.ToString()) },
                CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { ExpiresUtc = login.ExpiraEn, IsPersistent = false };
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = login.Token } });
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return RedirectToAction("Index", "Incidentes");
        } catch (ApiException ex) { ModelState.AddModelError("", ex.Message); return View(input); }
    }
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    public IActionResult AccesoDenegado() { Response.StatusCode = 403; return View(); }
}
