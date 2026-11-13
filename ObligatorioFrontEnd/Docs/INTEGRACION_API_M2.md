# M2 — Integración con ApiSiga: contexto de empresa, notificaciones y módulos asíncronos

> **Estado actual (posterior a M2):** todos los módulos del frontend consumen ApiSiga
> por HTTP. Los servicios en memoria, los modelos legacy, `Api/MapeoApi.cs`,
> `FotografiaStore` y el endpoint local `/fotos` fueron eliminados.
> El login real (`SesionService`/JWT) está habilitado por defecto con
> `ApiSiga:UseApi=true`. El único componente simulado que queda es
> `DemoAuthService`, que solo se usa si se fuerza `ApiSiga:UseApi=false`.
> La tabla final de rutas está en el [`README`](../../README.md). Este documento se
> conserva como registro del plan de M2; las secciones que describen módulos en modo
> mock ya no reflejan el código.

> **Estado:** propuesta de trabajo posterior a M1.
> **Base verificada:** `ObligatorioFrontEnd` compilando sin warnings, login real contra `ApiSiga` y módulo Empresas contra `http://localhost:5100`.
> **Regla de oro:** siguen siendo dos repositorios independientes. El frontend solo conoce HTTP/JSON; no hay `ProjectReference`, submódulo ni copia de código de la API.
>
> **Bloqueo externo conocido:** con `UseApi=true`, el alta de campañas con potreros
> responde `409` por un defecto de ApiSiga (`PK_Potreros`). El frontend no se
> modifica para evitarlo; el diagnóstico y el parche propuesto están en
> [`BUG_API_CAMPANAS.md`](BUG_API_CAMPANAS.md). El resto de los módulos migrados
> funciona contra la API.

---

## 1. Resumen ejecutivo

M1 dejó resuelto el eje vertical más importante: autenticación real, sesión con JWT en el servidor Blazor y el módulo Empresas contra la API, manteniendo el mock disponible con `ApiSiga:UseApi=false`.

M2 resuelve el problema que hoy impide migrar el resto de los módulos sin romper la aplicación:

1. **No existe un contexto de empresa único.** `SesionService` elige la primera empresa del administrador general y no hay forma de cambiarla; las páginas no saben sobre qué empresa operar.
2. **Cada servicio notifica por su cuenta.** Las páginas se suscriben a `StateChanged` de clases concretas. Al reemplazar un servicio mock por un adaptador HTTP, esos eventos desaparecen y las pantallas dejan de refrescarse.
3. **Las páginas son síncronas.** Llaman a los servicios mock en `OnInitialized`; los adaptadores HTTP son asíncronos y necesitan estados de carga, error, cancelación y concurrencia.
4. **La API ya expone más módulos de los que M1 consume:** Establecimientos, Potreros, Cultivos, Campañas y Usuarios. M2 los integra en ese orden, respetando la cadena de dependencias.

**Resultado esperado:** con `ApiSiga:UseApi=true`, un administrador general puede elegir empresa y operar contra la API en Empresas, Establecimientos, Potreros, Cultivos, Campañas y Usuarios; el resto de los módulos sigue funcionando con datos mock y se identifica claramente como demostración. Con `UseApi=false`, todo sigue funcionando como hoy.

**Estimación:** 8 a 10 días de trabajo efectivo.

---

## 2. Punto de partida verificado (M1)

- Login real con email/contraseña; `401` devuelve `false` sin romper la UI.
- JWT guardado en `TokenStore` del circuito; nunca llega al navegador.
- `401` con token ya enviado dispara `Expirada` y `MainLayout` navega a `/login`.
- Empresas: listar, crear, editar, desactivar y preservar `RazonSocial`.
- Aislamiento validado: `admin@siga.demo` ve las dos empresas; `empresa2@siga.demo` solo `Campo Norte`.
- Prerender deshabilitado en `App.razor` para evitar dobles llamadas sin sesión.
- Build limpio, smoke test de adaptadores y prueba de UI con Chrome/CDP aprobados.

Archivos clave que M2 va a reutilizar como patrón:

- `Api/ClienteApi.cs` — transporte HTTP, header JWT, JSON camelCase + enums string, manejo de `401`.
- `Api/ApiException.cs` — error HTTP tipado; hereda de `InvalidOperationException`.
- `Api/Contratos/` — DTOs locales, sin compartir código con la API.
- `Api/MapeoApi.cs` — único punto de traducción DTO ↔ modelo de UI.
- `Services/Api/ServicioEmpresasApi.cs` — patrón de adaptador.
- `Services/NotificadorDatos.cs` — notificación por módulo.
- `Services/ContextoEmpresa` — **a crear en M2**.

---

## 3. Contrato real disponible en la API hoy

| Módulo | Endpoints | DTO principal | Observaciones |
|---|---|---|---|
| Auth | `POST /api/v1/auth/login`, `GET /api/v1/auth/yo` | `RespuestaInicioSesionDto`, `UsuarioSesionDto` | Ya integrado en M1. |
| Empresas | `GET`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` | `EmpresaDto(Id, Nombre, RazonSocial, Activo, FechaCreacion, Version)` | Ya integrado. `DELETE` desactiva. |
| Establecimientos | `GET?empresaId=&incluirInactivos=`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` | `EstablecimientoDto(Id, EmpresaId, EmpresaNombre, Nombre, Ubicacion, Responsables, SuperficieHa, Activo, FechaCreacion, Version)` | `empresaId` solo tiene efecto para el administrador general. |
| Potreros | `GET?establecimientoId=&incluirInactivos=`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` | `PotreroDto(Id, EmpresaId, EstablecimientoId, EstablecimientoNombre, Nombre, SuperficieHa, Limites, Activo, FechaCreacion, Version)` | La empresa se deriva del establecimiento. |
| Cultivos | `GET?incluirInactivos=`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` | `CultivoDto(Id, EmpresaId, Nombre, Activo, FechaCreacion, Version)` | No acepta filtro por empresa; el AG recibe todas. |
| Campañas | `GET?incluirAnuladas=`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` | `CampanaDto(Id, EmpresaId, Nombre, CultivoId, CultivoNombre, FechaInicio, FechaFin, Estado, FechaCreacion, Version, Potreros[])` | `DELETE` anula. `PUT` **no** modifica potreros asignados. |
| Usuarios | `GET?incluirInactivos=`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}`, `PUT/{id}/contrasena` | `UsuarioDto(Id, Nombre, Email, Rol, Activo, EmpresaId, EmpresaNombre, Establecimientos[], FechaCreacion, Version)` | Crear exige `Contrasena` de 8+ caracteres. Editar no cambia email ni contraseña. |
| Sincronización | `POST /api/v1/sincronizacion` | — | Exclusivo de la app móvil; fuera de alcance. |

**Módulos que la API todavía no expone:** Eventos, Fotografías, Tareas, Tipos de evento, Tipos de tarea, Insumos, Maquinaria, Contratistas, Reportes y Auditoría. Permanecen mock en M2.

---

## 4. Alcance de M2

### Incluido

- Contexto de empresa por circuito y selector en la interfaz.
- Notificador único para todos los servicios y páginas.
- Interfaces asíncronas y adaptadores HTTP para Establecimientos, Potreros, Cultivos, Campañas y Usuarios.
- Conversión a asíncrono de las páginas de esos módulos.
- Alineación de la matriz de permisos del frontend con la API.
- Corrección de defectos de contrato detectados (fechas por defecto, campos que la UI no edita, roles, contraseña, etc.).
- Pruebas de build, adaptadores y UI en modo API y modo mock.

### Fuera de alcance (M3)

- Eventos, fotografías, tareas, catálogos, insumos, maquinaria, contratistas, reportes y auditoría contra API.
- Sincronización móvil.
- Persistencia de la empresa seleccionada entre sesiones o dispositivos.
- Notificaciones entre circuitos o pestañas distintas.
- Autenticación con refresh token o renovación automática.

---

## 5. Decisiones de arquitectura

### 5.1 `ContextoEmpresa`: una sola fuente de verdad

Se crea un servicio `scoped` (una instancia por circuito de Blazor):

```csharp
public interface IContextoEmpresa
{
    Guid? EmpresaActivaId { get; }
    Empresa? EmpresaActiva { get; }
    IReadOnlyList<Empresa> Disponibles { get; }
    bool PuedeCambiar { get; }
    event Action? Cambiada;

    Task InicializarAsync(CancellationToken cancellationToken = default);
    Task<bool> SeleccionarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task RefrescarAsync(CancellationToken cancellationToken = default);
}
```

Reglas:

- **Administrador de empresa, ingeniero, operario y contratista:** `EmpresaActivaId = CurrentUser.EmpresaId`, `PuedeCambiar = false`.
- **Administrador general:** `PuedeCambiar = true`; se listan solo empresas activas y se elige una. No se admite “Todas” en M2 porque las operaciones de alta necesitan una empresa destino.
- `SeleccionarAsync` valida que la empresa exista, esté activa y que `IUserScope.CanAccessEmpresa` la permita.
- Al cambiar la empresa se dispara `Cambiada` **y** `NotificadorDatos.Notificar(ModulosDatos.EmpresaActiva)`.
- `MainLayout` llama a `InicializarAsync` cuando hay sesión. También lo hace la página de Login después de un login exitoso, para que el contexto esté listo antes de navegar.
- Logout y expiración limpian el contexto.

`SesionService` deja de exponer `EmpresaActivaId` y `PrimeraEmpresaAsync`; esa responsabilidad pasa a `ContextoEmpresa`. Esto evita duplicar estado y mantiene `SesionService` enfocado en autenticación.

**Ubicación del selector:** en el pie del sidebar, arriba del resumen de usuario. Para roles sin cambio se muestra el nombre de la empresa como texto; para el administrador general, un `InputSelect` con las empresas activas.

### 5.2 Interfaces de servicio y adaptadores

Se agregan interfaces con firmas asíncronas, siguiendo `IServicioEmpresas`:

```csharp
public interface IServicioEstablecimientos
{
    Task<IReadOnlyList<Establecimiento>> GetAllAsync(Guid? empresaId = null, bool includeInactive = false, CancellationToken ct = default);
    Task<Establecimiento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Establecimiento> CreateAsync(Establecimiento nuevo, CancellationToken ct = default);
    Task<bool> UpdateAsync(Establecimiento actualizado, long expectedVersion = 0, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken ct = default);
}
```

Análogamente: `IServicioPotreros`, `IServicioCultivos`, `IServicioCampanas`, `IServicioUsuarios`.

Cada interfaz tiene dos implementaciones:

- **Mock:** la clase existente (`EstablecimientoService`, etc.), adaptada para implementar la interfaz y notificar por `NotificadorDatos`.
- **API:** `ServicioEstablecimientosApi`, `ServicioPotrerosApi`, `ServicioCultivosApi`, `ServicioCampanasApi`, `ServicioUsuariosApi`.

`Program.cs` elige una u otra con el mismo patrón que Empresas:

```csharp
builder.Services.AddScoped<ServicioEstablecimientosApi>();
builder.Services.AddScoped<IServicioEstablecimientos>(sp =>
    useApi
        ? sp.GetRequiredService<ServicioEstablecimientosApi>()
        : sp.GetRequiredService<EstablecimientoService>());
```

**Importante:** los módulos mock que dependen de estas entidades (Eventos, Tareas, Reportes, Historial) deben seguir usando las implementaciones concretas mock, no las interfaces, para no mezclar datos de API con datos mock. Es una limitación consciente de M2 y se documenta en la UI.

### 5.3 `NotificadorDatos` transversal

`ModulosDatos` se amplía:

```csharp
public static class ModulosDatos
{
    public const string Empresas = "empresas";
    public const string Establecimientos = "establecimientos";
    public const string Potreros = "potreros";
    public const string Cultivos = "cultivos";
    public const string Campanas = "campanas";
    public const string Usuarios = "usuarios";
    public const string TiposEvento = "tipos-evento";
    public const string TiposTarea = "tipos-tarea";
    public const string Eventos = "eventos";
    public const string Fotos = "fotos";
    public const string Tareas = "tareas";
    public const string Insumos = "insumos";
    public const string Maquinaria = "maquinaria";
    public const string Contratistas = "contratistas";
    public const string Auditoria = "auditoria";
    public const string EmpresaActiva = "empresa-activa";
}
```

Reglas de implementación:

- Todo servicio notifica `NotificadorDatos.Notificar(...)` después de una mutación exitosa.
- `StateChanged` se mantiene **temporalmente** como alias mientras se migran las páginas; al final de M2 se elimina de todos los servicios y páginas.
- Las páginas se suscriben en `OnInitialized`, se desuscriben en `Dispose` y filtran por módulo:

```csharp
private void HandleChanged(string modulo)
{
    if (modulo != ModulosDatos.Potreros && modulo != ModulosDatos.EmpresaActiva)
        return;

    _ = InvokeAsync(async () =>
    {
        await LoadAsync();
        StateHasChanged();
    });
}
```

- `AuditService` también notifica `Auditoria` para que Reportes y Auditoría se refresquen.
- El notificador es `scoped`: no propaga cambios entre pestañas ni circuitos. Se documenta como limitación conocida.

### 5.4 Política de modo híbrido

Con `UseApi=true`:

- Los módulos con API usan HTTP.
- Los módulos sin API siguen usando mock.
- Las pantallas mock muestran una banda discreta: “Datos de demostración: este módulo todavía no está conectado a la API”.
- El menú puede mostrar un punto o etiqueta “demo” en esos módulos.

Esto evita el peor escenario: que un usuario vea establecimientos de la API y campañas mock con IDs que no existen en la API, sin saberlo. La banda y la etiqueta se retiran en M3 cuando el módulo se migra.

### 5.5 Reglas de mapeo y defectos de contrato

Todos los adaptadores siguen el patrón de `ServicioEmpresasApi`:

1. DTO local en `Api/Contratos/`.
2. Mapeo en `Api/MapeoApi.cs`.
3. `GetById` traduce `404` a `null`.
4. `Update`/`Deactivate` traducen `404` a `false`.
5. `expectedVersion == 0` significa “traer la versión actual antes de actualizar”.
6. Los campos que la UI no edita se conservan leyendo el DTO actual antes del `PUT`.
7. No se llama a `AuditService` desde un adaptador: la API ya audita. Hacerlo duplicaría registros.
8. Se notifica `NotificadorDatos` solo después de una mutación exitosa.

Defectos detectados que M2 debe neutralizar:

- **`FechaCreacion` de la API vuelve `0001-01-01`.** `AuditoriaInterceptor` tiene un bug de asignación. Mientras el equipo de API lo corrige, el frontend muestra `—` cuando la fecha es anterior a 2000. No se inventa una fecha.
- **`PUT` de empresa borra `RazonSocial`.** Ya resuelto en M1 leyendo el DTO actual; se replica el criterio en el resto.
- **`PUT` de campaña no actualiza potreros.** La UI debe mostrar los potreros como solo lectura en edición, o bien pedir el cambio de contrato a la API.
- **Crear campaña siempre deja estado `Planificada`.** La UI no debe ofrecer estado editable al crear; se cambia después con “Editar”.
- **Crear usuario exige contraseña.** La UI actual no tiene campo de contraseña; M2 lo agrega solo en alta.
- **Editar usuario no cambia email.** En edición, el email pasa a solo lectura.
- **Un administrador de empresa no puede gestionarse a sí mismo.** La UI debe ocultar “Editar” sobre el propio usuario cuando el rol es Administrador de empresa.
- **Operario de campo exige al menos un establecimiento.** Validación de UI antes de enviar.
- **Listados de cultivos, campañas y usuarios no aceptan filtro por empresa.** Para el administrador general se filtran en el cliente por `EmpresaId` de la empresa activa. Para el resto, la API ya filtra por token.

### 5.6 Alineación de permisos

La matriz del frontend y la de la API difieren en gestión. M2 corrige `Services/Permissions.cs`:

| Módulo | Frontend hoy | API | Corrección M2 |
|---|---|---|---|
| Establecimientos | solo Administrador de empresa | AG + AE | Agregar AG |
| Potreros | solo Administrador de empresa | AG + AE | Agregar AG |
| Campañas | AE + IA | AG + AE + IA | Agregar AG |
| Cultivos | AE + IA (`CanManageCultivos`) | AG + AE + IA | Agregar AG |
| Catálogos | solo AE | AG + AE + IA | Agregar AG + IA |

La UI sigue siendo solo una primera barrera: la API vuelve a validar cada operación.

### 5.7 Solicitudes de cambio a la API (para el compañero)

No bloquean M2, pero conviene acordarlas:

1. **Corregir `FechaCreacion` en `AuditoriaInterceptor`.**
   Línea actual:
   ```csharp
   entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion = default ? ahora : entrada.Entity.FechaCreacion;
   ```
   Corrección propuesta:
   ```csharp
   entrada.Entity.FechaCreacion = entrada.Entity.FechaCreacion == default ? ahora : entrada.Entity.FechaCreacion;
   ```
2. **Agregar `PotreroIds` a `ActualizarCampanaDto`** y sincronizar asignaciones en `ServicioCampanas.ActualizarAsync`, para que editar una campaña no obligue a dejar los potreros fijos.
3. **Agregar filtro `empresaId` a `GET /cultivos`, `GET /campanas` y `GET /usuarios`** para el administrador general. El filtrado en cliente funciona, pero no escala.
4. **Definir el cierre por potrero.** Hoy la API solo cierra la campaña completa; si la UI necesita cerrar un potrero suelto, hace falta endpoint o campo en el `PUT`.
5. **Permitir que un administrador de empresa edite su propio usuario** sin cambiar de rol, si se quiere conservar la funcionalidad actual de la UI.

---

## 6. Paquetes de trabajo

### M2.1 — Contexto de empresa y selector (1,5 días)

**Objetivo:** que todas las operaciones tengan una empresa destino explícita y que el administrador general pueda cambiarla.

**Archivos nuevos**

- `Services/ContextoEmpresa/IContextoEmpresa.cs`
- `Services/ContextoEmpresa/ContextoEmpresa.cs`

**Archivos a modificar**

- `Program.cs` — registrar `IContextoEmpresa`.
- `Services/Sesion/SesionService.cs` — quitar `EmpresaActivaId` y `PrimeraEmpresaAsync`.
- `Components/Layout/MainLayout.razor` — inicializar contexto y mostrar selector.
- `Components/Pages/Login.razor` — inicializar contexto después del login.
- `Components/Pages/Empresas.razor` — refrescar contexto después de crear/desactivar.
- `Services/NotificadorDatos.cs` — agregar `EmpresaActiva`.
- `wwwroot/app.css` — estilos del selector y de la etiqueta “demo”.

**Tareas**

- [ ] Implementar `InicializarAsync`, `SeleccionarAsync`, `RefrescarAsync`.
- [ ] Resolver empresa fija para roles con `EmpresaId`.
- [ ] Cargar empresas activas para el AG y elegir la primera si no hay selección válida.
- [ ] Limpiar contexto en logout y expiración.
- [ ] Mostrar selector solo si `PuedeCambiar`.
- [ ] Notificar `EmpresaActiva` al cambiar.
- [ ] Impedir seleccionar empresas inactivas.

**Terminado cuando**

- El AG cambia de empresa y las páginas migradas recargan sus listados.
- Un AE ve su empresa fija, sin selector.
- Al cerrar sesión y entrar con otro usuario, no queda ninguna empresa anterior seleccionada.
- Si la empresa activa se desactiva desde Empresas, el contexto elige otra o queda vacío sin romper la UI.

---

### M2.2 — Notificador de datos transversal (1,5 días)

**Objetivo:** eliminar la dependencia de `StateChanged` por servicio y dejar un único mecanismo de refresco.

**Archivos a modificar**

- `Services/NotificadorDatos.cs` — constantes de todos los módulos.
- Todos los servicios de `Services/*.cs` — inyectar `NotificadorDatos` y notificar.
- Todas las páginas de `Components/Pages/*.razor` — suscribirse por módulo.
- `Services/AuditService.cs` — notificar `Auditoria`.
- `Services/FotografiaService.cs` — notificar `Fotos`.
- `Components/Pages/Home.razor`, `Reportes.razor`, `Historial.razor` — escuchar varios módulos.

**Tareas**

- [ ] Agregar constantes de módulo.
- [ ] Reemplazar `StateChanged?.Invoke()` por `Notificar(...)` en cada servicio mock.
- [ ] Mantener `StateChanged` solo mientras quede alguna página sin migrar.
- [ ] Migrar todas las páginas al patrón `HandleChanged(string modulo)`.
- [ ] Asegurar `Dispose` en todas las páginas.
- [ ] Evitar dobles cargas cuando una operación notifica dos módulos.
- [ ] Quitar `StateChanged` de servicios y páginas al final del paquete.

**Terminado cuando**

- No queda ninguna página suscripta a `StateChanged` de un servicio concreto.
- Crear/editar/desactivar en un módulo refresca su listado y los tableros que dependen de él.
- Navegar entre páginas no duplica handlers ni llamadas HTTP.

---

### M2.3 — Base de contratos, mapeos y permisos (0,5 días)

**Objetivo:** dejar el terreno preparado antes de tocar páginas.

**Archivos nuevos**

- `Api/Contratos/Establecimientos.cs`
- `Api/Contratos/Potreros.cs`
- `Api/Contratos/Cultivos.cs`
- `Api/Contratos/Campanas.cs`
- `Api/Contratos/Usuarios.cs`

**Archivos a modificar**

- `Api/MapeoApi.cs` — métodos `AModelo` para cada DTO.
- `Services/Permissions.cs` — alineación de la tabla 5.6.
- `Services/SigaTime.cs` — `FormatOrDash` para fechas `default`.
- `Models/` — campos faltantes: `Cultivo.Version`, `Cultivo.CreatedAt`, `Potrero.EmpresaId`, `CampanaPotrero.PotreroNombre`.

**Tareas**

- [ ] Crear DTOs locales idénticos al contrato real (camelCase se resuelve con `JsonSerializerDefaults.Web`).
- [ ] Mapear enums de estado (`EstadoCampana`).
- [ ] Agregar `FormatOrDash` y usarlo en todas las tablas.
- [ ] Corregir la matriz de permisos.
- [ ] Extender modelos de UI sin romper los servicios mock existentes.

**Terminado cuando**

- El proyecto compila con los DTOs nuevos aunque todavía no se usen.
- Los tests de mapeo cubren fechas `default`, enums y campos opcionales.

---

### M2.4 — Establecimientos contra API (1,5 días)

**Objetivo:** migrar el primer módulo de la cadena estructural.

**Archivos nuevos**

- `Services/IServicioEstablecimientos.cs`
- `Services/Api/ServicioEstablecimientosApi.cs`

**Archivos a modificar**

- `Services/EstablecimientoService.cs` — implementar la interfaz y notificar.
- `Program.cs` — registro condicional.
- `Components/Pages/Establecimientos.razor` — asíncrono, carga, error, empresa activa.
- `Components/Pages/EstablecimientoForm.razor` — recibir empresas/establecimientos por parámetro o interfaz; no inyectar `EmpresaService` concreto.
- `Components/Pages/Usuarios.razor` — si sigue mock, no tocar en este paquete.

**Tareas**

- [ ] `GetAllAsync` envía `empresaId` solo si el usuario es AG.
- [ ] `CreateAsync` envía `EmpresaId = ContextoEmpresa.EmpresaActivaId` para AG; para el resto lo omite.
- [ ] `UpdateAsync` conserva campos no editados y usa `Version`.
- [ ] `DeactivateAsync` usa `DELETE`.
- [ ] La página muestra “Cargando…”, error de API y lista vacía.
- [ ] El formulario bloquea la empresa para roles no AG y la preselecciona.
- [ ] Se filtran empresas inactivas, salvo la ya asignada al registro editado.
- [ ] `CanManage` incluye AG.
- [ ] Al cambiar `EmpresaActiva`, la página recarga.

**Terminado cuando**

- AG crea un establecimiento en la empresa seleccionada y lo ve en la lista.
- AE solo ve y crea establecimientos de su empresa.
- Editar conserva `Ubicacion`, `Responsables` y `SuperficieHa`.
- Desactivar lo marca inactivo sin borrar historial.
- Un intento de editar con versión vieja muestra el mensaje de conflicto de la API.

---

### M2.5 — Potreros contra API (1 día)

**Objetivo:** completar la estructura agrícola.

**Archivos nuevos**

- `Services/IServicioPotreros.cs`
- `Services/Api/ServicioPotrerosApi.cs`

**Archivos a modificar**

- `Services/PotreroService.cs`
- `Program.cs`
- `Components/Pages/Potreros.razor`
- `Components/Pages/PotreroForm.razor`

**Tareas**

- [ ] Listar potreros por establecimiento o todos los de la empresa activa.
- [ ] Crear con `EstablecimientoId` validado contra la empresa activa.
- [ ] Editar nombre, superficie y límites; conservar `Version`.
- [ ] Desactivar con `DELETE`.
- [ ] Formulario: establecimientos activos de la empresa activa; conservar el establecimiento actual si está inactivo.
- [ ] Mostrar nombre del establecimiento en la tabla.
- [ ] `CanManage` incluye AG.
- [ ] Recargar al cambiar empresa activa.

**Terminado cuando**

- El AG ve solo potreros de la empresa seleccionada.
- AE solo ve los de su empresa.
- No se puede crear un potrero con un establecimiento de otra empresa.
- La tabla muestra el establecimiento correcto.

---

### M2.6 — Cultivos y Campañas contra API (2 días)

**Objetivo:** migrar la cadena productiva, la más sensible por reglas de negocio y concurrencia.

**Archivos nuevos**

- `Services/IServicioCultivos.cs`
- `Services/Api/ServicioCultivosApi.cs`
- `Services/IServicioCampanas.cs`
- `Services/Api/ServicioCampanasApi.cs`

**Archivos a modificar**

- `Services/CultivoService.cs`
- `Services/CampanaService.cs`
- `Program.cs`
- `Components/Pages/Cultivos.razor`
- `Components/Pages/Campanas.razor`
- `Components/Pages/CampanaForm.razor`

**Tareas de Cultivos**

- [ ] Listar con `incluirInactivos`; filtrar por empresa activa en cliente para AG.
- [ ] Crear con `EmpresaId` para AG.
- [ ] Editar nombre y estado; conservar `Version`.
- [ ] Desactivar con `DELETE`.
- [ ] Mostrar `FechaCreacion` con `FormatOrDash`.
- [ ] `CanManageCultivos` incluye AG.

**Tareas de Campañas**

- [ ] Listar con `incluirAnuladas`; filtrar por `EmpresaId` en cliente para AG.
- [ ] Mapear `CampanaDto.Potreros` a `CampanaPotrero` y mostrar nombres.
- [ ] Crear con `EmpresaId`, `CultivoId`, fechas y `PotreroIds`; el estado inicial queda `Planificada`.
- [ ] Editar nombre, cultivo, fechas y estado; **no** permitir cambiar potreros (mostrarlos como solo lectura).
- [ ] “Cerrar” envía `Estado = Cerrada` por `PUT`.
- [ ] “Anular” usa `DELETE` y conserva historial.
- [ ] Validar que `FechaFin >= FechaInicio`.
- [ ] `CanManage` incluye AG.
- [ ] Recargar al cambiar empresa activa.

**Terminado cuando**

- AG crea una campaña para la empresa seleccionada con potreros de esa empresa.
- AE e IA operan solo dentro de su empresa.
- Editar una campaña no pierde potreros ni cultivo.
- Cerrar y anular se reflejan en la lista y en el historial mock.
- Un cultivo inactivo no se puede asignar a una campaña nueva.
- Una campaña con potreros solapados muestra el conflicto de la API.

---

### M2.7 — Usuarios contra API (2 días)

**Objetivo:** migrar el último módulo con contrato disponible, respetando reglas de roles y alcances.

**Archivos nuevos**

- `Services/IServicioUsuarios.cs`
- `Services/Api/ServicioUsuariosApi.cs`

**Archivos a modificar**

- `Services/UserService.cs`
- `Program.cs`
- `Components/Pages/Usuarios.razor`

**Tareas**

- [ ] Listar con `incluirInactivos`; filtrar por empresa activa en cliente para AG.
- [ ] Alta: agregar campo de contraseña (mínimo 8) y enviarlo solo en creación.
- [ ] Alta: enviar `EmpresaId` para AG; para AE la API la toma del token.
- [ ] Alta: enviar `EstablecimientoIds`; validar operario con al menos uno.
- [ ] Edición: email en solo lectura; no enviar contraseña.
- [ ] Edición: filtrar roles asignables según el rol actual.
- [ ] Ocultar edición del propio usuario para AE.
- [ ] Desactivar con `DELETE`; no permitir desactivarse a sí mismo.
- [ ] Mostrar empresa y establecimientos con los nombres que devuelve la API.
- [ ] Cambiar contraseña: acción separada que usa `PUT /usuarios/{id}/contrasena`.
- [ ] Recargar al cambiar empresa activa.

**Terminado cuando**

- AG crea un usuario en cualquier empresa y puede asignarle establecimientos.
- AE solo crea ingenieros, operarios y contratistas de su empresa.
- Un operario sin establecimientos es rechazado con mensaje claro.
- Editar no cambia email ni contraseña.
- Cambiar contraseña funciona y permite iniciar sesión con la nueva.
- Un AE no ve opción de editarse a sí mismo.

---

### M2.8 — Endurecimiento y pruebas (1 día)

**Objetivo:** cerrar M2 con evidencia reproducible.

**Tareas**

- [ ] Build sin warnings.
- [ ] Extender el smoke test de adaptadores a todos los módulos.
- [ ] Probar `UseApi=false` completo.
- [ ] Probar `UseApi=true` con login de AG, AE, IA y operario.
- [ ] Probar aislamiento con `empresa2@siga.demo` en cada módulo.
- [ ] Probar conflicto de versión en cada `PUT`.
- [ ] Probar expiración de token en medio de una operación.
- [ ] Probar UI con Chrome/CDP: alta, edición y baja por módulo.
- [ ] Revisar que no queden llamadas síncronas a servicios en páginas migradas.
- [ ] Revisar que no haya `StateChanged` residual.
- [ ] Documentar limitaciones del modo híbrido.

**Terminado cuando**

- Todos los flujos anteriores pasan en modo API y en modo mock.
- No hay regresiones en Empresas ni en Login.
- El README o `Docs/` explica cómo levantar API y frontend y qué módulos están integrados.

---

## 7. Matriz de mapeo por entidad

### Establecimiento

| API | Modelo UI | Transformación |
|---|---|---|
| `Id` | `Id` | Directa |
| `EmpresaId` | `EmpresaId` | Directa |
| `EmpresaNombre` | — | Solo para DTO local; la UI usa el nombre de `ContextoEmpresa` |
| `Nombre` | `Nombre` | Directa |
| `Ubicacion` | `Ubicacion` | Directa |
| `Responsables` | `Responsables` | Directa |
| `SuperficieHa` | `SuperficieHa` | Directa |
| `Activo` | `Activo` | Directa |
| `FechaCreacion` | `CreatedAt` | `FormatOrDash` si es `default` |
| `Version` | `Version` | Directa |

### Potrero

| API | Modelo UI | Transformación |
|---|---|---|
| `Id` | `Id` | Directa |
| `EmpresaId` | `EmpresaId` | Agregar al modelo |
| `EstablecimientoId` | `EstablecimientoId` | Directa |
| `EstablecimientoNombre` | — | Para tabla y DTO local |
| `Nombre` | `Nombre` | Directa |
| `SuperficieHa` | `SuperficieHa` | Directa |
| `Limites` | `Limites` | Directa |
| `Activo` | `Activo` | Directa |
| `FechaCreacion` | `CreatedAt` | `FormatOrDash` |
| `Version` | `Version` | Directa |

### Cultivo

| API | Modelo UI | Transformación |
|---|---|---|
| `Id` | `Id` | Directa |
| `EmpresaId` | `EmpresaId` | Directa |
| `Nombre` | `Nombre` | Directa |
| `Activo` | `Activo` | Directa |
| `FechaCreacion` | `CreatedAt` | Agregar al modelo; `FormatOrDash` |
| `Version` | `Version` | Agregar al modelo |

### Campaña

| API | Modelo UI | Transformación |
|---|---|---|
| `Id` | `Id` | Directa |
| `EmpresaId` | `EmpresaId` | Directa |
| `Nombre` | `Nombre` | Directa |
| `CultivoId` | `CultivoId` | Directa |
| `CultivoNombre` | — | Para tabla y DTO local |
| `FechaInicio` / `FechaFin` | Iguales | Directa; mostrar con `SigaTime` |
| `Estado` | `Estado` | Enums homónimos |
| `FechaCreacion` | `CreatedAt` | `FormatOrDash` |
| `Version` | `Version` | Directa |
| `Potreros[]` | `GetPotreros` | Mapear a `CampanaPotrero` con `PotreroNombre` |

### Usuario

| API | Modelo UI | Transformación |
|---|---|---|
| `Id` | `Id` | Directa |
| `Nombre` | `Nombre` | Directa |
| `Email` | `Email` | Directa; solo lectura en edición |
| `Rol` | `Rol` | Enums homónimos |
| `Activo` | `Activo` | Directa |
| `EmpresaId` / `EmpresaNombre` | `EmpresaId` | El nombre se muestra desde el DTO o `ContextoEmpresa` |
| `Establecimientos[]` | `EstablecimientoIds` | Extraer IDs |
| `FechaCreacion` | `CreatedAt` | `FormatOrDash` |
| `Version` | `Version` | Directa |

---

## 8. Plan de pruebas

### 8.1 Build

```powershell
dotnet build ObligatorioFrontEnd\ObligatorioFrontEnd.csproj -v minimal
```

Criterio: 0 errores y 0 advertencias.

### 8.2 Smoke de adaptadores

Extender `M1Smoke` (fuera de los repos) con una sección por módulo:

- Login AG y AE.
- Listar, crear, editar y desactivar.
- Verificar `Version` incrementada.
- Verificar `RazonSocial` y campos no editados preservados.
- Verificar `404 → null/false`.
- Verificar `401 → Expirada`.
- Verificar notificaciones por módulo.
- Verificar filtrado por empresa.

### 8.3 UI con Chrome/CDP

Por módulo:

1. Login como AG.
2. Elegir empresa.
3. Alta.
4. Edición con versión actual.
5. Baja/desactivación.
6. Cambio de empresa y verificación de listados.

Repetir con `empresa2@siga.demo` para aislamiento.

### 8.4 Casos borde

- Empresa activa desactivada mientras se opera.
- Edición con versión vieja.
- Token expirado en medio de un `PUT`.
- API caída al cargar una página.
- Lista vacía.
- Fechas `default`.
- Campaña con potreros solapados.
- Operario sin establecimientos.
- AE intentando editar su propio usuario.
- AG sin empresas activas.

---

## 9. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Mezclar datos API y mock sin aviso | Alto | Banda “demo” y etiqueta en el menú; no compartir IDs entre fuentes. |
| `FechaCreacion` en `0001` | Medio | `FormatOrDash` y solicitud de fix a la API. |
| `PUT` de campaña no cambia potreros | Medio | Potreros solo lectura en edición; solicitud de cambio de contrato. |
| Notificador `scoped` no cruza pestañas | Bajo | Documentar; M3 puede usar singleton + SignalR. |
| Doble auditoría | Alto | Los adaptadores no llaman a `AuditService`. |
| Doble carga por notificar dos módulos | Bajo | Filtro por módulo y una sola recarga por evento. |
| Pérdida de campos no editados | Alto | `GET` previo al `PUT` y conservación explícita. |
| Roles de usuario divergentes | Medio | Filtrar roles y ocultar acciones no permitidas; la API sigue validando. |
| Expiración de sesión en operación | Medio | `TokenStore` ya dispara `Expirada`; las páginas muestran error y vuelven al login. |

---

## 10. Criterio de terminado de M2

- [ ] `ApiSiga:UseApi=true` funciona con login real y empresa seleccionable.
- [ ] Empresas, Establecimientos, Potreros, Cultivos, Campañas y Usuarios operan contra la API.
- [ ] El resto de los módulos sigue funcionando con mock y está identificado como demostración.
- [ ] `ApiSiga:UseApi=false` mantiene la aplicación completa como hoy.
- [ ] No hay `ProjectReference` ni código copiado de la API.
- [ ] No hay páginas migradas con llamadas síncronas ni suscripciones a `StateChanged`.
- [ ] Build sin warnings.
- [ ] Smoke de adaptadores y pruebas de UI aprobadas.
- [ ] Aislamiento por empresa verificado en todos los módulos migrados.
- [ ] Documentación de limitaciones y solicitudes de cambio a la API.

---

## 11. Vista previa de M3

Una vez que la API exponga los módulos restantes:

1. Eventos y fotografías.
2. Tareas y asignaciones.
3. Catálogos: tipos de evento y tipos de tarea.
4. Insumos, maquinaria y contratistas.
5. Reportes y auditoría.
6. Sincronización y notificaciones entre circuitos.

M2 deja preparado el terreno: interfaces, contexto de empresa, notificador único y patrón de adaptador.
