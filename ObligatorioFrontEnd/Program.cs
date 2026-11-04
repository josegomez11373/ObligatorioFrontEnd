using System.Globalization;
using ObligatorioFrontEnd.Components;
using ObligatorioFrontEnd.Services;

var culture = CultureInfo.GetCultureInfo("es-UY");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<DemoAuthService>();
builder.Services.AddScoped<IUserScope>(sp => sp.GetRequiredService<DemoAuthService>());
builder.Services.AddScoped<AuditService>();
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
