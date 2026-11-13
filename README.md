# SIGA — Frontend Blazor

Frontend del Sistema Integrado de Gestión Agrícola (SIGA), construido con **Blazor Server** sobre **.NET 10**. Todos los datos se consumen de **ApiSiga por HTTP**; el frontend no comparte proyecto, submódulo ni código copiado con la API.

La referencia funcional es [`SIGA_Analisis_y_Propuesta_v2.docx`](ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx). El plan de trabajo histórico está en [`PLAN_FRONTEND_SIGA.md`](ObligatorioFrontEnd/Docs/PLAN_FRONTEND_SIGA.md) y el detalle de la integración en [`INTEGRACION_API_M2.md`](ObligatorioFrontEnd/Docs/INTEGRACION_API_M2.md).

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Navegador moderno
- ApiSiga escuchando en `ApiSiga:BaseUrl` (por defecto `http://localhost:5100/`)

## Ejecutar

```powershell
dotnet run --project ObligatorioFrontEnd
```

Luego abrir la URL que muestra la consola (por defecto `http://localhost:5021`).

### Modos de ejecución

Todos los módulos consultan ApiSiga. La clave `ApiSiga:UseApi` decide **únicamente el origen del login**:

| Archivo | `UseApi` | Login |
| --- | --- | --- |
| `appsettings.json` | `true` | ApiSiga (`POST /api/v1/auth/login`) con JWT |
| `appsettings.Development.json` | `true` | ApiSiga (`POST /api/v1/auth/login`) con JWT |

`UseApi=false` queda solo como alternativa para entrar con `DemoAuthService` sin API.
Como ese login no emite JWT, las consultas de datos responderán `401`.

Se puede forzar por variable de entorno, que tiene prioridad sobre los JSON:

```powershell
# Login real contra la API
$env:ApiSiga__UseApi = 'true'
$env:ApiSiga__BaseUrl = 'http://localhost:5100/'
dotnet run --project ObligatorioFrontEnd

# Login demo (los datos se siguen pidiendo a la API)
$env:ApiSiga__UseApi = 'false'
dotnet run --project ObligatorioFrontEnd
```

> **Importante:** `DemoAuthService` no emite JWT. Con `UseApi=false` la API responde
> `401` a todas las consultas y las pantallas muestran listas vacías o el mensaje de
> error, sin romperse. Para ver datos reales hay que usar `UseApi=true` con un usuario
> válido de ApiSiga.

Compilar sin ejecutar:

```powershell
dotnet build ObligatorioFrontEnd.slnx -nologo
```

## Usuarios de ApiSiga (semilla)

Son los usuarios creados por la semilla de ApiSiga. Todos usan la contraseña **`siga2026`**.

| Email | Rol | Alcance |
| --- | --- | --- |
| `admin@siga.demo` | Administrador General | Toda la plataforma |
| `empresa@siga.demo` | Administrador de Empresa | Agro del Sur |
| `ingeniero@siga.demo` | Ingeniero Agrónomo | Agro del Sur |
| `operario@siga.demo` | Operario de Campo | Agro del Sur · Establecimiento La Esperanza |
| `empresa2@siga.demo` | Administrador de Empresa | Campos Verdes |
| `contratista@siga.demo` | Contratista | Agro del Sur · solo tareas asignadas |

## Módulos

Todos los módulos usan DTOs locales en `Api/Contratos/` y adaptadores HTTP en
`Services/Api/`:

- **Empresas**, **Establecimientos**, **Potreros**, **Cultivos**, **Campañas** y
  **Usuarios** contra los endpoints ya existentes de ApiSiga.
- **Tipos de evento**, **Tipos de tarea**, **Insumos**, **Maquinaria** y
  **Contratistas** con alta, edición y baja lógica.
- **Eventos** con ubicación, GPS, fotografías por `multipart/form-data`, anulación
  con motivo y registro masivo de un evento por potrero.
- **Tareas** con ciclo de vida, asignación a usuarios o contratistas, insumos,
  maquinaria, costos, resultados, verificación y anulación.
- **Historial** cronológico por campaña, calculado en el cliente.
- **Reportes**, **Auditoría** e **Inicio** con métricas y actividad reciente. Los
  reportes se agregan en el cliente (`Services/Reportes/CalculoReportes.cs`); no
  existe un endpoint `/reportes`.

Si un endpoint todavía no está disponible, la página muestra la lista vacía o el
mensaje de error de la API y sigue siendo navegable.

### Rutas esperadas en ApiSiga

| Módulo | Rutas |
| --- | --- |
| Auth | `POST /api/v1/auth/login`, `GET /api/v1/auth/yo` |
| Empresas | `GET/POST /api/v1/empresas`, `GET/PUT/DELETE /api/v1/empresas/{id}` |
| Establecimientos | `GET/POST /api/v1/establecimientos`, `GET/PUT/DELETE /{id}` |
| Potreros | `GET/POST /api/v1/potreros`, `GET/PUT/DELETE /{id}` |
| Cultivos | `GET/POST /api/v1/cultivos`, `GET/PUT/DELETE /{id}` |
| Campañas | `GET/POST /api/v1/campanas`, `GET/PUT/DELETE /{id}` |
| Usuarios | `GET/POST /api/v1/usuarios`, `GET/PUT/DELETE /{id}`, `PUT /{id}/contrasena` |
| Tipos de evento | `GET/POST /api/v1/tipos-evento`, `GET/PUT/DELETE /{id}` |
| Tipos de tarea | `GET/POST /api/v1/tipos-tarea`, `GET/PUT/DELETE /{id}` |
| Insumos | `GET/POST /api/v1/insumos`, `GET/PUT/DELETE /{id}` |
| Maquinaria | `GET/POST /api/v1/maquinaria`, `GET/PUT/DELETE /{id}` |
| Contratistas | `GET/POST /api/v1/contratistas`, `GET/PUT/DELETE /{id}` |
| Eventos | `GET/POST /api/v1/eventos`, `GET/PUT/DELETE /{id}?motivoAnulacion=` |
| Fotografías | `POST /api/v1/eventos/{id}/fotografias` (campo `archivo`), `PUT /{id}/fotografias/{fotoId}/ocultar`, `GET /{id}/fotografias/{fotoId}/archivo` |
| Tareas | `GET/POST /api/v1/tareas`, `GET/PUT/DELETE /{id}`, `PUT /{id}/asignar`, `PUT /{id}/realizar`, `PUT /{id}/verificar`, `PUT /{id}/anular` |
| Auditoría | `GET /api/v1/auditoria` con `entidad`, `actor`, `desde`, `hasta`, `empresaId`, `pagina`, `tamanoPagina` (máx. 200) |

La carga de fotos usa un request por archivo con el campo multipart `archivo` y los
campos `fechaCaptura`, `latitud` y `longitud`. La visualización descarga el binario de
forma autenticada y lo convierte a `data:` URL, porque un `<img src>` directo no puede
enviar el bearer token.

La matriz de permisos de la sección 4.1 del documento v2 está centralizada en
`Services/Permissions.cs`. La interfaz oculta opciones, pero cada adaptador vuelve
a validar permisos y alcance antes de operar; la propia ApiSiga repite esas
validaciones.

## Sesión y empresa activa

- **Modo API:** el login se valida contra `POST /api/v1/auth/login`. El JWT se guarda en `TokenStore`, en el servidor Blazor del circuito, y **nunca** se envía al navegador.
- **Modo demo:** la autenticación es simulada con los usuarios demo y la misma contraseña; no hay JWT.
- La sesión sobrevive a un **F5**. El navegador solo guarda un identificador opaco en `ProtectedSessionStorage`; los datos de la sesión viven en un `AlmacenSesiones` singleton del servidor.
- La **empresa activa** del administrador general también se guarda y se restaura tras F5. Los demás roles usan la empresa fija de su usuario.
- Al crear o desactivar una empresa, el selector de empresa activa se refresca automáticamente.
- Logout, expiración del token o reinicio del servidor invalidan la sesión y vuelven a `/login`.
- La sesión no se comparte entre pestañas ni entre circuitos: cada pestaña restaura su propio circuito a partir del identificador guardado.

## Decisiones técnicas

- **Sin datos mock:** ya no existen servicios en memoria ni entidades legacy. El único componente simulado es `DemoAuthService` (login).
- **DTOs como contrato:** `Api/Contratos/` es un espejo de los contratos de ApiSiga. Los campos que la API no devuelve todavía llegan en `null`/`0` y la interfaz los muestra como `—` o lista vacía.
- **Nombres resueltos:** la API devuelve `PotreroNombre`, `TipoEventoNombre`, `TipoTareaNombre` y `EjecutorNombre`. La campaña se deduce en el cliente cruzando `CampanaPotreroId` con `CampanaDto.Potreros`.
- **Errores de API:** `ApiException` hereda de `InvalidOperationException`, así las páginas existentes muestran `exception.Message` sin cambios. Los errores de formato JSON también se traducen a `ApiException`.
- **Fotografías:** el frontend envía un archivo por request y muestra las imágenes con `data:` URL obtenidas por descarga autenticada. La API solo devuelve fotografías visibles.
- **Auditoría:** se consume la respuesta paginada real; los reportes recorren todas las páginas de 200 registros.
- **Hora:** todo se guarda en UTC y se muestra en `America/Montevideo` mediante `Services/SigaTime.cs`.
- **Historia completa:** las entidades se desactivan o anulan; no se borran.
- **Costos:** la API calcula el costo de una tarea como insumos + maquinaria. No existe costo de contratista.
- **Reportes:** se calculan en el cliente a partir de los listados descargados. Si una fuente falla con `ApiException`, esa lista se trata como vacía.

## Estructura

```
ObligatorioFrontEnd/
├─ Api/                 Cliente HTTP, excepciones y contratos (DTOs/enums)
│  └─ Contratos/        Espejo de los contratos de ApiSiga
├─ Components/          Componentes y páginas Razor
├─ Models/              Modelos de formulario y de reportes
├─ Services/            Permisos, notificaciones, sesión y cálculos
│  ├─ Api/              Adaptadores HTTP por módulo
│  ├─ ContextoEmpresa/  Empresa activa por circuito
│  ├─ Reportes/         Agregaciones del tablero
│  └─ Sesion/           Token, sesión y persistencia
├─ Docs/                Documento funcional, plan, integración y reporte de API
├─ wwwroot/             Estilos y activos estáticos
└─ Program.cs           DI, cultura es-UY y selección del login
```

## Cambios pendientes

- **Filtros por empresa en la API.** Los listados de cultivos, campañas y usuarios no aceptan `empresaId`; el administrador general filtra en el cliente.
- **Reportes del servidor.** Existen `/api/v1/reportes/actividad`, `/costos` y `/tareas`, pero el tablero todavía se agrega en el cliente.
- **Cierre por potrero.** La API expone `PUT /campanas/{id}/potreros/{campanaPotreroId}/cerrar`; la pantalla actual cierra solo la campaña completa.

## Limitaciones de esta etapa

- **Fotografías ocultas:** `EventoDto.Fotografias` solo incluye visibles, por lo que una foto ocultada deja de aparecer al recargar el evento.
- **Nombre de archivo:** la API guarda una ruta con GUID; la pantalla muestra el nombre del archivo almacenado.
- **HEIC:** se puede subir, pero la visualización depende del soporte del navegador.
- Una campaña nueva siempre se crea como **Planificada**; el estado se cambia después con Editar.
- `FechaCreacion` puede mostrarse como `—` porque ApiSiga todavía devuelve `0001-01-01` en algunas entidades (defecto del `AuditoriaInterceptor`).
