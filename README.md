# SIGA — Frontend Blazor

Frontend del Sistema Integrado de Gestión Agrícola (SIGA), construido con **Blazor Server** sobre **.NET 10**. Todos los datos se consumen de **ApiSiga por HTTP**; el frontend no comparte proyecto, submódulo ni código copiado con la API.

La referencia funcional es [`SIGA_Analisis_y_Propuesta_v2.docx`](ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx). El procedimiento de validación funcional está en [`PRUEBAS_MANUALES.md`](PRUEBAS_MANUALES.md).

Documentación de integración:

- [`ObligatorioFrontEnd/Docs/CONTRATO_API.md`](ObligatorioFrontEnd/Docs/CONTRATO_API.md): revisión endpoint por endpoint y DTOs espejo.
- [`ObligatorioFrontEnd/Docs/BUG_API_CAMPANAS.md`](ObligatorioFrontEnd/Docs/BUG_API_CAMPANAS.md): hallazgos de API resueltos y su evidencia.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Navegador moderno
- ApiSiga escuchando en `ApiSiga:BaseUrl` (por defecto `http://localhost:5100/`)

## Ejecutar

```powershell
dotnet run --project ObligatorioFrontEnd
```

Luego abrir la URL que muestra la consola (por defecto `http://localhost:5021`).

### Configuración

Todo el frontend consulta ApiSiga y el login se valida contra
`POST /api/v1/auth/login` (JWT). La URL de la API se configura con
`ApiSiga:BaseUrl`:

| Archivo | `BaseUrl` | Uso |
| --- | --- | --- |
| `appsettings.json` | `http://localhost:5100/` | Valor por defecto |
| `appsettings.Development.json` | `http://localhost:5100/` | `dotnet run` en Development |
| `appsettings.Production.json` | *(vacío)* | Obliga a definir la URL en el entorno |

En **Development** la URL de localhost ya está configurada. En **Production**
el archivo deja `BaseUrl` vacío a propósito: hay que definir la variable de
entorno `ApiSiga__BaseUrl` (o editar el JSON) antes de iniciar. Si falta o no es
una URL absoluta `http`/`https`, `Program.cs` detiene el arranque con un mensaje
claro en lugar de fallar en el primer request.

Se puede forzar por variable de entorno, que tiene prioridad sobre los JSON:

```powershell
$env:ApiSiga__BaseUrl = 'http://localhost:5100/'
dotnet run --project ObligatorioFrontEnd
```

En Production:

```powershell
$env:ApiSiga__BaseUrl = 'https://api.siga.ejemplo/'
dotnet run --project ObligatorioFrontEnd -c Release
```

Si la API no está en ejecución, las pantallas muestran el mensaje de error y
siguen siendo navegables.

Compilar y probar sin ejecutar:

```powershell
dotnet build ObligatorioFrontEnd.slnx -nologo
dotnet test tests/ObligatorioFrontEnd.Tests/ObligatorioFrontEnd.Tests.csproj
```

El proyecto `tests/ObligatorioFrontEnd.Tests` (xUnit, .NET 10) cubre la matriz
de permisos, el cliente HTTP y su caché/reintentos, la sesión, la empresa
activa, los DTOs y las agregaciones de reportes. Son **207 pruebas**.

## Usuarios de ApiSiga (semilla)

Son los usuarios creados por la semilla de ApiSiga. Todos usan la contraseña **`siga2026`**. Son datos de demostración: la pantalla de login no los muestra ni precarga credenciales.

| Email | Rol | Alcance |
| --- | --- | --- |
| `admin@siga.demo` | Administrador General | Acceso global |
| `empresa@siga.demo` | Administrador de Empresa | Agro del Sur |
| `ingeniero@siga.demo` | Ingeniero Agrónomo | Agro del Sur |
| `operario@siga.demo` | Operario de Campo | Agro del Sur · Establecimiento La Esperanza |
| `empresa2@siga.demo` | Administrador de Empresa | Campo Norte |
| `contratista@siga.demo` | Contratista | Agro del Sur · solo tareas asignadas |

## Módulos

Todos los módulos usan DTOs locales en `Api/DTOs/` y adaptadores HTTP en
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

### Mapa de establecimientos

La página `/mapa` (visible para los roles que ven Establecimientos) muestra en
**Leaflet + OpenStreetMap** los establecimientos de la empresa activa y sus
eventos geolocalizados.

- ApiSiga no expone `Latitud`/`Longitud` para establecimientos, así que el
  frontend guarda un overlay en
  `ObligatorioFrontEnd/Data/establecimientos-geo.json` (`establecimientoId`,
  `latitud`, `longitud`). Se versiona con fines de demo y se edita desde la propia
  página con el botón **Ubicar**.
- Los establecimientos sin coordenada propia se dibujan como **aproximados** en el
  centro de sus eventos geolocalizados.
- Los eventos usan `Latitud`/`Longitud` de la API y se pueden filtrar por tipo y
  rango de fechas.
- Al **registrar o editar un evento**, el formulario incluye el mismo mini-mapa:
  clic o pin arrastrable, **Usar mi ubicación**, **Buscar dirección** y
  **Quitar ubicación**. Las coordenadas manuales quedan en un desplegable de
  respaldo. El texto de `Ubicación` sigue siendo la referencia humana y, si está
  vacío, se completa con la dirección encontrada.
- La búsqueda de direcciones usa **Nominatim** (OpenStreetMap) desde el navegador y
  solo se ejecuta al presionar **Buscar**; requiere internet.
- Leaflet está vendorizado en `ObligatorioFrontEnd/wwwroot/lib/leaflet`. Los *tiles*
  se piden a OpenStreetMap, por lo que el mapa necesita conexión; si fallan, la
  página muestra un aviso y mantiene los pines y el listado.
- La edición de coordenadas reutiliza `SigaModule.Establecimientos`; un Contratista
  no ve la página.
- **Mejora futura:** agregar `Latitud`/`Longitud` (o un polígono GeoJSON) al
  establecimiento en ApiSiga y reemplazar el overlay por esos campos.

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

- **Sesión y empresa activa:** el login se valida contra ApiSiga. El JWT se guarda en `TokenStore`, en el servidor Blazor del circuito, y **nunca** se envía al navegador.
- La sesión sobrevive a un **F5**. El navegador solo guarda un identificador opaco en `ProtectedSessionStorage`; los datos de la sesión viven en un `AlmacenSesiones` singleton del servidor.
- La **empresa activa** del administrador general también se guarda y se restaura tras F5. Los demás roles usan la empresa fija de su usuario.
- Al crear o desactivar una empresa, el selector de empresa activa se refresca automáticamente.
- Logout, expiración del token o reinicio del servidor invalidan la sesión y vuelven a `/login`.
- La sesión no se comparte entre pestañas ni entre circuitos: cada pestaña restaura su propio circuito a partir del identificador guardado.

## Decisiones técnicas

- **Sin datos mock:** todos los módulos consumen ApiSiga por HTTP; no existen servicios en memoria ni entidades legacy.
- **DTOs como contrato:** `Api/DTOs/` es un espejo de los contratos de ApiSiga. Los campos que la API no devuelve todavía llegan en `null`/`0` y la interfaz los muestra como `—` o lista vacía.
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
│  └─ DTOs/             Espejo de los contratos de ApiSiga
├─ Components/          Componentes y páginas Razor
├─ Models/              Modelos de formulario y de reportes
├─ Services/            Permisos, notificaciones, sesión y cálculos
│  ├─ Api/              Adaptadores HTTP por módulo
│  ├─ ContextoEmpresa/  Empresa activa por circuito
│  ├─ Geo/              Coordenadas propias de establecimientos
│  ├─ Interfaces/       Contratos de los servicios de datos
│  ├─ Reportes/         Agregaciones del tablero
│  └─ Sesion/           Token, sesión y persistencia
├─ Data/                Coordenadas geográficas de establecimientos
├─ Docs/                Documento funcional, contrato de API y reporte de bugs
├─ wwwroot/             Estilos y activos estáticos
└─ Program.cs           DI, cultura es-UY y login contra la API
```

## Pendientes y observaciones

- **Filtro `empresaId` en la API.** Varios listados aceptan `empresaId`, pero el frontend no lo envía: descarga el alcance que la API ya limita por token y filtra en el cliente con la empresa activa. No hay fuga de datos; enviarlo reduciría el volumen transferido cuando el administrador general trabaja con muchas empresas. Ver [`CONTRATO_API.md`](ObligatorioFrontEnd/Docs/CONTRATO_API.md).
- **Reportes del servidor.** Existen `/api/v1/reportes/actividad`, `/costos` y `/tareas`, pero el tablero todavía se agrega en el cliente para combinar filtros y fuentes.
- **Cierre por potrero.** La API expone `PUT /campanas/{id}/potreros/{campanaPotreroId}/cerrar`; la pantalla actual cierra solo la campaña completa.

## Limitaciones de esta etapa

- **Coordenadas de establecimientos:** `EstablecimientoDto` no expone `Latitud`/`Longitud`. El frontend usa el overlay local `Data/establecimientos-geo.json` y dibuja los establecimientos sin coordenada como **aproximados**. Es una limitación del contrato de la API, documentada en [`CONTRATO_API.md`](ObligatorioFrontEnd/Docs/CONTRATO_API.md).
- **Fotografías ocultas:** `EventoDto.Fotografias` solo incluye visibles, por lo que una foto ocultada deja de aparecer al recargar el evento.
- **Nombre de archivo:** la API guarda una ruta con GUID; la pantalla muestra el nombre del archivo almacenado.
- **HEIC:** se puede subir, pero la visualización depende del soporte del navegador.
- Una campaña nueva siempre se crea como **Planificada**; el estado se cambia después con Editar.
- `FechaCreacion` ya se recibe correctamente: el interceptor de auditoría de ApiSiga fue corregido. `SigaTime.FormatOrDash` se conserva como defensa para registros históricos anteriores al fix. Ver [`BUG_API_CAMPANAS.md`](ObligatorioFrontEnd/Docs/BUG_API_CAMPANAS.md).
