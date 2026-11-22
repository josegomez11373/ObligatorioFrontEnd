# Checklist de pruebas manuales — SIGA Frontend

Procedimiento de aceptación funcional del frontend Blazor antes de una entrega o
demostración. Se ejecuta contra **ApiSiga real**; el frontend no modifica ni
simula endpoints.

- **Frontend:** `http://localhost:5021`
- **API:** `http://localhost:5100` (configurada en `ApiSiga:BaseUrl`)
- **Referencia funcional:** `ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx`
- **Contrato revisado:** `ObligatorioFrontEnd/Docs/CONTRATO_API.md`

Marcar cada casilla con `[x]` cuando el resultado coincida con lo esperado. Si
algo falla, registrar el defecto en la sección 7 antes de continuar.

---

## 1. Preparación del entorno

### 1.1 Levantar ApiSiga

Desde la raíz del repositorio de la API:

```powershell
dotnet run --project src/ApiSiga.Api
```

- [ ] La API responde en `http://localhost:5100`.
- [ ] `GET http://localhost:5100/swagger` abre la documentación.
- [ ] La base tiene los datos de la semilla. Si la base está vacía, la API
      siembra empresas, usuarios, establecimientos, potreros y catálogos al
      iniciar. **La semilla no se vuelve a ejecutar** si ya existe al menos una
      empresa.

### 1.2 Levantar el frontend

Desde la raíz de este repositorio:

```powershell
dotnet run --project ObligatorioFrontEnd
```

- [ ] La consola muestra `http://localhost:5021`.
- [ ] La página de login abre sin errores en la consola del navegador.
- [ ] `appsettings.Development.json` apunta a `http://localhost:5100/`.

### 1.3 Usuarios de prueba

Todos usan la contraseña **`siga2026`**. Son datos de demostración de la
semilla de ApiSiga y no se muestran en la pantalla de login.

| Email | Rol | Alcance |
| --- | --- | --- |
| `admin@siga.demo` | Administrador general | Acceso global |
| `empresa@siga.demo` | Administrador de empresa | Agro del Sur |
| `empresa2@siga.demo` | Administrador de empresa | Campo Norte |
| `ingeniero@siga.demo` | Ingeniero agrónomo | Agro del Sur |
| `operario@siga.demo` | Operario de campo | Agro del Sur · La Esperanza |
| `contratista@siga.demo` | Contratista | Agro del Sur · tareas asignadas |

### 1.4 Datos de prueba

- [ ] Usar nombres identificables con el prefijo `PRUEBA-<fecha>` para no
      confundir los registros creados con los datos de la semilla.
- [ ] Anotar los IDs o nombres creados para poder verificarlos en Reportes y
      Auditoría.
- [ ] No eliminar datos de la semilla durante la prueba.

---

## 2. Matriz de permisos esperada

`V` = puede ver el módulo. `G` = puede crear, editar, desactivar o ejecutar la
acción principal. `—` = el módulo no aparece en el menú y una URL directa
redirige al inicio.

| Módulo | Admin general | Admin empresa | Ingeniero | Operario | Contratista |
| --- | --- | --- | --- | --- | --- |
| Empresas | V + G | V | — | — | — |
| Establecimientos | V + G | V + G | V | V | — |
| Potreros | V + G | V + G | V | V | — |
| Campañas | V + G | V + G | V + G | V | — |
| Eventos | V | V | V + G | V + G | — |
| Tareas | V | V + asignar/verificar/anular | V + G | V + ejecutar | V + ejecutar (solo asignadas) |
| Historial | V | V | V | V | V |
| Catálogos | V + G | V + G | V | — | — |
| Cultivos | V + G | V + G | V + G | — | — |
| Reportes | V | V | V | — | — |
| Usuarios | V + G | V + G | — | — | — |
| Auditoría | V | V | — | — | — |
| Insumos | V + G | V + G | V | — | — |
| Maquinaria | V + G | V + G | V | — | — |
| Contratistas | V + G | V + G | — | — | — |
| Mapa | V + G | V + G | V | V | — |

Notas de la matriz:

- El **administrador general** y el **administrador de empresa** no crean ni
  editan eventos: solo los consultan. Es una decisión de la matriz del
  documento v2, no un defecto.
- El **ingeniero** administra campañas, eventos y tareas, pero no ejecuta
  tareas ni registra resultados de campo.
- El **operario** y el **contratista** ejecutan tareas, pero no las crean ni las
  asignan.
- El **contratista** no ve estructura agrícola, catálogos, reportes ni auditoría.

---

## 3. Sesión, navegación y estados globales

Ejecutar con todos los usuarios.

### 3.1 Login y sesión

- [ ] Un usuario válido inicia sesión y llega al resumen.
- [ ] Un email inexistente o una contraseña incorrecta muestra un mensaje
      comprensible y no crea sesión.
- [ ] El formulario de login abre vacío: no precarga email ni muestra la
      contraseña de demostración.
- [ ] Al presionar **F5** en cualquier pantalla, la sesión se restaura y se
      mantiene la pantalla actual.
- [ ] Al cerrar sesión se vuelve a `/login` y el botón Atrás no reabre datos
      privados.
- [ ] Si el token expira o la API responde `401` con un token enviado, se
      informa la expiración y se vuelve al login.
- [ ] Abrir una segunda pestaña no comparte la sesión de la primera; cada
      pestaña restaura su propio circuito.

### 3.2 Navegación y permisos

- [ ] El menú lateral muestra solo los módulos permitidos para el rol.
- [ ] Escribir a mano una URL no autorizada (por ejemplo `/empresas` como
      contratista) redirige al inicio.
- [ ] El resumen muestra el nombre, rol y alcance del usuario.
- [ ] Los accesos rápidos del resumen respetan la matriz de permisos.

### 3.3 Estados vacíos y de error

- [ ] Con la API detenida, cada pantalla muestra un mensaje de error legible y
      el resto de la navegación sigue funcionando.
- [ ] Al reiniciar la API y recargar, los listados vuelven a cargar.
- [ ] Una lista sin datos muestra un texto del tipo «No hay … registrados para
      la empresa activa» en lugar de una tabla vacía.
- [ ] Una búsqueda o filtro sin coincidencias muestra «No hay … que coincidan
      con los filtros».
- [ ] Los errores de validación de la API se muestran junto al formulario y no
      como una página de excepción.

### 3.4 Paneles emergentes (SidePanel)

Repetir en al menos un alta de cada módulo.

- [ ] El panel se abre desde **Nuevo …** o **Editar**.
- [ ] La **X**, el botón **Cancelar** y la tecla **Escape** cierran el panel.
- [ ] Un clic en el fondo oscuro cierra el panel.
- [ ] **Presionar dentro del panel, arrastrar el mouse fuera y soltar NO cierra
      el formulario.** Es el caso crítico del componente `SidePanel`.
- [ ] Si se cierra el panel con datos cargados, al reabrirlo no quedan valores
      viejos.

---

## 4. Checklist por rol

### 4.1 Administrador general — `admin@siga.demo`

**Alcance y empresa activa**

- [ ] El selector **Empresa activa** aparece en el pie del menú lateral.
- [ ] Cambiar a **Campo Norte** actualiza establecimientos, potreros,
      campañas, eventos, tareas, catálogos, reportes y mapa.
- [ ] Volver a **Agro del Sur** restaura sus datos.
- [ ] Crear una empresa nueva y comprobar que aparece en el selector sin
      recargar la página.
- [ ] Desactivar una empresa y comprobar que deja de ofrecerse como empresa
      activa.

**Empresas**

- [ ] **Nueva empresa** abre el panel y guarda con un nombre válido.
- [ ] Editar el nombre actualiza el listado y el selector.
- [ ] Desactivar deja la fila en estado «Desactivada» y conserva el historial.
- [ ] Un nombre vacío o demasiado largo muestra validación.

**Establecimientos y potreros**

- [ ] Crear un establecimiento con nombre, superficie, ubicación y
      responsables; la empresa aparece fija en la empresa activa.
- [ ] Editar el establecimiento y comprobar que conserva potreros y campañas.
- [ ] Desactivarlo y verificar que sigue visible en listados históricos cuando
      corresponde.
- [ ] Crear un potrero activo con establecimiento, superficie y límites.
- [ ] Editar y desactivar un potrero; el historial se conserva.

**Campañas**

- [ ] Crear una campaña con nombre, cultivo, período y uno o más potreros.
- [ ] La campaña nueva queda en estado **Planificada**.
- [ ] Intentar crear una campaña superpuesta en el mismo potrero y comprobar
      que la API la rechaza con un mensaje claro.
- [ ] Editar fechas, cultivo, estado o potreros.
- [ ] Pasar una campaña a **Activa** y luego **Cerrar** con confirmación.
- [ ] La campaña cerrada sigue visible y conserva eventos y tareas.

**Eventos (solo consulta para este rol)**

- [ ] La lista de eventos carga con filtros de texto, tipo y estado.
- [ ] **Ver** abre el detalle con ubicación, coordenadas y fotografías.
- [ ] **No** aparece el botón **Nuevo evento** ni **Editar**/**Anular**.
- [ ] Abrir una fotografía autenticada desde el detalle.

**Tareas (solo consulta para este rol)**

- [ ] La lista carga con búsqueda y filtro por estado.
- [ ] **Detalle** muestra recursos, costos, resultado y verificación.
- [ ] **No** aparece **Nueva tarea** ni acciones de asignar/realizar/verificar/anular.

**Historial**

- [ ] Elegir una campaña y comprobar que eventos y tareas aparecen en orden
      cronológico.
- [ ] Alternar el filtro **Todo / Solo eventos / Solo tareas**.
- [ ] Una campaña sin registros muestra «Todavía no hay registros…».

**Catálogos, cultivos y recursos**

- [ ] Crear, editar y desactivar un cultivo.
- [ ] Crear, editar y desactivar tipos de evento y tipos de tarea.
- [ ] Crear, editar y desactivar insumos con unidad y precio.
- [ ] Crear, editar y desactivar maquinaria con identificación y costo por hora.
- [ ] Crear, editar y desactivar contratistas vinculados a un usuario.
- [ ] Un insumo o maquinaria desactivado no se ofrece al agregar recursos a una
      tarea.

**Usuarios**

- [ ] Crear un usuario con nombre, correo, contraseña, rol, empresa y
      establecimientos.
- [ ] Editar un usuario y cambiar su contraseña.
- [ ] Desactivar un usuario y comprobar que no puede iniciar sesión.
- [ ] El correo no se puede modificar en edición.

**Reportes**

- [ ] Recorrer las 12 pestañas: Potrero, Problemas, Autores, Respuestas,
      Recursos, Costos, Tratamientos, Zonas, Campañas, Eventos por tipo, Tareas
      por tipo y Actividad.
- [ ] Abrir **Crear reporte** y aplicar filtros de empresa, establecimiento,
      potrero, campaña, período y estados.
- [ ] **Limpiar filtros** restaura el tablero completo.
- [ ] Los totales de campañas, eventos, tareas y costos coinciden con los
      registros creados en la prueba.
- [ ] Una combinación sin datos muestra el estado vacío del reporte.

**Auditoría**

- [ ] Filtrar por entidad, actor, empresa y rango de fechas.
- [ ] La tabla pagina y los botones Anterior/Siguiente se deshabilitan en los
      extremos.
- [ ] Al cambiar de página la vista vuelve al encabezado de la tabla.
- [ ] **Ver cambios** abre el detalle con JSON indentado y solo campos de
      negocio; no muestra contraseñas, tokens ni hashes.
- [ ] Las fechas del detalle se muestran en hora de Uruguay (no ISO con `Z`).
- [ ] Cerrar el detalle con **X**, **Cerrar** y clic en el fondo.
- [ ] Las fechas se muestran en hora de Uruguay.

**Mapa**

- [ ] La página `/mapa` dibuja los establecimientos de la empresa activa.
- [ ] Los establecimientos sin coordenada propia se muestran como
      **aproximados** y los geolocalizados como **exactos**.
- [ ] **Ubicar** abre el mini-mapa; se puede hacer clic, arrastrar el pin,
      buscar una dirección y usar la ubicación del navegador.
- [ ] **Guardar ubicación** persiste el pin y **Quitar** lo elimina.
- [ ] Los eventos geolocalizados aparecen y se pueden filtrar por tipo y fechas.
- [ ] Si los *tiles* de OpenStreetMap fallan, se muestra un aviso y la lista de
      establecimientos sigue visible.

### 4.2 Administrador de empresa — `empresa@siga.demo` y `empresa2@siga.demo`

**Aislamiento por empresa**

- [ ] No aparece el selector de empresa activa (o solo muestra la empresa
      propia): el alcance queda fijo al token.
- [ ] `empresa@siga.demo` solo ve datos de **Agro del Sur**.
- [ ] `empresa2@siga.demo` solo ve datos de **Campo Norte**.
- [ ] Ninguno puede abrir ni modificar registros de la otra empresa cambiando
      una URL con un ID ajeno.
- [ ] En **Empresas** el listado es de solo lectura: no aparecen **Nueva
      empresa**, **Editar** ni **Desactivar**.
- [ ] Crear un establecimiento, potrero o campaña y comprobar que la empresa
      del formulario está fija y no se puede cambiar.

**Gestión**

- [ ] Crear, editar y desactivar establecimientos y potreros.
- [ ] Crear y editar campañas; cerrar una campaña con confirmación.
- [ ] Consultar eventos y su detalle, sin botones de alta o edición.
- [ ] Asignar una tarea a un usuario o contratista.
- [ ] Registrar resultado, verificar y anular tareas.
- [ ] Crear y editar usuarios de su empresa, incluyendo establecimientos
      permitidos; no puede crear un administrador general.
- [ ] Gestionar cultivos, tipos, insumos, maquinaria y contratistas.
- [ ] Consultar Reportes y Auditoría limitados a su empresa.
- [ ] En el mapa, ubicar y mover establecimientos de su empresa.

### 4.3 Ingeniero agrónomo — `ingeniero@siga.demo`

- [ ] Ve Establecimientos, Potreros, Campañas, Eventos, Tareas, Historial,
      Cultivos, Catálogos, Reportes, Insumos, Maquinaria y Mapa.
- [ ] No ve Empresas, Usuarios ni Auditoría.
- [ ] En Catálogos, Tipos de evento, Tipos de tarea, Insumos y Maquinaria el
      listado es de solo lectura (sin botones de alta, edición o baja).
- [ ] En Cultivos sí puede crear, editar y desactivar.
- [ ] Crear una campaña con potreros y comprobar que queda **Planificada**.
- [ ] Editar una campaña y cambiar su estado a **Activa**.
- [ ] Crear un evento con tipo, fecha, descripción, ubicación y coordenadas.
- [ ] Crear un evento con varios potreros y comprobar que se genera uno por
      potrero.
- [ ] Adjuntar una o varias fotografías y abrirlas desde el detalle.
- [ ] Editar un evento registrado y anular otro con motivo; el anulado queda en
      el historial.
- [ ] Crear una tarea con o sin evento de origen.
- [ ] Asignar la tarea a un usuario o contratista.
- [ ] Registrar el resultado y verificar la tarea cuando esté realizada.
- [ ] Anular una tarea con motivo y comprobar que conserva trazabilidad.
- [ ] **No** aparecen las acciones de ejecución de campo (agregar recursos y
      marcar como realizada), reservadas a operario y contratista.
- [ ] En el mini-mapa del evento, probar clic, pin arrastrable, **Usar mi
      ubicación**, **Buscar dirección** y **Quitar ubicación**.
- [ ] En Reportes, aplicar filtros y comparar totales con los datos creados.

### 4.4 Operario de campo — `operario@siga.demo`

- [ ] Ve Establecimientos, Potreros, Campañas, Eventos, Tareas e Historial.
- [ ] No ve Empresas, Usuarios, Catálogos, Cultivos, Reportes, Auditoría,
      Insumos, Maquinaria ni Contratistas.
- [ ] Solo ve establecimientos, potreros y campañas de **La Esperanza**.
- [ ] No aparecen botones de alta, edición o baja en Establecimientos,
      Potreros ni Campañas.
- [ ] Crear un evento en una campaña y potrero de su alcance.
- [ ] Editar y anular sus eventos; no puede editar eventos de otro
      establecimiento.
- [ ] En Tareas, abrir el detalle de una tarea **Asignada** a su usuario.
- [ ] Agregar insumos o maquinaria y marcar la tarea como realizada con
      resultado, fecha y «¿Solucionó el problema?».
- [ ] **No** aparecen **Nueva tarea**, **Editar**, **Asignar**, **Verificar**
      ni **Anular**.
- [ ] Consultar el Historial de sus campañas.

### 4.5 Contratista — `contratista@siga.demo`

- [ ] El menú solo muestra **Resumen**, **Tareas** e **Historial**.
- [ ] Escribir `/empresas`, `/eventos` o `/catalogos` redirige al inicio.
- [ ] En Tareas solo aparecen las tareas asignadas a su usuario.
- [ ] Abrir el detalle de una tarea asignada.
- [ ] Agregar recursos y marcar la tarea como realizada.
- [ ] **No** puede crear, asignar, verificar ni anular tareas.
- [ ] En el Historial solo puede consultar el alcance de su empresa.
- [ ] La semilla puede no tener tareas asignadas: en ese caso la lista muestra
      el estado vacío y no un error.

---

## 5. Pruebas transversales

### 5.1 Responsive y usabilidad

- [ ] Con ancho de escritorio (≥ 1280 px) las tablas no desbordan la tarjeta.
- [ ] Con ancho de tablet (≈ 768 px) el menú y las tablas siguen siendo
      utilizables.
- [ ] Con ancho de móvil (≈ 390 px) el login, el resumen y al menos un listado
      se leen sin scroll horizontal de página.
- [ ] Las tablas anchas se desplazan horizontalmente **dentro** de la tarjeta en
      pantallas chicas, sin empujar el ancho de la página.
- [ ] En móvil el menú lateral se oculta y se abre con el botón ☰ de la barra
      superior; no ocupa la pantalla al entrar.
- [ ] El menú móvil se cierra con **✕**, con clic en el fondo, con **Escape** y al
      elegir un destino.
- [ ] Mientras el menú móvil está abierto, el fondo no se desplaza.
- [ ] En móvil siguen disponibles el selector de empresa activa y el cierre de
      sesión dentro del menú.
- [ ] Los botones de acción tienen texto claro y no dependen solo del color.
- [ ] Los mensajes de error son visibles con lector de pantalla (`role="alert"`).
- [ ] El foco del teclado recorre el menú, las tablas y los paneles.

### 5.2 Rendimiento percibido

- [ ] La primera carga del resumen no bloquea la navegación.
- [ ] Al cambiar de empresa activa, los listados se refrescan en un tiempo
      razonable y sin parpadeos.
- [ ] El mapa carga Leaflet solo al abrir `/mapa` o el mini-mapa de un evento.
- [ ] Los catálogos de un formulario se cargan al abrirlo, no al entrar a la
      página.

### 5.3 Regresión automatizada

Ejecutar antes de firmar:

```powershell
dotnet build ObligatorioFrontEnd.slnx -nologo
dotnet test tests/ObligatorioFrontEnd.Tests/ObligatorioFrontEnd.Tests.csproj
```

- [ ] La solución compila con 0 errores y 0 advertencias.
- [ ] Las **207** pruebas del frontend pasan.
- [ ] La prueba de API del bug de campañas pasa:
      `dotnet test tests/ApiSiga.Tests/ApiSiga.Tests.csproj --filter ActualizarCampana_ReemplazaPotrerosYReutilizaBajaLogica`
      (desde el repositorio de la API).

---

## 6. Criterios de aceptación

Se considera aprobada la entrega cuando:

- [ ] Todos los ítems de las secciones 3 y 4 están marcados o tienen un defecto
      registrado y aceptado.
- [ ] No hay respuestas `500`, errores de JavaScript ni pantallas de excepción
      en el recorrido por los cinco roles.
- [ ] El build y las pruebas automatizadas están en verde.
- [ ] No hay secretos, tokens ni contraseñas reales en archivos versionados.
- [ ] `appsettings.Production.json` no contiene una URL de producción y la
      variable `ApiSiga__BaseUrl` está documentada.

---

## 7. Registro de defectos

| # | Fecha | Rol | Pantalla | Pasos | Esperado | Obtenido | Severidad | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | | | | | | | | |
| 2 | | | | | | | | |
| 3 | | | | | | | | |

Severidad sugerida: **Bloqueante** (impide operar), **Alta** (dato incorrecto o
pérdida de trabajo), **Media** (usabilidad), **Baja** (cosmética).

---

## 8. Firma de la ejecución

| Rol | Usuario | Fecha | Responsable | Resultado | Observaciones |
| --- | --- | --- | --- | --- | --- |
| Administrador general | `admin@siga.demo` | | | Aprobado / Con defectos | |
| Administrador de empresa | `empresa@siga.demo` | | | Aprobado / Con defectos | |
| Administrador de empresa 2 | `empresa2@siga.demo` | | | Aprobado / Con defectos | |
| Ingeniero agrónomo | `ingeniero@siga.demo` | | | Aprobado / Con defectos | |
| Operario de campo | `operario@siga.demo` | | | Aprobado / Con defectos | |
| Contratista | `contratista@siga.demo` | | | Aprobado / Con defectos | |

**Build y pruebas:** `dotnet build` correcto · `dotnet test` 207/207 · Fecha: ____

**Firma final:** ______________________________
