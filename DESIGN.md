---
name: SIGA
description: "Sistema Integrado de Gestión Agrícola: interfaz editorial y serena para la operación agropecuaria."
colors:
  primary: "#173d31"
  primary-deep: "#102d24"
  primary-hover: "#255443"
  secondary: "#4f8a65"
  accent-lime: "#dceabc"
  neutral-bg: "#f7f6f0"
  neutral-surface: "#ffffff"
  neutral-ink: "#1c2924"
  neutral-muted: "#62716b"
  neutral-line: "#dce2da"
  nav-idle: "#607069"
  nav-active: "#eaf2e8"
  sidebar-ink: "#f1f6f0"
  sidebar-idle: "#c2d1c7"
  sidebar-line: "rgba(220, 234, 188, 0.18)"
  sidebar-active: "rgba(220, 234, 188, 0.15)"
  chip-bg: "#e9f0d8"
  chip-ink: "#436733"
  success-bg: "#e3f2e5"
  success-ink: "#2f6b3e"
  warning-bg: "#fff1cb"
  warning-ink: "#82601c"
  danger: "#a83939"
  danger-bg: "#f8e3e3"
  map-approx: "#c58a1b"
typography:
  display:
    fontFamily: "Fraunces, Georgia, serif"
    fontSize: "clamp(2.45rem, 5vw, 4.5rem)"
    fontWeight: 700
    lineHeight: 1.04
    letterSpacing: "-0.045em"
  headline:
    fontFamily: "Fraunces, Georgia, serif"
    fontSize: "clamp(2.5rem, 4.5vw, 3.8rem)"
    fontWeight: 700
    lineHeight: 1.04
    letterSpacing: "-0.045em"
  title:
    fontFamily: "Fraunces, Georgia, serif"
    fontSize: "1.55rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.02em"
  metric:
    fontFamily: "Fraunces, Georgia, serif"
    fontSize: "2.4rem"
    fontWeight: 700
    lineHeight: 1
    letterSpacing: "-0.04em"
  body:
    fontFamily: "DM Sans, Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "DM Sans, Arial, sans-serif"
    fontSize: "0.72rem"
    fontWeight: 700
    lineHeight: 1.3
    letterSpacing: "0.11em"
  button:
    fontFamily: "DM Sans, Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 700
    lineHeight: 1.2
rounded:
  sm: "7px"
  md: "9px"
  lg: "12px"
  xl: "14px"
  pill: "999px"
spacing:
  xs: "0.5rem"
  sm: "0.75rem"
  md: "1rem"
  lg: "1.35rem"
  xl: "2rem"
  "2xl": "2.75rem"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.neutral-surface}"
    typography: "{typography.button}"
    rounded: "{rounded.md}"
    padding: "0 1rem 0 1.2rem"
    height: "3.2rem"
    gap: "0.65rem"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
    typography: "{typography.button}"
    rounded: "{rounded.sm}"
    padding: "0.45rem 0.65rem"
  input:
    backgroundColor: "{colors.neutral-surface}"
    textColor: "{colors.neutral-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "0.7rem 0.8rem"
    height: "3rem"
  card:
    backgroundColor: "{colors.neutral-surface}"
    rounded: "{rounded.lg}"
    padding: "1.35rem"
  sidebar:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.sidebar-ink}"
    padding: "1.7rem 1rem"
  nav-item:
    textColor: "{colors.sidebar-idle}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "0.72rem 0.8rem"
  nav-item-active:
    backgroundColor: "{colors.sidebar-active}"
    textColor: "{colors.accent-lime}"
  table-header:
    backgroundColor: "{colors.neutral-bg}"
    textColor: "{colors.primary}"
    typography: "{typography.label}"
    padding: "0.72rem 0.6rem"
    borderBottom: "2px solid {colors.primary}"
  page-header:
    borderBottom: "1px solid {colors.neutral-line}"
    accent: "{colors.secondary}"
    padding: "0 0 1.5rem"
  chip-status:
    backgroundColor: "{colors.chip-bg}"
    textColor: "{colors.chip-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.3rem 0.55rem"
  report-tab:
    backgroundColor: "transparent"
    textColor: "{colors.neutral-muted}"
    typography: "{typography.body}"
    rounded: "{rounded.pill}"
    padding: "0.5rem 0.85rem"
  report-tab-active:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.neutral-surface}"
  icon-button:
    backgroundColor: "transparent"
    textColor: "{colors.neutral-muted}"
    rounded: "{rounded.pill}"
    size: "2.2rem"
  metric-figure:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
    typography: "{typography.metric}"
---

# Design System: SIGA

## Overview

**Creative North Star: "El Cuaderno de Campo"**

SIGA se lee como un cuaderno de establecimiento bien llevado: papel cálido, tinta verde profunda y títulos con carácter editorial. La interfaz no busca impresionar con efectos sino transmitir orden y confianza; el aire alrededor de los datos es parte del mensaje. Es una superficie **Operate**: la persona viene a registrar, asignar, verificar o consultar, y el sistema debe desaparecer detrás de esa tarea.

La densidad es media y deliberada. Las tablas son legibles, con encabezados en mayúsculas pequeñas, fondo de papel y una regla verde de 2px que las separa del contenido; las tarjetas agrupan sin encerrar. El color se usa con disciplina: verde monte para la acción, la jerarquía y el marco de navegación; lima de brote para la marca, los glifos y la selección; y rojo o ámbar solo cuando el estado lo exige.

La expresividad vive en la tipografía y en el ritmo espacial, no en gradientes ni adornos. Fraunces aporta el tono de registro y almanaque; DM Sans mantiene la legibilidad operativa. La calma visual es lo que permite que un operario cargue un evento al sol y un administrador revise auditoría durante horas.

**Key Characteristics:**
- Fondo crema tipo papel, nunca blanco puro en la página.
- Sidebar y topbar en verde monte: un marco oscuro y estable alrededor del papel.
- Lima de brote en la marca, los glifos y la barra del enlace activo.
- Títulos en Fraunces con tracking negativo; interfaz en DM Sans.
- Bordes finos de 1px; 2px en la cabecera de tabla y 3px en el acento de indicadores.
- Sombras verdes apenas perceptibles; radios contenidos: 9px en controles, 12px en tarjetas, pill en chips.
- Aire generoso y jerarquía clara; densidad media, nunca de planilla apretada.

## Colors

Paleta agrícola y sobria: un verde de monte para la estructura, un verde hoja para el foco, lima de brote para la selección y una escala neutra cálida que imita papel.

### Primary
- **Verde Monte Profundo** (#173d31): botones primarios, sidebar, topbar móvil, títulos de tarjeta y texto de marca. Es el ancla de jerarquía y el marco de la aplicación.
- **Verde Monte Nocturno** (#102d24): fondo plano del panel de acceso, borde del sidebar y texto sobre lima.
- **Verde Monte Encendido** (#255443): estado hover del botón primario.

### Secondary
- **Verde Hoja** (#4f8a65): foco de campos, guion y regla del encabezado, acento de indicadores, línea de tiempo, pines de eventos y detalles de interacción. Nunca compite con el verde monte.
- **Lima Brote** (#dceabc): marca "S", avatar, glifos y barra activa del sidebar, flecha del botón primario y acentos sobre fondo oscuro.

### Neutral
- **Papel de Campo** (#f7f6f0): fondo de página y de paneles laterales.
- **Blanco de Superficie** (#ffffff): tarjetas, campos y modales; es el plano que se eleva sobre el papel.
- **Tinta de Campo** (#1c2924): texto principal.
- **Gris de Lectura** (#62716b): texto secundario, ayudas y metadatos.
- **Línea de Cuaderno** (#dce2da): bordes, separadores y contornos de campos.
- **Gris de Navegación** (#607069): enlaces inactivos en superficies claras.
- **Tinta de Marco** (#f1f6f0): nombres y datos del usuario sobre el sidebar oscuro.
- **Gris de Marco** (#c2d1c7): enlaces inactivos y metadatos sobre el sidebar oscuro.
- **Verde de Selección** (#eaf2e8): hover de enlaces rápidos en superficies claras.

### Named Rules
**La Regla del Papel.** El fondo de página es siempre Papel de Campo (#f7f6f0). El blanco puro se reserva a superficies que deben leerse como elevadas: tarjetas, campos y modales.

**La Regla del Marco de Monte.** El sidebar y la topbar móvil son siempre Verde Monte Profundo (#173d31), nunca blancos. El texto es claro, los glifos son lima y el enlace activo suma una barra lima de 3px en el borde izquierdo.

**La Regla del Verde Escaso.** El verde monte ocupa la acción, la jerarquía y el marco de navegación, no el contenido. Las excepciones son el sidebar, el bloque de bienvenida del inicio y el panel de acceso, donde el verde profundo es el escenario.

## Typography

**Display Font:** Fraunces (con Georgia, serif)
**Body Font:** DM Sans (con Arial, sans-serif)

**Character:** Fraunces aporta el pulso editorial de un cuaderno impreso; DM Sans mantiene la precisión de una herramienta de trabajo. El contraste entre ambas es el gesto visual principal del sistema.

### Hierarchy
- **Display** (700, `clamp(2.45rem, 5vw, 4.5rem)`, 1.04, -0.045em): título del login y bienvenida. Máximo dos líneas.
- **Headline** (700, `clamp(2.5rem, 4.5vw, 3.8rem)`, 1.04, -0.045em): título de página (`h1`), una sola vez por pantalla, con guion de sección y folio.
- **Title** (700, 1.55rem, 1.2, -0.02em): títulos de panel lateral, modales y reportes.
- **Metric** (700, 2.4rem, 1, -0.04em): cifras de indicadores en el inicio y en reportes. Es la única cifra que compite con el título de página.
- **Body** (400, 1rem, 1.5): texto de interfaz, tablas y formularios. Ancho de lectura máximo 42rem en párrafos introductorios.
- **Label** (700, 0.72rem, 1.3, 0.11em, mayúsculas): eyebrows, encabezados de tabla, leyendas de grupo y chips.

### Named Rules
**La Regla del Título Único.** Fraunces se usa en títulos de página, nombres de marca y cifras editoriales. No se usa en tablas, botones, campos ni etiquetas.

**La Regla de la Etiqueta Pequeña.** Todo encabezado de tabla o grupo va en DM Sans 700, mayúsculas y tracking amplio: 0.72rem en eyebrows y leyendas, 0.7rem en cabeceras de tabla. Nunca en el mismo tamaño que el cuerpo.

**La Regla de la Cifra Editorial.** Los indicadores se muestran como cifras grandes en Fraunces, sin fondo ni chip. La etiqueta va debajo en DM Sans y el detalle en Gris de Lectura. La tarjeta que la contiene lleva un acento superior de 3px en Verde Hoja.

**La Regla del Encabezado Editorial.** Cada página abre con un guion Verde Hoja antes del eyebrow, un `h1` en Fraunces, una regla inferior de 1px y un folio de dos dígitos alineado a la derecha. El folio se calcula desde la ruta, no se escribe en cada página.

## Layout

El armazón es una grilla de dos columnas: sidebar fijo de 17.5rem en verde monte y contenido flexible sobre papel. El contenido se centra con un máximo de 1120px y un padding fluido de `clamp(2rem, 5vw, 4.5rem)`. La densidad es media: bloques separados por 1.25rem, tarjetas con 1.35rem internos y encabezados de página con una regla inferior, 1.5rem de padding y 2rem a 2.75rem de aire inferior.

Las grillas internas siguen el contenido: `1.35fr / 0.65fr` para contenido y contexto, tres columnas iguales para métricas del inicio, dos columnas para formularios y `auto-fill` para fotografías. Las tablas anchas viven dentro de `.table-wrap` con scroll horizontal contenido; nunca empujan el ancho de la página.

Los cortes responsive son parte del sistema:
- **≤1000px:** el mapa y su lista pasan a una columna.
- **≤800px:** sidebar off-canvas con barra superior fija y botón ☰; grillas de contenido y formularios a una columna.
- **≤700px:** los tabs de reportes se compactan y el encabezado se apila.
- **≤540px:** filtros, fechas y coordenadas a una columna; acciones alineadas a la izquierda; paneles a ancho completo.

El ritmo vertical usa la escala de espaciado: 0.5rem entre elementos emparentados, 0.75rem entre campos, 1rem a 1.35rem entre bloques y 2rem a 2.75rem entre secciones.

## Elevation & Depth

El sistema es **híbrido**: las tarjetas son casi planas y se separan del papel con una sombra verde de opacidad mínima; los overlays (paneles laterales, modales y drawer de reportes) se elevan con sombras amplias y direccionales. La profundidad comunica jerarquía de capa, no decoración.

### Shadow Vocabulary
- **Tarjeta en reposo** (`0 6px 20px rgba(23,61,49,.045)`): separa tarjetas y enlaces de catálogo del fondo crema.
- **Barra de reportes** (`0 8px 24px rgba(23,61,49,.06)`): eleva el selector de reportes sticky.
- **Chip de rol** (`0 2px 8px rgba(23,61,49,.08)`): microelevación del chip de contexto.
- **Panel lateral / drawer** (`-18px 0 45px rgba(16,45,36,.2)`): entrada desde el borde derecho.
- **Modal de auditoría** (`0 22px 60px rgba(16,45,36,.3)`): máxima elevación, centrado.
- **Menú móvil** (`0 0 60px rgba(16,45,36,.22)`): separa el drawer off-canvas del contenido.

### Named Rules
**La Regla de la Sombra Mínima.** Una tarjeta en reposo nunca supera 0.06 de opacidad. Si hace falta más separación, se ajusta el borde o el espaciado, no la sombra.

**La Regla del Overlay Elevado.** Solo las capas que interrumpen el flujo (panel, modal, drawer, menú móvil) pueden usar sombras amplias y oscuras.

## Shapes

La forma es contenida y funcional. Los controles (botones, campos, selects) usan 9px de radio; las acciones compactas de tabla, 7px; las tarjetas, 12px; los contenedores de reportes y modales, 14px. Chips, badges y pestañas usan pill completo (999px). Los botones de icono y avatares son círculos.

El borde estándar es de 1px en Línea de Cuaderno (#dce2da). Las excepciones son estructurales: 2px en la regla inferior de la cabecera de tabla, 3px en el acento superior de las tarjetas de indicadores y 3px en la barra del enlace activo. No hay bordes dobles ni líneas decorativas. Las fotografías recortan a 6px y las imágenes de tarjeta usan `object-fit: cover`. No hay clipping decorativo ni esquinas asimétricas: la geometría es tranquila y repetible.

## Components

### Buttons
- **Shape:** radio de 9px, altura mínima 3.2rem, padding `0 1rem 0 1.2rem` y `gap` de 0.65rem; el texto se distribuye con `space-between` cuando incluye un icono.
- **Primary:** Verde Monte Profundo (#173d31) con texto blanco, peso 700. Es la única acción sólida de la pantalla. Cuando lleva flecha, esta vive dentro de un círculo lima de 2rem.
- **Hover / Focus:** hover a #255443 con `translateY(-1px)`; foco visible con anillo de 3px `rgba(79,138,101,.35)` y offset de 2px.
- **Secondary:** fondo transparente, borde de 1px, texto verde, radio 7px y padding `.45rem .65rem`. Se usa en acciones de tabla y formularios.
- **Destructive:** mismo formato secundario con borde #e4baba y texto #a83939.

### Chips
- **Style:** fondo Verde de Selección (#eaf2e8) o Chip (#e9f0d8), texto #436733, pill completo, 700 en 0.78rem. El chip de rol del encabezado agrega sombra de 2px.
- **State:** los estados de campaña y tarea usan badges propios: activo (#e3f2e5 / #397148), cerrado (#edf0ed / #59645e), anulado (#f8e3e3 / #993b3b) y planificado (#fff1cb / #82601c).

### Cards / Containers
- **Corner Style:** 12px; 14px en contenedores de reportes y modales.
- **Background:** Blanco de Superficie sobre Papel de Campo.
- **Shadow Strategy:** sombra mínima en reposo; ver Elevation & Depth.
- **Border:** 1px Línea de Cuaderno. Las tarjetas de indicadores suman un acento superior de 3px en Verde Hoja.
- **Header Rule:** el encabezado interno de tarjeta se separa con una regla de 1px y 0.85rem de padding inferior.
- **Internal Padding:** 1.35rem; 1.6rem en paneles de reportes; 2.1rem en el bloque de bienvenida.

### Metrics
- **Style:** cifra en Fraunces 700, 2.4rem, tracking -0.04em y color Verde Monte; sin fondo, borde ni chip.
- **Context:** tarjetas de indicadores del inicio y de reportes. La etiqueta va debajo en DM Sans 700, 1rem; el detalle en Gris de Lectura 0.88rem.
- **Currency:** el costo conserva la misma cifra editorial a 1.5rem para no romper el ancho de la tarjeta.

### Inputs / Fields
- **Style:** fondo blanco, borde de 1px #dce2da, radio 9px, altura mínima 3rem, padding `.7rem .8rem`, texto de 1rem.
- **Focus:** borde Verde Hoja (#4f8a65) y halo `0 0 0 3px rgba(79,138,101,.14)`.
- **Error / Disabled:** error en bloque con fondo #fff1f1, borde #efcccc y texto #ae3131; deshabilitado con opacidad 0.5 y cursor `not-allowed`.

### Navigation
- **Desktop:** sidebar verde monte de 17.5rem con borde derecho en Verde Monte Nocturno. Marca en lima, enlaces en DM Sans 600, 0.94rem, color #c2d1c7, radio 8px y padding `.72rem .8rem`. Activo y hover con fondo `rgba(220,234,188,.15)` y texto lima; el activo suma una barra lima de 3px en el borde izquierdo. Hover con desplazamiento de 2px.
- **Mobile:** a ≤800px el sidebar pasa a drawer off-canvas de `min(20rem, 85vw)` sobre fondo oscurecido; topbar sticky verde monte con botón ☰ lima, cierre con ✕, fondo, Escape y navegación. El pie conserva selector de empresa en superficie clara y cierre de sesión.
- **Glyphs:** cada enlace lleva un glifo monocromo a la izquierda como ancla visual; hereda el lima del sidebar y el texto siempre está presente. No se usan emojis a color.

### Page Header
- **Style:** eyebrow en Verde Monte con guion Verde Hoja de 1.6rem, `h1` en Fraunces con `clamp(2.5rem, 4.5vw, 3.8rem)`, regla inferior de 1px con segmento Verde Hoja de 4.5rem y folio de dos dígitos en Verde Monte Encendido a la derecha.
- **Folio:** se calcula desde la ruta en el layout y se inyecta como variable CSS; en móvil se oculta para no competir con el chip de rol.

### Tables
- **Style:** ancho completo, 0.88rem. Cabecera con fondo Papel de Campo (#f7f6f0), etiqueta en Label (0.7rem, mayúsculas, Verde Monte) y regla inferior de 2px en Verde Monte. Filas con borde inferior de 1px y padding `.95rem .6rem`.
- **Behavior:** hover de fila #f7f9f4; celdas numéricas o de moneda con `white-space: nowrap`; tablas anchas dentro de `.table-wrap` con scroll horizontal y `min-width` de 38rem (34rem en móvil).

### Side Panel
- **Style:** drawer derecho de `min(38rem, 100%)`, fondo Papel de Campo, padding 1.6rem, sombra `-18px 0 45px rgba(16,45,36,.2)`.
- **Behavior:** se abre con animación de 0.22s, enfoca el contenedor y cierra con Escape, ✕ o clic en el fondo. Arrastrar desde adentro y soltar afuera no lo cierra.

## Do's and Don'ts

### Do:
- **Do** usar Papel de Campo (#f7f6f0) como fondo de página y blanco solo en superficies elevadas.
- **Do** reservar Fraunces para títulos y DM Sans para todo lo operativo.
- **Do** separar con 1px de #dce2da y sombras de opacidad ≤0.06 en reposo.
- **Do** mantener radios de 9px en controles, 12px en tarjetas y pill en chips.
- **Do** dejar aire: 1.25rem entre bloques y 2rem o más antes de un título de página.
- **Do** usar el verde monte para una sola acción primaria por pantalla.
- **Do** usar el sidebar verde monte como marco fijo y reservar el papel para el contenido.
- **Do** abrir cada página con guion, regla y folio; la cabecera de tabla va en papel con regla de 2px.
- **Do** acompañar cada estado con texto además del color.

### Don't:
- **Don't** usar una paleta SaaS azul ni el look de dashboard corporativo genérico.
- **Don't** agregar gradientes decorativos, glassmorphism ni brillos.
- **Don't** convertir las tablas en planillas apretadas sin jerarquía ni aire.
- **Don't** usar blanco puro como fondo de página ni como fondo del sidebar.
- **Don't** usar emojis a color en la navegación; los glifos deben heredar el lima.
- **Don't** introducir una segunda familia tipográfica de display.
- **Don't** superar 0.06 de opacidad en sombras de tarjetas en reposo.
- **Don't** usar mayúsculas en párrafos o cuerpos de texto.
