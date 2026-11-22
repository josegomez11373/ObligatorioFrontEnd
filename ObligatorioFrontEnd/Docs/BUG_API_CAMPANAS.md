# Reporte de API — campañas y fechas de auditoría

**Estado:** resuelto y verificado · **Fecha de verificación:** 19/11/2026
**Repositorio afectado:** ApiSiga (`ApiSigaActualizada`) — solo lectura desde el
frontend; no se modificó código de la API en este repositorio.

Este documento archiva los dos hallazgos detectados al conectar el frontend
Blazor con ApiSiga, deja la evidencia de la corrección aplicada en la API y la
prueba automatizada que impide la regresión.

---

## 1. Crear campaña con potreros respondía 409 y no persistía

**Severidad original:** alta — bloqueaba el alta de campañas desde la interfaz.
**Endpoint:** `POST /api/v1/campanas`
**Estado:** resuelto.

### Síntoma original

La API respondía `409 Conflict` con el cuerpo genérico:

```json
{
  "title": "Conflicto",
  "status": 409,
  "detail": "El registro entra en conflicto con datos existentes.",
  "instance": "/api/v1/campanas"
}
```

En el log del servidor aparecía:

```
Microsoft.Data.SqlClient.SqlException (0x80131904):
Violation of PRIMARY KEY constraint 'PK_Potreros'.
Cannot insert duplicate key in object 'dbo.Potreros'.
The duplicate key value is (<potreroId>).
```

### Causa

`ServicioCampanas.CrearAsync` obtenía los potreros con
`RepositorioPotreros.ObtenerPorIdsAsync`, que usa `AsNoTracking()`, y luego
asignaba la entidad completa en la navegación de la nueva asignación:

```csharp
// Código con el defecto (ya eliminado)
campana.CampanaPotreros.Add(new CampanaPotrero
{
    EmpresaId = empresaId,
    CampanaId = campana.Id,
    PotreroId = potrero.Id,
    Potrero = potrero, // <-- entidad no rastreada
    FechaInicio = dto.FechaInicio,
    FechaFin = dto.FechaFin,
    Estado = EstadoCampana.Planificada
});
```

EF Core interpretaba `Potrero = potrero` como un alta de un potrero existente y
generaba un `INSERT` duplicado sobre la clave primaria.

### Corrección aplicada en ApiSiga

La asignación usa únicamente la clave foránea `PotreroId`; no se asigna la
navegación. Código actual de `ServicioCampanas.CrearAsync`:

```csharp
foreach (var potrero in potreros)
{
    campana.CampanaPotreros.Add(new CampanaPotrero
    {
        EmpresaId = empresaId,
        CampanaId = campana.Id,
        PotreroId = potrero.Id,
        FechaInicio = dto.FechaInicio,
        FechaFin = dto.FechaFin,
        Estado = EstadoCampana.Planificada
    });
}
```

`RepositorioPotreros.ObtenerPorIdsAsync` conserva `AsNoTracking()`; ya no rompe
el alta porque no se adjunta la entidad a la navegación.

La misma revisión confirmó que `SincronizarPotreros` reutiliza la baja lógica:
los potreros que dejan de estar seleccionados pasan a `EstadoCampana.Anulada`
en lugar de borrarse, para no perder eventos, tareas ni auditoría.

### Verificación

Prueba de integración de la API:

```
ActualizarCampana_ReemplazaPotrerosYReutilizaBajaLogica
```

Ejecución del 19/11/2026:

```powershell
dotnet test tests/ApiSiga.Tests/ApiSiga.Tests.csproj `
  --filter ActualizarCampana_ReemplazaPotrerosYReutilizaBajaLogica
```

Resultado: **1 superada, 0 fallidas**. La prueba crea una campaña con
`PotreroNorte`, la actualiza con `PotreroSur` y comprueba que queda una sola
asignación activa, que la anterior se anula lógicamente y que la respuesta no
duplica potreros.

### Impacto en el frontend

- `CampanaForm` envía `CrearCampanaDto`/`ActualizarCampanaDto` con `PotreroIds`.
- `ServicioCampanasApi` consume `POST /api/v1/campanas` y
  `PUT /api/v1/campanas/{id}` sin cambios.
- El alta de campañas ya no requiere reintentos ni workarounds.

---

## 2. `FechaCreacion` llegaba como `0001-01-01` en algunas entidades

**Severidad original:** media — la auditoría y las tablas mostraban fechas
inválidas.
**Entidades afectadas:** entidades auditables creadas por la API.
**Estado:** resuelto.

### Síntoma original

Algunas filas mostraban `FechaCreacion = 0001-01-01T00:00:00`, por lo que el
frontend tenía que mostrar una raya (`—`) en las columnas de fecha.

### Causa

El interceptor de auditoría sobrescribía `FechaCreacion` en cada alta, incluso
cuando la entidad ya traía una fecha válida asignada por la capa de negocio.

### Corrección aplicada en ApiSiga

`AuditoriaInterceptor` conserva la fecha existente y solo asigna la fecha actual
cuando viene vacía:

```csharp
case EntityState.Added:
    entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion == default
        ? ahora
        : entrada.Entity.FechaCreacion;
    entrada.Entity.CreadoPor = usuario;
    entrada.Entity.Version = 1;
    auditorias.Add(CrearAuditoria(entrada, AccionAuditoria.Alta, null, ahora));
    break;
```

### Impacto en el frontend

- Las tablas y la auditoría muestran la fecha real.
- `SigaTime.FormatOrDash` se conserva como defensa para datos históricos
  anteriores a la corrección; no se eliminó para no romper registros viejos.

---

## 3. Conclusión

| Hallazgo | Estado | Evidencia |
| --- | --- | --- |
| Alta/edición de campaña con potreros (`409`, PK duplicada) | Resuelto | `ServicioCampanas.CrearAsync` sin `Potrero = potrero`; prueba `ActualizarCampana_ReemplazaPotrerosYReutilizaBajaLogica` en verde |
| `FechaCreacion` en `0001-01-01` | Resuelto | `AuditoriaInterceptor` conserva la fecha existente y solo completa `default` |

El frontend no requiere cambios adicionales para estos dos puntos. Si la API se
despliega desde una versión anterior, conviene ejecutar la prueba de integración
antes de la demostración.
