// Interop de Leaflet para la página /mapa y el mini-mapa de eventos.
// Leaflet se descarga bajo demanda la primera vez que se crea un mapa.

const mapas = new Map();

let leafletPromesa = null;

function leaflet() {
    return window.L ?? null;
}

// Carga /lib/leaflet/leaflet.js una sola vez. Sin esto, 144 KB viajarían en
// todas las páginas aunque el usuario nunca abra un mapa.
function cargarLeaflet() {
    if (leaflet()) return Promise.resolve(window.L);
    if (leafletPromesa) return leafletPromesa;

    leafletPromesa = new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'lib/leaflet/leaflet.js';
        script.async = true;
        script.onload = () => leaflet()
            ? resolve(window.L)
            : reject(new Error('Leaflet no se inicializó.'));
        script.onerror = () => reject(new Error('No se pudo cargar Leaflet.'));
        document.head.appendChild(script);
    });

    return leafletPromesa;
}

function opcion(opciones, camel, pascal, porDefecto) {
    if (!opciones) return porDefecto;
    if (opciones[camel] !== undefined && opciones[camel] !== null) return opciones[camel];
    if (opciones[pascal] !== undefined && opciones[pascal] !== null) return opciones[pascal];
    return porDefecto;
}

function crearIcono(tipo, tamano) {
    const L = leaflet();
    return L.divIcon({
        className: `mapa-pin mapa-pin--${tipo}`,
        html: '<span></span>',
        iconSize: [tamano, tamano],
        iconAnchor: [tamano / 2, tamano / 2],
        popupAnchor: [0, -(tamano / 2) - 4]
    });
}

function construirPopup(punto) {
    const contenedor = document.createElement('div');
    contenedor.className = 'mapa-popup';

    const titulo = document.createElement('strong');
    titulo.textContent = punto.nombre ?? '';
    contenedor.appendChild(titulo);

    if (punto.detalle) {
        const detalle = document.createElement('p');
        detalle.textContent = punto.detalle;
        contenedor.appendChild(detalle);
    }

    if (punto.enlace) {
        const enlace = document.createElement('a');
        enlace.href = punto.enlace;
        enlace.textContent = punto.enlaceTexto || 'Ver detalle';
        contenedor.appendChild(enlace);
    }

    return contenedor;
}

function agregarTiles(mapa, opciones) {
    const L = leaflet();
    const tileUrl = opcion(opciones, 'tileUrl', 'TileUrl', 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png');
    const attribution = opcion(
        opciones,
        'attribution',
        'Attribution',
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>');

    return L.tileLayer(tileUrl, {
        attribution: attribution,
        maxZoom: 19
    }).addTo(mapa);
}

export async function crearMapa(id, opciones, referencia) {
    try {
        await cargarLeaflet();
    } catch {
        return false;
    }

    const L = leaflet();
    const contenedor = document.getElementById(id);
    if (!L || !contenedor) return false;

    destruir(id);

    const centroLat = Number(opcion(opciones, 'centroLat', 'CentroLat', -34.9011));
    const centroLng = Number(opcion(opciones, 'centroLng', 'CentroLng', -56.1645));
    const zoom = Number(opcion(opciones, 'zoom', 'Zoom', 7));

    const mapa = L.map(contenedor, { zoomControl: true }).setView([centroLat, centroLng], zoom);
    agregarTiles(mapa, opciones);

    let avisado = false;
    mapa.on('tileerror', () => {
        if (avisado || !referencia) return;
        avisado = true;
        referencia.invokeMethodAsync('OnTilesError');
    });

    L.control.scale({ imperial: false }).addTo(mapa);

    const capa = L.layerGroup().addTo(mapa);
    mapas.set(id, { mapa, capa, marcadores: new Map(), referencia });

    requestAnimationFrame(() => {
        try {
            mapa.invalidateSize();
        } catch {
            // El contenedor pudo desaparecer durante la navegación.
        }
    });
    return true;
}

export function dibujarPuntos(id, json) {
    const L = leaflet();
    const entrada = mapas.get(id);
    if (!L || !entrada) return false;

    const puntos = JSON.parse(json ?? '[]');
    entrada.capa.clearLayers();
    entrada.marcadores.clear();

    for (const punto of puntos) {
        const esEvento = punto.tipo === 'evento';
        const tamano = esEvento ? 14 : 22;
        const marcador = L.marker([punto.latitud, punto.longitud], {
            icon: crearIcono(punto.tipo, tamano),
            title: punto.nombre ?? '',
            riseOnHover: true
        });

        marcador.bindPopup(construirPopup(punto));
        marcador.addTo(entrada.capa);
        entrada.marcadores.set(punto.id, marcador);
    }

    return true;
}

export function ajustarVista(id) {
    const L = leaflet();
    const entrada = mapas.get(id);
    if (!L || !entrada) return;

    try {
        const marcadores = entrada.capa.getLayers();
        if (marcadores.length === 0) return;

        if (marcadores.length === 1) {
            entrada.mapa.setView(marcadores[0].getLatLng(), 13, { animate: false });
            return;
        }

        const limites = L.latLngBounds(marcadores.map((marcador) => marcador.getLatLng()));
        entrada.mapa.fitBounds(limites.pad(0.25), { maxZoom: 14, animate: false });
    } catch {
        // La vista pudo quedar inválida si el mapa se destruyó al navegar.
    }
}

export async function crearSelector(id, latitud, longitud, referencia, opciones) {
    try {
        await cargarLeaflet();
    } catch {
        return false;
    }

    const L = leaflet();
    const contenedor = document.getElementById(id);
    if (!L || !contenedor) return false;

    destruir(id);

    // En formularios conviene arrancar sin pin: todavía no hay ubicación elegida.
    const conPin = opcion(opciones, 'conPin', 'ConPin', true) !== false;
    const zoomRueda = opcion(opciones, 'zoomRueda', 'ZoomRueda', true) !== false;
    const titulo = opcion(opciones, 'titulo', 'Titulo', 'Ubicación');

    const mapa = L.map(contenedor, { zoomControl: true, scrollWheelZoom: zoomRueda })
        .setView([latitud, longitud], 14);
    agregarTiles(mapa, null);
    L.control.scale({ imperial: false }).addTo(mapa);

    const entrada = {
        mapa,
        capa: L.layerGroup().addTo(mapa),
        marcador: null,
        colocar: null
    };

    const avisar = (posicion) => {
        if (!referencia) return;
        referencia.invokeMethodAsync('OnPinMovido', posicion.lat, posicion.lng);
    };

    entrada.colocar = (posicion, notificar) => {
        if (!entrada.marcador) {
            entrada.marcador = L.marker(posicion, {
                draggable: true,
                icon: crearIcono('seleccion', 26),
                title: titulo
            }).addTo(mapa);

            entrada.marcador.on('dragend', () => avisar(entrada.marcador.getLatLng()));
        } else {
            entrada.marcador.setLatLng(posicion);
        }

        if (notificar) avisar(entrada.marcador.getLatLng());
    };

    mapa.on('click', (evento) => entrada.colocar(evento.latlng, true));

    if (conPin) {
        entrada.colocar(L.latLng(latitud, longitud), false);
    }

    mapas.set(id, entrada);

    requestAnimationFrame(() => {
        try {
            mapa.invalidateSize();
        } catch {
            // El contenedor pudo desaparecer durante la navegación.
        }
    });
    return true;
}

export function fijarPin(id, latitud, longitud) {
    const entrada = mapas.get(id);
    if (!entrada || !entrada.colocar) return;

    const posicion = [latitud, longitud];
    entrada.colocar(posicion, false);
    entrada.mapa.panTo(posicion, { animate: false });
}

export function quitarPin(id) {
    const entrada = mapas.get(id);
    if (!entrada || !entrada.marcador) return;

    entrada.mapa.removeLayer(entrada.marcador);
    entrada.marcador = null;
}

export function obtenerUbicacionActual() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('El navegador no permite obtener la ubicación.'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (posicion) => resolve([posicion.coords.latitude, posicion.coords.longitude]),
            (error) => reject(new Error(error.message || 'No se pudo obtener la ubicación.')),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 });
    });
}

export async function buscarDireccion(consulta) {
    const texto = (consulta ?? '').trim();
    if (!texto) return null;

    const url = 'https://nominatim.openstreetmap.org/search'
        + '?format=json&limit=1&countrycodes=uy&q=' + encodeURIComponent(texto);

    const respuesta = await fetch(url, {
        headers: { 'Accept': 'application/json', 'Accept-Language': 'es' }
    });

    if (!respuesta.ok) {
        throw new Error('El servicio de direcciones no respondió. Marcá el punto en el mapa.');
    }

    const resultados = await respuesta.json();
    if (!Array.isArray(resultados) || resultados.length === 0) return null;

    const primero = resultados[0];
    return JSON.stringify({
        Latitud: Number.parseFloat(primero.lat),
        Longitud: Number.parseFloat(primero.lon),
        Direccion: primero.display_name ?? texto
    });
}

export function destruir(id) {
    const entrada = mapas.get(id);
    if (!entrada) return;

    mapas.delete(id);

    try {
        entrada.mapa.stop();
        entrada.mapa.off();
        entrada.mapa.remove();
    } catch {
        // Leaflet falla si el contenedor ya fue retirado del DOM.
    }
}
