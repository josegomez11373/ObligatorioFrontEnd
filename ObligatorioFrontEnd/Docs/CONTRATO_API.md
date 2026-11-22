# Revisión del contrato frontend ↔ ApiSiga

**Fecha:** 19/11/2026 · **Alcance:** frontend Blazor `ObligatorioFrontEnd` y API
`ApiSiga` (`ApiSigaActualizada`).

El frontend consume la API únicamente por HTTP. Este documento deja la revisión
punto por punto de rutas, verbos, parámetros y DTOs, y las diferencias conocidas
que no bloquean la entrega. **No se modificó ningún endpoint de la API.**

---

## 1. Transporte y convenciones

| Tema | Implementación |
| --- | --- |
| Cliente | `HttpClient` tipado en `Api/ClienteApi.cs` |
| URL base | `ApiSiga:BaseUrl` (Development: `http://localhost:5100/`) |
| Autenticación | `Authorization: Bearer <JWT>` agregado por `ClienteApi` |
| Serialización | `JsonSerializerDefaults.Web` (camelCase) + `JsonStringEnumConverter` |
| Enums | La API los devuelve como texto (`"Activa"`, `"Registrado"`, …) |
| Errores | `ProblemDetails` → `ApiException` con `Status`, `Message` y `Errores` |
| Reintentos | 3 intentos solo en `GET` ante error de red o `5xx` |
| Caché | `CacheDatos` por circuito, TTL 20 s, invalidada al escribir |
| Fechas | La API envía UTC; la interfaz muestra `America/Montevideo` |
| Colecciones | Los DTOs normalizan `null` a lista vacía |
| Concurrencia | Se envía `Version` en los `PUT`; `0` significa «leer la versión actual» |

---

## 2. Endpoints usados por el frontend

`✔` = usado y verificado · `—` = existe en la API pero el frontend no lo usa.

### 2.1 Autenticación

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `POST /api/v1/auth/login` | Login y obtención del JWT | `SesionService` | ✔ |
| `GET /api/v1/auth/yo` | No se usa: el login ya devuelve el usuario | — | — |

### 2.2 Empresas

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/empresas?incluirInactivas={bool}` | Listado y selector de empresa | `ServicioEmpresasApi` | ✔ |
| `GET /api/v1/empresas/{id}` | Lectura previa a editar | `ServicioEmpresasApi` | ✔ |
| `POST /api/v1/empresas` | Alta | `ServicioEmpresasApi` | ✔ |
| `PUT /api/v1/empresas/{id}` | Edición con `Version` | `ServicioEmpresasApi` | ✔ |
| `DELETE /api/v1/empresas/{id}` | Baja lógica | `ServicioEmpresasApi` | ✔ |

### 2.3 Establecimientos, potreros y cultivos

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/establecimientos?incluirInactivos={bool}` | Listado | `ServicioEstablecimientosApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/establecimientos…` | Detalle y CRUD | `ServicioEstablecimientosApi` | ✔ |
| `GET /api/v1/potreros?incluirInactivos={bool}` | Listado y formularios | `ServicioPotrerosApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/potreros…` | Detalle y CRUD | `ServicioPotrerosApi` | ✔ |
| `GET /api/v1/cultivos?incluirInactivos={bool}` | Listado y formularios | `ServicioCultivosApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/cultivos…` | Detalle y CRUD | `ServicioCultivosApi` | ✔ |

### 2.4 Campañas

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/campanas?incluirAnuladas={bool}` | Listado, eventos, tareas, reportes | `ServicioCampanasApi` | ✔ |
| `GET /api/v1/campanas/{id}` | Detalle previo a editar | `ServicioCampanasApi` | ✔ |
| `POST /api/v1/campanas` | Alta con `PotreroIds` | `ServicioCampanasApi` | ✔ |
| `PUT /api/v1/campanas/{id}` | Edición y cambio de estado | `ServicioCampanasApi` | ✔ |
| `DELETE /api/v1/campanas/{id}` | Anulación lógica | `ServicioCampanasApi` | ✔ |
| `PUT /api/v1/campanas/{id}/potreros/{campanaPotreroId}/cerrar` | No se usa: la pantalla cierra la campaña completa | — | — |

### 2.5 Usuarios y catálogos

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/usuarios?incluirInactivos={bool}` | Listado y selectores de ejecutor | `ServicioUsuariosApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/usuarios…` | CRUD de usuarios | `ServicioUsuariosApi` | ✔ |
| `PUT /api/v1/usuarios/{id}/contrasena` | Cambio de contraseña | `ServicioUsuariosApi` | ✔ |
| `GET /api/v1/tipos-evento?incluirInactivos={bool}` | Catálogo y formulario de eventos | `ServicioTiposEventoApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/tipos-evento…` | CRUD | `ServicioTiposEventoApi` | ✔ |
| `GET /api/v1/tipos-tarea?incluirInactivos={bool}` | Catálogo y formulario de tareas | `ServicioTiposTareaApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/tipos-tarea…` | CRUD | `ServicioTiposTareaApi` | ✔ |
| `GET /api/v1/insumos?incluirInactivos={bool}` | Catálogo y recursos de tareas | `ServicioInsumosApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/insumos…` | CRUD | `ServicioInsumosApi` | ✔ |
| `GET /api/v1/maquinaria?incluirInactivas={bool}` | Catálogo y recursos de tareas | `ServicioMaquinariaApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/maquinaria…` | CRUD | `ServicioMaquinariaApi` | ✔ |
| `GET /api/v1/contratistas?incluirInactivos={bool}` | Catálogo y asignación de tareas | `ServicioContratistasApi` | ✔ |
| `GET/POST/PUT/DELETE /api/v1/contratistas…` | CRUD | `ServicioContratistasApi` | ✔ |

### 2.6 Eventos y fotografías

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/eventos` con `incluirAnulados`, `campanaId`, `potreroId`, `tipoEventoId`, `desde`, `hasta` | Listado, historial, reportes y mapa | `ServicioEventosApi` | ✔ |
| `GET /api/v1/eventos/{id}` | Detalle | `ServicioEventosApi` | ✔ |
| `POST /api/v1/eventos` | Alta individual y alta por potrero | `ServicioEventosApi` | ✔ |
| `PUT /api/v1/eventos/{id}` | Edición | `ServicioEventosApi` | ✔ |
| `DELETE /api/v1/eventos/{id}?motivoAnulacion=…` | Anulación con motivo | `ServicioEventosApi` | ✔ |
| `POST /api/v1/eventos/{id}/fotografias` (`multipart/form-data`) | Alta de fotos, un request por archivo | `ServicioFotografiasApi` | ✔ |
| `PUT /api/v1/eventos/{id}/fotografias/{fotoId}/ocultar` | Ocultar foto con motivo y `Version` | `ServicioFotografiasApi` | ✔ |
| `GET /api/v1/eventos/{id}/fotografias/{fotoId}/archivo` | Descarga autenticada del binario | `ServicioFotografiasApi` | ✔ |
| `DELETE /api/v1/eventos/{id}/fotografias/{fotoId}` | No se usa: la interfaz oculta, no elimina | — | — |

### 2.7 Tareas

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/tareas` con `incluirAnuladas`, `campanaId`, `potreroId`, `estado`, `ejecutorId`, `desde`, `hasta` | Listado, historial y reportes | `ServicioTareasApi` | ✔ |
| `GET /api/v1/tareas/{id}` | Detalle | `ServicioTareasApi` | ✔ |
| `POST /api/v1/tareas` | Alta | `ServicioTareasApi` | ✔ |
| `PUT /api/v1/tareas/{id}` | Edición | `ServicioTareasApi` | ✔ |
| `PUT /api/v1/tareas/{id}/asignar` | Asignación a usuario o contratista | `ServicioTareasApi` | ✔ |
| `PUT /api/v1/tareas/{id}/realizar` | Resultado, recursos y fecha de realización | `ServicioTareasApi` | ✔ |
| `PUT /api/v1/tareas/{id}/verificar` | Verificación y fecha de comprobación | `ServicioTareasApi` | ✔ |
| `PUT /api/v1/tareas/{id}/anular` | Anulación con motivo | `ServicioTareasApi` | ✔ |
| `DELETE /api/v1/tareas/{id}?motivoAnulacion=…` | No se usa: la interfaz usa `PUT …/anular` | — | — |

### 2.8 Auditoría

| Endpoint | Uso | Adaptador | Estado |
| --- | --- | --- | --- |
| `GET /api/v1/auditoria` con `empresaId`, `entidad`, `actor`, `desde`, `hasta`, `pagina`, `tamanoPagina` | Pantalla de auditoría y actividad reciente | `ServicioAuditoriaApi` | ✔ |

`tamanoPagina` se limita a **200**, el máximo aceptado por la API. Los reportes
recorren todas las páginas.

### 2.9 Endpoints de la API no usados

| Endpoint | Motivo |
| --- | --- |
| `GET /api/v1/reportes/actividad`, `/costos`, `/tareas` | El tablero se agrega en el cliente (`CalculoReportes`) para combinar filtros y datos de varias fuentes. |
| `POST /api/v1/sincronizacion` | Es el canal de la aplicación móvil; el frontend web no sincroniza lotes. |
| `PUT /api/v1/campanas/{id}/potreros/{campanaPotreroId}/cerrar` | La pantalla actual cierra la campaña completa; el cierre por potrero queda como mejora. |
| `DELETE /api/v1/eventos/{id}/fotografias/{fotoId}` | La política del sistema es ocultar, no borrar evidencia. |
| `GET /api/v1/auth/yo` | El login devuelve todos los datos de sesión necesarios. |

---

## 3. Observaciones del contrato

### 3.1 Filtro `empresaId`

La API acepta `empresaId` en los listados de establecimientos, potreros,
cultivos, campañas, eventos, tareas, tipos, insumos, maquinaria, contratistas,
usuarios y auditoría.

El frontend **no envía** `empresaId` en esos listados: descarga el alcance que la
API ya limita por token y filtra en el cliente con `ContextoEmpresa`. Esto es
correcto para el alcance actual y no genera fuga de datos, porque la API aplica
el aislamiento por empresa antes de responder. Como mejora futura, enviar
`empresaId` reduciría el volumen transferido cuando el administrador general
trabaja con muchas empresas.

### 3.2 Fechas y zona horaria

La API devuelve `DateTime` en UTC. El frontend no reenvía fechas ya formateadas:
los formularios usan `InputDate` y la serialización envía ISO 8601. La
visualización se convierte a `America/Montevideo` en `Services/SigaTime.cs`.

### 3.3 Bajas lógicas

Todos los `DELETE` de la API son bajas lógicas o anulaciones. El frontend nunca
asume que el registro desaparece: los listados incluyen inactivos/anulados
cuando la pantalla lo necesita y muestran el estado en una columna.

### 3.4 Versionado optimista

Los `PUT` incluyen `Version`. Cuando la pantalla no conoce la versión, el
adaptador hace un `GET` previo y usa la versión actual; si la API responde `409`,
el mensaje se muestra al usuario. No se reintenta automáticamente una escritura.

### 3.5 Colecciones nulas

La API puede omitir o devolver `null` en colecciones como `Potreros`,
`Fotografias` o `Recursos`. Los DTOs espejo las normalizan a lista vacía en el
constructor, de modo que las páginas no necesitan comprobar `null`.

### 3.6 Fotografías

- El alta usa un request `multipart/form-data` por archivo, con el campo
  `archivo` y los campos opcionales `fechaCaptura`, `latitud` y `longitud`.
- La descarga se hace con el bearer token y se convierte a `data:` URL, porque
  un `<img src>` directo no puede enviar el token.
- `EventoDto.Fotografias` solo incluye fotos visibles; una foto ocultada deja de
  aparecer al recargar el evento.

### 3.7 Establecimientos sin coordenadas

`EstablecimientoDto` no expone `Latitud`/`Longitud`. El frontend mantiene un
overlay versionado en `ObligatorioFrontEnd/Data/establecimientos-geo.json` y
dibuja los establecimientos sin coordenada como **aproximados** a partir del
centro de sus eventos geolocalizados. Es una limitación conocida del contrato,
no un error del frontend.

### 3.8 Enums

Los enums espejo (`EstadoCampana`, `EstadoEvento`, `EstadoTarea`,
`TipoRecursoTarea`, `TipoEjecutorTarea`, `AccionAuditoria`, `RolApi`) coinciden
nombre a nombre con `ApiSiga.Contracts`. Un valor desconocido provoca un
`JsonException` controlado y se informa como error de formato.

---

## 4. DTOs espejo

Los contratos viven en `ObligatorioFrontEnd/Api/DTOs/` y son un espejo de
`ApiSiga.Contracts`. La revisión confirmó los campos y tipos de:

| DTO | Campos principales | Observación |
| --- | --- | --- |
| `EmpresaDto` | `Id`, `Nombre`, `RazonSocial`, `Activo`, `FechaCreacion`, `Version` | — |
| `EstablecimientoDto` | `EmpresaId`, `EmpresaNombre`, `Nombre`, `Ubicacion`, `Responsables`, `SuperficieHa`, `Activo` | Sin latitud/longitud |
| `PotreroDto` | `EstablecimientoId`, `EstablecimientoNombre`, `Nombre`, `SuperficieHa`, `Limites`, `Activo` | — |
| `CultivoDto` | `EmpresaId`, `Nombre`, `Activo` | — |
| `CampanaDto` | `EmpresaId`, `Nombre`, `CultivoId`, `CultivoNombre`, fechas, `Estado`, `Version`, `Potreros` | `Potreros` normalizado |
| `CampanaPotreroDto` | `PotreroId`, `PotreroNombre`, fechas, `Estado` | — |
| `UsuarioSesionDto` | `Id`, `Nombre`, `Email`, `Rol`, `EmpresaId`, `EmpresaNombre`, `EtiquetaAlcance`, `EstablecimientoIds` | `Rol` mapeado a `UserRole` |
| `UsuarioDto` | `Nombre`, `Email`, `Rol`, `EmpresaId`, `EstablecimientoIds`, `Activo` | — |
| `EventoDto` | `CampanaPotreroId`, `PotreroNombre`, `TipoEventoId/Nombre`, `Fecha`, `Descripcion`, `Ubicacion`, `Latitud`, `Longitud`, `RegistradoPor`, `Estado`, `Fotografias` | `Fotografias` normalizado |
| `FotografiaDto` | `EventoId`, `FechaCaptura`, `Latitud`, `Longitud`, `Archivo`, `Visible`, `MotivoOcultamiento` | `NombreArchivo` calculado |
| `TareaDto` | `CampanaPotreroId`, `PotreroNombre`, `EventoId`, `TipoTareaId/Nombre`, `Descripcion`, fechas, `Resultado`, `SolucionoProblema`, `EjecutorId/Nombre/Tipo`, `Estado`, `Costo`, `Recursos` | `Recursos` normalizado |
| `TareaRecursoDto` | `Tipo`, `RecursoId`, `Nombre`, `Cantidad`, `CostoUnitario`, `CostoTotal` | — |
| `TipoEventoDto` / `TipoTareaDto` | `EmpresaId`, `Nombre`, `Activo` | — |
| `InsumoDto` | `Nombre`, `Unidad`, `PrecioUnitario`, `Activo` | — |
| `MaquinariaDto` | `Nombre`, `Identificacion`, `CostoHora`, `Activo` | — |
| `ContratistaDto` | `Nombre`, `UsuarioId`, `UsuarioNombre`, `Activo` | — |
| `AuditoriaDto` | `EmpresaId`, `Actor`, `FechaUtc`, `Entidad`, `EntidadId`, `Accion`, `DatosAntes`, `DatosDespues` | Detalle JSON indentado |
| `AuditoriaPaginadaDto` | `Items`, `Pagina`, `TamanoPagina`, `Total` | `Items` normalizado |

---

## 5. Verificación realizada

- **Rutas y verbos:** coinciden con los controllers de `ApiSiga.Api`.
- **Parámetros de query:** nombres y tipos coinciden (`incluirAnuladas`,
  `incluirInactivos`, `incluirInactivas`, `campanaId`, `potreroId`, etc.).
- **Cuerpos de request:** los DTOs de alta/edición coinciden campo a campo.
- **Respuestas:** se deserializan con camelCase y enums como texto.
- **Pruebas automatizadas del frontend:** `dotnet test` → **207 superadas, 0
  fallidas** (permisos, reportes, cliente HTTP, DTOs, sesión y empresa activa).
- **Prueba de API del bug de campañas:**
  `ActualizarCampana_ReemplazaPotrerosYReutilizaBajaLogica` → **1 superada**.
- **Recorrido E2E manual con 5 roles:** sin respuestas `401`/`403`/`500`
  inesperadas, sin errores de JavaScript, mapa y mini-mapa operativos.

---

## 6. Mantenimiento

Si `ApiSiga.Contracts` cambia:

1. Actualizar el DTO correspondiente en `ObligatorioFrontEnd/Api/DTOs/`.
2. Ajustar el adaptador en `ObligatorioFrontEnd/Services/Api/`.
3. Ejecutar `dotnet build ObligatorioFrontEnd.slnx` y `dotnet test`.
4. Repetir el checklist de `PRUEBAS_MANUALES.md` para el módulo afectado.
