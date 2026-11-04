# SIGA — Frontend Blazor

Frontend del Sistema Integrado de Gestión Agrícola (SIGA), construido con **Blazor Server** sobre **.NET 10**. Esta etapa funciona con servicios en memoria y autenticación simulada; la API y la base de datos se incorporan después.

La referencia funcional es [`SIGA_Analisis_y_Propuesta_v2.docx`](ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx). El plan de trabajo histórico está en [`PLAN_FRONTEND_SIGA.md`](ObligatorioFrontEnd/Docs/PLAN_FRONTEND_SIGA.md).

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Navegador moderno

## Ejecutar

```powershell
dotnet run --project ObligatorioFrontEnd
```

Luego abrir la URL HTTPS que muestra la consola (por ejemplo `https://localhost:7xxx`).

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

## Módulos implementados

- **Empresas, establecimientos, potreros y campañas** con aislamiento por empresa y regla RN-05 (sin campañas activas solapadas en un mismo potrero).
- **Eventos** con ubicación, GPS, fotografías, anulación con motivo y registro masivo de un evento por potrero.
- **Tareas** con ciclo de vida, asignación a usuarios o contratistas, insumos, maquinaria, costos, resultados y verificación.
- **Historial** cronológico por campaña.
- **Catálogos**: cultivos, tipos de evento, tipos de tarea, insumos, maquinaria y contratistas.
- **Usuarios** con roles y alcance.
- **Reportes** operativos y de costos, y **auditoría** de cambios.
- **Inicio** con métricas y actividad reciente.

La matriz de permisos de la sección 4.1 del documento v2 está centralizada en `Services/Permissions.cs`. La interfaz oculta opciones, pero cada servicio vuelve a validar permisos y alcance antes de operar.

## Decisiones técnicas

- **Fotos fuera de `wwwroot`**: se guardan en `ObligatorioFrontEnd/App_Data/uploads` y se sirven por `GET /fotos/{id}?token=...`. El token de capacidad se entrega solo a usuarios con acceso a la empresa. Los metadatos viven en un `FotografiaStore` singleton porque el endpoint HTTP corre fuera del circuito de Blazor.
- **Formatos de foto**: JPG, PNG y HEIC/HEIF, con validación por cabecera mágica y máximo de 10 MB. Las fotos no se eliminan: solo se ocultan con un motivo.
- **Hora**: todo se guarda en UTC y se muestra en `America/Montevideo` mediante `Services/SigaTime.cs`.
- **Historia completa**: las entidades se desactivan o anulan; no se borran.
- **Costos**: el costo de una tarea es insumos + maquinaria + contratista; el de una campaña es la suma de sus tareas no anuladas.

## Estructura

```
ObligatorioFrontEnd/
├─ Components/        Componentes y páginas Razor
├─ Models/            Entidades y modelos de edición (records)
├─ Services/          Lógica de negocio, permisos, auditoría y almacenamiento
├─ Docs/              Documento funcional y plan
├─ wwwroot/           Estilos y activos estáticos
└─ Program.cs         DI, cultura es-UY y endpoint de fotos
```

## Limitaciones de esta etapa

- Los datos de negocio viven en memoria y se reinician al detener la aplicación.
- Las imágenes permanecen en disco, pero sus metadatos también se reinician; pueden quedar archivos huérfanos en `App_Data/uploads`.
- La autenticación es simulada; no hay identidad real ni API.
- La visualización de HEIC depende del soporte del navegador.
