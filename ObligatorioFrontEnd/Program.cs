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

// DemoAuthService se registra siempre: UserService lo usa para mantener el padrón
// mock, incluso cuando el login real está activo.
builder.Services.AddScoped<DemoAuthService>();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<NotificadorDatos>();

// La sesión vive en el servidor y el navegador solo guarda un id opaco; así el F5
// no pierde el login. El almacén es singleton porque comparte datos entre circuitos.
builder.Services.AddSingleton<AlmacenSesiones>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<SesionPersistencia>();
builder.Services.AddScoped<IContextoEmpresa, ContextoEmpresa>();

var useApi = builder.Configuration.GetValue<bool>("ApiSiga:UseApi");

if (useApi)
{
    var baseUrl = builder.Configuration["ApiSiga:BaseUrl"] ?? "http://localhost:5100/";

    builder.Services.AddHttpClient<ClienteApi>(http =>
    {
        http.BaseAddress = new Uri(baseUrl);
        http.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddScoped<SesionService>();
    builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<SesionService>());

    builder.Services.AddScoped<ServicioEmpresasApi>();
    builder.Services.AddScoped<ServicioEstablecimientosApi>();
    builder.Services.AddScoped<ServicioPotrerosApi>();
    builder.Services.AddScoped<ServicioCultivosApi>();
    builder.Services.AddScoped<ServicioCampanasApi>();
    builder.Services.AddScoped<ServicioUsuariosApi>();

    builder.Services.AddScoped<IServicioEmpresas>(sp => sp.GetRequiredService<ServicioEmpresasApi>());
    builder.Services.AddScoped<IServicioEstablecimientos>(sp => sp.GetRequiredService<ServicioEstablecimientosApi>());
    builder.Services.AddScoped<IServicioPotreros>(sp => sp.GetRequiredService<ServicioPotrerosApi>());
    builder.Services.AddScoped<IServicioCultivos>(sp => sp.GetRequiredService<ServicioCultivosApi>());
    builder.Services.AddScoped<IServicioCampanas>(sp => sp.GetRequiredService<ServicioCampanasApi>());
    builder.Services.AddScoped<IServicioUsuarios>(sp => sp.GetRequiredService<ServicioUsuariosApi>());
}
else
{
    builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<DemoAuthService>());

    builder.Services.AddScoped<IServicioEmpresas>(sp => sp.GetRequiredService<EmpresaService>());
    builder.Services.AddScoped<IServicioEstablecimientos>(sp => sp.GetRequiredService<EstablecimientoService>());
    builder.Services.AddScoped<IServicioPotreros>(sp => sp.GetRequiredService<PotreroService>());
    builder.Services.AddScoped<IServicioCultivos>(sp => sp.GetRequiredService<CultivoService>());
    builder.Services.AddScoped<IServicioCampanas>(sp => sp.GetRequiredService<CampanaService>());
    builder.Services.AddScoped<IServicioUsuarios>(sp => sp.GetRequiredService<UserService>());
}

builder.Services.AddScoped<IUserScope>(sp => sp.GetRequiredService<IAuthService>());
builder.Services.AddScoped<AuditService>();

// EmpresaService sigue registrado en modo API porque los módulos mock
// (EstablecimientoService, UserService, etc.) lo inyectan concretamente.
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<EstablecimientoService>();
builder.Services.AddScoped<PotreroService>();
builder.Services.AddScoped<CultivoService>();
builder.Services.AddScoped<CampanaService>();
builder.Services.AddScoped<TipoEventoService>();
builder.Services.AddScoped<EventoService>();
builder.Services.AddSingleton<FotografiaStore>();
builder.Services.AddScoped<FotografiaService>();
builder.Services.AddScoped<TipoTareaService>();
builder.Services.AddScoped<TareaService>();
builder.Services.AddScoped<InsumoService>();
builder.Services.AddScoped<MaquinariaService>();
builder.Services.AddScoped<ContratistaService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Las páginas usan una pantalla propia para 404. El endpoint de fotos queda fuera:
// una imagen con token inválido debe responder 404 y no redirigir al login.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/fotos"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Las fotos se guardan fuera de wwwroot. El token de capacidad lo entrega el servicio
// solo a usuarios con acceso a la empresa, y el endpoint lo valida sin depender de la
// sesión de Blazor.
app.MapGet("/fotos/{id:guid}", (Guid id, string? token, FotografiaStore fotografias) =>
{
    var file = fotografias.Read(id, token);
    return file is null
        ? Results.NotFound()
        : Results.File(file.Value.Content, file.Value.ContentType);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
