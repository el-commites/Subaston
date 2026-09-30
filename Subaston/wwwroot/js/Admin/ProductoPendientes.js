document.addEventListener('DOMContentLoaded', function () {

    console.log("ProductosPendientes.js cargado");

    document.querySelectorAll('.btn-rechazar').forEach(btn => {

        btn.addEventListener('click', function (e) {

            console.log("CLICK RECHAZAR");

            e.preventDefault();

            const url = this.href;

            Swal.fire({
                title: '¿Rechazar publicación?',
                text: 'El producto pendiente será eliminado.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, rechazar',
                cancelButtonText: 'Cancelar',
                background: '#1a0933',
                color: '#ffffff',
                position: 'top-end',
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6'
            }).then((result) => {

                if (result.isConfirmed) {
                    window.location.href = url;
                }

            });

        });

    });

});