# Reporte upstream — ApiSiga

Hallazgos detectados al conectar el frontend Blazor con `ApiSiga` en
`http://localhost:5100`. El repositorio del frontend no modifica el de la API;
este documento deja el diagnóstico y el parche propuesto para el equipo de API.

---

## 1. Crear campaña con potreros responde 409 y no persiste

**Severidad:** alta — bloquea el alta de campañas desde la interfaz.

**Endpoint:** `POST /api/v1/campanas`

**Síntoma:** la API responde `409 Conflict` con el cuerpo genérico:

```json
{
  "title": "Conflicto",
  "status": 409,
  "detail": "El registro entra en conflicto con datos existentes.",
  "instance": "/api/v1/campanas"
}
```

En el log del servidor aparece:

```
Microsoft.Data.SqlClient.SqlException (0x80131904):
Violation of PRIMARY KEY constraint 'PK_Potreros'.
Cannot insert duplicate key in object 'dbo.Potreros'.
The duplicate key value is (<potreroId>).
```

**Pasos para reproducir:**

1. Iniciar sesión como administrador general (`admin@siga.demo` / `siga2026`).
2. `POST /api/v1/campanas` con un cultivo y al menos un potrero existentes y activos:

```powershell
$login = Invoke-RestMethod -Uri http://localhost:5100/api/v1/auth/login `
  -Method POST -ContentType 'application/json' `
  -Body '{"email":"admin@siga.demo","contrasena":"siga2026"}'

$headers = @{ Authorization = "Bearer $($login.token)" }

$body = @{
  empresaId   = '<empresaId>'
  nombre      = 'Campaña de prueba'
  cultivoId   = '<cultivoId>'
  fechaInicio = '2026-11-09T00:00:00'
  potreroIds  = @('<potreroId>')
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5100/api/v1/campanas `
  -Method POST -Headers $headers -ContentType 'application/json' -Body $body
```

3. La respuesta es 409 y la campaña no se crea. Omitir `potreroIds` tampoco es
   una salida: la validación responde `400 "Seleccioná al menos un potrero."`.

**Causa raíz:**

`RepositorioPotreros.ObtenerPorIdsAsync` carga los potreros con `AsNoTracking`:

```csharp
// src/ApiSiga.Infrastructure/Repositorios/RepositorioPotreros.cs
return await _db.Potreros
    .AsNoTracking()
    .Where(p => ids.Contains(p.Id))
    .ToListAsync(cancellationToken);
```

Luego `ServicioCampanas.CrearAsync` asigna esa instancia a la navegación de la
nueva asignación y agrega el grafo completo:

```csharp
// src/ApiSiga.Core/Servicios/Implementaciones/ServicioCampanas.cs
campana.CampanaPotreros.Add(new CampanaPotrero
{
    EmpresaId = empresaId,
    PotreroId = potrero.Id,
    Potrero = potrero,          // instancia no trackeada
    ...
});

_campanas.Agregar(campana);      // _db.Campanas.Add(campana): marca todo el grafo como Added
```

Como el potrero no está trackeado, EF lo interpreta como una entidad nueva y
genera un `INSERT INTO Potreros` con una PK que ya existe.

**Parche mínimo propuesto (una línea):**

```diff
--- a/src/ApiSiga.Infrastructure/Repositorios/RepositorioPotreros.cs
+++ b/src/ApiSiga.Infrastructure/Repositorios/RepositorioPotreros.cs
@@
-        return await _db.Potreros
-            .AsNoTracking()
+        return await _db.Potreros
             .Where(p => ids.Contains(p.Id))
             .ToListAsync(cancellationToken);
```

`ObtenerPorIdsAsync` solo se usa en `ServicioCampanas.CrearAsync`, así que no
hay otro consumidor que dependa del no-tracking. Con el potrero trackeado, EF lo
deja en estado `Unchanged` y solo inserta `Campanas` y `CampanaPotreros`.

**Alternativa equivalente:** no asignar la navegación en el servicio:

```csharp
foreach (var potrero in potreros)
{
    campana.CampanaPotreros.Add(new CampanaPotrero
    {
        EmpresaId = empresaId,
        PotreroId = potrero.Id,
        // Potrero = potrero,  ← quitar esta línea
        ...
    });
}
```

Cualquiera de las dos opciones mantiene el contrato HTTP y las validaciones de
empresa, potrero activo y solapamiento.

**Verificación esperada:** `POST /api/v1/campanas` devuelve `201` con la campaña
y sus potreros; el frontend muestra la campaña como `Planificada`, permite
editarla a `Activa` y cerrarla.

**Impacto en el frontend:** el módulo Campañas muestra el mensaje de error de la
API en el formulario y conserva los datos ingresados; no hay corrupción local.
El resto del módulo (listado, edición, cierre de campañas existentes) funciona.

---

## 2. `AuditoriaInterceptor` no completa `FechaCreacion` al insertar

**Severidad:** baja — afecta la trazabilidad, no bloquea operaciones.

**Archivo:** `src/ApiSiga.Infrastructure/Persistencia/AuditoriaInterceptor.cs`, línea 57.

```csharp
// Actual: la expresión condicional queda dentro de una asignación encadenada.
entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion = default ? ahora : entrada.Entity.FechaCreacion;
```

La condición es `default`, que siempre es `false`, por lo que la entidad conserva
su `FechaCreacion` (normalmente `default`) en lugar de recibir `ahora`.

**Parche propuesto:**

```diff
- entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion = default ? ahora : entrada.Entity.FechaCreacion;
+ entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion == default
+     ? ahora
+     : entrada.Entity.FechaCreacion;
```

---

## Reproducción automatizada usada por el frontend

El frontend se validó con un smoke test de navegador (Chrome headless,
`playwright-core`) contra `http://localhost:5021` en modo API:

- Login real y restauración de sesión tras F5.
- Empresas: alta, edición y baja lógica.
- Establecimientos, potreros y cultivos: alta, edición y baja lógica.
- Usuarios: alta, edición y baja lógica.
- Selector de empresa activa del administrador general y filtrado por empresa.
- Campañas: el alta con potreros es el único paso bloqueado por el hallazgo 1;
  la edición de una campaña existente sí se verificó.

En modo mock (`ApiSiga:UseApi=false`) el mismo recorrido, incluido el alta,
edición de estado y cierre de campaña, pasa 19/19.
