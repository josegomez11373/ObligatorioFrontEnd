# SIGA — Frontend Blazor

Frontend del Sistema Integrado de Gestión Agrícola (SIGA), construido con **Blazor Server** sobre **.NET 10**. Puede funcionar contra **ApiSiga por HTTP** o con los **servicios en memoria (modo mock)**, sin compartir proyecto, submódulo ni código copiado con la API.

La referencia funcional es [`SIGA_Analisis_y_Propuesta_v2.docx`](ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx). El plan de trabajo histórico está en [`PLAN_FRONTEND_SIGA.md`](ObligatorioFrontEnd/Docs/PLAN_FRONTEND_SIGA.md) y el detalle de la integración en [`INTEGRACION_API_M2.md`](ObligatorioFrontEnd/Docs/INTEGRACION_API_M2.md).

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Navegador moderno

## Ejecutar

```powershell
dotnet run --project ObligatorioFrontEnd
```

Luego abrir la URL que muestra la consola (por defecto `http://localhost:5021`).

### Modos de ejecución

El backend se elige con la clave `ApiSiga:UseApi`:

| Archivo | `UseApi` | Backend |
| --- | --- | --- |
| `appsettings.Development.json` | `true` | ApiSiga en `http://localhost:5100/` |
| `appsettings.json` | `false` | Servicios en memoria (mock) |

Se puede forzar por variable de entorno, que tiene prioridad sobre los JSON:

```powershell
# Modo mock
$env:ApiSiga__UseApi = 'false'
dotnet run --project ObligatorioFrontEnd

# Modo API (la API debe estar escuchando en BaseUrl)
$env:ApiSiga__UseApi = 'true'
$env:ApiSiga__BaseUrl = 'http://localhost:5100/'
dotnet run --project ObligatorioFrontEnd
```

El modo mock no necesita la API ni base de datos. El modo API usa el login real
de ApiSiga y el JWT se guarda **solo en el servidor Blazor**; el navegador recibe
un identificador opaco.

Compilar sin ejecutar:

```powershell
dotnet build ObligatorioFrontEnd.slnx -nologo
```

## Usuarios demo

Todos usan la contraseña **`siga2026`**.

| Email | Rol | Alcance |
| --- | --- | --- |
| `admin@siga.demo` | Administrador General | Toda la plataforma |
| `empresa@siga.demo` | Administrador de Empresa | Agro del Sur |
| `ingeniero@siga.demo` | Ingeniero Agrónomo | Agro del Sur |
| `operario@siga.demo` | Operario de Campo | Agro del Sur · Establecimiento La Esperanza |
| `empresa2@siga.demo` | Administrador de Empresa | Campos Verdes |
| `contratista@siga.demo` | Contratista | Agro del Sur · solo tareas asignadas |

## Módulos

### Conectados a ApiSiga (`ApiSiga:UseApi=true`)

- **Empresas** con alta, edición y baja lógica.
- **Establecimientos** y **potreros**, con aislamiento por empresa y la empresa derivada del establecimiento.
- **Cultivos** por empresa.
- **Campañas** con potreros, estados (planificada, activa, cerrada, anulada) y regla RN-05 (sin campañas activas solapadas en un mismo potrero).
- **Usuarios** con roles, empresa, establecimientos asignados y cambio de contraseña.

### En modo demostración (mock)

Estos módulos siguen usando los servicios en memoria porque la API todavía no expone sus endpoints:

- **Eventos** con ubicación, GPS, fotografías, anulación con motivo y registro masivo de un evento por potrero.
- **Tareas** con ciclo de vida, asignación a usuarios o contratistas, insumos, maquinaria, costos, resultados y verificación.
- **Historial** cronológico por campaña.
- **Catálogos secundarios**: tipos de evento, tipos de tarea, insumos, maquinaria y contratistas.
- **Reportes**, **auditoría** de cambios e **Inicio** con métricas y actividad reciente.

Con `UseApi=true` esos módulos siguen siendo una demostración y no comparten datos con la API; sus identificadores no existen en la base real. La migración de cada uno queda para una etapa posterior.

La matriz de permisos de la sección 4.1 del documento v2 está centralizada en `Services/Permissions.cs`. La interfaz oculta opciones, pero cada servicio vuelve a validar permisos y alcance antes de operar; en modo API la propia ApiSiga repite esas validaciones.

## Sesión y empresa activa

- **Modo API:** el login se valida contra `POST /api/v1/auth/login`. El JWT se guarda en `TokenStore`, en el servidor Blazor del circuito, y **nunca** se envía al navegador.
- **Modo mock:** la autenticación es simulada con los usuarios demo y la misma contraseña.
- La sesión sobrevive a un **F5**. El navegador solo guarda un identificador opaco en `ProtectedSessionStorage`; los datos de la sesión viven en un `AlmacenSesiones` singleton del servidor.
- La **empresa activa** del administrador general también se guarda y se restaura tras F5. Los demás roles usan la empresa fija de su usuario.
- Al crear o desactivar una empresa, el selector de empresa activa se refresca automáticamente.
- Logout, expiración del token o reinicio del servidor invalidan la sesión y vuelven a `/login`.
- La sesión no se comparte entre pestañas ni entre circuitos: cada pestaña restaura su propio circuito a partir del identificador guardado.

## Decisiones técnicas

- **Fotos fuera de `wwwroot`**: se guardan en `ObligatorioFrontEnd/App_Data/uploads` y se sirven por `GET /fotos/{id}?token=...`. El token de capacidad se entrega solo a usuarios con acceso a la empresa. Los metadatos viven en un `FotografiaStore` singleton porque el endpoint HTTP corre fuera del circuito de Blazor.
- **Formatos de foto**: JPG, PNG y HEIC/HEIF, con validación por cabecera mágica y máximo de 10 MB. Las fotos no se eliminan: solo se ocultan con un motivo.
- **Hora**: todo se guarda en UTC y se muestra en `America/Montevideo` mediante `Services/SigaTime.cs`.
- **Historia completa**: las entidades se desactivan o anulan; no se borran.
- **Costos**: el costo de una tarea es insumos + maquinaria + contratista; el de una campaña es la suma de sus tareas no anuladas.

## Estructura

```
ObligatorioFrontEnd/
├─ Api/                 Cliente HTTP, DTOs locales, excepciones y mapeos
├─ Components/          Componentes y páginas Razor
├─ Models/              Modelos de UI; Legacy/ conserva las entidades del mock
├─ Services/            Dominio, permisos, auditoría y almacenamiento
│  ├─ Api/              Adaptadores HTTP por módulo
│  ├─ Contratos/        Interfaces asíncronas de servicios
│  ├─ ContextoEmpresa/  Empresa activa por circuito
│  └─ Sesion/           Token, sesión y persistencia
├─ Docs/                Documento funcional, plan, integración y reporte de API
├─ wwwroot/             Estilos y activos estáticos
└─ Program.cs           DI, cultura es-UY y endpoint de fotos
```

## Limitaciones de esta etapa

- **Campañas en modo API:** el alta con potreros queda bloqueada por un defecto de ApiSiga (`PK_Potreros`); listar, editar y cerrar campañas existentes funciona. El diagnóstico y el parche propuesto están en [`BUG_API_CAMPANAS.md`](ObligatorioFrontEnd/Docs/BUG_API_CAMPANAS.md).
- En modo API, editar una campaña **no permite cambiar sus potreros**: `ActualizarCampanaDto` no incluye `PotreroIds`.
- Una campaña nueva siempre se crea como **Planificada**; el estado se cambia después con Editar.
- `FechaCreacion` puede mostrarse como `—` porque ApiSiga todavía devuelve `0001-01-01` en algunas entidades (defecto del `AuditoriaInterceptor`).
- Los datos de los módulos mock viven en memoria por circuito: se reinician al recargar la página y al detener la aplicación.
- Las imágenes de los módulos mock permanecen en disco, pero sus metadatos se reinician; pueden quedar archivos huérfanos en `App_Data/uploads`.
- La visualización de HEIC depende del soporte del navegador.
