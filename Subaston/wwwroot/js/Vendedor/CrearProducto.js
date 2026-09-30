$(document).ready(function () {
    // Establecer la zona horaria actual en minutos
    const offsetInput = document.getElementById('timezone-offset');
    if (offsetInput) {
        offsetInput.value = new Date().getTimezoneOffset();
    }

    $('form').on('submit', function (e) {
        e.preventDefault();
        var form = this;

        Swal.fire({
            title: '¿Quieres publicar este producto?',
            text: 'Tu producto será enviado a revisión antes de publicarse.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Si, publicar',
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

document.getElementById('input-imagen').addEventListener('change', function () {
    const nombre = this.files[0] ? this.files[0].name : 'Ningun archivo seleccionado';
    document.getElementById('nombre-archivo').textContent = nombre;
});