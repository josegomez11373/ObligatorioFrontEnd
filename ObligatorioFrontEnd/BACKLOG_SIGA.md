# Backlog inicial de SIGA

Este backlog ordena el trabajo para construir la primera versión de SIGA. Los ítems con prioridad **P0** forman el flujo mínimo demostrable. Los ítems **P1** completan el MVP. Los **P2** quedan para después del MVP.

Las decisiones marcadas como **Pendiente de cliente** no bloquean el inicio del proyecto, pero deben resolverse antes de cerrar su implementación.

## Objetivo del primer flujo demostrable

Un usuario autorizado inicia sesión, selecciona una campaña, registra un evento con una fotografía y luego consulta ese evento en el historial de la campaña.

## P0 - Fundaciones y flujo central

| ID | Historia de usuario | Criterio de terminado | Dependencias |
| --- | --- | --- | --- |
| BL-01 | Como usuario, quiero iniciar sesión para acceder solamente a las funciones autorizadas. | Credenciales válidas permiten entrar; las inválidas son rechazadas; la sesión identifica usuario y rol. | Definir mecanismo de autenticación. |
| BL-02 | Como administrador general, quiero administrar empresas para organizar la plataforma. | Permite crear, consultar, editar y desactivar empresas sin borrar su historial. | BL-01. |
| BL-03 | Como administrador de empresa, quiero administrar establecimientos para registrar los campos de mi empresa. | Permite crear, editar, consultar y desactivar establecimientos con nombre, ubicación, superficie y responsables. No permite operar fuera de la empresa autorizada. | BL-01, BL-02. |
| BL-04 | Como administrador de empresa, quiero administrar potreros para conservar su superficie, límites e historial. | Permite crear y consultar potreros de un establecimiento; un potrero sin campaña activa sigue visible; desactivarlo no borra su historial. | BL-03. **Pendiente de cliente:** formato de límites. |
| BL-05 | Como administrador o ingeniero, quiero administrar cultivos y campañas para registrar el trabajo agrícola de cada potrero. | Una campaña queda asociada a potrero y cultivo, con inicio, fin y estado; las campañas anteriores se pueden consultar. | BL-04. **Pendiente de cliente:** solapamientos y estados. |
| BL-06 | Como operario o ingeniero, quiero registrar un evento para informar algo observado en una campaña. | El evento guarda tipo, fecha, descripción, ubicación disponible, campaña y usuario registrador. | BL-01, BL-05. **Pendiente de cliente:** catálogo de tipos y obligatoriedad de GPS. |
| BL-07 | Como operario o ingeniero, quiero adjuntar fotografías a un evento para conservar evidencia. | Permite una o varias fotos por evento y registra autor, fecha de captura, ubicación disponible y referencia segura al archivo. | BL-06. **Pendiente de cliente:** tamaño, formatos y retención. |
| BL-08 | Como usuario autorizado, quiero consultar el historial de una campaña para conocer lo ocurrido. | Al elegir potrero y campaña, se ven los eventos y sus fotografías en orden cronológico. | BL-06, BL-07. |
| BL-09 | Como sistema, quiero auditar cambios relevantes para mantener trazabilidad. | Crear, editar, desactivar o anular entidades históricas registra usuario, fecha/hora, entidad, identificador, acción y valores relevantes. | BL-02 a BL-08. **Pendiente de cliente:** acciones consideradas relevantes. |

## P1 - Completar el MVP

| ID | Historia de usuario | Criterio de terminado | Dependencias |
| --- | --- | --- | --- |
| BL-10 | Como ingeniero, quiero crear tareas vinculadas a eventos para indicar una acción de respuesta. | Una tarea registra tipo, responsable, estado y fechas; queda claramente diferenciada de un evento. | BL-06. **Pendiente de cliente:** si puede existir sin evento o responder a varios. |
| BL-11 | Como responsable de una tarea, quiero registrar su ejecución para dejar constancia de lo realizado. | Permite indicar ejecutor, fecha de realización, resultado y estado de la tarea. | BL-10. **Pendiente de cliente:** estados y confirmación de ejecución. |
| BL-12 | Como responsable de una tarea, quiero registrar insumos y maquinaria para conocer los recursos consumidos. | Una tarea puede tener varios insumos con cantidades y costos, y varias máquinas con horas y costos. | BL-11. **Pendiente de cliente:** unidades, precios históricos y fórmula de costos. |
| BL-13 | Como ingeniero, quiero registrar el resultado de una tarea para evaluar si resolvió el problema. | Permite indicar resultado, si solucionó el problema y fecha de comprobación; muestra tiempo transcurrido. | BL-10. **Pendiente de cliente:** criterio de evaluación. |
| BL-14 | Como administrador o ingeniero, quiero asignar tareas a contratistas para gestionar trabajos externos. | Permite asignar un contratista y registrar su ejecución; su acceso queda limitado a lo asignado. | BL-10. **Pendiente de cliente:** cuenta propia y permisos del contratista. |
| BL-15 | Como administrador o ingeniero, quiero consultar costos y recursos de una campaña para analizarla. | Muestra tareas, insumos, maquinaria y costo de campaña según la regla acordada. | BL-12. **Pendiente de cliente:** cálculo final del costo. |
| BL-16 | Como operario, quiero registrar eventos y fotos sin conexión para trabajar en el campo. | Los registros se guardan localmente y se muestran como pendientes sin perderse al cerrar la aplicación. | BL-06, BL-07. |
| BL-17 | Como operario, quiero sincronizar mis registros pendientes al recuperar Internet para enviarlos al sistema. | La sincronización informa estado pendiente, sincronizando, sincronizado o error; un reintento no duplica registros; la app conserva los fallidos. | BL-16. **Pendiente de cliente:** conflictos de edición. |

## P2 - Posterior al MVP

| ID | Funcionalidad | Motivo para postergarla |
| --- | --- | --- |
| BL-18 | Mapas avanzados y polígonos geográficos. | Depende de definir precisión, fuente de mapas y modelo geográfico. |
| BL-19 | Reportes avanzados de tratamientos y zonas problemáticas. | Requiere historial suficiente y métricas acordadas. |
| BL-20 | Exportación de informes a PDF o Excel. | Debe definirse formato, destinatarios y permisos. |
| BL-21 | Integraciones externas, sensores, drones, satélites e IA. | Están explícitamente fuera del alcance inicial. |

## Orden de implementación recomendado

1. Preparar el proyecto, modelo de dominio y autenticación: BL-01.
2. Construir la estructura agrícola: BL-02 a BL-05.
3. Completar el primer flujo demostrable: BL-06 a BL-08.
4. Añadir trazabilidad: BL-09.
5. Incorporar tareas, recursos, resultados y costos: BL-10 a BL-15.
6. Construir el flujo móvil online y luego almacenamiento local: BL-16.
7. Implementar sincronización idempotente y manejo de errores: BL-17.

## Decisiones a solicitar antes de cada bloque

- Antes de BL-04 y BL-05: límites geográficos, campaña por potrero y solapamientos.
- Antes de BL-10 a BL-15: ciclo de vida de tareas, relación con eventos, contratistas, unidades y costos.
- Antes de BL-16 y BL-17: datos disponibles offline, caducidad de sesión, límites de fotos y política de conflictos.
