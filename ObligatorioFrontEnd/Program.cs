using System.Globalization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Components;
using ObligatorioFrontEnd.Services;

var culture = CultureInfo.GetCultureInfo("es-UY");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// El login de demostración sigue disponible mientras no se configure la API.
builder.Services.AddScoped<DemoAuthService>();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<NotificadorDatos>();

// La sesión vive en el servidor y el navegador solo guarda un id opaco; así el F5
// no pierde el login. El almacén es singleton porque comparte datos entre circuitos.
builder.Services.AddSingleton<AlmacenSesiones>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<SesionPersistencia>();
builder.Services.AddScoped<IContextoEmpresa, ContextoEmpresa>();

// Todos los datos vienen de ApiSiga. ApiSiga:UseApi decide únicamente si el login
// usa la API (SesionService) o el usuario de demostración (DemoAuthService).
var useApi = builder.Configuration.GetValue<bool>("ApiSiga:UseApi");
var baseUrl = builder.Configuration["ApiSiga:BaseUrl"] ?? "http://localhost:5100/";

builder.Services.AddHttpClient<ClienteApi>(http =>
{
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromSeconds(30);
});

if (useApi)
{
    builder.Services.AddScoped<SesionService>();
    builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<SesionService>());
}
else
{
    builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<DemoAuthService>());
}

builder.Services.AddScoped<ServicioEmpresasApi>();
builder.Services.AddScoped<ServicioEstablecimientosApi>();
builder.Services.AddScoped<ServicioPotrerosApi>();
builder.Services.AddScoped<ServicioCultivosApi>();
builder.Services.AddScoped<ServicioCampanasApi>();
builder.Services.AddScoped<ServicioUsuariosApi>();
builder.Services.AddScoped<ServicioTiposEventoApi>();
builder.Services.AddScoped<ServicioTiposTareaApi>();
builder.Services.AddScoped<ServicioEventosApi>();
builder.Services.AddScoped<ServicioFotografiasApi>();
builder.Services.AddScoped<ServicioTareasApi>();
builder.Services.AddScoped<ServicioInsumosApi>();
builder.Services.AddScoped<ServicioMaquinariaApi>();
builder.Services.AddScoped<ServicioContratistasApi>();
builder.Services.AddScoped<ServicioAuditoriaApi>();
builder.Services.AddScoped<ServicioReportesApi>();

builder.Services.AddScoped<IServicioEmpresas>(sp => sp.GetRequiredService<ServicioEmpresasApi>());
builder.Services.AddScoped<IServicioEstablecimientos>(sp => sp.GetRequiredService<ServicioEstablecimientosApi>());
builder.Services.AddScoped<IServicioPotreros>(sp => sp.GetRequiredService<ServicioPotrerosApi>());
builder.Services.AddScoped<IServicioCultivos>(sp => sp.GetRequiredService<ServicioCultivosApi>());
builder.Services.AddScoped<IServicioCampanas>(sp => sp.GetRequiredService<ServicioCampanasApi>());
builder.Services.AddScoped<IServicioUsuarios>(sp => sp.GetRequiredService<ServicioUsuariosApi>());
builder.Services.AddScoped<IServicioTiposEvento>(sp => sp.GetRequiredService<ServicioTiposEventoApi>());
builder.Services.AddScoped<IServicioTiposTarea>(sp => sp.GetRequiredService<ServicioTiposTareaApi>());
builder.Services.AddScoped<IServicioEventos>(sp => sp.GetRequiredService<ServicioEventosApi>());
builder.Services.AddScoped<IServicioFotografias>(sp => sp.GetRequiredService<ServicioFotografiasApi>());
builder.Services.AddScoped<IServicioTareas>(sp => sp.GetRequiredService<ServicioTareasApi>());
builder.Services.AddScoped<IServicioInsumos>(sp => sp.GetRequiredService<ServicioInsumosApi>());
builder.Services.AddScoped<IServicioMaquinaria>(sp => sp.GetRequiredService<ServicioMaquinariaApi>());
builder.Services.AddScoped<IServicioContratistas>(sp => sp.GetRequiredService<ServicioContratistasApi>());
builder.Services.AddScoped<IServicioAuditoria>(sp => sp.GetRequiredService<ServicioAuditoriaApi>());
builder.Services.AddScoped<IServicioReportes>(sp => sp.GetRequiredService<ServicioReportesApi>());

builder.Services.AddScoped<IUserScope>(sp => sp.GetRequiredService<IAuthService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
