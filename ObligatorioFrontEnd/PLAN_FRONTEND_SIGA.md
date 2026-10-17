# Plan de trabajo del FrontEnd de SIGA

> **Base de este plan:** la versión de `ObligatorioFrontEnd` que se subió al inicio de la conversación (Blazor Server, .NET 10, servicios en memoria). No incluye los cambios hechos después. Si algún punto ya está resuelto, se tacha y se sigue con el siguiente.
>
> **Referencia:** `SIGA_Analisis_y_Propuesta_v2.docx`. Los códigos HU-xx y RF-xx salen de ahí.

## Objetivo

Que la web cumpla lo que el documento dice que debe hacer, y que quede preparada para consumir la API sin reescribir las páginas.

Orden de las fases: **0 → 1 → 2 → 3** son correcciones y alineación (rápidas). **4 → 5** son pantallas nuevas y preparación para la API. **6** es la API.

---

## Fase 0. Bugs a corregir

- [x] **Selector de empresa / establecimiento sin valor real.** `InputSelect` sobre `Guid` muestra la primera opción pero el modelo queda en `Guid.Empty`, y `[Required]` no lo detecta.
  - Archivos: `EstablecimientoForm.razor`, `PotreroForm.razor`.
  - Hacer: agregar la opción «Seleccioná…» (`value="@Guid.Empty"`) y validar `!= Guid.Empty` (atributo propio o validación en el submit).
  - Terminado cuando: no se puede guardar un establecimiento ni un potrero sin elegir el padre.
- [x] **Editar una empresa pisa `CreatedAt`.** En `Empresas.razor`, `HandleFormSubmit` crea `new Empresa { ..., CreatedAt = DateTime.UtcNow }`.
  - Hacer: `existing with { Nombre = ..., Activa = ... }` y mover esa lógica al servicio (`EmpresaService.Update(id, nombre, activa)`).
- [x] **Formulario con datos viejos al volver a `/potreros`, `/empresas`, `/establecimientos`.** `OnParametersSet` solo carga el modelo si hay `editId`.
  - Hacer: en el `else`, reiniciar `editModel = new()`.
- [x] **Los desplegables ofrecen elementos desactivados.** Empresas inactivas en `EstablecimientoForm`; establecimientos inactivos en `PotreroForm`.
  - Hacer: filtrar por `Activa` / `Activo`, salvo que el registro editado ya apunte a uno inactivo.
- [x] **Estilos que no existen en `wwwroot/app.css`:** `.card`, `.content-grid`, `.table`, `.actions`, `.destructive`, `.form-actions`.
  - Hacer: definirlos con la paleta existente (`--forest`, `--leaf`, `--line`, `--cream`).
- [x] **Estilos que se pisan:**
  - `.form-field input` fuerza `width: 100%` y `min-height: 3rem` también en los checkboxes de «Activo». Excluir `input[type=checkbox]`.
  - `.primary-button` es de ancho completo y con margen superior; se reutiliza en «Nuevo …» y «Guardar». Crear una variante (`.primary-button--inline`) o un `.secondary-button`.
- [x] **Textos en inglés** en una app con `lang="es"`: `NotFound.razor`, `Error.razor`, `ReconnectModal.razor` y el `#blazor-error-ui` de `MainLayout.razor`.
- [x] **`Home.razor` desactualizado.** Dice que la estructura agrícola es «Siguiente» aunque empresas, establecimientos y potreros ya existen. Actualizar los estados.
- [x] **Etiquetas de desarrollo en la UI** (`BL-02 · Gestión de empresas`, etc.). Sacarlas antes de entregar o reemplazarlas por texto de producto.

---

## Fase 1. Aislamiento por empresa (RN-16, RNF-03, HU-04)

Es la regla de seguridad más importante y hoy no se cumple: un administrador de empresa ve y crea datos de cualquier empresa.

- [x] **IDs fijos para la semilla.** Crear `SeedIds` con GUID constantes para las dos empresas y los dos establecimientos. `EstablecimientoService` hoy asigna por posición en la lista; pasar a IDs explícitos.
- [x] **Usuario con alcance real.** Reemplazar `Scope` (texto) en `DemoUser` por:
  - `Guid? EmpresaId` (`null` = toda la plataforma),
  - `IReadOnlyList<Guid> EstablecimientoIds` (vacío = todos los de su empresa),
  - `ScopeLabel` (el texto que muestra la interfaz).
- [x] **Usuarios demo.**

  | Usuario | Rol | Empresa | Establecimientos |
  |---|---|---|---|
  | `admin@siga.demo` | Administrador general | — | todos |
  | `empresa@siga.demo` | Administrador de empresa | Agro del Sur | todos los suyos |
  | `ingeniero@siga.demo` | Ingeniero agrónomo | Agro del Sur | todos los suyos |
  | `operario@siga.demo` | Operario de campo | Agro del Sur | La Esperanza |
  | `empresa2@siga.demo` (**nuevo**) | Administrador de empresa | Campos Verdes | todos los suyos |
  | `contratista@siga.demo` (**nuevo**) | Contratista | Agro del Sur | — |

  El segundo administrador permite demostrar el aislamiento: dos usuarios, dos empresas, datos distintos.
- [x] **`IUserScope`** con `CanAccessEmpresa(id)` y `CanAccessEstablecimiento(id)`, implementado por `DemoAuthService` y registrado en `Program.cs`.
- [x] **Filtrar en los servicios, no en las páginas.**
  - `GetAll()` y `GetById()` devuelven solo lo que el alcance permite.
  - `Create` y `Update` rechazan un `EmpresaId` o `EstablecimientoId` fuera del alcance.
  - Los potreros se filtran por la empresa de su establecimiento y, para el operario, por su lista de establecimientos.
- [x] **Interfaz.** Para el administrador de empresa, la empresa aparece preseleccionada y bloqueada en el formulario. Solo el administrador general elige empresa.
- **Terminado:** entrando con `empresa@` y con `empresa2@` se ven listados distintos, y no hay forma de crear ni editar nada de la otra empresa, ni siquiera forzando un ID.

---

## Fase 2. Alinear los modelos con el documento v2

- [ ] **`UserRole`:** agregar `Contratista` (HU-14, RF-35) y su `DisplayName`.
- [ ] **`Establecimiento`:** agregar `Ubicacion` y `Responsables` (RF-08, HU-04). Reflejarlo en el formulario y el listado.
- [ ] **`Potrero`:** agregar `Limites` como texto o GeoJSON (supuesto 11, RF-11).
- [ ] **`Campana` → dos niveles** (RF-15, RF-16, supuesto 1):
  - `Campana`: `Id`, `EmpresaId`, `Nombre`, `CultivoId`, `FechaInicio`, `FechaFin`, `Estado`.
  - `CampanaPotrero` (nuevo): `Id`, `CampanaId`, `PotreroId`, `FechaInicio`, `FechaFin`, `Estado`.
  - Estados como `enum` (planificada, activa, cerrada, anulada), no `bool Activa`.
- [ ] **`Cultivo`:** agregar `Activo`.
- [ ] **Campos comunes** en las entidades editables: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `Version` (soporte de auditoría y conflictos, RNF-04, RN-18).
- [ ] **Modelos nuevos para las etapas siguientes:** `TipoEvento`, `Evento`, `Fotografia`, `TipoTarea`, `Tarea` (con `EventoId` opcional y estados), `Insumo`, `Maquinaria`, `Contratista`. Pueden crearse vacíos y completarse cuando se construya cada pantalla.

---

## Fase 3. Permisos según la matriz del documento (sección 4.1)

Hoy los permisos del código no coinciden con la matriz.

| Hallazgo actual | Corrección |
|---|---|
| El ingeniero puede crear y desactivar establecimientos y potreros. | Solo consulta. |
| El administrador general no ve establecimientos ni potreros. | Los consulta. |
| El administrador de empresa no ve eventos. | Los consulta. |
| No existe pantalla de usuarios. | Ver fase 4. |

- [x] Centralizar los permisos en un solo lugar (por ejemplo `Permissions.cs` con `CanManage(role, módulo)` y `CanView(role, módulo)`) en vez de listas de roles repetidas en cada `RoleGuard` y en el menú.
- [x] `RoleGuard` y `MainLayout` leen de ese lugar.
- [x] Los servicios también validan (no solo la interfaz).
- [x] Un usuario con rol de solo consulta ve los listados sin los botones de crear, editar y desactivar.

---

## Fase 4. Pantallas pendientes (en el orden del flujo)

Cada una con: listado con filtros, formulario con validación en español, desactivar en lugar de eliminar, y alcance por empresa.

| Orden | Pantalla | Historias | Notas |
|---|---|---|---|
| 1 | **Usuarios** | HU-03 | Alta, rol, alcance (empresa y establecimientos). El administrador de empresa solo ve su empresa. |
| 2 | **Cultivos** | HU-06 | Catálogo simple. |
| 3 | **Campañas** | HU-07 | Crear campaña, elegir uno o varios potreros de la empresa, fechas por potrero, impedir solapamientos (RN-05), cerrar un potrero sin cerrar los demás. |
| 4 | **Eventos** | HU-08 | Elegir campaña-potrero, tipo, fecha, descripción, ubicación opcional. Opción «crear un evento por potrero». |
| 5 | **Fotografías** | HU-09 | Subir fotos a un evento; ocultar (no borrar) con motivo. En la maqueta, guardar en memoria o en `wwwroot` temporal. |
| 6 | **Historial de campaña** | HU-10 | Línea de tiempo por potrero y campaña. Es el cierre del primer flujo demostrable. |
| 7 | **Catálogos** | HU-12 | Insumos, maquinaria, contratistas, tipos de tarea. |
| 8 | **Tareas** | HU-13 a HU-16 | Crear (con o sin evento), asignar, ejecutar con recursos y costo, verificar resultado. |
| 9 | **Reportes** | HU-17 a HU-19 | Los ocho de la letra. |
| 10 | **Auditoría** | HU-22 | Se habilita cuando existan registros de auditoría (fase 5). |

**Primer flujo demostrable (meta intermedia):** iniciar sesión → elegir una campaña → registrar un evento con foto → verlo en el historial. Cubre las pantallas 1 a 6.

---

## Fase 5. Preparar el código para la API

Es el cambio más barato ahora y el más caro después.

- [ ] **Interfaces asíncronas.** Definir `IEmpresaService`, `IEstablecimientoService`, `IPotreroService`, etc., con métodos que devuelvan `Task<>`. Hoy son clases concretas y síncronas, y las páginas llaman a `GetAll()` directamente.
- [ ] Convertir las páginas a `OnInitializedAsync` y manejar estados de carga y de error.
- [ ] Mantener las implementaciones en memoria como `InMemory…Service` mientras no exista la API, y cambiar el registro en `Program.cs` cuando llegue.
- [ ] **Sacar la lógica de negocio de las páginas.** Hoy la validación del nombre está duplicada entre el formulario y el submit. La web valida formato; las reglas viven en el servicio (y luego en la API).
- [ ] **Extraer lo repetido.** Empresas, Establecimientos y Potreros son casi la misma página. Antes de sumar más, extraer un componente de listado con acciones y un patrón de formulario común.
- [ ] **Auditoría en la maqueta.** Un `AuditService` que registre usuario, fecha/hora UTC, entidad, id, acción y valores antes y después, invocado desde los servicios (HU-11).
- [ ] **Autenticación.** Mientras no haya API, mantener `DemoAuthService` pero detrás de `IAuthService`. Quitar la contraseña fija del código cuando exista login real.

---

## Fase 6. API y estructura de la solución

El obligatorio exige API; hoy el repositorio solo tiene la web.

- [ ] Reorganizar la solución: `SIGA.Core`, `SIGA.Infrastructure`, `SIGA.Api`, `SIGA.Web`.
- [ ] `SIGA.Api` con login (JWT con `empresa_id`, rol y establecimientos como *claims*), Swagger y los endpoints de HU-01 a HU-07.
- [ ] EF Core + SQL Server con filtro global por empresa y desactivación lógica.
- [ ] Interceptor de auditoría.
- [ ] La web pasa a implementaciones con `HttpClient` de las interfaces de la fase 5.
- [ ] La empresa **nunca** se toma del cuerpo de la petición para un usuario de empresa: siempre del token.
- [ ] Prueba automática de aislamiento: un usuario de la empresa A no puede leer ni modificar datos de la empresa B.

---

## Higiene del repositorio

- [ ] Mover `BACKLOG_SIGA.md` a `docs/` en la raíz y renumerarlo con las HU-01 a HU-26 del documento v2.
- [ ] Agregar `README.md`: cómo ejecutar, usuarios demo, estado de cada módulo.
- [ ] Confirmar que `.gitignore` excluye `bin/`, `obj/` y `.vs/` (ya lo hace).
- [ ] Commits por fase (por ejemplo, `fase-0-bugs`, `fase-1-aislamiento`) para poder revertir.

---

## Orden de ejecución sugerido

1. **Fase 0** completa (bugs y estilos).
2. **Fase 1** (aislamiento) y **Fase 2** (modelos), porque todo lo demás depende de ellas.
3. **Fase 3** (permisos).
4. **Fase 5**, en lo que respecta a interfaces asíncronas, antes de construir pantallas nuevas.
5. **Fase 4**, pantallas 1 a 6 (primer flujo demostrable).
6. **Fase 4**, pantallas 7 a 10 y **Fase 6** (API).
