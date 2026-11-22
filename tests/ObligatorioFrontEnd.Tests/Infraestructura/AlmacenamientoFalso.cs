using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;

namespace ObligatorioFrontEnd.Tests.Infraestructura;

/// <summary>
/// <see cref="ProtectedSessionStorage"/> real con un runtime JS que falla y un
/// protector efímero. Los servicios que persisten sesión capturan el error.
/// </summary>
public static class AlmacenamientoFalso
{
    public static ProtectedSessionStorage Crear() =>
        new(new JsRuntimeFalso(), new EphemeralDataProtectionProvider());
}
