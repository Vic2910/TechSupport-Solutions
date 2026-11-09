using Microsoft.AspNetCore.Authentication.Cookies;
using TechSupport_Solutions.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/Cuenta/Login"; o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(60); o.SlidingExpiration = false;
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(client => {
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5080/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
var app = builder.Build();
app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Incidentes}/{action=Index}/{id?}");
app.Run();
