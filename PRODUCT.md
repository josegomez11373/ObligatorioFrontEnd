# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Administrador general**: gestiona todas las empresas, usuarios y auditoría desde escritorio.
- **Administrador de empresa**: administra establecimientos, potreros, campañas, catálogos, usuarios y auditoría de su empresa, desde escritorio.
- **Ingeniero agrónomo**: planifica campañas, crea y asigna tareas, registra y verifica resultados; alterna oficina y campo.
- **Operario de campo**: ejecuta tareas y carga eventos con ubicación, GPS y fotografías desde el celular, dentro de su establecimiento asignado.
- **Contratista**: solo ve y ejecuta las tareas que tiene asignadas.

El uso es mixto: administración en escritorio y operación en campo desde el celular.

## Product Purpose

SIGA (Sistema Integrado de Gestión Agrícola) es el frontend de gestión agropecuaria para empresas con uno o más establecimientos. Permite planificar campañas, registrar eventos de campo y administrar tareas con insumos, maquinaria y costos, además de consultar reportes, historial y auditoría.

El éxito es que cada rol complete su trabajo diario dentro del sistema, sin planillas paralelas y sin ver datos de otra empresa.

## Positioning

Es un frontend que consume el contrato existente de ApiSiga sin modificarlo. El valor está en aplicar la matriz de permisos por rol y el aislamiento multiempresa en la interfaz y en cada servicio, sobre una API que ya existe.

## Operating Context

- El trabajo real ocurre en establecimientos rurales, con conectividad móvil variable; la carga de eventos y tareas desde el celular es parte del uso esperado.
- Los datos son de demostración, provistos por la semilla de ApiSiga; la pantalla de login no muestra ni precarga credenciales.
- La API se ejecuta aparte (por defecto `http://localhost:5100/`). El frontend se levanta con `dotnet run --project ObligatorioFrontEnd` en `http://localhost:5021`.
- Referencia funcional: `ObligatorioFrontEnd/Docs/SIGA_Analisis_y_Propuesta_v2.docx`. Validación manual: `PRUEBAS_MANUALES.md`.
- Es una entrega académica (obligatorio) con operación demo, no un despliegue en producción.

## Capabilities and Constraints

- Módulos: Empresas, Establecimientos, Potreros, Cultivos, Campañas, Eventos, Tareas, Historial, Catálogos (tipos de evento, tipos de tarea, insumos, maquinaria, contratistas), Reportes, Usuarios, Auditoría y Mapa.
- Permisos según la matriz de la sección 4.1 del documento v2: la interfaz oculta opciones y cada servicio revalida la operación.
- Multiempresa: cada usuario ve solo su empresa y alcance; el administrador general puede cambiar la empresa activa.
- ApiSiga no expone latitud/longitud de establecimientos: el frontend mantiene un overlay versionado en `ObligatorioFrontEnd/Data/establecimientos-geo.json` y permite ubicarlos desde `/mapa`.
- Los reportes se agregan en el cliente (`Services/Reportes/CalculoReportes.cs`); no existe un endpoint `/reportes`.
- Si la API no responde, las páginas muestran el error o la lista vacía y siguen siendo navegables.
- Restricción dura: **no se modifica ApiSiga**. El contrato revisado está en `ObligatorioFrontEnd/Docs/CONTRATO_API.md`.
- Idioma: español rioplatense; fechas y moneda de Uruguay.
- Limitación conocida y documentada: las coordenadas de establecimientos no vienen de la API.

## Brand Commitments

- Nombre: **SIGA — Sistema Integrado de Gestión Agrícola**.
- Voz: español rioplatense, directa y profesional, sin tecnicismos innecesarios.
- La identidad actual es tipográfica: marca "S" en verde lima junto al logotipo SIGA sobre una paleta verde. No hay archivo de logo ni manual de marca provistos.

## Evidence on Hand

- Documento funcional v2 y `PRUEBAS_MANUALES.md` con la matriz de permisos y los checklists por rol.
- `Docs/CONTRATO_API.md` y `Docs/BUG_API_CAMPANAS.md` con la revisión de endpoints y los hallazgos.
- Semilla de ApiSiga: empresas **Agro del Sur**, **Campo Norte** y **PepeElGrillos**; seis usuarios demo con contraseña `siga2026`.
- Suite de pruebas automatizadas (xUnit) y verificación E2E, usadas como evidencia de calidad pero no versionadas como material de producto.
- No hay clientes reales, testimonios, métricas de producción ni material de marketing. No inventarlos.

## Product Principles

1. **El rol manda**: cada pantalla muestra solo lo que el usuario puede ver o hacer, y el servicio vuelve a validarlo.
2. **Aislamiento multiempresa antes que comodidad**: nunca mezclar datos entre empresas.
3. **El campo primero**: evento, tarea, foto y GPS deben poder completarse en un celular con una mano y conectividad irregular.
4. **El contrato de ApiSiga es fijo**: adaptarse a la API, no al revés.
5. **Estados honestos**: si falta un dato o la API falla, decirlo; nunca simular datos.

## Accessibility & Inclusion

- Navegación por teclado con foco visible; los paneles laterales se cierran con Escape.
- Etiquetas ARIA en navegación, paneles y botones de icono.
- Objetivos táctiles cómodos en móvil y tablas anchas con desplazamiento contenido en su tarjeta.
- Texto legible en condiciones de campo (luz solar) y en español claro.
- No hay un estándar formal (por ejemplo WCAG) acordado como requisito contractual; se trata de una expectativa de calidad.
