$(document).ready(function () {

    // Mostrar u ocultar campo de apodo
    const sel = document.getElementById('selectDestinatario');
    const campoApodo = document.getElementById('campoApodo');

    if (sel && campoApodo) {

        campoApodo.style.display =
            sel.value === 'usuario' ? 'block' : 'none';

        sel.addEventListener('change', function () {
            campoApodo.style.display =
                this.value === 'usuario' ? 'block' : 'none';
        });
    }

    // Confirmar envío de notificación
    $('#formEnviarNotificacion').on('submit', function (e) {

        e.preventDefault();

        const form = this;

        Swal.fire({
            title: '¿Enviar notificación?',
            text: 'La notificación será enviada a los destinatarios seleccionados.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, enviar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true,
            background: '#1a0933',
            color: 'white',
            confirmButtonColor: '#00B8D9',
            cancelButtonColor: '#ff0f0f'
        }).then((result) => {

            if (result.isConfirmed) {

                form.submit();

            }

        });

    });

});

$(document).ready(function () {

    $('.form-eliminar').on('submit', function (e) {

        e.preventDefault();

        const form = this;

        Swal.fire({
            toast: true,
            position: 'top-end',
            title: '¿Eliminar subasta?',
            text: 'Esta acción eliminará la subasta y no podrá recuperarse.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true,

            background: '#1a0933',
            color: '#fff',

            confirmButtonColor: '#ff0f0f',
            cancelButtonColor: '#00B8D9'
        }).then((result) => {

            if (result.isConfirmed) {
                form.submit();
            }

        });

    });

});