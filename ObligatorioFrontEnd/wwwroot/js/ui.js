// Utilidades de interfaz compartidas por las páginas Razor.

// Desplaza el inicio de un bloque a la parte superior de la vista. Se usa, por
// ejemplo, al paginar una tabla larga para que la nueva página se lea desde el
// principio y no a mitad de la lista.
export function scrollToId(id) {
    const elemento = document.getElementById(id);
    if (elemento) {
        elemento.scrollIntoView({ block: 'start' });
    }
}

// Bloquea el desplazamiento del fondo mientras el menú móvil está abierto.
export function bloquearScroll(activo) {
    document.body.classList.toggle('nav-abierto', Boolean(activo));
}
