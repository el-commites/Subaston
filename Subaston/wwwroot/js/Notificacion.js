// Convierte una fecha ISO a texto relativo en español
function tiempoRelativo(fechaStr) {
    const fecha = new Date(fechaStr);
    const ahora = new Date();
    const diff = Math.floor((ahora - fecha) / 1000); // diferencia en segundos

    if (diff < 60) return 'Hace un momento';
    if (diff < 3600) return `Hace ${Math.floor(diff / 60)} min`;
    if (diff < 86400) return `Hace ${Math.floor(diff / 3600)} h`;
    if (diff < 604800) return `Hace ${Math.floor(diff / 86400)} días`;

    // Si tiene más de 7 días, mostrar fecha corta
    return fecha.toLocaleDateString('es-MX', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
    });
}

$(document).ready(function () {

    // Revisa si hay notificaciones sin leer y muestra el punto rojo
    function revisarNotificaciones() {
        $.get('/Notificaciones/ConteoNuevas', function (data) {
            if (data.total > 0) {
                $('#puntoNotificacion').show();
            } else {
                $('#puntoNotificacion').hide();
            }
        });
    }

    // Carga y renderiza las notificaciones en el panel
    function cargarNotificaciones() {
        $.get('/Notificaciones/Obtener', function (data) {
            let html = '';

            if (data.length === 0) {
                html = '<div class="item-notificacion">No tienes notificaciones.</div>';
            }

            data.forEach(function (n) {
                let clase = n.leida ? '' : 'no-leida';
                let url = n.urlDestino || '#';

                html += `
                    <div class="item-notificacion ${clase}">
                        <a href="${url}">
                            <strong>${n.titulo}</strong>
                            <span>${n.mensaje}</span>
                            <small>${tiempoRelativo(n.fecha)}</small>
                        </a>
                    </div>
                `;
            });

            $('#listaNotificaciones').html(html);
        });
    }

    // Click en el botón de campanita
    $('#btnNotificaciones').click(function (e) {
        e.preventDefault();
        $('#panelNotificaciones').toggle();

        // Solo carga y marca como leídas si se está abriendo
        if ($('#panelNotificaciones').is(':visible')) {
            cargarNotificaciones();
            $.post('/Notificaciones/MarcarComoLeidas', function () {
                $('#puntoNotificacion').hide();
            });
        }
    });

    // Cerrar panel al hacer click fuera
    $(document).click(function (e) {
        if (!$(e.target).closest('.notificaciones-wrapper').length) {
            $('#panelNotificaciones').hide();
        }
    });

    // Revisar al cargar la página
    revisarNotificaciones();

    // Revisar cada 30 segundos
    setInterval(revisarNotificaciones, 30000);
});